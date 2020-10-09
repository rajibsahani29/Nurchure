using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class MerchantVerificationDetails
    {
        [Key]
        public int Id { get; set; }
        public string MerchantId { get; set; }
        public string VerificationCode { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime ExpiryTime { get; set; }
        public string RequestValue { get; set; }
    }
}
