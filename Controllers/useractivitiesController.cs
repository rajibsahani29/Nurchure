using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NurchureAPI.Models;

namespace NurchureAPI.Controllers
{
    //[System.Web.Http.Route("api/useractivities")]
    [Route("api/[controller]")]
    [ApiController]
    public class useractivitiesController : Controller
    {
        private AppDbContext _appDbContext;
        private IConfiguration configuration;

        public useractivitiesController(AppDbContext context, IConfiguration iConfig)
        {
            _appDbContext = context;
            configuration = iConfig;
        }

        // POST api/useractivities/GetAllActivities
        [HttpPost]
        [Route("GetAllActivities")]
        public ActionResult GetAllActivities()
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

                var objActivities = (from a in _appDbContext.Activities
                                    select a);

                if (objActivities == null || objActivities.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "No activities available." }));
                }

                return Ok(new { Status = "OK", Activities = objActivities });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/useractivities/SaveUserActivity
        [HttpPost]
        [Route("SaveUserActivity")]
        public async Task<ActionResult> SaveUserActivity([FromBody] JObject objJson)
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

                var objActivities = (from a in _appDbContext.Activities
                                     where a.Id == Convert.ToInt32(objJson.SelectToken("activityid"))
                                     select a);

                if (objActivities == null || objActivities.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Activity not available." }));
                }

                var objUserActivitiesDetails = (from ua in _appDbContext.UserActivities
                                                 where ua.UserId == strUserId
                                                 orderby ua.Id descending
                                                 select ua).ToList<UserActivities>();

                if (objUserActivitiesDetails.Where(t => t.ActivityId == Convert.ToInt32(objJson.SelectToken("activityid"))).Count() > 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "You already saved this activity." }));
                }

                int intRank = 1;
                if (objUserActivitiesDetails.Count() > 0)
                {
                    intRank = Convert.ToInt32(objUserActivitiesDetails[0].Rank) + 1;
                }

                UserActivities objUserActivities = new UserActivities();
                objUserActivities.UserId = strUserId;
                objUserActivities.ActivityId = Convert.ToInt32(objJson.SelectToken("activityid"));
                objUserActivities.Rank = intRank;
                objUserActivities.CreatedDate = DateTime.UtcNow;
                await _appDbContext.UserActivities.AddAsync(objUserActivities);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Activity saved." });
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

        // POST api/useractivities/GetPersonalityMatchResult
        [HttpPost]
        [Route("GetPersonalityMatchResult")]
        public ActionResult GetPersonalityMatchResult()
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

                var objUserPersonalitySummaryDetail = (from ups in _appDbContext.UserPersonalitySummary
                                                        where ups.UserId == strUserId
                                                        select ups).ToList<UserPersonalitySummary>();

                if (objUserPersonalitySummaryDetail == null || objUserPersonalitySummaryDetail.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "No users available." }));
                }

                var objUsers = (from u in _appDbContext.Users
                                select u);

                if (objUsers == null || objUsers.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "No users available." }));
                }

                //Temp query for define structure
                var objUserPersonalitySummary = new List<PersonalityMatchResult>();
                    /*(from ups in _appDbContext.UserPersonalitySummary
                                                join u in _appDbContext.Users on ups.UserId equals u.UserId into DetailsUsers
                                                from u1 in DetailsUsers.DefaultIfEmpty()
                                                join ula in _appDbContext.UserLoginAccount on ups.UserId equals ula.UserId into DetailsUserLoginAccount
                                                from ula1 in DetailsUserLoginAccount.DefaultIfEmpty()
                                                where 1 == 2
                                                select new { ups.UserId, ups.PrefferedMBTIMatch, u1.GeoLocationLatitude, u1.GeoLocationLongitude, u1.Age, ula1.CreatedDate }).ToList();*/

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

                //return Ok(new { Status = "OK", Count = objDistanceResult.Count(), MatchResult = objDistanceResult });

                var objUserActivities = (from ua in _appDbContext.UserActivities
                                        where ua.UserId == strUserId
                                        orderby ua.Rank
                                        select ua).Take(3).ToList<UserActivities>();

                if (objUserActivities == null || objUserActivities.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "No activity found." }));
                }

                var objMatchResult = new List<PersonalityMatchResult>();
                foreach (var objItem in objDistanceResult)
                {
                    var objUserActivitiesChild = (from ua in _appDbContext.UserActivities
                                             where ua.UserId == objItem.UserId
                                             orderby ua.Rank
                                             select ua).Take(3).ToList<UserActivities>();

                    int recCount = objUserActivitiesChild.Select(t => t.ActivityId).Intersect(objUserActivities.Select(x => x.ActivityId)).Count();
                    if (recCount > 0) {
                        objMatchResult.Add(objItem);
                    }
                }

                objMatchResult = objMatchResult.OrderBy(t => t.CreatedDate).Take(10).ToList();

                return Ok(new { Status = "OK", Count = objMatchResult.Count(),  MatchResult = objMatchResult });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }
    }
}