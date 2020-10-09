using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class UserCardInfo
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public string NameOnCard { get; set; }
        public string CardNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string CardType { get; set; }
        public string Issuer { get; set; }
        public string SecurityNumber { get; set; }
        public DateTime AddedDate { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
    }
}