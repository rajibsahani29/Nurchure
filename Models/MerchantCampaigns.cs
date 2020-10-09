using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class MerchantCampaigns
    {
        [Key]
        public int? Id { get; set; }
        public string MerchantId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Disclosure { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int StateProvinceID { get; set; }
        public int CityID { get; set; }
        public int CountryID { get; set; }
        public bool Active { get; set; }
    }
}
