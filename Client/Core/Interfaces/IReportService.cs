using System;
using System.Collections.Generic;
using System.Text;
using Client.Core.Interfaces.DTO.API;

namespace Client.Core.Interfaces
{
    public interface IReportService
    {
        /// <summary>
        /// Получение личного отчета за период
        /// </summary>
        Task<PersonalReportResponse> GetPersonalReportAsync(PersonalReportRequest request, CancellationToken ct = default);
    }
}
