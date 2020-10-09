using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class clsAppCrypt
    {
        public IConfiguration configuration;

        public clsAppCrypt(IConfiguration iConfig)
        {
            configuration = iConfig;
        }

        public string encryptQS(string thestring)
        {
            return System.Net.WebUtility.HtmlEncode(Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(thestring)));
        }
        public string decryptQS(string thestring)
        {
            return System.Net.WebUtility.HtmlDecode(System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(thestring)));
        }

        public static string GetSalt(int size)
        {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] buff = new byte[size];
            rng.GetBytes(buff);
            string salt = Convert.ToBase64String(buff);
            if (salt.Length > size)
            {
                salt = salt.Substring(0, size);
            }
            return salt;
        }

        /*public static string GetPasswordHash(string pwd, string salt)
        {
            string saltAndPwd = String.Concat(pwd, salt);
            string hashedPwd =
                FormsAuthentication.HashPasswordForStoringInConfigFile(
                saltAndPwd, "SHA1");
            return hashedPwd;
        }*/

        //Encryption method for credit card

        public string EncryptTripleDES(string Plaintext)
        {
            try
            {
                string Key = Convert.ToString(configuration.GetSection("appSettings").GetSection("EncryptKey").Value);
                byte[] Buffer = new byte[0];

                System.Security.Cryptography.TripleDESCryptoServiceProvider DES = new System.Security.Cryptography.TripleDESCryptoServiceProvider();

                System.Security.Cryptography.MD5CryptoServiceProvider hashMD5 = new System.Security.Cryptography.MD5CryptoServiceProvider();

                DES.Key = hashMD5.ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(Key));

                DES.Mode = System.Security.Cryptography.CipherMode.ECB;

                System.Security.Cryptography.ICryptoTransform DESEncrypt = DES.CreateEncryptor();

                Buffer = System.Text.ASCIIEncoding.ASCII.GetBytes(Plaintext);

                string TripleDES = Convert.ToBase64String(DESEncrypt.TransformFinalBlock(Buffer, 0, Buffer.Length));

                return TripleDES;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }


        //Decryption Method 

        public string DecryptTripleDES(string base64Text)
        {
            try
            {
                string Key = Convert.ToString(configuration.GetSection("appSettings").GetSection("EncryptKey").Value);
                byte[] Buffer = new byte[0];

                System.Security.Cryptography.TripleDESCryptoServiceProvider DES = new System.Security.Cryptography.TripleDESCryptoServiceProvider();

                System.Security.Cryptography.MD5CryptoServiceProvider hashMD5 = new System.Security.Cryptography.MD5CryptoServiceProvider();

                DES.Key = hashMD5.ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(Key));

                DES.Mode = System.Security.Cryptography.CipherMode.ECB;

                System.Security.Cryptography.ICryptoTransform DESDecrypt = DES.CreateDecryptor();

                Buffer = Convert.FromBase64String(base64Text);

                string DecTripleDES = System.Text.ASCIIEncoding.ASCII.GetString(DESDecrypt.TransformFinalBlock(Buffer, 0, Buffer.Length));

                return DecTripleDES;
            }
            catch (Exception)
            {

                return string.Empty;
            }
        }
    }
}
