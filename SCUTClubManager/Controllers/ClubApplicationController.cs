﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Linq.Expressions;
using SCUTClubManager.Models;
using SCUTClubManager.DAL;
using SCUTClubManager.BusinessLogic;

namespace SCUTClubManager.Controllers
{
    public class ClubApplicationController : Controller
    {
        UnitOfWork db = new UnitOfWork();

        //
        // GET: /ClubApplication/

        public ActionResult Index()
        {
            return View();
        }

        [Authorize]
        public ActionResult List(int club_id = 0, int page_number = 1, string order = "Date", string pass_filter = "", string search = "", 
            string search_option = "ClubName", string type_filter = "")
        {
            if (!User.IsInRole("社联") && !ScmRoleProvider.HasMembershipIn(club_id))
            {
                // TODO: 将用户重定向到另一个页面。
                return RedirectToAction("Index", "Home");
            }

            IQueryable<Application> applications = 
                db.Applications.Include(s => s.Club).Include(s => s.Club.MajorInfo).Include(s => s.Applicant).ToList() as IQueryable<Application>;

            // 下拉框
            List<KeyValuePair<string, string>> select_list = new List<KeyValuePair<string, string>>();
            select_list.Add(new KeyValuePair<string, string>("社团名称", "ClubName"));
            select_list.Add(new KeyValuePair<string, string>("申请人名", "ApplicantName"));

            ViewBag.SearchOptions = new SelectList(select_list, "Value", "Key", search_option);
            
            ViewBag.ClubId = club_id;
            ViewBag.CurrentOrder = order;
            ViewBag.DateOrderOpt = order == "Date" ? "DateDesc" : "Date";
            ViewBag.ApplicantNameOrderOpt = order == "Applicant.Name" ? "Applicant.NameDesc" : "Applicant.Name";
            ViewBag.PassOrderOpt = order == "Status" ? "StatusDesc" : "Status";
            ViewBag.Search = search;
            ViewBag.PassFilter = pass_filter;
            ViewBag.SearchOption = search_option;
            ViewBag.TypeFilter = type_filter;

            Expression<Func<Application, bool>> filter = null;
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
                }
            }

            applications = QueryProcessor.FilterApplication(applications, pass_filter, type_filter, club_id);

            var club_list = QueryProcessor.Query<Application>(applications, filter: filter,
                order_by: order, page_number: page_number, items_per_page: 20);          

            return View(club_list);
        }

        //
        // GET: /ClubApplication/Details/5

        [Authorize]
        public ActionResult Details(int id, int club_id = 0, int page_number = 1, string order = "Date", string pass_filter = "", string search = "",
            string search_option = "", string type_filter = "")
        {
            Application application = db.Applications.Include(s => s.Applicant).Include(s => s.Club).Find(id);

            ViewBag.ClubId = club_id;
            ViewBag.CurrentOrder = order;
            ViewBag.PageNumber = page_number;
            ViewBag.Search = search;
            ViewBag.PassFilter = pass_filter;
            ViewBag.SearchOption = search_option;
            ViewBag.TypeFilter = type_filter;

            if (application != null)
            {
                if (User.IsInRole("社联") || (application is ClubRegisterApplication &&
                    (application as ClubRegisterApplication).Applicants.Any(s => s.ApplicantUserName == User.Identity.Name)) ||
                    ScmRoleProvider.HasMembershipIn(application.ClubId.Value))
                {
                    if (application is ClubRegisterApplication)
                    {
                        return View("RegisterApplicationDetails", application as ClubRegisterApplication);
                    }
                    else if (application is ClubUnregisterApplication)
                    {
                        return View("UnregisterApplicationDetails", application as ClubUnregisterApplication);
                    }
                    else if (application is ClubInfoModificationApplication)
                    {
                        return View("InfoModificationApplicationDetails", application as ClubInfoModificationApplication);
                    }
                }
                else
                {
                    return View("PermissionDeniedError");
                }
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [Authorize(Roles = "社联")]
        public ActionResult Verify(int id, bool is_passed)
        {
            Application application = db.Applications.Find(id);

            if (application != null && application.Status == "n")
            {
                // 更改申请状态
                application.Status = is_passed ? "p" : "f";

                // 使申请生效
                if (is_passed)
                {
                    if (application is ClubRegisterApplication)
                    {
                        ClubRole president = null;
                        ClubRole member = null;

                        foreach (var role in db.ClubRoles.ToList())
                        {
                            if (role.Name == "会长")
                            {
                                president = role;
                            }
                            else if (role.Name == "会员")
                            {
                                member = role;
                            }
                        }

                        ClubRegisterApplication register_application = application as ClubRegisterApplication;
                        int applicant_count = register_application.Applicants.Count();

                        ClubBranch member_branch = new ClubBranch
                        {
                            BranchName = "会员部",
                            MemberCount = applicant_count,
                            NewMemberCount = applicant_count,
                            Members = new List<ClubMember>()
                        };

                        ClubMajorInfo major_info = new ClubMajorInfo
                        {
                            Name = register_application.MajorInfo.Name,
                            Instructor = register_application.MajorInfo.Instructor
                        };

                        ClubSubInfo sub_info = new ClubSubInfo
                        {
                            Principle = register_application.SubInfo.Principle,
                            Purpose = register_application.SubInfo.Purpose,
                            Range = register_application.SubInfo.Range,
                            Address = register_application.SubInfo.Address,
                            PosterUrl = register_application.SubInfo.PosterUrl,
                            Regulation = register_application.SubInfo.Regulation
                        };

                        Club registering_club = new Club
                        {
                            Level = 1,
                            Fund = 0,
                            FoundDate = DateTime.Now,
                            MemberCount = applicant_count,
                            NewMemberCount = applicant_count,
                            MajorInfo = major_info,
                            SubInfo = sub_info,
                            Branches = new List<ClubBranch>
                        {
                            member_branch
                        },
                            Applications = new List<Application>
                        {
                            register_application
                        }
                        };

                        if (register_application.Applicants != null)
                        {
                            foreach (var applicant in register_application.Applicants)
                            {
                                member_branch.Members.Add(new ClubMember
                                {
                                    JoinDate = DateTime.Now,
                                    Student = applicant.Applicant,
                                    Club = registering_club,
                                    ClubRole = applicant.IsMainApplicant ? president : member
                                });
                            }
                        }
                        else
                        {
                            // 不可能出现没有申请人的社团申请
                            throw new ArgumentException("No applicant in this club register application.");
                        }

                        if (register_application.Branches != null)
                        {
                            foreach (var created_branch in register_application.Branches)
                            {
                                registering_club.Branches.Add(new ClubBranch
                                {
                                    BranchName = created_branch.BranchName,
                                    MemberCount = 0,
                                    NewMemberCount = 0
                                });
                            }
                        }

                        db.Clubs.Add(registering_club);
                    }
                    else if (application is ClubInfoModificationApplication)
                    {
                        ClubInfoModificationApplication info_modification_application = application as ClubInfoModificationApplication;
                        Club modifying_club = info_modification_application.Club;

                        if (modifying_club != null)
                        {
                            if (info_modification_application.MajorInfo != null)
                            {
                                modifying_club.MajorInfo.Name = info_modification_application.MajorInfo.Name;
                                modifying_club.MajorInfo.Instructor = info_modification_application.MajorInfo.Instructor;
                            }

                            if (info_modification_application.SubInfo != null)
                            {
                                modifying_club.SubInfo.Address = info_modification_application.SubInfo.Address;
                                modifying_club.SubInfo.PosterUrl = info_modification_application.SubInfo.PosterUrl;
                                modifying_club.SubInfo.Principle = info_modification_application.SubInfo.Principle;
                                modifying_club.SubInfo.Purpose = info_modification_application.SubInfo.Purpose;
                                modifying_club.SubInfo.Range = info_modification_application.SubInfo.Range;
                                modifying_club.SubInfo.Regulation = info_modification_application.SubInfo.Regulation;
                            }

                            if (info_modification_application.ModificationBranches != null)
                            {
                                foreach (var branch_modification in info_modification_application.ModificationBranches)
                                {
                                    if (branch_modification is BranchCreation)
                                    {
                                        modifying_club.Branches.Add(new ClubBranch
                                        {
                                            BranchName = branch_modification.BranchName,
                                            MemberCount = 0,
                                            NewMemberCount = 0
                                        });
                                    }
                                    else if (branch_modification is BranchDeletion)
                                    {
                                        var orig_branch = branch_modification.OrigBranch;
                                        ClubBranch receiving_branch = null;

                                        // 优先选用会员部作为收容部门，若会员部不存在则使用第一个找到的部门。
                                        receiving_branch = modifying_club.Branches.First(s => s.BranchName == "会员部");
                                        if (receiving_branch == null)
                                        {
                                            receiving_branch = modifying_club.Branches.First();
                                        }

                                        // 还是没有部门？没救了
                                        if (receiving_branch == null)
                                        {
                                            throw new NullReferenceException("Damn! There is no branch in this stupid club!!!");
                                        }

                                        foreach (var branch_member in branch_modification.OrigBranch.Members)
                                        {
                                            receiving_branch.Members.Add(branch_member);
                                        }

                                        receiving_branch.MemberCount += orig_branch.MemberCount;
                                        receiving_branch.NewMemberCount += orig_branch.NewMemberCount;

                                        modifying_club.Branches.Remove(branch_modification.OrigBranch);
                                        db.ClubBranches.Delete(branch_modification.OrigBranch);
                                    }
                                    else if (branch_modification is BranchUpdate)
                                    {
                                        branch_modification.OrigBranch.BranchName = branch_modification.BranchName;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // 修改一个不存在的社团。。。
                            throw new ArgumentException("The club being modified does not exist.");
                        }
                    }
                    else if (application is ClubUnregisterApplication)
                    {
                        ClubUnregisterApplication unregister_application = application as ClubUnregisterApplication;

                        db.Clubs.Delete(unregister_application.Club);
                    }
                    else
                    {
                        // 只有3种社团事务申请。
                        throw new ArgumentException("Invalid club transaction application!");
                    }
                }

                db.SaveChanges();

                return RedirectToAction("List");
            }

            return View("InvalidOperationError");
        }

        //
        // GET: /ClubApplication/Create

        public ActionResult ApplyNewClub()
        {
            return View();
        } 

        //
        // POST: /ClubApplication/Create

        [HttpPost]
        public ActionResult ApplyNewClub(FormCollection collection)
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
        // GET: /ClubApplication/Edit/5
 
        public ActionResult Edit(int id)
        {
            return View();
        }

        //
        // POST: /ClubApplication/Edit/5

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
        // GET: /ClubApplication/Delete/5
 
        public ActionResult Delete(int id)
        {
            return View();
        }

        //
        // POST: /ClubApplication/Delete/5

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
