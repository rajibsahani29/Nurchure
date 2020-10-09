using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NurchureAPI.Models;

namespace NurchureAPI.Controllers
{
    //[System.Web.Http.Route("api/events")]
    [Route("api/[controller]")]
    [ApiController]
    public class eventsController : Controller
    {
        private AppDbContext _appDbContext;
        private IConfiguration configuration;

        public eventsController(AppDbContext context, IConfiguration iConfig)
        {
            _appDbContext = context;
            configuration = iConfig;
        }

        // POST api/events/CreateEvent
        [HttpPost]
        [Route("CreateEvent")]
        public async Task<ActionResult> CreateEvent([FromBody] JObject objJson)
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

                Events objEvents = new Events();
                objEvents.GroupId = Convert.ToInt32(objJson.SelectToken("groupid"));
                objEvents.EventName = Convert.ToString(objJson.SelectToken("eventname"));
                //objEvents.MerchantId = Convert.ToString(objJson.SelectToken("merchantid"));
                objEvents.PackageId = Convert.ToInt32(objJson.SelectToken("packageid"));
                objEvents.LatestPollingId = Convert.ToString(objJson.SelectToken("latestpollingid"));
                //objEvents.EventDate = Convert.ToDateTime(objJson.SelectToken("eventdate"));
                //objEvents.EventDate = Convert.ToDateTime(objJson.SelectToken("eventpreferreddate"));
                //objEvents.EventCoordinator = Convert.ToString(objJson.SelectToken("eventcoordinator"));
                objEvents.EventCoordinator = strUserId;
                objEvents.BookingStageId = Convert.ToInt32(objJson.SelectToken("bookingstageid"));
                //objEvents.NumberOfAttendee = Convert.ToInt32(objJson.SelectToken("numberofattendee"));
                //objEvents.ConfirmedAttendee = Convert.ToInt32(objJson.SelectToken("confirmedattendee"));
                //objEvents.NumberOfAttendee = Convert.ToInt32(_appDbContext.UserChatGroupFriends.Where(x => x.GroupId == Convert.ToInt32(objJson.SelectToken("groupid"))).Count());
                //objEvents.ConfirmedAttendee = 1;
                objEvents.PaymentStageId = Convert.ToInt32(objJson.SelectToken("paymentstageid"));
                objEvents.AttendeeWithPaymentComplete = Convert.ToString(objJson.SelectToken("attendeewithpaymentcomplete"));
                objEvents.ActualAttendee = Convert.ToString(objJson.SelectToken("actualattendee"));
                objEvents.CreatedDate = DateTime.UtcNow;
                await _appDbContext.Events.AddAsync(objEvents);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    GroupPoll objGroupPoll = new GroupPoll();
                    objGroupPoll.EventId = Convert.ToInt32(objEvents.Id);
                    objGroupPoll.EventDate1 = Convert.ToDateTime(objJson.SelectToken("eventpreferreddate"));
                    DateTime dt;

                    if (!DateTime.TryParseExact(Convert.ToString(objJson.SelectToken("eventpreferredtime")), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    {
                        objGroupPoll.EventTime1 = TimeSpan.Parse("00:00");
                    }
                    else
                    {
                        objGroupPoll.EventTime1 = dt.TimeOfDay;
                    }

                    objGroupPoll.EventDate2 = Convert.ToDateTime(objJson.SelectToken("eventbackupdate"));

                    if (!DateTime.TryParseExact(Convert.ToString(objJson.SelectToken("eventbackuptime")), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    {
                        objGroupPoll.EventTime2 = TimeSpan.Parse("00:00");
                    }
                    else
                    {
                        objGroupPoll.EventTime2 = dt.TimeOfDay;
                    }

                    if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("preferredmerchantid"))))
                    {
                        var objPreferredMerchantTilt = (from mc in _appDbContext.MerchantCampaigns
                                                        join mp in _appDbContext.MerchantPackages on mc.Id equals mp.CampaignID into DetailsMerchantPackages
                                                        from mp1 in DetailsMerchantPackages.DefaultIfEmpty()
                                                        where mc.MerchantId == Convert.ToString(objJson.SelectToken("preferredmerchantid"))
                                                        select new
                                                        {
                                                            PreferredMerchantTilt = mp1.MinPerson,
                                                        }).Take(1).ToList();

                        objGroupPoll.PreferredTilt = objPreferredMerchantTilt[0].PreferredMerchantTilt;
                    }

                    if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("backupmerchantid"))))
                    {
                        var objBackupMerchantTilt = (from mc in _appDbContext.MerchantCampaigns
                                                     join mp in _appDbContext.MerchantPackages on mc.Id equals mp.CampaignID into DetailsMerchantPackages
                                                     from mp1 in DetailsMerchantPackages.DefaultIfEmpty()
                                                     where mc.MerchantId == Convert.ToString(objJson.SelectToken("backupmerchantid"))
                                                     select new
                                                     {
                                                         BackupMerchantTilt = mp1.MinPerson,
                                                     }).Take(1).ToList();

                        objGroupPoll.BackupTilt = objBackupMerchantTilt[0].BackupMerchantTilt;
                    }
                    objGroupPoll.StartDate = DateTime.UtcNow;
                    objGroupPoll.EndDate = DateTime.UtcNow.AddHours(Convert.ToInt32(configuration.GetSection("appSettings").GetSection("PollEndHrs").Value));
                    objGroupPoll.PollStatus = "active";
                    objGroupPoll.OwnerUserId = strUserId;
                    objGroupPoll.CreatedDate = DateTime.UtcNow;
                    objGroupPoll.PreferredMerchantId = Convert.ToString(objJson.SelectToken("preferredmerchantid"));
                    objGroupPoll.BackupMerchantId = Convert.ToString(objJson.SelectToken("backupmerchantid"));
                    await _appDbContext.GroupPoll.AddAsync(objGroupPoll);
                    await _appDbContext.SaveChangesAsync();

                    EventParticipateUsers objEventParticipateUsers = new EventParticipateUsers();
                    objEventParticipateUsers.UserId = strUserId;
                    objEventParticipateUsers.EventId = Convert.ToInt32(objEvents.Id);

                    objEventParticipateUsers.IsPreferredAttended = true;
                    objEventParticipateUsers.IsBackupAttended = true;

                    objEventParticipateUsers.CreatedDate = DateTime.UtcNow;
                    await _appDbContext.EventParticipateUsers.AddAsync(objEventParticipateUsers);
                    returnVal = await _appDbContext.SaveChangesAsync();

                    if (returnVal > 0)
                    {
                        return Ok(new { Status = "OK", Detail = "Event created." });
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

        // POST api/events/UpdateEvent
        [HttpPost]
        [Route("UpdateEvent")]
        public async Task<ActionResult> UpdateEvent([FromBody] JObject objJson)
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

                var objEventsDetails = _appDbContext.Events
                                        .Where(e => e.Id == Convert.ToInt32(objJson.SelectToken("eventid")))
                                        .OrderByDescending(t => t.Id).AsNoTracking().FirstOrDefault();

                if (objEventsDetails == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Event doesn’t exist or Invalid Merchant." }));
                }

                Events objEvents = new Events();
                objEvents.Id = Convert.ToInt32(objJson.SelectToken("eventid"));
                objEvents.GroupId = Convert.ToInt32(objJson.SelectToken("groupid"));
                objEvents.EventName = Convert.ToString(objJson.SelectToken("eventname"));
                objEvents.PackageId = Convert.ToInt32(objJson.SelectToken("packageid"));
                objEvents.LatestPollingId = Convert.ToString(objJson.SelectToken("latestpollingid"));
                objEvents.BookingStageId = Convert.ToInt32(objJson.SelectToken("bookingstageid"));
                objEvents.PaymentStageId = Convert.ToInt32(objJson.SelectToken("paymentstageid"));
                objEvents.AttendeeWithPaymentComplete = Convert.ToString(objJson.SelectToken("attendeewithpaymentcomplete"));
                objEvents.ActualAttendee = Convert.ToString(objJson.SelectToken("actualattendee"));
                objEvents.LastUpdated = DateTime.UtcNow;

                _appDbContext.Events.Attach(objEvents);
                _appDbContext.Entry(objEvents).Property("GroupId").IsModified = true;
                _appDbContext.Entry(objEvents).Property("EventName").IsModified = true;
                _appDbContext.Entry(objEvents).Property("PackageId").IsModified = true;
                _appDbContext.Entry(objEvents).Property("LatestPollingId").IsModified = true;
                _appDbContext.Entry(objEvents).Property("BookingStageId").IsModified = true;
                _appDbContext.Entry(objEvents).Property("PaymentStageId").IsModified = true;
                _appDbContext.Entry(objEvents).Property("AttendeeWithPaymentComplete").IsModified = true;
                _appDbContext.Entry(objEvents).Property("ActualAttendee").IsModified = true;
                _appDbContext.Entry(objEvents).Property("LastUpdated").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    var objGroupPollId = _appDbContext.GroupPoll.Select(t => new { GroupPollId = t.Id, EventId = t.EventId }).Where(s => s.EventId == Convert.ToInt32(objJson.SelectToken("eventid"))).FirstOrDefault();

                    GroupPoll objGroupPoll = new GroupPoll();
                    objGroupPoll.Id = Convert.ToInt32(objGroupPollId.GroupPollId);
                    objGroupPoll.EventId = Convert.ToInt32(objJson.SelectToken("eventid"));
                    objGroupPoll.EventDate1 = Convert.ToDateTime(objJson.SelectToken("eventpreferreddate"));
                    DateTime dt;
                    if (!DateTime.TryParseExact(Convert.ToString(objJson.SelectToken("eventpreferredtime")), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    {
                        objGroupPoll.EventTime1 = TimeSpan.Parse("00:00");
                    }
                    else
                    {
                        objGroupPoll.EventTime1 = dt.TimeOfDay;
                    }
                    objGroupPoll.EventDate2 = Convert.ToDateTime(objJson.SelectToken("eventbackupdate"));
                    if (!DateTime.TryParseExact(Convert.ToString(objJson.SelectToken("eventbackuptime")), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    {
                        objGroupPoll.EventTime2 = TimeSpan.Parse("00:00");
                    }
                    else
                    {
                        objGroupPoll.EventTime2 = dt.TimeOfDay;
                    }

                    objGroupPoll.PollStatus = "active";
                    objGroupPoll.OwnerUserId = strUserId;
                    objGroupPoll.StartDate = DateTime.UtcNow;
                    objGroupPoll.EndDate = DateTime.UtcNow.AddHours(Convert.ToInt32(configuration.GetSection("appSettings").GetSection("PollEndHrs").Value));
                    objGroupPoll.LastUpdatedDate = DateTime.UtcNow;
                    objGroupPoll.PreferredMerchantId = Convert.ToString(objJson.SelectToken("preferredmerchantid"));
                    objGroupPoll.BackupMerchantId = Convert.ToString(objJson.SelectToken("backupmerchantid"));

                    _appDbContext.GroupPoll.Attach(objGroupPoll);
                    _appDbContext.Entry(objGroupPoll).Property("EventDate1").IsModified = true;
                    _appDbContext.Entry(objGroupPoll).Property("EventTime1").IsModified = true;
                    _appDbContext.Entry(objGroupPoll).Property("EventDate2").IsModified = true;
                    _appDbContext.Entry(objGroupPoll).Property("EventTime2").IsModified = true;
                    _appDbContext.Entry(objGroupPoll).Property("OwnerUserId").IsModified = true;
                    _appDbContext.Entry(objGroupPoll).Property("PollStatus").IsModified = true;
                    _appDbContext.Entry(objGroupPoll).Property("StartDate").IsModified = true;
                    _appDbContext.Entry(objGroupPoll).Property("EndDate").IsModified = true;
                    _appDbContext.Entry(objGroupPoll).Property("LastUpdatedDate").IsModified = true;
                    _appDbContext.Entry(objGroupPoll).Property("PreferredMerchantId").IsModified = true;
                    _appDbContext.Entry(objGroupPoll).Property("BackupMerchantId").IsModified = true;
                    returnVal = await _appDbContext.SaveChangesAsync();

                    if (returnVal > 0)
                    {
                        return Ok(new { Status = "OK", Detail = "Event Updated." });
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

        // POST api/events/DeleteEvent
        [HttpPost]
        [Route("DeleteEvent")]
        public async Task<ActionResult> DeleteEvent([FromBody] JObject objJson)
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

                var objEvents = (from e in _appDbContext.Events
                                 where (e.Id == Convert.ToInt32(objJson.SelectToken("eventid")))
                                 select e).ToList<Events>();

                if (objEvents == null || objEvents.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Event doesn’t exist or Invalid Merchant." }));
                }
                else
                {
                    _appDbContext.Events.Remove(objEvents[0]);
                    int returnVal = await _appDbContext.SaveChangesAsync();
                    if (returnVal > 0)
                    {
                        return Ok(new { Status = "OK", Detail = "Event Deleted." });
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

        // POST api/events/GetEventById
        [HttpPost]
        [Route("GetEventById")]
        public ActionResult GetEventById([FromBody] JObject objJson)
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
                                         join bs in _appDbContext.BookingStages on e.BookingStageId equals bs.Id into DetailsBookingStages
                                         from bs1 in DetailsBookingStages.DefaultIfEmpty()
                                         join ps in _appDbContext.PaymentStages on e.PaymentStageId equals ps.Id into DetailsPaymentStages
                                         from ps1 in DetailsPaymentStages.DefaultIfEmpty()
                                         join mc in _appDbContext.MerchantCampaigns on m1.MerchantId equals mc.MerchantId into DetailsMerchantCampaigns
                                         from mc1 in DetailsMerchantCampaigns.DefaultIfEmpty()
                                         join mp in _appDbContext.MerchantPackages on mc1.Id equals mp.CampaignID into DetailsMerchantPackages
                                         from mp1 in DetailsMerchantPackages.DefaultIfEmpty()
                                         join mpp in _appDbContext.MerchantPackagesPictures on mp1.PackageId equals mpp.PackageId into DetailsMerchantPackagesPictures
                                         from mpp1 in DetailsMerchantPackagesPictures.DefaultIfEmpty()
                                         join sp in _appDbContext.StateProvinces on m1.StateProvinceID equals sp.Id into DetailsStateProvinces
                                         from sp1 in DetailsStateProvinces.DefaultIfEmpty()
                                             //where (e.Id == Convert.ToInt32(objJson.SelectToken("eventid")) && e.MerchantId == strMerchantId)
                                         where (e.Id == Convert.ToInt32(objJson.SelectToken("eventid")))
                                         select new
                                         {
                                             Id = Convert.ToString(e.Id),
                                             EventName = e.EventName,
                                             GroupName = ugc1.GroupName,
                                             PackageId = Convert.ToString(e.PackageId),
                                             PackagePrice = mp1.Price,
                                             LatestPollingId = e.LatestPollingId,
                                             EventDateTest = Convert.ToString(gp1.EventDate1),
                                             EventTime = Convert.ToString(gp1.EventTime1),
                                             EventCoordinator = e.EventCoordinator,
                                             BookingStageName = bs1.Name,
                                             PaymentStageName = ps1.Name,
                                             AttendeeWithPaymentComplete = e.AttendeeWithPaymentComplete,
                                             ActualAttendee = e.ActualAttendee,
                                             CreatedDate = Convert.ToString(e.CreatedDate),
                                             LastUpdated = Convert.ToString(e.LastUpdated),
                                             MerchantId = m1.MerchantId,
                                             MerchantName = m1.MerchantName,
                                             AddressLine1 = m1.AddressLine1,
                                             AddressLine2 = m1.AddressLine2,
                                             AddressLine3 = m1.AddressLine3,
                                             Zip = m1.ZipPostalCode,
                                             State = sp1.Description,
                                             Rating = m1.RatingAverage,
                                             MerchantImageUrl = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("Merchant").Value) + mpp1.ImageName),
                                             InterestedUsersCount = (
                                                                from epu in _appDbContext.EventParticipateUsers
                                                                where (epu.IsPreferredAttended == true && epu.EventId == Convert.ToInt32(objJson.SelectToken("eventid")))
                                                                //where (epu.EventId == Convert.ToInt32(objJson.SelectToken("eventid")))
                                                                select epu.Id
                                                              ).Count(),
                                             InterestedUsersList = (
                                                                from epu in _appDbContext.EventParticipateUsers
                                                                join u in _appDbContext.Users on epu.UserId equals u.UserId into DetailsUsers
                                                                from u1 in DetailsUsers.DefaultIfEmpty()
                                                                where (epu.IsPreferredAttended == true && epu.EventId == Convert.ToInt32(objJson.SelectToken("eventid")))
                                                                //where (epu.EventId == Convert.ToInt32(objJson.SelectToken("eventid")))
                                                                select new
                                                                {
                                                                    UserId = epu.UserId,
                                                                    FirstName = u1.FirstName,
                                                                    LastName = u1.LastName,
                                                                    ImageDetails = (from upi in _appDbContext.UserProfileImages
                                                                                    where (upi.UserId == u1.UserId)
                                                                                    orderby upi.Id ascending
                                                                                    select new
                                                                                    {
                                                                                        ImageUrl = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("UserProfileImages").Value)) + upi.FileName,
                                                                                    }).Take(1),
                                                                }
                                                              )
                                         });

                var objBackupEvent = (from e in _appDbContext.Events
                                      join ugc in _appDbContext.UserChatGroup on e.GroupId equals ugc.Id into DetailsUserChatGroup
                                      from ugc1 in DetailsUserChatGroup.DefaultIfEmpty()
                                      join gp in _appDbContext.GroupPoll on e.Id equals gp.EventId into DetailsGroupPoll
                                      from gp1 in DetailsGroupPoll.DefaultIfEmpty()
                                      join m in _appDbContext.Merchants on gp1.BackupMerchantId equals m.MerchantId into DetailsMerchants
                                      from m1 in DetailsMerchants.DefaultIfEmpty()
                                      join bs in _appDbContext.BookingStages on e.BookingStageId equals bs.Id into DetailsBookingStages
                                      from bs1 in DetailsBookingStages.DefaultIfEmpty()
                                      join ps in _appDbContext.PaymentStages on e.PaymentStageId equals ps.Id into DetailsPaymentStages
                                      from ps1 in DetailsPaymentStages.DefaultIfEmpty()
                                      join mc in _appDbContext.MerchantCampaigns on m1.MerchantId equals mc.MerchantId into DetailsMerchantCampaigns
                                      from mc1 in DetailsMerchantCampaigns.DefaultIfEmpty()
                                      join mp in _appDbContext.MerchantPackages on mc1.Id equals mp.CampaignID into DetailsMerchantPackages
                                      from mp1 in DetailsMerchantPackages.DefaultIfEmpty()
                                      join mpp in _appDbContext.MerchantPackagesPictures on mp1.PackageId equals mpp.PackageId into DetailsMerchantPackagesPictures
                                      from mpp1 in DetailsMerchantPackagesPictures.DefaultIfEmpty()
                                      join sp in _appDbContext.StateProvinces on m1.StateProvinceID equals sp.Id into DetailsStateProvinces
                                      from sp1 in DetailsStateProvinces.DefaultIfEmpty()
                                          //where (e.Id == Convert.ToInt32(objJson.SelectToken("eventid")) && e.MerchantId == strMerchantId)
                                      where (e.Id == Convert.ToInt32(objJson.SelectToken("eventid")))
                                      select new
                                      {
                                          Id = Convert.ToString(e.Id),
                                          EventName = e.EventName,
                                          GroupName = ugc1.GroupName,
                                          PackageId = Convert.ToString(e.PackageId),
                                          PackagePrice = mp1.Price,
                                          LatestPollingId = e.LatestPollingId,
                                          EventDateTest = Convert.ToString(gp1.EventDate2),
                                          EventTime = Convert.ToString(gp1.EventTime2),
                                          EventCoordinator = e.EventCoordinator,
                                          BookingStageName = bs1.Name,
                                          PaymentStageName = ps1.Name,
                                          AttendeeWithPaymentComplete = e.AttendeeWithPaymentComplete,
                                          ActualAttendee = e.ActualAttendee,
                                          CreatedDate = Convert.ToString(e.CreatedDate),
                                          LastUpdated = Convert.ToString(e.LastUpdated),
                                          MerchantId = m1.MerchantId,
                                          MerchantName = m1.MerchantName,
                                          AddressLine1 = m1.AddressLine1,
                                          AddressLine2 = m1.AddressLine2,
                                          AddressLine3 = m1.AddressLine3,
                                          Zip = m1.ZipPostalCode,
                                          State = sp1.Description,
                                          Rating = m1.RatingAverage,
                                          MerchantImageUrl = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("Merchant").Value) + mpp1.ImageName),
                                          InterestedUsersCount = (
                                                             from epu in _appDbContext.EventParticipateUsers
                                                             where (epu.IsBackupAttended == true && epu.EventId == Convert.ToInt32(objJson.SelectToken("eventid")))
                                                             //where (epu.EventId == Convert.ToInt32(objJson.SelectToken("eventid")))
                                                             select epu.Id
                                                           ).Count(),
                                          InterestedUsersList = (
                                                             from epu in _appDbContext.EventParticipateUsers
                                                             join u in _appDbContext.Users on epu.UserId equals u.UserId into DetailsUsers
                                                             from u1 in DetailsUsers.DefaultIfEmpty()
                                                             where (epu.IsBackupAttended == true && epu.EventId == Convert.ToInt32(objJson.SelectToken("eventid")))
                                                             //where (epu.EventId == Convert.ToInt32(objJson.SelectToken("eventid")))
                                                             select new
                                                             {
                                                                 UserId = epu.UserId,
                                                                 FirstName = u1.FirstName,
                                                                 LastName = u1.LastName,
                                                                 ImageDetails = (from upi in _appDbContext.UserProfileImages
                                                                                 where (upi.UserId == u1.UserId)
                                                                                 orderby upi.Id ascending
                                                                                 select new
                                                                                 {
                                                                                     ImageUrl = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + Convert.ToString(configuration.GetSection("appSettings").GetSection("UserProfileImages").Value)) + upi.FileName,
                                                                                 }).Take(1),
                                                             }
                                                           )
                                      });

                return Ok(new { Status = "OK", PreferredEvent = objPreferredEvent, BackupEvent = objBackupEvent });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/events/GetEventByGroupId
        [HttpPost]
        [Route("GetEventByGroupId")]
        public ActionResult GetEventByGroupId([FromBody] JObject objJson)
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

                var objEvents = (from e in _appDbContext.Events
                                 join ugc in _appDbContext.UserChatGroup on e.GroupId equals ugc.Id into DetailsUserChatGroup
                                 from ugc1 in DetailsUserChatGroup.DefaultIfEmpty()
                                     //join m in _appDbContext.Merchants on e.MerchantId equals m.MerchantId into DetailsMerchants
                                     //from m1 in DetailsMerchants.DefaultIfEmpty()
                                 join bs in _appDbContext.BookingStages on e.BookingStageId equals bs.Id into DetailsBookingStages
                                 from bs1 in DetailsBookingStages.DefaultIfEmpty()
                                 join ps in _appDbContext.PaymentStages on e.PaymentStageId equals ps.Id into DetailsPaymentStages
                                 from ps1 in DetailsPaymentStages.DefaultIfEmpty()
                                     //where (e.GroupId == Convert.ToInt32(objJson.SelectToken("groupid")) && e.MerchantId == Convert.ToString(objJson.SelectToken("merchantid")))
                                 select new
                                 {
                                     EventId = Convert.ToString(e.Id),
                                     GroupName = ugc1.GroupName,
                                     //MerchantName = m1.MerchantName,
                                     PackageId = Convert.ToString(e.PackageId),
                                     LatestPollingId = e.LatestPollingId,
                                     EventDate = Convert.ToString(e.EventDate),
                                     EventCoordinator = e.EventCoordinator,
                                     BookingStageName = bs1.Name,
                                     //NumberOfAttendee = Convert.ToString(e.NumberOfAttendee),
                                     //ConfirmedAttendee = Convert.ToString(e.ConfirmedAttendee),
                                     PaymentStageName = ps1.Name,
                                     AttendeeWithPaymentComplete = e.AttendeeWithPaymentComplete,
                                     ActualAttendee = e.ActualAttendee,
                                     CreatedDate = Convert.ToString(e.CreatedDate),
                                     LastUpdated = Convert.ToString(e.LastUpdated)
                                 });

                if (objEvents == null || objEvents.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Events doesn’t exist." }));
                }

                return Ok(new { Status = "OK", Events = objEvents });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/events/GetEventByMerchantId
        [HttpPost]
        [Route("GetEventByMerchantId")]
        public ActionResult GetEventByMerchantId()
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

                var objEvents = (from e in _appDbContext.Events
                                 join ugc in _appDbContext.UserChatGroup on e.GroupId equals ugc.Id into DetailsUserChatGroup
                                 from ugc1 in DetailsUserChatGroup.DefaultIfEmpty()
                                     //join m in _appDbContext.Merchants on e.MerchantId equals m.MerchantId into DetailsMerchants
                                     //from m1 in DetailsMerchants.DefaultIfEmpty()
                                 join bs in _appDbContext.BookingStages on e.BookingStageId equals bs.Id into DetailsBookingStages
                                 from bs1 in DetailsBookingStages.DefaultIfEmpty()
                                 join ps in _appDbContext.PaymentStages on e.PaymentStageId equals ps.Id into DetailsPaymentStages
                                 from ps1 in DetailsPaymentStages.DefaultIfEmpty()
                                     //where (e.MerchantId == strMerchantId)
                                 select new
                                 {
                                     Id = Convert.ToString(e.Id),
                                     GroupName = ugc1.GroupName,
                                     //MerchantName = m1.MerchantName,
                                     PackageId = Convert.ToString(e.PackageId),
                                     LatestPollingId = e.LatestPollingId,
                                     EventDate = Convert.ToString(e.EventDate),
                                     EventCoordinator = e.EventCoordinator,
                                     BookingStageName = bs1.Name,
                                     //NumberOfAttendee = Convert.ToString(e.NumberOfAttendee),
                                     //ConfirmedAttendee = Convert.ToString(e.ConfirmedAttendee),
                                     PaymentStageName = ps1.Name,
                                     AttendeeWithPaymentComplete = e.AttendeeWithPaymentComplete,
                                     ActualAttendee = e.ActualAttendee,
                                     CreatedDate = Convert.ToString(e.CreatedDate),
                                     LastUpdated = Convert.ToString(e.LastUpdated)
                                 });

                if (objEvents == null || objEvents.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Events doesn’t exist." }));
                }

                return Ok(new { Status = "OK", Events = objEvents });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/events/AddEventParticipateUsers
        [HttpPost]
        [Route("AddEventParticipateUsers")]
        public async Task<ActionResult> AddEventParticipateUsers([FromBody] JObject objJson)
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

                var objEventsDetails = _appDbContext.Events
                                        .Where(e => e.Id == Convert.ToInt32(objJson.SelectToken("eventid")))
                                        .OrderByDescending(t => t.Id).AsNoTracking().FirstOrDefault();

                if (objEventsDetails == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Event doesn’t exist." }));
                }

                //May be we need this later if there is change in functionality

                //var objEventParticipateUsersDetails = (from epu in _appDbContext.EventParticipateUsers
                //                                       where (epu.EventId == Convert.ToInt32(objJson.SelectToken("eventid")) && epu.UserId == strUserId && (epu.IsPreferredAttended == true || epu.IsBackupAttended == true))
                //                                       select epu).ToList<EventParticipateUsers>();

                //if (objEventParticipateUsersDetails.Count() > 0)
                //{
                //    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "You already added for attend the event." }));
                //}

                EventParticipateUsers objEventParticipateUsers = new EventParticipateUsers();
                objEventParticipateUsers.UserId = Convert.ToString(objJson.SelectToken("userid"));
                objEventParticipateUsers.EventId = Convert.ToInt32(objJson.SelectToken("eventid"));

                if (Convert.ToInt32(objJson.SelectToken("IsPreferredAttended")) == 1)
                {
                    objEventParticipateUsers.IsPreferredAttended = true;
                }
                else
                {
                    objEventParticipateUsers.IsPreferredAttended = false;
                }

                if (Convert.ToInt32(objJson.SelectToken("IsBackupAttended")) == 1)
                {
                    objEventParticipateUsers.IsBackupAttended = true;
                }
                else
                {
                    objEventParticipateUsers.IsBackupAttended = false;
                }

                objEventParticipateUsers.CreatedDate = DateTime.UtcNow;
                await _appDbContext.EventParticipateUsers.AddAsync(objEventParticipateUsers);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "User added successfully for event." });
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

        // POST api/events/DeleteEventParticipateUsers
        [HttpPost]
        [Route("DeleteEventParticipateUsers")]
        public async Task<ActionResult> DeleteEventParticipateUsers([FromBody] JObject objJson)
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

                var objEventParticipateUsersDetails = (from epu in _appDbContext.EventParticipateUsers
                                                       where (epu.EventId == Convert.ToInt32(objJson.SelectToken("eventid")) && epu.UserId == Convert.ToString(objJson.SelectToken("userid")))
                                                       select epu).AsNoTracking().FirstOrDefault();

                if (objEventParticipateUsersDetails == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "You are not attend the event." }));
                }

                EventParticipateUsers objEventParticipateUsers = new EventParticipateUsers();
                objEventParticipateUsers.Id = objEventParticipateUsersDetails.Id;
                objEventParticipateUsers.IsPreferredAttended = false;
                objEventParticipateUsers.IsBackupAttended = false;
                objEventParticipateUsers.LastUpdatedDate = DateTime.UtcNow;

                _appDbContext.EventParticipateUsers.Attach(objEventParticipateUsers);
                _appDbContext.Entry(objEventParticipateUsers).Property("IsPreferredAttended").IsModified = true;
                _appDbContext.Entry(objEventParticipateUsers).Property("IsBackupAttended").IsModified = true;
                _appDbContext.Entry(objEventParticipateUsers).Property("LastUpdatedDate").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", User = "User deleted successfully from event." });
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

        // POST api/events/GetEventsByDate
        [HttpPost]
        [Route("GetEventsByDate")]
        public ActionResult GetEventsByDate([FromBody] JObject objJson)
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

                var objEvents = (from e in _appDbContext.Events
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
                                 join epu in _appDbContext.EventParticipateUsers on e.Id equals epu.EventId into DetailsEventParticipateUsers
                                 from epu1 in DetailsEventParticipateUsers.DefaultIfEmpty()
                                 where ((epu1.UserId == strUserId) &&
                                 gp1.EventDate1 == Convert.ToDateTime(objJson.SelectToken("eventdate"))
                                 && gp1.PollStatus == "active")
                                 select new
                                 {
                                     EventId = e.Id,
                                     EventName = e.EventName,
                                     GroupName = ugc1.GroupName,
                                     MerchantName = m1.MerchantName,
                                     EventDate = gp1.EventDate1,
                                     EventTime = gp1.EventTime1,
                                     Price = mp1.Price,
                                     InterestedUsersCount = (
                                                        from epu in _appDbContext.EventParticipateUsers
                                                        where (epu.IsPreferredAttended == true && epu.EventId == e.Id)
                                                        select epu.Id
                                                      ).Count()
                                 }).Concat
                                 (from e in _appDbContext.Events
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
                                  join epu in _appDbContext.EventParticipateUsers on e.Id equals epu.EventId into DetailsEventParticipateUsers
                                  from epu1 in DetailsEventParticipateUsers.DefaultIfEmpty()
                                  where ((epu1.UserId == strUserId) &&
                                  gp1.EventDate2 == Convert.ToDateTime(objJson.SelectToken("eventdate"))
                                  && gp1.PollStatus == "active")
                                  select new
                                  {
                                      EventId = e.Id,
                                      EventName = e.EventName,
                                      GroupName = ugc1.GroupName,
                                      MerchantName = m1.MerchantName,
                                      EventDate = gp1.EventDate2,
                                      EventTime = gp1.EventTime2,
                                      Price = mp1.Price,
                                      InterestedUsersCount = (
                                                        from epu in _appDbContext.EventParticipateUsers
                                                        where (epu.IsBackupAttended == true && epu.EventId == e.Id)
                                                        select epu.Id
                                                      ).Count()
                                  }).Concat
                                 (from uce in _appDbContext.UserCalendarEvent
                                  join e in _appDbContext.Events on uce.EventId equals e.Id into DetailsEvents
                                  from e1 in DetailsEvents.DefaultIfEmpty()
                                  join ugc in _appDbContext.UserChatGroup on e1.GroupId equals ugc.Id into DetailsUserChatGroup
                                  from ugc1 in DetailsUserChatGroup.DefaultIfEmpty()
                                  join gp in _appDbContext.GroupPoll on e1.Id equals gp.EventId into DetailsGroupPoll
                                  from gp1 in DetailsGroupPoll.DefaultIfEmpty()
                                  join m in _appDbContext.Merchants on uce.MerchantId equals m.MerchantId into DetailsMerchants
                                  from m1 in DetailsMerchants.DefaultIfEmpty()
                                  join mc in _appDbContext.MerchantCampaigns on m1.MerchantId equals mc.MerchantId into DetailsMerchantCampaigns
                                  from mc1 in DetailsMerchantCampaigns.DefaultIfEmpty()
                                  join mp in _appDbContext.MerchantPackages on mc1.Id equals mp.CampaignID into DetailsMerchantPackages
                                  from mp1 in DetailsMerchantPackages.DefaultIfEmpty()
                                  where ((uce.UserId == strUserId) &&
                                  uce.EventDate == Convert.ToDateTime(objJson.SelectToken("eventdate")))
                                  select new
                                  {
                                      EventId = e1.Id,
                                      EventName = e1.EventName,
                                      GroupName = ugc1.GroupName,
                                      MerchantName = m1.MerchantName,
                                      EventDate = uce.EventDate,
                                      EventTime = uce.EventTime,
                                      Price = mp1.Price,
                                      InterestedUsersCount = (
                                                        from uce in _appDbContext.UserCalendarEvent
                                                        where (uce.EventId == Convert.ToInt32(objJson.SelectToken("eventid")))
                                                        select uce.Id
                                                      ).Count()
                                  });

                if (objEvents == null || objEvents.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Event doesn’t exist." }));
                }

                return Ok(new { Status = "OK", Events = objEvents });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/events/GetAllEventsByUserId
        [HttpPost]
        [Route("GetAllEventsByUserId")]
        public ActionResult GetAllEventsByUserId([FromBody] JObject objJson)
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

                //var objEvents = (from e in _appDbContext.Events
                //                 join ugc in _appDbContext.UserChatGroup on e.GroupId equals ugc.Id into DetailsUserChatGroup
                //                 from ugc1 in DetailsUserChatGroup.DefaultIfEmpty()
                //                 join gp in _appDbContext.GroupPoll on e.Id equals gp.EventId into DetailsGroupPoll
                //                 from gp1 in DetailsGroupPoll.DefaultIfEmpty()
                //                 join m in _appDbContext.Merchants on gp1.PreferredMerchantId equals m.MerchantId into DetailsMerchants
                //                 from m1 in DetailsMerchants.DefaultIfEmpty()
                //                 join mc in _appDbContext.MerchantCampaigns on m1.MerchantId equals mc.MerchantId into DetailsMerchantCampaigns
                //                 from mc1 in DetailsMerchantCampaigns.DefaultIfEmpty()
                //                 join mp in _appDbContext.MerchantPackages on mc1.Id equals mp.CampaignID into DetailsMerchantPackages
                //                 from mp1 in DetailsMerchantPackages.DefaultIfEmpty()
                //                 where ((gp1.OwnerUserId == strUserId))
                //                 select new
                //                 {
                //                     EventName = e.EventName,
                //                     GroupName = ugc1.GroupName,
                //                     MerchantName = m1.MerchantName,
                //                     EventDate = gp1.EventDate1,
                //                     EventTime = gp1.EventTime1,
                //                     Price = mp1.Price,
                //                     InterestedUsersCount = (
                //                                        from epu in _appDbContext.EventParticipateUsers
                //                                        where (epu.IsPreferredAttended == true && epu.UserId == strUserId)
                //                                        select epu.Id
                //                                      ).Count()
                //                 }).Concat
                //                 (from e in _appDbContext.Events
                //                  join ugc in _appDbContext.UserChatGroup on e.GroupId equals ugc.Id into DetailsUserChatGroup
                //                  from ugc1 in DetailsUserChatGroup.DefaultIfEmpty()
                //                  join gp in _appDbContext.GroupPoll on e.Id equals gp.EventId into DetailsGroupPoll
                //                  from gp1 in DetailsGroupPoll.DefaultIfEmpty()
                //                  join m in _appDbContext.Merchants on gp1.BackupMerchantId equals m.MerchantId into DetailsMerchants
                //                  from m1 in DetailsMerchants.DefaultIfEmpty()
                //                  join mc in _appDbContext.MerchantCampaigns on m1.MerchantId equals mc.MerchantId into DetailsMerchantCampaigns
                //                  from mc1 in DetailsMerchantCampaigns.DefaultIfEmpty()
                //                  join mp in _appDbContext.MerchantPackages on mc1.Id equals mp.CampaignID into DetailsMerchantPackages
                //                  from mp1 in DetailsMerchantPackages.DefaultIfEmpty()
                //                  where ((gp1.OwnerUserId == strUserId))
                //                  select new
                //                  {
                //                      EventName = e.EventName,
                //                      GroupName = ugc1.GroupName,
                //                      MerchantName = m1.MerchantName,
                //                      EventDate = gp1.EventDate2,
                //                      EventTime = gp1.EventTime2,
                //                      Price = mp1.Price,
                //                      InterestedUsersCount = (
                //                                        from epu in _appDbContext.EventParticipateUsers
                //                                        where (epu.IsBackupAttended == true && epu.UserId == strUserId)
                //                                        select epu.Id
                //                                      ).Count()
                //                  }).ToList().ToList();

                var objEvents = (from e in _appDbContext.Events
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
                                 join epu in _appDbContext.EventParticipateUsers on e.Id equals epu.EventId into DetailsEventParticipateUsers
                                 from epu1 in DetailsEventParticipateUsers.DefaultIfEmpty()
                                 where ((epu1.UserId == strUserId)
                                 && gp1.PollStatus == "active")
                                 select new
                                 {
                                     EventId = e.Id,
                                     EventName = e.EventName,
                                     GroupName = ugc1.GroupName,
                                     MerchantName = m1.MerchantName,
                                     EventDate = gp1.EventDate1,
                                     EventTime = gp1.EventTime1,
                                     Price = mp1.Price,
                                     InterestedUsersCount = (
                                                        from epu in _appDbContext.EventParticipateUsers
                                                        where (epu.IsPreferredAttended == true && epu.EventId == e.Id)
                                                        select epu.Id
                                                      ).Count()
                                 }).Concat
                                 (from e in _appDbContext.Events
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
                                  join epu in _appDbContext.EventParticipateUsers on e.Id equals epu.EventId into DetailsEventParticipateUsers
                                  from epu1 in DetailsEventParticipateUsers.DefaultIfEmpty()
                                  where ((epu1.UserId == strUserId)
                                  && gp1.PollStatus == "active")
                                  select new
                                  {
                                      EventId = e.Id,
                                      EventName = e.EventName,
                                      GroupName = ugc1.GroupName,
                                      MerchantName = m1.MerchantName,
                                      EventDate = gp1.EventDate2,
                                      EventTime = gp1.EventTime2,
                                      Price = mp1.Price,
                                      InterestedUsersCount = (
                                                        from epu in _appDbContext.EventParticipateUsers
                                                        where (epu.IsBackupAttended == true && epu.EventId == e.Id)
                                                        select epu.Id
                                                      ).Count()
                                  }).Concat
                                 (from uce in _appDbContext.UserCalendarEvent
                                  join e in _appDbContext.Events on uce.EventId equals e.Id into DetailsEvents
                                  from e1 in DetailsEvents.DefaultIfEmpty()
                                  join ugc in _appDbContext.UserChatGroup on e1.GroupId equals ugc.Id into DetailsUserChatGroup
                                  from ugc1 in DetailsUserChatGroup.DefaultIfEmpty()
                                  join gp in _appDbContext.GroupPoll on e1.Id equals gp.EventId into DetailsGroupPoll
                                  from gp1 in DetailsGroupPoll.DefaultIfEmpty()
                                  join m in _appDbContext.Merchants on uce.MerchantId equals m.MerchantId into DetailsMerchants
                                  from m1 in DetailsMerchants.DefaultIfEmpty()
                                  join mc in _appDbContext.MerchantCampaigns on m1.MerchantId equals mc.MerchantId into DetailsMerchantCampaigns
                                  from mc1 in DetailsMerchantCampaigns.DefaultIfEmpty()
                                  join mp in _appDbContext.MerchantPackages on mc1.Id equals mp.CampaignID into DetailsMerchantPackages
                                  from mp1 in DetailsMerchantPackages.DefaultIfEmpty()
                                  where ((uce.UserId == strUserId))
                                  select new
                                  {
                                      EventId = e1.Id,
                                      EventName = e1.EventName,
                                      GroupName = ugc1.GroupName,
                                      MerchantName = m1.MerchantName,
                                      EventDate = uce.EventDate,
                                      EventTime = uce.EventTime,
                                      Price = mp1.Price,
                                      InterestedUsersCount = (
                                                        from uce in _appDbContext.UserCalendarEvent
                                                        where (uce.EventId == e1.Id)
                                                        select uce.Id
                                                      ).Count()
                                  });

                if (objEvents == null || objEvents.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Event doesn’t exist." }));
                }

                return Ok(new { Status = "OK", Events = objEvents });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }
    }
}