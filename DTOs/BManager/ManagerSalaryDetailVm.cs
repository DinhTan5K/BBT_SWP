namespace start.DTOs
{
    public class ManagerSalaryDetailVm
    {
        public decimal GrossSalary { get; set; }
        public decimal Insurance { get; set; }
        public decimal TaxableIncome { get; set; }
        public decimal PersonalDeduction { get; set; }
        public decimal AssessableIncome { get; set; }
        public decimal IncomeTax { get; set; }
        public decimal NetSalary { get; set; }
    }
}