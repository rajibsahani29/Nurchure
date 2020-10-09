using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NurchureAPI.Models;

namespace NurchureAPI.Controllers
{
    //[System.Web.Http.Route("api/jokes")]
    [Route("api/[controller]")]
    [ApiController]
    public class jokesController : Controller
    {
        private AppDbContext _appDbContext;
        private IConfiguration configuration;

        public jokesController(AppDbContext context, IConfiguration iConfig)
        {
            _appDbContext = context;
            configuration = iConfig;
        }

        // POST api/jokes/GetRandomJokes
        [HttpPost]
        [Route("GetRandomJokes")]
        public ActionResult GetRandomJokes()
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

                Random objRandom = new Random();
                int toSkip = objRandom.Next(1, _appDbContext.Jokes.Count());
                
                var objJokes = (from j in _appDbContext.Jokes
                                orderby (objRandom.Next(j.Id))
                                select j).Skip(toSkip).Take(1);

                if (objJokes == null || objJokes.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "No jocks available." }));
                }

                return Ok(new { Status = "OK", Jokes = objJokes });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }
    }
}