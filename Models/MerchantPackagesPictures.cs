using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class MerchantPackagesPictures
    {
        [Key]
        public int Id { get; set; }
        public int PackageId { get; set; }
        public string ImageName { get; set; }
        public DateTime AddedDate { get; set; }
    }
}
