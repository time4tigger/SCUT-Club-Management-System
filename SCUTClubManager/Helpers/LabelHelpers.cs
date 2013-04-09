using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SCUTClubManager.Models;

namespace SCUTClubManager.Helpers
{
    public static class LabelHelpers
    {
        public static string GetStatus(DateTime openTime, DateTime closeTime)
        {
            if (openTime.CompareTo(DateTime.Now) > 0)
            {
                return "暂未开始";
            }
            else if (openTime.CompareTo(DateTime.Now) <= 0 &&
                closeTime.CompareTo(DateTime.Now) >= 0)
            {
                return "进行中";
            }
            else if (closeTime.CompareTo(DateTime.Now) < 0)
            {
                return "已结束";
            }

            return "";
        }

        public static string GetStatus(string status_code)
        {
            switch (status_code)
            {
                case "n":
                    return "未审批";
                case "p":
                    return "通过";
                case "f":
                    return "拒绝";
                default:
                    return "未知";
            }
        }

        public static HtmlString GetComparedText(object orig_obj, object new_obj)
        {
            string result = "";

            if (!orig_obj.GetType().Equals(new_obj.GetType()))
            {
                throw new ArgumentException();
            }

            if (orig_obj.Equals(new_obj))
            {
                result += "<div class=\"NoChangeComparedText\">" + orig_obj.ToString() + "</div>";
            }
            else
            {
                result += "<div class=\"ChangedComparedText\">" + new_obj.ToString() + 
                    "</div><div class=\"ChangedComparedText\">(原为" + orig_obj.ToString() + ")</div>";
            }

            return new HtmlString(result);
        }
    }
}