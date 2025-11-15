public class SalaryReport
{
    public string EmployeeID { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public int TotalShifts { get; set; }
    public double TotalHours { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal Bonus { get; set; }
    public decimal Penalty { get; set; }
    public decimal TotalOvertimeSalary { get; set; }
    public decimal TotalSalary { get; set; }
    public bool IsFinalized { get; set; } 
    public int FinalizationCount { get; set; }
}
