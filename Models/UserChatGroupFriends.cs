using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class UserChatGroupFriends
    {
        [Key]
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string AddedUserId { get; set; }
        public string UserId { get; set; }
        public DateTime AddedDate { get; set; }
        public bool AdminRights { get; set; }
        public DateTime AdminRightsAddedDate { get; set; }
        public DateTime LastUpdatedAdminRights { get; set; }
    }
}
