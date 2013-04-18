﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Linq.Expressions;
using SCUTClubManager.Models;
using SCUTClubManager.DAL;
using SCUTClubManager.BusinessLogic;
using SCUTClubManager.Helpers;
using SCUTClubManager.Models.View_Models;
using PagedList;

namespace SCUTClubManager.Controllers
{
    public class ClubMemberApplicationController : Controller
    {
        UnitOfWork db = new UnitOfWork();

        //
        // GET: /ClubMemberApplication/

        public ActionResult Index()
        {
            return View();
        }

        /**
         *  @param club_id 此参数为0时表示查找与当前用户相关的所有加入社团申请，其余则表示查找与给定社团相关的加入申请。
         *  @param branch_filter 此参数为0时表示查找给定社团全部部门的加入申请，-1表示查找待调剂的申请，其余表示查找与给定社团给定部门相关的加入申请。注意，当club_id为0时该参数无效。
         *  @param role_filter 此参数为0时表示查找不限角色加入给定社团的申请，其余表示以指定角色加入给定社团的加入申请。注意，当club_id为0时该参数无效。
         *  @param page_number 这个大家都知道就不说了。
         *  @param order 用途就不说了……只解释下用法。假设值为***，那么返回的Entity集合就会以Entity.***排序，若在最后加上Desc或者Descending的话则会变成倒序。
         *  排序属性可存在于导航属性中，写法为实体点导航属性点叉叉叉。
         *  @param pass_filter 以审批状态来过滤集合元素，具体参见QueryProcessor.FilterApplication。当club_id为0时才有效。
         *  @param search 作用同样不说，""时表示不搜索。
         *  @param search_option 一句话，按***搜索。
         */
        public ActionResult List(int club_id = Application.ALL, int branch_filter = Application.ALL, int role_filter = Application.ALL, int page_number = 1,
            string order = "Date", string pass_filter = "", string search = "", string search_option = "ClubName")
        {
            if (!User.IsInRole("社联") && !ScmRoleProvider.HasMembershipIn(club_id))
            {
                // TODO: 将用户重定向到另一个页面。
                return RedirectToAction("Index", "Home");
            }

            ClubMember membership = ScmRoleProvider.GetRoleInClub(club_id);
            IQueryable<ClubApplication> applications =
                db.ClubApplications.Include(t => t.Applicant).Include(t => t.Inclinations.Select(s => s.Branch)).ToList().Where(t => t.ClubId == club_id);
            Club club = db.Clubs.Include(t => t.Branches).Include(t => t.MajorInfo).Find(club_id);

            ViewBag.BranchName = "";
            ViewBag.ClubName = club.MajorInfo.Name;
            ViewBag.RoleName = "";

            // 搜索选项下拉框
            List<KeyValuePair<string, string>> search_option_list = new List<KeyValuePair<string, string>>();
            search_option_list.Add(new KeyValuePair<string, string>("姓名", "Name"));
            search_option_list.Add(new KeyValuePair<string, string>("用户名", "UserName"));
            ViewBag.SearchOptions = new SelectList(search_option_list, "Value", "Key", search_option);

            // 部门选项下拉框
            List<KeyValuePair<string, int>> branch_list = new List<KeyValuePair<string, int>>();
            foreach (var branch in club.Branches)
            {
                branch_list.Add(new KeyValuePair<string, int>(branch.BranchName, branch.Id));

                if (branch.Id == branch_filter)
                {
                    ViewBag.BranchName = branch.BranchName;
                }
            }
            if (membership != null)
            {
                branch_list.Add(new KeyValuePair<string, int>("本部门", membership.BranchId));
            }
            branch_list.Add(new KeyValuePair<string, int>("全部", Application.ALL));
            branch_list.Add(new KeyValuePair<string,int>("待调剂", Application.FLEXIBLE));
            if (branch_filter == Application.FLEXIBLE)
            {
                ViewBag.BranchName = "待调剂";
            }
            ViewBag.BranchFilters = new SelectList(branch_list, "Value", "Key", branch_filter);

            // 角色选项下拉框
            List<KeyValuePair<string, int>> role_list = new List<KeyValuePair<string, int>>();
            int member_role = ScmRoleProvider.GetRoleIdByName("会员");
            int executor_role = ScmRoleProvider.GetRoleIdByName("干事");
            role_list.Add(new KeyValuePair<string, int>("会员", member_role));
            role_list.Add(new KeyValuePair<string, int>("干事", executor_role));
            if (role_filter == member_role)
            {
                ViewBag.RoleName = "会员";
            }
            else if (role_filter == executor_role)
            {
                ViewBag.RoleName = "干事";
            }
            role_list.Add(new KeyValuePair<string, int>("全部", Application.ALL));
            ViewBag.RoleFilters = new SelectList(role_list, "Value", "Key", role_filter);

            ViewBag.ClubId = club_id;
            ViewBag.CurrentOrder = order;
            ViewBag.DateOrderOpt = order == "Date" ? "DateDesc" : "Date";
            ViewBag.ApplicantNameOrderOpt = order == "Applicant.Name" ? "Applicant.NameDesc" : "Applicant.Name";
            ViewBag.ApplicantUserNameOrderOpt = order == "ApplicantUserName" ? "ApplicantUserNameDesc" : "ApplicantUserName";
            ViewBag.PassOrderOpt = order == "Status" ? "StatusDesc" : "Status";
            ViewBag.ClubNameOrderOpt = order == "Club.MajorInfo.Name" ? "Club.MajorInfo.NameDesc" : "Club.MajorInfo.Name";
            ViewBag.RoleOrderOpt = order == "Role.Name" ? "Role.NameDesc" : "Role.Name";
            ViewBag.Search = search;
            ViewBag.PassFilter = pass_filter;
            ViewBag.BranchFilter = branch_filter;
            ViewBag.SearchOption = search_option;
            ViewBag.RoleFilter = role_filter;

            Expression<Func<ClubApplication, bool>> filter = null;
            if (!String.IsNullOrWhiteSpace(search) && !String.IsNullOrWhiteSpace(search_option))
            {
                switch (search_option)
                {
                    case "ClubName":
                        filter = s => s.Club.MajorInfo.Name.Contains(search);
                        break;
                    case "ApplicantName":
                        filter = s => s.Applicant.Name.Contains(search);
                        break;
                    case "ApplicantUserName":
                        filter = s => s.ApplicantUserName.Contains(search);
                        break;
                }
            }

            var application_list = QueryProcessor.Query<ClubApplication>(applications, filter: filter,
                order_by: order, page_number: page_number, items_per_page: 20);

            // 由于Include这坑爹方法不支持OrderBy，只能用传说中的In-Memory Sorting了……
            foreach (var application in application_list)
            {
                application.Inclinations = application.Inclinations.OrderBy(t => t.Order).ToList();
            }

            if (club_id == Application.ALL) // 用户查看自己的社团战史
            {
                string my_user_name = User.Identity.Name;
                application_list = application_list.Where(t => t.ApplicantUserName == my_user_name) as IPagedList<ClubApplication>;
                application_list = QueryProcessor.FilterApplication(application_list, pass_filter) as IPagedList<ClubApplication>;
            }
            else // 社团*长们查看自己社团当前被战状况
            {
                // 社团*长们只能查看未被审批的加入申请
                application_list = QueryProcessor.FilterApplication(application_list, Application.NOT_VERIFIED) as IPagedList<ClubApplication>;

                if (branch_filter != Application.ALL)
                {
                    if (branch_filter == Application.FLEXIBLE)
                    {
                        // 查找所有待调剂（全部志愿被拒绝且可调剂）的申请
                        application_list = application_list.Where(t => t.Inclinations.All(s => s.Status == Application.FAILED)) as IPagedList<ClubApplication>;
                    }
                    else // 按部门过滤的时候*长们只能查看当前未审批的最优先志愿为给定部门的申请
                    {
                        // 找出所有未被审批志愿中包含给定部门的申请
                        application_list = application_list.Where(t => t.Inclinations.Count(s => s.Status == Application.NOT_VERIFIED
                            && s.BranchId == branch_filter) > 0) as IPagedList<ClubApplication>;
                        // 找出这些申请中最优先未审批志愿为给定部门的
                        application_list = application_list.Where(t => t.Inclinations.First(s => s.Status == Application.NOT_VERIFIED).BranchId == branch_filter)
                            as IPagedList<ClubApplication>;
                    }
                }

                // 找出所有申请角色为给定角色的申请
                if (role_filter != Application.ALL)
                {
                    application_list = application_list.Where(t => t.RoleId == role_filter) as IPagedList<ClubApplication>;
                }
            }

            return View(application_list);
        }

        //
        // GET: /ClubMemberApplication/Details/5

        public ActionResult Details(int id)
        {
            return View();
        }

        //
        // GET: /ClubMemberApplication/Create

        public ActionResult Create()
        {
            return View();
        } 

        //
        // POST: /ClubMemberApplication/Create

        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
        
        //
        // GET: /ClubMemberApplication/Edit/5
 
        public ActionResult Edit(int id)
        {
            return View();
        }

        //
        // POST: /ClubMemberApplication/Edit/5

        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here
 
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        //
        // GET: /ClubMemberApplication/Delete/5
 
        public ActionResult Delete(int id)
        {
            return View();
        }

        //
        // POST: /ClubMemberApplication/Delete/5

        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here
 
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}