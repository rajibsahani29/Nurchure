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
    public class customerpersonalityController : Controller
    {
        private AppDbContext _appDbContext;
        private IConfiguration configuration;

        public customerpersonalityController(AppDbContext context, IConfiguration iConfig)
        {
            _appDbContext = context;
            configuration = iConfig;
        }

        // POST api/customerpersonality/GetAllPersonalityQuestions
        [HttpPost]
        [Route("GetAllPersonalityQuestions")]
        public ActionResult GetAllPersonalityQuestions()
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

                var objPersonalityQuestions = _appDbContext.PersonalityQuestionaire.ToList();

                if (objPersonalityQuestions == null || objPersonalityQuestions.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "No result found." }));
                }

                return Ok(new { Status = "OK", PersonalityQuestion = objPersonalityQuestions });
                
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/customerpersonality/GetAllAnsweredPersonalityQuestions
        [HttpPost]
        [Route("GetAllAnsweredPersonalityQuestions")]
        public ActionResult GetAllAnsweredPersonalityQuestions()
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

                var objPersonalityQuestions = from upqd in _appDbContext.UserPersonalityQuestionaireDetails
                                              join pq in _appDbContext.PersonalityQuestionaire on upqd.PersonalityQuestonId equals pq.Id into Details
                                              from pq1 in Details.DefaultIfEmpty()
                                              where(upqd.UserId == strUserId)
                                              select new
                                              {
                                                  UserId = upqd.UserId,
                                                  PersonalityQuestonId = upqd.PersonalityQuestonId,
                                                  PersonalityQAnswer = upqd.PersonalityQAnswer,
                                                  PersonalityQuestion = pq1.PersonalityQuestion,
                                                  Option1 = pq1.Option1,
                                                  Option2 = pq1.Option2,
                                                  Option3 = pq1.Option3,
                                                  Option4 = pq1.Option4,
                                                  MediaAssociatedId = pq1.MediaAssociatedId
                                              };

                if (objPersonalityQuestions == null || objPersonalityQuestions.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "No result found." }));
                }

                return Ok(new { Status = "OK", PersonalityQuestion = objPersonalityQuestions });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/customerpersonality/GetAllUnAnsweredPersonalityQuestions
        [HttpPost]
        [Route("GetAllUnAnsweredPersonalityQuestions")]
        public ActionResult GetAllUnAnsweredPersonalityQuestions()
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

                var objPersonalityQuestions = from pq in _appDbContext.PersonalityQuestionaire
                                              where !(
                                                from upqd in _appDbContext.UserPersonalityQuestionaireDetails
                                                where(upqd.UserId == strUserId)
                                                select upqd.PersonalityQuestonId
                                              ).Contains(pq.Id)
                                              select pq;

                if (objPersonalityQuestions == null || objPersonalityQuestions.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "No result found." }));
                }

                return Ok(new { Status = "OK", PersonalityQuestion = objPersonalityQuestions });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/customerpersonality/GetRandomUnAnsweredPersonalityQuestion
        [HttpPost]
        [Route("GetRandomUnAnsweredPersonalityQuestion")]
        public ActionResult GetRandomUnAnsweredPersonalityQuestion()
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

                var objPersonalityQuestions = from pq in _appDbContext.PersonalityQuestionaire
                                              where !(
                                                from upqd in _appDbContext.UserPersonalityQuestionaireDetails
                                                where (upqd.UserId == strUserId)
                                                select upqd.PersonalityQuestonId
                                              ).Contains(pq.Id)
                                              orderby(objRandom.Next(pq.Id))
                                              select pq;

                if (objPersonalityQuestions == null || objPersonalityQuestions.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "No result found." }));
                }

                return Ok(new { Status = "OK", PersonalityQuestion = objPersonalityQuestions });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/customerpersonality/GetPersonalityQuestionById
        [HttpPost]
        [Route("GetPersonalityQuestionById")]
        public ActionResult GetPersonalityQuestionById([FromBody] JObject objJson)
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

                var objPersonalityQuestions = from pq in _appDbContext.PersonalityQuestionaire
                                              where (pq.Id == Convert.ToInt32(objJson.SelectToken("Id")))
                                              select pq;

                if (objPersonalityQuestions == null || objPersonalityQuestions.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "No result found." }));
                }

                return Ok(new { Status = "OK", PersonalityQuestion = objPersonalityQuestions });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/customerpersonality/SaveUserPersonalityQuestionaireAnswers
        [HttpPost]
        [Route("SaveUserPersonalityQuestionaireAnswers")]
        public async Task<ActionResult> SaveUserPersonalityQuestionaireAnswers([FromBody] JObject objJson)
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
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strMBTIType = GetMBTITypeByQuestionAnswer(Convert.ToInt32(objJson.SelectToken("personalityquestonid")), Convert.ToString(objJson.SelectToken("personalityqanswer")));

                if (strMBTIType == "") {
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid Personality Queston Id" }));
                }

                UserPersonalityQuestionaireDetails objUserPersonalityQuestionaire = _appDbContext.UserPersonalityQuestionaireDetails
                                .Where(upqd => upqd.UserId == strUserId && upqd.PersonalityQuestonId == Convert.ToInt32(objJson.SelectToken("personalityquestonid")))
                                .OrderByDescending(t => t.Id).AsNoTracking().FirstOrDefault();

                int returnVal = 0;
                if (objUserPersonalityQuestionaire == null)
                {
                    UserPersonalityQuestionaireDetails objUserPersonalityQuestionaireDetails = new UserPersonalityQuestionaireDetails();
                    objUserPersonalityQuestionaireDetails.UserId = strUserId;
                    objUserPersonalityQuestionaireDetails.PersonalityQuestonId = Convert.ToInt32(objJson.SelectToken("personalityquestonid"));
                    objUserPersonalityQuestionaireDetails.PersonalityQAnswer = Convert.ToString(objJson.SelectToken("personalityqanswer"));
                    objUserPersonalityQuestionaireDetails.MBTIType = strMBTIType;
                    objUserPersonalityQuestionaireDetails.CreatedDate = DateTime.UtcNow;
                    await _appDbContext.UserPersonalityQuestionaireDetails.AddAsync(objUserPersonalityQuestionaireDetails);
                    returnVal = await _appDbContext.SaveChangesAsync();
                }
                else {
                    UserPersonalityQuestionaireDetails objUserPersonalityQuestionaireDetails = new UserPersonalityQuestionaireDetails();
                    objUserPersonalityQuestionaireDetails.Id = objUserPersonalityQuestionaire.Id;
                    objUserPersonalityQuestionaireDetails.PersonalityQAnswer = Convert.ToString(objJson.SelectToken("personalityqanswer"));
                    objUserPersonalityQuestionaireDetails.MBTIType = strMBTIType;
                    objUserPersonalityQuestionaireDetails.LastUpdated = DateTime.UtcNow;

                    _appDbContext.UserPersonalityQuestionaireDetails.Attach(objUserPersonalityQuestionaireDetails);
                    _appDbContext.Entry(objUserPersonalityQuestionaireDetails).Property("PersonalityQAnswer").IsModified = true;
                    _appDbContext.Entry(objUserPersonalityQuestionaireDetails).Property("MBTIType").IsModified = true;
                    _appDbContext.Entry(objUserPersonalityQuestionaireDetails).Property("LastUpdated").IsModified = true;
                    returnVal = await _appDbContext.SaveChangesAsync();
                }

                if (returnVal > 0)
                {
                    var objUserPersonalityQuestionaireDetails = (from upqd in _appDbContext.UserPersonalityQuestionaireDetails
                                                                 where (upqd.UserId == strUserId)
                                                                 select upqd).ToList<UserPersonalityQuestionaireDetails>();

                    int E_RawPointCount = Convert.ToInt32(objUserPersonalityQuestionaireDetails.Where(t => t.MBTIType == "E").Count());
                    int I_RawPointCount = Convert.ToInt32(objUserPersonalityQuestionaireDetails.Where(t => t.MBTIType == "I").Count());
                    int S_RawPointCount = Convert.ToInt32(objUserPersonalityQuestionaireDetails.Where(t => t.MBTIType == "S").Count());
                    int N_RawPointCount = Convert.ToInt32(objUserPersonalityQuestionaireDetails.Where(t => t.MBTIType == "N").Count());
                    int T_RawPointCount = Convert.ToInt32(objUserPersonalityQuestionaireDetails.Where(t => t.MBTIType == "T").Count());
                    int F_RawPointCount = Convert.ToInt32(objUserPersonalityQuestionaireDetails.Where(t => t.MBTIType == "F").Count());
                    int J_RawPointCount = Convert.ToInt32(objUserPersonalityQuestionaireDetails.Where(t => t.MBTIType == "J").Count());
                    int P_RawPointCount = Convert.ToInt32(objUserPersonalityQuestionaireDetails.Where(t => t.MBTIType == "P").Count());

                    int EI_ClarityVal = (E_RawPointCount > I_RawPointCount ? E_RawPointCount : I_RawPointCount);
                    int SN_ClarityVal = (S_RawPointCount > N_RawPointCount ? S_RawPointCount : N_RawPointCount);
                    int TF_ClarityVal = (T_RawPointCount > F_RawPointCount ? T_RawPointCount : F_RawPointCount);
                    int JP_ClarityVal = (J_RawPointCount > P_RawPointCount ? J_RawPointCount : P_RawPointCount);

                    string strPrefferedMBTIMatch = (E_RawPointCount > I_RawPointCount ? "E" : "I");
                    strPrefferedMBTIMatch += (S_RawPointCount > N_RawPointCount ? "S" : "N");
                    strPrefferedMBTIMatch += (T_RawPointCount > F_RawPointCount ? "T" : "F");
                    strPrefferedMBTIMatch += (J_RawPointCount > P_RawPointCount ? "J" : "P");

                    UserPersonalitySummary objUserPersonalitySummary = new UserPersonalitySummary();
                    objUserPersonalitySummary.UserId = strUserId;
                    objUserPersonalitySummary.PrefferedMBTIMatch = strPrefferedMBTIMatch;
                    objUserPersonalitySummary.E_RawPoint = E_RawPointCount;
                    objUserPersonalitySummary.I_RawPoint = I_RawPointCount;
                    objUserPersonalitySummary.S_RawPoint = S_RawPointCount;
                    objUserPersonalitySummary.N_RawPoint = N_RawPointCount;
                    objUserPersonalitySummary.T_RawPoint = T_RawPointCount;
                    objUserPersonalitySummary.F_RawPoint = F_RawPointCount;
                    objUserPersonalitySummary.J_RawPoint = J_RawPointCount;
                    objUserPersonalitySummary.P_RawPoint = P_RawPointCount;
                    objUserPersonalitySummary.EI_Preference = GetPreference(EI_ClarityVal, "EI");
                    objUserPersonalitySummary.EI_ClarityId = EI_ClarityVal;
                    objUserPersonalitySummary.SN_Preference = GetPreference(SN_ClarityVal, "SN");
                    objUserPersonalitySummary.SN_ClarityId = SN_ClarityVal;
                    objUserPersonalitySummary.TF_Preference = GetPreference(TF_ClarityVal, "TF");
                    objUserPersonalitySummary.TF_ClarityId = TF_ClarityVal;
                    objUserPersonalitySummary.JP_Preference = GetPreference(JP_ClarityVal, "JP");
                    objUserPersonalitySummary.JP_ClarityId = JP_ClarityVal;
                    //objUserPersonalitySummary.CreatedDate = DateTime.UtcNow;

                    UserPersonalitySummary objUserPersonalitySummaryDetails = _appDbContext.UserPersonalitySummary
                                .Where(ups => ups.UserId == strUserId)
                                .AsNoTracking().FirstOrDefault();
                    if (objUserPersonalitySummaryDetails == null)
                    {
                        objUserPersonalitySummary.CreatedDate = DateTime.UtcNow;
                        await _appDbContext.UserPersonalitySummary.AddAsync(objUserPersonalitySummary);
                        returnVal = await _appDbContext.SaveChangesAsync();
                    }
                    else {
                        objUserPersonalitySummary.LastUpdated = DateTime.UtcNow;

                        _appDbContext.UserPersonalitySummary.Attach(objUserPersonalitySummary);
                        _appDbContext.Entry(objUserPersonalitySummary).Property("PrefferedMBTIMatch").IsModified = true;
                        _appDbContext.Entry(objUserPersonalitySummary).Property("E_RawPoint").IsModified = true;
                        _appDbContext.Entry(objUserPersonalitySummary).Property("I_RawPoint").IsModified = true;
                        _appDbContext.Entry(objUserPersonalitySummary).Property("S_RawPoint").IsModified = true;
                        _appDbContext.Entry(objUserPersonalitySummary).Property("N_RawPoint").IsModified = true;
                        _appDbContext.Entry(objUserPersonalitySummary).Property("T_RawPoint").IsModified = true;
                        _appDbContext.Entry(objUserPersonalitySummary).Property("F_RawPoint").IsModified = true;
                        _appDbContext.Entry(objUserPersonalitySummary).Property("J_RawPoint").IsModified = true;
                        _appDbContext.Entry(objUserPersonalitySummary).Property("P_RawPoint").IsModified = true;
                        _appDbContext.Entry(objUserPersonalitySummary).Property("EI_Preference").IsModified = true;
                        _appDbContext.Entry(objUserPersonalitySummary).Property("EI_ClarityId").IsModified = true;
                        _appDbContext.Entry(objUserPersonalitySummary).Property("SN_Preference").IsModified = true;
                        _appDbContext.Entry(objUserPersonalitySummary).Property("SN_ClarityId").IsModified = true;
                        _appDbContext.Entry(objUserPersonalitySummary).Property("TF_Preference").IsModified = true;
                        _appDbContext.Entry(objUserPersonalitySummary).Property("TF_ClarityId").IsModified = true;
                        _appDbContext.Entry(objUserPersonalitySummary).Property("JP_Preference").IsModified = true;
                        _appDbContext.Entry(objUserPersonalitySummary).Property("JP_ClarityId").IsModified = true;
                        _appDbContext.Entry(objUserPersonalitySummary).Property("LastUpdated").IsModified = true;
                        returnVal = await _appDbContext.SaveChangesAsync();
                    }

                    return Ok(new { Status = "OK", Detail = "Answer saved." });
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

        public string GetMBTITypeByQuestionAnswer(int intQuestionId, string strAnswer) {
            try
            {
                string strMBTIType = "";
                switch (intQuestionId)
                {
                    case 1:
                        strMBTIType = (strAnswer == "a" ? "J" : (strAnswer == "b" ? "P" : "NIL"));
                        break;
                    case 2:
                        strMBTIType = (strAnswer == "a" ? "P" : (strAnswer == "b" ? "J" : "NIL"));
                        break;
                    case 3:
                        strMBTIType = (strAnswer == "a" ? "N" : (strAnswer == "b" ? "S" : "NIL"));
                        break;
                    case 4:
                        strMBTIType = (strAnswer == "a" ? "F" : (strAnswer == "b" ? "T" : "NIL"));
                        break;
                    case 5:
                        strMBTIType = (strAnswer == "a" ? "P" : (strAnswer == "b" ? "J" : "NIL"));
                        break;
                    case 6:
                        strMBTIType = (strAnswer == "a" ? "I" : (strAnswer == "b" ? "E" : "NIL"));
                        break;
                    case 7:
                        strMBTIType = (strAnswer == "a" ? "F" : (strAnswer == "b" ? "T" : "NIL"));
                        break;
                    case 8:
                        strMBTIType = (strAnswer == "a" ? "E" : (strAnswer == "b" ? "I" : "NIL"));
                        break;
                    case 9:
                        strMBTIType = (strAnswer == "a" ? "E" : (strAnswer == "b" ? "I" : "NIL"));
                        break;
                    case 10:
                        strMBTIType = (strAnswer == "a" ? "N" : (strAnswer == "b" ? "S" : "NIL"));
                        break;
                    case 11:
                        strMBTIType = (strAnswer == "a" ? "I" : (strAnswer == "b" ? "E" : "NIL"));
                        break;
                    case 12:
                        strMBTIType = (strAnswer == "a" ? "F" : (strAnswer == "b" ? "T" : "NIL"));
                        break;
                    case 13:
                        strMBTIType = (strAnswer == "a" ? "T" : (strAnswer == "b" ? "F" : "NIL"));
                        break;
                    case 14:
                        strMBTIType = (strAnswer == "a" ? "S" : (strAnswer == "b" ? "N" : "NIL"));
                        break;
                    case 15:
                        strMBTIType = (strAnswer == "a" ? "P" : (strAnswer == "b" ? "J" : "NIL"));
                        break;
                    case 16:
                        strMBTIType = (strAnswer == "a" ? "N" : (strAnswer == "b" ? "S" : "NIL"));
                        break;
                    case 17:
                        strMBTIType = (strAnswer == "a" ? "I" : (strAnswer == "b" ? "E" : "NIL"));
                        break;
                    case 18:
                        strMBTIType = (strAnswer == "a" ? "I" : (strAnswer == "b" ? "E" : "NIL"));
                        break;
                    case 19:
                        strMBTIType = (strAnswer == "a" ? "J" : (strAnswer == "b" ? "P" : "NIL"));
                        break;
                    case 20:
                        strMBTIType = (strAnswer == "a" ? "S" : (strAnswer == "b" ? "N" : "NIL"));
                        break;
                    case 21:
                        strMBTIType = (strAnswer == "a" ? "F" : (strAnswer == "b" ? "T" : "NIL"));
                        break;
                    case 22:
                        strMBTIType = (strAnswer == "a" ? "S" : (strAnswer == "b" ? "N" : "NIL"));
                        break;
                    case 23:
                        strMBTIType = (strAnswer == "a" ? "F" : (strAnswer == "b" ? "T" : "NIL"));
                        break;
                    case 24:
                        strMBTIType = (strAnswer == "a" ? "E" : (strAnswer == "b" ? "I" : "NIL"));
                        break;
                    case 25:
                        strMBTIType = (strAnswer == "a" ? "J" : (strAnswer == "b" ? "P" : "NIL"));
                        break;
                    case 26:
                        strMBTIType = (strAnswer == "a" ? "P" : (strAnswer == "b" ? "J" : "NIL"));
                        break;
                    case 27:
                        strMBTIType = (strAnswer == "a" ? "P" : (strAnswer == "b" ? "J" : "NIL"));
                        break;
                    case 28:
                        strMBTIType = (strAnswer == "a" ? "T" : (strAnswer == "b" ? "F" : "NIL"));
                        break;
                    case 29:
                        strMBTIType = (strAnswer == "a" ? "F" : (strAnswer == "b" ? "T" : "NIL"));
                        break;
                    case 30:
                        strMBTIType = (strAnswer == "a" ? "T" : (strAnswer == "b" ? "F" : "NIL"));
                        break;
                    case 31:
                        strMBTIType = (strAnswer == "a" ? "S" : (strAnswer == "b" ? "N" : "NIL"));
                        break;
                    case 32:
                        strMBTIType = (strAnswer == "a" ? "N" : (strAnswer == "b" ? "S" : "NIL"));
                        break;
                    case 33:
                        strMBTIType = (strAnswer == "a" ? "S" : (strAnswer == "b" ? "N" : "NIL"));
                        break;
                    case 34:
                        strMBTIType = (strAnswer == "a" ? "S" : (strAnswer == "b" ? "N" : "NIL"));
                        break;
                    default:
                        break;
                }
                return strMBTIType;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public string GetPreference(int intClarity, string strMBTIType)
        {
            try
            {
                string strPreferenceVal = "";
                if (strMBTIType == "EI") {
                    if (intClarity >= 1 && intClarity <= 2) {
                        strPreferenceVal = "Slight";
                    }
                    else if (intClarity >= 3 && intClarity <= 4)
                    {
                        strPreferenceVal = "Moderate";
                    }
                    else if (intClarity >= 5 && intClarity <= 6)
                    {
                        strPreferenceVal = "Clear";
                    }
                    else if (intClarity >= 7 && intClarity <= 7)
                    {
                        strPreferenceVal = "Very Clear";
                    }
                    else
                    {
                        strPreferenceVal = "Other";
                    }
                }
                else if (strMBTIType == "SN")
                {
                    if (intClarity >= 1 && intClarity <= 2)
                    {
                        strPreferenceVal = "Slight";
                    }
                    else if (intClarity >= 3 && intClarity <= 6)
                    {
                        strPreferenceVal = "Moderate";
                    }
                    else if (intClarity >= 7 && intClarity <= 9)
                    {
                        strPreferenceVal = "Clear";
                    }
                    else if (intClarity >= 10 && intClarity <= 10)
                    {
                        strPreferenceVal = "Very Clear";
                    }
                    else
                    {
                        strPreferenceVal = "Other";
                    }
                }
                else if (strMBTIType == "TF")
                {
                    if (intClarity >= 1 && intClarity <= 2)
                    {
                        strPreferenceVal = "Slight";
                    }
                    else if (intClarity >= 3 && intClarity <= 5)
                    {
                        strPreferenceVal = "Moderate";
                    }
                    else if (intClarity >= 6 && intClarity <= 8)
                    {
                        strPreferenceVal = "Clear";
                    }
                    else if (intClarity >= 9 && intClarity <= 9)
                    {
                        strPreferenceVal = "Very Clear";
                    }
                    else
                    {
                        strPreferenceVal = "Other";
                    }
                }
                else if (strMBTIType == "JP")
                {
                    if (intClarity >= 1 && intClarity <= 2)
                    {
                        strPreferenceVal = "Slight";
                    }
                    else if (intClarity >= 3 && intClarity <= 4)
                    {
                        strPreferenceVal = "Moderate";
                    }
                    else if (intClarity >= 5 && intClarity <= 7)
                    {
                        strPreferenceVal = "Clear";
                    }
                    else if (intClarity >= 8 && intClarity <= 8)
                    {
                        strPreferenceVal = "Very Clear";
                    }
                    else
                    {
                        strPreferenceVal = "Other";
                    }
                }

                return strPreferenceVal;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        // POST api/customerpersonality/MBTIPersonalitiesSummary
        [HttpPost]
        [Route("MBTIPersonalitiesSummary")]
        public ActionResult MBTIPersonalitiesSummary([FromBody] JObject objJson)
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
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid ApiKey" }));
                }

                string strGroupName = "";

                var objUserPersonalitySummaryDetail = (from ups in _appDbContext.UserPersonalitySummary
                                                       where ups.UserId == strUserId
                                                       select ups).ToList<UserPersonalitySummary>();

                strGroupName = objUserPersonalitySummaryDetail[0].PrefferedMBTIMatch;

                if (string.IsNullOrEmpty(strGroupName))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, (new { Status = "Error", Error = "Invalid MBT Match" }));
                }

                MBTIPersonalities objMBTIPersonalities = _appDbContext.MBTIPersonalities
                                .Where(mbt => mbt.Name == strGroupName).AsNoTracking().FirstOrDefault();

                if (objMBTIPersonalities == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Match is empty" }));
                }
                else
                {
                    return Ok(new { Status = "OK", Description = objMBTIPersonalities.Description, Value1 = objMBTIPersonalities.Value1, Value2 = objMBTIPersonalities.Value2, Value3 = objMBTIPersonalities.Value3});
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }
    }
}