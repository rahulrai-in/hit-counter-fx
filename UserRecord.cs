using Microsoft.Azure.Cosmos.Table;

namespace RR.AZLabs.HitCounter
{
    public class UserRecord : TableEntity
    {
        public UserRecord()
        {
            // no-op. Required by serializer.
        }

        public UserRecord(string userId, string partitionKey = "User", bool isBlocked = false)
        {
            PartitionKey = partitionKey;
            RowKey = userId;
            IsBlocked = isBlocked;
        }

        public bool IsBlocked { get; set; }
    }
}