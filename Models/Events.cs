using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class Events
    {
        [Key]
        public int Id { get; set; }
        public int GroupId { get; set; }
        //public string MerchantId { get; set; }
        public int PackageId { get; set; }
        public string LatestPollingId { get; set; }
        public DateTime EventDate { get; set; }
        public string EventCoordinator { get; set; }
        public int BookingStageId { get; set; }
        //public int NumberOfAttendee { get; set; }
        //public int ConfirmedAttendee { get; set; }
        public int PaymentStageId { get; set; }
        public string AttendeeWithPaymentComplete { get; set; }
        public string ActualAttendee { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string EventName { get; set; }
    }
}