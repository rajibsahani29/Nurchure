using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class clsSMS
    {
        public IConfiguration configuration;

        public clsSMS(IConfiguration iConfig)
        {
            configuration = iConfig;
        }

        public string SendSMS(string strToPhoneNumber, string strBody)
        {
            try
            {
                if (strToPhoneNumber != "")
                {
                    string strApiKey = Convert.ToString(configuration.GetSection("appSettings").GetSection("SMSApiKey").Value);

                    if (strApiKey == "")
                    {
                        return "Api Key Missing";
                    }

                    string strRequestUrl = "https://api.sms.to/sms/send";
                    //string strRequestUrl = "https://api.sms.to/message/send";
                    //string strRequestUrl = "https://api.sms.to/v1/sms/send";
                    string strRequestData = "{\"message\": \"" + strBody + "\",\"to\": \"" + strToPhoneNumber + "\",\"sender_id\": \"NurchureAPP\"}";
                    WebRequest objWebRequest = WebRequest.Create(strRequestUrl);
                    objWebRequest.Method = "POST";
                    byte[] byteArray;
                    byteArray = Encoding.UTF8.GetBytes(strRequestData);
                    objWebRequest.ContentType = "application/json";
                    objWebRequest.ContentLength = byteArray.Length;
                    objWebRequest.Headers.Add("Authorization", "Bearer " + strApiKey);
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
                        return "Error";
                    }

                    JObject objResponseJson = JObject.Parse(strResponse);
                    if (!string.IsNullOrEmpty(Convert.ToString(objResponseJson.SelectToken("success"))))
                    {
                        if (Convert.ToBoolean(objResponseJson.SelectToken("success")) == true)
                        {
                            return "Success";
                        }
                        else
                        {
                            return "Error";
                        }
                    }
                    else
                    {
                        return "Error";
                    }
                }
                else
                {
                    return "PhoneNumber Missing";
                }
            }
            catch (Exception)
            {
                return "Error";
            }
        }
    }
}