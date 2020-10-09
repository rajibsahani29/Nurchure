using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class UserActivities
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public int ActivityId { get; set; }
        public int Rank { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
    }
}
