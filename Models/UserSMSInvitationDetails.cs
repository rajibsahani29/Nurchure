using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class UserSMSInvitationDetails
    {
        [Key]
        public int Id { get; set; }
        public string SenderUserId { get; set; }
        public string ReceiverPhoneNumber { get; set; }
        public int UserChatGroupId { get; set; }
        public string InvitationCode { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime ExpiryTime { get; set; }
    }
}
