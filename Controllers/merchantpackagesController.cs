using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NurchureAPI.Models;
using System.Drawing;
using Microsoft.AspNetCore.Hosting;

namespace NurchureAPI.Controllers
{
    //[System.Web.Http.Route("api/merchantpackages")]
    [Route("api/[controller]")]
    [ApiController]
    public class merchantpackagesController : Controller
    {
        private AppDbContext _appDbContext;
        private IConfiguration configuration;
        private IHostingEnvironment _hostingEnvironment;

        public merchantpackagesController(AppDbContext context, IConfiguration iConfig, IHostingEnvironment environment)
        {
            _appDbContext = context;
            configuration = iConfig;
            _hostingEnvironment = environment;
        }

        // POST api/merchantpackages/CreateMerchantPackage
        [HttpPost]
        [Route("CreateMerchantPackage")]
        public async Task<ActionResult> CreateMerchantPackage([FromBody] JObject objJson)
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
                                                   .Where(mc => mc.Id == Convert.ToInt32(objJson.SelectToken("campaignid")) && mc.MerchantId == strMerchantId)
                                                   .OrderByDescending(t => t.Id).AsNoTracking().FirstOrDefault();

                if (objMerchantCampaignsDetails == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Campaign doesn’t exist or Invalid Merchant." }));
                }

                MerchantPackages objMerchantPackages = new MerchantPackages();
                objMerchantPackages.CampaignID = Convert.ToInt32(objJson.SelectToken("campaignid"));
                objMerchantPackages.PackageTypeID = Convert.ToInt32(objJson.SelectToken("packagetypeid"));
                objMerchantPackages.Description = Convert.ToString(objJson.SelectToken("description"));
                objMerchantPackages.Price = Convert.ToDecimal(objJson.SelectToken("price"));
                objMerchantPackages.CreatedDate = DateTime.UtcNow;
                objMerchantPackages.StartDate = objMerchantCampaignsDetails.StartDate;
                objMerchantPackages.EndDate = objMerchantCampaignsDetails.EndDate;
                await _appDbContext.MerchantPackages.AddAsync(objMerchantPackages);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Merchant Package Created." });
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

        // POST api/merchantpackages/UpdateMerchantPackage
        [HttpPost]
        [Route("UpdateMerchantPackage")]
        public async Task<ActionResult> UpdateMerchantPackage([FromBody] JObject objJson)
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

                var objMerchantPackageDetails = (from mp in _appDbContext.MerchantPackages
                                                join mc in _appDbContext.MerchantCampaigns on mp.CampaignID equals mc.Id into DetailsMerchantCampaigns
                                                from mc1 in DetailsMerchantCampaigns.DefaultIfEmpty()
                                                where (mp.PackageId == Convert.ToInt32(objJson.SelectToken("merchantpackagesid")) && mc1.MerchantId == strMerchantId)
                                                select mp);

                if (objMerchantPackageDetails == null || objMerchantPackageDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Package doesn’t exist or Invalid Merchant." }));
                }

                MerchantPackages objMerchantPackages = new MerchantPackages();
                objMerchantPackages.PackageId = Convert.ToInt32(objJson.SelectToken("merchantpackagesid"));
                objMerchantPackages.PackageTypeID = Convert.ToInt32(objJson.SelectToken("packagetypeid"));
                objMerchantPackages.Description = Convert.ToString(objJson.SelectToken("description"));
                objMerchantPackages.Price = Convert.ToDecimal(objJson.SelectToken("price"));
                
                _appDbContext.MerchantPackages.Attach(objMerchantPackages);
                _appDbContext.Entry(objMerchantPackages).Property("PackageTypeID").IsModified = true;
                _appDbContext.Entry(objMerchantPackages).Property("Description").IsModified = true;
                _appDbContext.Entry(objMerchantPackages).Property("Price").IsModified = true;
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Merchant Package Updated." });
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

        // POST api/merchantpackages/GetMerchantPackageById
        [HttpPost]
        [Route("GetMerchantPackageById")]
        public ActionResult GetMerchantPackageById([FromBody] JObject objJson)
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

                var objMerchantPackages = (from mp in _appDbContext.MerchantPackages
                                           join mc in _appDbContext.MerchantCampaigns on mp.CampaignID equals mc.Id into DetailsMerchantCampaigns
                                           from mc1 in DetailsMerchantCampaigns.DefaultIfEmpty()
                                           join pt in _appDbContext.PackageType on mp.PackageTypeID equals pt.Id into DetailsPackageType
                                           from pt1 in DetailsPackageType.DefaultIfEmpty()
                                           where (mp.PackageId == Convert.ToInt32(objJson.SelectToken("merchantpackagesid")) && mc1.MerchantId == strMerchantId)
                                           select new
                                           {
                                               PackageId = Convert.ToString(mp.PackageId),
                                               CampaignName = mc1.Name,
                                               PackageType = pt1.Description,
                                               Description = mp.Description,
                                               Price = Convert.ToString(mp.Price),
                                               CreatedDate = Convert.ToString(mp.CreatedDate),
                                               StartDate = Convert.ToString(mp.StartDate),
                                               EndDate = Convert.ToString(mp.EndDate),
                                               PackagePictures =
                                                (
                                                    from mpp in _appDbContext.MerchantPackagesPictures
                                                    where (mpp.PackageId == mp.PackageId)
                                                    select new
                                                    {
                                                        Id = Convert.ToString(mpp.Id),
                                                        ImageUrl = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + "MerchantPackagesPictures/" + mpp.ImageName),
                                                        AddedDate = Convert.ToString(mpp.AddedDate)
                                                    }
                                                )
                                           });

                if (objMerchantPackages == null || objMerchantPackages.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Package doesn’t exist." }));
                }

                return Ok(new { Status = "OK", MerchantPackages = objMerchantPackages });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/merchantpackages/ExtendMerchantPackage
        [HttpPost]
        [Route("ExtendMerchantPackage")]
        public async Task<ActionResult> ExtendMerchantPackage([FromBody] JObject objJson)
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

                var objMerchantPackageDetails = (from mp in _appDbContext.MerchantPackages
                                                 join mc in _appDbContext.MerchantCampaigns on mp.CampaignID equals mc.Id into DetailsMerchantCampaigns
                                                 from mc1 in DetailsMerchantCampaigns.DefaultIfEmpty()
                                                 where (mp.PackageId == Convert.ToInt32(objJson.SelectToken("merchantpackagesid")) && mc1.MerchantId == strMerchantId)
                                                 select mp ).ToList<MerchantPackages>();

                if (objMerchantPackageDetails == null || objMerchantPackageDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Packages doesn’t exist or Invalid Merchant." }));
                }

                MerchantPackages objMerchantPackages = new MerchantPackages();
                objMerchantPackages.CampaignID = objMerchantPackageDetails[0].CampaignID;
                objMerchantPackages.PackageTypeID = objMerchantPackageDetails[0].PackageTypeID;
                objMerchantPackages.Description = objMerchantPackageDetails[0].Description;
                objMerchantPackages.Price = objMerchantPackageDetails[0].Price;
                objMerchantPackages.CreatedDate = DateTime.UtcNow;
                objMerchantPackages.StartDate = Convert.ToDateTime(objJson.SelectToken("startdate"));
                objMerchantPackages.EndDate = Convert.ToDateTime(objJson.SelectToken("enddate"));
                await _appDbContext.MerchantPackages.AddAsync(objMerchantPackages);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Duplicate Merchant Package Created." });
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

        // POST api/merchantpackages/UploadPackagePictures
        [HttpPost]
        [Route("UploadPackagePictures")]
        public async Task<ActionResult> UploadPackagePictures([FromBody] JObject objJson)
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

                var objMerchantPackageDetails = (from mp in _appDbContext.MerchantPackages
                                                 join mc in _appDbContext.MerchantCampaigns on mp.CampaignID equals mc.Id into DetailsMerchantCampaigns
                                                 from mc1 in DetailsMerchantCampaigns.DefaultIfEmpty()
                                                 where (mp.PackageId == Convert.ToInt32(objJson.SelectToken("merchantpackagesid")) && mc1.MerchantId == strMerchantId)
                                                 select mp);

                if (objMerchantPackageDetails == null || objMerchantPackageDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Package doesn’t exist or Invalid Merchant." }));
                }

                string FileExtension = Path.GetExtension(Convert.ToString(objJson.SelectToken("filename")));
                if (FileExtension != ".jpg" && FileExtension != ".jpeg" && FileExtension != ".png" && FileExtension != ".gif") {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid File Type. Upload only - .jpg, .jpeg, .png, .gif." }));
                }

                if (string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("base64string")))) {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Base64string missing." }));
                }

                string strFileName = "PkgImg_" + Convert.ToString(objJson.SelectToken("merchantpackagesid")) + "_" + Convert.ToString(DateTime.UtcNow.Ticks) + FileExtension;
                string strSavedPath = "MerchantPackagesPictures/" + strFileName;
                var filePath = Path.Combine(_hostingEnvironment.WebRootPath, strSavedPath);

                string base64string = Convert.ToString(objJson.SelectToken("base64string")).Replace(" ", "+");
                byte[] imageBytes = Convert.FromBase64String(base64string);
                MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);
                ms.Write(imageBytes, 0, imageBytes.Length);
                Image image = System.Drawing.Image.FromStream(ms, true);
                if (ms.Length > ((1000 * 1024) * 5)) {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid File size. Upload upto 5 MB image size." }));
                }
                image.Save(filePath);

                MerchantPackagesPictures objMerchantPackagesPictures = new MerchantPackagesPictures();
                objMerchantPackagesPictures.PackageId = Convert.ToInt32(objJson.SelectToken("merchantpackagesid"));
                objMerchantPackagesPictures.ImageName = strFileName;
                objMerchantPackagesPictures.AddedDate = DateTime.UtcNow;
                await _appDbContext.MerchantPackagesPictures.AddAsync(objMerchantPackagesPictures);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Merchant package picture upload successfully." });
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

        // POST api/merchantpackages/DeletePackagePciture
        [HttpPost]
        [Route("DeletePackagePciture")]
        public async Task<ActionResult> DeletePackagePciture([FromBody] JObject objJson)
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

                var objMerchantPackagesPictures = (from mpp in _appDbContext.MerchantPackagesPictures
                                                 join mp in _appDbContext.MerchantPackages on mpp.PackageId equals mp.PackageId into DetailsMerchantPackages
                                                 from mp1 in DetailsMerchantPackages.DefaultIfEmpty()
                                                 join mc in _appDbContext.MerchantCampaigns on mp1.CampaignID equals mc.Id into DetailsMerchantCampaigns
                                                 from mc1 in DetailsMerchantCampaigns.DefaultIfEmpty()
                                                 where (mpp.Id == Convert.ToInt32(objJson.SelectToken("merchantpackagespicturesid")) && mc1.MerchantId == strMerchantId)
                                                 select mpp).ToList<MerchantPackagesPictures>();

                if (objMerchantPackagesPictures == null || objMerchantPackagesPictures.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Package pictures doesn’t exist or Invalid Merchant." }));
                }
                else
                {
                    _appDbContext.MerchantPackagesPictures.Remove(objMerchantPackagesPictures[0]);
                    int returnVal = await _appDbContext.SaveChangesAsync();
                    if (returnVal > 0)
                    {
                        string strSavedPath = "MerchantPackagesPictures/" + objMerchantPackagesPictures[0].ImageName;
                        var filePath = Path.Combine(_hostingEnvironment.WebRootPath, strSavedPath);
                        if (System.IO.File.Exists(filePath)) {
                            System.IO.File.Delete(filePath);
                        }
                        return Ok(new { Status = "OK", Detail = "Merchant Packages Picture Deleted." });
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
    }
}