using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class GooglePlaceTypes
    {
        [Key]
        public int Id { get; set; }
        public string PlaceName { get; set; }
    }
}
