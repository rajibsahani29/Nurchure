using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class Countries
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string CountryShortCode { get; set; }
    }
}
