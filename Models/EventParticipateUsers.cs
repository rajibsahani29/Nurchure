using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class EventParticipateUsers
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public int EventId { get; set; }
        //public bool IsAttended { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public bool IsPreferredAttended { get; set; }
        public bool IsBackupAttended { get; set; }
    }
}
