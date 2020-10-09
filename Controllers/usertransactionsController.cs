using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NurchureAPI.Models;

namespace NurchureAPI.Controllers
{
    //[System.Web.Http.Route("api/usertransactions")]
    [Route("api/[controller]")]
    [ApiController]
    public class usertransactionsController : Controller
    {
        private AppDbContext _appDbContext;
        private IConfiguration configuration;

        public usertransactionsController(AppDbContext context, IConfiguration iConfig)
        {
            _appDbContext = context;
            configuration = iConfig;
        }

        // POST api/usertransactions/CreatePayment
        [HttpPost]
        [Route("CreatePayment")]
        public async Task<ActionResult> CreatePayment([FromBody] JObject objJson)
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

                var objSquareCustomerData = _appDbContext.SquareCustomerDetails
                                .Where(scd => scd.UserId == strUserId)
                                .OrderByDescending(t => t.Id).FirstOrDefault();

                string strSquareRequestUrl = Convert.ToString(configuration.GetSection("appSettings").GetSection("SquareRequestUrl").Value);
                string strSquareAccessToken = Convert.ToString(configuration.GetSection("appSettings").GetSection("SquareAccessToken").Value);

                if (strSquareRequestUrl == "" || strSquareAccessToken == "")
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                string idempotency_key = Convert.ToString(Guid.NewGuid());
                string customerId = objSquareCustomerData.SquareCustomerId;

                string strRequestUrl = strSquareRequestUrl + "payments";
                string strRequestData = "{\"idempotency_key\": \"" + idempotency_key + "\",\"amount_money\": {\"amount\":" + Convert.ToString(objJson.SelectToken("amount")) + ",\"currency\": \"" + Convert.ToString(objJson.SelectToken("currency")) + "\"},\"source_id\": \"" + Convert.ToString(objJson.SelectToken("source_id")) + "\",\"customer_id\": \"" + customerId + "\"}";
                WebRequest objWebRequest = WebRequest.Create(strRequestUrl);
                objWebRequest.Method = "POST";
                byte[] byteArray;
                byteArray = Encoding.UTF8.GetBytes(strRequestData);
                objWebRequest.ContentType = "application/json";
                objWebRequest.ContentLength = byteArray.Length;
                objWebRequest.Headers.Add("Authorization", "Bearer " + strSquareAccessToken);
                Stream dataStream = objWebRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse objWebResponse = objWebRequest.GetResponse();
                dataStream = objWebResponse.GetResponseStream();
                StreamReader dataReader = new StreamReader(dataStream);
                string strResponse = dataReader.ReadToEnd();
                dataReader.Close();
                dataStream.Close();
                objWebResponse.Close();

                if (string.IsNullOrEmpty(strResponse))
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                return Ok(new { Status = "OK", Detail = "Payment done successfully on square." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/usertransactions/CreateUserTransaction
        [HttpPost]
        [Route("CreateUserTransaction")]
        public async Task<ActionResult> CreateUserTransaction([FromBody] JObject objJson)
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

                UserTransactions objUserTransactions = new UserTransactions();
                objUserTransactions.UserId = strUserId;
                objUserTransactions.PaymentId = Convert.ToInt32(objJson.SelectToken("paymentid"));
                objUserTransactions.MerchantId = Convert.ToString(objJson.SelectToken("merchantid"));
                objUserTransactions.PackageId = Convert.ToInt32(objJson.SelectToken("packageid"));
                objUserTransactions.EventId = Convert.ToInt32(objJson.SelectToken("eventid"));
                objUserTransactions.TransactionTypeId = Convert.ToInt32(objJson.SelectToken("transactiontypeid"));//
                objUserTransactions.PaymentTypeId = Convert.ToInt32(objJson.SelectToken("paymenttypeid"));//
                objUserTransactions.UserTransactionsId = Convert.ToInt32(objJson.SelectToken("usertransactionsid"));
                objUserTransactions.SquareCustomerCardDetailsId = Convert.ToInt32(objJson.SelectToken("squarecustomercarddetailsid"));
                objUserTransactions.InitialRevenue = Convert.ToDecimal(objJson.SelectToken("initialrevenue"));
                objUserTransactions.DiscountPercent = Convert.ToDouble(objJson.SelectToken("discountpercent"));//packageid
                objUserTransactions.Discount = Convert.ToDecimal(objJson.SelectToken("discount"));//
                objUserTransactions.RevenueReceived = Convert.ToDecimal(objJson.SelectToken("revenuereceived"));
                objUserTransactions.CostTypeId = Convert.ToInt32(objJson.SelectToken("costtypeid"));//
                objUserTransactions.IsRefunded = false;
                objUserTransactions.RefundDetails = Convert.ToString(objJson.SelectToken("refunddetails"));//
                objUserTransactions.Notes = Convert.ToString(objJson.SelectToken("notes"));//
                objUserTransactions.SquareRefNo = Convert.ToString(objJson.SelectToken("squarerefno"));//
                objUserTransactions.ResponseText = Convert.ToString(objJson.SelectToken("responsetext"));//
                objUserTransactions.CreatedDate = DateTime.UtcNow;
                await _appDbContext.UserTransactions.AddAsync(objUserTransactions);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    return Ok(new { Status = "OK", Detail = "User transaction added." });
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

        // POST api/usertransactions/GetUserTransactionByEventId
        [HttpPost]
        [Route("GetUserTransactionByEventId")]
        public ActionResult GetUserTransactionByEventId([FromBody] JObject objJson)
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

                var objUserTransactions = from ut in _appDbContext.UserTransactions
                                          join u in _appDbContext.Users on ut.UserId equals u.UserId into DetailsUsers
                                          from u1 in DetailsUsers.DefaultIfEmpty()
                                          join uph in _appDbContext.UserPaymentHistory on ut.PaymentId equals uph.Id into DetailsUserPaymentHistory
                                          from uph1 in DetailsUserPaymentHistory.DefaultIfEmpty()
                                          join m in _appDbContext.Merchants on ut.MerchantId equals m.MerchantId into DetailsMerchants
                                          from m1 in DetailsMerchants.DefaultIfEmpty()
                                          join mp in _appDbContext.MerchantPackages on ut.PackageId equals mp.PackageId into DetailsMerchantPackages
                                          from mp1 in DetailsMerchantPackages.DefaultIfEmpty()
                                          join mc in _appDbContext.MerchantCampaigns on mp1.CampaignID equals mc.Id into DetailsMerchantCampaigns
                                          from mc1 in DetailsMerchantCampaigns.DefaultIfEmpty()
                                          join e in _appDbContext.Events on ut.EventId equals e.Id into DetailsEvents
                                          from e1 in DetailsEvents.DefaultIfEmpty()
                                          join tt in _appDbContext.TransactionType on ut.TransactionTypeId equals tt.Id into DetailsTransactionType
                                          from tt1 in DetailsTransactionType.DefaultIfEmpty()
                                          join ct in _appDbContext.CostType on ut.CostTypeId equals ct.Id into DetailsCostType
                                          from ct1 in DetailsCostType.DefaultIfEmpty()
                                          join pt in _appDbContext.PaymentType on ut.PaymentTypeId equals pt.Id into DetailsPaymentType
                                          from pt1 in DetailsPaymentType.DefaultIfEmpty()
                                          where (ut.EventId == Convert.ToInt32(objJson.SelectToken("eventid")) && ut.UserId == strUserId && ut.PaymentTypeId == 1)
                                          select new
                                          {
                                              Id = Convert.ToString(ut.Id),
                                              UserId = Convert.ToString(ut.UserId),
                                              UserName = (u1.FirstName + ' ' + u1.LastName),
                                              PaymentId = Convert.ToString(ut.PaymentId),
                                              MerchantId = Convert.ToString(ut.MerchantId),
                                              MerchantName = m1.MerchantName,
                                              PackageId = Convert.ToString(ut.PackageId),
                                              MerchantCampaignsId = Convert.ToString(mc1.Id),
                                              MerchantCampaignsName = mc1.Name,
                                              EventId = Convert.ToString(ut.EventId),
                                              TransactionTypeId = Convert.ToString(ut.TransactionTypeId),
                                              TransactionTypeDescription = tt1.Description,
                                              PaymentTypeId = Convert.ToString(pt1.Id),
                                              PaymentTypeDescription = pt1.Description,
                                              UserTransactionsId = Convert.ToString(ut.UserTransactionsId),
                                              SquareCustomerCardDetailsId = Convert.ToString(ut.SquareCustomerCardDetailsId),
                                              InitialRevenue = Convert.ToString(ut.InitialRevenue),
                                              DiscountPercent = Convert.ToString(ut.DiscountPercent),
                                              Discount = Convert.ToString(ut.Discount),
                                              RevenueReceived = Convert.ToString(ut.RevenueReceived),
                                              CostTypeId = Convert.ToString(ut.CostTypeId),
                                              CostTypeName = ct1.Name,
                                              IsRefunded = Convert.ToString(ut.IsRefunded),
                                              RefundDetails = ut.RefundDetails,
                                              Notes = ut.Notes,
                                              SquareRefNo = ut.SquareRefNo,
                                              //ResponseText = ut.ResponseText,
                                              CreatedDate = Convert.ToString(ut.CreatedDate),
                                              LastUpdated = Convert.ToString(ut.LastUpdated),
                                              Refund =
                                                (
                                                    from ut1 in _appDbContext.UserTransactions
                                                    where (ut1.UserTransactionsId == ut.Id)
                                                    select new
                                                    {
                                                        Id = Convert.ToString(ut1.Id),
                                                        RefundAmount = Convert.ToString(ut1.RevenueReceived),
                                                        RefundDetails = ut1.RefundDetails,
                                                        RefundNotes = ut1.Notes,
                                                        SquareRefNo = ut1.SquareRefNo,
                                                        CreatedDatepo = Convert.ToString(ut1.CreatedDate)
                                                    }
                                                )
                                          };

                if (objUserTransactions == null || objUserTransactions.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Transaction doesn’t exist." }));
                }

                return Ok(new { Status = "OK", UserTransactions = objUserTransactions });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/usertransactions/GetHistoricUserTransactionByPeriod
        [HttpPost]
        [Route("GetHistoricUserTransactionByPeriod")]
        public ActionResult GetHistoricUserTransactionByPeriod([FromBody] JObject objJson)
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

                var objUserTransactions = from ut in _appDbContext.UserTransactions
                                          join u in _appDbContext.Users on ut.UserId equals u.UserId into DetailsUsers
                                          from u1 in DetailsUsers.DefaultIfEmpty()
                                          join uph in _appDbContext.UserPaymentHistory on ut.PaymentId equals uph.Id into DetailsUserPaymentHistory
                                          from uph1 in DetailsUserPaymentHistory.DefaultIfEmpty()
                                          join m in _appDbContext.Merchants on ut.MerchantId equals m.MerchantId into DetailsMerchants
                                          from m1 in DetailsMerchants.DefaultIfEmpty()
                                          join mp in _appDbContext.MerchantPackages on ut.PackageId equals mp.PackageId into DetailsMerchantPackages
                                          from mp1 in DetailsMerchantPackages.DefaultIfEmpty()
                                          join mc in _appDbContext.MerchantCampaigns on mp1.CampaignID equals mc.Id into DetailsMerchantCampaigns
                                          from mc1 in DetailsMerchantCampaigns.DefaultIfEmpty()
                                          join e in _appDbContext.Events on ut.EventId equals e.Id into DetailsEvents
                                          from e1 in DetailsEvents.DefaultIfEmpty()
                                          join tt in _appDbContext.TransactionType on ut.TransactionTypeId equals tt.Id into DetailsTransactionType
                                          from tt1 in DetailsTransactionType.DefaultIfEmpty()
                                          join ct in _appDbContext.CostType on ut.CostTypeId equals ct.Id into DetailsCostType
                                          from ct1 in DetailsCostType.DefaultIfEmpty()
                                          join pt in _appDbContext.PaymentType on ut.PaymentTypeId equals pt.Id into DetailsPaymentType
                                          from pt1 in DetailsPaymentType.DefaultIfEmpty()
                                          where (Convert.ToDateTime(ut.CreatedDate.ToString("yyyy-MM-dd")) > Convert.ToDateTime(DateTime.UtcNow.AddDays(-Convert.ToInt32(objJson.SelectToken("numberofdays"))).ToString("yyyy-MM-dd")) && ut.UserId == strUserId && ut.PaymentTypeId == 1)
                                          select new
                                          {
                                              Id = Convert.ToString(ut.Id),
                                              UserId = Convert.ToString(ut.UserId),
                                              UserName = (u1.FirstName + ' ' + u1.LastName),
                                              PaymentId = Convert.ToString(ut.PaymentId),
                                              MerchantId = Convert.ToString(ut.MerchantId),
                                              MerchantName = m1.MerchantName,
                                              PackageId = Convert.ToString(ut.PackageId),
                                              MerchantCampaignsId = Convert.ToString(mc1.Id),
                                              MerchantCampaignsName = mc1.Name,
                                              EventId = Convert.ToString(ut.EventId),
                                              TransactionTypeId = Convert.ToString(ut.TransactionTypeId),
                                              TransactionTypeDescription = tt1.Description,
                                              PaymentTypeId = Convert.ToString(pt1.Id),
                                              PaymentTypeDescription = pt1.Description,
                                              UserTransactionsId = Convert.ToString(ut.UserTransactionsId),
                                              SquareCustomerCardDetailsId = Convert.ToString(ut.SquareCustomerCardDetailsId),
                                              InitialRevenue = Convert.ToString(ut.InitialRevenue),
                                              DiscountPercent = Convert.ToString(ut.DiscountPercent),
                                              Discount = Convert.ToString(ut.Discount),
                                              RevenueReceived = Convert.ToString(ut.RevenueReceived),
                                              CostTypeId = Convert.ToString(ut.CostTypeId),
                                              CostTypeName = ct1.Name,
                                              IsRefunded = Convert.ToString(ut.IsRefunded),
                                              RefundDetails = ut.RefundDetails,
                                              Notes = ut.Notes,
                                              SquareRefNo = ut.SquareRefNo,
                                              //ResponseText = ut.ResponseText,
                                              CreatedDate = Convert.ToString(ut.CreatedDate),
                                              LastUpdated = Convert.ToString(ut.LastUpdated),
                                              Refund =
                                                (
                                                    from ut1 in _appDbContext.UserTransactions
                                                    where (ut1.UserTransactionsId == ut.Id)
                                                    select new
                                                    {
                                                        Id = Convert.ToString(ut1.Id),
                                                        RefundAmount = Convert.ToString(ut1.RevenueReceived),
                                                        RefundDetails = ut1.RefundDetails,
                                                        RefundNotes = ut1.Notes,
                                                        SquareRefNo = ut1.SquareRefNo,
                                                        CreatedDatepo = Convert.ToString(ut1.CreatedDate)
                                                    }
                                                )
                                          };

                if (objUserTransactions == null || objUserTransactions.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Transaction doesn’t exist." }));
                }

                return Ok(new { Status = "OK", UserTransactions = objUserTransactions });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/usertransactions/GetMerchantTransactionByPeriod
        [HttpPost]
        [Route("GetMerchantTransactionByPeriod")]
        public ActionResult GetMerchantTransactionByPeriod([FromBody] JObject objJson)
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

                var objUserTransactions = from ut in _appDbContext.UserTransactions
                                          join u in _appDbContext.Users on ut.UserId equals u.UserId into DetailsUsers
                                          from u1 in DetailsUsers.DefaultIfEmpty()
                                          join uph in _appDbContext.UserPaymentHistory on ut.PaymentId equals uph.Id into DetailsUserPaymentHistory
                                          from uph1 in DetailsUserPaymentHistory.DefaultIfEmpty()
                                          join m in _appDbContext.Merchants on ut.MerchantId equals m.MerchantId into DetailsMerchants
                                          from m1 in DetailsMerchants.DefaultIfEmpty()
                                          join mp in _appDbContext.MerchantPackages on ut.PackageId equals mp.PackageId into DetailsMerchantPackages
                                          from mp1 in DetailsMerchantPackages.DefaultIfEmpty()
                                          join mc in _appDbContext.MerchantCampaigns on mp1.CampaignID equals mc.Id into DetailsMerchantCampaigns
                                          from mc1 in DetailsMerchantCampaigns.DefaultIfEmpty()
                                          join e in _appDbContext.Events on ut.EventId equals e.Id into DetailsEvents
                                          from e1 in DetailsEvents.DefaultIfEmpty()
                                          join tt in _appDbContext.TransactionType on ut.TransactionTypeId equals tt.Id into DetailsTransactionType
                                          from tt1 in DetailsTransactionType.DefaultIfEmpty()
                                          join ct in _appDbContext.CostType on ut.CostTypeId equals ct.Id into DetailsCostType
                                          from ct1 in DetailsCostType.DefaultIfEmpty()
                                          join pt in _appDbContext.PaymentType on ut.PaymentTypeId equals pt.Id into DetailsPaymentType
                                          from pt1 in DetailsPaymentType.DefaultIfEmpty()
                                          where (Convert.ToDateTime(ut.CreatedDate.ToString("yyyy-MM-dd")) > Convert.ToDateTime(DateTime.UtcNow.AddDays(-Convert.ToInt32(objJson.SelectToken("numberofdays"))).ToString("yyyy-MM-dd")) && ut.MerchantId == Convert.ToString(objJson.SelectToken("merchantid")) && ut.PaymentTypeId == 1)
                                          select new
                                          {
                                              Id = Convert.ToString(ut.Id),
                                              UserId = Convert.ToString(ut.UserId),
                                              UserName = (u1.FirstName + ' ' + u1.LastName),
                                              PaymentId = Convert.ToString(ut.PaymentId),
                                              MerchantId = Convert.ToString(ut.MerchantId),
                                              MerchantName = m1.MerchantName,
                                              PackageId = Convert.ToString(ut.PackageId),
                                              MerchantCampaignsId = Convert.ToString(mc1.Id),
                                              MerchantCampaignsName = mc1.Name,
                                              EventId = Convert.ToString(ut.EventId),
                                              TransactionTypeId = Convert.ToString(ut.TransactionTypeId),
                                              TransactionTypeDescription = tt1.Description,
                                              PaymentTypeId = Convert.ToString(pt1.Id),
                                              PaymentTypeDescription = pt1.Description,
                                              UserTransactionsId = Convert.ToString(ut.UserTransactionsId),
                                              SquareCustomerCardDetailsId = Convert.ToString(ut.SquareCustomerCardDetailsId),
                                              InitialRevenue = Convert.ToString(ut.InitialRevenue),
                                              DiscountPercent = Convert.ToString(ut.DiscountPercent),
                                              Discount = Convert.ToString(ut.Discount),
                                              RevenueReceived = Convert.ToString(ut.RevenueReceived),
                                              CostTypeId = Convert.ToString(ut.CostTypeId),
                                              CostTypeName = ct1.Name,
                                              IsRefunded = Convert.ToString(ut.IsRefunded),
                                              RefundDetails = ut.RefundDetails,
                                              Notes = ut.Notes,
                                              SquareRefNo = ut.SquareRefNo,
                                              //ResponseText = ut.ResponseText,
                                              CreatedDate = Convert.ToString(ut.CreatedDate),
                                              LastUpdated = Convert.ToString(ut.LastUpdated),
                                              Refund =
                                                (
                                                    from ut1 in _appDbContext.UserTransactions
                                                    where (ut1.UserTransactionsId == ut.Id)
                                                    select new
                                                    {
                                                        Id = Convert.ToString(ut1.Id),
                                                        RefundAmount = Convert.ToString(ut1.RevenueReceived),
                                                        RefundDetails = ut1.RefundDetails,
                                                        RefundNotes = ut1.Notes,
                                                        SquareRefNo = ut1.SquareRefNo,
                                                        CreatedDatepo = Convert.ToString(ut1.CreatedDate)
                                                    }
                                                )
                                          };

                if (objUserTransactions == null || objUserTransactions.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Transaction doesn’t exist." }));
                }

                return Ok(new { Status = "OK", UserTransactions = objUserTransactions });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/usertransactions/CreateRefundByTransactionId
        [HttpPost]
        [Route("CreateRefundByTransactionId")]
        public async Task<ActionResult> CreateRefundByTransactionId([FromBody] JObject objJson)
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

                var objUserTransactionsData = _appDbContext.UserTransactions
                                .Where(ut => ut.UserId == strUserId && ut.Id == Convert.ToInt32(objJson.SelectToken("usertransactionid")))
                                .AsNoTracking().OrderByDescending(t => t.Id).FirstOrDefault();

                if (objUserTransactionsData == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Invalid user transaction." }));
                }

                if (objUserTransactionsData.IsRefunded == true)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Amount alreaded refuned." }));
                }

                string strSquareRequestUrl = Convert.ToString(configuration.GetSection("appSettings").GetSection("SquareRequestUrl").Value);
                string strSquareAccessToken = Convert.ToString(configuration.GetSection("appSettings").GetSection("SquareAccessToken").Value);

                if (strSquareRequestUrl == "" || strSquareAccessToken == "")
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                var objEvents = _appDbContext.Events
                                .Where(e => e.Id == Convert.ToInt32(objUserTransactionsData.EventId))
                                .AsNoTracking().OrderByDescending(t => t.Id).FirstOrDefault();

                if (objEvents == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Event doesn't exist." }));
                }

                if (DateTime.UtcNow > Convert.ToDateTime(objEvents.EventDate))
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Event closed. You can not refund." }));
                }

                decimal RefundAmount = 0;
                string strNote = "No refund.";
                if (DateTime.UtcNow < Convert.ToDateTime(objEvents.EventDate).AddHours(-24))
                {
                    //100% refund
                    RefundAmount = Convert.ToDecimal(objUserTransactionsData.RevenueReceived);
                    strNote = "100% refund";
                }
                else if (DateTime.UtcNow > Convert.ToDateTime(objEvents.EventDate).AddHours(-24) && DateTime.UtcNow < Convert.ToDateTime(objEvents.EventDate).AddHours(-12))
                {
                    //75% refund
                    RefundAmount = (Convert.ToDecimal(objUserTransactionsData.RevenueReceived) * 75) / 100;
                    strNote = "75% refund";
                }
                else if (DateTime.UtcNow > Convert.ToDateTime(objEvents.EventDate).AddHours(-12) && DateTime.UtcNow < Convert.ToDateTime(objEvents.EventDate).AddHours(-6))
                {
                    //50% refund
                    RefundAmount = (Convert.ToDecimal(objUserTransactionsData.RevenueReceived) * 50) / 100;
                    strNote = "50% refund";
                }

                //return Ok(new { Status = "OK", Detail = "Refund amount = " + Convert.ToString(RefundAmount) });

                string strRequestUrl = strSquareRequestUrl + "refunds";
                string strRequestData = "{\"idempotency_key\": \"" + Convert.ToString(Guid.NewGuid()) + "\",\"payment_id\": \"" + Convert.ToString(objUserTransactionsData.SquareRefNo) + "\",\"amount_money\": { \"amount\": " + Convert.ToInt32(RefundAmount * 100) + ", \"currency\": \"CAD\" }}";
                WebRequest objWebRequest = WebRequest.Create(strRequestUrl);
                objWebRequest.Method = "POST";
                byte[] byteArray;
                byteArray = Encoding.UTF8.GetBytes(strRequestData);
                objWebRequest.ContentType = "application/json";
                objWebRequest.ContentLength = byteArray.Length;
                objWebRequest.Headers.Add("Authorization", "Bearer " + strSquareAccessToken);
                Stream dataStream = objWebRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse objWebResponse = objWebRequest.GetResponse();
                dataStream = objWebResponse.GetResponseStream();
                StreamReader dataReader = new StreamReader(dataStream);
                string strResponse = dataReader.ReadToEnd();
                dataReader.Close();
                dataStream.Close();
                objWebResponse.Close();

                if (string.IsNullOrEmpty(strResponse))
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                string strSquareRefundId = "";

                JObject objResponseJson = JObject.Parse(strResponse);
                if (objResponseJson.SelectToken("refund") != null)
                {
                    strSquareRefundId = Convert.ToString(objResponseJson.SelectToken("refund").SelectToken("id"));
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                UserTransactions objUserTransactions = new UserTransactions();
                objUserTransactions.UserId = objUserTransactionsData.UserId;
                objUserTransactions.PaymentId = objUserTransactionsData.PaymentId;
                objUserTransactions.MerchantId = objUserTransactionsData.MerchantId;
                objUserTransactions.PackageId = objUserTransactionsData.PackageId;
                objUserTransactions.EventId = objUserTransactionsData.EventId;
                objUserTransactions.TransactionTypeId = objUserTransactionsData.TransactionTypeId;
                objUserTransactions.PaymentTypeId = 2;
                objUserTransactions.UserTransactionsId = objUserTransactionsData.Id;
                objUserTransactions.SquareCustomerCardDetailsId = objUserTransactionsData.SquareCustomerCardDetailsId;
                objUserTransactions.InitialRevenue = 0;
                objUserTransactions.DiscountPercent = 0;
                objUserTransactions.Discount = 0;
                objUserTransactions.RevenueReceived = RefundAmount;
                objUserTransactions.CostTypeId = objUserTransactionsData.CostTypeId;
                objUserTransactions.IsRefunded = true;
                objUserTransactions.RefundDetails = Convert.ToString(objJson.SelectToken("refundreason"));
                objUserTransactions.Notes = strNote;
                objUserTransactions.SquareRefNo = strSquareRefundId;
                objUserTransactions.ResponseText = strResponse;
                objUserTransactions.CreatedDate = DateTime.UtcNow;
                await _appDbContext.UserTransactions.AddAsync(objUserTransactions);
                int returnVal = await _appDbContext.SaveChangesAsync();

                if (returnVal > 0)
                {
                    UserTransactions objUserTransactions1 = new UserTransactions();
                    objUserTransactions1.Id = objUserTransactionsData.Id;
                    objUserTransactions1.IsRefunded = true;
                    objUserTransactions1.LastUpdated = DateTime.UtcNow;
                    _appDbContext.UserTransactions.Attach(objUserTransactions1);
                    _appDbContext.Entry(objUserTransactions1).Property("IsRefunded").IsModified = true;
                    _appDbContext.Entry(objUserTransactions1).Property("LastUpdated").IsModified = true;
                    returnVal = await _appDbContext.SaveChangesAsync();

                    return Ok(new { Status = "OK", Detail = "Your refund request is being proceed." });
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

        // POST api/usertransactions/CheckRefundStatusByTransactionId
        [HttpPost]
        [Route("CheckRefundStatusByTransactionId")]
        public ActionResult CheckRefundStatusByTransactionId([FromBody] JObject objJson)
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

                var objUserTransactionsData = _appDbContext.UserTransactions
                                .Where(ut => ut.UserId == strUserId && ut.Id == Convert.ToInt32(objJson.SelectToken("usertransactionid")) && ut.PaymentTypeId == 1)
                                .AsNoTracking().OrderByDescending(t => t.Id).FirstOrDefault();

                if (objUserTransactionsData == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Invalid user transaction." }));
                }

                if (objUserTransactionsData.IsRefunded == false)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "You did not applied for refund." }));
                }

                var objUserTransactionsDataRefund = _appDbContext.UserTransactions
                                .Where(ut => ut.UserId == strUserId && ut.UserTransactionsId == Convert.ToInt32(objJson.SelectToken("usertransactionid")) && ut.PaymentTypeId == 2)
                                .AsNoTracking().OrderByDescending(t => t.Id).FirstOrDefault();

                if (objUserTransactionsDataRefund == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Invalid user transaction." }));
                }

                string strSquareRequestUrl = Convert.ToString(configuration.GetSection("appSettings").GetSection("SquareRequestUrl").Value);
                string strSquareAccessToken = Convert.ToString(configuration.GetSection("appSettings").GetSection("SquareAccessToken").Value);

                if (strSquareRequestUrl == "" || strSquareAccessToken == "")
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                string strSquareRefundId = objUserTransactionsDataRefund.SquareRefNo;

                string strRequestUrl = strSquareRequestUrl + "refunds/" + strSquareRefundId;
                WebRequest objWebRequest = WebRequest.Create(strRequestUrl);
                objWebRequest.Method = "GET";
                objWebRequest.ContentType = "application/json";
                objWebRequest.Headers.Add("Authorization", "Bearer " + strSquareAccessToken);
                //Stream dataStream = objWebRequest.GetRequestStream();
                //dataStream.Close();
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
                return Ok(new { Status = "OK", SquareRefundDetails = objResponseJson });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/usertransactions/CreateRefundRequestbyTransactionId
        [HttpPost]
        [Route("CreateRefundRequestbyTransactionId")]
        public ActionResult CreateRefundRequestbyTransactionId([FromBody] JObject objJson)
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

                var objUserTransactionsData = (from ut in _appDbContext.UserTransactions
                                               join u in _appDbContext.Users on ut.UserId equals u.UserId into DetailsUsers
                                               from u1 in DetailsUsers.DefaultIfEmpty()
                                               join e in _appDbContext.Events on ut.EventId equals e.Id into DetailsEvents
                                               from e1 in DetailsEvents.DefaultIfEmpty()
                                               where (ut.UserId == strUserId && ut.Id == Convert.ToInt32(objJson.SelectToken("usertransactionid")) && ut.PaymentTypeId == 1)
                                               orderby ut.Id descending
                                               select new { ut, u1, e1 }).ToList();

                if (objUserTransactionsData == null || objUserTransactionsData.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Invalid user transaction." }));
                }

                if (objUserTransactionsData[0].ut.IsRefunded == true)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "You already applied for refund." }));
                }

                StringBuilder objEmailBody = new StringBuilder();
                objEmailBody.AppendLine("<html>");
                objEmailBody.AppendLine("<body>");
                objEmailBody.AppendLine("<div>");
                objEmailBody.AppendLine("Request for Refund <br/>");
                objEmailBody.AppendLine("<br/>Transaction Id : " + Convert.ToString(objUserTransactionsData[0].ut.Id));
                objEmailBody.AppendLine("<br/>User Id : " + Convert.ToString(objUserTransactionsData[0].ut.UserId));
                objEmailBody.AppendLine("<br/>User Name : " + Convert.ToString(objUserTransactionsData[0].u1.FirstName) + " " + Convert.ToString(objUserTransactionsData[0].u1.LastName));
                objEmailBody.AppendLine("<br/>Event Id : " + Convert.ToString(objUserTransactionsData[0].ut.EventId));
                objEmailBody.AppendLine("<br/>Transaction Amount : " + Convert.ToString(objUserTransactionsData[0].ut.RevenueReceived));
                objEmailBody.AppendLine("<br/>Date of Transaction : " + Convert.ToString(objUserTransactionsData[0].ut.CreatedDate));
                objEmailBody.AppendLine("<br/>Refund Reason : " + Convert.ToString(objJson.SelectToken("refundreason")));
                objEmailBody.AppendLine("<br/><br/>");
                objEmailBody.AppendLine("Thank You.");
                objEmailBody.AppendLine("</div>");
                objEmailBody.AppendLine("</body>");
                objEmailBody.AppendLine("</html>");

                string strMailStatus = new clsEmail(configuration).SendEmail(Convert.ToString(configuration.GetSection("appSettings").GetSection("RefundRequestEmail").Value), "Refund Request", Convert.ToString(objEmailBody), "Nurchure - Refund Request");
                if (strMailStatus == "Success")
                {
                    return Ok(new { Status = "OK", Description = "Your request for refund send successfully." });
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = strMailStatus }));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/usertransactions/CreateGroupRefundRequestByEventId
        [HttpPost]
        [Route("CreateGroupRefundRequestByEventId")]
        public ActionResult CreateGroupRefundRequestByEventId([FromBody] JObject objJson)
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

                var objUserTransactionsData = (from ut in _appDbContext.UserTransactions
                                               join u in _appDbContext.Users on ut.UserId equals u.UserId into DetailsUsers
                                               from u1 in DetailsUsers.DefaultIfEmpty()
                                               join e in _appDbContext.Events on ut.EventId equals e.Id into DetailsEvents
                                               from e1 in DetailsEvents.DefaultIfEmpty()
                                               where (ut.EventId == Convert.ToInt32(objJson.SelectToken("eventid")) && ut.PaymentTypeId == 1)
                                               select new { ut, u1, e1 }).ToList();

                if (objUserTransactionsData == null || objUserTransactionsData.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Invalid Event Id." }));
                }

                StringBuilder objEmailBody = new StringBuilder();
                objEmailBody.AppendLine("<html>");
                objEmailBody.AppendLine("<body>");
                objEmailBody.AppendLine("<div>");
                objEmailBody.AppendLine("Request for Refund <br/>");
                objEmailBody.AppendLine("Refund Reason : <b>" + Convert.ToString(objJson.SelectToken("refundreason")) + "</b><br/><br/>");
                objEmailBody.AppendLine("<table border='1' width='100%' style='text-align: center;'>");
                objEmailBody.AppendLine("<tr>");
                objEmailBody.AppendLine("<th>Transaction Id</th>");
                objEmailBody.AppendLine("<th>User Id</th>");
                objEmailBody.AppendLine("<th>User Name</th>");
                objEmailBody.AppendLine("<th>Event Id</th>");
                objEmailBody.AppendLine("<th>Transaction Amount</th>");
                objEmailBody.AppendLine("<th>Date of Transaction</th>");
                objEmailBody.AppendLine("</tr>");
                for (int i = 0; i < objUserTransactionsData.Count(); i++)
                {
                    objEmailBody.AppendLine("<tr>");
                    objEmailBody.AppendLine("<td>" + Convert.ToString(objUserTransactionsData[i].ut.Id) + "</td>");
                    objEmailBody.AppendLine("<td>" + Convert.ToString(objUserTransactionsData[i].ut.UserId) + "</td>");
                    objEmailBody.AppendLine("<td>" + Convert.ToString(objUserTransactionsData[i].u1.FirstName) + " " + Convert.ToString(objUserTransactionsData[0].u1.LastName) + "</td>");
                    objEmailBody.AppendLine("<td>" + Convert.ToString(objUserTransactionsData[i].ut.EventId) + "</td>");
                    objEmailBody.AppendLine("<td>" + Convert.ToString(objUserTransactionsData[i].ut.RevenueReceived) + "</td>");
                    objEmailBody.AppendLine("<td>" + Convert.ToString(objUserTransactionsData[i].ut.CreatedDate) + "</td>");
                    objEmailBody.AppendLine("</tr>");
                }
                objEmailBody.AppendLine("</table>");
                objEmailBody.AppendLine("<br/><br/>");
                objEmailBody.AppendLine("Thank You.");
                objEmailBody.AppendLine("</div>");
                objEmailBody.AppendLine("</body>");
                objEmailBody.AppendLine("</html>");

                string strMailStatus = new clsEmail(configuration).SendEmail(Convert.ToString(configuration.GetSection("appSettings").GetSection("RefundRequestEmail").Value), "Refund Request", Convert.ToString(objEmailBody), "Nurchure - Refund Request");
                if (strMailStatus == "Success")
                {
                    return Ok(new { Status = "OK", Description = "Your request for refund send successfully." });
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = strMailStatus }));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/usertransactions/CheckRefundHistory
        [HttpPost]
        [Route("CheckRefundHistory")]
        public ActionResult CheckRefundHistory([FromBody] JObject objJson)
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

                var objUserTransactions = from ut in _appDbContext.UserTransactions
                                          join u in _appDbContext.Users on ut.UserId equals u.UserId into DetailsUsers
                                          from u1 in DetailsUsers.DefaultIfEmpty()
                                          join uph in _appDbContext.UserPaymentHistory on ut.PaymentId equals uph.Id into DetailsUserPaymentHistory
                                          from uph1 in DetailsUserPaymentHistory.DefaultIfEmpty()
                                          join m in _appDbContext.Merchants on ut.MerchantId equals m.MerchantId into DetailsMerchants
                                          from m1 in DetailsMerchants.DefaultIfEmpty()
                                          join mp in _appDbContext.MerchantPackages on ut.PackageId equals mp.PackageId into DetailsMerchantPackages
                                          from mp1 in DetailsMerchantPackages.DefaultIfEmpty()
                                          join mc in _appDbContext.MerchantCampaigns on mp1.CampaignID equals mc.Id into DetailsMerchantCampaigns
                                          from mc1 in DetailsMerchantCampaigns.DefaultIfEmpty()
                                          join e in _appDbContext.Events on ut.EventId equals e.Id into DetailsEvents
                                          from e1 in DetailsEvents.DefaultIfEmpty()
                                          join tt in _appDbContext.TransactionType on ut.TransactionTypeId equals tt.Id into DetailsTransactionType
                                          from tt1 in DetailsTransactionType.DefaultIfEmpty()
                                          join ct in _appDbContext.CostType on ut.CostTypeId equals ct.Id into DetailsCostType
                                          from ct1 in DetailsCostType.DefaultIfEmpty()
                                          join pt in _appDbContext.PaymentType on ut.PaymentTypeId equals pt.Id into DetailsPaymentType
                                          from pt1 in DetailsPaymentType.DefaultIfEmpty()
                                          where (Convert.ToDateTime(ut.CreatedDate.ToString("yyyy-MM-dd")) > Convert.ToDateTime(DateTime.UtcNow.AddDays(-Convert.ToInt32(objJson.SelectToken("numberofdays"))).ToString("yyyy-MM-dd")) && ut.UserId == strUserId && ut.PaymentTypeId == 2)
                                          select new
                                          {
                                              Id = Convert.ToString(ut.Id),
                                              UserId = Convert.ToString(ut.UserId),
                                              UserName = (u1.FirstName + ' ' + u1.LastName),
                                              PaymentId = Convert.ToString(ut.PaymentId),
                                              MerchantId = Convert.ToString(ut.MerchantId),
                                              MerchantName = m1.MerchantName,
                                              PackageId = Convert.ToString(ut.PackageId),
                                              MerchantCampaignsId = Convert.ToString(mc1.Id),
                                              MerchantCampaignsName = mc1.Name,
                                              EventId = Convert.ToString(ut.EventId),
                                              TransactionTypeId = Convert.ToString(ut.TransactionTypeId),
                                              TransactionTypeDescription = tt1.Description,
                                              PaymentTypeId = Convert.ToString(pt1.Id),
                                              PaymentTypeDescription = pt1.Description,
                                              UserTransactionsId = Convert.ToString(ut.UserTransactionsId),
                                              SquareCustomerCardDetailsId = Convert.ToString(ut.SquareCustomerCardDetailsId),
                                              RefundAmount = Convert.ToString(ut.RevenueReceived),
                                              CostTypeId = Convert.ToString(ut.CostTypeId),
                                              CostTypeName = ct1.Name,
                                              IsRefunded = Convert.ToString(ut.IsRefunded),
                                              RefundDetails = ut.RefundDetails,
                                              Notes = ut.Notes,
                                              SquareRefNo = ut.SquareRefNo,
                                              //ResponseText = ut.ResponseText,
                                              CreatedDate = Convert.ToString(ut.CreatedDate),
                                              LastUpdated = Convert.ToString(ut.LastUpdated)
                                          };

                if (objUserTransactions == null || objUserTransactions.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Refund doesn’t exist." }));
                }

                return Ok(new { Status = "OK", RefundHistory = objUserTransactions });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/usertransactions/InitiateAutoUserRefundByEventId
        [HttpPost]
        [Route("InitiateAutoUserRefundByEventId")]
        public async Task<ActionResult> InitiateAutoUserRefundByEventId([FromBody] JObject objJson)
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

                var objUserTransactionsData = _appDbContext.UserTransactions
                                .Where(ut => ut.EventId == Convert.ToInt32(objJson.SelectToken("eventid")) && ut.PaymentTypeId == 1)
                                .AsNoTracking().ToList<UserTransactions>();

                if (objUserTransactionsData.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "No any transaction found for that event." }));
                }

                var objEvents = _appDbContext.Events
                                .Where(e => e.Id == Convert.ToInt32(objJson.SelectToken("eventid")))
                                .AsNoTracking().OrderByDescending(t => t.Id).FirstOrDefault();

                if (objEvents == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Event doesn't exist." }));
                }

                if (DateTime.UtcNow > Convert.ToDateTime(objEvents.EventDate))
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Event closed. You can not refund." }));
                }

                string strSquareRequestUrl = Convert.ToString(configuration.GetSection("appSettings").GetSection("SquareRequestUrl").Value);
                string strSquareAccessToken = Convert.ToString(configuration.GetSection("appSettings").GetSection("SquareAccessToken").Value);

                if (strSquareRequestUrl == "" || strSquareAccessToken == "")
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                }

                bool bnlErrorflag = false;
                foreach (UserTransactions objItem in objUserTransactionsData)
                {
                    if (objItem.IsRefunded == true)
                    {
                        continue;
                    }

                    decimal RefundAmount = 0;
                    string strNote = "No refund.";
                    if (DateTime.UtcNow < Convert.ToDateTime(objEvents.EventDate).AddHours(-24))
                    {
                        //100% refund
                        RefundAmount = Convert.ToDecimal(objItem.RevenueReceived);
                        strNote = "100% refund";
                    }
                    else if (DateTime.UtcNow > Convert.ToDateTime(objEvents.EventDate).AddHours(-24) && DateTime.UtcNow < Convert.ToDateTime(objEvents.EventDate).AddHours(-12))
                    {
                        //75% refund
                        RefundAmount = (Convert.ToDecimal(objItem.RevenueReceived) * 75) / 100;
                        strNote = "75% refund";
                    }
                    else if (DateTime.UtcNow > Convert.ToDateTime(objEvents.EventDate).AddHours(-12) && DateTime.UtcNow < Convert.ToDateTime(objEvents.EventDate).AddHours(-6))
                    {
                        //50% refund
                        RefundAmount = (Convert.ToDecimal(objItem.RevenueReceived) * 50) / 100;
                        strNote = "50% refund";
                    }

                    string strRequestUrl = strSquareRequestUrl + "refunds";
                    string strRequestData = "{\"idempotency_key\": \"" + Convert.ToString(Guid.NewGuid()) + "\",\"payment_id\": \"" + Convert.ToString(objItem.SquareRefNo) + "\",\"amount_money\": { \"amount\": " + Convert.ToInt32(RefundAmount * 100) + ", \"currency\": \"CAD\" }}";
                    string strResponse = "";

                    try
                    {
                        WebRequest objWebRequest = WebRequest.Create(strRequestUrl);
                        objWebRequest.Method = "POST";
                        byte[] byteArray;
                        byteArray = Encoding.UTF8.GetBytes(strRequestData);
                        objWebRequest.ContentType = "application/json";
                        objWebRequest.ContentLength = byteArray.Length;
                        objWebRequest.Headers.Add("Authorization", "Bearer " + strSquareAccessToken);
                        Stream dataStream = objWebRequest.GetRequestStream();
                        dataStream.Write(byteArray, 0, byteArray.Length);
                        dataStream.Close();
                        WebResponse objWebResponse = objWebRequest.GetResponse();
                        dataStream = objWebResponse.GetResponseStream();
                        StreamReader dataReader = new StreamReader(dataStream);
                        strResponse = dataReader.ReadToEnd();
                        dataReader.Close();
                        dataStream.Close();
                        objWebResponse.Close();
                    }
                    catch (WebException e)
                    {
                        bnlErrorflag = true;
                        using (WebResponse response = e.Response)
                        {
                            HttpWebResponse httpResponse = (HttpWebResponse)response;
                            //Console.WriteLine("Error code: {0}", httpResponse.StatusCode);
                            using (Stream data = response.GetResponseStream())
                            using (var reader = new StreamReader(data))
                            {
                                string text = reader.ReadToEnd();
                                PaymentErrorHistory objPaymentErrorHistory = new PaymentErrorHistory();
                                objPaymentErrorHistory.UserTransactionId = objItem.Id;
                                objPaymentErrorHistory.PaymentTypeId = 2;
                                objPaymentErrorHistory.Amount = RefundAmount;
                                objPaymentErrorHistory.Notes = strNote + ", Refund Reason : " + Convert.ToString(objJson.SelectToken("refundreason"));
                                objPaymentErrorHistory.ResponseError = text;
                                objPaymentErrorHistory.CreatedDate = DateTime.UtcNow;
                                await _appDbContext.PaymentErrorHistory.AddAsync(objPaymentErrorHistory);
                                int errorReturnVal = await _appDbContext.SaveChangesAsync();
                            }
                        }
                        continue;
                    }

                    if (string.IsNullOrEmpty(strResponse))
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                    }

                    string strSquareRefundId = "";

                    JObject objResponseJson = JObject.Parse(strResponse);
                    if (objResponseJson.SelectToken("refund") != null)
                    {
                        strSquareRefundId = Convert.ToString(objResponseJson.SelectToken("refund").SelectToken("id"));
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                    }

                    UserTransactions objUserTransactions = new UserTransactions();
                    objUserTransactions.UserId = objItem.UserId;
                    objUserTransactions.PaymentId = objItem.PaymentId;
                    objUserTransactions.MerchantId = objItem.MerchantId;
                    objUserTransactions.PackageId = objItem.PackageId;
                    objUserTransactions.EventId = objItem.EventId;
                    objUserTransactions.TransactionTypeId = objItem.TransactionTypeId;
                    objUserTransactions.PaymentTypeId = 2;
                    objUserTransactions.UserTransactionsId = objItem.Id;
                    objUserTransactions.SquareCustomerCardDetailsId = objItem.SquareCustomerCardDetailsId;
                    objUserTransactions.InitialRevenue = 0;
                    objUserTransactions.DiscountPercent = 0;
                    objUserTransactions.Discount = 0;
                    objUserTransactions.RevenueReceived = RefundAmount;
                    objUserTransactions.CostTypeId = objItem.CostTypeId;
                    objUserTransactions.IsRefunded = true;
                    objUserTransactions.RefundDetails = Convert.ToString(objJson.SelectToken("refundreason"));
                    objUserTransactions.Notes = strNote;
                    objUserTransactions.SquareRefNo = strSquareRefundId;
                    objUserTransactions.ResponseText = strResponse;
                    objUserTransactions.CreatedDate = DateTime.UtcNow;
                    await _appDbContext.UserTransactions.AddAsync(objUserTransactions);
                    int returnVal = await _appDbContext.SaveChangesAsync();

                    if (returnVal > 0)
                    {
                        UserTransactions objUserTransactions1 = new UserTransactions();
                        objUserTransactions1.Id = objItem.Id;
                        objUserTransactions1.IsRefunded = true;
                        objUserTransactions1.LastUpdated = DateTime.UtcNow;
                        _appDbContext.UserTransactions.Attach(objUserTransactions1);
                        _appDbContext.Entry(objUserTransactions1).Property("IsRefunded").IsModified = true;
                        _appDbContext.Entry(objUserTransactions1).Property("LastUpdated").IsModified = true;
                        returnVal = await _appDbContext.SaveChangesAsync();
                        //return Ok(new { Status = "OK", Detail = "Your refund request is being proceed." });
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support" }));
                    }
                }

                if (bnlErrorflag == false)
                {
                    return Ok(new { Status = "OK", Detail = "Your refund request is being proceed." });
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Some refund is not placed successfully. Please contact customer support" }));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

        // POST api/usertransactions/CalcDailyPNL
        [HttpPost]
        [Route("CalcDailyPNL")]
        public async Task<ActionResult> CalcDailyPNL()
        {
            try
            {
                var objMerchantDetails = _appDbContext.Merchants
                                .AsNoTracking().ToList<Merchants>();

                if (objMerchantDetails.Count() <= 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Merchant Not Found." }));
                }

                bool bnlErrorflag = false;
                foreach (Merchants objItem in objMerchantDetails)
                {
                    var objUserTransactionsDetails = _appDbContext.UserTransactions
                                .Where(ut => ut.MerchantId == objItem.MerchantId && Convert.ToDateTime(ut.CreatedDate.ToString("yyyy-MM-dd")) == Convert.ToDateTime(DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd")))
                                .AsNoTracking().ToList<UserTransactions>();

                    decimal dblTotalPurchaseAmount = 0;
                    decimal dblTotalRefundAmount = 0;
                    if (objUserTransactionsDetails.Count() > 0)
                    {
                        dblTotalPurchaseAmount = objUserTransactionsDetails.Where(t => t.PaymentTypeId == 1).Sum(t => t.RevenueReceived);
                        dblTotalRefundAmount = objUserTransactionsDetails.Where(t => t.PaymentTypeId == 2).Sum(t => t.RevenueReceived);
                    }

                    DailyPNL objDailyPNL = new DailyPNL();
                    objDailyPNL.MerchantId = objItem.MerchantId;
                    objDailyPNL.DailyPNLAmount = (dblTotalPurchaseAmount - dblTotalRefundAmount);
                    objDailyPNL.PNLDate = DateTime.UtcNow.AddDays(-1);
                    objDailyPNL.AddedDate = DateTime.UtcNow;
                    await _appDbContext.DailyPNL.AddAsync(objDailyPNL);
                    int returnVal = await _appDbContext.SaveChangesAsync();

                    if (returnVal <= 0)
                    {
                        bnlErrorflag = true;
                    }
                }

                if (bnlErrorflag == false)
                {
                    return Ok(new { Status = "OK", Detail = "Daily PNL calculate successfully." });
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Some record are not calculate successfully. Please contact customer support" }));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, (new { Status = "Error", Error = "Internal Server error. Please contact customer support", SystemError = ex.Message }));
            }
        }

    }
}