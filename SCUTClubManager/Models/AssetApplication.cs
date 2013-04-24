﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace SCUTClubManager.Models
{
    public class AssetApplication : Application
    {
        public int? SubEventId { get; set; }

        public virtual ICollection<Time> Times { get; set; }
        public virtual SubEvent SubEvent { get; set; }
        public virtual ICollection<ApplicatedAsset> ApplicatedAssets { get; set; }
    }
}