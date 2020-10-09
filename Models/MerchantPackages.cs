using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class MerchantPackages
    {
        [Key]
        public int? PackageId { get; set; }
        public int CampaignID { get; set; }
        public int PackageTypeID { get; set; }
        public string Description { get; set; }
        public decimal? Price { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int MinPerson { get; set; }
        public decimal? DiscountPercentage { get; set; }
    }
}
