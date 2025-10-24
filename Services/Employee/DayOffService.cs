using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using start.Data;
using start.Models;
using start.Models.ViewModels;


public class DayOffService : IDayOffService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _cfg;

    public DayOffService(ApplicationDbContext db, IConfiguration cfg)
    { _db = db; _cfg = cfg; }

    public async Task<int> CreateOneDayAsync(DayOffOneDayVm vm)
    {
        // Rule: phải sau hôm nay >= 3 ngày
        if (vm.OffDate.Date < DateTime.Today.AddDays(3))
            throw new InvalidOperationException("Ngày nghỉ phải sau hôm nay ít nhất 3 ngày.");

        // Rule: không trùng ngày (đã có UQ ở DB, nhưng check mềm trước cho đẹp)
        bool dupe = await _db.DayOffRequests
            .AnyAsync(x => x.EmployeeID == vm.EmployeeID && x.OffDate == vm.OffDate.Date);
        if (dupe)
            throw new InvalidOperationException("Bạn đã có đơn nghỉ cho ngày này.");

        var entity = new DayOffRequest
        {
            EmployeeID = vm.EmployeeID,
            BranchID   = vm.BranchID,
            OffDate    = vm.OffDate.Date,
            Reason     = vm.Reason,
            Status     = "Pending"
        };
        _db.DayOffRequests.Add(entity);
        await _db.SaveChangesAsync();

        // Gửi mail tới quản lý cùng chi nhánh (ví dụ RoleID = "QL")
        var managers = await _db.Employees
            .Where(e => e.BranchID == vm.BranchID && e.RoleID == "BM" && e.Email != null && e.Email != "")
            .Select(e => e.Email!)
            .ToListAsync();

        if (managers.Count > 0)
        {
            var emp = await _db.Employees.FindAsync(vm.EmployeeID);
            var html = BuildManagerEmail(emp?.FullName ?? vm.EmployeeID, vm.OffDate, vm.OffDate, vm.Reason);
            await SendEmailAsync(managers.ToArray(),
                $"[Leave] Yêu cầu nghỉ phép từ {emp?.FullName ?? vm.EmployeeID}",
                html);
        }

        return entity.Id;
    }

    public async Task<List<DayOffListItemVm>> GetMyAsync(string employeeId)
        => await _db.DayOffRequests
            .Where(x => x.EmployeeID == employeeId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new DayOffListItemVm {
                Id = x.Id, OffDate = x.OffDate, Status = x.Status, Reason = x.Reason
            }).ToListAsync();

    // email template theo đúng yêu cầu của bạn
    private string BuildManagerEmail(string employeeName, DateTime startDate, DateTime endDate, string reason) => $@"
<h2>Yêu cầu nghỉ phép mới</h2>
<p>Chào quản lý,</p>
<p>Bạn đã nhận được một yêu cầu xin nghỉ phép từ nhân viên <strong>{employeeName}</strong> với các thông tin chi tiết như sau:</p>
<ul>
    <li><strong>Nhân viên:</strong> {employeeName}</li>
    <li><strong>Ngày nghỉ:</strong> {startDate:dd/MM/yyyy}</li>
    <li><strong>Lý do:</strong> {WebUtility.HtmlEncode(reason)}</li>
</ul>
<p>Vui lòng đăng nhập vào hệ thống để duyệt hoặc từ chối yêu cầu này.</p>
<p>Trân trọng,<br>Hệ thống Quản lý Nhân sự</p>";

    private async Task SendEmailAsync(string[] to, string subject, string html)
    {
        var host = _cfg["Email:Smtp:Host"]!;
        var port = int.Parse(_cfg["Email:Smtp:Port"] ?? "587");
        var user = _cfg["Email:Smtp:User"]!;
        var pass = _cfg["Email:Smtp:Pass"]!;
        var from = _cfg["Email:From"]!;
        var display = _cfg["Email:DisplayName"] ?? "HR";
        var ssl = bool.Parse(_cfg["Email:Smtp:UseStartTls"] ?? "true");

        using var client = new SmtpClient(host, port) { EnableSsl = ssl, Credentials = new NetworkCredential(user, pass) };
        using var msg = new MailMessage { From = new MailAddress(from, display), Subject = subject, Body = html, IsBodyHtml = true };
        foreach (var t in to) msg.To.Add(t);
        await client.SendMailAsync(msg);
    }
}