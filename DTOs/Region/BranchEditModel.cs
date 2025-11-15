using System;
using System.ComponentModel.DataAnnotations;



    public class BranchEditModel
    {
        public int BranchID { get; set; }

        [Required, StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        // Latitude / Longitude (nullable: branch may not have coords)
        [Range(-90.0, 90.0, ErrorMessage = "Latitude phải nằm trong khoảng -90 đến 90")]
        public decimal? Latitude { get; set; }
        
        [Range(-180.0, 180.0, ErrorMessage = "Longitude phải nằm trong khoảng -180 đến 180")]
        public decimal? Longitude { get; set; }


    }

