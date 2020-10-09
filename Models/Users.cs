using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class Users
    {
        [Key]
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string InvoiceDeliveryEmail { get; set; }
        public string PhoneNumber { get; set; }
        public int Age { get; set; }
        public int GenderId { get; set; }
        public string LoginIdentityId { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public int CityId { get; set; }
        public int StateProvinceId { get; set; }
        public int CountryId { get; set; }
        public string ZipPostalCode { get; set; }
        public double GeoLocationLongitude { get; set; }
        public double GeoLocationLatitude { get; set; }
        public DateTime LastUpdated { get; set; }
        public int UserPersonalityTypeID { get; set; }
        public string UserFavourite { get; set; }
        public string Desciption { get; set; }
        public string TagLine { get; set; }
        public int MaxDistance { get; set; }
        public int MaxAge { get; set; }
        public int MinAge { get; set; }
        public bool UserCurrentStatus { get; set; }
        public DateTime LastUpdatedUserCurrentStatus { get; set; }
        public string Activity { get; set; }
        public string DOB { get; set; }
    }
}