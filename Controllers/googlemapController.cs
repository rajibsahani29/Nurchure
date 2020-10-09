using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NurchureAPI.Models;

namespace NurchureAPI.Controllers
{
    //[System.Web.Http.Route("api/googlemap")]
    [Route("api/[controller]")]
    [ApiController]
    public class googlemapController : Controller
    {
        private AppDbContext _appDbContext;
        private IConfiguration configuration;

        public googlemapController(AppDbContext context, IConfiguration iConfig)
        {
            _appDbContext = context;
            configuration = iConfig;
        }

        // POST api/googlemap/GetGooglePlaceTypes
        [HttpPost]
        [Route("GetGooglePlaceTypes")]
        public ActionResult GetGooglePlaceTypes()
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

                var objGooglePlaceTypes = from gpt in _appDbContext.GooglePlaceTypes
                                     select gpt;

                if (objGooglePlaceTypes == null || objGooglePlaceTypes.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }
           
                return Ok(new { Status = "OK", GooglePlaceTypes = objGooglePlaceTypes });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/googlemap/GetListOfPlacesByGeoLocation
        [HttpPost]
        [Route("GetListOfPlacesByGeoLocation")]
        public ActionResult GetListOfPlacesByGeoLocation([FromBody] JObject objJson)
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

                string strGoogleMapApiKey = Convert.ToString(configuration.GetSection("appSettings").GetSection("GoogleMapApiKey").Value);
                if (string.IsNullOrEmpty(strGoogleMapApiKey)) {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Google Map Api Key Missing" }));
                }

                int intRadius = 5000;
                if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("radius")))) {
                    intRadius = Convert.ToInt32(objJson.SelectToken("radius"));
                }

                string strRequestUrl = "https://maps.googleapis.com/maps/api/place/nearbysearch/json?location="+ Convert.ToString(objJson.SelectToken("latitude")) + "," + Convert.ToString(objJson.SelectToken("longitude")) + "&radius=" + Convert.ToString(intRadius) + "&type=" + Convert.ToString(objJson.SelectToken("placetype")) + "&key=" + strGoogleMapApiKey;
                WebRequest objWebRequest = WebRequest.Create(strRequestUrl);
                objWebRequest.Method = "GET";
                //objWebRequest.ContentType = "application/json";
                WebResponse objWebResponse = objWebRequest.GetResponse();
                Stream dataStream = objWebResponse.GetResponseStream();
                StreamReader dataReader = new StreamReader(dataStream);
                string strResponse = dataReader.ReadToEnd();
                dataReader.Close();
                dataStream.Close();
                objWebResponse.Close();

                if (string.IsNullOrEmpty(strResponse)) {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                return Ok(new { Status = "OK", GoogleMapData = JObject.Parse(strResponse) });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/googlemap/GetDetailsOfSelectedPlaces
        [HttpPost]
        [Route("GetDetailsOfSelectedPlaces")]
        public ActionResult GetDetailsOfSelectedPlaces([FromBody] JObject objJson)
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

                string strGoogleMapApiKey = Convert.ToString(configuration.GetSection("appSettings").GetSection("GoogleMapApiKey").Value);
                if (string.IsNullOrEmpty(strGoogleMapApiKey))
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Google Map Api Key Missing" }));
                }

                int intRadius = 5000;
                if (!string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("radius"))))
                {
                    intRadius = Convert.ToInt32(objJson.SelectToken("radius"));
                }

                string strRequestUrl = "https://maps.googleapis.com/maps/api/place/nearbysearch/json?location=" + Convert.ToString(objJson.SelectToken("latitude")) + "," + Convert.ToString(objJson.SelectToken("longitude")) + "&radius=" + Convert.ToString(intRadius) + "&type=" + Convert.ToString(objJson.SelectToken("placetype")) + "&key=" + strGoogleMapApiKey;
                WebRequest objWebRequest = WebRequest.Create(strRequestUrl);
                objWebRequest.Method = "GET";
                //objWebRequest.ContentType = "application/json";
                WebResponse objWebResponse = objWebRequest.GetResponse();
                Stream dataStream = objWebResponse.GetResponseStream();
                StreamReader dataReader = new StreamReader(dataStream);
                string strResponse = dataReader.ReadToEnd();
                dataReader.Close();
                dataStream.Close();
                objWebResponse.Close();

                if (string.IsNullOrEmpty(strResponse))
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                JObject objResponseJson = JObject.Parse(strResponse);
                var filterResult = from res in objResponseJson["results"]
                                   where Convert.ToString(res["name"]) == Convert.ToString(Convert.ToString(objJson.SelectToken("placename")))
                                   select res;

                if (filterResult == null) {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                return Ok(new { Status = "OK", PlaceDetail = filterResult });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }
    }
}