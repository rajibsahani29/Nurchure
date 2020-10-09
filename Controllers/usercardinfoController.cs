using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NurchureAPI.Models;

namespace NurchureAPI.Controllers
{
    //[System.Web.Http.Route("api/usercardinfo")]
    [Route("api/[controller]")]
    [ApiController]
    public class usercardinfoController : Controller
    {
        private AppDbContext _appDbContext;
        private IConfiguration configuration;

        public usercardinfoController(AppDbContext context, IConfiguration iConfig)
        {
            _appDbContext = context;
            configuration = iConfig;
        }

        // POST api/usercardinfo/AddUserCardInfo
        [HttpPost]
        [Route("AddUserCardInfo")]
        public async Task<ActionResult> AddUserCardInfo([FromBody] JObject objJson)
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

                clsAppCrypt objclsAppCrypt = new clsAppCrypt(configuration);

                UserCardInfo objUserCardInfo = new UserCardInfo();
                objUserCardInfo.UserId = strUserId;
                objUserCardInfo.NameOnCard = Convert.ToString(objJson.SelectToken("nameoncard"));
                objUserCardInfo.CardNumber = objclsAppCrypt.EncryptTripleDES(Convert.ToString(objJson.SelectToken("cardnumber")));
                objUserCardInfo.ExpiryDate = Convert.ToString(objJson.SelectToken("expirydate"));
                objUserCardInfo.CardType = Convert.ToString(objJson.SelectToken("cardtype"));
                objUserCardInfo.Issuer = Convert.ToString(objJson.SelectToken("issuer"));
                objUserCardInfo.SecurityNumber = objclsAppCrypt.EncryptTripleDES(Convert.ToString(objJson.SelectToken("securitynumber")));
                objUserCardInfo.AddedDate = DateTime.UtcNow;
                await _appDbContext.UserCardInfo.AddAsync(objUserCardInfo);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Card Info Added." });
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

        // POST api/usercardinfo/UpdateUserCardInfo
        [HttpPost]
        [Route("UpdateUserCardInfo")]
        public async Task<ActionResult> UpdateUserCardInfo([FromBody] JObject objJson)
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

                var objUserCardInfoDetails = from uci in _appDbContext.UserCardInfo
                                             where (uci.Id == Convert.ToInt32(objJson.SelectToken("usercardinfoid")) && uci.UserId == strUserId)
                                             select uci;

                if (objUserCardInfoDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Card info doesn’t exist or Invalid User." }));
                }

                clsAppCrypt objclsAppCrypt = new clsAppCrypt(configuration);

                UserCardInfo objUserCardInfo = new UserCardInfo();
                objUserCardInfo.Id = Convert.ToInt32(objJson.SelectToken("usercardinfoid"));
                objUserCardInfo.NameOnCard = Convert.ToString(objJson.SelectToken("nameoncard"));
                objUserCardInfo.CardNumber = objclsAppCrypt.EncryptTripleDES(Convert.ToString(objJson.SelectToken("cardnumber")));
                objUserCardInfo.ExpiryDate = Convert.ToString(objJson.SelectToken("expirydate"));
                objUserCardInfo.CardType = Convert.ToString(objJson.SelectToken("cardtype"));
                objUserCardInfo.Issuer = Convert.ToString(objJson.SelectToken("issuer"));
                objUserCardInfo.SecurityNumber = objclsAppCrypt.EncryptTripleDES(Convert.ToString(objJson.SelectToken("securitynumber")));
                objUserCardInfo.LastUpdatedDate = DateTime.UtcNow;

                _appDbContext.UserCardInfo.Attach(objUserCardInfo);
                _appDbContext.Entry(objUserCardInfo).Property("NameOnCard").IsModified = true;
                _appDbContext.Entry(objUserCardInfo).Property("CardNumber").IsModified = true;
                _appDbContext.Entry(objUserCardInfo).Property("ExpiryDate").IsModified = true;
                _appDbContext.Entry(objUserCardInfo).Property("CardType").IsModified = true;
                _appDbContext.Entry(objUserCardInfo).Property("Issuer").IsModified = true;
                _appDbContext.Entry(objUserCardInfo).Property("SecurityNumber").IsModified = true;
                _appDbContext.Entry(objUserCardInfo).Property("LastUpdatedDate").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Card Info Updated." });
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

        // POST api/usercalendarevent/DeleteUserCard
        [HttpPost]
        [Route("DeleteUserCard")]
        public async Task<ActionResult> DeleteUserCard([FromBody] JObject objJson)
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

                UserCardInfo objUserCardInfo = _appDbContext.UserCardInfo
                                .Where(uci => uci.Id == Convert.ToInt32(objJson.SelectToken("usercardinfoid")) && uci.UserId == strUserId)
                                .FirstOrDefault();

                if (objUserCardInfo == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Card info doesn’t exist or Invalid User." }));
                }
                else
                {
                    _appDbContext.UserCardInfo.Remove(objUserCardInfo);
                    int returnVal = await _appDbContext.SaveChangesAsync();
                    if (returnVal > 0)
                    {
                        return Ok(new { Status = "OK", Detail = "Card Info Deleted." });
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/usercalendarevent/GetUserCardInfoByUserId
        [HttpPost]
        [Route("GetUserCardInfoByUserId")]
        public ActionResult GetUserCardInfoByUserId()
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

                clsAppCrypt objclsAppCrypt = new clsAppCrypt(configuration);

                var objUserCardInfo = from uci in _appDbContext.UserCardInfo
                                      where uci.UserId == strUserId
                                      select new
                                      {
                                          Id = uci.Id,
                                          UserId = uci.UserId,
                                          NameOnCard = uci.NameOnCard,
                                          CardNumber = objclsAppCrypt.DecryptTripleDES(uci.CardNumber),
                                          ExpiryDate = uci.ExpiryDate,
                                          CardType = uci.CardType,
                                          Issuer = uci.Issuer,
                                          SecurityNumber = objclsAppCrypt.DecryptTripleDES(uci.SecurityNumber),
                                          AddedDate = uci.AddedDate,
                                          LastUpdatedDate = uci.LastUpdatedDate
                                      };

                if (objUserCardInfo == null || objUserCardInfo.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Card info doesn’t exist." }));
                }

                return Ok(new { Status = "OK", UserCardInfo = objUserCardInfo });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/usercalendarevent/CheckCardExpiry
        [HttpPost]
        [Route("CheckCardExpiry")]
        public ActionResult CheckCardExpiry([FromBody] JObject objJson)
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

                clsAppCrypt objclsAppCrypt = new clsAppCrypt(configuration);

                var objUserCardInfo = _appDbContext.UserCardInfo
                                .Where(uci => uci.UserId == strUserId && objclsAppCrypt.DecryptTripleDES(uci.CardNumber).EndsWith(Convert.ToString(objJson.SelectToken("cardlast4digit"))))
                                .OrderByDescending(t => t.Id).FirstOrDefault();

                if (objUserCardInfo == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Card info doesn’t exist." }));
                }

                string[] strExpDateArr = objUserCardInfo.ExpiryDate.Split("-");
                if (strExpDateArr.Count() > 0)
                {
                    DateTime dtCardExpDate = new DateTime(CultureInfo.CurrentCulture.Calendar.ToFourDigitYear(Convert.ToInt32(strExpDateArr[1])), Convert.ToInt32(strExpDateArr[0]), 1);
                    if (dtCardExpDate.AddMonths(1) <= DateTime.UtcNow)
                    {
                        return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Your card is expired." }));
                    }
                    else
                    {
                        objUserCardInfo.CardNumber = objclsAppCrypt.DecryptTripleDES(objUserCardInfo.CardNumber);
                        objUserCardInfo.SecurityNumber = objclsAppCrypt.DecryptTripleDES(objUserCardInfo.SecurityNumber);
                        return Ok(new { Status = "OK", UserCardInfo = objUserCardInfo });
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

        // POST api/usercalendarevent/NotifyUserOnCardExpiry
        [HttpPost]
        [Route("NotifyUserOnCardExpiry")]
        public ActionResult NotifyUserOnCardExpiry([FromBody] JObject objJson)
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

                var objUserCardInfo = _appDbContext.UserCardInfo
                                .Where(uci => uci.UserId == strUserId).ToList<UserCardInfo>();

                if (objUserCardInfo.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Card info doesn’t exist." }));
                }

                clsAppCrypt objclsAppCrypt = new clsAppCrypt(configuration);

                List<UserCardInfo> lstExpiredCards = new List<UserCardInfo>();
                foreach (UserCardInfo objItem in objUserCardInfo)
                {
                    string[] strExpDateArr = objItem.ExpiryDate.Split("-");
                    if (strExpDateArr.Count() > 0)
                    {
                        int lastDayOfMonth = DateTime.DaysInMonth(CultureInfo.CurrentCulture.Calendar.ToFourDigitYear(Convert.ToInt32(strExpDateArr[1])), Convert.ToInt32(strExpDateArr[0]));
                        DateTime dtCardExpDate = new DateTime(CultureInfo.CurrentCulture.Calendar.ToFourDigitYear(Convert.ToInt32(strExpDateArr[1])), Convert.ToInt32(strExpDateArr[0]), lastDayOfMonth);
                        if (dtCardExpDate.AddDays(-Convert.ToInt32(objJson.SelectToken("remainingdays"))) < DateTime.UtcNow)
                        {
                            objItem.CardNumber = objclsAppCrypt.DecryptTripleDES(objItem.CardNumber);
                            objItem.SecurityNumber = objclsAppCrypt.DecryptTripleDES(objItem.SecurityNumber);
                            lstExpiredCards.Add(objItem);
                            //return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Your card is expired within " + Convert.ToString(objJson.SelectToken("remainingdays")) + " days." }));
                        }
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                    }
                }

                return Ok(new { Status = "OK", UserExpiredCardInfo = lstExpiredCards });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }
    }
}