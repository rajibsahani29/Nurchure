using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NurchureAPI.Models;

namespace NurchureAPI.Controllers
{
    //[System.Web.Http.Route("api/merchants")]
    [Route("api/[controller]")]
    [ApiController]
    public class merchantsController : Controller
    {
        private AppDbContext _appDbContext;
        private IConfiguration configuration;

        public merchantsController(AppDbContext context, IConfiguration iConfig)
        {
            _appDbContext = context;
            configuration = iConfig;
        }

        // POST api/merchants/RegisterMerchant
        [HttpPost]
        [Route("RegisterMerchant")]
        public async Task<ActionResult> RegisterMerchant([FromBody] JObject objJson)
        {
            try
            {
                int intRecCount = _appDbContext.Merchants.Where(t => t.UserName == Convert.ToString(objJson.SelectToken("username"))).Count();
                if (intRecCount > 0)
                {
                    return BadRequest(new { Status = "Error", Error = "Merchant already exists" });
                }

                //Regex regex = new Regex(@"(?=^.{8,}$)(?=.*\d)(?=.*[a-z])(?=.*[!@#$%^&*()_+}{:;'?>.<,])(?!.*\s).*$");
                Regex regex = new Regex(@"(?=^.{8,15}$)(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[!@#$%^&*()_+}{:;'?>.<,])(?!.*\s).*$");
                Match match = regex.Match(Convert.ToString(objJson.SelectToken("password")));

                if (!match.Success)
                {
                    return BadRequest(new { Status = "Error", Error = "Password must contain 8-15 alpha numeric characters, including one UPPER case, one LOWER case and one special character" });
                }

                match = regex.Match(Convert.ToString(objJson.SelectToken("confirmpassword")));
                if (!match.Success)
                {
                    return BadRequest(new { Status = "Error", Error = "Confirm Password must contain 8-15 alpha numeric characters, including one UPPER case, one LOWER case and one special character" });
                }

                if (Convert.ToString(objJson.SelectToken("password")) != Convert.ToString(objJson.SelectToken("confirmpassword")))
                {
                    return BadRequest(new { Status = "Error", Error = "Please make sure that Password and Confirm Password matches" });
                }

                //Create MerchantId
                string strMerchantId = "";
                var objMerchantList = _appDbContext.Merchants.Select(t => new { MerchantIdVal = t.MerchantId, SystemAddDateVal = t.SystemAddDate }).OrderByDescending(p => p.SystemAddDateVal).FirstOrDefault();
                if (objMerchantList != null)
                {
                    string[] spearator = { "NIM" };
                    string[] strlist = objMerchantList.MerchantIdVal.Split(spearator, StringSplitOptions.None);
                    if (strlist.Length > 1)
                    {
                        strMerchantId = "NIM" + Convert.ToString(Convert.ToInt32(strlist[1]) + 1).PadLeft(9, '0');
                    }
                    else
                    {
                        strMerchantId = "NIM000000001";
                    }
                }
                else
                {
                    strMerchantId = "NIM000000001";
                }

                Merchants objMerchants = new Merchants();
                objMerchants.MerchantId = strMerchantId;
                objMerchants.UserName = Convert.ToString(objJson.SelectToken("username"));
                objMerchants.PasswordHash = clsCommon.GetHashedValue(Convert.ToString(objJson.SelectToken("password")));
                objMerchants.MerchantType = Convert.ToInt16(objJson.SelectToken("merchanttype"));
                objMerchants.MerchantName = Convert.ToString(objJson.SelectToken("merchantname"));
                objMerchants.AddressLine1 = Convert.ToString(objJson.SelectToken("addressline1"));
                objMerchants.AddressLine2 = Convert.ToString(objJson.SelectToken("addressline2"));
                objMerchants.AddressLine3 = Convert.ToString(objJson.SelectToken("addressline3"));
                objMerchants.CityID = Convert.ToInt32(objJson.SelectToken("cityid"));
                objMerchants.ZipPostalCode = Convert.ToString(objJson.SelectToken("zippostalcode"));
                objMerchants.StateProvinceID = Convert.ToInt32(objJson.SelectToken("stateprovinceid"));
                objMerchants.CountryID = Convert.ToInt32(objJson.SelectToken("countryid"));
                objMerchants.PrimaryContactName = Convert.ToString(objJson.SelectToken("primarycontactname"));
                objMerchants.PrimaryContactEmail = Convert.ToString(objJson.SelectToken("primarycontactemail"));
                objMerchants.PrimaryContactPhone = Convert.ToString(objJson.SelectToken("primarycontactphone"));
                objMerchants.PrimaryContactFax = Convert.ToString(objJson.SelectToken("primarycontactfax"));
                objMerchants.SecondaryContactName = Convert.ToString(objJson.SelectToken("secondarycontactname"));
                objMerchants.SecondaryContactEmail = Convert.ToString(objJson.SelectToken("secondarycontactemail"));
                objMerchants.SecondaryContactPhone = Convert.ToString(objJson.SelectToken("secondarycontactphone"));
                objMerchants.SecondaryContactFax = Convert.ToString(objJson.SelectToken("secondarycontactfax"));
                objMerchants.BillingContactName = Convert.ToString(objJson.SelectToken("billingcontactname"));
                objMerchants.BillingContactEmail = Convert.ToString(objJson.SelectToken("billingcontactemail"));
                objMerchants.BillingContactPhone = Convert.ToString(objJson.SelectToken("billingcontactphone"));
                objMerchants.BillingAddressLine1 = Convert.ToString(objJson.SelectToken("billingaddressline1"));
                objMerchants.BillingContactFax = Convert.ToString(objJson.SelectToken("billingcontactfax"));
                objMerchants.BillingAddressLine2 = Convert.ToString(objJson.SelectToken("billingaddressline2"));
                objMerchants.BillingAddressLine3 = Convert.ToString(objJson.SelectToken("billingaddressline3"));
                objMerchants.BillingZipPostalCode = Convert.ToString(objJson.SelectToken("billingzippostalcode"));
                objMerchants.BillingCityID = Convert.ToInt32(objJson.SelectToken("billingcityid"));
                objMerchants.BillingStateProvinceID = Convert.ToInt32(objJson.SelectToken("billingstateprovinceid"));
                objMerchants.BillingCountryID = Convert.ToInt32(objJson.SelectToken("billingcountryid"));
                objMerchants.LoyalyLevel = Convert.ToByte(objJson.SelectToken("loyalylevel"));
                objMerchants.GeolocationLattitude = Convert.ToDouble(objJson.SelectToken("geolocationlattitude"));
                objMerchants.GeolocationLongitude = Convert.ToDouble(objJson.SelectToken("geolocationlongitude"));
                objMerchants.Currency = Convert.ToString(objJson.SelectToken("currency"));
                objMerchants.InvoiceDeliveryID = Convert.ToByte(objJson.SelectToken("invoicedeliveryid"));
                objMerchants.InvoiceDeliveryEmail = Convert.ToString(objJson.SelectToken("invoicedeliveryemail"));
                objMerchants.Active = true;
                objMerchants.SystemAddDate = DateTime.UtcNow;
                objMerchants.Notes = Convert.ToString(objJson.SelectToken("notes"));
                objMerchants.RatingAverage = Convert.ToDecimal(objJson.SelectToken("ratingaverage"));

                await _appDbContext.Merchants.AddAsync(objMerchants);
                int returnVal = await _appDbContext.SaveChangesAsync();
                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Description = "Merchant registered successfully." });
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/merchants/LoginMerchant
        [HttpPost]
        [Route("LoginMerchant")]
        public async Task<ActionResult> LoginMerchant([FromBody] JObject objJson)
        {
            try
            {
                //Create PasswordHash
                string hashedPassword = clsCommon.GetHashedValue(Convert.ToString(objJson.SelectToken("password")));

                var objMerchantList = _appDbContext.Merchants
                                .Select(s => new { s.MerchantId, s.UserName, s.PasswordHash, s.Active })
                                .Where(s => s.UserName == Convert.ToString(objJson.SelectToken("username")) && s.PasswordHash == hashedPassword)
                                .OrderByDescending(t => t.MerchantId).FirstOrDefault();

                if (objMerchantList == null)
                {
                    return BadRequest(new { Status = "Error", Error = "Please provide valid username or password" });
                }

                if (objMerchantList.Active == false)
                {
                    return StatusCode(StatusCodes.Status405MethodNotAllowed, (new { Status = "Error", Error = "Your account has been de-active." }));
                }

                Merchants objMerchants = new Merchants();
                objMerchants.MerchantId = objMerchantList.MerchantId;
                objMerchants.ModifiedDate = DateTime.UtcNow;
                _appDbContext.Merchants.Attach(objMerchants);
                _appDbContext.Entry(objMerchants).Property("ModifiedDate").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();
                if (returnVal > 0)
                {
                    MerchantLoginSessionKeys objSessionKeysList = _appDbContext.MerchantLoginSessionKeys
                                .Where(s => s.MerchantId == Convert.ToString(objMerchantList.MerchantId))
                                .FirstOrDefault();

                    if (objSessionKeysList != null)
                    {
                        _appDbContext.MerchantLoginSessionKeys.Remove(objSessionKeysList);
                        await _appDbContext.SaveChangesAsync();
                    }

                    MerchantLoginSessionKeys objMerchantLoginSessionKeys = new MerchantLoginSessionKeys();
                    objMerchantLoginSessionKeys.MerchantId = objMerchantList.MerchantId;
                    objMerchantLoginSessionKeys.ApiKeys = Guid.NewGuid();
                    objMerchantLoginSessionKeys.CreatedDate = DateTime.UtcNow;
                    await _appDbContext.MerchantLoginSessionKeys.AddAsync(objMerchantLoginSessionKeys);
                    returnVal = await _appDbContext.SaveChangesAsync();

                    if (returnVal > 0)
                    {
                        return Ok(new { Status = "OK", api_key = Convert.ToString(objMerchantLoginSessionKeys.ApiKeys), Description = Convert.ToString(objMerchantList.UserName) + " has successfully logged in" });
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                    }
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/merchants/UpdateMerchantDetail
        [HttpPost]
        [Route("UpdateMerchantDetail")]
        public async Task<ActionResult> UpdateMerchantDetail([FromBody] JObject objJson)
        {
            try
            {
                if (string.IsNullOrEmpty(Convert.ToString(Request.Headers["api_key"])))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strMerchantId = MerchantLoginSessionKeys.GetMerchantIdByApiKey(_appDbContext, Guid.Parse(Request.Headers["api_key"]));
                if (string.IsNullOrEmpty(strMerchantId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                Merchants objMerchants = new Merchants();
                objMerchants.MerchantId = strMerchantId;
                objMerchants.MerchantType = Convert.ToInt16(objJson.SelectToken("merchanttype"));
                objMerchants.MerchantName = Convert.ToString(objJson.SelectToken("merchantname"));
                objMerchants.AddressLine1 = Convert.ToString(objJson.SelectToken("addressline1"));
                objMerchants.AddressLine2 = Convert.ToString(objJson.SelectToken("addressline2"));
                objMerchants.AddressLine3 = Convert.ToString(objJson.SelectToken("addressline3"));
                objMerchants.CityID = Convert.ToInt32(objJson.SelectToken("cityid"));
                objMerchants.ZipPostalCode = Convert.ToString(objJson.SelectToken("zippostalcode"));
                objMerchants.StateProvinceID = Convert.ToInt32(objJson.SelectToken("stateprovinceid"));
                objMerchants.CountryID = Convert.ToInt32(objJson.SelectToken("countryid"));
                objMerchants.PrimaryContactName = Convert.ToString(objJson.SelectToken("primarycontactname"));
                objMerchants.PrimaryContactEmail = Convert.ToString(objJson.SelectToken("primarycontactemail"));
                objMerchants.PrimaryContactPhone = Convert.ToString(objJson.SelectToken("primarycontactphone"));
                objMerchants.PrimaryContactFax = Convert.ToString(objJson.SelectToken("primarycontactfax"));
                objMerchants.SecondaryContactName = Convert.ToString(objJson.SelectToken("secondarycontactname"));
                objMerchants.SecondaryContactEmail = Convert.ToString(objJson.SelectToken("secondarycontactemail"));
                objMerchants.SecondaryContactPhone = Convert.ToString(objJson.SelectToken("secondarycontactphone"));
                objMerchants.SecondaryContactFax = Convert.ToString(objJson.SelectToken("secondarycontactfax"));
                objMerchants.BillingContactName = Convert.ToString(objJson.SelectToken("billingcontactname"));
                objMerchants.BillingContactEmail = Convert.ToString(objJson.SelectToken("billingcontactemail"));
                objMerchants.BillingContactPhone = Convert.ToString(objJson.SelectToken("billingcontactphone"));
                objMerchants.BillingAddressLine1 = Convert.ToString(objJson.SelectToken("billingaddressline1"));
                objMerchants.BillingContactFax = Convert.ToString(objJson.SelectToken("billingcontactfax"));
                objMerchants.BillingAddressLine2 = Convert.ToString(objJson.SelectToken("billingaddressline2"));
                objMerchants.BillingAddressLine3 = Convert.ToString(objJson.SelectToken("billingaddressline3"));
                objMerchants.BillingZipPostalCode = Convert.ToString(objJson.SelectToken("billingzippostalcode"));
                objMerchants.BillingCityID = Convert.ToInt32(objJson.SelectToken("billingcityid"));
                objMerchants.BillingStateProvinceID = Convert.ToInt32(objJson.SelectToken("billingstateprovinceid"));
                objMerchants.BillingCountryID = Convert.ToInt32(objJson.SelectToken("billingcountryid"));
                objMerchants.LoyalyLevel = Convert.ToByte(objJson.SelectToken("loyalylevel"));
                objMerchants.GeolocationLattitude = Convert.ToDouble(objJson.SelectToken("geolocationlattitude"));
                objMerchants.GeolocationLongitude = Convert.ToDouble(objJson.SelectToken("geolocationlongitude"));
                objMerchants.Currency = Convert.ToString(objJson.SelectToken("currency"));
                objMerchants.InvoiceDeliveryID = Convert.ToByte(objJson.SelectToken("invoicedeliveryid"));
                objMerchants.InvoiceDeliveryEmail = Convert.ToString(objJson.SelectToken("invoicedeliveryemail"));
                objMerchants.ModifiedDate = DateTime.UtcNow;
                objMerchants.ModifiedBy = Convert.ToInt32(objJson.SelectToken("modifiedby"));
                objMerchants.Notes = Convert.ToString(objJson.SelectToken("notes"));
                objMerchants.RatingAverage = Convert.ToDecimal(objJson.SelectToken("ratingaverage"));

                _appDbContext.Merchants.Attach(objMerchants);
                _appDbContext.Entry(objMerchants).Property("MerchantType").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("MerchantName").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("AddressLine1").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("AddressLine2").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("AddressLine3").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("CityID").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("ZipPostalCode").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("StateProvinceID").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("CountryID").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("PrimaryContactName").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("PrimaryContactEmail").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("PrimaryContactPhone").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("PrimaryContactFax").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("SecondaryContactName").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("SecondaryContactEmail").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("SecondaryContactPhone").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("SecondaryContactFax").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("BillingContactName").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("BillingContactEmail").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("BillingContactPhone").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("BillingAddressLine1").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("BillingContactFax").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("BillingAddressLine2").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("BillingAddressLine3").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("BillingZipPostalCode").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("BillingCityID").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("BillingStateProvinceID").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("BillingCountryID").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("LoyalyLevel").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("GeolocationLattitude").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("GeolocationLongitude").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("Currency").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("InvoiceDeliveryID").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("InvoiceDeliveryEmail").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("ModifiedDate").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("ModifiedBy").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("Notes").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("RatingAverage").IsModified = true;

                int returnVal = await _appDbContext.SaveChangesAsync();
                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Description = "Merchant details updated successfully." });
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/merchants/DisableMerchantAccount
        [HttpPost]
        [Route("DisableMerchantAccount")]
        public async Task<ActionResult> DisableMerchantAccount([FromBody] JObject objJson)
        {
            try
            {
                if (string.IsNullOrEmpty(Convert.ToString(Request.Headers["api_key"])))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strMerchantId = MerchantLoginSessionKeys.GetMerchantIdByApiKey(_appDbContext, Guid.Parse(Request.Headers["api_key"]));
                if (string.IsNullOrEmpty(strMerchantId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                Merchants objMerchants = new Merchants();
                objMerchants.MerchantId = strMerchantId;
                objMerchants.Active = Convert.ToBoolean(Convert.ToInt32(objJson.SelectToken("activestatus")));
                objMerchants.ModifiedDate = DateTime.UtcNow;

                _appDbContext.Merchants.Attach(objMerchants);
                _appDbContext.Entry(objMerchants).Property("Active").IsModified = true;
                _appDbContext.Entry(objMerchants).Property("ModifiedDate").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    _appDbContext.MerchantCampaigns.Where(x => x.MerchantId == strMerchantId).ToList().ForEach(x =>
                    {
                        x.Active = Convert.ToBoolean(Convert.ToInt32(objJson.SelectToken("activestatus")));
                    });
                    //_appDbContext.SaveChanges();
                    await _appDbContext.SaveChangesAsync();

                    return Ok(new { Status = "OK", User = "Merchant avtive status has been updated." });
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/merchants/ForgotMerchantPassword
        [HttpPost]
        [Route("ForgotMerchantPassword")]
        public async Task<ActionResult> ForgotMerchantPassword([FromBody] JObject objJson)
        {
            try
            {
                //int intRecCount = _appDbContext.UserLoginAccount.Where(t => t.Email == Convert.ToString(objJson.SelectToken("username"))).Count();
                var objMerchantList = _appDbContext.Merchants.Select(t => new { MerchantIdVal = t.MerchantId, UserNameVal = t.UserName }).Where(s => s.UserNameVal == Convert.ToString(objJson.SelectToken("username"))).FirstOrDefault();
                if (objMerchantList == null)
                {
                    return BadRequest(new { Status = "Error", Error = "Merchant doesn't exists" });
                }

                string strVerificationCode = clsCommon.GetRandomAlphaNumeric();

                MerchantVerificationDetails objMerchantVerificationDetails = new MerchantVerificationDetails();
                objMerchantVerificationDetails.MerchantId = objMerchantList.MerchantIdVal;
                objMerchantVerificationDetails.VerificationCode = strVerificationCode;
                objMerchantVerificationDetails.CreationTime = DateTime.UtcNow;
                objMerchantVerificationDetails.ExpiryTime = DateTime.UtcNow.AddMinutes(30);
                objMerchantVerificationDetails.RequestValue = "forgotpassword";

                string strMailStatus = SendVerificationEmail_ForgotMerchantPassword(Convert.ToString(objJson.SelectToken("username")), strVerificationCode);
                if (strMailStatus == "Success")
                {
                    await _appDbContext.MerchantVerificationDetails.AddAsync(objMerchantVerificationDetails);
                    int returnVal = await _appDbContext.SaveChangesAsync();

                    if (returnVal > 0)
                    {
                        return Ok(new { Status = "OK", Description = "A verification code for password change has been sent to " + Convert.ToString(objJson.SelectToken("username")) });
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                    }
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = strMailStatus }));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/merchants/VerifyVerificationCode
        [HttpPost]
        [Route("VerifyVerificationCode")]
        public ActionResult VerifyVerificationCode([FromBody] JObject objJson)
        {
            try
            {
                var objMerchantList = _appDbContext.Merchants
                                .Select(s => new { s.MerchantId, s.UserName })
                                .Where(s => s.UserName == Convert.ToString(objJson.SelectToken("username")))
                                .FirstOrDefault();

                if (objMerchantList == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid verification code or username" }));
                }

                var objMerchantVerificationDetails = _appDbContext.MerchantVerificationDetails
                                .Where(s => s.MerchantId == Convert.ToString(objMerchantList.MerchantId) && s.RequestValue == "forgotpassword")
                                .OrderByDescending(t => t.Id).FirstOrDefault();

                if (objMerchantVerificationDetails == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid verification code or username" }));
                }

                if (objMerchantVerificationDetails.VerificationCode != Convert.ToString(objJson.SelectToken("verificationcode")))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid verification code or username" }));
                }

                if (DateTime.UtcNow > Convert.ToDateTime(objMerchantVerificationDetails.ExpiryTime))
                {
                    return StatusCode(StatusCodes.Status402PaymentRequired, (new { Status = "Error", Error = "Verification code has expired" }));
                }

                return Ok(new { Status = "OK", Description = "Verification code is valid" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/merchants/ChangeMerchantPassword
        [HttpPost]
        [Route("ChangeMerchantPassword")]
        public async Task<ActionResult> ChangeMerchantPassword([FromBody] JObject objJson)
        {
            try
            {
                var objMerchantList = _appDbContext.Merchants
                                .Select(s => new { s.MerchantId, s.UserName })
                                .Where(s => s.UserName == Convert.ToString(objJson.SelectToken("username")))
                                .FirstOrDefault();

                if (objMerchantList == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid username" }));
                }

                //Regex regex = new Regex(@"(?=^.{8,}$)(?=.*\d)(?=.*[a-z])(?=.*[!@#$%^&*()_+}{:;'?>.<,])(?!.*\s).*$");
                Regex regex = new Regex(@"(?=^.{8,15}$)(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[!@#$%^&*()_+}{:;'?>.<,])(?!.*\s).*$");
                Match match = regex.Match(Convert.ToString(objJson.SelectToken("password")));

                if (!match.Success)
                {
                    return BadRequest(new { Status = "Error", Error = "Password must contain 8-15 alpha numeric characters, including one UPPER case, one LOWER case and one special character" });
                }

                match = regex.Match(Convert.ToString(objJson.SelectToken("confirmpassword")));
                if (!match.Success)
                {
                    return BadRequest(new { Status = "Error", Error = "Confirm Password must contain 8-15 alpha numeric characters, including one UPPER case, one LOWER case and one special character" });
                }

                if (Convert.ToString(objJson.SelectToken("password")) != Convert.ToString(objJson.SelectToken("confirmpassword")))
                {
                    return BadRequest(new { Status = "Error", Error = "Please make sure that Password and Confirm Password matches" });
                }

                Merchants objMerchants = new Merchants();
                objMerchants.MerchantId = objMerchantList.MerchantId;
                objMerchants.PasswordHash = clsCommon.GetHashedValue(Convert.ToString(objJson.SelectToken("password")));
                _appDbContext.Merchants.Attach(objMerchants);
                _appDbContext.Entry(objMerchants).Property("PasswordHash").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();
                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Description = "Merchant account password has been changed" });
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/merchants/GetPrivateMerchantDetailbyID
        [HttpPost]
        [Route("GetPrivateMerchantDetailbyID")]
        public ActionResult GetPrivateMerchantDetailbyID()
        {
            try
            {
                if (string.IsNullOrEmpty(Convert.ToString(Request.Headers["api_key"])))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strMerchantId = MerchantLoginSessionKeys.GetMerchantIdByApiKey(_appDbContext, Guid.Parse(Request.Headers["api_key"]));
                if (string.IsNullOrEmpty(strMerchantId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                var objMerchantDetails = (from m in _appDbContext.Merchants
                                          where (m.MerchantId == strMerchantId)
                                          select m);

                if (objMerchantDetails == null || objMerchantDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Merchant doesn't exists." }));
                }

                return Ok(new { Status = "OK", MerchantDetails = objMerchantDetails });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/merchants/GetPublicMerchantDetailById
        [HttpPost]
        [Route("GetPublicMerchantDetailById")]
        public ActionResult GetPublicMerchantDetailById()
        {
            try
            {
                if (string.IsNullOrEmpty(Convert.ToString(Request.Headers["api_key"])))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strMerchantId = MerchantLoginSessionKeys.GetMerchantIdByApiKey(_appDbContext, Guid.Parse(Request.Headers["api_key"]));
                if (string.IsNullOrEmpty(strMerchantId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                var objMerchantDetails = from m in _appDbContext.Merchants
                                         join c in _appDbContext.Countries on m.CountryID equals c.Id into DetailsCountries
                                         from c1 in DetailsCountries.DefaultIfEmpty()
                                         join s in _appDbContext.StateProvinces on m.StateProvinceID equals s.Id into DetailsStateProvinces
                                         from s1 in DetailsStateProvinces.DefaultIfEmpty()
                                         where (m.MerchantId == strMerchantId)
                                         select new
                                         {
                                             MerchantName = m.MerchantName,
                                             MerchantType = Convert.ToString(m.MerchantType),
                                             AddressLine1 = m.AddressLine1,
                                             AddressLine2 = m.AddressLine2,
                                             AddressLine3 = m.AddressLine3,
                                             City = Convert.ToString(m.CityID),
                                             StateProvince = s1.Name,
                                             Country = c1.Name,
                                             ZipPostalCode = m.ZipPostalCode
                                         };

                if (objMerchantDetails == null || objMerchantDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Merchant doesn't exists." }));
                }

                /*
                var objMerchantCampaigns = _appDbContext.MerchantCampaigns
                                .Where(mc => mc.MerchantId == strMerchantId && mc.Active == true).ToList();

                List<JObject> lstMerchantCampaignsAndPackages = new List<JObject>();
                if (objMerchantCampaigns.Count() > 0)
                {
                    foreach (MerchantCampaigns objCampaignsItem in objMerchantCampaigns)
                    {
                        List<MerchantPackages> lstMerchantPackages = new List<MerchantPackages>();
                        var objMerchantPackages = _appDbContext.MerchantPackages
                                    .Where(mp => mp.CampaignID == objCampaignsItem.Id).ToList<MerchantPackages>();
                        if (objMerchantPackages.Count() > 0)
                        {
                            foreach (MerchantPackages objPackagesItem in objMerchantPackages)
                            {
                                lstMerchantPackages.Add(objPackagesItem);
                            }
                        }

                        JObject obj = new JObject();
                        obj.Add("Campaign", JToken.FromObject(objCampaignsItem));
                        obj.Add("Packages", JToken.FromObject(lstMerchantPackages));
                        lstMerchantCampaignsAndPackages.Add(obj);
                    }
                }
                */

                var objMerchantCampaignsAndPackages = (from mc in _appDbContext.MerchantCampaigns
                                                       join c in _appDbContext.Countries on mc.CountryID equals c.Id into DetailsCountries
                                                       from c1 in DetailsCountries.DefaultIfEmpty()
                                                       join s in _appDbContext.StateProvinces on mc.StateProvinceID equals s.Id into DetailsStateProvinces
                                                       from s1 in DetailsStateProvinces.DefaultIfEmpty()
                                                       where (mc.MerchantId == strMerchantId && mc.Active == true)
                                                       select new
                                                       {
                                                           Id = Convert.ToString(mc.Id),
                                                           MerchantId = mc.MerchantId,
                                                           Name = mc.Name,
                                                           Description = mc.Description,
                                                           Disclosure = mc.Disclosure,
                                                           CreatedDate = Convert.ToString(mc.CreatedDate),
                                                           StartDate = Convert.ToString(mc.StartDate),
                                                           EndDate = Convert.ToString(mc.EndDate),
                                                           StateProvince = s1.Name,
                                                           City = Convert.ToString(mc.CityID),
                                                           Country = c1.Name,
                                                           Active = mc.Active,
                                                           Packages =
                                                           (
                                                               from mp in _appDbContext.MerchantPackages
                                                               join pt in _appDbContext.PackageType on mp.PackageTypeID equals pt.Id into DetailsPackageType
                                                               from pt1 in DetailsPackageType.DefaultIfEmpty()
                                                               where (mp.CampaignID == mc.Id)
                                                               select new
                                                               {
                                                                   PackageId = Convert.ToString(mp.PackageId),
                                                                   CampaignName = mc.Name,
                                                                   PackageType = pt1.Description,
                                                                   Description = mp.Description,
                                                                   Price = Convert.ToString(mp.Price),
                                                                   CreatedDate = Convert.ToString(mp.CreatedDate),
                                                                   StartDate = Convert.ToString(mp.StartDate),
                                                                   EndDate = Convert.ToString(mp.EndDate)
                                                               }
                                                           )
                                                       });



                return Ok(new { Status = "OK", MerchantDetails = objMerchantDetails, MerchantCampaignsAndPackages = objMerchantCampaignsAndPackages });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/merchants/GetMerchantActiveCampaign
        [HttpPost]
        [Route("GetMerchantActiveCampaign")]
        public ActionResult GetMerchantActiveCampaign()
        {
            try
            {
                if (string.IsNullOrEmpty(Convert.ToString(Request.Headers["api_key"])))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strMerchantId = MerchantLoginSessionKeys.GetMerchantIdByApiKey(_appDbContext, Guid.Parse(Request.Headers["api_key"]));
                if (string.IsNullOrEmpty(strMerchantId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                var objMerchantCampaigns = (from mc in _appDbContext.MerchantCampaigns
                                            join c in _appDbContext.Countries on mc.CountryID equals c.Id into DetailsCountries
                                            from c1 in DetailsCountries.DefaultIfEmpty()
                                            join s in _appDbContext.StateProvinces on mc.StateProvinceID equals s.Id into DetailsStateProvinces
                                            from s1 in DetailsStateProvinces.DefaultIfEmpty()
                                            where (mc.MerchantId == strMerchantId && mc.Active == true)
                                            select new
                                            {
                                                Id = Convert.ToString(mc.Id),
                                                MerchantId = mc.MerchantId,
                                                Name = mc.Name,
                                                Description = mc.Description,
                                                Disclosure = mc.Disclosure,
                                                CreatedDate = Convert.ToString(mc.CreatedDate),
                                                StartDate = Convert.ToString(mc.StartDate),
                                                EndDate = Convert.ToString(mc.EndDate),
                                                StateProvince = s1.Name,
                                                City = Convert.ToString(mc.CityID),
                                                Country = c1.Name,
                                                Active = mc.Active,
                                            });


                if (objMerchantCampaigns == null || objMerchantCampaigns.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "No any active campaigns available." }));
                }

                return Ok(new { Status = "OK", MerchantCampaigns = objMerchantCampaigns });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/merchants/GetMerchantActivePackage
        [HttpPost]
        [Route("GetMerchantActivePackage")]
        public ActionResult GetMerchantActivePackage()
        {
            try
            {
                if (string.IsNullOrEmpty(Convert.ToString(Request.Headers["api_key"])))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strMerchantId = MerchantLoginSessionKeys.GetMerchantIdByApiKey(_appDbContext, Guid.Parse(Request.Headers["api_key"]));
                if (string.IsNullOrEmpty(strMerchantId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                var objMerchantPackages = (from mp in _appDbContext.MerchantPackages
                                           join mc in _appDbContext.MerchantCampaigns on mp.CampaignID equals mc.Id into DetailsMerchantCampaigns
                                           from mc1 in DetailsMerchantCampaigns.DefaultIfEmpty()
                                           join pt in _appDbContext.PackageType on mp.PackageTypeID equals pt.Id into DetailsPackageType
                                           from pt1 in DetailsPackageType.DefaultIfEmpty()
                                           where (mc1.MerchantId == strMerchantId && mc1.Active == true)
                                           select new
                                           {
                                               PackageId = Convert.ToString(mp.PackageId),
                                               CampaignName = mc1.Name,
                                               PackageType = pt1.Description,
                                               Description = mp.Description,
                                               Price = Convert.ToString(mp.Price),
                                               CreatedDate = Convert.ToString(mp.CreatedDate),
                                               StartDate = Convert.ToString(mp.StartDate),
                                               EndDate = Convert.ToString(mp.EndDate)
                                           });


                if (objMerchantPackages == null || objMerchantPackages.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "No any active packages available." }));
                }

                return Ok(new { Status = "OK", MerchantPackages = objMerchantPackages });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/merchants/GetMerchantActiveCampaignAndPackages
        [HttpPost]
        [Route("GetMerchantActiveCampaignAndPackages")]
        public ActionResult GetMerchantActiveCampaignAndPackages()
        {
            try
            {
                if (string.IsNullOrEmpty(Convert.ToString(Request.Headers["api_key"])))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strMerchantId = MerchantLoginSessionKeys.GetMerchantIdByApiKey(_appDbContext, Guid.Parse(Request.Headers["api_key"]));
                if (string.IsNullOrEmpty(strMerchantId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                var objMerchantCampaignsAndPackages = (from mc in _appDbContext.MerchantCampaigns
                                                       join c in _appDbContext.Countries on mc.CountryID equals c.Id into DetailsCountries
                                                       from c1 in DetailsCountries.DefaultIfEmpty()
                                                       join s in _appDbContext.StateProvinces on mc.StateProvinceID equals s.Id into DetailsStateProvinces
                                                       from s1 in DetailsStateProvinces.DefaultIfEmpty()
                                                       where (mc.MerchantId == strMerchantId && mc.Active == true)
                                                       select new
                                                       {
                                                           Id = Convert.ToString(mc.Id),
                                                           MerchantId = mc.MerchantId,
                                                           Name = mc.Name,
                                                           Description = mc.Description,
                                                           Disclosure = mc.Disclosure,
                                                           CreatedDate = Convert.ToString(mc.CreatedDate),
                                                           StartDate = Convert.ToString(mc.StartDate),
                                                           EndDate = Convert.ToString(mc.EndDate),
                                                           StateProvince = s1.Name,
                                                           City = Convert.ToString(mc.CityID),
                                                           Country = c1.Name,
                                                           Active = mc.Active,
                                                           Packages =
                                                           (
                                                               from mp in _appDbContext.MerchantPackages
                                                               join pt in _appDbContext.PackageType on mp.PackageTypeID equals pt.Id into DetailsPackageType
                                                               from pt1 in DetailsPackageType.DefaultIfEmpty()
                                                               where (mp.CampaignID == mc.Id)
                                                               select new
                                                               {
                                                                   PackageId = Convert.ToString(mp.PackageId),
                                                                   CampaignName = mc.Name,
                                                                   PackageType = pt1.Description,
                                                                   Description = mp.Description,
                                                                   Price = Convert.ToString(mp.Price),
                                                                   CreatedDate = Convert.ToString(mp.CreatedDate),
                                                                   StartDate = Convert.ToString(mp.StartDate),
                                                                   EndDate = Convert.ToString(mp.EndDate)
                                                               }
                                                           )
                                                       });


                if (objMerchantCampaignsAndPackages == null || objMerchantCampaignsAndPackages.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "No any active campaigns and packages available." }));
                }

                return Ok(new { Status = "OK", MerchantCampaignsAndPackages = objMerchantCampaignsAndPackages });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/merchants/GetMerchantList
        [HttpPost]
        [Route("GetMerchantList")]
        public ActionResult GetMerchantList([FromBody] JObject objJson)
        {
            try
            {
                if (string.IsNullOrEmpty(Convert.ToString(Request.Headers["api_key"])))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strUserId = UserLoginSessionKeys.GetUserIdByApiKey(_appDbContext, Guid.Parse(Request.Headers["api_key"]));
                if (string.IsNullOrEmpty(strUserId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                var objUsersDetail = (from u in _appDbContext.Users
                                      where u.UserId == strUserId
                                      select u).ToList<Users>();

                var objMerchantList = (from m in _appDbContext.Merchants
                                       join mc in _appDbContext.MerchantCampaigns on m.MerchantId equals mc.MerchantId into DetailsMerchantCampaigns
                                       from mc1 in DetailsMerchantCampaigns.DefaultIfEmpty()
                                       join mp in _appDbContext.MerchantPackages on mc1.Id equals mp.CampaignID into DetailsMerchantPackages
                                       from mp1 in DetailsMerchantPackages.DefaultIfEmpty()
                                       join mpp in _appDbContext.MerchantPackagesPictures on mp1.PackageId equals mpp.PackageId into DetailsMerchantPackagesPictures
                                       from mpp1 in DetailsMerchantPackagesPictures.DefaultIfEmpty()
                                       join pt in _appDbContext.PackageType on mp1.PackageTypeID equals pt.Id into DetailsPackageType
                                       from pt1 in DetailsPackageType.DefaultIfEmpty()
                                       join sp in _appDbContext.StateProvinces on m.StateProvinceID equals sp.Id into DetailsStateProvinces
                                       from sp1 in DetailsStateProvinces.DefaultIfEmpty()
                                       where m.Active == true //&&
                                       //(Convert.ToDateTime(mp1.StartDate.Value.Date) <= Convert.ToDateTime(objJson.SelectToken("preferreddate")))
                                       //&& (Convert.ToDateTime(mp1.EndDate.Value.Date) >= Convert.ToDateTime(objJson.SelectToken("preferreddate")))
                                       select new
                                       {
                                           MerchantId = m.MerchantId,
                                           MerchantName = m.MerchantName,
                                           AddressLine1 = m.AddressLine1,
                                           AddressLine2 = m.AddressLine2,
                                           AddressLine3 = m.AddressLine3,
                                           Zip = m.ZipPostalCode,
                                           State = sp1.Description,
                                           Rating = m.RatingAverage,
                                           CampaignId = mc1.Id,
                                           CampaignName = mc1.Name,
                                           CampaignDescription = mc1.Description,
                                           CampaignStartDate = mc1.StartDate,
                                           CampaignEndDate = mc1.EndDate,
                                           PackageId = mp1.PackageId,
                                           PackageDescription = mp1.Description,
                                           PackagePrice = mp1.Price,
                                           PackageStartDate = mp1.StartDate,
                                           PackageEndDate = mp1.EndDate,
                                           MinPersonRequired = mp1.MinPerson,
                                           DiscountPercentage = mp1.DiscountPercentage,
                                           PackageImage = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("Merchant").Value) + mpp1.ImageName),
                                           PackageType = pt1.Description,
                                           isAvailablePreferDate = true,
                                           isAvailableBackupDate = false,
                                           StartDate = mp1.StartDate.Value.Date,
                                           EndDate = mp1.EndDate.Value.Date,
                                           MerchantType = m.MerchantType,
                                           Distance = GetDistance(objUsersDetail[0].GeoLocationLatitude, objUsersDetail[0].GeoLocationLongitude,m.GeolocationLattitude,m.GeolocationLongitude),
                                       }).AsQueryable();

                if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("preferreddate"))))
                {
                    objMerchantList = objMerchantList.Where(e =>
                    (Convert.ToDateTime(e.StartDate) <= Convert.ToDateTime(objJson.SelectToken("preferreddate")))
                                       && (Convert.ToDateTime(e.EndDate) >= Convert.ToDateTime(objJson.SelectToken("preferreddate")))
                    );
                }

                if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("backupdate"))))
                {
                    objMerchantList = objMerchantList.Where(e =>
                    (Convert.ToDateTime(e.StartDate) <= Convert.ToDateTime(objJson.SelectToken("backupdate")))
                                       && (Convert.ToDateTime(e.EndDate) >= Convert.ToDateTime(objJson.SelectToken("backupdate")))
                    );
                }

                if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("minprice"))))
                {
                    objMerchantList = objMerchantList.Where(e =>
                    (Convert.ToDecimal(e.PackagePrice) >= Convert.ToDecimal(objJson.SelectToken("minprice"))));
                }

                if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("maxprice"))))
                {
                    objMerchantList = objMerchantList.Where(e =>
                     (Convert.ToDecimal(e.PackagePrice) <= Convert.ToDecimal(objJson.SelectToken("maxprice")))
                    );
                }

                if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("minperson"))))
                {
                    objMerchantList = objMerchantList.Where(e =>
                    (e.MinPersonRequired) >= Convert.ToInt32(objJson.SelectToken("minperson")));
                }

                //will add it later.
                if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("relevance"))))
                {
                    //objMerchantList = objMerchantList.OrderBy
                }

                if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("pricelowtohigh"))))
                {
                    objMerchantList = objMerchantList.OrderBy(t => t.PackagePrice);
                }

                if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("pricehightolow"))))
                {
                    objMerchantList = objMerchantList.OrderByDescending(t => t.PackagePrice);
                }

                if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("nearesttofurthest"))))
                {
                    objMerchantList = objMerchantList.OrderBy(t => t.Distance);
                }

                if (objMerchantList == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Merchant List not available" }));
                }

                return Ok(new { Status = "OK", MerchantList = objMerchantList });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/merchants/GetMerchantTypes
        [HttpPost]
        [Route("GetMerchantTypes")]
        public ActionResult GetMerchantTypes([FromBody] JObject objJson)
        {
            try
            {
                if (string.IsNullOrEmpty(Convert.ToString(Request.Headers["api_key"])))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strUserId = UserLoginSessionKeys.GetUserIdByApiKey(_appDbContext, Guid.Parse(Request.Headers["api_key"]));
                if (string.IsNullOrEmpty(strUserId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }
                var objMerchantTypes = _appDbContext.MerchantTypes.Select(s => s);

                if (objMerchantTypes == null || objMerchantTypes.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Data Not Found" }));
                }

                return Ok(new { Status = "OK", MerchantTypes = objMerchantTypes });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        public string SendVerificationEmail_ForgotMerchantPassword(string strEmail, string strVerificationCode)
        {
            try
            {
                StringBuilder objEmailBody = new StringBuilder();
                objEmailBody.AppendLine("<html>");
                objEmailBody.AppendLine("<body>");
                objEmailBody.AppendLine("Your verification code is " + strVerificationCode + " for change password. Please enter your verification code in the app to change password.");
                objEmailBody.AppendLine("<br/><br/>");
                objEmailBody.AppendLine("Thank You.");
                objEmailBody.AppendLine("</div>");
                objEmailBody.AppendLine("</body>");
                objEmailBody.AppendLine("</html>");

                string strMailStatus = new clsEmail(configuration).SendEmail(strEmail, "Merchant Change Password", Convert.ToString(objEmailBody), "Nurchure - Merchant Change Password");
                return strMailStatus;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public double GetDistance(double longitude, double latitude, double otherLongitude, double otherLatitude)
        {
            var d1 = latitude * (Math.PI / 180.0);
            var num1 = longitude * (Math.PI / 180.0);
            var d2 = otherLatitude * (Math.PI / 180.0);
            var num2 = otherLongitude * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

            return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
        }
    }
}