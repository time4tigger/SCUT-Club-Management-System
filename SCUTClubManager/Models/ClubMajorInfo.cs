﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace SCUTClubManager.Models
{
    public class ClubMajorInfo
    {
        public int Id { get; set; }

        [MaxLength(20)]
        [Required(ErrorMessage = "请输入社团名称")]
        [Display(Name = "社团名称")]
        public string Name { get; set; }

        [Display(Name = "指导教师")]
        [Required(ErrorMessage = "请输入指导教师")]
        public string Instructor { get; set; }
    }
}