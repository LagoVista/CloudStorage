using LagoVista.Core.Models.UIMetaData;
using System;

public static class ListRequestCursorWriter
{
    public static void SetNextCursor<T>(
        this ListRequest req,
        T lastItem,
        Func<T, DateTime> sortKey,
        Func<T, string> rowKey)
    {
        if (lastItem == null)
        {
            req.NextPartitionKey = null;
            req.NextRowKey = null;
            return;
        }

        req.NextPartitionKey = sortKey(lastItem).ToString("O");
        req.NextRowKey = rowKey(lastItem);
    }
}
