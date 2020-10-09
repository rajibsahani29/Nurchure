using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class UserPersonalityQuestionaireDetails
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public int PersonalityQuestonId { get; set; }
        public string PersonalityQAnswer { get; set; }
        public string MBTIType { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
