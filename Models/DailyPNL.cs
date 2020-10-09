using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class DailyPNL
    {
        [Key]
        public int Id { get; set; }
        public string MerchantId { get; set; }
        public decimal DailyPNLAmount { get; set; }
        public DateTime PNLDate { get; set; }
        public DateTime AddedDate { get; set; }
    }
}
