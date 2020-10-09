using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class ReportedUsers
    {
        [Key]
        public int Id { get; set; }
        public string ReportingUserId { get; set; }
        public string BlockedUserId { get; set; }
        public DateTime AddedDate { get; set; }
    }
}
