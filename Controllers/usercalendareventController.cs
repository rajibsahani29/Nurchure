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
    //[System.Web.Http.Route("api/usercalendarevent")]
    [Route("api/[controller]")]
    [ApiController]
    public class usercalendareventController : Controller
    {
        private AppDbContext _appDbContext;
        private IConfiguration configuration;

        public usercalendareventController(AppDbContext context, IConfiguration iConfig)
        {
            _appDbContext = context;
            configuration = iConfig;
        }

        // POST api/usercalendarevent/CreateUserCalendarEvent
        [HttpPost]
        [Route("CreateUserCalendarEvent")]
        public async Task<ActionResult> CreateUserCalendarEvent([FromBody] JObject objJson)
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

                UserCalendarEvent objUserCalendarEvent = new UserCalendarEvent();
                objUserCalendarEvent.UserId = strUserId;
                objUserCalendarEvent.EventName = Convert.ToString(objJson.SelectToken("eventname"));
                objUserCalendarEvent.Location = Convert.ToString(objJson.SelectToken("location"));
                objUserCalendarEvent.EventDate = Convert.ToDateTime(objJson.SelectToken("eventdate"));
                DateTime dt;
                if (!DateTime.TryParseExact(Convert.ToString(objJson.SelectToken("eventtime")), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                {
                    objUserCalendarEvent.EventTime = TimeSpan.Parse("00:00");
                }
                else
                {
                    objUserCalendarEvent.EventTime = dt.TimeOfDay;
                }
                objUserCalendarEvent.CreatedDate = DateTime.UtcNow;
                await _appDbContext.UserCalendarEvent.AddAsync(objUserCalendarEvent);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Calendar Event Created." });
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

        // POST api/usercalendarevent/UpdateUserCaledarEvent
        [HttpPost]
        [Route("UpdateUserCaledarEvent")]
        public async Task<ActionResult> UpdateUserCaledarEvent([FromBody] JObject objJson)
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

                var objUserCalendarEventDetails = from uce in _appDbContext.UserCalendarEvent
                                                  where (uce.Id == Convert.ToInt32(objJson.SelectToken("usercalendareventid")) && uce.UserId == strUserId)
                                                  select uce;

                if (objUserCalendarEventDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Event doesn’t exist or Invalid User." }));
                }

                UserCalendarEvent objUserCalendarEvent = new UserCalendarEvent();
                objUserCalendarEvent.Id = Convert.ToInt32(objJson.SelectToken("usercalendareventid"));
                objUserCalendarEvent.EventName = Convert.ToString(objJson.SelectToken("eventname"));
                objUserCalendarEvent.Location = Convert.ToString(objJson.SelectToken("location"));
                objUserCalendarEvent.EventDate = Convert.ToDateTime(objJson.SelectToken("eventdate"));
                DateTime dt;
                if (!DateTime.TryParseExact(Convert.ToString(objJson.SelectToken("eventtime")), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                {
                    objUserCalendarEvent.EventTime = TimeSpan.Parse("00:00");
                }
                else
                {
                    objUserCalendarEvent.EventTime = dt.TimeOfDay;
                }
                objUserCalendarEvent.LastUpdatedDate = DateTime.UtcNow;

                _appDbContext.UserCalendarEvent.Attach(objUserCalendarEvent);
                _appDbContext.Entry(objUserCalendarEvent).Property("EventName").IsModified = true;
                _appDbContext.Entry(objUserCalendarEvent).Property("Location").IsModified = true;
                _appDbContext.Entry(objUserCalendarEvent).Property("EventDate").IsModified = true;
                _appDbContext.Entry(objUserCalendarEvent).Property("EventTime").IsModified = true;
                _appDbContext.Entry(objUserCalendarEvent).Property("LastUpdatedDate").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Calendar Event Updated." });
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

        // POST api/usercalendarevent/DeleteUserCalendarEventById
        [HttpPost]
        [Route("DeleteUserCalendarEventById")]
        public async Task<ActionResult> DeleteUserCalendarEventById([FromBody] JObject objJson)
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

                UserCalendarEvent objUserCalendarEvent = _appDbContext.UserCalendarEvent
                                .Where(uce => uce.Id == Convert.ToInt32(objJson.SelectToken("usercalendareventid")))
                                .FirstOrDefault();

                if (objUserCalendarEvent == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Event doesn’t exist." }));
                }
                else
                {
                    _appDbContext.UserCalendarEvent.Remove(objUserCalendarEvent);
                    int returnVal = await _appDbContext.SaveChangesAsync();
                    if (returnVal > 0)
                    {
                        return Ok(new { Status = "OK", Detail = "Calendar Event Deleted." });
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

        // POST api/usercalendarevent/GetUserCalendarEventById
        [HttpPost]
        [Route("GetUserCalendarEventById")]
        public ActionResult GetUserCalendarEventById([FromBody] JObject objJson)
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

                var objUserCalendarEvent = from uce in _appDbContext.UserCalendarEvent
                                           where uce.Id == Convert.ToInt32(objJson.SelectToken("usercalendareventid"))
                                           select uce;

                if (objUserCalendarEvent == null || objUserCalendarEvent.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Event doesn’t exist." }));
                }

                return Ok(new { Status = "OK", CalendarEvent = objUserCalendarEvent });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/usercalendarevent/GetUserCalendarEventsByDay
        [HttpPost]
        [Route("GetUserCalendarEventsByDay")]
        public ActionResult GetUserCalendarEventsByDay()
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

                var objUserCalendarEvent = from uce in _appDbContext.UserCalendarEvent
                                           where (uce.EventDate == Convert.ToDateTime(DateTime.UtcNow.ToString("yyyy-MM-dd")) && uce.UserId == strUserId)
                                           orderby uce.EventTime
                                           select uce;

                if (objUserCalendarEvent.Count() <= 0)
                {
                    return Ok(new { Status = "OK", Detail = "No events found." });
                }

                return Ok(new { Status = "OK", Detail = objUserCalendarEvent });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/usercalendarevent/GetuserCalendarEventsByWeek
        [HttpPost]
        [Route("GetuserCalendarEventsByWeek")]
        public ActionResult GetuserCalendarEventsByWeek()
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

                var objUserCalendarEvent = from uce in _appDbContext.UserCalendarEvent
                                           where ((uce.EventDate >= Convert.ToDateTime(DateTime.UtcNow.ToString("yyyy-MM-dd")) && uce.EventDate < Convert.ToDateTime(DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"))) && uce.UserId == strUserId)
                                           orderby uce.EventDate, uce.EventTime
                                           select uce;

                if (objUserCalendarEvent.Count() <= 0)
                {
                    return Ok(new { Status = "OK", Detail = "No events found." });
                }

                return Ok(new { Status = "OK", Detail = objUserCalendarEvent });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/usercalendarevent/GetUserCalendarEventsByMonth
        [HttpPost]
        [Route("GetUserCalendarEventsByMonth")]
        public ActionResult GetUserCalendarEventsByMonth()
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

                var objUserCalendarEvent = from uce in _appDbContext.UserCalendarEvent
                                           where ((uce.EventDate >= Convert.ToDateTime(DateTime.UtcNow.ToString("yyyy-MM-dd")) && uce.EventDate < Convert.ToDateTime(DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"))) && uce.UserId == strUserId)
                                           orderby uce.EventDate, uce.EventTime
                                           select uce;

                if (objUserCalendarEvent.Count() <= 0)
                {
                    return Ok(new { Status = "OK", Detail = "No events found." });
                }

                return Ok(new { Status = "OK", Detail = objUserCalendarEvent });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }
    }
}