using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class Merchants
    {
        [Key]
        public string MerchantId { get; set; }
        public string UserName { get; set; }
        public string PasswordHash { get; set; }
        public Int16 MerchantType { get; set; }
        public string MerchantName { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string AddressLine3 { get; set; }
        public int CityID { get; set; }
        public string ZipPostalCode { get; set; }
        public int StateProvinceID { get; set; }
        public int CountryID { get; set; }
        public string PrimaryContactName { get; set; }
        public string PrimaryContactEmail { get; set; }
        public string PrimaryContactPhone { get; set; }
        public string PrimaryContactFax { get; set; }
        public string SecondaryContactName { get; set; }
        public string SecondaryContactEmail { get; set; }
        public string SecondaryContactPhone { get; set; }
        public string SecondaryContactFax { get; set; }
        public string BillingContactName { get; set; }
        public string BillingContactEmail { get; set; }
        public string BillingContactPhone { get; set; }
        public string BillingAddressLine1 { get; set; }
        public string BillingContactFax { get; set; }
        public string BillingAddressLine2 { get; set; }
        public string BillingAddressLine3 { get; set; }
        public string BillingZipPostalCode { get; set; }
        public int BillingCityID { get; set; }
        public int BillingStateProvinceID { get; set; }
        public int BillingCountryID { get; set; }
        public byte LoyalyLevel { get; set; }
        public double GeolocationLattitude { get; set; }
        public double GeolocationLongitude { get; set; }
        public string Currency { get; set; }
        public byte InvoiceDeliveryID { get; set; }
        public string InvoiceDeliveryEmail { get; set; }
        public bool Active { get; set; }
        public DateTime SystemAddDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }
        public string Notes { get; set; }
        public decimal RatingAverage { get; set; }
    }
}
