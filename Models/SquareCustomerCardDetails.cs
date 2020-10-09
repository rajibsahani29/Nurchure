using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class SquareCustomerCardDetails
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public string SquareCustomerId { get; set; }
        public string SquareCustomerCardId { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime AddedDate { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
    }
}