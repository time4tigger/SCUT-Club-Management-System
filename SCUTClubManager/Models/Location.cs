﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SCUTClubManager.Models
{
    public class Location
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public IEnumerable<LocationAvailableTime> AvailableTimes { get; set; }
    }
}