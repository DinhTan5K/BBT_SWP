using System;
using System.ComponentModel.DataAnnotations;
namespace start.Models;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Customer")]
public class Customer
{
    public int CustomerID { get; set; }

    [Required]
    [StringLength(100)]
    public string? Name { get; set; }


    [StringLength(200)]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Required]
    [StringLength(15)]
    public string? Username { get; set; }

    [Phone]
    public string? Phone { get; set; }

    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [DataType(DataType.Date)]
    public DateTime? BirthDate { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsEmailConfirmed { get; set; } = false;
    public string? OtpCode { get; set; }

    [StringLength(255)]
    public string? ProfileImagePath { get; set; }
    
        public DateTime? OtpExpired { get; set; }
    [NotMapped]
    public string Role { get; set; } = "CU";

}
