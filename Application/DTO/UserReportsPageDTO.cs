using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTO
{
    public class UserReportsPageDTO
    {
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public int TotalReports { get; set; }
        public int AdmittedReports { get; set; }
        public List<UserReportDTO> Reports { get; set; }
    }
}
