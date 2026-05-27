using System;
using System.Collections.Generic;
using System.Text;

namespace Client.Core.Interfaces.DTO.API
{
    public record BatchSyncResult(int TotalProcessed,
        int TotalSynced,
        int TotalFailed,
        List<BatchSyncError> Errors);
}
