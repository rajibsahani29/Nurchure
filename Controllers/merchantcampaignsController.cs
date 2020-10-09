using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NurchureAPI.Models;

namespace NurchureAPI.Controllers
{
    //[System.Web.Http.Route("api/merchantcampaigns")]
    [Route("api/[controller]")]
    [ApiController]
    public class merchantcampaignsController : Controller
    {
        private AppDbContext _appDbContext;
        private IConfiguration configuration;

        public merchantcampaignsController(AppDbContext context, IConfiguration iConfig)
        {
            _appDbContext = context;
            configuration = iConfig;
        }

        // POST api/merchantcampaigns/CreateMerchantCampaign
        [HttpPost]
        [Route("CreateMerchantCampaign")]
        public async Task<ActionResult> CreateMerchantCampaign([FromBody] JObject objJson)
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

                MerchantCampaigns objMerchantCampaigns = new MerchantCampaigns();
                objMerchantCampaigns.MerchantId = strMerchantId;
                objMerchantCampaigns.Name = Convert.ToString(objJson.SelectToken("name"));
                objMerchantCampaigns.Description = Convert.ToString(objJson.SelectToken("description"));
                objMerchantCampaigns.Disclosure = Convert.ToString(objJson.SelectToken("disclosure"));
                objMerchantCampaigns.CreatedDate = DateTime.UtcNow;
                objMerchantCampaigns.StartDate = Convert.ToDateTime(objJson.SelectToken("startdate"));
                objMerchantCampaigns.EndDate = Convert.ToDateTime(objJson.SelectToken("enddate"));
                objMerchantCampaigns.StateProvinceID = Convert.ToInt32(objJson.SelectToken("stateprovinceid"));
                objMerchantCampaigns.CityID = Convert.ToInt32(objJson.SelectToken("cityid"));
                objMerchantCampaigns.CountryID = Convert.ToInt32(objJson.SelectToken("countryid"));
                objMerchantCampaigns.Active = true;
                await _appDbContext.MerchantCampaigns.AddAsync(objMerchantCampaigns);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Merchant Campaign Created." });
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

        // POST api/merchantcampaigns/UpdateMercahantCampaign
        [HttpPost]
        [Route("UpdateMercahantCampaign")]
        public async Task<ActionResult> UpdateMercahantCampaign([FromBody] JObject objJson)
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

                var objMerchantCampaignsDetails = _appDbContext.MerchantCampaigns
                                                   .Where(mc => mc.Id == Convert.ToInt32(objJson.SelectToken("merchantcampaignsid")) && mc.MerchantId == strMerchantId)
                                                   .OrderByDescending(t => t.Id).AsNoTracking().FirstOrDefault();

                if (objMerchantCampaignsDetails == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Campaign doesn’t exist or Invalid Merchant." }));
                }

                if (Convert.ToBoolean(objMerchantCampaignsDetails.Active) == false) {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Campaign doesn’t active." }));
                }

                MerchantCampaigns objMerchantCampaigns = new MerchantCampaigns();
                objMerchantCampaigns.Id = Convert.ToInt32(objJson.SelectToken("merchantcampaignsid"));
                objMerchantCampaigns.Name = Convert.ToString(objJson.SelectToken("name"));
                objMerchantCampaigns.Description = Convert.ToString(objJson.SelectToken("description"));
                objMerchantCampaigns.Disclosure = Convert.ToString(objJson.SelectToken("disclosure"));
                objMerchantCampaigns.StartDate = Convert.ToDateTime(objJson.SelectToken("startdate"));
                objMerchantCampaigns.EndDate = Convert.ToDateTime(objJson.SelectToken("enddate"));
                objMerchantCampaigns.StateProvinceID = Convert.ToInt32(objJson.SelectToken("stateprovinceid"));
                objMerchantCampaigns.CityID = Convert.ToInt32(objJson.SelectToken("cityid"));
                objMerchantCampaigns.CountryID = Convert.ToInt32(objJson.SelectToken("countryid"));
                
                _appDbContext.MerchantCampaigns.Attach(objMerchantCampaigns);
                _appDbContext.Entry(objMerchantCampaigns).Property("Name").IsModified = true;
                _appDbContext.Entry(objMerchantCampaigns).Property("Description").IsModified = true;
                _appDbContext.Entry(objMerchantCampaigns).Property("Disclosure").IsModified = true;
                _appDbContext.Entry(objMerchantCampaigns).Property("StartDate").IsModified = true;
                _appDbContext.Entry(objMerchantCampaigns).Property("EndDate").IsModified = true;
                _appDbContext.Entry(objMerchantCampaigns).Property("StateProvinceID").IsModified = true;
                _appDbContext.Entry(objMerchantCampaigns).Property("CityID").IsModified = true;
                _appDbContext.Entry(objMerchantCampaigns).Property("CountryID").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Merchant Campaign Updated." });
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

        // POST api/merchantcampaigns/DisableMerchantCampaign
        [HttpPost]
        [Route("DisableMerchantCampaign")]
        public async Task<ActionResult> DisableMerchantCampaign([FromBody] JObject objJson)
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

                var objMerchantCampaignsDetails = _appDbContext.MerchantCampaigns
                                                   .Where(mc => mc.Id == Convert.ToInt32(objJson.SelectToken("merchantcampaignsid")) && mc.MerchantId == strMerchantId)
                                                   .OrderByDescending(t => t.Id).AsNoTracking().FirstOrDefault();

                if (objMerchantCampaignsDetails == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Campaign doesn’t exist or Invalid Merchant." }));
                }

                MerchantCampaigns objMerchantCampaigns = new MerchantCampaigns();
                objMerchantCampaigns.Id = Convert.ToInt32(objJson.SelectToken("merchantcampaignsid"));
                objMerchantCampaigns.Active = Convert.ToBoolean(Convert.ToInt32(objJson.SelectToken("activestatus")));
                
                _appDbContext.MerchantCampaigns.Attach(objMerchantCampaigns);
                _appDbContext.Entry(objMerchantCampaigns).Property("Active").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Merchant Campaign status has been updated." });
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

        // POST api/merchantcampaigns/ExtendDisabledCampaign
        [HttpPost]
        [Route("ExtendDisabledCampaign")]
        public async Task<ActionResult> ExtendDisabledCampaign([FromBody] JObject objJson)
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

                var objMerchantCampaignsDetails = _appDbContext.MerchantCampaigns
                                                   .Where(mc => mc.Id == Convert.ToInt32(objJson.SelectToken("merchantcampaignsid")) && mc.MerchantId == strMerchantId)
                                                   .OrderByDescending(t => t.Id).AsNoTracking().FirstOrDefault();

                if (objMerchantCampaignsDetails == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Campaign doesn’t exist or Invalid Merchant." }));
                }

                if (Convert.ToBoolean(objMerchantCampaignsDetails.Active) == true)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Campaign is active. You only extend/duplicate deactive campaign." }));
                }

                MerchantCampaigns objMerchantCampaigns = new MerchantCampaigns();
                objMerchantCampaigns.MerchantId = strMerchantId;
                objMerchantCampaigns.Name = objMerchantCampaignsDetails.Name;
                objMerchantCampaigns.Description = objMerchantCampaignsDetails.Description;
                objMerchantCampaigns.Disclosure = objMerchantCampaignsDetails.Disclosure;
                objMerchantCampaigns.CreatedDate = DateTime.UtcNow;
                objMerchantCampaigns.StartDate = Convert.ToDateTime(objJson.SelectToken("startdate"));
                objMerchantCampaigns.EndDate = Convert.ToDateTime(objJson.SelectToken("enddate"));
                objMerchantCampaigns.StateProvinceID = objMerchantCampaignsDetails.StateProvinceID;
                objMerchantCampaigns.CityID = objMerchantCampaignsDetails.CityID;
                objMerchantCampaigns.CountryID = objMerchantCampaignsDetails.CountryID;
                objMerchantCampaigns.Active = true;
                await _appDbContext.MerchantCampaigns.AddAsync(objMerchantCampaigns);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Duplicate Merchant Campaign Created." });
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

        // POST api/merchantcampaigns/DeleteMercahantCampaignById
        [HttpPost]
        [Route("DeleteMercahantCampaignById")]
        public async Task<ActionResult> DeleteMercahantCampaignById([FromBody] JObject objJson)
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

                var objMerchantCampaignsDetails = _appDbContext.MerchantCampaigns
                                                   .Where(mc => mc.Id == Convert.ToInt32(objJson.SelectToken("merchantcampaignsid")) && mc.MerchantId == strMerchantId)
                                                   .AsNoTracking().FirstOrDefault();

                if (objMerchantCampaignsDetails == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Campaign doesn’t exist or Invalid Merchant." }));
                }
                else
                {
                    var objMerchantPackages = _appDbContext.MerchantPackages
                                                .Where(mp => mp.CampaignID == Convert.ToInt32(objJson.SelectToken("merchantcampaignsid")))
                                                .AsNoTracking().FirstOrDefault();

                    if (objMerchantPackages != null)
                    {
                        _appDbContext.MerchantPackages.RemoveRange(_appDbContext.MerchantPackages.Where(x => x.CampaignID == Convert.ToInt32(objJson.SelectToken("merchantcampaignsid"))));
                        int returnVal = await _appDbContext.SaveChangesAsync();
                        if (returnVal > 0)
                        {
                            _appDbContext.MerchantCampaigns.Remove(objMerchantCampaignsDetails);
                            returnVal = await _appDbContext.SaveChangesAsync();
                            if (returnVal > 0)
                            {
                                return Ok(new { Status = "OK", Detail = "Merchant Campaign Deleted." });
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
                    else {
                        _appDbContext.MerchantCampaigns.Remove(objMerchantCampaignsDetails);
                        int returnVal = await _appDbContext.SaveChangesAsync();
                        if (returnVal > 0)
                        {
                            return Ok(new { Status = "OK", Detail = "Merchant Campaign Deleted." });
                        }
                        else
                        {
                            return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/merchantcampaigns/GetMercahantCampaignById
        [HttpPost]
        [Route("GetMercahantCampaignById")]
        public ActionResult GetMercahantCampaignById([FromBody] JObject objJson)
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

                var objMerchantCampaigns = (from mc in _appDbContext.MerchantCampaigns
                                            join c in _appDbContext.Countries on mc.CountryID equals c.Id into DetailsCountries
                                            from c1 in DetailsCountries.DefaultIfEmpty()
                                            join s in _appDbContext.StateProvinces on mc.StateProvinceID equals s.Id into DetailsStateProvinces
                                            from s1 in DetailsStateProvinces.DefaultIfEmpty()
                                            where (mc.Id == Convert.ToInt32(objJson.SelectToken("merchantcampaignsid")) && mc.MerchantId == strMerchantId)
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
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Campaign doesn’t exist." }));
                }

                return Ok(new { Status = "OK", MerchantCampaigns = objMerchantCampaigns });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }
    }
}