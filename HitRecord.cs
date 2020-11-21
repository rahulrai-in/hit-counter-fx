using Microsoft.Azure.Cosmos.Table;

namespace RR.AZLabs.HitCounter
{
    public class HitRecord : TableEntity
    {
        public HitRecord()
        {
            // no-op. Required by serializer.
        }

        public HitRecord(string user, string pageId, long hitCount)
        {
            PartitionKey = user;
            RowKey = pageId;
            HitCount = hitCount;
        }

        public long HitCount { get; set; }
    }
}