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
    //[System.Web.Http.Route("api/poll")]
    [Route("api/[controller]")]
    [ApiController]
    public class pollController : Controller
    {
        private AppDbContext _appDbContext;
        private IConfiguration configuration;

        public pollController(AppDbContext context, IConfiguration iConfig)
        {
            _appDbContext = context;
            configuration = iConfig;
        }

        // POST api/poll/CreatePollForGroupEvent
        [HttpPost]
        [Route("CreatePollForGroupEvent")]
        public async Task<ActionResult> CreatePollForGroupEvent([FromBody] JObject objJson)
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

                var objEvents = from e in _appDbContext.Events
                                where (e.Id == Convert.ToInt32(objJson.SelectToken("eventid")))
                                select e;

                if (objEvents.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Event doesn’t exist." }));
                }

                GroupPoll objGroupPoll = new GroupPoll();
                objGroupPoll.EventId = Convert.ToInt32(objJson.SelectToken("eventid"));
                //objGroupPoll.RestaurantName = Convert.ToString(objJson.SelectToken("restaurantname"));
                objGroupPoll.EventDate1 = Convert.ToDateTime(objJson.SelectToken("eventdate1"));
                DateTime dt;
                if (!DateTime.TryParseExact(Convert.ToString(objJson.SelectToken("eventtime1")), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                {
                    objGroupPoll.EventTime1 = TimeSpan.Parse("00:00");
                }
                else
                {
                    objGroupPoll.EventTime1 = dt.TimeOfDay;
                }
                objGroupPoll.EventDate2 = Convert.ToDateTime(objJson.SelectToken("eventdate2"));
                if (!DateTime.TryParseExact(Convert.ToString(objJson.SelectToken("eventtime2")), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                {
                    objGroupPoll.EventTime2 = TimeSpan.Parse("00:00");
                }
                else
                {
                    objGroupPoll.EventTime2 = dt.TimeOfDay;
                }
                objGroupPoll.StartDate = DateTime.UtcNow;
                objGroupPoll.EndDate = DateTime.UtcNow.AddHours(Convert.ToInt32(configuration.GetSection("appSettings").GetSection("PollEndHrs").Value));
                objGroupPoll.PollStatus = "active";
                objGroupPoll.OwnerUserId = strUserId;
                //objGroupPoll.TiltCount = 0;
                objGroupPoll.CreatedDate = DateTime.UtcNow;
                await _appDbContext.GroupPoll.AddAsync(objGroupPoll);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Poll created." });
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

        // POST api/poll/UpdatePollInfoById
        [HttpPost]
        [Route("UpdatePollInfoById")]
        public async Task<ActionResult> UpdatePollInfoById([FromBody] JObject objJson)
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

                var objGroupPollDetails = from gp in _appDbContext.GroupPoll
                                          where (gp.Id == Convert.ToInt32(objJson.SelectToken("grouppollid")) && gp.OwnerUserId == strUserId)
                                          select gp;

                if (objGroupPollDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "GroupPoll " + Convert.ToString(objJson.SelectToken("grouppollid")) + " doesn’t exist or Invalid User." }));
                }

                GroupPoll objGroupPoll = new GroupPoll();
                objGroupPoll.Id = Convert.ToInt32(objJson.SelectToken("grouppollid"));
                //objGroupPoll.RestaurantName = Convert.ToString(objJson.SelectToken("restaurantname"));
                objGroupPoll.EventDate1 = Convert.ToDateTime(objJson.SelectToken("eventdate1"));
                DateTime dt;
                if (!DateTime.TryParseExact(Convert.ToString(objJson.SelectToken("eventtime1")), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                {
                    objGroupPoll.EventTime1 = TimeSpan.Parse("00:00");
                }
                else
                {
                    objGroupPoll.EventTime1 = dt.TimeOfDay;
                }
                objGroupPoll.EventDate2 = Convert.ToDateTime(objJson.SelectToken("eventdate2"));
                if (!DateTime.TryParseExact(Convert.ToString(objJson.SelectToken("eventtime2")), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                {
                    objGroupPoll.EventTime2 = TimeSpan.Parse("00:00");
                }
                else
                {
                    objGroupPoll.EventTime2 = dt.TimeOfDay;
                }
                objGroupPoll.StartDate = Convert.ToDateTime(objJson.SelectToken("startdate"));
                objGroupPoll.EndDate = Convert.ToDateTime(objJson.SelectToken("enddate"));
                objGroupPoll.LastUpdatedDate = DateTime.UtcNow;

                _appDbContext.GroupPoll.Attach(objGroupPoll);
                _appDbContext.Entry(objGroupPoll).Property("RestaurantName").IsModified = true;
                _appDbContext.Entry(objGroupPoll).Property("EventDate1").IsModified = true;
                _appDbContext.Entry(objGroupPoll).Property("EventTime1").IsModified = true;
                _appDbContext.Entry(objGroupPoll).Property("EventDate2").IsModified = true;
                _appDbContext.Entry(objGroupPoll).Property("EventTime2").IsModified = true;
                _appDbContext.Entry(objGroupPoll).Property("StartDate").IsModified = true;
                _appDbContext.Entry(objGroupPoll).Property("EndDate").IsModified = true;
                _appDbContext.Entry(objGroupPoll).Property("LastUpdatedDate").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Poll updated." });
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

        // POST api/poll/DeletePollById
        [HttpPost]
        [Route("DeletePollById")]
        public async Task<ActionResult> DeletePollById([FromBody] JObject objJson)
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

                GroupPoll objGroupPoll = _appDbContext.GroupPoll
                                .Where(gp => gp.Id == Convert.ToInt32(objJson.SelectToken("grouppollid")))
                                .FirstOrDefault();

                if (objGroupPoll == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "GroupPoll " + Convert.ToString(objJson.SelectToken("grouppollid")) + " doesn’t exist" }));
                }
                else
                {
                    _appDbContext.GroupPoll.Remove(objGroupPoll);
                    int returnVal = await _appDbContext.SaveChangesAsync();
                    if (returnVal > 0)
                    {
                        return Ok(new { Status = "OK", Detail = "Poll Deleted." });
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

        // POST api/poll/GetPollInfoById
        [HttpPost]
        [Route("GetPollInfoById")]
        public ActionResult GetPollInfoById([FromBody] JObject objJson)
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

                var objGroupPoll = from gp in _appDbContext.GroupPoll
                                   where gp.Id == Convert.ToInt32(objJson.SelectToken("grouppollid"))
                                   select gp;

                if (objGroupPoll == null || objGroupPoll.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "GroupPoll " + Convert.ToString(objJson.SelectToken("grouppollid")) + " doesn’t exist" }));
                }

                return Ok(new { Status = "OK", GroupPoll = objGroupPoll });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/poll/NotifyPollWhenEnded
        [HttpPost]
        [Route("NotifyPollWhenEnded")]
        public ActionResult NotifyPollWhenEnded()
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

                var objGroupPollDetails = from gp in _appDbContext.GroupPoll
                                          where (gp.OwnerUserId == strUserId && gp.PollStatus == "active" && gp.EndDate <= DateTime.UtcNow)
                                          select gp;

                if (objGroupPollDetails.Count() <= 0)
                {
                    return Ok(new { Status = "OK", Detail = "No data found." });
                }

                return Ok(new { Status = "OK", Detail = objGroupPollDetails });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }
    }
}