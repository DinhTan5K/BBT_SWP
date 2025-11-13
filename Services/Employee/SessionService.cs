public class SessionService
{
    private readonly IHttpContextAccessor _http;

    public SessionService(IHttpContextAccessor http)
    {
        _http = http;
    }

    public int? GetBranchId()
    {
        var s = _http.HttpContext!.Session.GetString("BranchId");
        return string.IsNullOrEmpty(s) ? null : int.Parse(s);
    }

    public string? GetEmployeeId()
        => _http.HttpContext!.Session.GetString("EmployeeID");

    public string GetLeaderName()
        => _http.HttpContext!.Session.GetString("EmployeeName") ?? "Không xác định";
    public string GetEmployeeName()
    {
        return _http.HttpContext!.Session.GetString("EmployeeName") ?? "Không xác định";
    }

}
