﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace SCUTClubManager.Models
{
    public class Location
    {
        public int Id { get; set; }

        [RegularExpression(@"^[a-z0-9A-Z]{1,20}$", ErrorMessage = "只能是数字和字母的组合，长度不能超过20个字符")]
        [MaxLength(20)]
        public string Name { get; set; }

        public virtual ICollection<LocationAvailableTime> AvailableTimes { get; set; }
        public virtual ICollection<LocationApplication> Applications { get; set; }
        public virtual ICollection<LocationAssignment> Assignments { get; set; }
    }
}