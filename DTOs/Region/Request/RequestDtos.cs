using System;

namespace start.DTOs
{
    public enum RequestCategory
    {
        Branch = 0,
        EmployeeBranch = 1,
        Product = 2,
        Category = 3
    }

    public class SentRequestListItem
    {
        public int Id { get; set; }
        public RequestCategory Category { get; set; }
        public string CategoryLabel => Category.ToString();
        public int RequestType { get; set; } // giữ nguyên giá trị numeric từ DB (0=Add,1=Edit,2=Delete)
        public string RequestTypeLabel { get; set; } = string.Empty;
        public string ContentSummary { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public int Status { get; set; } // 0 pending,1 approved,2 rejected
        public string StatusLabel { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
    }

    public class SentRequestDetail
    {
        public int Id { get; set; }
        public RequestCategory Category { get; set; }
        public int RequestType { get; set; }
        public string RequestedBy { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public int Status { get; set; }
        public string ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string RejectionReason { get; set; }

        // payload fields (nullable) — will be filled depending on category
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? BranchPhone { get; set; }

        public string? CategoryName { get; set; }
        public int? CategoryId { get; set; }

        public string? ProductName { get; set; }
        public int? ProductId { get; set; }

        // Employee fields
        public string? EmployeeId { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public int? RegionId { get; set; }

        // raw JSON fallback (if you want to inspect the whole request)
        public string? RawData { get; set; }
    }
}
