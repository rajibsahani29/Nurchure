﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class SubscriptionType
    {
        [Key]
        public int Id { get; set; }
        public string Description { get; set; }
        public double Cost { get; set; }
        public int SubscriptionPeriod { get; set; }
    }
}
