using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class UserChatGroupMessages
    {
        [Key]
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string UserId { get; set; }
        public string Message { get; set; }
        public string MessageType { get; set; }
        public DateTime? AddedDate { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}