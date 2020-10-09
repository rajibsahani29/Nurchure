using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class HelperClasses
    {

    }
    public class PersonalityMatchResult
    {
        public string UserId { get; set; }
        public string PrefferedMBTIMatch { get; set; }
        public double GeoLocationLatitude { get; set; }
        public double GeoLocationLongitude { get; set; }
        public int Age { get; set; }
        public DateTime CreatedDate { get; set; }
        public double Distance { get; set; }
    }
}
