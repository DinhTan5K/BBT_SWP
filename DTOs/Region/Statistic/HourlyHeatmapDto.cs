 public class HourlyHeatmapDto
    {
       // Danh sách ngày (chuỗi yyyy-MM-dd) theo thứ tự
    public List<string> Days { get; set; } = new();

    // Ma trận units: MatrixUnits[dayIndex][hour] => units sold
    public List<List<int>> MatrixUnits { get; set; } = new();

    // Ma trận revenue: MatrixRevenue[dayIndex][hour] => revenue VND
    public List<List<decimal>> MatrixRevenue { get; set; } = new();
    }