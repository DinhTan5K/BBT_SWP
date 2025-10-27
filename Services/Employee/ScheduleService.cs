using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using start.Data;
using start.Models;
using start.Models.ViewModels;

public class ScheduleService : IScheduleService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuthService _auth;
    private readonly IWebHostEnvironment _env;

    public ScheduleService(ApplicationDbContext db, IAuthService auth, IWebHostEnvironment env)

    {
        _db = db; _auth = auth; _env = env;
    }

    public MonthScheduleDto GetMonthSchedule(string employeeId, int month, int year)
    {
        // validate tồn tại nhân viên
        var emp = _db.Employees
                     .Include(e => e.Branch)    // để view header dùng Branch
                     .AsNoTracking()
                     .SingleOrDefault(e => e.EmployeeID == employeeId);
        if (emp == null)
            throw new InvalidOperationException("Nhân viên không tồn tại.");

        var first = new DateTime(year, month, 1);
        var lastEx = first.AddMonths(1);

        var items = _db.WorkSchedules
                       .AsNoTracking()
                       .Where(w => w.EmployeeID == employeeId &&
                                   w.WorkDate >= first && w.WorkDate < lastEx)
                       .OrderBy(w => w.WorkDate)
                       .ToList();

        return new MonthScheduleDto
        {
            Employee = emp,
            Items = items,
            Month = month,
            Year = year
        };
    }

}