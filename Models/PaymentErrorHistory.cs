using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class PaymentErrorHistory
    {
        [Key]
        public int Id { get; set; }
        public int UserTransactionId { get; set; }
        public int PaymentTypeId { get; set; }
        public decimal Amount { get; set; }
        public string Notes { get; set; }
        public string ResponseError { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
