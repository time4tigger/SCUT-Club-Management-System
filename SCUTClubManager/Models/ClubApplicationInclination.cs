﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace SCUTClubManager.Models
{
    public class ClubApplicationInclination
    {
        public int Id { get; set; }

        [Required]
        public int ApplicationId { get; set; }

        [Required]
        public int BranchId { get; set; }
        public int Order { get; set; }

        [MaxLength(1)]
        public string Status { get; set; }

        public virtual ClubBranch Branch { get; set; }
        public virtual ClubApplication Application { get; set; }
    }
}