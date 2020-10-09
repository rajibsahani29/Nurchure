using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class GroupPoll
    {
        [Key]
        public int Id { get; set; }
        public int EventId { get; set; }
        //public string RestaurantName { get; set; }
        public DateTime EventDate1 { get; set; }
        public TimeSpan EventTime1 { get; set; }
        public DateTime EventDate2 { get; set; }
        public TimeSpan EventTime2 { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        //public decimal? TiltCount { get; set; }
        public string PollStatus { get; set; }
        public string OwnerUserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string PreferredMerchantId { get; set; }
        public string BackupMerchantId { get; set; }
        public int PreferredTilt { get; set; }
        public int BackupTilt { get; set; }
    }
}