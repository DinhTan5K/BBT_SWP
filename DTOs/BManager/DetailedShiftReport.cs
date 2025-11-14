using System;
using System.ComponentModel.DataAnnotations; // <-- THÊM DÒNG NÀY
using start.Models;
public class DetailedShiftReport
{
    // --- Thuộc tính lấy từ WorkSchedule (Dữ liệu ca làm) ---
    public int WorkScheduleID { get; set; }
    public string EmployeeID { get; set; } = null!;
    public DateTime Date { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? Shift { get; set; }
    
    // --- Thuộc tính tính toán (Phục vụ báo cáo) ---
    [Display(Name = "Duration (Hrs)")]
    public double DurationHours => CheckOutTime.HasValue && CheckInTime.HasValue 
                                   ? (CheckOutTime.Value - CheckInTime.Value).TotalHours 
                                   : 0;

    [Display(Name = "Base Rate")]
    public decimal BaseRate { get; set; }
    
    [Display(Name = "Multiplier")]
    public double Multiplier { get; set; }
    
    [Display(Name = "Total Pay")]
    public decimal TotalShiftPay { get; set; }
    
    // Thêm các thuộc tính hiển thị (ví dụ: Tên nhân viên)
    public string? FullName { get; set; }
}