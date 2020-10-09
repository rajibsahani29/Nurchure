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
    [Route("api/[controller]")]
    [ApiController]
    public class ActivitiesController : ControllerBase
    {
        private AppDbContext _appDbContext;
        private IConfiguration configuration;

        public ActivitiesController(AppDbContext context, IConfiguration iConfig)
        {
            _appDbContext = context;
            configuration = iConfig;
        }
        // POST api/Activities/GetActivitiesDetails
        [HttpPost]
        [Route("GetActivitiesDetails")]
        public ActionResult GetActivitiesDetails([FromBody] JObject objJson)
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
                var objActivitiesDetails = _appDbContext.Activities.Select(s => s);

                if (objActivitiesDetails == null || objActivitiesDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Data Not Found" }));
                }

                return Ok(new { Status = "OK", User = objActivitiesDetails });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }
    }

}