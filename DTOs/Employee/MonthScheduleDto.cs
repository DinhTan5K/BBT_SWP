using System;
using System.Collections.Generic;

namespace start.Models
{
    public class MonthScheduleDto
    {
        public Employee Employee { get; set; } = null!;
        public IReadOnlyList<WorkSchedule> Items { get; set; } = Array.Empty<WorkSchedule>();
        public int Month { get; set; }
        public int Year  { get; set; }
    }
}