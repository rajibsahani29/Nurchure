using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class MuteChatNotificationDetails
    {
        [Key]
        public int Id { get; set; }
        public int ChatId { get; set; }
        public string UserId { get; set; }
        public DateTime AddedTime { get; set; }
        public DateTime ExpiryTime { get; set; }
    }
}
