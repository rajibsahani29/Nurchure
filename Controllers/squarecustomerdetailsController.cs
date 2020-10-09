using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NurchureAPI.Models;

namespace NurchureAPI.Controllers
{
    //[System.Web.Http.Route("api/squarecustomerdetails")]
    [Route("api/[controller]")]
    [ApiController]
    public class squarecustomerdetailsController : Controller
    {
        private AppDbContext _appDbContext;
        private IConfiguration configuration;

        public squarecustomerdetailsController(AppDbContext context, IConfiguration iConfig)
        {
            _appDbContext = context;
            configuration = iConfig;
        }

        // POST api/squarecustomerdetails/AddSquareCustomerDetails
        [HttpPost]
        [Route("AddSquareCustomerDetails")]
        public async Task<ActionResult> AddSquareCustomerDetails([FromBody] JObject objJson)
        {
            try
            {
                if (string.IsNullOrEmpty(Convert.ToString(Request.Headers["api_key"])))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strUserId = UserLoginSessionKeys.GetUserIdByApiKey(_appDbContext, Guid.Parse(Request.Headers["api_key"]));
                if (string.IsNullOrEmpty(strUserId))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                var objSquareCustomerData = _appDbContext.SquareCustomerDetails
                                .Where(scd => scd.UserId == strUserId)
                                .OrderByDescending(t => t.Id).FirstOrDefault();

                if (objSquareCustomerData != null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Customer already registered on square." }));
                }

                string strSquareRequestUrl = Convert.ToString(configuration.GetSection("appSettings").GetSection("SquareRequestUrl").Value);
                string strSquareAccessToken = Convert.ToString(configuration.GetSection("appSettings").GetSection("SquareAccessToken").Value);

                if (strSquareRequestUrl == "" || strSquareAccessToken == "")
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                string strRequestUrl = strSquareRequestUrl + "customers";
                string strRequestData = "{\"given_name\": \"" + Convert.ToString(objJson.SelectToken("given_name")) + "\",\"family_name\": \"" + Convert.ToString(objJson.SelectToken("family_name")) + "\",\"email_address\": \"" + Convert.ToString(objJson.SelectToken("email_address")) + "\",\"address\": { \"address_line_1\": \"" + Convert.ToString(objJson.SelectToken("address_line_1")) + "\", \"address_line_2\": \"" + Convert.ToString(objJson.SelectToken("address_line_2")) + "\", \"locality\": \"" + Convert.ToString(objJson.SelectToken("locality")) + "\", \"administrative_district_level_1\": \"" + Convert.ToString(objJson.SelectToken("administrative_district_level_1")) + "\", \"postal_code\": \"" + Convert.ToString(objJson.SelectToken("postal_code")) + "\", \"country\": \"" + Convert.ToString(objJson.SelectToken("country")) + "\" }, \"phone_number\": \"" + Convert.ToString(objJson.SelectToken("phone_number")) + "\", \"reference_id\": \"" + Convert.ToString(objJson.SelectToken("reference_id")) + "\", \"note\": \"" + Convert.ToString(objJson.SelectToken("note")) + "\"}";
                WebRequest objWebRequest = WebRequest.Create(strRequestUrl);
                objWebRequest.Method = "POST";
                byte[] byteArray;
                byteArray = Encoding.UTF8.GetBytes(strRequestData);
                objWebRequest.ContentType = "application/json";
                objWebRequest.ContentLength = byteArray.Length;
                objWebRequest.Headers.Add("Authorization", "Bearer " + strSquareAccessToken);
                Stream dataStream = objWebRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse objWebResponse = objWebRequest.GetResponse();
                dataStream = objWebResponse.GetResponseStream();
                StreamReader dataReader = new StreamReader(dataStream);
                string strResponse = dataReader.ReadToEnd();
                dataReader.Close();
                dataStream.Close();
                objWebResponse.Close();

                if (string.IsNullOrEmpty(strResponse))
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                string strSquareCustomerId = "";

                JObject objResponseJson = JObject.Parse(strResponse);
                if (objResponseJson.SelectToken("customer") != null)
                {
                    strSquareCustomerId = Convert.ToString(objResponseJson.SelectToken("customer").SelectToken("id"));
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                SquareCustomerDetails objSquareCustomerDetails = new SquareCustomerDetails();
                objSquareCustomerDetails.UserId = strUserId;
                objSquareCustomerDetails.SquareCustomerId = strSquareCustomerId;
                objSquareCustomerDetails.AddedDate = DateTime.UtcNow;
                await _appDbContext.SquareCustomerDetails.AddAsync(objSquareCustomerDetails);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Customer details added on square." });
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

        // POST api/squarecustomerdetails/GetSquareCustomerDetailsByUserId
        [HttpPost]
        [Route("GetSquareCustomerDetailsByUserId")]
        public ActionResult GetSquareCustomerDetailsByUserId()
        {
            try
            {
                if (string.IsNullOrEmpty(Convert.ToString(Request.Headers["api_key"])))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strUserId = UserLoginSessionKeys.GetUserIdByApiKey(_appDbContext, Guid.Parse(Request.Headers["api_key"]));
                if (string.IsNullOrEmpty(strUserId))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strSquareRequestUrl = Convert.ToString(configuration.GetSection("appSettings").GetSection("SquareRequestUrl").Value);
                string strSquareAccessToken = Convert.ToString(configuration.GetSection("appSettings").GetSection("SquareAccessToken").Value);

                if (strSquareRequestUrl == "" || strSquareAccessToken == "")
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                var objSquareCustomerDetails = _appDbContext.SquareCustomerDetails
                                .Where(scd => scd.UserId == strUserId)
                                .OrderByDescending(t => t.Id).FirstOrDefault();

                if (objSquareCustomerDetails == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Square customer details doesn’t exist." }));
                }

                string strSquareCustomerId = objSquareCustomerDetails.SquareCustomerId;

                string strRequestUrl = strSquareRequestUrl + "customers/" + strSquareCustomerId;
                WebRequest objWebRequest = WebRequest.Create(strRequestUrl);
                objWebRequest.Method = "GET";
                objWebRequest.ContentType = "application/json";
                objWebRequest.Headers.Add("Authorization", "Bearer " + strSquareAccessToken);
                //Stream dataStream = objWebRequest.GetRequestStream();
                //dataStream.Close();
                WebResponse objWebResponse = objWebRequest.GetResponse();
                Stream dataStream = objWebResponse.GetResponseStream();
                StreamReader dataReader = new StreamReader(dataStream);
                string strResponse = dataReader.ReadToEnd();
                dataReader.Close();
                dataStream.Close();
                objWebResponse.Close();

                if (string.IsNullOrEmpty(strResponse))
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                JObject objResponseJson = JObject.Parse(strResponse);
                return Ok(new { Status = "OK", SquareCustomerDetails = objResponseJson });
                
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/squarecustomerdetails/UpdateSquareCustomerDetails
        [HttpPost]
        [Route("UpdateSquareCustomerDetails")]
        public async Task<ActionResult> UpdateSquareCustomerDetails([FromBody] JObject objJson)
        {
            try
            {
                if (string.IsNullOrEmpty(Convert.ToString(Request.Headers["api_key"])))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strUserId = UserLoginSessionKeys.GetUserIdByApiKey(_appDbContext, Guid.Parse(Request.Headers["api_key"]));
                if (string.IsNullOrEmpty(strUserId))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strSquareRequestUrl = Convert.ToString(configuration.GetSection("appSettings").GetSection("SquareRequestUrl").Value);
                string strSquareAccessToken = Convert.ToString(configuration.GetSection("appSettings").GetSection("SquareAccessToken").Value);

                if (strSquareRequestUrl == "" || strSquareAccessToken == "")
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                var objSquareCustomerData = _appDbContext.SquareCustomerDetails
                                .Where(scd => scd.UserId == strUserId)
                                .AsNoTracking().OrderByDescending(t => t.Id).FirstOrDefault();

                if (objSquareCustomerData == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Square customer details doesn’t exist." }));
                }

                string strSquareCustomerId = objSquareCustomerData.SquareCustomerId;

                string strRequestUrl = strSquareRequestUrl + "customers/" + strSquareCustomerId;
                string strRequestData = "{\"given_name\": \"" + Convert.ToString(objJson.SelectToken("given_name")) + "\",\"family_name\": \"" + Convert.ToString(objJson.SelectToken("family_name")) + "\",\"email_address\": \"" + Convert.ToString(objJson.SelectToken("email_address")) + "\",\"address\": { \"address_line_1\": \"" + Convert.ToString(objJson.SelectToken("address_line_1")) + "\", \"address_line_2\": \"" + Convert.ToString(objJson.SelectToken("address_line_2")) + "\", \"locality\": \"" + Convert.ToString(objJson.SelectToken("locality")) + "\", \"administrative_district_level_1\": \"" + Convert.ToString(objJson.SelectToken("administrative_district_level_1")) + "\", \"postal_code\": \"" + Convert.ToString(objJson.SelectToken("postal_code")) + "\", \"country\": \"" + Convert.ToString(objJson.SelectToken("country")) + "\" }, \"phone_number\": \"" + Convert.ToString(objJson.SelectToken("phone_number")) + "\", \"reference_id\": \"" + Convert.ToString(objJson.SelectToken("reference_id")) + "\", \"note\": \"" + Convert.ToString(objJson.SelectToken("note")) + "\"}";
                WebRequest objWebRequest = WebRequest.Create(strRequestUrl);
                objWebRequest.Method = "PUT";
                byte[] byteArray;
                byteArray = Encoding.UTF8.GetBytes(strRequestData);
                objWebRequest.ContentType = "application/json";
                objWebRequest.ContentLength = byteArray.Length;
                objWebRequest.Headers.Add("Authorization", "Bearer " + strSquareAccessToken);
                Stream dataStream = objWebRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse objWebResponse = objWebRequest.GetResponse();
                dataStream = objWebResponse.GetResponseStream();
                StreamReader dataReader = new StreamReader(dataStream);
                string strResponse = dataReader.ReadToEnd();
                dataReader.Close();
                dataStream.Close();
                objWebResponse.Close();

                if (string.IsNullOrEmpty(strResponse))
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                JObject objResponseJson = JObject.Parse(strResponse);
                if (objResponseJson.SelectToken("customer") == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }
                
                SquareCustomerDetails objSquareCustomerDetails = new SquareCustomerDetails();
                objSquareCustomerDetails.Id = Convert.ToInt32(objSquareCustomerData.Id);
                objSquareCustomerDetails.LastUpdatedDate = DateTime.UtcNow;

                _appDbContext.SquareCustomerDetails.Attach(objSquareCustomerDetails);
                _appDbContext.Entry(objSquareCustomerDetails).Property("LastUpdatedDate").IsModified = true;

                int returnVal = await _appDbContext.SaveChangesAsync();
                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Customer details updated on square." });
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

        // POST api/squarecustomerdetails/DeleteSquareCustomerDetails
        [HttpPost]
        [Route("DeleteSquareCustomerDetails")]
        public async Task<ActionResult> DeleteSquareCustomerDetails()
        {
            try
            {
                if (string.IsNullOrEmpty(Convert.ToString(Request.Headers["api_key"])))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strUserId = UserLoginSessionKeys.GetUserIdByApiKey(_appDbContext, Guid.Parse(Request.Headers["api_key"]));
                if (string.IsNullOrEmpty(strUserId))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strSquareRequestUrl = Convert.ToString(configuration.GetSection("appSettings").GetSection("SquareRequestUrl").Value);
                string strSquareAccessToken = Convert.ToString(configuration.GetSection("appSettings").GetSection("SquareAccessToken").Value);

                if (strSquareRequestUrl == "" || strSquareAccessToken == "")
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                var objSquareCustomerDetails = _appDbContext.SquareCustomerDetails
                                .Where(scd => scd.UserId == strUserId)
                                .OrderByDescending(t => t.Id).FirstOrDefault();

                if (objSquareCustomerDetails == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Square customer details doesn’t exist." }));
                }

                string strSquareCustomerId = objSquareCustomerDetails.SquareCustomerId;

                string strRequestUrl = strSquareRequestUrl + "customers/" + strSquareCustomerId;
                WebRequest objWebRequest = WebRequest.Create(strRequestUrl);
                objWebRequest.Method = "DELETE";
                //objWebRequest.ContentType = "application/json";
                objWebRequest.Headers.Add("Authorization", "Bearer " + strSquareAccessToken);
                //Stream dataStream = objWebRequest.GetRequestStream();
                //dataStream.Close();
                WebResponse objWebResponse = objWebRequest.GetResponse();
                Stream dataStream = objWebResponse.GetResponseStream();
                StreamReader dataReader = new StreamReader(dataStream);
                string strResponse = dataReader.ReadToEnd();
                dataReader.Close();
                dataStream.Close();
                objWebResponse.Close();

                JObject objResponseJson = JObject.Parse(strResponse);
                if (objResponseJson.Count > 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                _appDbContext.SquareCustomerDetails.Remove(objSquareCustomerDetails);
                int returnVal = await _appDbContext.SaveChangesAsync();
                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Customer details deleted on square." });
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
    }
}