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
    //[System.Web.Http.Route("api/squarecustomercarddetails")]
    [Route("api/[controller]")]
    [ApiController]
    public class squarecustomercarddetailsController : Controller
    {
        private AppDbContext _appDbContext;
        private IConfiguration configuration;

        public squarecustomercarddetailsController(AppDbContext context, IConfiguration iConfig)
        {
            _appDbContext = context;
            configuration = iConfig;
        }

        // POST api/squarecustomercarddetails/AddSquareCustomerCardDetails
        [HttpPost]
        [Route("AddSquareCustomerCardDetails")]
        public async Task<ActionResult> AddSquareCustomerCardDetails([FromBody] JObject objJson)
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

                if (objSquareCustomerData == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Square customer details doesn’t exist." }));
                }

                string strSquareRequestUrl = Convert.ToString(configuration.GetSection("appSettings").GetSection("SquareRequestUrl").Value);
                string strSquareAccessToken = Convert.ToString(configuration.GetSection("appSettings").GetSection("SquareAccessToken").Value);

                if (strSquareRequestUrl == "" || strSquareAccessToken == "")
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                string strSquareCustomerId = objSquareCustomerData.SquareCustomerId;

                string strRequestUrl = strSquareRequestUrl + "customers/" + strSquareCustomerId + "/cards";
                string strRequestData = "{\"card_nonce\": \"" + Convert.ToString(objJson.SelectToken("card_nonce")) + "\",\"billing_address\": { \"address_line_1\": \"" + Convert.ToString(objJson.SelectToken("address_line_1")) + "\", \"address_line_2\": \"" + Convert.ToString(objJson.SelectToken("address_line_2")) + "\", \"locality\": \"" + Convert.ToString(objJson.SelectToken("locality")) + "\", \"administrative_district_level_1\": \"" + Convert.ToString(objJson.SelectToken("administrative_district_level_1")) + "\", \"postal_code\": \"" + Convert.ToString(objJson.SelectToken("postal_code")) + "\", \"country\": \"" + Convert.ToString(objJson.SelectToken("country")) + "\" }, \"cardholder_name\": \"" + Convert.ToString(objJson.SelectToken("cardholder_name")) + "\"}";
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

                string strSquareCustomerCardId = "";

                JObject objResponseJson = JObject.Parse(strResponse);
                if (objResponseJson.SelectToken("card") != null)
                {
                    strSquareCustomerCardId = Convert.ToString(objResponseJson.SelectToken("card").SelectToken("id"));
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                if (Convert.ToBoolean(Convert.ToInt32(objJson.SelectToken("isprimary"))) == true)
                {
                    _appDbContext.SquareCustomerCardDetails.Where(x => x.UserId == strUserId).ToList().ForEach(x =>
                    {
                        x.IsPrimary = false;
                    });
                    await _appDbContext.SaveChangesAsync();
                }

                SquareCustomerCardDetails objSquareCustomerCardDetails = new SquareCustomerCardDetails();
                objSquareCustomerCardDetails.UserId = strUserId;
                objSquareCustomerCardDetails.SquareCustomerId = strSquareCustomerId;
                objSquareCustomerCardDetails.SquareCustomerCardId = strSquareCustomerCardId;
                objSquareCustomerCardDetails.IsPrimary = Convert.ToBoolean(Convert.ToInt32(objJson.SelectToken("isprimary")));
                objSquareCustomerCardDetails.AddedDate = DateTime.UtcNow;
                await _appDbContext.SquareCustomerCardDetails.AddAsync(objSquareCustomerCardDetails);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Customer card details added on square." });
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

        // POST api/squarecustomercarddetails/GetSquareCustomerCardDetailsByUserId
        [HttpPost]
        [Route("GetSquareCustomerCardDetailsByUserId")]
        public ActionResult GetSquareCustomerCardDetailsByUserId()
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

        // POST api/squarecustomercarddetails/DeleteSquareCustomerCardDetails
        [HttpPost]
        [Route("DeleteSquareCustomerCardDetails")]
        public async Task<ActionResult> DeleteSquareCustomerCardDetails([FromBody] JObject objJson)
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

                var objSquareCustomerCardDetails = _appDbContext.SquareCustomerCardDetails
                                .Where(sccd => sccd.UserId == strUserId && sccd.SquareCustomerCardId == Convert.ToString(objJson.SelectToken("squarecustomercardid")))
                                .OrderByDescending(t => t.Id).FirstOrDefault();

                if (objSquareCustomerCardDetails == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Square customer card details doesn’t exist." }));
                }

                string strSquareCustomerId = objSquareCustomerCardDetails.SquareCustomerId;
                string strSquareCustomerCardId = objSquareCustomerCardDetails.SquareCustomerCardId;

                string strRequestUrl = strSquareRequestUrl + "customers/" + strSquareCustomerId + "/cards/" + strSquareCustomerCardId;
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

                _appDbContext.SquareCustomerCardDetails.Remove(objSquareCustomerCardDetails);
                int returnVal = await _appDbContext.SaveChangesAsync();
                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Customer card details deleted on square." });
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

        // POST api/squarecustomercarddetails/DeleteAllSquareCustomerCardDetails
        [HttpPost]
        [Route("DeleteAllSquareCustomerCardDetails")]
        public async Task<ActionResult> DeleteAllSquareCustomerCardDetails()
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

                var objSquareCustomerCardDetails = _appDbContext.SquareCustomerCardDetails
                                .Where(sccd => sccd.UserId == strUserId).ToList<SquareCustomerCardDetails>();

                if (objSquareCustomerCardDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Square customer card details doesn’t exist." }));
                }

                bool bnlErrorflag = false;
                foreach (SquareCustomerCardDetails objItem in objSquareCustomerCardDetails)
                {
                    string strSquareCustomerId = objItem.SquareCustomerId;
                    string strSquareCustomerCardId = objItem.SquareCustomerCardId;

                    string strRequestUrl = strSquareRequestUrl + "customers/" + strSquareCustomerId + "/cards/" + strSquareCustomerCardId;
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
                        //return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                        bnlErrorflag = true;
                    }

                    _appDbContext.SquareCustomerCardDetails.Remove(objItem);
                    int returnVal = await _appDbContext.SaveChangesAsync();
                    if (returnVal <= 0)
                    {
                        bnlErrorflag = true;
                    }
                }
                if (bnlErrorflag == false)
                {
                    return Ok(new { Status = "OK", Detail = "Customer all cards deleted on square." });
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Some cards are not deleted, Please try again." }));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }
    }
}