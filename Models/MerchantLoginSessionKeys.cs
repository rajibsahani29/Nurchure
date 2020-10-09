using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class MerchantLoginSessionKeys
    {
        [Key]
        public int Id { get; set; }
        public string MerchantId { get; set; }
        public Guid ApiKeys { get; set; }
        public DateTime CreatedDate { get; set; }

        public static string GetMerchantIdByApiKey(AppDbContext objDbContext, Guid api_key)
        {
            try
            {
                var objMerchantList = objDbContext.MerchantLoginSessionKeys
                                .Where(s => s.ApiKeys == api_key)
                                .FirstOrDefault();
                if (objMerchantList == null)
                {
                    return "";
                }
                else
                {
                    return Convert.ToString(objMerchantList.MerchantId);
                }
            }
            catch (Exception ex)
            {
                return Convert.ToString(ex.Message);
            }
        }
    }
}
