using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class Media
    {
        [Key]
        public int Id { get; set; }
        public string RefId { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string RefFlag { get; set; }
        public DateTime AddedDate { get; set; }
    }
}
