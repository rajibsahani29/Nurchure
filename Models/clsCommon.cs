using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class clsCommon
    {
        public static string GetHashedValue(string strVal)
        {
            try
            {
                var bytes = new UTF8Encoding().GetBytes(strVal);
                byte[] hashBytes;
                using (var algorithm = new System.Security.Cryptography.SHA512Managed())
                {
                    hashBytes = algorithm.ComputeHash(bytes);
                }
                string strHashedValue = Convert.ToBase64String(hashBytes);
                return strHashedValue;
            }
            catch (Exception ex)
            {
                return Convert.ToString(ex.Message);
            }
        }

        //Only numeric OTP
        public static string GetRandomNumeric()
        {
            try
            {
                System.Random objRand = new System.Random();
               // var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
                var numbers = "0123456789";
                string randomNumeric = new string(numbers.Select(c => numbers[objRand.Next(numbers.Length)]).Take(6).ToArray());
                string RandmNumeric = new string(randomNumeric.ToCharArray().OrderBy(s => (objRand.Next(2) % 2) == 0).ToArray());
                return RandmNumeric;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public static string GetRandomAlphaNumeric()
        {
            try
            {
                System.Random objRand = new System.Random();
                var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
                var numbers = "0123456789";
                string randomString = new string(chars.Select(c => chars[objRand.Next(chars.Length)]).Take(6).ToArray()) + new string(numbers.Select(c => numbers[objRand.Next(numbers.Length)]).Take(2).ToArray());
                string strAlphaNumeric = new string(randomString.ToCharArray().OrderBy(s => (objRand.Next(2) % 2) == 0).ToArray());
                return strAlphaNumeric;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public static string GetRandomNumber()
        {
            try
            {
                System.Random objRand = new System.Random();
                string strRandNumber = objRand.Next(1, 999999).ToString("D6");
                return strRandNumber;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
