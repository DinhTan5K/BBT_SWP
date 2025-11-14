// WeeklyEmployeeSummary.cs
using System;

namespace start.Models
{
    public class WeeklySummary
    {
        public string EmployeeID { get; set; }
        public string FullName { get; set; }
        public double TotalHours { get; set; } // Tổng giờ làm thực tế
        public int TotalShifts { get; set; } // Tổng số ca đã hoàn thành
        public int ShiftsPending { get; set; } // Số ca chờ duyệt
    }
}