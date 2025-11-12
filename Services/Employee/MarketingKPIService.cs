using Microsoft.EntityFrameworkCore;
using start.Data;
using start.Models;
using start.Models.ViewModels;

namespace start.Services
{
    public class MarketingKPIService : IMarketingKPIService
    {
        private readonly ApplicationDbContext _db;
        private const decimal TARGET_SCORE = 70.0m; // Điểm KPI mục tiêu 70%
        private const decimal BONUS_RATE_70_80 = 0.05m; // 5% bonus nếu KPI 70-80%
        private const decimal BONUS_RATE_80_90 = 0.10m; // 10% bonus nếu KPI 80-90%
        private const decimal BONUS_RATE_90_100 = 0.15m; // 15% bonus nếu KPI 90-100%

        public MarketingKPIService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<MarketingKPIVm?> CalculateKPIAsync(string employeeId, int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Lấy thông tin employee
            var employee = await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeID == employeeId);

            if (employee == null) return null;

            // Đếm News Requests
            var newsRequests = await _db.NewsRequests
                .Where(nr => nr.RequestedBy == employeeId 
                    && nr.RequestedAt >= startDate 
                    && nr.RequestedAt <= endDate)
                .ToListAsync();

            var totalNewsRequests = newsRequests.Count;
            var approvedNewsRequests = newsRequests.Count(nr => nr.Status == RequestStatus.Approved);
            var rejectedNewsRequests = newsRequests.Count(nr => nr.Status == RequestStatus.Rejected);
            var pendingNewsRequests = newsRequests.Count(nr => nr.Status == RequestStatus.Pending);

            // Đếm Discount Requests
            var discountRequests = await _db.DiscountRequests
                .Where(dr => dr.RequestedBy == employeeId 
                    && dr.RequestedAt >= startDate 
                    && dr.RequestedAt <= endDate)
                .ToListAsync();

            var totalDiscountRequests = discountRequests.Count;
            var approvedDiscountRequests = discountRequests.Count(dr => dr.Status == RequestStatus.Approved);
            var rejectedDiscountRequests = discountRequests.Count(dr => dr.Status == RequestStatus.Rejected);
            var pendingDiscountRequests = discountRequests.Count(dr => dr.Status == RequestStatus.Pending);

            // Tính KPI Score
            // Công thức: 
            // - Tỷ lệ approve: 50% (tối đa 50 điểm)
            // - Số lượng request đã approve: 30% (tối đa 30 điểm, mỗi 10 request = 10 điểm)
            // - Tỷ lệ reject thấp: 20% (tối đa 20 điểm, reject rate < 20% = 20 điểm)
            var totalRequests = totalNewsRequests + totalDiscountRequests;
            var totalApproved = approvedNewsRequests + approvedDiscountRequests;
            var totalRejected = rejectedNewsRequests + rejectedDiscountRequests;

            decimal approveRateScore = 0;
            decimal quantityScore = 0;
            decimal rejectRateScore = 0;

            if (totalRequests > 0)
            {
                var approveRate = (decimal)totalApproved / totalRequests * 100;
                approveRateScore = Math.Min(approveRate * 0.5m, 50); // Tối đa 50 điểm

                // Số lượng request đã approve (mỗi 10 request = 10 điểm, tối đa 30 điểm)
                quantityScore = Math.Min((totalApproved / 10m) * 10m, 30);

                var rejectRate = (decimal)totalRejected / totalRequests * 100;
                // Nếu reject rate < 20% thì được 20 điểm, nếu >= 20% thì giảm dần
                rejectRateScore = rejectRate < 20 ? 20 : Math.Max(20 - (rejectRate - 20) * 0.5m, 0);
            }

            var kpiScore = approveRateScore + quantityScore + rejectRateScore;
            var isKPIAchieved = kpiScore >= TARGET_SCORE;

            // Lấy base salary từ contract hoặc salary gần nhất
            var baseSalary = await GetBaseSalaryAsync(employeeId, year, month);

            // Tính bonus
            var kpiBonus = CalculateKPIBonus(kpiScore, baseSalary);

            return new MarketingKPIVm(
                employeeId,
                employee.FullName,
                startDate,
                totalNewsRequests,
                approvedNewsRequests,
                rejectedNewsRequests,
                pendingNewsRequests,
                totalDiscountRequests,
                approvedDiscountRequests,
                rejectedDiscountRequests,
                pendingDiscountRequests,
                kpiScore,
                TARGET_SCORE,
                isKPIAchieved,
                kpiBonus,
                baseSalary
            );
        }

        public async Task<MarketingKPIVm?> GetKPIAsync(string employeeId, int year, int month)
        {
            var kpiMonth = new DateTime(year, month, 1);
            var kpi = await _db.MarketingKPIs
                .Include(m => m.Employee)
                .FirstOrDefaultAsync(m => m.EmployeeID == employeeId && m.KpiMonth == kpiMonth);

            if (kpi == null) return null;

            var baseSalary = await GetBaseSalaryAsync(employeeId, year, month);

            return new MarketingKPIVm(
                kpi.EmployeeID,
                kpi.Employee?.FullName,
                kpi.KpiMonth,
                kpi.TotalNewsRequests,
                kpi.ApprovedNewsRequests,
                kpi.RejectedNewsRequests,
                kpi.PendingNewsRequests,
                kpi.TotalDiscountRequests,
                kpi.ApprovedDiscountRequests,
                kpi.RejectedDiscountRequests,
                kpi.PendingDiscountRequests,
                kpi.KPIScore,
                kpi.TargetScore,
                kpi.IsKPIAchieved,
                kpi.KPIBonus,
                baseSalary
            );
        }

        public async Task<MarketingKPIVm?> CalculateAndSaveKPIAsync(string employeeId, int year, int month)
        {
            var kpiVm = await CalculateKPIAsync(employeeId, year, month);
            if (kpiVm == null) return null;

            var kpiMonth = new DateTime(year, month, 1);
            var existingKpi = await _db.MarketingKPIs
                .FirstOrDefaultAsync(m => m.EmployeeID == employeeId && m.KpiMonth == kpiMonth);

            if (existingKpi != null)
            {
                // Update existing
                existingKpi.TotalNewsRequests = kpiVm.TotalNewsRequests;
                existingKpi.ApprovedNewsRequests = kpiVm.ApprovedNewsRequests;
                existingKpi.RejectedNewsRequests = kpiVm.RejectedNewsRequests;
                existingKpi.PendingNewsRequests = kpiVm.PendingNewsRequests;
                existingKpi.TotalDiscountRequests = kpiVm.TotalDiscountRequests;
                existingKpi.ApprovedDiscountRequests = kpiVm.ApprovedDiscountRequests;
                existingKpi.RejectedDiscountRequests = kpiVm.RejectedDiscountRequests;
                existingKpi.PendingDiscountRequests = kpiVm.PendingDiscountRequests;
                existingKpi.NewsApproveRate = kpiVm.NewsApproveRate;
                existingKpi.DiscountApproveRate = kpiVm.DiscountApproveRate;
                existingKpi.OverallApproveRate = kpiVm.OverallApproveRate;
                existingKpi.KPIScore = kpiVm.KPIScore;
                existingKpi.IsKPIAchieved = kpiVm.IsKPIAchieved;
                existingKpi.TargetScore = kpiVm.TargetScore;
                existingKpi.KPIBonus = kpiVm.KPIBonus;
                existingKpi.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new
                var newKpi = new MarketingKPI
                {
                    EmployeeID = employeeId,
                    KpiMonth = kpiMonth,
                    TotalNewsRequests = kpiVm.TotalNewsRequests,
                    ApprovedNewsRequests = kpiVm.ApprovedNewsRequests,
                    RejectedNewsRequests = kpiVm.RejectedNewsRequests,
                    PendingNewsRequests = kpiVm.PendingNewsRequests,
                    TotalDiscountRequests = kpiVm.TotalDiscountRequests,
                    ApprovedDiscountRequests = kpiVm.ApprovedDiscountRequests,
                    RejectedDiscountRequests = kpiVm.RejectedDiscountRequests,
                    PendingDiscountRequests = kpiVm.PendingDiscountRequests,
                    NewsApproveRate = kpiVm.NewsApproveRate,
                    DiscountApproveRate = kpiVm.DiscountApproveRate,
                    OverallApproveRate = kpiVm.OverallApproveRate,
                    KPIScore = kpiVm.KPIScore,
                    IsKPIAchieved = kpiVm.IsKPIAchieved,
                    TargetScore = kpiVm.TargetScore,
                    KPIBonus = kpiVm.KPIBonus,
                    CreatedAt = DateTime.UtcNow
                };
                _db.MarketingKPIs.Add(newKpi);
            }

            await _db.SaveChangesAsync();
            return kpiVm;
        }

        public decimal CalculateKPIBonus(decimal kpiScore, decimal baseSalary)
        {
            if (kpiScore < TARGET_SCORE) return 0; // Không đạt KPI thì không có bonus

            decimal bonusRate = 0;
            if (kpiScore >= 90)
                bonusRate = BONUS_RATE_90_100;
            else if (kpiScore >= 80)
                bonusRate = BONUS_RATE_80_90;
            else if (kpiScore >= TARGET_SCORE)
                bonusRate = BONUS_RATE_70_80;

            return baseSalary * bonusRate;
        }

        private async Task<decimal> GetBaseSalaryAsync(string employeeId, int year, int month)
        {
            // Lấy base salary từ Salary table
            var salary = await _db.Salaries
                .FirstOrDefaultAsync(s => s.EmployeeID == employeeId 
                    && s.SalaryMonth.Year == year 
                    && s.SalaryMonth.Month == month);

            if (salary != null) return salary.BaseSalary;

            // Nếu không có, lấy từ Contract
            var contract = await _db.Contracts
                .Where(c => c.EmployeeId == employeeId && c.Status == "Hiệu lực")
                .OrderByDescending(c => c.StartDate)
                .FirstOrDefaultAsync();

            if (contract == null) return 0;

            // Tính base salary từ BaseRate
            // Nếu PaymentType = "Tháng" thì BaseRate chính là lương tháng
            // Nếu PaymentType = "Giờ" thì tính lương tháng = BaseRate * số giờ làm việc trong tháng (giả sử 160 giờ/tháng)
            if (contract.PaymentType == "Tháng")
            {
                return contract.BaseRate;
            }
            else
            {
                // Tính theo giờ: giả sử 160 giờ làm việc/tháng
                const decimal HOURS_PER_MONTH = 160m;
                return contract.BaseRate * HOURS_PER_MONTH;
            }
        }
    }
}

