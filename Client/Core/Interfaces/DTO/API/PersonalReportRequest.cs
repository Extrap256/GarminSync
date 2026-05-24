using System;
using System.Collections.Generic;
using System.Text;

namespace Client.Core.Interfaces.DTO.API
{
    public record PersonalReportRequest(DateTime From, DateTime To);
}
