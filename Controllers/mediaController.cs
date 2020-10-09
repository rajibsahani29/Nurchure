using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NurchureAPI.Models;

namespace NurchureAPI.Controllers
{
    //[System.Web.Http.Route("api/merchantpackages")]
    [Route("api/[controller]")]
    [ApiController]
    public class mediaController : Controller
    {
        private AppDbContext _appDbContext;
        private IConfiguration configuration;
        private IHostingEnvironment _hostingEnvironment;

        public mediaController(AppDbContext context, IConfiguration iConfig, IHostingEnvironment environment)
        {
            _appDbContext = context;
            configuration = iConfig;
            _hostingEnvironment = environment;
        }

        // POST api/media/UploadMediaFiles
        [HttpPost]
        [Route("UploadMediaFiles")]
        public async Task<ActionResult> UploadMediaFiles([FromBody] JObject objJson)
        {
            try
            {
                string FileSavedFolderName = "";
                if (Convert.ToString(objJson.SelectToken("refflag")) == "Merchant")
                {
                    var objMerchantDetails = (from m in _appDbContext.Merchants
                                              where (m.MerchantId == Convert.ToString(objJson.SelectToken("refid")))
                                              select m);

                    if (objMerchantDetails == null || objMerchantDetails.Count() <= 0)
                    {
                        return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Merchant doesn't exists." }));
                    }
                    FileSavedFolderName = "Merchant";
                }
                else if(Convert.ToString(objJson.SelectToken("refflag")) == "User")
                {
                    var objUserDetails = (from u in _appDbContext.Users
                                              where (u.UserId == Convert.ToString(objJson.SelectToken("refid")))
                                              select u);

                    if (objUserDetails == null || objUserDetails.Count() <= 0)
                    {
                        return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "User doesn't exists." }));
                    }
                    FileSavedFolderName = "User";
                }
                else if (Convert.ToString(objJson.SelectToken("refflag")) == "Chat")
                {
                    var objChatMessagesDetails = (from cm in _appDbContext.ChatMessages
                                          where (cm.ChatId == Convert.ToInt32(objJson.SelectToken("refid")))
                                          select cm);

                    if (objChatMessagesDetails == null || objChatMessagesDetails.Count() <= 0)
                    {
                        return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Chat doesn't exists." }));
                    }
                    FileSavedFolderName = "Chat";
                }
                else if (Convert.ToString(objJson.SelectToken("refflag")) == "ChatGroup")
                {
                    var objUserChatGroupDetails = (from ucg in _appDbContext.UserChatGroup
                                          where (ucg.Id == Convert.ToInt32(objJson.SelectToken("refid")))
                                          select ucg);

                    if (objUserChatGroupDetails == null || objUserChatGroupDetails.Count() <= 0)
                    {
                        return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Chat Group doesn't exists." }));
                    }
                    FileSavedFolderName = "ChatGroup";
                }

                string FileExtension = Path.GetExtension(Convert.ToString(objJson.SelectToken("filename")));

                if (Convert.ToString(objJson.SelectToken("filetype")) == "Image") {
                    if (FileExtension != ".jpg" && FileExtension != ".jpeg" && FileExtension != ".png" && FileExtension != ".gif")
                    {
                        return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid File Type. Upload only - .jpg, .jpeg, .png, .gif." }));
                    }
                }

                if (string.IsNullOrEmpty(Convert.ToString(objJson.SelectToken("base64string"))))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Base64string missing." }));
                }

                string strFileName = "File_" + Convert.ToString(objJson.SelectToken("refid")) + "_" + Convert.ToString(DateTime.UtcNow.Ticks) + FileExtension;
                //string strSavedPath = @"C:/Media/" + FileSavedFolderName + "/" + strFileName;
                string strSavedPath = @"C:\Media\" + FileSavedFolderName + $@"\{strFileName}";
                //var filePath = Path.Combine(_hostingEnvironment.WebRootPath, strSavedPath);

                string base64string = Convert.ToString(objJson.SelectToken("base64string")).Replace(" ", "+");
                byte[] imageBytes = Convert.FromBase64String(base64string);
                System.IO.File.WriteAllBytes(strSavedPath, imageBytes);

                //MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);
                //ms.Write(imageBytes, 0, imageBytes.Length);
                //Image image = System.Drawing.Image.FromStream(ms, true);
                /*if (ms.Length > ((1000 * 1024) * 5))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Invalid File size. Upload upto 5 MB image size." }));
                }*/
                //image.Save(filePath);
                //image.Save(strSavedPath);

                Media objMedia = new Media();
                objMedia.RefId = Convert.ToString(objJson.SelectToken("refid"));
                objMedia.FileName = strFileName;
                objMedia.FileType = Convert.ToString(objJson.SelectToken("filetype"));
                objMedia.RefFlag = Convert.ToString(objJson.SelectToken("refflag"));
                objMedia.AddedDate = DateTime.UtcNow;
                await _appDbContext.Media.AddAsync(objMedia);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "Media File uploaded successfully." });
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

        // POST api/media/DeleteMediaFiles
        [HttpPost]
        [Route("DeleteMediaFiles")]
        public async Task<ActionResult> DeleteMediaFiles([FromBody] JObject objJson)
        {
            try
            {
                var objMedia = (from m in _appDbContext.Media
                                where (m.Id == Convert.ToInt32(objJson.SelectToken("mediaid")))
                                select m).ToList<Media>();

                if (objMedia == null || objMedia.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Media File doesn’t exist." }));
                }
                else
                {
                    _appDbContext.Media.Remove(objMedia[0]);
                    int returnVal = await _appDbContext.SaveChangesAsync();
                    if (returnVal > 0)
                    {
                        string strSavedPath = @"C:\Media\" + objMedia[0].RefFlag + $@"\{objMedia[0].FileName}";
                        if (System.IO.File.Exists(strSavedPath))
                        {
                            System.IO.File.Delete(strSavedPath);
                        }
                        return Ok(new { Status = "OK", Detail = "Media File Deleted." });
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

        // POST api/media/GetMediaFilesById
        [HttpPost]
        [Route("GetMediaFilesById")]
        public ActionResult GetMediaFilesById([FromBody] JObject objJson)
        {
            try
            {
                var objMedia = (from m in _appDbContext.Media
                                where (m.Id == Convert.ToInt32(objJson.SelectToken("mediaid")))
                                select new{
                                    Id = m.Id,
                                    RefId = m.RefId,
                                    FileName = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + "Media/" + m.RefFlag + "/" + m.FileName),
                                    FileType = m.FileType,
                                    RefFlag = m.RefFlag,
                                    AddedDate = m.AddedDate
                                });

                if (objMedia == null || objMedia.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Media File doesn’t exist." }));
                }

                return Ok(new { Status = "OK", MediaDetails = objMedia });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/media/GetMediaFilesByRefId
        [HttpPost]
        [Route("GetMediaFilesByRefId")]
        public ActionResult GetMediaFilesByRefId([FromBody] JObject objJson)
        {
            try
            {
                var objMedia = (from m in _appDbContext.Media
                                where (m.RefId == Convert.ToString(objJson.SelectToken("refid")))
                                select new
                                {
                                    Id = m.Id,
                                    RefId = m.RefId,
                                    FileName = (Convert.ToString(configuration.GetSection("appSettings").GetSection("ServerUrl").Value) + "Media/" + m.RefFlag + "/" + m.FileName),
                                    FileType = m.FileType,
                                    RefFlag = m.RefFlag,
                                    AddedDate = m.AddedDate
                                });

                if (objMedia == null || objMedia.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, (new { Status = "Error", Error = "Media Files doesn’t exist." }));
                }

                return Ok(new { Status = "OK", MediaDetails = objMedia });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }
    }
}