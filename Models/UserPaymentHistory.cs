using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class UserPaymentHistory
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public double PaymentValue { get; set; }
        public int PaymentTypeId { get; set; }
        public int PaymentCategoryId { get; set; }
        public int EventId { get; set; }
        public string Notes { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
