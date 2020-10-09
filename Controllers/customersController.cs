using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NurchureAPI.Models;
using System.Net.Http;
using System.Web.Http.ModelBinding;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Newtonsoft.Json;
using System.Web;

namespace NurchureAPI.Controllers
{
    //[System.Web.Http.Route("api/customers")]
    [Route("api/[controller]")]
    [ApiController]
    public class customersController : ControllerBase
    {
        private AppDbContext _appDbContext;
        private IConfiguration configuration;

        public customersController(AppDbContext context, IConfiguration iConfig)
        {
            _appDbContext = context;
            configuration = iConfig;
        }

        // POST api/customers/userregister
        [HttpPost]
        [Route("userregister")]
        public async Task<ActionResult> userregister([FromBody] JObject objJson)
        {
            try
            {
                //LogError("userregister start");
                //LogError(Convert.ToString(objJson.SelectToken("username")));

                if (string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("username"))))
                {
                    //LogError("Phone Number Missing.");
                    return BadRequest(new { Status = "Error", Error = "Phone Number Missing." });
                }

                int intRecCount = _appDbContext.UserLoginAccount.Where(t => t.PhoneNumber == Convert.ToString(objJson.SelectToken("username"))).Count();
                if (intRecCount > 0)
                {
                    //LogError("Account already exists");
                    return BadRequest(new { Status = "Error", Error = "Account already exists" });
                }

                List<UserSMSInvitationDetails> objUserSMSInvitationDetails = new List<UserSMSInvitationDetails>();
                if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("invitationcode"))))
                {
                    objUserSMSInvitationDetails = (from upi in _appDbContext.UserSMSInvitationDetails
                                                   where (upi.ReceiverPhoneNumber == Convert.ToString(objJson.SelectToken("username")))
                                                   orderby upi.Id descending
                                                   select upi).Take(1).AsNoTracking().ToList<UserSMSInvitationDetails>();
                    if (objUserSMSInvitationDetails == null || objUserSMSInvitationDetails.Count() <= 0)
                    {
                        //LogError(Convert.ToString(objJson.SelectToken("invitationcode")) + " " + "Invalid invitation code.");
                        return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid invitation code." }));
                    }

                    if (objUserSMSInvitationDetails[0].InvitationCode != Convert.ToString(objJson.SelectToken("invitationcode")))
                    {
                        //LogError(Convert.ToString(objJson.SelectToken("invitationcode")) + " " + "Invalid invitation code.");
                        return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid invitation code." }));
                    }

                    if (DateTime.UtcNow > Convert.ToDateTime(objUserSMSInvitationDetails[0].ExpiryTime))
                    {
                        //LogError(Convert.ToString(objJson.SelectToken("invitationcode")) + " " + "Invitation code has expired.");
                        return StatusCode(StatusCodes.Status402PaymentRequired, (new { Status = "Error", Error = "Invitation code has expired." }));
                    }
                }

                //Regex regex = new Regex(@"(?=^.{8,}$)(?=.*\d)(?=.*[a-z])(?=.*[!@#$%^&*()_+}{:;'?>.<,])(?!.*\s).*$");
                Regex regex = new Regex(@"(?=^.{8,15}$)(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[!@#$%^&*()_+}{:;'?>.<,])(?!.*\s).*$");
                Match match = regex.Match(Convert.ToString(objJson.SelectToken("password")));

                if (!match.Success)
                {
                    //LogError(Convert.ToString(objJson.SelectToken("password")) + " " + "Password must contain 8-15 alpha numeric characters, including one UPPER case, one LOWER case and one special character.");
                    return BadRequest(new { Status = "Error", Error = "Password must contain 8-15 alpha numeric characters, including one UPPER case, one LOWER case and one special character." });
                }

                match = regex.Match(Convert.ToString(objJson.SelectToken("confirmpassword")));

                if (!match.Success)
                {
                    //LogError(Convert.ToString(objJson.SelectToken("confirmpassword")) + " " + "Confirm Password must contain 8-15 alpha numeric characters, including one UPPER case, one LOWER case and one special character.");
                    return BadRequest(new { Status = "Error", Error = "Confirm Password must contain 8-15 alpha numeric characters, including one UPPER case, one LOWER case and one special character." });
                }

                if (Convert.ToString(objJson.SelectToken("password")) != Convert.ToString(objJson.SelectToken("confirmpassword")))
                {
                    //LogError(Convert.ToString(objJson.SelectToken("password")) + " " + Convert.ToString(objJson.SelectToken("confirmpassword")) + " " + "Please make sure that Password and Confirm Password matches.");
                    return BadRequest(new { Status = "Error", Error = "Please make sure that Password and Confirm Password matches." });
                }

                //Create UserID
                string strUserId = "";
                //List<UserLoginAccount> objUserList = _appDbContext.UserLoginAccount.ToList();
                var objUserList = _appDbContext.UserLoginAccount.Select(t => new { UserIdVal = t.UserId, CreatedDateVal = t.CreatedDate }).OrderByDescending(p => p.CreatedDateVal).FirstOrDefault();
                //UserLoginAccount objUserList = from tbl in _appDbContext.UserLoginAccount select new { UserId = Convert.ToString(tbl.UserId), CreatedDate = Convert.ToDateTime(tbl.CreatedDate) };
                if (objUserList != null)
                {
                    string[] spearator = { "NIC" };
                    string[] strlist = objUserList.UserIdVal.Split(spearator, StringSplitOptions.None);
                    if (strlist.Length > 1)
                    {
                        strUserId = "NIC" + Convert.ToString(Convert.ToInt32(strlist[1]) + 1).PadLeft(9, '0');
                    }
                    else
                    {
                        strUserId = "NIC000000001";
                    }
                }
                else
                {
                    strUserId = "NIC000000001";
                }

                UserLoginAccount objUserLoginAccount = new UserLoginAccount();
                objUserLoginAccount.UserId = strUserId;
                objUserLoginAccount.HashId = Guid.NewGuid();
                objUserLoginAccount.Email = ""; //Convert.ToString(objJson.SelectToken("username"));
                objUserLoginAccount.PhoneNumber = Convert.ToString(objJson.SelectToken("username"));
                objUserLoginAccount.PasswordHash = clsCommon.GetHashedValue(Convert.ToString(objJson.SelectToken("password")));
                objUserLoginAccount.EmailConfirmed = false;
                objUserLoginAccount.PhoneNumberConfirmed = false;
                objUserLoginAccount.UserName = Convert.ToString(objJson.SelectToken("username"));
                objUserLoginAccount.TwoFactorEnabled = false;
                objUserLoginAccount.CreatedDate = DateTime.UtcNow;
                objUserLoginAccount.LastUpdated = DateTime.UtcNow;
                await _appDbContext.UserLoginAccount.AddAsync(objUserLoginAccount);
                //await _appDbContext.SaveChangesAsync();
                string UserName = Convert.ToString(objJson.SelectToken("name"));
                var names = UserName.Split(' ');
                string firstName = names[0];
                string lastName = "";

                if (names.Count() > 1)
                {
                    lastName = names[1];
                }

                Users objUsers = new Users();
                objUsers.UserId = strUserId;
                objUsers.FirstName = firstName;
                objUsers.LastName = lastName;
                objUsers.Email = "";
                objUsers.PhoneNumber = Convert.ToString(objJson.SelectToken("username"));
                objUsers.LoginIdentityId = "0";
                objUsers.GeoLocationLongitude = 0;
                objUsers.GeoLocationLatitude = 0;
                objUsers.GenderId = 1;
                objUsers.StateProvinceId = 1;
                objUsers.CountryId = 1;
                await _appDbContext.Users.AddAsync(objUsers);

                int returnVal = await _appDbContext.SaveChangesAsync();

                //LogError("Data saved in UserLoginAccount & Users table");

                if (returnVal > 0)
                {
                    if (objUserSMSInvitationDetails.Count() > 0)
                    {
                        UserChatGroupFriends objUserChatGroupFriends = new UserChatGroupFriends();
                        objUserChatGroupFriends.GroupId = objUserSMSInvitationDetails[0].UserChatGroupId;
                        objUserChatGroupFriends.AddedUserId = objUserSMSInvitationDetails[0].SenderUserId;
                        objUserChatGroupFriends.UserId = strUserId;
                        objUserChatGroupFriends.AddedDate = DateTime.UtcNow;
                        objUserChatGroupFriends.AdminRights = false;
                        await _appDbContext.UserChatGroupFriends.AddAsync(objUserChatGroupFriends);
                        returnVal = await _appDbContext.SaveChangesAsync();
                        //LogError("Data saved in UserChatGroupFriends table");
                    }
                    // string strVerificationCode = clsCommon.GetRandomAlphaNumeric();
                    string strVerificationCode = clsCommon.GetRandomNumeric();

                    UserVerificationDetails objUserVerificationDetails = new UserVerificationDetails();
                    objUserVerificationDetails.UserId = strUserId;
                    objUserVerificationDetails.VerificationCode = strVerificationCode;
                    objUserVerificationDetails.CreationTime = DateTime.UtcNow;
                    objUserVerificationDetails.ExpiryTime = DateTime.UtcNow.AddMinutes(30);
                    objUserVerificationDetails.RequestValue = "register";
                    await _appDbContext.UserVerificationDetails.AddAsync(objUserVerificationDetails);
                    returnVal = await _appDbContext.SaveChangesAsync();
                    //LogError("Data saved in UserVerificationDetails table");

                    if (returnVal > 0)
                    {
                        string strMessage = "Nurchure App : Your verification code for registration is " + strVerificationCode;
                        string strSMSStatus = new clsSMS(configuration).SendSMS(Convert.ToString(objJson.SelectToken("username")), strMessage);
                        //string strMailStatus = SendVerificationEmail_Register(Convert.ToString(objJson.SelectToken("username")), strVerificationCode);
                        if (strSMSStatus == "Success")
                        {
                            //LogError("An SMS with verification code has been sent to " + Convert.ToString(objJson.SelectToken("username")) + ". Please confirm the account with the verification code.");
                            return Ok(new { Status = "OK", Description = "An SMS with verification code has been sent to " + Convert.ToString(objJson.SelectToken("username")) + ". Please confirm the account with the verification code." });
                        }
                        else
                        {
                            //LogError("Error to send verification SMS to " + Convert.ToString(objJson.SelectToken("username")) + ". Please contact customer support.");
                            return BadRequest(new { Status = "OK", Description = "Error to send verification SMS to " + Convert.ToString(objJson.SelectToken("username")) + ". Please contact customer support." });
                        }
                    }
                    else
                    {
                        //LogError("Invalid phone number. Please enter valid number to signup.");
                        return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Invalid phone number. Please enter valid number to signup." }));
                    }
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }
            }
            catch (Exception ex)
            {
                //LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/customers/userlogin
        [HttpPost]
        [Route("userlogin")]
        public async Task<ActionResult> userlogin([FromBody] JObject objJson)
        {
            try
            {
                //LogError("userlogin start");
                //LogError(Convert.ToString(objJson.SelectToken("username")));

                //int intRecCount = _appDbContext.UserLoginAccount.Where(t => t.Email == Convert.ToString(objJson.SelectToken("username"))).Count();
                //Create PasswordHash
                string hashedPassword = clsCommon.GetHashedValue(Convert.ToString(objJson.SelectToken("password")));

                var objUserList = _appDbContext.UserLoginAccount
                                .Select(t => new { UserIdVal = t.UserId, EmailVal = t.Email, PhoneNumberVal = t.PhoneNumber, PasswordHashVal = t.PasswordHash, PhoneNumberConfirmedVal = t.PhoneNumberConfirmed })
                                .Where(s => s.PhoneNumberVal == Convert.ToString(objJson.SelectToken("username")) && s.PasswordHashVal == hashedPassword)
                                .FirstOrDefault();
                if (objUserList == null)
                {
                    //LogError("Please provide valid username or password");
                    return BadRequest(new { Status = "Error", Error = "Please provide valid username or password" });
                }

                if (objUserList.PhoneNumberConfirmedVal == false)
                {
                    //LogError("Account has not been verified. Please verify account before login.");
                    return StatusCode(StatusCodes.Status405MethodNotAllowed, (new { Status = "Error", Error = "Account has not been verified. Please verify account before login." }));
                }

                UserLoginAccount objUserLoginAccount = new UserLoginAccount();
                objUserLoginAccount.UserId = objUserList.UserIdVal;
                objUserLoginAccount.LoggedIn = true;
                objUserLoginAccount.LastUpdated = DateTime.UtcNow;
                _appDbContext.UserLoginAccount.Attach(objUserLoginAccount);
                _appDbContext.Entry(objUserLoginAccount).Property("LoggedIn").IsModified = true;
                _appDbContext.Entry(objUserLoginAccount).Property("LastUpdated").IsModified = true;
                //_appDbContext.Attach(objUserLoginAccount).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                int returnVal = await _appDbContext.SaveChangesAsync();

                //LogError("Data saved in UserLoginAccount table");

                if (returnVal > 0)
                {
                    UserLoginSessionKeys objSessionKeysList = _appDbContext.UserLoginSessionKeys
                                .Where(s => s.UserId == Convert.ToString(objUserList.UserIdVal))
                                .FirstOrDefault();

                    if (objSessionKeysList != null)
                    {
                        _appDbContext.UserLoginSessionKeys.Remove(objSessionKeysList);
                        await _appDbContext.SaveChangesAsync();
                    }

                    UserLoginSessionKeys objUserLoginSessionKeys = new UserLoginSessionKeys();
                    objUserLoginSessionKeys.UserId = objUserList.UserIdVal;
                    objUserLoginSessionKeys.ApiKeys = Guid.NewGuid();
                    objUserLoginSessionKeys.CreatedDate = DateTime.UtcNow;
                    await _appDbContext.UserLoginSessionKeys.AddAsync(objUserLoginSessionKeys);
                    returnVal = await _appDbContext.SaveChangesAsync();
                    //LogError("Data saved in UserLoginSessionKeys table");

                    if (returnVal > 0)
                    {
                        //LogError(Convert.ToString(objUserList.PhoneNumberVal) + " has successfully logged in");
                        return Ok(new { Status = "OK", api_key = Convert.ToString(objUserLoginSessionKeys.ApiKeys), Description = Convert.ToString(objUserList.PhoneNumberVal) + " has successfully logged in" });
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
                //LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/customers/sendverificationemail
        [HttpPost]
        [Route("sendverificationemail")]
        public async Task<ActionResult> sendverificationemail([FromBody] JObject objJson)
        {
            try
            {
                //LogError("sendverificationemail start");
                //LogError(Convert.ToString(objJson.SelectToken("username")));
                //int intRecCount = _appDbContext.UserLoginAccount.Where(t => t.Email == Convert.ToString(objJson.SelectToken("username"))).Count();
                var objUserList = _appDbContext.UserLoginAccount.Select(t => new { UserIdVal = t.UserId, EmailVal = t.Email }).Where(s => s.EmailVal == Convert.ToString(objJson.SelectToken("username"))).FirstOrDefault();
                if (objUserList == null)
                {
                    //LogError("Username doesn't exists");
                    return BadRequest(new { Status = "Error", Error = "Username doesn't exists" });
                }

                string strVerificationCode = clsCommon.GetRandomAlphaNumeric();

                UserVerificationDetails objUserVerificationDetails = new UserVerificationDetails();
                objUserVerificationDetails.UserId = objUserList.UserIdVal;
                objUserVerificationDetails.VerificationCode = strVerificationCode;
                objUserVerificationDetails.CreationTime = DateTime.UtcNow;
                objUserVerificationDetails.ExpiryTime = DateTime.UtcNow.AddMinutes(30);
                objUserVerificationDetails.RequestValue = Convert.ToString(objJson.SelectToken("requestvalue"));

                string strMailStatus = "";
                string strResponseDescription = "";
                if (Convert.ToString(objJson.SelectToken("requestvalue")) == "register")
                {
                    strMailStatus = SendVerificationEmail_Register(Convert.ToString(objJson.SelectToken("username")), strVerificationCode);
                    strResponseDescription = "Verification code has been sent to user email";
                }
                else if (Convert.ToString(objJson.SelectToken("requestvalue")) == "changepasswordanonymous")
                {
                    strMailStatus = SendVerificationEmail_ChangePasswordAnonymous(Convert.ToString(objJson.SelectToken("username")), strVerificationCode);
                    strResponseDescription = "A verification code for password change has been sent to " + Convert.ToString(objJson.SelectToken("username"));
                }

                if (strMailStatus == "Success")
                {
                    await _appDbContext.UserVerificationDetails.AddAsync(objUserVerificationDetails);
                    int returnVal = await _appDbContext.SaveChangesAsync();

                    if (returnVal > 0)
                    {
                        return Ok(new { Status = "OK", Description = strResponseDescription });
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

        // POST api/customers/sendverificationsms
        [HttpPost]
        [Route("sendverificationsms")]
        public async Task<ActionResult> sendverificationsms([FromBody] JObject objJson)
        {
            try
            {
                //int intRecCount = _appDbContext.UserLoginAccount.Where(t => t.Email == Convert.ToString(objJson.SelectToken("username"))).Count();
                var objUserList = _appDbContext.UserLoginAccount.Select(t => new { UserIdVal = t.UserId, EmailVal = t.Email, PhoneNumberVal = t.PhoneNumber }).Where(s => s.PhoneNumberVal == Convert.ToString(objJson.SelectToken("username"))).FirstOrDefault();
                if (objUserList == null)
                {
                    return BadRequest(new { Status = "Error", Error = "Phone Number doesn't exists" });
                }

                //string strVerificationCode = clsCommon.GetRandomAlphaNumeric();
                string strVerificationCode = clsCommon.GetRandomNumeric();

                UserVerificationDetails objUserVerificationDetails = new UserVerificationDetails();
                objUserVerificationDetails.UserId = objUserList.UserIdVal;
                objUserVerificationDetails.VerificationCode = strVerificationCode;
                objUserVerificationDetails.CreationTime = DateTime.UtcNow;
                objUserVerificationDetails.ExpiryTime = DateTime.UtcNow.AddMinutes(30);
                objUserVerificationDetails.RequestValue = Convert.ToString(objJson.SelectToken("requestvalue"));

                string strSMSStatus = "";
                string strResponseDescription = "";
                if (Convert.ToString(objJson.SelectToken("requestvalue")) == "register")
                {
                    //strMailStatus = SendVerificationEmail_Register(Convert.ToString(objJson.SelectToken("username")), strVerificationCode);
                    string strMessage = "Nurchure App : Your verification code for registration is " + strVerificationCode;
                    strSMSStatus = new clsSMS(configuration).SendSMS(Convert.ToString(objJson.SelectToken("username")), strMessage);
                    strResponseDescription = "Verification code has been sent to user phone number";
                }
                else if (Convert.ToString(objJson.SelectToken("requestvalue")) == "changepasswordanonymous")
                {
                    //strMailStatus = SendVerificationEmail_ChangePasswordAnonymous(Convert.ToString(objJson.SelectToken("phonenumber")), strVerificationCode);
                    string strMessage = "Nurchure App : Your verification code for change password is " + strVerificationCode;
                    strSMSStatus = new clsSMS(configuration).SendSMS(Convert.ToString(objJson.SelectToken("username")), strMessage);
                    strResponseDescription = "A verification code for password change has been sent to " + Convert.ToString(objJson.SelectToken("username"));
                }

                if (strSMSStatus == "Success")
                {
                    await _appDbContext.UserVerificationDetails.AddAsync(objUserVerificationDetails);
                    int returnVal = await _appDbContext.SaveChangesAsync();

                    if (returnVal > 0)
                    {
                        return Ok(new { Status = "OK", Description = strResponseDescription });
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                    }
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = strSMSStatus }));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/customers/verifyemailbyCode
        [HttpPost]
        [Route("verifyemailbyCode")]
        public async Task<ActionResult> verifyemailbyCode([FromBody] JObject objJson)
        {
            try
            {
                var objUserList = _appDbContext.UserLoginAccount
                                .Select(t => new { UserIdVal = t.UserId, EmailVal = t.Email })
                                .Where(s => s.EmailVal == Convert.ToString(objJson.SelectToken("username")))
                                .FirstOrDefault();

                if (objUserList == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid verification code or username" }));
                }

                var objUserVerificationDetails = _appDbContext.UserVerificationDetails
                                .Where(s => s.UserId == Convert.ToString(objUserList.UserIdVal) && s.RequestValue == "register")
                                .OrderByDescending(t => t.Id).FirstOrDefault();

                if (objUserVerificationDetails == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid verification code or username" }));
                }

                if (objUserVerificationDetails.VerificationCode != Convert.ToString(objJson.SelectToken("verificationcode")))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid verification code or username" }));
                }

                if (DateTime.UtcNow > Convert.ToDateTime(objUserVerificationDetails.ExpiryTime))
                {
                    return StatusCode(StatusCodes.Status402PaymentRequired, (new { Status = "Error", Error = "Verification code has expired" }));
                }

                UserLoginAccount objUserLoginAccount = new UserLoginAccount();
                objUserLoginAccount.UserId = objUserList.UserIdVal;
                objUserLoginAccount.EmailConfirmed = true;
                _appDbContext.UserLoginAccount.Attach(objUserLoginAccount);
                _appDbContext.Entry(objUserLoginAccount).Property("EmailConfirmed").IsModified = true;
                //_appDbContext.Attach(objUserLoginAccount).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                int returnVal = await _appDbContext.SaveChangesAsync();
                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Description = "User account " + Convert.ToString(objUserList.EmailVal) + " has been successfully verified" });
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

        // POST api/customers/verifyphonenumberbycode
        [HttpPost]
        [Route("verifyphonenumberbycode")]
        public async Task<ActionResult> verifyphonenumberbycode([FromBody] JObject objJson)
        {
            try
            {
                var objUserList = _appDbContext.UserLoginAccount
                                .Select(t => new { UserIdVal = t.UserId, EmailVal = t.Email, PhoneNumberVal = t.PhoneNumber })
                                .Where(s => s.PhoneNumberVal == Convert.ToString(objJson.SelectToken("username")))
                                .FirstOrDefault();

                if (objUserList == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid verification code or username" }));
                }

                var objUserVerificationDetails = _appDbContext.UserVerificationDetails
                                .Where(s => s.UserId == Convert.ToString(objUserList.UserIdVal) && s.RequestValue == "register")
                                .OrderByDescending(t => t.Id).FirstOrDefault();

                if (objUserVerificationDetails == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid verification code or username" }));
                }

                if (objUserVerificationDetails.VerificationCode != Convert.ToString(objJson.SelectToken("verificationcode")))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid verification code" }));
                }

                if (DateTime.UtcNow > Convert.ToDateTime(objUserVerificationDetails.ExpiryTime))
                {
                    return StatusCode(StatusCodes.Status402PaymentRequired, (new { Status = "Error", Error = "Verification code has expired" }));
                }

                UserLoginAccount objUserLoginAccount = new UserLoginAccount();
                objUserLoginAccount.UserId = objUserList.UserIdVal;
                objUserLoginAccount.PhoneNumberConfirmed = true;
                _appDbContext.UserLoginAccount.Attach(objUserLoginAccount);
                _appDbContext.Entry(objUserLoginAccount).Property("PhoneNumberConfirmed").IsModified = true;
                //_appDbContext.Attach(objUserLoginAccount).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    UserSMSInvitationDetails objUserSMSInvitationDetails = _appDbContext.UserSMSInvitationDetails
                                .Where(usid => usid.ReceiverPhoneNumber == Convert.ToString(objJson.SelectToken("username"))).AsNoTracking().FirstOrDefault();

                    if (objUserSMSInvitationDetails != null)
                    {
                        UserChatGroupFriends objUserChatGroupFriends = new UserChatGroupFriends();
                        objUserChatGroupFriends.GroupId = objUserSMSInvitationDetails.UserChatGroupId;
                        objUserChatGroupFriends.AddedUserId = objUserSMSInvitationDetails.SenderUserId;
                        objUserChatGroupFriends.UserId = objUserList.UserIdVal;
                        objUserChatGroupFriends.AddedDate = DateTime.UtcNow;
                        objUserChatGroupFriends.AdminRights = false;
                        objUserChatGroupFriends.AdminRightsAddedDate = DateTime.UtcNow;
                        await _appDbContext.UserChatGroupFriends.AddAsync(objUserChatGroupFriends);
                        await _appDbContext.SaveChangesAsync();
                    }
                    return Ok(new { Status = "OK", Description = "User account " + Convert.ToString(objUserList.PhoneNumberVal) + " has been successfully verified" });
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

        // POST api/customers/changepasswordanonymous
        [HttpPost]
        [Route("changepasswordanonymous")]
        public async Task<ActionResult> changepasswordanonymous([FromBody] JObject objJson)
        {
            try
            {
                var objUserList = _appDbContext.UserLoginAccount
                                .Select(t => new { UserIdVal = t.UserId, EmailVal = t.Email, PhoneNumberVal = t.PhoneNumber })
                                .Where(s => s.PhoneNumberVal == Convert.ToString(objJson.SelectToken("username")))
                                .FirstOrDefault();

                if (objUserList == null)
                {
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Please provide valid username or password" }));
                }

                var objUserVerificationDetails = _appDbContext.UserVerificationDetails
                                .Where(s => s.UserId == Convert.ToString(objUserList.UserIdVal) && s.RequestValue == "changepasswordanonymous")
                                .OrderByDescending(t => t.Id).FirstOrDefault();

                if (objUserVerificationDetails == null)
                {
                    return BadRequest((new { Status = "Error", Error = "Please provide valid username or password" }));
                }

                if (objUserVerificationDetails.VerificationCode != Convert.ToString(objJson.SelectToken("verificationcode")))
                {
                    return BadRequest((new { Status = "Error", Error = "Please provide valid verification code" }));
                }

                if (DateTime.UtcNow > Convert.ToDateTime(objUserVerificationDetails.ExpiryTime))
                {
                    return BadRequest((new { Status = "Error", Error = "Verification code has expired" }));
                }

                //Regex regex = new Regex(@"(?=^.{8,}$)(?=.*\d)(?=.*[a-z])(?=.*[!@#$%^&*()_+}{:;'?>.<,])(?!.*\s).*$");
                Regex regex = new Regex(@"(?=^.{8,15}$)(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[!@#$%^&*()_+}{:;'?>.<,])(?!.*\s).*$");
                Match match = regex.Match(Convert.ToString(objJson.SelectToken("newpassword")));

                if (!match.Success)
                {
                    return BadRequest(new { Status = "Error", Error = "Password must contain 8-15 alpha numeric characters, including one UPPER case, one LOWER case and one special character" });
                }

                match = regex.Match(Convert.ToString(objJson.SelectToken("confirmnewpassword")));
                if (!match.Success)
                {
                    return BadRequest(new { Status = "Error", Error = "Confirm Password must contain 8-15 alpha numeric characters, including one UPPER case, one LOWER case and one special character" });
                }

                if (Convert.ToString(objJson.SelectToken("newpassword")) != Convert.ToString(objJson.SelectToken("confirmnewpassword")))
                {
                    return BadRequest(new { Status = "Error", Error = "Please make sure that Password and Confirm Password matches" });
                }

                UserLoginAccount objUserLoginAccount = new UserLoginAccount();
                objUserLoginAccount.UserId = objUserList.UserIdVal;
                objUserLoginAccount.PasswordHash = clsCommon.GetHashedValue(Convert.ToString(objJson.SelectToken("newpassword")));
                _appDbContext.UserLoginAccount.Attach(objUserLoginAccount);
                _appDbContext.Entry(objUserLoginAccount).Property("PasswordHash").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();
                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Description = "Account password has been changed" });
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

        // POST api/customers/changepassword
        [HttpPost]
        [Route("changepassword")]
        public async Task<ActionResult> changepassword([FromBody] JObject objJson)
        {
            try
            {
                if (string.IsNullOrEmpty(Convert.ToString(Request.Headers["api_key"])))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, (new { Status = "Error", Error = "API Key is missing or invalid" }));
                }

                string strUserId = UserLoginSessionKeys.GetUserIdByApiKey(_appDbContext, Guid.Parse(Request.Headers["api_key"]));
                if (string.IsNullOrEmpty(strUserId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, (new { Status = "Error", Error = "API Key is missing or invalid" }));
                }

                var objUserList = _appDbContext.UserLoginAccount
                                .Where(s => s.UserId == strUserId)
                                .AsNoTracking().FirstOrDefault();

                if (objUserList == null)
                {
                    return BadRequest((new { Status = "Error", Error = "Please provide valid username or password" }));
                }

                if (objUserList.PasswordHash != clsCommon.GetHashedValue(Convert.ToString(objJson.SelectToken("oldpassword"))))
                {
                    return BadRequest((new { Status = "Error", Error = "Please provide valid username or password" }));
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

                //_appDbContext.UserLoginAccount.AsNoTracking();
                UserLoginAccount objUserLoginAccount = new UserLoginAccount();
                objUserLoginAccount.UserId = strUserId;
                objUserLoginAccount.PasswordHash = clsCommon.GetHashedValue(Convert.ToString(objJson.SelectToken("password")));
                _appDbContext.UserLoginAccount.Attach(objUserLoginAccount);
                _appDbContext.Entry(objUserLoginAccount).Property("PasswordHash").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();
                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Description = "Account password has been changed" });
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

        // POST api/customers/userfblogin
        [HttpPost]
        [Route("userfblogin")]
        public async Task<ActionResult> userfblogin([FromBody] JObject objJson)
        {
            try
            {
                if (string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("access_token"))))
                {
                    return BadRequest((new { Status = "Error", Error = "Access_Token is missing or invalid." }));
                }

                string strJson = GetFacebookUserJson(Convert.ToString(objJson.SelectToken("access_token")));
                FacebookUser oUser = JsonConvert.DeserializeObject<FacebookUser>(strJson);
                if (string.IsNullOrEmpty(Convert.ToString(oUser)))
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                string strEmail = Convert.ToString(oUser.email);
                var objUserList = _appDbContext.UserLoginAccount
                                .Select(t => new { UserIdVal = t.UserId, EmailVal = t.Email, PasswordHashVal = t.PasswordHash, EmailConfirmedVal = t.EmailConfirmed })
                                //.Where(s => s.EmailVal == Convert.ToString(oUser.email))
                                .Where(s => s.EmailVal == strEmail)
                                .AsNoTracking().FirstOrDefault();

                string strUserId = "";
                if (objUserList == null)
                {
                    //Register User
                    //string strJson = "{\"email\" : \""+ strEmail + "\", \"first_name\" : \"Test\", \"last_name\" : \"User\"}";
                    string strRegisterStatus = await RegisterExternalUser(JObject.Parse(strJson));
                    if (strRegisterStatus.Contains("Success#NIC"))
                    {
                        string[] spearator = { "#" };
                        string[] strlist = strRegisterStatus.Split(spearator, StringSplitOptions.None);
                        if (strlist.Length > 1)
                        {
                            strUserId = Convert.ToString(strlist[1]);
                        }
                        else
                        {
                            return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = strRegisterStatus }));
                        }
                    }
                    else if (strRegisterStatus == "Error")
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = strRegisterStatus }));
                    }
                }

                //Login User
                UserLoginAccount objUserLoginAccount = new UserLoginAccount();
                objUserLoginAccount.UserId = (strUserId != "" ? strUserId : objUserList.UserIdVal);
                objUserLoginAccount.LoggedIn = true;
                objUserLoginAccount.LastUpdated = DateTime.UtcNow;
                _appDbContext.UserLoginAccount.Attach(objUserLoginAccount);
                _appDbContext.Entry(objUserLoginAccount).Property("LoggedIn").IsModified = true;
                _appDbContext.Entry(objUserLoginAccount).Property("LastUpdated").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();
                if (returnVal > 0)
                {
                    UserLoginSessionKeys objSessionKeysList = _appDbContext.UserLoginSessionKeys
                                .Where(s => s.UserId == Convert.ToString((strUserId != "" ? strUserId : objUserList.UserIdVal)))
                                .FirstOrDefault();

                    if (objSessionKeysList != null)
                    {
                        _appDbContext.UserLoginSessionKeys.Remove(objSessionKeysList);
                        await _appDbContext.SaveChangesAsync();
                    }

                    UserLoginSessionKeys objUserLoginSessionKeys = new UserLoginSessionKeys();
                    objUserLoginSessionKeys.UserId = (strUserId != "" ? strUserId : objUserList.UserIdVal);
                    objUserLoginSessionKeys.ApiKeys = Guid.NewGuid();
                    objUserLoginSessionKeys.CreatedDate = DateTime.UtcNow;
                    await _appDbContext.UserLoginSessionKeys.AddAsync(objUserLoginSessionKeys);

                    if (objUserList == null)
                    {
                        ExternalAccountLogin objExternalAccountLogin = new ExternalAccountLogin();
                        //objExternalAccountLogin.LoginProvider = "Facebook";
                        objExternalAccountLogin.UserId = (strUserId != "" ? strUserId : objUserList.UserIdVal);
                        objExternalAccountLogin.ProviderKey = Convert.ToString(objJson.SelectToken("access_token"));
                        objExternalAccountLogin.ProviderDisplayName = "Facebook";
                        await _appDbContext.ExternalAccountLogin.AddAsync(objExternalAccountLogin);
                    }
                    returnVal = await _appDbContext.SaveChangesAsync();

                    if (returnVal > 0)
                    {
                        return Ok(new { Status = "OK", api_key = Convert.ToString(objUserLoginSessionKeys.ApiKeys), name = oUser.name, Email = strEmail, Description = strEmail + " has successfully logged in" });
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

        public string GetFacebookUserJson(string access_token)
        {
            try
            {
                string strUrl = String.Format("https://graph.facebook.com/me?access_token={0}&fields=email,name,first_name,last_name,link", access_token);
                WebClient objWebClient = new WebClient();
                Stream objData = objWebClient.OpenRead(strUrl);
                StreamReader objReader = new StreamReader(objData);
                string strResponseString = objReader.ReadToEnd();
                objData.Close();
                objReader.Close();
                return strResponseString;
            }
            catch (Exception ex)
            {
                return Convert.ToString(ex.Message);
            }
        }

        // POST api/customers/usergooglelogin
        [HttpPost]
        [Route("usergooglelogin")]
        public async Task<ActionResult> usergooglelogin([FromBody] JObject objJson)
        {
            try
            {
                if (string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("access_token"))))
                {
                    return BadRequest((new { Status = "Error", Error = "Access_Token is missing or invalid." }));
                }

                string strJson = GetGoogleUserJson(Convert.ToString(objJson.SelectToken("access_token")));

                if (string.IsNullOrEmpty(strJson))
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                JObject objJsonVal = JObject.Parse(strJson);
                string strEmail = Convert.ToString(objJsonVal.SelectToken("email"));

                var objUserList = _appDbContext.UserLoginAccount
                                .Select(t => new { UserIdVal = t.UserId, EmailVal = t.Email, PasswordHashVal = t.PasswordHash, EmailConfirmedVal = t.EmailConfirmed })
                                //.Where(s => s.EmailVal == Convert.ToString(oUser.email))
                                .Where(s => s.EmailVal == strEmail)
                                .AsNoTracking().FirstOrDefault();

                string strUserId = "";
                if (objUserList == null)
                {
                    //Register User
                    string strJsonVal = "{\"email\" : \"" + strEmail + "\", \"first_name\" : \"" + Convert.ToString(objJsonVal.SelectToken("given_name")) + "\", \"last_name\" : \"" + Convert.ToString(objJsonVal.SelectToken("family_name")) + "\"}";
                    string strRegisterStatus = await RegisterExternalUser(JObject.Parse(strJsonVal));
                    if (strRegisterStatus.Contains("Success#NIC"))
                    {
                        string[] spearator = { "#" };
                        string[] strlist = strRegisterStatus.Split(spearator, StringSplitOptions.None);
                        if (strlist.Length > 1)
                        {
                            strUserId = Convert.ToString(strlist[1]);
                        }
                        else
                        {
                            return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = strRegisterStatus }));
                        }
                    }
                    else if (strRegisterStatus == "Error")
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = strRegisterStatus }));
                    }
                }

                //Login User
                UserLoginAccount objUserLoginAccount = new UserLoginAccount();
                objUserLoginAccount.UserId = (strUserId != "" ? strUserId : objUserList.UserIdVal);
                objUserLoginAccount.LoggedIn = true;
                objUserLoginAccount.LastUpdated = DateTime.UtcNow;
                _appDbContext.UserLoginAccount.Attach(objUserLoginAccount);
                _appDbContext.Entry(objUserLoginAccount).Property("LoggedIn").IsModified = true;
                _appDbContext.Entry(objUserLoginAccount).Property("LastUpdated").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();
                if (returnVal > 0)
                {
                    UserLoginSessionKeys objSessionKeysList = _appDbContext.UserLoginSessionKeys
                                .Where(s => s.UserId == Convert.ToString((strUserId != "" ? strUserId : objUserList.UserIdVal)))
                                .FirstOrDefault();

                    if (objSessionKeysList != null)
                    {
                        _appDbContext.UserLoginSessionKeys.Remove(objSessionKeysList);
                        await _appDbContext.SaveChangesAsync();
                    }

                    UserLoginSessionKeys objUserLoginSessionKeys = new UserLoginSessionKeys();
                    objUserLoginSessionKeys.UserId = (strUserId != "" ? strUserId : objUserList.UserIdVal);
                    objUserLoginSessionKeys.ApiKeys = Guid.NewGuid();
                    objUserLoginSessionKeys.CreatedDate = DateTime.UtcNow;
                    await _appDbContext.UserLoginSessionKeys.AddAsync(objUserLoginSessionKeys);

                    if (objUserList == null)
                    {
                        ExternalAccountLogin objExternalAccountLogin = new ExternalAccountLogin();
                        //objExternalAccountLogin.LoginProvider = "Google";
                        objExternalAccountLogin.UserId = (strUserId != "" ? strUserId : objUserList.UserIdVal);
                        objExternalAccountLogin.ProviderKey = Convert.ToString(objJson.SelectToken("access_token"));
                        objExternalAccountLogin.ProviderDisplayName = "Google";
                        await _appDbContext.ExternalAccountLogin.AddAsync(objExternalAccountLogin);
                    }

                    returnVal = await _appDbContext.SaveChangesAsync();

                    if (returnVal > 0)
                    {
                        return Ok(new { Status = "OK", api_key = Convert.ToString(objUserLoginSessionKeys.ApiKeys), Email = strEmail, Description = strEmail + " has successfully logged in" });
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

        public string GetGoogleUserJson(string access_token)
        {
            try
            {
                string strUrl = String.Format("https://www.googleapis.com/oauth2/v3/tokeninfo?id_token={0}", access_token);
                //string strUrl = String.Format("https://www.googleapis.com/oauth2/v1/userinfo?alt=json&access_token={0}", access_token);
                WebClient objWebClient = new WebClient();
                Stream objData = objWebClient.OpenRead(strUrl);
                StreamReader objReader = new StreamReader(objData);
                string strResponseString = objReader.ReadToEnd();
                objData.Close();
                objReader.Close();
                return strResponseString;
            }
            catch (Exception ex)
            {
                return Convert.ToString(ex.Message);
            }
        }

        public async Task<string> RegisterExternalUser([FromBody] JObject objJson)
        {
            try
            {
                string connectionString = Convert.ToString(configuration.GetSection("ConnectionStrings").GetSection("DefaultConnection").Value);
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                optionsBuilder.UseSqlServer(connectionString);
                AppDbContext dbContext = new AppDbContext(optionsBuilder.Options);

                //Create UserID
                string strUserId = "";
                var objUserData = dbContext.UserLoginAccount.Select
                                 (t => new { UserIdVal = t.UserId, CreatedDateVal = t.CreatedDate })
                                 .OrderByDescending(p => p.CreatedDateVal)
                                 .AsNoTracking().FirstOrDefault();

                if (objUserData != null)
                {
                    string[] spearator = { "NIC" };
                    string[] strlist = objUserData.UserIdVal.Split(spearator, StringSplitOptions.None);
                    if (strlist.Length > 1)
                    {
                        strUserId = "NIC" + Convert.ToString(Convert.ToInt32(strlist[1]) + 1).PadLeft(9, '0');
                    }
                    else
                    {
                        strUserId = "NIC000000001";
                    }
                }
                else
                {
                    strUserId = "NIC000000001";
                }

                UserLoginAccount objUserLoginAccount = new UserLoginAccount();
                objUserLoginAccount.UserId = strUserId;
                objUserLoginAccount.HashId = Guid.NewGuid();
                objUserLoginAccount.Email = Convert.ToString(objJson.SelectToken("email"));
                objUserLoginAccount.PasswordHash = "";
                objUserLoginAccount.EmailConfirmed = true;
                objUserLoginAccount.PhoneNumber = "";
                objUserLoginAccount.PhoneNumberConfirmed = false;
                objUserLoginAccount.UserName = Convert.ToString(objJson.SelectToken("email"));
                objUserLoginAccount.TwoFactorEnabled = false;
                objUserLoginAccount.CreatedDate = DateTime.UtcNow;
                objUserLoginAccount.LastUpdated = DateTime.UtcNow;
                await dbContext.UserLoginAccount.AddAsync(objUserLoginAccount);
                //await _appDbContext.SaveChangesAsync();

                Users objUsers = new Users();
                objUsers.UserId = strUserId;
                objUsers.FirstName = Convert.ToString(objJson.SelectToken("first_name"));
                objUsers.LastName = Convert.ToString(objJson.SelectToken("last_name"));
                objUsers.Email = Convert.ToString(objJson.SelectToken("email"));
                objUsers.LoginIdentityId = "0";
                objUsers.GeoLocationLongitude = 0;
                objUsers.GeoLocationLatitude = 0;
                objUsers.GenderId = 1;
                objUsers.StateProvinceId = 1;
                objUsers.CountryId = 1;
                await dbContext.Users.AddAsync(objUsers);

                int returnVal = await dbContext.SaveChangesAsync();
                if (returnVal > 0)
                {
                    return "Success" + "#" + strUserId;
                }
                else
                {
                    return "Error";
                }
            }
            catch (Exception ex)
            {
                return Convert.ToString(ex.Message);
            }
        }

        // POST api/customers/GetExternalUserProfileByUsername
        [HttpPost]
        [Route("GetExternalUserProfileByUsername")]
        public ActionResult GetExternalUserProfileByUsername([FromBody] JObject objJson)
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

                var objUserDetails = from u in _appDbContext.Users
                                     join g in _appDbContext.Genders on u.GenderId equals g.Id into DetailsGenders
                                     from g1 in DetailsGenders.DefaultIfEmpty()
                                     join c in _appDbContext.Countries on u.CountryId equals c.Id into DetailsCountries
                                     from c1 in DetailsCountries.DefaultIfEmpty()
                                     where (u.PhoneNumber == Convert.ToString(objJson.SelectToken("username")))
                                     select new
                                     {
                                         FirstName = u.FirstName,
                                         LastName = u.LastName,
                                         Gender = g1.Name,
                                         Age = Convert.ToString(u.Age),
                                         City = Convert.ToString(u.CityId),
                                         Country = c1.Name,
                                         ProfileDescription = u.Desciption,
                                         ProfileTagLine = u.TagLine,
                                         Likes = u.UserFavourite,
                                         CurrentlyOnline = ""
                                     };

                if (objUserDetails == null || objUserDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid username" }));
                }

                return Ok(new { Status = "OK", User = objUserDetails });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/customers/GetUserProfileInfo
        [HttpPost]
        [Route("GetUserProfileInfo")]
        public ActionResult GetUserProfileInfo([FromBody] JObject objJson)
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

                var objUserDetails = from u in _appDbContext.Users
                                     join g in _appDbContext.Genders on u.GenderId equals g.Id into DetailsGenders
                                     from g1 in DetailsGenders.DefaultIfEmpty()
                                     join c in _appDbContext.Countries on u.CountryId equals c.Id into DetailsCountries
                                     from c1 in DetailsCountries.DefaultIfEmpty()
                                     join s in _appDbContext.StateProvinces on u.StateProvinceId equals s.Id into DetailsStateProvinces
                                     from s1 in DetailsStateProvinces.DefaultIfEmpty()
                                     join ula in _appDbContext.UserLoginAccount on u.UserId equals ula.UserId into DetailsUserLoginAccount
                                     from ula1 in DetailsUserLoginAccount.DefaultIfEmpty()
                                     where (u.PhoneNumber == Convert.ToString(objJson.SelectToken("username"))
                                     || u.Email == Convert.ToString(objJson.SelectToken("username")))
                                     select new
                                     {
                                         UserName = u.PhoneNumber,
                                         UserId = u.UserId,
                                         FirstName = u.FirstName,
                                         LastName = u.LastName,
                                         Gender = g1.Name,
                                         Age = u.Age,
                                         //Email = u.Email,
                                         DOB = u.DOB,
                                         PhoneNumber = u.PhoneNumber,
                                         Email = ula1.Email,
                                         Address = new
                                         {
                                             AddressLine1 = u.AddressLine1,
                                             AddressLine2 = u.AddressLine2,
                                             City = Convert.ToString(u.CityId),
                                             StateProvince = s1.Name,
                                             Country = c1.Name,
                                             ZipPostalCode = u.ZipPostalCode
                                         },
                                         ProfileDescription = u.Desciption,
                                         ProfileTagLine = u.TagLine,
                                         Likes = u.UserFavourite,
                                         CurrentlyOnline = ""
                                     };

                int intRecCount = _appDbContext.UserPersonalityQuestionaireDetails.Where(t => t.UserId == strUserId).Count();

                int isAnswered = 0;

                if (intRecCount > 0)
                    isAnswered = 1;

                UserSMSInvitationDetails objUserSMSInvitationDetails = _appDbContext.UserSMSInvitationDetails
                                .Where(usid => usid.ReceiverPhoneNumber == Convert.ToString(objJson.SelectToken("username"))).AsNoTracking().FirstOrDefault();

                if (objUserDetails == null || objUserDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid username" }));
                }

                if (objUserSMSInvitationDetails == null)
                {
                    return Ok(new { Status = "OK", User = objUserDetails, isAnswered = isAnswered, GroupId = 0 });
                }
                else
                {
                    return Ok(new { Status = "OK", User = objUserDetails, isAnswered = isAnswered, GroupId = objUserSMSInvitationDetails.UserChatGroupId });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/customers/UpdateUserProfileInfo
        [HttpPost]
        [Route("UpdateUserProfileInfo")]
        public async Task<ActionResult> UpdateUserProfileInfo([FromBody] JObject objJson)
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

                Users objUsers = new Users();
                objUsers.UserId = strUserId;
                string UserName = Convert.ToString(objJson.SelectToken("Name"));
                var names = UserName.Split(' ');
                string firstName = names[0];
                string lastName = "";

                if (names.Count() > 1)
                {
                    lastName = names[1];
                }

                objUsers.FirstName = firstName;
                objUsers.LastName = lastName;
                objUsers.PhoneNumber = Convert.ToString(objJson.SelectToken("PhoneNumber"));
                objUsers.DOB = Convert.ToString(objJson.SelectToken("DOB"));
                objUsers.GenderId = Convert.ToInt32(objJson.SelectToken("Gender"));

                //objUsers.AddressLine1 = Convert.ToString(objJson.SelectToken("AddressLine1"));
                //objUsers.AddressLine2 = Convert.ToString(objJson.SelectToken("AddressLine2"));
                //objUsers.CityId = Convert.ToInt32(objJson.SelectToken("City"));
                //objUsers.StateProvinceId = Convert.ToInt32(objJson.SelectToken("StateProvince"));
                //objUsers.CountryId = Convert.ToInt32(objJson.SelectToken("Country"));
                //objUsers.ZipPostalCode = Convert.ToString(objJson.SelectToken("ZipPostalCode"));
                //objUsers.Desciption = Convert.ToString(objJson.SelectToken("ProfileDescription"));
                //objUsers.TagLine = Convert.ToString(objJson.SelectToken("ProfileTagLine"));
                //objUsers.UserFavourite = Convert.ToString(objJson.SelectToken("Likes"));

                if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("DOB"))))
                {
                    DateTime dt = DateTime.ParseExact(Convert.ToString(objJson.SelectToken("DOB")), "dd-MM-yyyy", null);
                    int age = DateTime.UtcNow.Year - dt.Year;
                    objUsers.Age = age;
                }

                objUsers.LastUpdated = DateTime.UtcNow;

                _appDbContext.Users.Attach(objUsers);

                UserLoginAccount objUserLoginAccount = new UserLoginAccount();
                objUserLoginAccount.UserId = strUserId;

                if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("Email"))))
                    objUserLoginAccount.Email = Convert.ToString(objJson.SelectToken("Email"));

                objUserLoginAccount.LastUpdated = DateTime.UtcNow;
                _appDbContext.UserLoginAccount.Attach(objUserLoginAccount);

                //Get User Latitude & Longitude by address
                //string strGoogleMapApiKey = Convert.ToString(configuration.GetSection("appSettings").GetSection("GoogleMapApiKey").Value);
                //string strRequestUrl = "https://maps.googleapis.com/maps/api/geocode/json?address=" + HttpUtility.UrlEncode(Convert.ToString(objJson.SelectToken("AddressLine1"))) + ",+" + HttpUtility.UrlEncode(Convert.ToString(objJson.SelectToken("AddressLine2"))) + "&key=" + strGoogleMapApiKey;
                //WebRequest objWebRequest = WebRequest.Create(strRequestUrl);
                //objWebRequest.Method = "GET";
                ////objWebRequest.ContentType = "application/json";
                //WebResponse objWebResponse = objWebRequest.GetResponse();
                //Stream dataStream = objWebResponse.GetResponseStream();
                //StreamReader dataReader = new StreamReader(dataStream);
                //string strResponse = dataReader.ReadToEnd();
                //dataReader.Close();
                //dataStream.Close();
                //objWebResponse.Close();
                //if (!string.IsNullOrEmpty(strResponse))
                //{
                //    JObject objResponseJson = JObject.Parse(strResponse);
                //    if (objResponseJson.SelectToken("results").Count() > 0)
                //    {
                //        if (!String.IsNullOrEmpty(Convert.ToString(objResponseJson.SelectToken("results").FirstOrDefault().SelectToken("geometry"))))
                //        {
                //            if (!string.IsNullOrEmpty(Convert.ToString(objResponseJson.SelectToken("results").FirstOrDefault().SelectToken("geometry").SelectToken("location").SelectToken("lat"))))
                //            {
                //                objUsers.GeoLocationLatitude = Convert.ToDouble(objResponseJson.SelectToken("results").FirstOrDefault().SelectToken("geometry").SelectToken("location").SelectToken("lat"));
                //                _appDbContext.Entry(objUsers).Property("GeoLocationLatitude").IsModified = true;
                //            }

                //            if (!string.IsNullOrEmpty(Convert.ToString(objResponseJson.SelectToken("results").FirstOrDefault().SelectToken("geometry").SelectToken("location").SelectToken("lng"))))
                //            {
                //                objUsers.GeoLocationLongitude = Convert.ToDouble(objResponseJson.SelectToken("results").FirstOrDefault().SelectToken("geometry").SelectToken("location").SelectToken("lng"));
                //                _appDbContext.Entry(objUsers).Property("GeoLocationLongitude").IsModified = true;
                //            }
                //        }
                //    }
                //}

                _appDbContext.Entry(objUsers).Property("FirstName").IsModified = true;
                _appDbContext.Entry(objUsers).Property("LastName").IsModified = true;
                _appDbContext.Entry(objUsers).Property("GenderId").IsModified = true;
                _appDbContext.Entry(objUsers).Property("DOB").IsModified = true;
                //_appDbContext.Entry(objUsers).Property("Age").IsModified = true;
                //_appDbContext.Entry(objUsers).Property("PhoneNumber").IsModified = true;
                //_appDbContext.Entry(objUsers).Property("Email").IsModified = true;
                //_appDbContext.Entry(objUsers).Property("AddressLine1").IsModified = true;
                //_appDbContext.Entry(objUsers).Property("AddressLine2").IsModified = true;
                //_appDbContext.Entry(objUsers).Property("CityId").IsModified = true;
                //_appDbContext.Entry(objUsers).Property("StateProvinceId").IsModified = true;
                //_appDbContext.Entry(objUsers).Property("CountryId").IsModified = true;
                //_appDbContext.Entry(objUsers).Property("ZipPostalCode").IsModified = true;
                //_appDbContext.Entry(objUsers).Property("Desciption").IsModified = true;
                //_appDbContext.Entry(objUsers).Property("TagLine").IsModified = true;
                //_appDbContext.Entry(objUsers).Property("UserFavourite").IsModified = true;

                if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("DOB"))))
                {
                    _appDbContext.Entry(objUsers).Property("Age").IsModified = true;
                }

                _appDbContext.Entry(objUsers).Property("LastUpdated").IsModified = true;
                //_appDbContext.Users.Update(objUsers);
                await _appDbContext.SaveChangesAsync();

                if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("Email"))))
                    _appDbContext.Entry(objUserLoginAccount).Property("Email").IsModified = true;
                _appDbContext.Entry(objUserLoginAccount).Property("LastUpdated").IsModified = true;

                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    var objUserDetails = from u in _appDbContext.Users
                                         join g in _appDbContext.Genders on u.GenderId equals g.Id into DetailsGenders
                                         from g1 in DetailsGenders.DefaultIfEmpty()
                                         join c in _appDbContext.Countries on u.CountryId equals c.Id into DetailsCountries
                                         from c1 in DetailsCountries.DefaultIfEmpty()
                                         join s in _appDbContext.StateProvinces on u.StateProvinceId equals s.Id into DetailsStateProvinces
                                         from s1 in DetailsStateProvinces.DefaultIfEmpty()
                                         join ula in _appDbContext.UserLoginAccount on u.UserId equals ula.UserId into DetailsUserLoginAccount
                                         from ula1 in DetailsUserLoginAccount.DefaultIfEmpty()
                                         where (u.UserId == strUserId)
                                         select new
                                         {
                                             UserName = u.PhoneNumber,
                                             UserId = u.UserId,
                                             FirstName = u.FirstName,
                                             LastName = u.LastName,
                                             Gender = g1.Name,
                                             Age = u.Age,
                                             //Email = u.Email,
                                             DOB = u.DOB,
                                             PhoneNumber = u.PhoneNumber,
                                             Email = ula1.Email,
                                             Address = new
                                             {
                                                 AddressLine1 = u.AddressLine1,
                                                 AddressLine2 = u.AddressLine2,
                                                 City = Convert.ToString(u.CityId),
                                                 StateProvince = s1.Name,
                                                 Country = c1.Name,
                                                 ZipPostalCode = u.ZipPostalCode,
                                                 Latitude = Convert.ToString(u.GeoLocationLatitude),
                                                 Longitude = Convert.ToString(u.GeoLocationLongitude)
                                             },
                                             ProfileDescription = u.Desciption,
                                             ProfileTagLine = u.TagLine,
                                             Likes = u.UserFavourite,
                                             CurrentlyOnline = ""
                                         };

                    if (objUserDetails == null || objUserDetails.Count() <= 0)
                    {
                        return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid phone number" }));
                    }

                    return Ok(new { Status = "OK", User = objUserDetails });
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

        // POST api/customers/GetUserAccountSettingInfo
        [HttpPost]
        [Route("GetUserAccountSettingInfo")]
        public ActionResult GetUserAccountSettingInfo([FromBody] JObject objJson)
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

                var objUserDetails = from u in _appDbContext.Users
                                     join up in _appDbContext.UserProfileSetting on u.UserId equals up.UserId into DetailsUserProfileSetting
                                     from up1 in DetailsUserProfileSetting.DefaultIfEmpty()
                                     join s in _appDbContext.SubscriptionType on up1.SubscriptionTypeID equals s.Id into DetailsSubscriptionType
                                     from s1 in DetailsSubscriptionType.DefaultIfEmpty()
                                     where (u.PhoneNumber == Convert.ToString(objJson.SelectToken("username")))
                                     select new
                                     {
                                         UserName = u.PhoneNumber,
                                         Email = u.Email,
                                         UserId = u.UserId,
                                         Subscription = new
                                         {
                                             Id = Convert.ToString(s1.Id),
                                             Description = s1.Description,
                                             Cost = Convert.ToString(s1.Cost),
                                             SubscriptionPeriod = Convert.ToString(s1.SubscriptionPeriod),
                                         },
                                         Notification = new
                                         {
                                             EmailEnabled = Convert.ToString(up1.EmailNotificationEnabled),
                                             PhonePush = Convert.ToString(up1.PushNotificationEnabled),
                                             PublicProfileVisible = Convert.ToString(up1.ProfileVisibilityId)
                                         }
                                     };

                if (objUserDetails == null || objUserDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid phone number" }));
                }

                return Ok(new { Status = "OK", User = objUserDetails });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/customers/UpdateUserSubscriptionSetting
        [HttpPost]
        [Route("UpdateUserSubscriptionSetting")]
        public async Task<ActionResult> UpdateUserSubscriptionSetting([FromBody] JObject objJson)
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
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                var objUserList = _appDbContext.Users.Select(t => new { UserIdVal = t.UserId, EmailVal = t.Email, PhoneNumberVal = t.PhoneNumber }).Where(s => s.PhoneNumberVal == Convert.ToString(objJson.SelectToken("username"))).OrderByDescending(p => p.UserIdVal).FirstOrDefault();
                if (objUserList == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid username" }));
                }

                if (strUserId != objUserList.UserIdVal)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid username" }));
                }

                int intRecCount = _appDbContext.SubscriptionType.Where(t => t.Id == Convert.ToInt32(objJson.SelectToken("subscriptionid"))).Count();
                if (intRecCount <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid Subscription Id" }));
                }

                UserProfileSetting objUserProfileSetting = new UserProfileSetting();
                objUserProfileSetting.UserId = strUserId;
                objUserProfileSetting.SubscriptionTypeID = Convert.ToInt32(objJson.SelectToken("subscriptionid"));

                _appDbContext.UserProfileSetting.Attach(objUserProfileSetting);
                _appDbContext.Entry(objUserProfileSetting).Property("SubscriptionTypeID").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    var objUserDetails = from u in _appDbContext.Users
                                         join up in _appDbContext.UserProfileSetting on u.UserId equals up.UserId into DetailsUserProfileSetting
                                         from up1 in DetailsUserProfileSetting.DefaultIfEmpty()
                                         join s in _appDbContext.SubscriptionType on up1.SubscriptionTypeID equals s.Id into DetailsSubscriptionType
                                         from s1 in DetailsSubscriptionType.DefaultIfEmpty()
                                         where (u.UserId == strUserId)
                                         select new
                                         {
                                             UserName = u.Email,
                                             UserId = u.UserId,
                                             Subscription = new
                                             {
                                                 Id = Convert.ToString(s1.Id),
                                                 Description = s1.Description,
                                                 Cost = Convert.ToString(s1.Cost),
                                                 SubscriptionPeriod = Convert.ToString(s1.SubscriptionPeriod)
                                             }
                                         };

                    if (objUserDetails == null || objUserDetails.Count() <= 0)
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                    }

                    return Ok(new { Status = "OK", User = objUserDetails });
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

        // POST api/customers/UpdateUserAccountNotificationSetting
        [HttpPost]
        [Route("UpdateUserAccountNotificationSetting")]
        public async Task<ActionResult> UpdateUserAccountNotificationSetting([FromBody] JObject objJson)
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
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                var objUserList = _appDbContext.Users.Select(t => new { UserIdVal = t.UserId, EmailVal = t.Email, PhoneNumberVal = t.PhoneNumber }).Where(s => s.PhoneNumberVal == Convert.ToString(objJson.SelectToken("username"))).OrderByDescending(p => p.UserIdVal).FirstOrDefault();
                if (objUserList == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid username" }));
                }

                if (strUserId != objUserList.UserIdVal)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid username" }));
                }

                UserProfileSetting objUserProfileSetting = new UserProfileSetting();
                objUserProfileSetting.UserId = strUserId;
                objUserProfileSetting.EmailNotificationEnabled = Convert.ToBoolean(Convert.ToInt32(objJson.SelectToken("EmailNotificationEnabled")));
                objUserProfileSetting.PushNotificationEnabled = Convert.ToBoolean(Convert.ToInt32(objJson.SelectToken("PushNotificationEnabled")));
                objUserProfileSetting.ProfileVisibilityId = Convert.ToInt32(objJson.SelectToken("ProfileVisibilityId"));

                _appDbContext.UserProfileSetting.Attach(objUserProfileSetting);
                _appDbContext.Entry(objUserProfileSetting).Property("EmailNotificationEnabled").IsModified = true;
                _appDbContext.Entry(objUserProfileSetting).Property("PushNotificationEnabled").IsModified = true;
                _appDbContext.Entry(objUserProfileSetting).Property("ProfileVisibilityId").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    var objUserDetails = from u in _appDbContext.Users
                                         join up in _appDbContext.UserProfileSetting on u.UserId equals up.UserId into DetailsUserProfileSetting
                                         from up1 in DetailsUserProfileSetting.DefaultIfEmpty()
                                         where (u.UserId == strUserId)
                                         select new
                                         {
                                             UserName = u.PhoneNumber,
                                             Email = u.Email,
                                             UserId = u.UserId,
                                             Notification = new
                                             {
                                                 EmailEnabled = Convert.ToString(up1.EmailNotificationEnabled),
                                                 PhonePush = Convert.ToString(up1.PushNotificationEnabled),
                                                 PublicProfileVisible = Convert.ToString(up1.ProfileVisibilityId)
                                             }
                                         };

                    if (objUserDetails == null || objUserDetails.Count() <= 0)
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                    }

                    return Ok(new { Status = "OK", User = objUserDetails });
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

        // POST api/customerpersonality/GetUserProfileSearchSettingInfo
        [HttpPost]
        [Route("GetUserProfileSearchSettingInfo")]
        public ActionResult GetUserProfileSearchSettingInfo([FromBody] JObject objJson)
        {
            try
            {
                if (string.IsNullOrEmpty(Convert.ToString(Request.Headers["api_key"])))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strUserId = UserLoginSessionKeys.GetUserIdByApiKey(_appDbContext, Guid.Parse(Request.Headers["api_key"]));
                if (string.IsNullOrEmpty(strUserId))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                var objUserDetails = from u in _appDbContext.Users
                                     where (u.PhoneNumber == Convert.ToString(objJson.SelectToken("username")))
                                     select new
                                     {
                                         UserName = u.PhoneNumber,
                                         Email = u.Email,
                                         UserId = u.UserId,
                                         ProfileSearch = new
                                         {
                                             MaxDistance = Convert.ToString(u.MaxDistance),
                                             MinAge = Convert.ToString(u.MinAge),
                                             MaxAge = Convert.ToString(u.MaxAge)
                                         }
                                     };

                if (objUserDetails == null || objUserDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid username" }));
                }

                return Ok(new { Status = "OK", User = objUserDetails });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/customers/UpdateUserProfileSearchSettingInfo
        [HttpPost]
        [Route("UpdateUserProfileSearchSettingInfo")]
        public async Task<ActionResult> UpdateUserProfileSearchSettingInfo([FromBody] JObject objJson)
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
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                var objUserList = _appDbContext.Users.Select(t => new { UserIdVal = t.UserId, EmailVal = t.Email, PhoneNumberVal = t.PhoneNumber }).Where(s => s.PhoneNumberVal == Convert.ToString(objJson.SelectToken("username"))).OrderByDescending(p => p.UserIdVal).FirstOrDefault();
                if (objUserList == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid username" }));
                }

                if (strUserId != objUserList.UserIdVal)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid username" }));
                }

                Users objUsers = new Users();
                objUsers.UserId = strUserId;
                objUsers.MaxDistance = Convert.ToInt32(objJson.SelectToken("MaxDistance"));
                objUsers.MinAge = Convert.ToInt32(objJson.SelectToken("MinAge"));
                objUsers.MaxAge = Convert.ToInt32(objJson.SelectToken("MaxAge"));

                _appDbContext.Users.Attach(objUsers);
                _appDbContext.Entry(objUsers).Property("MaxDistance").IsModified = true;
                _appDbContext.Entry(objUsers).Property("MinAge").IsModified = true;
                _appDbContext.Entry(objUsers).Property("MaxAge").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    var objUserDetails = from u in _appDbContext.Users
                                         where (u.UserId == strUserId)
                                         select new
                                         {
                                             UserName = u.PhoneNumber,
                                             Email = u.Email,
                                             UserId = u.UserId,
                                             ProfileSearch = new
                                             {
                                                 MaxDistance = Convert.ToString(u.MaxDistance),
                                                 MinAge = Convert.ToString(u.MinAge),
                                                 MaxAge = Convert.ToString(u.MaxAge)
                                             }
                                         };

                    if (objUserDetails == null || objUserDetails.Count() <= 0)
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                    }

                    return Ok(new { Status = "OK", User = objUserDetails });
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

        // POST api/customerpersonality/GetUserbyId
        [HttpPost]
        [Route("GetUserbyId")]
        public ActionResult GetUserbyId([FromBody] JObject objJson)
        {
            try
            {
                if (string.IsNullOrEmpty(Convert.ToString(Request.Headers["api_key"])))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strUserId = UserLoginSessionKeys.GetUserIdByApiKey(_appDbContext, Guid.Parse(Request.Headers["api_key"]));
                if (string.IsNullOrEmpty(strUserId))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                var objUserDetails = from u in _appDbContext.Users
                                     where (u.PhoneNumber == Convert.ToString(objJson.SelectToken("username")))
                                     select new
                                     {
                                         FirstName = u.FirstName,
                                         LastName = u.LastName,
                                         UserName = u.PhoneNumber,
                                         Email = u.Email,
                                         City = Convert.ToString(u.CityId)
                                     };

                if (objUserDetails == null || objUserDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid username" }));
                }

                return Ok(new { Status = "OK", User = objUserDetails });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/customers/AddUserToFriendList
        [HttpPost]
        [Route("AddUserToFriendList")]
        public async Task<ActionResult> AddUserToFriendList([FromBody] JObject objJson)
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
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                var objUserList = _appDbContext.Users.Select(t => new { UserIdVal = t.UserId, EmailVal = t.Email, PhoneNumberVal = t.PhoneNumber }).Where(s => s.PhoneNumberVal == Convert.ToString(objJson.SelectToken("username"))).OrderByDescending(p => p.UserIdVal).FirstOrDefault();
                if (objUserList == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid username. Cannot add user to friend list" }));
                }

                var objUserDetails = from uf in _appDbContext.UserFriendList
                                     where (uf.UserId == strUserId && uf.FriendId == objUserList.UserIdVal)
                                     select uf;

                if (objUserDetails.Count() > 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "User " + Convert.ToString(objUserList.UserIdVal) + " already added as friend" }));
                }

                UserFriendList objUserFriendList = new UserFriendList();
                objUserFriendList.UserId = strUserId;
                objUserFriendList.FriendId = objUserList.UserIdVal;
                objUserFriendList.ConnectionDegree = 1;
                await _appDbContext.UserFriendList.AddAsync(objUserFriendList);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "User " + Convert.ToString(objUserList.UserIdVal) + " added as friend" });
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

        // POST api/customerpersonality/GetFriendbyId
        [HttpPost]
        [Route("GetFriendbyId")]
        public ActionResult GetFriendbyId([FromBody] JObject objJson)
        {
            try
            {
                if (string.IsNullOrEmpty(Convert.ToString(Request.Headers["api_key"])))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strUserId = UserLoginSessionKeys.GetUserIdByApiKey(_appDbContext, Guid.Parse(Request.Headers["api_key"]));
                if (string.IsNullOrEmpty(strUserId))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                var objUserList = _appDbContext.Users.Select(t => new { UserIdVal = t.UserId, EmailVal = t.Email, PhoneNumberVal = t.PhoneNumber }).Where(s => s.PhoneNumberVal == Convert.ToString(objJson.SelectToken("username"))).OrderByDescending(p => p.UserIdVal).FirstOrDefault();
                if (objUserList == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid username." }));
                }

                var objUserDetails = from uf in _appDbContext.UserFriendList
                                     join u in _appDbContext.Users on uf.FriendId equals u.UserId into Details
                                     from u1 in Details.DefaultIfEmpty()
                                     where (uf.UserId == strUserId && uf.FriendId == objUserList.UserIdVal)
                                     //select uf;
                                     select new
                                     {
                                         FirstName = u1.FirstName,
                                         LastName = u1.LastName,
                                         UserName = u1.PhoneNumber,
                                         Email = u1.Email,
                                         City = Convert.ToString(u1.CityId)
                                     };

                if (objUserDetails == null || objUserDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid username" }));
                }

                return Ok(new { Status = "OK", User = objUserDetails });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/customers/GetAllFriends
        [HttpPost]
        [Route("GetAllFriends")]
        public ActionResult GetAllFriends()
        {
            try
            {
                if (string.IsNullOrEmpty(Convert.ToString(Request.Headers["api_key"])))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strUserId = UserLoginSessionKeys.GetUserIdByApiKey(_appDbContext, Guid.Parse(Request.Headers["api_key"]));
                if (string.IsNullOrEmpty(strUserId))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                var objUserDetails = from uf in _appDbContext.UserFriendList
                                     join u in _appDbContext.Users on uf.FriendId equals u.UserId into Details
                                     from u1 in Details.DefaultIfEmpty()
                                     where (uf.UserId == strUserId)
                                     select new
                                     {
                                         UserFriendListId = uf.Id,
                                         UserFriendUserId = uf.FriendId,
                                         FirstName = u1.FirstName,
                                         LastName = u1.LastName,
                                         UserName = u1.Email,
                                         City = Convert.ToString(u1.CityId),

                                         ImageDetails = (from upi in _appDbContext.UserProfileImages
                                                         where (upi.UserId == uf.FriendId)
                                                         orderby upi.Id ascending
                                                         select new
                                                         {
                                                             ImageUrl = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("UserProfileImages").Value)) + upi.FileName,
                                                         }).Take(1),

                                     };

                if (objUserDetails == null || objUserDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                return Ok(new { Status = "OK", User = objUserDetails });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/customers/DeleteFriendbyId
        [HttpPost]
        [Route("DeleteFriendbyId")]
        public async Task<ActionResult> DeleteFriendbyId([FromBody] JObject objJson)
        {
            try
            {
                if (string.IsNullOrEmpty(Convert.ToString(Request.Headers["api_key"])))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strUserId = UserLoginSessionKeys.GetUserIdByApiKey(_appDbContext, Guid.Parse(Request.Headers["api_key"]));
                if (string.IsNullOrEmpty(strUserId))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                var objUserList = _appDbContext.Users.Select(t => new { UserIdVal = t.UserId, EmailVal = t.Email, PhoneNumberVal = t.PhoneNumber }).Where(s => s.PhoneNumberVal == Convert.ToString(objJson.SelectToken("username"))).OrderByDescending(p => p.UserIdVal).FirstOrDefault();
                if (objUserList == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid username." }));
                }

                UserFriendList objUserFriendList = _appDbContext.UserFriendList
                                .Where(uf => uf.UserId == strUserId && uf.FriendId == objUserList.UserIdVal)
                                .OrderByDescending(t => t.Id).FirstOrDefault();

                if (objUserFriendList == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "User " + Convert.ToString(objUserList.UserIdVal) + " not found. Cannot delete the user from friend list." }));
                }
                else
                {
                    _appDbContext.UserFriendList.Remove(objUserFriendList);
                    int returnVal = await _appDbContext.SaveChangesAsync();
                    if (returnVal > 0)
                    {
                        return Ok(new { Status = "OK", Detail = "User " + Convert.ToString(objUserList.UserIdVal) + " has been removed from friend list" });
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

        // POST api/customers/UpdateUserCurrentStatus
        [HttpPost]
        [Route("UpdateUserCurrentStatus")]
        public async Task<ActionResult> UpdateUserCurrentStatus([FromBody] JObject objJson)
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

                Users objUsers = new Users();
                objUsers.UserId = strUserId;
                objUsers.UserCurrentStatus = Convert.ToBoolean(Convert.ToInt32(objJson.SelectToken("UserCurrentStatus")));
                objUsers.LastUpdatedUserCurrentStatus = DateTime.UtcNow;

                _appDbContext.Users.Attach(objUsers);
                _appDbContext.Entry(objUsers).Property("UserCurrentStatus").IsModified = true;
                _appDbContext.Entry(objUsers).Property("LastUpdatedUserCurrentStatus").IsModified = true;
                //_appDbContext.Users.Update(objUsers);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", User = "UserCurrentStatus has been updated." });
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

        // POST api/customers/GetUserGeoLocale
        [HttpPost]
        [Route("GetUserGeoLocale")]
        public ActionResult GetUserGeoLocale([FromBody] JObject objJson)
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

                var objUserList = _appDbContext.Users.Select(t => new { UserIdVal = t.UserId, EmailVal = t.Email, PhoneNumberVal = t.PhoneNumber }).Where(s => s.PhoneNumberVal == Convert.ToString(objJson.SelectToken("username"))).OrderByDescending(p => p.UserIdVal).FirstOrDefault();
                if (objUserList == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid username" }));
                }

                if (strUserId != objUserList.UserIdVal)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid username" }));
                }

                var objUserDetails = from u in _appDbContext.Users
                                     join s in _appDbContext.StateProvinces on u.StateProvinceId equals s.Id into DetailsStateProvinces
                                     from s1 in DetailsStateProvinces.DefaultIfEmpty()
                                     where (u.UserId == strUserId)
                                     select new
                                     {
                                         Address1 = u.AddressLine1,
                                         Address2 = u.AddressLine2,
                                         City = Convert.ToString(u.CityId),
                                         StateProvince = s1.Name,
                                         ZipPostal = u.ZipPostalCode,
                                         Longitude = Convert.ToString(u.GeoLocationLongitude),
                                         Latitude = Convert.ToString(u.GeoLocationLatitude)
                                     };

                if (objUserDetails == null || objUserDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid username" }));
                }

                return Ok(new { Status = "OK", User = objUserDetails });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        public string SendVerificationEmail_Register(string strEmail, string strVerificationCode)
        {
            try
            {
                StringBuilder objEmailBody = new StringBuilder();
                objEmailBody.AppendLine("<html>");
                objEmailBody.AppendLine("<body>");
                objEmailBody.AppendLine("<div>");
                objEmailBody.AppendLine("Thank you for registering for Nurchure  App. you verification code is " + strVerificationCode + ". Please enter your verification code in the app to verify your email");
                objEmailBody.AppendLine("<br/><br/>");
                objEmailBody.AppendLine("Thank You.");
                objEmailBody.AppendLine("</div>");
                objEmailBody.AppendLine("</body>");
                objEmailBody.AppendLine("</html>");

                string strMailStatus = new clsEmail(configuration).SendEmail(strEmail, "New User Registration", Convert.ToString(objEmailBody), "Nurchure - User Registration");
                return strMailStatus;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public string SendVerificationEmail_ChangePasswordAnonymous(string strEmail, string strVerificationCode)
        {
            try
            {
                StringBuilder objEmailBody = new StringBuilder();
                objEmailBody.AppendLine("<html>");
                objEmailBody.AppendLine("<body>");
                objEmailBody.AppendLine("<div>");
                objEmailBody.AppendLine("Your verification code is " + strVerificationCode + " for change password. Please enter your verification code in the app to change password");
                objEmailBody.AppendLine("<br/><br/>");
                objEmailBody.AppendLine("Thank You.");
                objEmailBody.AppendLine("</div>");
                objEmailBody.AppendLine("</body>");
                objEmailBody.AppendLine("</html>");

                string strMailStatus = new clsEmail(configuration).SendEmail(strEmail, "Change Password", Convert.ToString(objEmailBody), "Nurchure - Change Password");
                return strMailStatus;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        // POST api/customers/UploadUserProfileImages
        [HttpPost]
        [Route("UploadUserProfileImages")]
        public async Task<ActionResult> UploadUserProfileImages([FromBody] JObject objJson)
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

                var objUserProfileImagesDetail = (from upi in _appDbContext.UserProfileImages
                                                  where (upi.UserId == strUserId)
                                                  orderby upi.Id descending
                                                  select upi).ToList<UserProfileImages>();

                int intImageOrder = 1;
                if (objUserProfileImagesDetail.Count() > 0)
                {
                    if (Convert.ToInt32(objUserProfileImagesDetail[0].ImageOrder) >= 6)
                    {
                        return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Upload limit exited. You can only upload 5 images in profile." }));
                    }

                    intImageOrder = objUserProfileImagesDetail[0].ImageOrder + 1;
                }

                string FileExtension = Path.GetExtension(Convert.ToString(objJson.SelectToken("filename")));

                if (FileExtension != ".jpg" && FileExtension != ".jpeg" && FileExtension != ".png" && FileExtension != ".gif")
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid File Type. Upload only - .jpg, .jpeg, .png, .gif." }));
                }

                if (string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("base64string"))))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Base64string missing." }));
                }

                string strFileName = "File_" + strUserId + "_" + Convert.ToString(DateTime.UtcNow.Ticks) + FileExtension;

                //string strSavedPath = String.Format("{0}\\Media\\UserProfileImages" + $@"\{strFileName}", Environment.CurrentDirectory);
                string strSavedPath = (Convert.ToString(configuration.GetSection("appSettings").GetSection("MediaUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("UserProfileImages").Value) + strFileName);

                string base64string = Convert.ToString(objJson.SelectToken("base64string")).Replace(" ", "+");
                byte[] imageBytes = Convert.FromBase64String(base64string);
                System.IO.File.WriteAllBytes(strSavedPath, imageBytes);

                UserProfileImages objUserProfileImages = new UserProfileImages();
                objUserProfileImages.UserId = strUserId;
                objUserProfileImages.FileName = strFileName;
                objUserProfileImages.ImageOrder = intImageOrder;
                objUserProfileImages.AddedDate = DateTime.UtcNow;
                await _appDbContext.UserProfileImages.AddAsync(objUserProfileImages);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Profile image uploaded successfully." });
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

        // POST api/customers/GetUserProfileImages
        [HttpPost]
        [Route("GetUserProfileImages")]
        public ActionResult GetUserProfileImages()
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

                var objUserProfileImagesDetail = (from upi in _appDbContext.UserProfileImages
                                                  where (upi.UserId == strUserId)
                                                  orderby upi.ImageOrder
                                                  select new
                                                  {
                                                      Id = Convert.ToString(upi.Id),
                                                      RefId = upi.UserId,
                                                      //FileName = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + "Media/UserProfileImages/" + upi.FileName),
                                                      FileName = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("UserProfileImages").Value) + upi.FileName),
                                                      //FileName = Environment.CurrentDirectory + "\\Media\\UserProfileImages\\" + upi.FileName,
                                                      ImageOrder = Convert.ToString(upi.ImageOrder),
                                                      AddedDate = Convert.ToString(upi.AddedDate),
                                                      LastUpdatedDate = Convert.ToString(upi.LastUpdatedDate)
                                                  });

                if (objUserProfileImagesDetail == null || objUserProfileImagesDetail.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Profile images doesn’t exist." }));
                }

                return Ok(new { Status = "OK", UserProfileImages = objUserProfileImagesDetail });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/customers/DeleteUserProfileImages
        [HttpPost]
        [Route("DeleteUserProfileImages")]
        public async Task<ActionResult> DeleteUserProfileImages([FromBody] JObject objJson)
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

                var objUserProfileImages = (from upi in _appDbContext.UserProfileImages
                                            where (upi.Id == Convert.ToInt32(objJson.SelectToken("userprofileimageid")) && upi.UserId == strUserId)
                                            select upi).ToList<UserProfileImages>();

                if (objUserProfileImages == null || objUserProfileImages.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "User profile image doesn’t exist." }));
                }
                else
                {
                    _appDbContext.UserProfileImages.Remove(objUserProfileImages[0]);
                    int returnVal = await _appDbContext.SaveChangesAsync();
                    if (returnVal > 0)
                    {
                        int intNewImageOrder = objUserProfileImages[0].ImageOrder;
                        _appDbContext.UserProfileImages.Where(x => x.UserId == strUserId).ToList().ForEach(x =>
                        {
                            if (x.ImageOrder > objUserProfileImages[0].ImageOrder)
                            {
                                x.ImageOrder = intNewImageOrder;
                                intNewImageOrder = intNewImageOrder + 1;
                            }
                        });
                        //_appDbContext.SaveChanges();
                        await _appDbContext.SaveChangesAsync();

                        //string strSavedPath = @"C:\Media\UserProfileImages" + $@"\{objUserProfileImages[0].FileName}";
                        //string strSavedPath = String.Format("{0}\\Media\\UserProfileImages" + $@"\{objUserProfileImages[0].FileName}", Environment.CurrentDirectory);
                        string strSavedPath = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("UserProfileImages").Value) + objUserProfileImages[0].FileName);

                        if (System.IO.File.Exists(strSavedPath))
                        {
                            System.IO.File.Delete(strSavedPath);
                        }
                        return Ok(new { Status = "OK", Detail = "User profile image deleted." });
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

        // POST api/customers/EditUserProfileImages
        [HttpPost]
        [Route("EditUserProfileImages")]
        public async Task<ActionResult> EditUserProfileImages([FromBody] JObject objJson)
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

                var objUserProfileImagesDetail = (from upi in _appDbContext.UserProfileImages
                                                  where (upi.Id == Convert.ToInt32(objJson.SelectToken("userprofileimageid")) && upi.UserId == strUserId)
                                                  select upi).AsNoTracking().ToList<UserProfileImages>();

                if (objUserProfileImagesDetail == null || objUserProfileImagesDetail.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "User profile image doesn’t exist." }));
                }

                string FileExtension = Path.GetExtension(Convert.ToString(objJson.SelectToken("filename")));

                if (FileExtension != ".jpg" && FileExtension != ".jpeg" && FileExtension != ".png" && FileExtension != ".gif")
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid File Type. Upload only - .jpg, .jpeg, .png, .gif." }));
                }

                if (string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("base64string"))))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Base64string missing." }));
                }

                string strFileName = "File_" + strUserId + "_" + Convert.ToString(DateTime.UtcNow.Ticks) + FileExtension;
                //string strSavedPath = @"C:\Media\UserProfileImages" + $@"\{strFileName}";
                //string strSavedPath = String.Format("{0}\\Media\\UserProfileImages" + $@"\{strFileName}", Environment.CurrentDirectory);
                string strSavedPath = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("UserProfileImages").Value) + strFileName);

                string base64string = Convert.ToString(objJson.SelectToken("base64string")).Replace(" ", "+");
                byte[] imageBytes = Convert.FromBase64String(base64string);
                System.IO.File.WriteAllBytes(strSavedPath, imageBytes);

                UserProfileImages objUserProfileImages = new UserProfileImages();
                objUserProfileImages.Id = objUserProfileImagesDetail[0].Id;
                objUserProfileImages.FileName = strFileName;
                objUserProfileImages.LastUpdatedDate = DateTime.UtcNow;

                _appDbContext.UserProfileImages.Attach(objUserProfileImages);
                _appDbContext.Entry(objUserProfileImages).Property("FileName").IsModified = true;
                _appDbContext.Entry(objUserProfileImages).Property("LastUpdatedDate").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    string strFilePath = @"C:\Media\UserProfileImages" + $@"\{objUserProfileImagesDetail[0].FileName}";
                    if (System.IO.File.Exists(strFilePath))
                    {
                        System.IO.File.Delete(strFilePath);
                    }
                    return Ok(new { Status = "OK", Detail = "Profile image updated successfully." });
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

        // POST api/customers/VerifyUserPhoneNumber
        [HttpPost]
        [Route("VerifyUserPhoneNumber")]
        public ActionResult VerifyUserPhoneNumber([FromBody] JObject objJson)
        {
            try
            {
                if (string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("phonenumber"))))
                {
                    return BadRequest(new { Status = "Error", Error = "Phone Number Missing." });
                }

                int intRecCount = _appDbContext.UserLoginAccount.Where(t => t.PhoneNumber == Convert.ToString(objJson.SelectToken("phonenumber"))).Count();
                if (intRecCount > 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Account already exists." }));
                }

                return Ok(new { Status = "OK", Detail = "Phone number is available." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/customers/UpdateLocation
        [HttpPost]
        [Route("UpdateLocation")]
        public async Task<ActionResult> UpdateLocation([FromBody] JObject objJson)
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

                Users objUpdateloc = new Users();
                objUpdateloc.UserId = strUserId;
                objUpdateloc.GeoLocationLongitude = Convert.ToDouble(objJson.SelectToken("Longitude"));
                objUpdateloc.GeoLocationLatitude = Convert.ToDouble(objJson.SelectToken("Latitude"));

                _appDbContext.Users.Attach(objUpdateloc);
                _appDbContext.Entry(objUpdateloc).Property("GeoLocationLongitude").IsModified = true;
                _appDbContext.Entry(objUpdateloc).Property("GeoLocationLatitude").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", User = "User Location has been updated." });
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

        // POST api/customers/UpdatePersonalityTest
        [HttpPost]
        [Route("UpdatePersonalityTest")]
        public async Task<ActionResult> UpdatePersonalityTest([FromBody] JObject objJson)
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
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                Users objUsers = new Users();
                objUsers.UserId = strUserId;
                objUsers.GenderId = Convert.ToInt32(objJson.SelectToken("Gender"));
                objUsers.Age = Convert.ToInt32(objJson.SelectToken("Age"));
                objUsers.Activity = Convert.ToString(objJson.SelectToken("Activity"));
                objUsers.LastUpdated = DateTime.UtcNow;
                _appDbContext.Users.Attach(objUsers);

                _appDbContext.Entry(objUsers).Property("GenderId").IsModified = true;
                _appDbContext.Entry(objUsers).Property("Age").IsModified = true;
                _appDbContext.Entry(objUsers).Property("Activity").IsModified = true;
                _appDbContext.Entry(objUsers).Property("LastUpdated").IsModified = true;

                int returnVal = await _appDbContext.SaveChangesAsync();

                UserLoginAccount objUserLoginAccount = new UserLoginAccount();
                objUserLoginAccount.UserId = strUserId;
                if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("Email"))))
                    objUserLoginAccount.Email = Convert.ToString(objJson.SelectToken("Email"));
                _appDbContext.UserLoginAccount.Attach(objUserLoginAccount);

                if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("Email"))))
                    _appDbContext.Entry(objUserLoginAccount).Property("Email").IsModified = true;

                returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Personality Test Answers submitted successfully" });
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

        // POST api/customers/usermanagement
        [HttpPost]
        [Route("usermanagement")]
        public async Task<ActionResult> usermanagement([FromBody] JObject objJson)
        {
            try
            {
                var objUserList = _appDbContext.UserLoginAccount
                                .Select(t => new { UserIdVal = t.UserId, EmailVal = t.Email, un = t.UserName })
                                .Where(s => s.un == Convert.ToString(objJson.SelectToken("username")))
                                .FirstOrDefault();

                if (objUserList == null)
                {
                    return BadRequest(new { Status = "Error", Error = "Please provide valid username or password" });
                }
                else
                {
                    string strUserId = objUserList.UserIdVal;

                    //Deleting User Profile Images
                    var objUserProfileImages = (from upi in _appDbContext.UserProfileImages
                                                where (upi.UserId == strUserId)
                                                select upi).ToList<UserProfileImages>();

                    foreach (var userprofimage in objUserProfileImages)
                    {
                        _appDbContext.UserProfileImages.Remove(userprofimage);
                        await _appDbContext.SaveChangesAsync();
                    }

                    //Deleting User Personality Summary
                    var objUserPersonalitySummary = (from ups in _appDbContext.UserPersonalitySummary
                                                     where (ups.UserId == strUserId)
                                                     select ups).ToList<UserPersonalitySummary>();

                    foreach (var userPerSum in objUserPersonalitySummary)
                    {
                        _appDbContext.UserPersonalitySummary.Remove(userPerSum);
                        await _appDbContext.SaveChangesAsync();
                    }

                    //Deleting User Personality Questionaire Details
                    var objUserPersonalityQuestionaireDetails = (from upqd in _appDbContext.UserPersonalityQuestionaireDetails
                                                                 where (upqd.UserId == strUserId)
                                                                 select upqd).ToList<UserPersonalityQuestionaireDetails>();

                    foreach (var userPerQues in objUserPersonalityQuestionaireDetails)
                    {
                        _appDbContext.UserPersonalityQuestionaireDetails.Remove(userPerQues);
                        await _appDbContext.SaveChangesAsync();
                    }

                    //Deleting User Activities
                    var objUserActivities = (from ua in _appDbContext.UserActivities
                                             where (ua.UserId == strUserId)
                                             select ua).ToList<UserActivities>();

                    foreach (var userAct in objUserActivities)
                    {
                        _appDbContext.UserActivities.Remove(userAct);
                        await _appDbContext.SaveChangesAsync();
                    }

                    //Deleting Media
                    var objMedia = (from me in _appDbContext.Media
                                    where (me.RefId == strUserId)
                                    select me).ToList<Media>();

                    foreach (var med in objMedia)
                    {
                        _appDbContext.Media.Remove(med);
                        await _appDbContext.SaveChangesAsync();
                    }

                    //Deleting External Account Login
                    var objExternalAccountLogin = (from extacclog in _appDbContext.ExternalAccountLogin
                                                   where (extacclog.UserId == strUserId)
                                                   select extacclog).ToList<ExternalAccountLogin>();

                    foreach (var externalAcc in objExternalAccountLogin)
                    {
                        _appDbContext.ExternalAccountLogin.Remove(externalAcc);
                        await _appDbContext.SaveChangesAsync();
                    }

                    //Deleting User SMS Invitation Details
                    var objUserSMSInvitationDetails = (from usid in _appDbContext.UserSMSInvitationDetails
                                                       where (usid.SenderUserId == strUserId)
                                                       select usid).ToList<UserSMSInvitationDetails>();

                    foreach (var usrsmsinvdet in objUserSMSInvitationDetails)
                    {
                        _appDbContext.UserSMSInvitationDetails.Remove(usrsmsinvdet);
                        await _appDbContext.SaveChangesAsync();
                    }

                    //Deleting User Verification Details
                    var objUserVerificationDetails = (from uvd in _appDbContext.UserVerificationDetails
                                                      where (uvd.UserId == strUserId)
                                                      select uvd).ToList<UserVerificationDetails>();

                    foreach (var usrverdet in objUserVerificationDetails)
                    {
                        _appDbContext.UserVerificationDetails.Remove(usrverdet);
                        await _appDbContext.SaveChangesAsync();
                    }

                    //Deleting User Login Session Keys
                    var objUserLoginSessionKeys = (from ulsk in _appDbContext.UserLoginSessionKeys
                                                   where (ulsk.UserId == strUserId)
                                                   select ulsk).ToList<UserLoginSessionKeys>();

                    foreach (var usrlogsesskey in objUserLoginSessionKeys)
                    {
                        _appDbContext.UserLoginSessionKeys.Remove(usrlogsesskey);
                        await _appDbContext.SaveChangesAsync();
                    }

                    //Deleting User Login Account
                    var objUserLoginAccount = (from ula in _appDbContext.UserLoginAccount
                                               where (ula.UserId == strUserId)
                                               select ula).ToList<UserLoginAccount>();

                    foreach (var usrlogacc in objUserLoginAccount)
                    {
                        _appDbContext.UserLoginAccount.Remove(usrlogacc);
                        await _appDbContext.SaveChangesAsync();
                    }

                    //Deleting Users
                    var objUsers = (from us in _appDbContext.Users
                                    where (us.UserId == strUserId)
                                    select us).ToList<Users>();

                    foreach (var usr in objUsers)
                    {
                        _appDbContext.Users.Remove(usr);
                        await _appDbContext.SaveChangesAsync();
                    }

                    return Ok(new { Status = "OK", Detail = "Data deleted" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = ex.Message, SystemError = ex.Message }));
            }
        }

        // POST api/customers/AboutUs
        [HttpPost]
        [Route("AboutUs")]
        public ActionResult AboutUs([FromBody] JObject objJson)
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

                string strAboutUsMessage = Convert.ToString(configuration.GetSection("appSettings").GetSection("AboutUs").Value);

                return Ok(new { Status = "OK", Message = strAboutUsMessage });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/customers/TermsAndConditions
        [HttpPost]
        [Route("TermsAndConditions")]
        public ActionResult TermsAndConditions([FromBody] JObject objJson)
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

                string strTermsAndConditions = Convert.ToString(configuration.GetSection("appSettings").GetSection("AboutUs").Value);

                return Ok(new { Status = "OK", Message = strTermsAndConditions });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/customers/FAQs
        [HttpPost]
        [Route("FAQs")]
        public ActionResult FAQs([FromBody] JObject objJson)
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

                string strFAQsMessage = Convert.ToString(configuration.GetSection("appSettings").GetSection("AboutUs").Value);

                return Ok(new { Status = "OK", Message = strFAQsMessage });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        private void LogError(string ex)
        {
            string message = "";
            message += Convert.ToString(DateTime.UtcNow) + " " + string.Format("Message: {0}", ex);
            message += Environment.NewLine;
            string path = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("Log").Value));
            //string path = System.Web.HttpContext.Current.Server.MapPath("~/Admin/ErrorLog/Error.txt");
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine(message);
                writer.Close();
            }
        }

        ~customersController()
        {
            _appDbContext.Dispose();
        }
    }
}