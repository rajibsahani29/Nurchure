using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    //[System.Web.Http.Route("api/chat")]
    [Route("api/[controller]")]
    [ApiController]
    public class chatController : Controller
    {
        private AppDbContext _appDbContext;
        private IConfiguration configuration;

        public chatController(AppDbContext context, IConfiguration iConfig)
        {
            _appDbContext = context;
            configuration = iConfig;
        }

        // POST api/chat/SendTextChat
        [HttpPost]
        [Route("SendTextChat")]
        public async Task<ActionResult> SendTextChat([FromBody] JObject objJson)
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

                int intRecCount = _appDbContext.ChatGroups.Where(t => t.UserId1 == Convert.ToString(objJson.SelectToken("senderuserid")) && t.UserId2 == Convert.ToString(objJson.SelectToken("receiveruserid"))
                || t.UserId1 == Convert.ToString(objJson.SelectToken("receiveruserid")) && t.UserId2 == Convert.ToString(objJson.SelectToken("senderuserid"))).Count();

                if (intRecCount == 0)
                {
                    ChatGroups objChatGroups = new ChatGroups();
                    objChatGroups.UserId1 = Convert.ToString(objJson.SelectToken("senderuserid"));
                    objChatGroups.UserId2 = Convert.ToString(objJson.SelectToken("receiveruserid"));
                    objChatGroups.AddedDate = DateTime.UtcNow;
                    objChatGroups.Active = true;
                    await _appDbContext.ChatGroups.AddAsync(objChatGroups);
                    await _appDbContext.SaveChangesAsync();
                }

                var objRecentChatId = _appDbContext.ChatGroups.Select(t => new { ChatId = t.Id, UserId1 = t.UserId1, UserId2 = t.UserId2 }).Where(s => s.UserId1 == Convert.ToString(objJson.SelectToken("senderuserid")) && s.UserId2 == Convert.ToString(objJson.SelectToken("receiveruserid")) ||
                s.UserId1 == Convert.ToString(objJson.SelectToken("receiveruserid")) && s.UserId2 == Convert.ToString(objJson.SelectToken("senderuserid"))).FirstOrDefault();

                ChatMessages objChatMessages = new ChatMessages();
                objChatMessages.ChatId = objRecentChatId.ChatId;
                objChatMessages.UserId = Convert.ToString(objJson.SelectToken("senderuserid"));
                //objChatMessages.Message = Convert.ToString(objJson.SelectToken("message"));
                objChatMessages.MessageType = Convert.ToString(objJson.SelectToken("messagetype"));
                objChatMessages.AddedDate = DateTime.UtcNow;

                if (Convert.ToString(objJson.SelectToken("messagetype")) == "I")
                {
                    string FileExtension = Path.GetExtension(Convert.ToString(objJson.SelectToken("message")));

                    if (FileExtension != ".jpg" && FileExtension != ".jpeg" && FileExtension != ".png" && FileExtension != ".gif")
                    {
                        return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid File Type. Upload only - .jpg, .jpeg, .png, .gif." }));
                    }

                    if (string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("base64string"))))
                    {
                        return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Base64string missing." }));
                    }

                    string strFileName = "File_" + Convert.ToString(objJson.SelectToken("senderuserid")) + "_" + Convert.ToString(objRecentChatId.ChatId) + "_" + Convert.ToString(DateTime.UtcNow.Ticks) + FileExtension;
                    string strSavedPath = (Convert.ToString(configuration.GetSection("appSettings").GetSection("MediaUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("Chat").Value) + strFileName);
                    objChatMessages.Message = strFileName;

                    string base64string = Convert.ToString(objJson.SelectToken("base64string")).Replace(" ", "+");
                    byte[] imageBytes = Convert.FromBase64String(base64string);
                    System.IO.File.WriteAllBytes(strSavedPath, imageBytes);
                }
                else
                {
                    objChatMessages.Message = Convert.ToString(objJson.SelectToken("message"));
                }

                await _appDbContext.ChatMessages.AddAsync(objChatMessages);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Message saved." });
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

        // POST api/chat/CreatePrivateChatGroup
        [HttpPost]
        [Route("CreatePrivateChatGroup")]
        public async Task<ActionResult> CreatePrivateChatGroup([FromBody] JObject objJson)
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

                var userids = Convert.ToString(objJson.SelectToken("userids")).Split(',');

                if (userids.Count() > 10)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Group can not have more the 11 members." }));
                }

                string FileExtension = Path.GetExtension(Convert.ToString(objJson.SelectToken("groupimagename")));

                if (FileExtension != ".jpg" && FileExtension != ".jpeg" && FileExtension != ".png" && FileExtension != ".gif")
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid File Type. Upload only - .jpg, .jpeg, .png, .gif." }));
                }

                if (string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("base64string"))))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Base64string missing." }));
                }

                UserChatGroup objUserChatGroup = new UserChatGroup();
                objUserChatGroup.GroupName = Convert.ToString(objJson.SelectToken("groupname"));
                objUserChatGroup.GroupImage = "";
                objUserChatGroup.CreatedUserId = strUserId;
                objUserChatGroup.GroupType = "Private";
                objUserChatGroup.ActivityId = 0;
                objUserChatGroup.CreatedDate = DateTime.UtcNow;
                await _appDbContext.UserChatGroup.AddAsync(objUserChatGroup);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    int intUserChatGroupId = objUserChatGroup.Id;

                    string strFileName = "File_" + strUserId + "_" + Convert.ToString(intUserChatGroupId) + "_" + Convert.ToString(DateTime.UtcNow.Ticks) + FileExtension;
                    //string strSavedPath = "http:\\3.18.122.133:8080\\Media\\GroupImages\\File_NIC000000068_56_637309403799570621.jpg";
                    string strSavedPath = (Convert.ToString(configuration.GetSection("appSettings").GetSection("MediaUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("GroupImages").Value) + strFileName);

                    string base64string = Convert.ToString(objJson.SelectToken("base64string")).Replace(" ", "+");
                    byte[] imageBytes = Convert.FromBase64String(base64string);
                    System.IO.File.WriteAllBytes(strSavedPath, imageBytes);

                    objUserChatGroup.GroupImage = strFileName;
                    _appDbContext.UserChatGroup.Attach(objUserChatGroup);
                    _appDbContext.Entry(objUserChatGroup).Property("GroupImage").IsModified = true;
                    returnVal = await _appDbContext.SaveChangesAsync();

                    UserChatGroupFriends objUserChatGroupAdmin = new UserChatGroupFriends();
                    objUserChatGroupAdmin.GroupId = intUserChatGroupId;
                    objUserChatGroupAdmin.AddedUserId = "0";
                    objUserChatGroupAdmin.UserId = strUserId;
                    objUserChatGroupAdmin.AddedDate = DateTime.UtcNow;
                    objUserChatGroupAdmin.AdminRights = true;
                    objUserChatGroupAdmin.AdminRightsAddedDate = DateTime.UtcNow;
                    await _appDbContext.UserChatGroupFriends.AddAsync(objUserChatGroupAdmin);
                    returnVal = await _appDbContext.SaveChangesAsync();

                    if (returnVal > 0)
                    {
                        foreach (string objItem in userids)
                        {
                            UserChatGroupFriends objUserChatGroupFriends = new UserChatGroupFriends();
                            objUserChatGroupFriends.GroupId = intUserChatGroupId;
                            objUserChatGroupFriends.AddedUserId = strUserId;
                            objUserChatGroupFriends.UserId = Convert.ToString(objItem);
                            objUserChatGroupFriends.AddedDate = DateTime.UtcNow;
                            objUserChatGroupFriends.AdminRights = false;
                            await _appDbContext.UserChatGroupFriends.AddAsync(objUserChatGroupFriends);

                            returnVal = await _appDbContext.SaveChangesAsync();
                        }

                        if (returnVal > 0)
                        {
                            return Ok(new { Status = "OK", GroupId = intUserChatGroupId, Detail = "Group created." });
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

        // POST api/chat/CreatePublicChatGroup
        [HttpPost]
        [Route("CreatePublicChatGroup")]
        public async Task<ActionResult> CreatePublicChatGroup()
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

                var objPersonalityMatchResult = GetPersonalityMatchResult(strUserId);
                if (objPersonalityMatchResult != null && objPersonalityMatchResult.Count() > 0)
                {
                    string strGroupName = "";

                    var objUserPersonalitySummaryDetail = (from ups in _appDbContext.UserPersonalitySummary
                                                           where ups.UserId == strUserId
                                                           select ups).ToList<UserPersonalitySummary>();

                    strGroupName = objUserPersonalitySummaryDetail[0].PrefferedMBTIMatch;

                    /*var objGroupForActivity = (from ua in _appDbContext.UserActivities
                                               join ucg in _appDbContext.UserChatGroup on ua.ActivityId equals ucg.ActivityId into DetailsUserChatGroup
                                               from ucg1 in DetailsUserChatGroup.DefaultIfEmpty()
                                               where ua.UserId == strUserId
                                               group ua by ua.ActivityId into ua1
                                               select new {
                                                   ActivityId = ua1.Key,
                                                   ActivityCount = Convert.ToString(ua1.Count())
                                               }).Take(3).OrderByDescending(t => t.ActivityCount).ToList();*/

                    var objGroupForActivity = (from ua in _appDbContext.UserActivities
                                               join a in _appDbContext.Activities on ua.ActivityId equals a.Id
                                               where ua.UserId == strUserId
                                               select new
                                               {
                                                   ActivityId = ua.ActivityId,
                                                   ActivityName = a.Description,
                                                   ActivityCount = (
                                                                        from ucg in _appDbContext.UserChatGroup
                                                                        where (ucg.ActivityId == ua.ActivityId)
                                                                        select ucg.ActivityId
                                                                   ).Count()
                                               }).Take(3).OrderByDescending(t => t.ActivityCount).ToList();

                    strGroupName += "_" + objGroupForActivity[0].ActivityName;

                    var objUsers = (from u in _appDbContext.Users
                                    where u.UserId == strUserId
                                    select u).ToList();

                    strGroupName += "_" + objUsers[0].AddressLine2;

                    string strAge = "";
                    if (objUsers[0].Age >= 20 && objUsers[0].Age <= 24)
                    {
                        strAge = "2024";
                    }
                    else if (objUsers[0].Age >= 25 && objUsers[0].Age <= 29)
                    {
                        strAge = "2529";
                    }
                    else if (objUsers[0].Age >= 30 && objUsers[0].Age <= 34)
                    {
                        strAge = "3034";
                    }
                    else if (objUsers[0].Age >= 35 && objUsers[0].Age <= 39)
                    {
                        strAge = "3539";
                    }
                    else if (objUsers[0].Age >= 40 && objUsers[0].Age <= 44)
                    {
                        strAge = "4044";
                    }
                    else if (objUsers[0].Age >= 45 && objUsers[0].Age <= 49)
                    {
                        strAge = "4549";
                    }

                    strGroupName += "_" + strAge;

                    //return Ok(new { Status = "OK", Count = objGroupForActivity.Count(), MatchResult = objGroupForActivity, GroupName = strGroupName });
                    //return Ok(new { Status = "OK", GroupName = strGroupName });

                    var objUserChatGroupDetails = (from ucg in _appDbContext.UserChatGroup
                                                   where ucg.CreatedUserId == strUserId && ucg.GroupName == strGroupName && ucg.GroupType == "Public"
                                                   select ucg).OrderByDescending(t => t.Id).ToList();

                    bool isCreateNewGroup = false;
                    int recIndex = 0;

                    if (objUserChatGroupDetails.Count() > 0)
                    {
                        var objUserChatGroupMessages = (from ucgm in _appDbContext.UserChatGroupMessages
                                                        where ucgm.GroupId == objUserChatGroupDetails[0].Id
                                                        select ucgm).OrderByDescending(t => t.Id).ToList();
                        if (objUserChatGroupMessages.Count() > 0)
                        {
                            if (objUserChatGroupMessages[0].AddedDate >= DateTime.UtcNow.AddDays(-21))
                            {
                                var objUserChatGroupFriendsDetails = (from ucgm in _appDbContext.UserChatGroupFriends
                                                                      where ucgm.GroupId == objUserChatGroupDetails[0].Id
                                                                      select ucgm).ToList();

                                if (objUserChatGroupFriendsDetails.Count() < 11)
                                {
                                    if (objPersonalityMatchResult != null && objPersonalityMatchResult.Count() > 0)
                                    {
                                        int returnVal = 0;
                                        int GroupFriendCount = objUserChatGroupFriendsDetails.Count();
                                        UserChatGroupFriends objUserChatGroupFriends = new UserChatGroupFriends();
                                        foreach (var objItem in objPersonalityMatchResult)
                                        {
                                            int count = objUserChatGroupFriendsDetails.Where(t => t.UserId == objItem.UserId).Count();
                                            if (count <= 0)
                                            {
                                                objUserChatGroupFriends = new UserChatGroupFriends();
                                                objUserChatGroupFriends.GroupId = objUserChatGroupDetails[0].Id;
                                                objUserChatGroupFriends.AddedUserId = strUserId;
                                                objUserChatGroupFriends.UserId = objItem.UserId;
                                                objUserChatGroupFriends.AddedDate = DateTime.UtcNow;
                                                objUserChatGroupFriends.AdminRights = false;
                                                await _appDbContext.UserChatGroupFriends.AddAsync(objUserChatGroupFriends);
                                                returnVal = await _appDbContext.SaveChangesAsync();
                                                GroupFriendCount = GroupFriendCount + 1;
                                            }
                                            recIndex = recIndex + 1;
                                            if (GroupFriendCount == 11)
                                            {
                                                break;
                                            }
                                        }

                                        if (recIndex < objPersonalityMatchResult.Count())
                                        {
                                            isCreateNewGroup = true;
                                        }
                                        else
                                        {
                                            if (returnVal > 0)
                                            {
                                                return Ok(new { Status = "OK", Detail = "Group created." });
                                            }
                                            else
                                            {
                                                return Ok(new { Status = "OK", Detail = "Same group already exist." });
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    isCreateNewGroup = true;
                                }
                            }
                            else
                            {
                                isCreateNewGroup = true;
                            }
                        }
                        else
                        {
                            isCreateNewGroup = true;
                        }
                    }
                    else
                    {
                        isCreateNewGroup = true;
                    }

                    if (isCreateNewGroup == true)
                    {
                        UserChatGroup objUserChatGroup = new UserChatGroup();
                        objUserChatGroup.GroupName = strGroupName;
                        objUserChatGroup.GroupImage = "";
                        objUserChatGroup.CreatedUserId = strUserId;
                        objUserChatGroup.GroupType = "Public";
                        objUserChatGroup.ActivityId = objGroupForActivity[0].ActivityId;
                        objUserChatGroup.CreatedDate = DateTime.UtcNow;
                        await _appDbContext.UserChatGroup.AddAsync(objUserChatGroup);
                        int returnVal = await _appDbContext.SaveChangesAsync();

                        if (returnVal > 0)
                        {
                            int intUserChatGroupId = objUserChatGroup.Id;

                            UserChatGroupFriends objUserChatGroupFriends = new UserChatGroupFriends();
                            objUserChatGroupFriends.GroupId = intUserChatGroupId;
                            objUserChatGroupFriends.AddedUserId = "0";
                            objUserChatGroupFriends.UserId = strUserId;
                            objUserChatGroupFriends.AddedDate = DateTime.UtcNow;
                            objUserChatGroupFriends.AdminRights = true;
                            objUserChatGroupFriends.AdminRightsAddedDate = DateTime.UtcNow;
                            await _appDbContext.UserChatGroupFriends.AddAsync(objUserChatGroupFriends);
                            returnVal = await _appDbContext.SaveChangesAsync();
                            if (returnVal > 0)
                            {
                                if (objPersonalityMatchResult != null && objPersonalityMatchResult.Count() > 0)
                                {
                                    int index = 0;
                                    foreach (var objItem in objPersonalityMatchResult)
                                    {
                                        index = index + 1;
                                        if (recIndex != 0 && index <= recIndex)
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            objUserChatGroupFriends = new UserChatGroupFriends();
                                            objUserChatGroupFriends.GroupId = intUserChatGroupId;
                                            objUserChatGroupFriends.AddedUserId = strUserId;
                                            objUserChatGroupFriends.UserId = objItem.UserId;
                                            objUserChatGroupFriends.AddedDate = DateTime.UtcNow;
                                            objUserChatGroupFriends.AdminRights = false;
                                            await _appDbContext.UserChatGroupFriends.AddAsync(objUserChatGroupFriends);
                                            returnVal = await _appDbContext.SaveChangesAsync();
                                        }
                                    }
                                }
                                return Ok(new { Status = "OK", Detail = "Group created." });
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
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "No matching result found." }));
                    }
                }
                else
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "No any matching user found." }));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/chat/UpdatePrivateChatGroup
        [HttpPost]
        [Route("UpdatePrivateChatGroup")]
        public async Task<ActionResult> UpdatePrivateChatGroup([FromBody] JObject objJson)
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

                var objUserChatGroupDetail = (from ucg in _appDbContext.UserChatGroup
                                              where (ucg.Id == Convert.ToInt32(objJson.SelectToken("groupid")))
                                              select ucg).AsNoTracking().ToList<UserChatGroup>();

                if (objUserChatGroupDetail == null || objUserChatGroupDetail.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "User char group doesn’t exist." }));
                }

                UserChatGroup objUserChatGroup = new UserChatGroup();

                bool flag = false;
                if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("groupimagename"))) && !string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("base64string"))))
                {

                    string FileExtension = Path.GetExtension(Convert.ToString(objJson.SelectToken("groupimagename")));
                    if (FileExtension != ".jpg" && FileExtension != ".jpeg" && FileExtension != ".png" && FileExtension != ".gif")
                    {
                        return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid File Type. Upload only - .jpg, .jpeg, .png, .gif." }));
                    }

                    string strFileName = "File_" + strUserId + "_" + Convert.ToString(objJson.SelectToken("groupid")) + "_" + Convert.ToString(DateTime.UtcNow.Ticks) + FileExtension;
                    string strSavedPath = (Convert.ToString(configuration.GetSection("appSettings").GetSection("MediaUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("GroupImages").Value) + strFileName);

                    string base64string = Convert.ToString(objJson.SelectToken("base64string")).Replace(" ", "+");
                    byte[] imageBytes = Convert.FromBase64String(base64string);
                    System.IO.File.WriteAllBytes(strSavedPath, imageBytes);

                    objUserChatGroup.GroupImage = strFileName;
                    flag = true;
                }

                objUserChatGroup.Id = Convert.ToInt32(objJson.SelectToken("groupid"));
                objUserChatGroup.GroupName = Convert.ToString(objJson.SelectToken("groupname"));
                objUserChatGroup.LastUpdatedDate = DateTime.UtcNow;

                _appDbContext.UserChatGroup.Attach(objUserChatGroup);
                _appDbContext.Entry(objUserChatGroup).Property("GroupName").IsModified = true;
                _appDbContext.Entry(objUserChatGroup).Property("LastUpdatedDate").IsModified = true;

                if (flag == true)
                {
                    _appDbContext.Entry(objUserChatGroup).Property("GroupImage").IsModified = true;
                }

                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    if (flag == true)
                    {
                        string strFilePath = (Convert.ToString(configuration.GetSection("appSettings").GetSection("MediaUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("GroupImages").Value) + objUserChatGroupDetail[0].GroupImage);
                        if (System.IO.File.Exists(strFilePath))
                        {
                            System.IO.File.Delete(strFilePath);
                        }
                    }

                    var objUpdatedUserChatGroupDetail = (from ucg in _appDbContext.UserChatGroup
                                                         where (ucg.Id == Convert.ToInt32(objJson.SelectToken("groupid")))
                                                         select new
                                                         {
                                                             Id = Convert.ToString(ucg.Id),
                                                             GroupName = ucg.GroupName,
                                                             GroupImage = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + "Media/GroupImages/" + ucg.GroupImage),
                                                             CreatedUserId = ucg.CreatedUserId,
                                                             GroupType = ucg.GroupType,
                                                             MembersCount = (
                                                                   from ucgf in _appDbContext.UserChatGroupFriends
                                                                   where (ucgf.GroupId == Convert.ToInt32(objJson.SelectToken("groupid")))
                                                                   select ucgf.Id
                                                                 ).Count(),
                                                             AddedDate = Convert.ToString(ucg.CreatedDate),
                                                             LastUpdatedDate = Convert.ToString(ucg.LastUpdatedDate)
                                                         });

                    var objUserChatGroupFriendsDetails = (from ucgf in _appDbContext.UserChatGroupFriends
                                                          join ula in _appDbContext.UserLoginAccount on ucgf.UserId equals ula.UserId into DetailsUserLoginAccount
                                                          from ula1 in DetailsUserLoginAccount.DefaultIfEmpty()
                                                          join u in _appDbContext.Users on ula1.UserId equals u.UserId into DetailsUsers
                                                          from u1 in DetailsUsers.DefaultIfEmpty()
                                                          where (ucgf.GroupId == Convert.ToInt32(objJson.SelectToken("groupid")))
                                                          select new
                                                          {
                                                              UserId = ucgf.UserId,
                                                              UserName = ula1.UserName,
                                                              FirstName = u1.FirstName,
                                                              LastName = u1.LastName,
                                                              ImageDetails = (from upi in _appDbContext.UserProfileImages
                                                                              where (upi.UserId == ucgf.UserId)
                                                                              orderby upi.Id ascending
                                                                              select new
                                                                              {
                                                                                  ImageUrl = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("UserProfileImages").Value)) + upi.FileName,
                                                                              }).Take(1),
                                                          });

                    return Ok(new { Status = "OK", Detail = "Group details updated.", UpdatedChatGroupDetail = objUpdatedUserChatGroupDetail, ChatGroupFriendsDetails = objUserChatGroupFriendsDetails });
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

        // POST api/chat/GetChatGroupDetails
        [HttpPost]
        [Route("GetChatGroupDetails")]
        public ActionResult GetChatGroupDetails([FromBody] JObject objJson)
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

                var objUserChatGroupDetail = (from ucg in _appDbContext.UserChatGroup
                                              where (ucg.Id == Convert.ToInt32(objJson.SelectToken("groupid")))
                                              select new
                                              {
                                                  Id = Convert.ToString(ucg.Id),
                                                  GroupName = ucg.GroupName,
                                                  GroupImage = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + "Media/GroupImages/" + ucg.GroupImage),
                                                  CreatedUserId = ucg.CreatedUserId,
                                                  GroupType = ucg.GroupType,
                                                  MembersCount = (
                                                        from ucgf in _appDbContext.UserChatGroupFriends
                                                        where (ucgf.GroupId == Convert.ToInt32(objJson.SelectToken("groupid")))
                                                        select ucgf.Id
                                                      ).Count(),
                                                  AddedDate = Convert.ToString(ucg.CreatedDate),
                                                  LastUpdatedDate = Convert.ToString(ucg.LastUpdatedDate)
                                              });

                var objUserChatGroupFriendsDetails = (from ucgf in _appDbContext.UserChatGroupFriends
                                                      join ula in _appDbContext.UserLoginAccount on ucgf.UserId equals ula.UserId into DetailsUserLoginAccount
                                                      from ula1 in DetailsUserLoginAccount.DefaultIfEmpty()
                                                      join u in _appDbContext.Users on ula1.UserId equals u.UserId into DetailsUsers
                                                      from u1 in DetailsUsers.DefaultIfEmpty()
                                                      where (ucgf.GroupId == Convert.ToInt32(objJson.SelectToken("groupid")))
                                                      select new
                                                      {
                                                          UserId = ucgf.UserId,
                                                          UserName = ula1.UserName,
                                                          FirstName = u1.FirstName,
                                                          LastName = u1.LastName,
                                                          ImageDetails = (from upi in _appDbContext.UserProfileImages
                                                                          where (upi.UserId == ucgf.UserId)
                                                                          orderby upi.Id ascending
                                                                          select new
                                                                          {
                                                                              ImageUrl = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("UserProfileImages").Value)) + upi.FileName,
                                                                          }).Take(1),
                                                      });

                if (objUserChatGroupDetail == null || objUserChatGroupDetail.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "User chat group doesn’t exist." }));
                }

                return Ok(new { Status = "OK", ChatGroupDetail = objUserChatGroupDetail, ChatGroupFriendsDetails = objUserChatGroupFriendsDetails });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/chat/AddUserChatGroupFriends
        [HttpPost]
        [Route("AddUserChatGroupFriends")]
        public async Task<ActionResult> AddUserChatGroupFriends([FromBody] JObject objJson)
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

                var objGroupUserDetails = from ucgf in _appDbContext.UserChatGroupFriends
                                          where (ucgf.GroupId == Convert.ToInt32(objJson.SelectToken("groupid")) && ucgf.UserId == Convert.ToString(objJson.SelectToken("friendid")))
                                          select ucgf;

                if (objGroupUserDetails.Count() > 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Friend already added in group" }));
                }

                int intRecCount = _appDbContext.UserChatGroupFriends.Where(t => t.GroupId == Convert.ToInt32(objJson.SelectToken("groupid"))).Count();

                if (intRecCount >= 11)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Group can not have more the 11 members." }));
                }

                UserChatGroupFriends objUserChatGroupFriends = new UserChatGroupFriends();
                objUserChatGroupFriends.GroupId = Convert.ToInt32(objJson.SelectToken("groupid"));
                objUserChatGroupFriends.AddedUserId = strUserId;
                objUserChatGroupFriends.UserId = Convert.ToString(objJson.SelectToken("friendid"));
                objUserChatGroupFriends.AddedDate = DateTime.UtcNow;
                objUserChatGroupFriends.AdminRights = Convert.ToBoolean(Convert.ToInt32(objJson.SelectToken("adminrights")));
                if (Convert.ToBoolean(Convert.ToInt32(objJson.SelectToken("adminrights"))) == true)
                {
                    objUserChatGroupFriends.AdminRightsAddedDate = DateTime.UtcNow;
                }
                await _appDbContext.UserChatGroupFriends.AddAsync(objUserChatGroupFriends);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Friend added in group." });
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

        // POST api/chat/SendTextChatGroup
        [HttpPost]
        [Route("SendTextChatGroup")]
        public async Task<ActionResult> SendTextChatGroup([FromBody] JObject objJson)
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

                UserChatGroupMessages objUserChatGroupMessages = new UserChatGroupMessages();
                objUserChatGroupMessages.GroupId = Convert.ToInt32(objJson.SelectToken("groupid"));
                objUserChatGroupMessages.UserId = Convert.ToString(objJson.SelectToken("userid"));

                if (Convert.ToString(objJson.SelectToken("messagetype")) == "I")
                {
                    string FileExtension = Path.GetExtension(Convert.ToString(objJson.SelectToken("message")));

                    if (FileExtension != ".jpg" && FileExtension != ".jpeg" && FileExtension != ".png" && FileExtension != ".gif")
                    {
                        return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid File Type. Upload only - .jpg, .jpeg, .png, .gif." }));
                    }

                    if (string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("base64string"))))
                    {
                        return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Base64string missing." }));
                    }

                    string strFileName = "File_" + Convert.ToString(objJson.SelectToken("userid")) + "_" + Convert.ToString(objJson.SelectToken("groupid")) + "_" + Convert.ToString(DateTime.UtcNow.Ticks) + FileExtension;
                    string strSavedPath = (Convert.ToString(configuration.GetSection("appSettings").GetSection("MediaUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("ChatGroup").Value) + strFileName);
                    objUserChatGroupMessages.Message = strFileName;

                    string base64string = Convert.ToString(objJson.SelectToken("base64string")).Replace(" ", "+");
                    byte[] imageBytes = Convert.FromBase64String(base64string);
                    System.IO.File.WriteAllBytes(strSavedPath, imageBytes);
                }
                else
                {
                    objUserChatGroupMessages.Message = Convert.ToString(objJson.SelectToken("message"));
                }

                objUserChatGroupMessages.MessageType = Convert.ToString(objJson.SelectToken("messagetype"));
                objUserChatGroupMessages.AddedDate = DateTime.UtcNow;
                await _appDbContext.UserChatGroupMessages.AddAsync(objUserChatGroupMessages);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Chat message saved." });
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

        // POST api/chat/GetRecentChat
        [HttpPost]
        [Route("GetRecentChat")]
        public ActionResult GetRecentChat([FromBody] JObject objJson)
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

                var objBlockedUser = _appDbContext.ReportedUsers.Select(t => new { ReportId = t.Id, ReportingUserId = t.ReportingUserId, BlockedUserId = t.BlockedUserId }).Where(s => s.ReportingUserId == Convert.ToString(objJson.SelectToken("userid1")) && s.BlockedUserId == Convert.ToString(objJson.SelectToken("userid2")) ||
                               s.ReportingUserId == Convert.ToString(objJson.SelectToken("userid2")) && s.BlockedUserId == Convert.ToString(objJson.SelectToken("userid1"))).FirstOrDefault();

                var objRecentChatId = _appDbContext.ChatGroups.Select(t => new { ChatId = t.Id, UserId1 = t.UserId1, UserId2 = t.UserId2 }).Where(s => s.UserId1 == Convert.ToString(objJson.SelectToken("userid1")) && s.UserId2 == Convert.ToString(objJson.SelectToken("userid2")) ||
                                s.UserId1 == Convert.ToString(objJson.SelectToken("userid2")) && s.UserId2 == Convert.ToString(objJson.SelectToken("userid1"))).FirstOrDefault();

                if (objBlockedUser == null)
                { 
                    if (objRecentChatId == null)
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "There is no chat available" }));
                    }
                    else
                    {
                        var objChatMessages = (from cm in _appDbContext.ChatMessages
                                               join ula in _appDbContext.UserLoginAccount on cm.UserId equals ula.UserId into DetailsUserLoginAccount
                                               from ula1 in DetailsUserLoginAccount.DefaultIfEmpty()
                                               join u in _appDbContext.Users on ula1.UserId equals u.UserId into DetailsUsers
                                               from u1 in DetailsUsers.DefaultIfEmpty()
                                               where cm.ChatId == Convert.ToInt32(objRecentChatId.ChatId)
                                               orderby cm.Id descending
                                               select new
                                               {
                                                   AddedDate = cm.AddedDate,
                                                   ChatId = cm.ChatId,
                                                   Message =
                                               (
                                                   cm.MessageType == "A" ? string.Format("{0}{1}{2}", Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value), Convert.ToString(configuration.GetSection("appSettings").GetSection("Chat").Value), cm.Message) :
                                                   cm.MessageType == "I" ? string.Format("{0}{1}{2}", Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value), Convert.ToString(configuration.GetSection("appSettings").GetSection("Chat").Value), cm.Message) :
                                                   cm.MessageType == "V" ? string.Format("{0}{1}{2}", Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value), Convert.ToString(configuration.GetSection("appSettings").GetSection("Chat").Value), cm.Message) : cm.Message
                                               ),

                                                   messageType = cm.MessageType,
                                                   UserId = cm.UserId,
                                                   UserName = ula1.UserName,
                                                   Firstname = u1.FirstName,
                                                   Lastname = u1.LastName,
                                                   ImageDetails = (from upi in _appDbContext.UserProfileImages
                                                                   where (upi.UserId == ula1.UserId)
                                                                   orderby upi.Id ascending
                                                                   select new
                                                                   {
                                                                       ImageUrl = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("UserProfileImages").Value)) + upi.FileName,
                                                                   }).Take(1),
                                               }).Take(25);

                        if (objChatMessages == null || objChatMessages.Count() <= 0)
                        {
                            return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Chat = objChatMessages, Error = "There are no chat messages available" }));
                        }

                        return Ok(new { Status = "OK", Chat = objChatMessages });
                    }
            }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", MessageBlocked = true, Error = "Messaging is blocked." }));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/chat/GetRecentGroupChat
        [HttpPost]
        [Route("GetRecentGroupChat")]
        public ActionResult GetRecentGroupChat([FromBody] JObject objJson)
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

                var objPreferredEvent = (from e in _appDbContext.Events
                                         join ugc in _appDbContext.UserChatGroup on e.GroupId equals ugc.Id into DetailsUserChatGroup
                                         from ugc1 in DetailsUserChatGroup.DefaultIfEmpty()
                                         join gp in _appDbContext.GroupPoll on e.Id equals gp.EventId into DetailsGroupPoll
                                         from gp1 in DetailsGroupPoll.DefaultIfEmpty()
                                         join m in _appDbContext.Merchants on gp1.PreferredMerchantId equals m.MerchantId into DetailsMerchants
                                         from m1 in DetailsMerchants.DefaultIfEmpty()
                                         join mc in _appDbContext.MerchantCampaigns on m1.MerchantId equals mc.MerchantId into DetailsMerchantCampaigns
                                         from mc1 in DetailsMerchantCampaigns.DefaultIfEmpty()
                                         join mp in _appDbContext.MerchantPackages on mc1.Id equals mp.CampaignID into DetailsMerchantPackages
                                         from mp1 in DetailsMerchantPackages.DefaultIfEmpty()
                                         join sp in _appDbContext.StateProvinces on m1.StateProvinceID equals sp.Id into DetailsStateProvinces
                                         from sp1 in DetailsStateProvinces.DefaultIfEmpty()
                                         where (e.GroupId == Convert.ToInt32(objJson.SelectToken("groupid")) && gp1.PreferredMerchantId != null)
                                         select new
                                         {
                                             EventName = e.EventName,
                                             PackagePrice = mp1.Price,
                                             EventDate = Convert.ToString(gp1.EventDate1),
                                             EventTime = Convert.ToString(gp1.EventTime1),
                                             EventCreatedDate = Convert.ToString(e.CreatedDate),
                                             MerchantName = m1.MerchantName,
                                             AddressLine1 = m1.AddressLine1,
                                             AddressLine2 = m1.AddressLine2,
                                             AddressLine3 = m1.AddressLine3,
                                             Zip = m1.ZipPostalCode,
                                             State = sp1.Description,
                                             Rating = Convert.ToString(m1.RatingAverage),
                                         });

                var objPreferredEventNull = (from e in _appDbContext.Events
                                         join ugc in _appDbContext.UserChatGroup on e.GroupId equals ugc.Id into DetailsUserChatGroup
                                         from ugc1 in DetailsUserChatGroup.DefaultIfEmpty()
                                         join gp in _appDbContext.GroupPoll on e.Id equals gp.EventId into DetailsGroupPoll
                                         from gp1 in DetailsGroupPoll.DefaultIfEmpty()
                                         join m in _appDbContext.Merchants on gp1.PreferredMerchantId equals m.MerchantId into DetailsMerchants
                                         from m1 in DetailsMerchants.DefaultIfEmpty()
                                         join mc in _appDbContext.MerchantCampaigns on m1.MerchantId equals mc.MerchantId into DetailsMerchantCampaigns
                                         from mc1 in DetailsMerchantCampaigns.DefaultIfEmpty()
                                         join mp in _appDbContext.MerchantPackages on mc1.Id equals mp.CampaignID into DetailsMerchantPackages
                                         from mp1 in DetailsMerchantPackages.DefaultIfEmpty()
                                         join sp in _appDbContext.StateProvinces on m1.StateProvinceID equals sp.Id into DetailsStateProvinces
                                         from sp1 in DetailsStateProvinces.DefaultIfEmpty()
                                         where (e.GroupId == 0)
                                         select new
                                         {
                                             EventName = e.EventName,
                                             PackagePrice = mp1.Price,
                                             EventDate = Convert.ToString(gp1.EventDate1),
                                             EventTime = Convert.ToString(gp1.EventTime1),
                                             EventCreatedDate = Convert.ToString(e.CreatedDate),
                                             MerchantName = m1.MerchantName,
                                             AddressLine1 = m1.AddressLine1,
                                             AddressLine2 = m1.AddressLine2,
                                             AddressLine3 = m1.AddressLine3,
                                             Zip = m1.ZipPostalCode,
                                             State = sp1.Description,
                                             Rating = Convert.ToString(m1.RatingAverage),
                                         });

                var objBackupEvent = (from e in _appDbContext.Events
                                      join ugc in _appDbContext.UserChatGroup on e.GroupId equals ugc.Id into DetailsUserChatGroup
                                      from ugc1 in DetailsUserChatGroup.DefaultIfEmpty()
                                      join gp in _appDbContext.GroupPoll on e.Id equals gp.EventId into DetailsGroupPoll
                                      from gp1 in DetailsGroupPoll.DefaultIfEmpty()
                                      join m in _appDbContext.Merchants on gp1.BackupMerchantId equals m.MerchantId into DetailsMerchants
                                      from m1 in DetailsMerchants.DefaultIfEmpty()
                                      join mc in _appDbContext.MerchantCampaigns on m1.MerchantId equals mc.MerchantId into DetailsMerchantCampaigns
                                      from mc1 in DetailsMerchantCampaigns.DefaultIfEmpty()
                                      join mp in _appDbContext.MerchantPackages on mc1.Id equals mp.CampaignID into DetailsMerchantPackages
                                      from mp1 in DetailsMerchantPackages.DefaultIfEmpty()
                                      join sp in _appDbContext.StateProvinces on m1.StateProvinceID equals sp.Id into DetailsStateProvinces
                                      from sp1 in DetailsStateProvinces.DefaultIfEmpty()
                                      where (e.GroupId == Convert.ToInt32(objJson.SelectToken("groupid")) && gp1.BackupMerchantId != null)
                                      select new
                                      {
                                          EventName = e.EventName,
                                          PackagePrice = mp1.Price,
                                          EventDate = Convert.ToString(gp1.EventDate2),
                                          EventTime = Convert.ToString(gp1.EventTime2),
                                          EventCreatedDate = Convert.ToString(e.CreatedDate),
                                          MerchantName = m1.MerchantName,
                                          AddressLine1 = m1.AddressLine1,
                                          AddressLine2 = m1.AddressLine2,
                                          AddressLine3 = m1.AddressLine3,
                                          Zip = m1.ZipPostalCode,
                                          State = sp1.Description,
                                          Rating = Convert.ToString(m1.RatingAverage),
                                      });

                var objBackupEventNull = (from e in _appDbContext.Events
                                          join ugc in _appDbContext.UserChatGroup on e.GroupId equals ugc.Id into DetailsUserChatGroup
                                          from ugc1 in DetailsUserChatGroup.DefaultIfEmpty()
                                          join gp in _appDbContext.GroupPoll on e.Id equals gp.EventId into DetailsGroupPoll
                                          from gp1 in DetailsGroupPoll.DefaultIfEmpty()
                                          join m in _appDbContext.Merchants on gp1.BackupMerchantId equals m.MerchantId into DetailsMerchants
                                          from m1 in DetailsMerchants.DefaultIfEmpty()
                                          join mc in _appDbContext.MerchantCampaigns on m1.MerchantId equals mc.MerchantId into DetailsMerchantCampaigns
                                          from mc1 in DetailsMerchantCampaigns.DefaultIfEmpty()
                                          join mp in _appDbContext.MerchantPackages on mc1.Id equals mp.CampaignID into DetailsMerchantPackages
                                          from mp1 in DetailsMerchantPackages.DefaultIfEmpty()
                                          join sp in _appDbContext.StateProvinces on m1.StateProvinceID equals sp.Id into DetailsStateProvinces
                                          from sp1 in DetailsStateProvinces.DefaultIfEmpty()
                                          where (e.GroupId == 0)
                                          select new
                                          {
                                              EventName = e.EventName,
                                              PackagePrice = mp1.Price,
                                              EventDate = Convert.ToString(gp1.EventDate2),
                                              EventTime = Convert.ToString(gp1.EventTime2),
                                              EventCreatedDate = Convert.ToString(e.CreatedDate),
                                              MerchantName = m1.MerchantName,
                                              AddressLine1 = m1.AddressLine1,
                                              AddressLine2 = m1.AddressLine2,
                                              AddressLine3 = m1.AddressLine3,
                                              Zip = m1.ZipPostalCode,
                                              State = sp1.Description,
                                              Rating = Convert.ToString(m1.RatingAverage),
                                          });

                var objGroupChatMessages = (from ucgm in _appDbContext.UserChatGroupMessages
                                            join ula in _appDbContext.UserLoginAccount on ucgm.UserId equals ula.UserId into DetailsUserLoginAccount
                                            from ula1 in DetailsUserLoginAccount.DefaultIfEmpty()
                                            join u in _appDbContext.Users on ula1.UserId equals u.UserId into DetailsUsers
                                            from u1 in DetailsUsers.DefaultIfEmpty()
                                            where ucgm.GroupId == Convert.ToInt32(objJson.SelectToken("groupid")) //&& ucgm.MessageType == "T"
                                            orderby ucgm.Id descending
                                            select new
                                            {
                                                AddedDate = ucgm.AddedDate,
                                                GroupId = ucgm.GroupId,
                                                Id = ucgm.Id,
                                                LastUpdated = ucgm.LastUpdated,
                                                //Message = ucgm.Message,

                                                Message =
                                            (
                                                ucgm.MessageType == "A" ? string.Format("{0}{1}{2}", Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value), Convert.ToString(configuration.GetSection("appSettings").GetSection("ChatGroup").Value), ucgm.Message) :
                                                ucgm.MessageType == "I" ? string.Format("{0}{1}{2}", Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value), Convert.ToString(configuration.GetSection("appSettings").GetSection("ChatGroup").Value), ucgm.Message) :
                                                ucgm.MessageType == "V" ? string.Format("{0}{1}{2}", Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value), Convert.ToString(configuration.GetSection("appSettings").GetSection("ChatGroup").Value), ucgm.Message) : ucgm.Message
                                            ),

                                                messageType = ucgm.MessageType,
                                                UserId = ucgm.UserId,
                                                UserName = ula1.UserName,
                                                Firstname = u1.FirstName,
                                                ImageDetails = (from upi in _appDbContext.UserProfileImages
                                                                where (upi.UserId == ula1.UserId)
                                                                orderby upi.Id ascending
                                                                select new
                                                                {
                                                                    ImageUrl = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("UserProfileImages").Value)) + upi.FileName,
                                                                }).Take(1),
                                                PreferredEvent = objPreferredEventNull,
                                                BackupEvent = objBackupEventNull
                                            }).Take(25).Concat
                                            (from e in _appDbContext.Events
                                             join ula in _appDbContext.UserLoginAccount on e.EventCoordinator equals ula.UserId into DetailsUserLoginAccount
                                             from ula1 in DetailsUserLoginAccount.DefaultIfEmpty()
                                             join u in _appDbContext.Users on ula1.UserId equals u.UserId into DetailsUsers
                                             from u1 in DetailsUsers.DefaultIfEmpty()
                                             where e.GroupId == Convert.ToInt32(objJson.SelectToken("groupid")) //&& ucgm.MessageType == "T"
                                             //orderby ucgm.Id descending
                                             select new
                                             {
                                                 AddedDate = e.CreatedDate,
                                                 GroupId = e.GroupId,
                                                 Id = e.Id,
                                                 LastUpdated = e.LastUpdated,
                                                 //Message = ucgm.Message,

                                                 Message = "",

                                                 messageType = "E",
                                                 UserId = e.EventCoordinator,
                                                 UserName = ula1.UserName,
                                                 Firstname = u1.FirstName,
                                                 ImageDetails = (from upi in _appDbContext.UserProfileImages
                                                                 where (upi.UserId == ula1.UserId)
                                                                 orderby upi.Id ascending
                                                                 select new
                                                                 {
                                                                     ImageUrl = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("UserProfileImages").Value)) + upi.FileName,
                                                                 }).Take(1),
                                                 PreferredEvent = objPreferredEvent,
                                                 BackupEvent = objBackupEvent
                                             });

                if (objGroupChatMessages == null || objGroupChatMessages.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "No messages available" }));
                }

                return Ok(new { Status = "OK", Chat = objGroupChatMessages });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new
                {
                    Status = "Error",
                    Error = "Internal Server error. Please contact customer support",
                    SystemError = ex.Message
                }));
            }
        }

        // POST api/chat/GetChatHistory
        [HttpPost]
        [Route("GetChatHistory")]
        public ActionResult GetChatHistory([FromBody] JObject objJson)
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

                int intLimit = 100;
                if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("limit"))))
                {
                    intLimit = Convert.ToInt32(objJson.SelectToken("limit"));
                }

                var objChatMessages = (from cm in _appDbContext.ChatMessages
                                       where cm.ChatId == Convert.ToInt32(objJson.SelectToken("chatid"))
                                       orderby cm.Id descending
                                       select cm).Take(intLimit);

                if (objChatMessages == null || objChatMessages.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Chat " + Convert.ToString(objJson.SelectToken("chatid")) + " doesn’t exist" }));
                }

                return Ok(new { Status = "OK", Chat = objChatMessages });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/chat/GetGroupChatHistory
        [HttpPost]
        [Route("GetGroupChatHistory")]
        public ActionResult GetGroupChatHistory([FromBody] JObject objJson)
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

                int intLimit = 100;
                if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("limit"))))
                {
                    intLimit = Convert.ToInt32(objJson.SelectToken("limit"));
                }

                var objGroupChatMessages = (from ucgm in _appDbContext.UserChatGroupMessages
                                            where ucgm.GroupId == Convert.ToInt32(objJson.SelectToken("groupid"))
                                            orderby ucgm.Id descending
                                            select ucgm).Take(intLimit);

                if (objGroupChatMessages == null || objGroupChatMessages.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Group " + Convert.ToString(objJson.SelectToken("groupid")) + " doesn’t exist" }));
                }

                return Ok(new { Status = "OK", Chat = objGroupChatMessages });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/chat/EditChatbyId
        [HttpPost]
        [Route("EditChatbyId")]
        public async Task<ActionResult> EditChatbyId([FromBody] JObject objJson)
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

                var objChatMesssage = from cm in _appDbContext.ChatMessages
                                      where (cm.Id == Convert.ToInt32(objJson.SelectToken("chatid")))
                                      select cm;

                if (objChatMesssage.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Chat " + Convert.ToString(objJson.SelectToken("chatid")) + " doesn’t exist" }));
                }

                ChatMessages objChatMessages = new ChatMessages();
                objChatMessages.Id = Convert.ToInt32(objJson.SelectToken("chatid"));
                objChatMessages.Message = Convert.ToString(objJson.SelectToken("message"));
                //objChatMessages.MessageType = Convert.ToString(objJson.SelectToken("messagetype"));
                //objChatMessages.LastUpdated = DateTime.UtcNow;

                _appDbContext.ChatMessages.Attach(objChatMessages);
                _appDbContext.Entry(objChatMessages).Property("Message").IsModified = true;
                //_appDbContext.Entry(objChatMessages).Property("MessageType").IsModified = true;
                _appDbContext.Entry(objChatMessages).Property("LastUpdated").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", User = "Message updated." });
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

        // POST api/chat/EditGroupChatbyId
        [HttpPost]
        [Route("EditGroupChatbyId")]
        public async Task<ActionResult> EditGroupChatbyId([FromBody] JObject objJson)
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

                var objGroupChatMesssage = from ucgm in _appDbContext.UserChatGroupMessages
                                           where (ucgm.Id == Convert.ToInt32(objJson.SelectToken("chatid")))
                                           select ucgm;

                if (objGroupChatMesssage.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Group chat " + Convert.ToString(objJson.SelectToken("chatid")) + " doesn’t exist" }));
                }

                UserChatGroupMessages objUserChatGroupMessages = new UserChatGroupMessages();
                objUserChatGroupMessages.Id = Convert.ToInt32(objJson.SelectToken("chatid"));
                objUserChatGroupMessages.Message = Convert.ToString(objJson.SelectToken("message"));
                //objChatMessages.MessageType = Convert.ToString(objJson.SelectToken("messagetype"));
                objUserChatGroupMessages.LastUpdated = DateTime.UtcNow;

                _appDbContext.UserChatGroupMessages.Attach(objUserChatGroupMessages);
                _appDbContext.Entry(objUserChatGroupMessages).Property("Message").IsModified = true;
                //_appDbContext.Entry(objChatMessages).Property("MessageType").IsModified = true;
                _appDbContext.Entry(objUserChatGroupMessages).Property("LastUpdated").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", User = "Message updated." });
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

        // POST api/chat/DeleteChatbyId
        [HttpPost]
        [Route("DeleteChatbyId")]
        public async Task<ActionResult> DeleteChatbyId([FromBody] JObject objJson)
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

                ChatMessages objChatMessages = _appDbContext.ChatMessages
                                .Where(cm => cm.Id == Convert.ToInt32(objJson.SelectToken("chatid")))
                                .FirstOrDefault();

                if (objChatMessages == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Chat " + Convert.ToString(objJson.SelectToken("chatid")) + " doesn’t exist" }));
                }
                else
                {
                    _appDbContext.ChatMessages.Remove(objChatMessages);
                    int returnVal = await _appDbContext.SaveChangesAsync();
                    if (returnVal > 0)
                    {
                        return Ok(new { Status = "OK", Detail = "Message Deleted" });
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

        // POST api/chat/DeleteGroupChatbyId
        [HttpPost]
        [Route("DeleteGroupChatbyId")]
        public async Task<ActionResult> DeleteGroupChatbyId([FromBody] JObject objJson)
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

                UserChatGroupMessages objUserChatGroupMessages = _appDbContext.UserChatGroupMessages
                                .Where(ucgm => ucgm.Id == Convert.ToInt32(objJson.SelectToken("chatid")))
                                .FirstOrDefault();

                if (objUserChatGroupMessages == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Group chat " + Convert.ToString(objJson.SelectToken("chatid")) + " doesn’t exist" }));
                }
                else
                {
                    _appDbContext.UserChatGroupMessages.Remove(objUserChatGroupMessages);
                    int returnVal = await _appDbContext.SaveChangesAsync();
                    if (returnVal > 0)
                    {
                        return Ok(new { Status = "OK", Detail = "Message Deleted" });
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

        // POST api/chat/UpdateChatGroupAdminRights
        [HttpPost]
        [Route("UpdateChatGroupAdminRights")]
        public async Task<ActionResult> UpdateChatGroupAdminRights([FromBody] JObject objJson)
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

                UserChatGroupFriends objUserChatGroupFriendsDetail = _appDbContext.UserChatGroupFriends
                                .Where(ucgf => ucgf.GroupId == Convert.ToInt32(objJson.SelectToken("groupid")) && ucgf.UserId == Convert.ToString(objJson.SelectToken("friendid")))
                                .AsNoTracking().FirstOrDefault();

                if (objUserChatGroupFriendsDetail == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "User doesn’t exist" }));
                }

                UserChatGroupFriends objUserChatGroupFriends = new UserChatGroupFriends();
                objUserChatGroupFriends.Id = objUserChatGroupFriendsDetail.Id;
                objUserChatGroupFriends.AdminRights = Convert.ToBoolean(Convert.ToInt32(objJson.SelectToken("adminrights")));
                objUserChatGroupFriends.LastUpdatedAdminRights = DateTime.UtcNow;

                _appDbContext.UserChatGroupFriends.Attach(objUserChatGroupFriends);
                _appDbContext.Entry(objUserChatGroupFriends).Property("AdminRights").IsModified = true;
                _appDbContext.Entry(objUserChatGroupFriends).Property("LastUpdatedAdminRights").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", User = "Admin rights updated." });
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

        // POST api/chat/SentEmailInvite
        [HttpPost]
        [Route("SentEmailInvite")]
        public async Task<ActionResult> SentEmailInvite([FromBody] JObject objJson)
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

                var objUserList = _appDbContext.Users.Select(t => new { UserIdVal = t.UserId, EmailVal = t.Email }).Where(s => s.EmailVal == Convert.ToString(objJson.SelectToken("username"))).OrderByDescending(p => p.UserIdVal).FirstOrDefault();
                if (objUserList == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid username" }));
                }

                string strInvitationCode = clsCommon.GetRandomAlphaNumeric();

                UserInvitationDetails objUserInvitationDetails = new UserInvitationDetails();
                objUserInvitationDetails.SenderUserId = strUserId;
                objUserInvitationDetails.ReceiverUesrId = objUserList.UserIdVal;
                objUserInvitationDetails.UserChatGroupId = Convert.ToInt32(objJson.SelectToken("groupid"));
                objUserInvitationDetails.InvitationCode = strInvitationCode;
                objUserInvitationDetails.CreationTime = DateTime.UtcNow;
                objUserInvitationDetails.ExpiryTime = DateTime.UtcNow.AddMinutes(30);
                objUserInvitationDetails.RequestValue = "email";
                await _appDbContext.UserInvitationDetails.AddAsync(objUserInvitationDetails);

                int returnVal = await _appDbContext.SaveChangesAsync();
                if (returnVal > 0)
                {
                    string strMailStatus = SendInviteEmail(Convert.ToString(objJson.SelectToken("username")), strInvitationCode);
                    if (strMailStatus == "Success")
                    {
                        return Ok(new { Status = "OK", Description = "An email with invitation code has been sent to " + Convert.ToString(objJson.SelectToken("username")) + "." });
                    }
                    else
                    {
                        return BadRequest(new { Status = "OK", Description = "Error to send invitation email to " + Convert.ToString(objJson.SelectToken("username")) + ". Please contact customer support" });
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

        public string SendInviteEmail(string strEmail, string strInvitationCode)
        {
            try
            {
                StringBuilder objEmailBody = new StringBuilder();
                objEmailBody.AppendLine("<html>");
                objEmailBody.AppendLine("<body>");
                objEmailBody.AppendLine("Nurchure  App : Your invitation code is " + strInvitationCode + ".");
                objEmailBody.AppendLine("<br/><br/>");
                objEmailBody.AppendLine("Thank You.");
                objEmailBody.AppendLine("</div>");
                objEmailBody.AppendLine("</body>");
                objEmailBody.AppendLine("</html>");

                string strMailStatus = new clsEmail(configuration).SendEmail(strEmail, "Invitation", Convert.ToString(objEmailBody), "Nurchure - User Invitation");
                return strMailStatus;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        // POST api/chat/ApplyInviteCode
        [HttpPost]
        [Route("ApplyInviteCode")]
        public async Task<ActionResult> ApplyInviteCode([FromBody] JObject objJson)
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

                var objUserInvitationDetails = _appDbContext.UserInvitationDetails
                                .Where(s => s.ReceiverUesrId == strUserId && s.RequestValue == Convert.ToString(objJson.SelectToken("requestvalue")))
                                .OrderByDescending(t => t.Id).FirstOrDefault();

                if (objUserInvitationDetails == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid User" }));
                }

                if (objUserInvitationDetails.InvitationCode != Convert.ToString(objJson.SelectToken("invitationcode")))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid invitation code" }));
                }

                if (DateTime.UtcNow > Convert.ToDateTime(objUserInvitationDetails.ExpiryTime))
                {
                    return StatusCode(StatusCodes.Status402PaymentRequired, (new { Status = "Error", Error = "Invitation code has expired" }));
                }

                var objGroupUserDetails = from ucgf in _appDbContext.UserChatGroupFriends
                                          where (ucgf.GroupId == objUserInvitationDetails.UserChatGroupId && ucgf.UserId == objUserInvitationDetails.ReceiverUesrId)
                                          select ucgf;

                if (objGroupUserDetails.Count() > 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Friend already added in group" }));
                }

                UserChatGroupFriends objUserChatGroupFriends = new UserChatGroupFriends();
                objUserChatGroupFriends.GroupId = objUserInvitationDetails.UserChatGroupId;
                objUserChatGroupFriends.AddedUserId = objUserInvitationDetails.SenderUserId;
                objUserChatGroupFriends.UserId = objUserInvitationDetails.ReceiverUesrId;
                objUserChatGroupFriends.AddedDate = DateTime.UtcNow;
                objUserChatGroupFriends.AdminRights = false;
                await _appDbContext.UserChatGroupFriends.AddAsync(objUserChatGroupFriends);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "You are added in group." });
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

        // POST api/chat/SendInviteViaPhone
        [HttpPost]
        [Route("SendInviteViaPhone")]
        public async Task<ActionResult> SendInviteViaPhone([FromBody] JObject objJson)
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
                if (objUserList == null || string.IsNullOrEmpty(Convert.ToString(objUserList.PhoneNumberVal)))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid username or phone number missing." }));
                }

                string strInvitationCode = clsCommon.GetRandomAlphaNumeric();

                UserInvitationDetails objUserInvitationDetails = new UserInvitationDetails();
                objUserInvitationDetails.SenderUserId = strUserId;
                objUserInvitationDetails.ReceiverUesrId = objUserList.UserIdVal;
                objUserInvitationDetails.UserChatGroupId = Convert.ToInt32(objJson.SelectToken("groupid"));
                objUserInvitationDetails.InvitationCode = strInvitationCode;
                objUserInvitationDetails.CreationTime = DateTime.UtcNow;
                objUserInvitationDetails.ExpiryTime = DateTime.UtcNow.AddMinutes(30);
                objUserInvitationDetails.RequestValue = "sms";
                await _appDbContext.UserInvitationDetails.AddAsync(objUserInvitationDetails);

                int returnVal = await _appDbContext.SaveChangesAsync();
                if (returnVal > 0)
                {
                    string strMessage = "Nurchure  App : Your invitation code is " + strInvitationCode + ".";
                    string strMailStatus = new clsSMS(configuration).SendSMS(Convert.ToString(objUserList.PhoneNumberVal), strMessage);
                    if (strMailStatus == "Success")
                    {
                        return Ok(new { Status = "OK", Description = "An SMS with invitation code has been sent to " + Convert.ToString(objUserList.PhoneNumberVal) + "." });
                    }
                    else if (strMailStatus == "Error")
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = strMailStatus }));
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

        // POST api/chat/SendMassInviteViaPhone
        [HttpPost]
        [Route("SendMassInviteViaPhone")]
        public async Task<ActionResult> SendMassInviteViaPhone([FromBody] JObject objJson)
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

                string strSuccessUsername = "";
                string strErrorUsername = "";
                JArray objUserNameJson = JArray.Parse(Convert.ToString(objJson.SelectToken("usernamelist")));
                for (int i = 0; i < objUserNameJson.Count(); i++)
                {
                    var objUserList = _appDbContext.Users.Select(t => new { UserIdVal = t.UserId, EmailVal = t.Email, PhoneNumberVal = t.PhoneNumber }).Where(s => s.EmailVal == Convert.ToString(objUserNameJson[i])).OrderByDescending(p => p.UserIdVal).FirstOrDefault();
                    if (objUserList == null || string.IsNullOrEmpty(Convert.ToString(objUserList.PhoneNumberVal)))
                    {
                        strErrorUsername += Convert.ToString(objUserNameJson[i]) + ",";
                    }
                    else
                    {
                        string strInvitationCode = clsCommon.GetRandomAlphaNumeric();

                        UserInvitationDetails objUserInvitationDetails = new UserInvitationDetails();
                        objUserInvitationDetails.SenderUserId = strUserId;
                        objUserInvitationDetails.ReceiverUesrId = objUserList.UserIdVal;
                        objUserInvitationDetails.UserChatGroupId = Convert.ToInt32(objJson.SelectToken("groupid"));
                        objUserInvitationDetails.InvitationCode = strInvitationCode;
                        objUserInvitationDetails.CreationTime = DateTime.UtcNow;
                        objUserInvitationDetails.ExpiryTime = DateTime.UtcNow.AddMinutes(30);
                        objUserInvitationDetails.RequestValue = "sms";
                        await _appDbContext.UserInvitationDetails.AddAsync(objUserInvitationDetails);

                        int returnVal = await _appDbContext.SaveChangesAsync();
                        if (returnVal > 0)
                        {
                            string strMessage = "Nurchure  App : Your invitation code is " + strInvitationCode + ".";
                            string strMailStatus = new clsSMS(configuration).SendSMS(Convert.ToString(objUserList.PhoneNumberVal), strMessage);
                            if (strMailStatus == "Success")
                            {
                                //return Ok(new { Status = "OK", Description = "An SMS with invitation code has been sent to " + Convert.ToString(objUserList.PhoneNumberVal) + "." });
                                strSuccessUsername += Convert.ToString(objUserNameJson[i]) + ",";
                            }
                            else if (strMailStatus == "Error")
                            {
                                strErrorUsername += Convert.ToString(objUserNameJson[i]) + ",";
                            }
                            else
                            {
                                strErrorUsername += Convert.ToString(objUserNameJson[i]) + ",";
                            }
                        }
                        else
                        {
                            return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                        }
                    }
                }

                return Ok(new { Status = "OK", SuccessUsernames = strSuccessUsername.TrimEnd(','), ErrorUsernames = strErrorUsername.TrimEnd(',') });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/chat/SendSMSInvite
        [HttpPost]
        [Route("SendSMSInvite")]
        public async Task<ActionResult> SendSMSInvite([FromBody] JObject objJson)
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

                //string strInvitationCode = clsCommon.GetRandomNumber();
                string strInvitationCode = clsCommon.GetRandomNumeric();

                UserSMSInvitationDetails objUserSMSInvitationDetails = new UserSMSInvitationDetails();
                objUserSMSInvitationDetails.SenderUserId = strUserId;
                objUserSMSInvitationDetails.ReceiverPhoneNumber = Convert.ToString(objJson.SelectToken("receiverphonenumber"));
                objUserSMSInvitationDetails.UserChatGroupId = Convert.ToInt32(objJson.SelectToken("groupid"));
                objUserSMSInvitationDetails.InvitationCode = strInvitationCode;
                objUserSMSInvitationDetails.CreationTime = DateTime.UtcNow;
                objUserSMSInvitationDetails.ExpiryTime = DateTime.UtcNow.AddDays(30);
                await _appDbContext.UserSMSInvitationDetails.AddAsync(objUserSMSInvitationDetails);

                int returnVal = await _appDbContext.SaveChangesAsync();
                if (returnVal > 0)
                {
                    string strMessage = "Nurchure  App : Your registratin link is " + Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + strInvitationCode;
                    string strMailStatus = new clsSMS(configuration).SendSMS(Convert.ToString(objJson.SelectToken("receiverphonenumber")), strMessage);
                    if (strMailStatus == "Success")
                    {
                        return Ok(new { Status = "OK", Description = "An SMS with registration link has been sent to " + Convert.ToString(objJson.SelectToken("receiverphonenumber")) + "." });
                    }
                    else if (strMailStatus == "Error")
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = strMailStatus }));
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

        // POST api/chat/SearchChatGroupById
        [HttpPost]
        [Route("SearchChatGroupById")]
        public ActionResult SearchChatGroupById([FromBody] JObject objJson)
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

                var objGroupChatMessages = (from ucgm in _appDbContext.UserChatGroupMessages
                                            where ucgm.GroupId == Convert.ToInt32(objJson.SelectToken("groupid")) && ucgm.Message.Contains(Convert.ToString(objJson.SelectToken("searchstring")))
                                            select new
                                            {
                                                Id = Convert.ToString(ucgm.Id),
                                                GroupId = Convert.ToString(ucgm.GroupId),
                                                UserId = ucgm.UserId,
                                                Message = ucgm.Message,
                                                MessageType = ucgm.MessageType,
                                                AddedDate = Convert.ToString(ucgm.AddedDate)
                                            });

                if (objGroupChatMessages == null || objGroupChatMessages.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Search string doesn’t exist" }));
                }

                return Ok(new { Status = "OK", SearchList = objGroupChatMessages });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/chat/SearchFriendByString
        [HttpPost]
        [Route("SearchFriendByString")]
        public ActionResult SearchFriendByString([FromBody] JObject objJson)
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

                var objUserChatGroupFriends = (from ucgf in _appDbContext.UserChatGroupFriends
                                               join u in _appDbContext.Users on ucgf.UserId equals u.UserId into DetailsUsers
                                               from u1 in DetailsUsers.DefaultIfEmpty()
                                               where ucgf.GroupId == Convert.ToInt32(objJson.SelectToken("groupid")) &&
                                               (u1.FirstName.Contains(Convert.ToString(objJson.SelectToken("searchstring"))) ||
                                               u1.LastName.Contains(Convert.ToString(objJson.SelectToken("searchstring"))) ||
                                               u1.Email.Contains(Convert.ToString(objJson.SelectToken("searchstring"))) ||
                                               u1.PhoneNumber.Contains(Convert.ToString(objJson.SelectToken("searchstring"))))
                                               select new
                                               {
                                                   Id = Convert.ToString(ucgf.Id),
                                                   GroupId = Convert.ToString(ucgf.GroupId),
                                                   AddedUserId = ucgf.AddedUserId,
                                                   UserId = ucgf.UserId,
                                                   AddedDate = Convert.ToString(ucgf.AddedDate),
                                                   AdminRights = Convert.ToString(ucgf.AdminRights),
                                                   AdminRightsAddedDate = Convert.ToString(ucgf.AdminRightsAddedDate),
                                                   LastUpdatedAdminRights = Convert.ToString(ucgf.LastUpdatedAdminRights),
                                                   UserFirstName = u1.FirstName,
                                                   UserLastName = u1.LastName,
                                                   UserEmail = u1.Email,
                                                   UserPhoneNumber = u1.PhoneNumber,
                                               });

                if (objUserChatGroupFriends == null || objUserChatGroupFriends.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Search string doesn’t exist" }));
                }

                return Ok(new { Status = "OK", SearchList = objUserChatGroupFriends });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/chat/MuteChatGroupNotificationById
        [HttpPost]
        [Route("MuteChatGroupNotificationById")]
        public async Task<ActionResult> MuteChatGroupNotificationById([FromBody] JObject objJson)
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

                var objUserChatGroupFriends = (from ucgf in _appDbContext.UserChatGroupFriends
                                               where ucgf.GroupId == Convert.ToInt32(objJson.SelectToken("groupid")) && ucgf.UserId == strUserId
                                               select ucgf);

                if (objUserChatGroupFriends == null || objUserChatGroupFriends.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Group doesn’t exist or Invalid User." }));
                }

                var objMuteChatGroupNotification = (from mcnd in _appDbContext.MuteChatGroupNotificationDetails
                                                    where (mcnd.GroupId == Convert.ToInt32(objJson.SelectToken("groupid")) && mcnd.UserId == strUserId)
                                                    select mcnd).ToList<MuteChatGroupNotificationDetails>();

                if (objMuteChatGroupNotification.Count() > 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "You already mute the group notification." }));
                }

                MuteChatGroupNotificationDetails objMuteChatGroupNotificationDetails = new MuteChatGroupNotificationDetails();
                objMuteChatGroupNotificationDetails.GroupId = Convert.ToInt32(objJson.SelectToken("groupid"));
                objMuteChatGroupNotificationDetails.UserId = strUserId;
                objMuteChatGroupNotificationDetails.AddedTime = DateTime.UtcNow;
                objMuteChatGroupNotificationDetails.ExpiryTime = DateTime.UtcNow.AddHours(Convert.ToInt32(objJson.SelectToken("mutehr")));
                await _appDbContext.MuteChatGroupNotificationDetails.AddAsync(objMuteChatGroupNotificationDetails);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Your group notification muted successfully." });
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

        // POST api/chat/UnMuteChatGroupNotificationById
        [HttpPost]
        [Route("UnMuteChatGroupNotificationById")]
        public async Task<ActionResult> UnMuteChatGroupNotificationById([FromBody] JObject objJson)
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

                var objMuteChatGroupNotificationDetails = (from mcnd in _appDbContext.MuteChatGroupNotificationDetails
                                                           where (mcnd.GroupId == Convert.ToInt32(objJson.SelectToken("groupid")) && mcnd.UserId == strUserId)
                                                           orderby mcnd.Id descending
                                                           select mcnd).ToList<MuteChatGroupNotificationDetails>();

                if (objMuteChatGroupNotificationDetails == null || objMuteChatGroupNotificationDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Your group not mute." }));
                }
                else
                {
                    _appDbContext.MuteChatGroupNotificationDetails.Remove(objMuteChatGroupNotificationDetails[0]);
                    int returnVal = await _appDbContext.SaveChangesAsync();
                    if (returnVal > 0)
                    {
                        return Ok(new { Status = "OK", Detail = "Group notification unmute successfully." });
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

        // POST api/chat/MuteAllChatGroupNotification
        [HttpPost]
        [Route("MuteAllChatGroupNotification")]
        public async Task<ActionResult> MuteAllChatGroupNotification([FromBody] JObject objJson)
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

                var objMuteChatGroupNotification = (from mcnd in _appDbContext.MuteChatGroupNotificationDetails
                                                    where (mcnd.UserId == strUserId)
                                                    select mcnd).ToList<MuteChatGroupNotificationDetails>();

                if (objMuteChatGroupNotification.Count() > 0)
                {
                    _appDbContext.MuteChatGroupNotificationDetails.RemoveRange(_appDbContext.MuteChatGroupNotificationDetails.Where(x => x.UserId == strUserId));
                    await _appDbContext.SaveChangesAsync();
                }

                var objUserChatGroupFriends = (from ucgf in _appDbContext.UserChatGroupFriends
                                               where ucgf.UserId == strUserId
                                               select ucgf).ToList<UserChatGroupFriends>();

                if (objUserChatGroupFriends == null || objUserChatGroupFriends.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "No any group available." }));
                }

                bool bnlErrorflag = false;
                foreach (UserChatGroupFriends objItem in objUserChatGroupFriends)
                {
                    MuteChatGroupNotificationDetails objMuteChatGroupNotificationDetails = new MuteChatGroupNotificationDetails();
                    objMuteChatGroupNotificationDetails.GroupId = objItem.GroupId;
                    objMuteChatGroupNotificationDetails.UserId = strUserId;
                    objMuteChatGroupNotificationDetails.AddedTime = DateTime.UtcNow;
                    objMuteChatGroupNotificationDetails.ExpiryTime = DateTime.UtcNow.AddHours(Convert.ToInt32(objJson.SelectToken("mutehr")));
                    await _appDbContext.MuteChatGroupNotificationDetails.AddAsync(objMuteChatGroupNotificationDetails);
                    int returnVal = await _appDbContext.SaveChangesAsync();

                    if (returnVal <= 0)
                    {
                        bnlErrorflag = true;
                    }
                }

                if (bnlErrorflag == false)
                {
                    return Ok(new { Status = "OK", Detail = "Your all group notification muted successfully." });
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Some groups are not muted, Please try again." }));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/chat/UnMuteAllChatGroupNotification
        [HttpPost]
        [Route("UnMuteAllChatGroupNotification")]
        public async Task<ActionResult> UnMuteAllChatGroupNotification()
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

                var objMuteChatGroupNotification = (from mcnd in _appDbContext.MuteChatGroupNotificationDetails
                                                    where (mcnd.UserId == strUserId)
                                                    select mcnd).ToList<MuteChatGroupNotificationDetails>();

                if (objMuteChatGroupNotification == null || objMuteChatGroupNotification.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Your all group notification already unmuted." }));
                }

                _appDbContext.MuteChatGroupNotificationDetails.RemoveRange(_appDbContext.MuteChatGroupNotificationDetails.Where(x => x.UserId == strUserId));
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Your all group notification unmuted successfully." });
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

        // POST api/chat/ExitGroupById
        [HttpPost]
        [Route("ExitGroupById")]
        public async Task<ActionResult> ExitGroupById([FromBody] JObject objJson)
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

                UserChatGroupFriends objUserChatGroupFriends = _appDbContext.UserChatGroupFriends
                                .Where(ucgf => ucgf.GroupId == Convert.ToInt32(objJson.SelectToken("groupid")) && ucgf.UserId == strUserId)
                                .FirstOrDefault();

                if (objUserChatGroupFriends == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "User doesn’t exist in group." }));
                }
                else
                {
                    _appDbContext.UserChatGroupFriends.Remove(objUserChatGroupFriends);
                    int returnVal = await _appDbContext.SaveChangesAsync();
                    if (returnVal > 0)
                    {
                        return Ok(new { Status = "OK", Detail = "Your are removed from group." });
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

        // POST api/chat/RemoveUserFromGroupById
        [HttpPost]
        [Route("RemoveUserFromGroupById")]
        public async Task<ActionResult> RemoveUserFromGroupById([FromBody] JObject objJson)
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

                UserChatGroupFriends objUserChatGroupFriends = _appDbContext.UserChatGroupFriends
                                .Where(ucgf => ucgf.GroupId == Convert.ToInt32(objJson.SelectToken("groupid")) && ucgf.UserId == strUserId)
                                .FirstOrDefault();

                if (objUserChatGroupFriends == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "You are not in group." }));
                }

                if (objUserChatGroupFriends.AdminRights == false)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Only admin can remove the user." }));
                }

                UserChatGroupFriends objUserChatGroupFriends1 = _appDbContext.UserChatGroupFriends
                                .Where(ucgf => ucgf.GroupId == Convert.ToInt32(objJson.SelectToken("groupid")) && ucgf.UserId == Convert.ToString(objJson.SelectToken("userid")))
                                .FirstOrDefault();

                if (objUserChatGroupFriends1 == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "User doesn'y exist in group." }));
                }

                if (objUserChatGroupFriends1.AdminRights == true)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "You can only remove non-admin user." }));
                }

                _appDbContext.UserChatGroupFriends.Remove(objUserChatGroupFriends1);
                int returnVal = await _appDbContext.SaveChangesAsync();
                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "User removed from group." });
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

        // POST api/chat/SearchChatById
        [HttpPost]
        [Route("SearchChatById")]
        public ActionResult SearchChatById([FromBody] JObject objJson)
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

                var objChatMessages = (from cm in _appDbContext.ChatMessages
                                       where cm.ChatId == Convert.ToInt32(objJson.SelectToken("chatid")) && cm.Message.Contains(Convert.ToString(objJson.SelectToken("searchstring")))
                                       select new
                                       {
                                           Id = Convert.ToString(cm.Id),
                                           ChatId = Convert.ToString(cm.ChatId),
                                           UserId = cm.UserId,
                                           Message = cm.Message,
                                           MessageType = cm.MessageType,
                                           AddedDate = Convert.ToString(cm.AddedDate)
                                       });

                if (objChatMessages == null || objChatMessages.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Search string doesn’t exist" }));
                }

                return Ok(new { Status = "OK", SearchList = objChatMessages });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/chat/MuteChatNotificationByChatId
        [HttpPost]
        [Route("MuteChatNotificationByChatId")]
        public async Task<ActionResult> MuteChatNotificationByChatId([FromBody] JObject objJson)
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

                var objUserFriendList = (from ufl in _appDbContext.UserFriendList
                                         where ufl.Id == Convert.ToInt32(objJson.SelectToken("chatid")) && (ufl.UserId == strUserId || ufl.FriendId == strUserId)
                                         select ufl);

                if (objUserFriendList == null || objUserFriendList.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Chat doesn’t exist or Invalid User." }));
                }

                var objMuteChatNotification = (from mcnd in _appDbContext.MuteChatNotificationDetails
                                               where (mcnd.ChatId == Convert.ToInt32(objJson.SelectToken("chatid")) && mcnd.UserId == strUserId)
                                               select mcnd).ToList<MuteChatNotificationDetails>();

                if (objMuteChatNotification.Count() > 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "You already mute the chat notification." }));
                }

                MuteChatNotificationDetails objMuteChatNotificationDetails = new MuteChatNotificationDetails();
                objMuteChatNotificationDetails.ChatId = Convert.ToInt32(objJson.SelectToken("chatid"));
                objMuteChatNotificationDetails.UserId = strUserId;
                objMuteChatNotificationDetails.AddedTime = DateTime.UtcNow;
                objMuteChatNotificationDetails.ExpiryTime = DateTime.UtcNow.AddHours(Convert.ToInt32(objJson.SelectToken("mutehr")));
                await _appDbContext.MuteChatNotificationDetails.AddAsync(objMuteChatNotificationDetails);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Your chat notification muted successfully." });
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

        // POST api/chat/UnMuteChatNotificationByChatId
        [HttpPost]
        [Route("UnMuteChatNotificationByChatId")]
        public async Task<ActionResult> UnMuteChatNotificationByChatId([FromBody] JObject objJson)
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

                var objMuteChatNotificationDetails = (from mcnd in _appDbContext.MuteChatNotificationDetails
                                                      where (mcnd.ChatId == Convert.ToInt32(objJson.SelectToken("chatid")) && mcnd.UserId == strUserId)
                                                      orderby mcnd.Id descending
                                                      select mcnd).ToList<MuteChatNotificationDetails>();

                if (objMuteChatNotificationDetails == null || objMuteChatNotificationDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Your chat not mute." }));
                }
                else
                {
                    _appDbContext.MuteChatNotificationDetails.Remove(objMuteChatNotificationDetails[0]);
                    int returnVal = await _appDbContext.SaveChangesAsync();
                    if (returnVal > 0)
                    {
                        return Ok(new { Status = "OK", Detail = "Chat notification unmute successfully." });
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

        // POST api/chat/MuteAllChatNotificationByUserId
        [HttpPost]
        [Route("MuteAllChatNotificationByUserId")]
        public async Task<ActionResult> MuteAllChatNotificationByUserId([FromBody] JObject objJson)
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

                var objMuteChatNotification = (from mcnd in _appDbContext.MuteChatNotificationDetails
                                               where (mcnd.UserId == strUserId)
                                               select mcnd).ToList<MuteChatNotificationDetails>();

                if (objMuteChatNotification.Count() > 0)
                {
                    _appDbContext.MuteChatNotificationDetails.RemoveRange(_appDbContext.MuteChatNotificationDetails.Where(x => x.UserId == strUserId));
                    await _appDbContext.SaveChangesAsync();
                }

                var objUserFriendList = (from ufl in _appDbContext.UserFriendList
                                         where (ufl.UserId == strUserId || ufl.FriendId == strUserId)
                                         select ufl).ToList<UserFriendList>();

                if (objUserFriendList == null || objUserFriendList.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "No any chat available." }));
                }

                bool bnlErrorflag = false;
                foreach (UserFriendList objItem in objUserFriendList)
                {
                    MuteChatNotificationDetails objMuteChatNotificationDetails = new MuteChatNotificationDetails();
                    objMuteChatNotificationDetails.ChatId = objItem.Id;
                    objMuteChatNotificationDetails.UserId = strUserId;
                    objMuteChatNotificationDetails.AddedTime = DateTime.UtcNow;
                    objMuteChatNotificationDetails.ExpiryTime = DateTime.UtcNow.AddHours(Convert.ToInt32(objJson.SelectToken("mutehr")));
                    await _appDbContext.MuteChatNotificationDetails.AddAsync(objMuteChatNotificationDetails);
                    int returnVal = await _appDbContext.SaveChangesAsync();

                    if (returnVal <= 0)
                    {
                        bnlErrorflag = true;
                    }
                }

                if (bnlErrorflag == false)
                {
                    return Ok(new { Status = "OK", Detail = "Your all chat notification muted successfully." });
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Some chats are not muted, Please try again." }));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/chat/UnMuteAllChatNotificationByUserId
        [HttpPost]
        [Route("UnMuteAllChatNotificationByUserId")]
        public async Task<ActionResult> UnMuteAllChatNotificationByUserId()
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

                var objMuteChatNotification = (from mcnd in _appDbContext.MuteChatNotificationDetails
                                               where (mcnd.UserId == strUserId)
                                               select mcnd).ToList<MuteChatNotificationDetails>();

                if (objMuteChatNotification == null || objMuteChatNotification.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Your all chat notification already unmuted." }));
                }

                _appDbContext.MuteChatNotificationDetails.RemoveRange(_appDbContext.MuteChatNotificationDetails.Where(x => x.UserId == strUserId));
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Your all chat notification unmuted successfully." });
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

        // POST api/chat/GetMuteChatNotificationDetails
        [HttpPost]
        [Route("GetMuteChatNotificationDetails")]
        public ActionResult GetMuteChatNotificationDetails()
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

                var objMuteChatNotificationDetails = (from mcnd in _appDbContext.MuteChatNotificationDetails
                                                      where mcnd.UserId == strUserId
                                                      select mcnd);

                if (objMuteChatNotificationDetails == null || objMuteChatNotificationDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "No any muted chat." }));
                }

                return Ok(new { Status = "OK", MutedChat = objMuteChatNotificationDetails });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/chat/GetMuteChatGroupNotificationDetails
        [HttpPost]
        [Route("GetMuteChatGroupNotificationDetails")]
        public ActionResult GetMuteChatGroupNotificationDetails()
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

                var objMuteChatGroupNotificationDetails = (from mcgnd in _appDbContext.MuteChatGroupNotificationDetails
                                                           where mcgnd.UserId == strUserId
                                                           select mcgnd);

                if (objMuteChatGroupNotificationDetails == null || objMuteChatGroupNotificationDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "No any muted chat group." }));
                }

                return Ok(new { Status = "OK", MutedChatGroup = objMuteChatGroupNotificationDetails });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        public List<PersonalityMatchResult> GetPersonalityMatchResult(string strUserId)
        {
            try
            {
                var objUserPersonalitySummaryDetail = (from ups in _appDbContext.UserPersonalitySummary
                                                       where ups.UserId == strUserId
                                                       select ups).ToList<UserPersonalitySummary>();

                if (objUserPersonalitySummaryDetail == null || objUserPersonalitySummaryDetail.Count() <= 0)
                {
                    return null;
                }

                var objUsers = (from u in _appDbContext.Users
                                select u);

                if (objUsers == null || objUsers.Count() <= 0)
                {
                    return null;
                }

                var objUserPersonalitySummary = new List<PersonalityMatchResult>();

                if (objUsers.Count() < 1000)
                {
                    objUserPersonalitySummary = (from ups in _appDbContext.UserPersonalitySummary
                                                 join u in _appDbContext.Users on ups.UserId equals u.UserId into DetailsUsers
                                                 from u1 in DetailsUsers.DefaultIfEmpty()
                                                 join ula in _appDbContext.UserLoginAccount on ups.UserId equals ula.UserId into DetailsUserLoginAccount
                                                 from ula1 in DetailsUserLoginAccount.DefaultIfEmpty()
                                                     //where ups.UserId != strUserId && ups.PrefferedMBTIMatch.StartsWith(objUserPersonalitySummaryDetail[0].PrefferedMBTIMatch.Substring(0,3))
                                                 where ups.UserId != strUserId && ups.PrefferedMBTIMatch.Intersect(objUserPersonalitySummaryDetail[0].PrefferedMBTIMatch).Count() >= 3
                                                 select new PersonalityMatchResult()
                                                 {
                                                     UserId = ups.UserId,
                                                     PrefferedMBTIMatch = ups.PrefferedMBTIMatch,
                                                     GeoLocationLatitude = u1.GeoLocationLatitude,
                                                     GeoLocationLongitude = u1.GeoLocationLongitude,
                                                     Age = u1.Age,
                                                     CreatedDate = ula1.CreatedDate
                                                 }).ToList();
                }
                else if (objUsers.Count() >= 1000 && objUsers.Count() < 10000)
                {
                    objUserPersonalitySummary = (from ups in _appDbContext.UserPersonalitySummary
                                                 join u in _appDbContext.Users on ups.UserId equals u.UserId into DetailsUsers
                                                 from u1 in DetailsUsers.DefaultIfEmpty()
                                                 join ula in _appDbContext.UserLoginAccount on ups.UserId equals ula.UserId into DetailsUserLoginAccount
                                                 from ula1 in DetailsUserLoginAccount.DefaultIfEmpty()
                                                     //where ups.UserId != strUserId && ups.PrefferedMBTIMatch == objUserPersonalitySummaryDetail[0].PrefferedMBTIMatch
                                                 where ups.UserId != strUserId && ups.PrefferedMBTIMatch.Intersect(objUserPersonalitySummaryDetail[0].PrefferedMBTIMatch).Count() == 4
                                                 select new PersonalityMatchResult()
                                                 {
                                                     UserId = ups.UserId,
                                                     PrefferedMBTIMatch = ups.PrefferedMBTIMatch,
                                                     GeoLocationLatitude = u1.GeoLocationLatitude,
                                                     GeoLocationLongitude = u1.GeoLocationLongitude,
                                                     Age = u1.Age,
                                                     CreatedDate = ula1.CreatedDate
                                                 }).ToList();
                }

                //return Ok(new { Status = "OK", Count = objUserPersonalitySummary.Count(), MatchResult = objUserPersonalitySummary });

                var objLoginUser = objUsers.Where(t => t.UserId == strUserId).ToList();

                var objDistanceResult = from objItem in objUserPersonalitySummary
                                        select new PersonalityMatchResult()
                                        {
                                            UserId = objItem.UserId,
                                            PrefferedMBTIMatch = objItem.PrefferedMBTIMatch,
                                            GeoLocationLatitude = objItem.GeoLocationLatitude,
                                            GeoLocationLongitude = objItem.GeoLocationLongitude,
                                            Age = objItem.Age,
                                            CreatedDate = objItem.CreatedDate,
                                            Distance = Math.Ceiling(Math.Sqrt(
                                                        Math.Pow(111.2 * (objItem.GeoLocationLatitude - objLoginUser[0].GeoLocationLatitude), 2) +
                                                        Math.Pow(111.2 * (objLoginUser[0].GeoLocationLongitude - objItem.GeoLocationLongitude) * Math.Cos(objItem.GeoLocationLatitude / 57.3), 2)))
                                        };

                //Get 50 KM distance users
                objDistanceResult = objDistanceResult.Where(t => t.Distance <= 50);

                //Get users as per age
                if (objLoginUser[0].Age >= 20 && objLoginUser[0].Age <= 24)
                {
                    objDistanceResult = objDistanceResult.Where(t => (t.Age >= 20 && t.Age <= 24));
                }
                else if (objLoginUser[0].Age >= 25 && objLoginUser[0].Age <= 29)
                {
                    objDistanceResult = objDistanceResult.Where(t => (t.Age >= 25 && t.Age <= 29));
                }
                else if (objLoginUser[0].Age >= 30 && objLoginUser[0].Age <= 34)
                {
                    objDistanceResult = objDistanceResult.Where(t => (t.Age >= 30 && t.Age <= 34));
                }
                else if (objLoginUser[0].Age >= 35 && objLoginUser[0].Age <= 39)
                {
                    objDistanceResult = objDistanceResult.Where(t => (t.Age >= 35 && t.Age <= 39));
                }
                else if (objLoginUser[0].Age >= 40 && objLoginUser[0].Age <= 44)
                {
                    objDistanceResult = objDistanceResult.Where(t => (t.Age >= 40 && t.Age <= 44));
                }
                else if (objLoginUser[0].Age >= 45 && objLoginUser[0].Age <= 49)
                {
                    objDistanceResult = objDistanceResult.Where(t => (t.Age >= 45 && t.Age <= 49));
                }

                var objUserActivities = (from ua in _appDbContext.UserActivities
                                         where ua.UserId == strUserId
                                         orderby ua.Rank
                                         select ua).Take(3).ToList<UserActivities>();

                if (objUserActivities == null || objUserActivities.Count() <= 0)
                {
                    return null;
                }

                var objMatchResult = new List<PersonalityMatchResult>();
                foreach (var objItem in objDistanceResult)
                {
                    var objUserActivitiesChild = (from ua in _appDbContext.UserActivities
                                                  where ua.UserId == objItem.UserId
                                                  orderby ua.Rank
                                                  select ua).Take(3).ToList<UserActivities>();

                    int recCount = objUserActivitiesChild.Select(t => t.ActivityId).Intersect(objUserActivities.Select(x => x.ActivityId)).Count();
                    if (recCount > 0)
                    {
                        objMatchResult.Add(objItem);
                    }
                }

                objMatchResult = objMatchResult.OrderBy(t => t.CreatedDate).Take(10).ToList();

                return objMatchResult;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // POST api/chat/SearchGroup
        [HttpPost]
        [Route("SearchGroup")]
        public async Task<ActionResult> SearchGroup([FromBody] JObject objJson)
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

                string strGroupName = "";

                var objUserPersonalitySummaryDetail = (from ups in _appDbContext.UserPersonalitySummary
                                                       where ups.UserId == strUserId
                                                       select ups).ToList<UserPersonalitySummary>();

                strGroupName = objUserPersonalitySummaryDetail[0].PrefferedMBTIMatch;

                if (string.IsNullOrEmpty(strGroupName))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid MBT Match" }));
                }

                MBTIPersonalities objMBTIPersonalities = _appDbContext.MBTIPersonalities
                                .Where(mbt => mbt.Name == strGroupName).AsNoTracking().FirstOrDefault();

                if (Convert.ToString(objJson.SelectToken("LocationAccess")) == "yes")
                {
                    var objUsersDetail = (from u in _appDbContext.Users
                                          where u.UserId == strUserId
                                          select u).ToList<Users>();

                    var objUsersGeoLocations = (from ucg in _appDbContext.UserChatGroup
                                                join u in _appDbContext.Users on ucg.CreatedUserId equals u.UserId into DetailsUsers
                                                from u1 in DetailsUsers.DefaultIfEmpty()
                                                where ucg.MBITName == strGroupName
                                                orderby ucg.CreatedDate descending
                                                select new
                                                {
                                                    GroupId = ucg.Id,
                                                    GeoLocationLatitude = u1.GeoLocationLatitude,
                                                    GeoLocationLongitude = u1.GeoLocationLongitude,
                                                    Distance = GetDistance(objUsersDetail[0].GeoLocationLongitude, objUsersDetail[0].GeoLocationLatitude, u1.GeoLocationLongitude, u1.GeoLocationLatitude),
                                                    FriendsCount = (
                                                                        from ucgf in _appDbContext.UserChatGroupFriends
                                                                        where (ucgf.GroupId == ucg.Id)
                                                                        select ucgf.UserId
                                                                   ).Count()
                                                }).AsQueryable();

                    objUsersGeoLocations = objUsersGeoLocations.Where(e =>
                    (e.Distance <= Convert.ToInt32(configuration.GetSection("appSettings").GetSection("SearchDistance").Value)));

                    objUsersGeoLocations = objUsersGeoLocations.Where(e => (e.FriendsCount < 11));

                    if (objUsersGeoLocations == null || objUsersGeoLocations.Count() <= 0)
                    {
                        UserChatGroup objUserChatGroup = new UserChatGroup();
                        objUserChatGroup.GroupName = Convert.ToString(objMBTIPersonalities.Description);

                        //Image is for testing
                        objUserChatGroup.GroupImage = Convert.ToString(configuration.GetSection("appSettings").GetSection("SampleGroupImage").Value);
                        objUserChatGroup.CreatedUserId = strUserId;
                        objUserChatGroup.GroupType = "Public";
                        objUserChatGroup.ActivityId = 0;
                        objUserChatGroup.CreatedDate = DateTime.UtcNow;
                        objUserChatGroup.MBITName = strGroupName;
                        await _appDbContext.UserChatGroup.AddAsync(objUserChatGroup);
                        int returnVal = await _appDbContext.SaveChangesAsync();

                        if (returnVal > 0)
                        {
                            int intUserChatGroupId = objUserChatGroup.Id;

                            UserChatGroupFriends objUserChatGroupFriends = new UserChatGroupFriends();
                            objUserChatGroupFriends.GroupId = intUserChatGroupId;
                            objUserChatGroupFriends.AddedUserId = "0";
                            objUserChatGroupFriends.UserId = strUserId;
                            objUserChatGroupFriends.AddedDate = DateTime.UtcNow;
                            objUserChatGroupFriends.AdminRights = true;
                            objUserChatGroupFriends.AdminRightsAddedDate = DateTime.UtcNow;
                            await _appDbContext.UserChatGroupFriends.AddAsync(objUserChatGroupFriends);
                            returnVal = await _appDbContext.SaveChangesAsync();

                            if (returnVal > 0)
                            {
                                return Ok(new { Status = "OK", UserGroupDetail = "Group created." });
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
                        //Put it in wait list
                        //UsersWaitList objUsersWaitList = new UsersWaitList();
                        //objUsersWaitList.UserId = strUserId;
                        //objUsersWaitList.MBITName = strGroupName;
                        //objUsersWaitList.AddedDate = DateTime.UtcNow;
                        //objUsersWaitList.Active = true;
                        //await _appDbContext.UsersWaitList.AddAsync(objUsersWaitList);
                        //await _appDbContext.SaveChangesAsync();

                        //return Ok(new { Status = "OK", UserGroupDetail = "User added in Wait List." });
                    }
                    else
                    {
                        UserChatGroupFriends objUserChatGroupFriends = new UserChatGroupFriends();
                        objUserChatGroupFriends.GroupId = objUsersGeoLocations.First().GroupId;
                        objUserChatGroupFriends.AddedUserId = "0";
                        objUserChatGroupFriends.UserId = strUserId;
                        objUserChatGroupFriends.AddedDate = DateTime.UtcNow;
                        objUserChatGroupFriends.AdminRights = false;
                        objUserChatGroupFriends.AdminRightsAddedDate = DateTime.UtcNow;
                        await _appDbContext.UserChatGroupFriends.AddAsync(objUserChatGroupFriends);
                        await _appDbContext.SaveChangesAsync();

                        return Ok(new { Status = "OK", UserGroupDetail = "User added in Group." });
                    }
                }
                else
                {
                    UserChatGroup objUserChatGroup = new UserChatGroup();
                    objUserChatGroup.GroupName = Convert.ToString(objMBTIPersonalities.Description);

                    //Image is for testing
                    objUserChatGroup.GroupImage = Convert.ToString(configuration.GetSection("appSettings").GetSection("SampleGroupImage").Value);
                    objUserChatGroup.CreatedUserId = strUserId;
                    objUserChatGroup.GroupType = "Public";
                    objUserChatGroup.ActivityId = 0;
                    objUserChatGroup.CreatedDate = DateTime.UtcNow;
                    objUserChatGroup.MBITName = strGroupName;
                    await _appDbContext.UserChatGroup.AddAsync(objUserChatGroup);
                    int returnVal = await _appDbContext.SaveChangesAsync();

                    if (returnVal > 0)
                    {
                        int intUserChatGroupId = objUserChatGroup.Id;

                        UserChatGroupFriends objUserChatGroupFriends = new UserChatGroupFriends();
                        objUserChatGroupFriends.GroupId = intUserChatGroupId;
                        objUserChatGroupFriends.AddedUserId = "0";
                        objUserChatGroupFriends.UserId = strUserId;
                        objUserChatGroupFriends.AddedDate = DateTime.UtcNow;
                        objUserChatGroupFriends.AdminRights = true;
                        objUserChatGroupFriends.AdminRightsAddedDate = DateTime.UtcNow;
                        await _appDbContext.UserChatGroupFriends.AddAsync(objUserChatGroupFriends);
                        returnVal = await _appDbContext.SaveChangesAsync();

                        if (returnVal > 0)
                        {
                            return Ok(new { Status = "OK", UserGroupDetail = "Group created." });
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
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/chat/GetUserGroup
        [HttpPost]
        [Route("GetUserGroup")]
        public ActionResult GetUserGroup()
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

                int intRecCount = _appDbContext.UserChatGroup.Where(t => t.CreatedUserId == strUserId).Count();

                if (intRecCount == 0)
                {
                    intRecCount = _appDbContext.UserChatGroupFriends.Where(t => t.UserId == strUserId).Count();
                }

                if (intRecCount > 0)
                {
                    var objUserGroupDetails = (from ucg in _appDbContext.UserChatGroup
                                               join ucgf in _appDbContext.UserChatGroupFriends on ucg.Id equals ucgf.GroupId into DetailsUserChatGroupFriends
                                               from ucgf1 in DetailsUserChatGroupFriends.DefaultIfEmpty()
                                                   //where (ucgf1.AddedUserId == strUserId || ucgf1.UserId == strUserId)
                                               where (ucgf1.UserId == strUserId)
                                               select new
                                               {
                                                   GroupId = ucg.Id,
                                                   GroupName = ucg.GroupName,
                                                   GroupImage = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("GroupImages").Value) + ucg.GroupImage),
                                                   GroupType = ucg.GroupType,
                                                   GroupCreatedUserId = ucg.CreatedUserId,
                                                   GroupCreatedDate = ucg.CreatedDate,

                                                   LastMessageDetails = (from ucgm in _appDbContext.UserChatGroupMessages
                                                                         where (ucgm.GroupId == ucg.Id)
                                                                         orderby ucgm.Id descending
                                                                         select new
                                                                         {
                                                                             Id = ucgm.Id,
                                                                             LastMessageUserId = ucgm.UserId,
                                                                             LastMessage = ucgm.Message,
                                                                             LastMessageType = ucgm.MessageType,
                                                                             LastMessageAddedDate = ucgm.AddedDate
                                                                             //}).Take(1).ToList(),
                                                                         }).Take(1),
                                               });

                    if (objUserGroupDetails == null || objUserGroupDetails.Count() <= 0)
                    {
                        return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid username" }));
                    }

                    return Ok(new { Status = "OK", UserGroup = objUserGroupDetails });
                }
                else
                {
                    return Ok(new { Status = "OK", Message = "No Group created" });

                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/chat/GetSharedMediaList
        [HttpPost]
        [Route("GetSharedMediaList")]
        public ActionResult GetSharedMediaList([FromBody] JObject objJson)
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

                var objUserChatGroupImageDetail = (from ucgm in _appDbContext.UserChatGroupMessages
                                                   where ((ucgm.GroupId == Convert.ToInt32(objJson.SelectToken("groupid"))) && ucgm.MessageType == "I")
                                                   select new
                                                   {
                                                       ChatImageUrl = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("ChatGroup").Value) + Convert.ToString(ucgm.Message)),
                                                       UploadedDate = Convert.ToString(ucgm.AddedDate),
                                                   });

                if (objUserChatGroupImageDetail == null || objUserChatGroupImageDetail.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "User chat group images found." }));
                }

                return Ok(new { Status = "OK", UserChatGroupImageDetail = objUserChatGroupImageDetail });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/chat/ReportedUsers
        [HttpPost]
        [Route("ReportedUsers")]
        public async Task<ActionResult> ReportedUsers([FromBody] JObject objJson)
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

                ReportedUsers objReportedUsers = new ReportedUsers();
                objReportedUsers.ReportingUserId = Convert.ToString(objJson.SelectToken("reportinguserId"));
                objReportedUsers.BlockedUserId = Convert.ToString(objJson.SelectToken("blockeduserId"));
                objReportedUsers.AddedDate = DateTime.UtcNow;
                await _appDbContext.ReportedUsers.AddAsync(objReportedUsers);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", UserGroupDetail = "User is blocked." });
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