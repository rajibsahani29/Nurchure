using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class UserProfileSetting
    {
        [Key]
        public string UserId { get; set; }
        public int SubscriptionTypeID { get; set; }
        public bool PushNotificationEnabled { get; set; }
        public bool EmailNotificationEnabled { get; set; }
        public int ProfileVisibilityId { get; set; }
    }
}
