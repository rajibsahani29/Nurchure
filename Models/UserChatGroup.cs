using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class UserChatGroup
    {
        [Key]
        public int Id { get; set; }
        public string GroupName { get; set; }
        public string GroupImage { get; set; }
        public string CreatedUserId { get; set; }
        public string GroupType { get; set; }
        public int ActivityId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public string MBITName { get; set; }
    }
}
