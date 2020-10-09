using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class UserLoginSessionKeys
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public Guid ApiKeys { get; set; }
        public DateTime CreatedDate { get; set; }

        public static string GetUserIdByApiKey(AppDbContext objDbContext, Guid api_key) {
            try
            {
                var objUserList = objDbContext.UserLoginSessionKeys
                                .Where(s => s.ApiKeys == api_key)
                                .FirstOrDefault();
                if (objUserList == null)
                {
                    return "";
                }
                else {
                    return Convert.ToString(objUserList.UserId);
                }
            }
            catch (Exception ex)
            {
                return Convert.ToString(ex.Message);
            }
        }
    }
}
