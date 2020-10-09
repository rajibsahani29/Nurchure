using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class UserPersonalitySummary
    {
        [Key]
        public string UserId { get; set; }
        public string PrefferedMBTIMatch { get; set; }
        public int E_RawPoint { get; set; }
        public int I_RawPoint { get; set; }
        public int S_RawPoint { get; set; }
        public int N_RawPoint { get; set; }
        public int T_RawPoint { get; set; }
        public int F_RawPoint { get; set; }
        public int J_RawPoint { get; set; }
        public int P_RawPoint { get; set; }
        public string EI_Preference { get; set; }
        public int EI_ClarityId { get; set; }
        public string SN_Preference { get; set; }
        public int SN_ClarityId { get; set; }
        public string TF_Preference { get; set; }
        public int TF_ClarityId { get; set; }
        public string JP_Preference { get; set; }
        public int JP_ClarityId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
