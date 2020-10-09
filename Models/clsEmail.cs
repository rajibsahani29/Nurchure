using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net.Http;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace NurchureAPI.Models
{
    public class clsEmail
    {
        public IConfiguration configuration;

        public clsEmail(IConfiguration iConfig)
        {
            configuration = iConfig;
        }

        public string SendEmail(string strToEmail, string strSubject, string strBody, string strDisplayName, string strCC = "", string strBCC = "")
        {
            try
            {
                if (strToEmail != "")
                {
                    string strFromEmail = Convert.ToString(configuration.GetSection("appSettings").GetSection("MailFrom").Value);
                    string strPassword = Convert.ToString(configuration.GetSection("appSettings").GetSection("MailFromPass").Value);
                    string strHostName = Convert.ToString(configuration.GetSection("appSettings").GetSection("MailHost").Value);
                    int intPort = Convert.ToInt32(configuration.GetSection("appSettings").GetSection("MailPort").Value);

                    MailMessage objMailMessage = new MailMessage();
                    SmtpClient objSmtpClient = new SmtpClient();

                    objMailMessage.From = new MailAddress(strFromEmail, strDisplayName);
                    objSmtpClient.Host = strHostName;
                    if (intPort > 0)
                    {
                        objSmtpClient.Port = intPort;
                    }
                    objSmtpClient.Credentials = new NetworkCredential(strFromEmail, strPassword);
                    objMailMessage.IsBodyHtml = true;
                    objMailMessage.Body = strBody;
                    objMailMessage.Subject = strSubject;
                    objMailMessage.To.Add(strToEmail);

                    if (strCC != "")
                    {
                        objMailMessage.CC.Add(strCC);
                    }
                    if (strBCC != "")
                    {
                        objMailMessage.Bcc.Add(strBCC);
                    }

                    objSmtpClient.Send(objMailMessage);

                    return "Success";
                }
                else
                {
                    return "EmailIdMissing";
                }
            }
            catch (Exception)
            {
                return "Error";
            }
        }
    }
}
