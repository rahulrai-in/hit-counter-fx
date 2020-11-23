using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace RR.AZLabs.HitCounter
{
    public class Function
    {
        private const string FxName = "hitcounter";
        private const string UserListPk = "User";
        private const string ImageFile = "RR.AZLabs.HitCounter.image.svg";
        private const string RecordStore = "hitcounterstore";
        private const string UserStore = "userstore";
        private const string ResponseType = "image/svg+xml; charset=utf-8";
        private static string _imageString;
        private static bool _recordTableCreationCheckExecuted;
        private static bool _userTableCreationCheckExecuted;

        private static readonly TableRequestOptions RequestOptions = new TableRequestOptions
        {
            RetryPolicy = new ExponentialRetry(TimeSpan.FromMilliseconds(100), 3)
        };

        private static readonly Regex PageIdRegex = new Regex(@"^[a-zA-Z0-9-_]+$");

        // Default concurrent requests allowed for function on consumption plan is 100.
        private static readonly SemaphoreSlim SlimLock = new SemaphoreSlim(1, 100);

        [FunctionName(FxName)]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "hc/{user}/{pageId?}")]
            Options options,
            string user,
            string pageId,
            HttpRequest request,
            [Table(RecordStore)] CloudTable recordTable,
            [Table(UserStore)] CloudTable userTable,
            ILogger logger)
        {
            try
            {
                HitRecord record;
                user = user.ToLowerInvariant();
                pageId = pageId?.ToLowerInvariant().Trim();

                logger.LogInformation("Request {Type} from {User} for record {Page}", request.Method, user, pageId);
                if (string.IsNullOrWhiteSpace(user) || user.Length > 10 || !user.All(char.IsLetterOrDigit))
                {
                    return new BadRequestResult();
                }

                if (request.Method.Equals("post", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation("Attempting to register {User}", user);
                    return await RegisterUser(user, userTable) ? (IActionResult) new OkResult() : new ConflictResult();
                }

                if (string.IsNullOrWhiteSpace(pageId) || pageId.Length > 50 || !PageIdRegex.IsMatch(pageId))
                {
                    return new BadRequestResult();
                }

                if (!await IsUserAllowed(user, userTable))
                {
                    return new UnauthorizedResult();
                }

                try
                {
                    // Try to avoid concurrency conflicts in a single function host.
                    await SlimLock.WaitAsync();

                    // Case insensitive record entity fetch
                    record = await FetchRecord(recordTable, user, pageId);
                    if (!options.NoCount)
                    {
                        ++record.HitCount;
                    }

                    // Update record
                    await UpdateEntity(record, recordTable);
                }
                finally
                {
                    SlimLock.Release();
                }

                // Explicitly tell clients to not cache the image.
                return NoCacheContentResponse(request, await PrepareImage(record, options));
            }
            catch (Exception exception)
            {
                logger.LogError(exception.Demystify(), "Error processing request for {User}. Record: {Page}", user,
                    pageId);
                throw;
            }
        }

        private static async Task<HitRecord> FetchRecord(CloudTable recordTable, string user, string pageId)
        {
            // Don't try to create table on every request
            if (!_recordTableCreationCheckExecuted)
            {
                await recordTable.CreateIfNotExistsAsync();
                _recordTableCreationCheckExecuted = true;
            }

            // Case block list record fetch
            var query = TableOperation.Retrieve<HitRecord>(user, pageId);
            var result = recordTable.Execute(query, RequestOptions);
            return result.Result is HitRecord hitRecord ? hitRecord : new HitRecord(user, pageId, 0);
        }

        private static IActionResult NoCacheContentResponse(HttpRequest request, string preparedImage)
        {
            request.HttpContext.Response.Headers.Add("cache-control", "no-cache, no-store, must-revalidate, max-age=0");
            return new ContentResult
            {
                Content = preparedImage,
                ContentType = ResponseType,
                StatusCode = (int) HttpStatusCode.OK
            };
        }

        private static async Task<bool> RegisterUser(string user, CloudTable userTable)
        {
            try
            {
                var operation = TableOperation.Insert(new UserRecord(user));
                await userTable.ExecuteAsync(operation);
            }
            catch (StorageException exception)
                when (exception.RequestInformation.HttpStatusCode == (int) HttpStatusCode.Conflict)
            {
                return false;
            }

            return true;
        }

        private static async Task<bool> IsUserAllowed(string user, CloudTable userTable)
        {
            // Don't try to create table on every request
            if (!_userTableCreationCheckExecuted)
            {
                await userTable.CreateIfNotExistsAsync();
                _userTableCreationCheckExecuted = true;
            }

            // Case block list record fetch
            var query = TableOperation.Retrieve<UserRecord>(UserListPk, user);
            var result = userTable.Execute(query, RequestOptions);
            return result.Result is UserRecord userRecord && !userRecord.IsBlocked;
        }

        private static async Task<string> PrepareImage(HitRecord record, Options options)
        {
            _imageString ??= await GetImageFromResource(ImageFile);
            var imageSb = new StringBuilder(_imageString);
            imageSb.Replace("@Count", FormatCount(record.HitCount, options));
            imageSb.Replace("@EyeBg", options.IconBackgroundColorCode);
            imageSb.Replace("@TextBg", options.TextBackgroundColorCode);
            imageSb.Replace("@EyeColor", options.EyeColorCode);
            imageSb.Replace("@TextColor", options.TextColorCode);
            return imageSb.ToString();
        }

        private static string FormatCount(long count, Options options)
        {
            if (!options.IsKmbFormat)
            {
                return count.ToString();
            }

            var index = (long) Math.Pow(10, (int) Math.Max(0, Math.Log10(count) - 2));
            count = count / index * index;

            if (count >= 1000000000)
            {
                return (count / 1000000000D).ToString("0.##") + "B";
            }

            if (count >= 1000000)
            {
                return (count / 1000000D).ToString("0.##") + "M";
            }

            if (count >= 1000)
            {
                return (count / 1000D).ToString("0.##") + "K";
            }

            return count.ToString("#,0");
        }

        private static async Task<string> GetImageFromResource(string imageFile)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(imageFile);
            var reader = new StreamReader(stream!);
            var imageString = await reader.ReadToEndAsync();
            return imageString;
        }

        private static async Task UpdateEntity(ITableEntity record, CloudTable cloudTable)
        {
            // I'll lose record for performance.
            record.ETag = "*";
            var operation = TableOperation.InsertOrReplace(record);
            await cloudTable.ExecuteAsync(operation);
        }
    }
}