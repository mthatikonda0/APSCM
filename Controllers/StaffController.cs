using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Web;
using System.Web.Mvc;
using AGE.CMS.Data.Models.Intake;
using AGE.CMS.Data.Models.Security;
using AGE.CMS.Data.Models.Staff;
using AGE.CMS.Data.Models.AdditionalResources;
using AGE.CMS.Data.Models.ENums;
using AGE.CMS.Web.Areas.CMS.Controllers;
using DOAFramework.Core.WebMVC.Filters;
using AGE.CMS.Data.Models.LookupModels;
using System.Net.Mail;
using AGE.CMS.Web.PersonProfileWCFService;
using AGE.CMS.Data.Models.Account;
using AGE.CMS.Data.APS.Tables;
using AGE.CMS.Data.Models.Email;
using AGE.CMS.Core.RoleServiceAgentSvcRef;
//using AGE.CMS.Web.APSCaseWCFService;

namespace AGE.CMS.Web.Areas.CMS.Controllers
{
    [Layout("_Layout")]
    public class StaffController : CMSController
    {


        RoleServiceAgentClient rsac = new RoleServiceAgentClient();



        public ActionResult Index()
        {
            viewStaffModel staff = new viewStaffModel();

            if (User.IsInRole("CMS_IDOAStaff"))
            {
                staff.viewStaffRegistry = CMSService.ListStaffRegistryItems();
                staff.viewraatracker.ListOfRAAActivity = CMSService.ListOfRAAActivity().ToList();
                //staff.viewstaffregistry.ListStaffRegistry = CMSService.ListOfStaffRegistry().ToList();
                staff.viewstaffdirectory = CMSService.ListOfStaffDirectoryByUserName();
                staff.viewstaffdirectory.Staff = staff.viewstaffdirectory.Staff.OrderBy(i => i.LastName).ToList();
                staff.viewTrainingTracker.ListTrainingTrackers = CMSService.ListOfTrainingTrackersByUserName(username).ToList();
            }
            else if (User.IsInRole("CMS_RAAAdmin"))
            {
                viewPSA psa = CMSService.GetPSAsByUser(username);
                var psacontractids = (from contract in psa.ListOfContracts select contract.Id).ToList();

                //Staff Reigistry
                staff.viewStaffRegistry.StaffList = CMSService.ListStaffRegistryItems().StaffList.Where(i => psacontractids.Any(s => i.AgencyIds.Contains(s)) || i.RAAIds.Contains(Convert.ToInt32(psa.PSAID))).ToList();
                staff.viewStaffRegistry.StaffList = staff.viewStaffRegistry.StaffList.Where(i => i.IsActive == true).ToList();

                staff.viewraatracker.ListOfRAAActivity = CMSService.ListOfRAAActivity().Where(i => psacontractids.Any(a => a == i.APSPAId)).ToList();
                staff.viewstaffdirectory.Staff = CMSService.ListOfStaffDirectoryByUserName().Staff.Where(i => psacontractids.Any(s => i.ContractIds.Contains(s)) || i.RAAIds.Contains(Convert.ToInt32(psa.PSAID))).ToList();
                staff.viewTrainingTracker.ListTrainingTrackers = CMSService.ListOfTrainingTrackersByUserName(username).Where(i => psacontractids.Any(a => a == i.UserContractId)).ToList();
            }
            else if (User.IsInRole("CMS_Supervisor"))
            {
                //var usercontracts = CMSService.ListOfContracts(username).ToList();
                var usercontracts = CMSService.GetContractsByUser(username);
                var contractids = (from contract in usercontracts select contract.ContractId).ToList();
                staff.viewstaffdirectory = CMSService.ListOfStaffDirectoryByUserName();
                staff.viewstaffdirectory.Staff = staff.viewstaffdirectory.Staff.Where(i => contractids.Any(s => i.ContractIds.Contains(s))).OrderBy(i => i.LastName).ToList();
                staff.viewTrainingTracker.ListTrainingTrackers = CMSService.ListOfTrainingTrackersByUserName(username).Where(i => contractids.Any(a => a == i.UserContractId)).ToList();
            }

            else if (User.IsInRole("CMS_Caseworker"))
            {
                var usercontracts = CMSService.ListOfContracts(username).ToList();
                var contractids = (from contract in usercontracts select contract.Id).ToList();
                staff.viewTrainingTracker.ListTrainingTrackers = CMSService.ListOfTrainingTrackersByUserName(username).Where(i => i.UserCreated == username).ToList();
            }

            staff.viewadditionalresources.ListAdditionalResourcesURLs = CMSService.ListOfAdditionalResourcesURLs().Where(s => (bool)s.IsWebinar).ToList();

            return View(staff);
        }

        #region Training Tracker

        public ActionResult ListTrainingTrackersByUserName()
        {
            viewTrainingTracker viewTrainingTracker = new viewTrainingTracker();


            //viewTrainingTracker.ListTrainingTrackers = CMSService.ListOfTrainingTrackersByUserName(username).Where(i => contractids.Any(a => a == i.UserContractId)).ToList(); 

            if (User.IsInRole("CMS_IDOAStaff"))
            {
                viewTrainingTracker.ListTrainingTrackers = CMSService.ListOfTrainingTrackersByUserName(username).OrderByDescending(v => v.Id).ToList();
            }
            else if (User.IsInRole("CMS_RAAAdmin"))
            {
                //var userpsa = new APS.Data.Entities.viewPSA();
                var userpsa = APSCaseService.ListOfPSAsByUserName(username).FirstOrDefault();

                //viewTrainingTracker.ListTrainingTrackers = CMSService.ListOfTrainingTrackersByUserName(username).Where(i => userpsa.ListOfContracts.Any(s => s.Id == i.UserContractId)).ToList();
                viewTrainingTracker.ListTrainingTrackers = CMSService.ListOfTrainingTrackersByUserName(username).Where(i => i.UserContractId == userpsa.Id).OrderByDescending(v => v.Id).ToList();
            }
            else if (User.IsInRole("CMS_Supervisor"))
            {
                viewTrainingTracker.ListTrainingTrackers = CMSService.ListOfTrainingTrackersByUserName(username).Where(i => contractids.Any(a => a == i.UserContractId)).OrderByDescending(v => v.Id).ToList();
            }
            else
            {
                viewTrainingTracker.ListTrainingTrackers = CMSService.ListOfTrainingTrackersByUserName(username).Where(i => contractids.Any(a => a == i.UserContractId) && i.UserCreated == username).OrderByDescending(v => v.Id).ToList();
            }


            return View(viewTrainingTracker);

        }

        public ActionResult ViewTrainingTracker(int Id)
        {
            viewTrainingTracker viewTrainingTracker;

            if (Id == 0)
            {
                viewTrainingTracker = new viewTrainingTracker();
            }

            else
            {
                viewTrainingTracker = CMSService.GetTrainingTracker(Id);

            }

            viewTrainingTracker.UserName = username;

            if (User.IsInRole("CMS_IDOAStaff"))
            {
                viewTrainingTracker.UserContractName = "IDoA Staff";
            }
            else if (User.IsInRole("CMS_RAAAdmin"))
            {
                var psa = CMSService.GetPSAByUserName(username);
                viewTrainingTracker.UserContractName = psa.AreaName;
                viewTrainingTracker.UserContractId = psa.Id;
            }
            else if (User.IsInRole("CMS_Supervisor"))
            {
                var ListContracts = CMSService.ListOfContracts(username).ToList();

                if (ListContracts.Count == 1)
                {
                    viewTrainingTracker.UserContractName = ListContracts.FirstOrDefault().ContractName;
                    viewTrainingTracker.UserContractId = ListContracts.FirstOrDefault().Id;
                }
            }

            else if (User.IsInRole("CMS_Caseworker"))
            {
                var ListContracts = CMSService.ListOfContracts(username).ToList();

                if (ListContracts.Count == 1)
                {
                    viewTrainingTracker.UserContractName = ListContracts.FirstOrDefault().ContractName;
                    viewTrainingTracker.UserContractId = ListContracts.FirstOrDefault().Id;

                }
            }



            //var listofcontracts = CMSService.ListOfContracts(username).ToList();
            //if (listofcontracts != null && listofcontracts.Any() && listofcontracts.Count == 1)
            //{
            //    viewTrainingTracker.UserContractId = listofcontracts.FirstOrDefault().Id;
            //    viewTrainingTracker.UserContractName = (from contract in listofcontracts where contract.Id == viewTrainingTracker.UserContractId select contract.ContractName).FirstOrDefault();
            //}

            viewTrainingTracker.ListTrainingTypes = CMSService.ListOfTrainingTypes().ToList();

            return View(viewTrainingTracker);
        }


        public ActionResult EditTrainingTracker(int Id)
        {
            viewTrainingTracker viewTrainingTracker;

            if (Id == 0)
            {
                viewTrainingTracker = new viewTrainingTracker();
            }

            else
            {
                viewTrainingTracker = CMSService.GetTrainingTracker(Id);

            }

            viewTrainingTracker.UserName = username;

            if (User.IsInRole("CMS_IDOAStaff"))
            {
                viewTrainingTracker.UserContractName = "IDoA Staff";
            }
            else if (User.IsInRole("CMS_RAAAdmin"))
            {
                var psa = CMSService.GetPSAByUserName(username);
                viewTrainingTracker.UserContractName = psa.AreaName;
                viewTrainingTracker.UserContractId = psa.Id;
            }
            else if (User.IsInRole("CMS_Supervisor"))
            {
                var ListContracts = CMSService.ListOfContracts(username).ToList();

                if (ListContracts.Count == 1)
                {
                    viewTrainingTracker.UserContractName = ListContracts.FirstOrDefault().ContractName;
                    viewTrainingTracker.UserContractId = ListContracts.FirstOrDefault().Id;
                }
            }

            else if (User.IsInRole("CMS_Caseworker"))
            {
                var ListContracts = CMSService.ListOfContracts(username).ToList();

                if (ListContracts.Count == 1)
                {
                    viewTrainingTracker.UserContractName = ListContracts.FirstOrDefault().ContractName;
                    viewTrainingTracker.UserContractId = ListContracts.FirstOrDefault().Id;

                }
            }



            //var listofcontracts = CMSService.ListOfContracts(username).ToList();
            //if (listofcontracts != null && listofcontracts.Any() && listofcontracts.Count == 1)
            //{
            //    viewTrainingTracker.UserContractId = listofcontracts.FirstOrDefault().Id;
            //    viewTrainingTracker.UserContractName = (from contract in listofcontracts where contract.Id == viewTrainingTracker.UserContractId select contract.ContractName).FirstOrDefault();
            //}

            viewTrainingTracker.ListTrainingTypes = CMSService.ListOfTrainingTypes().ToList();

            return View(viewTrainingTracker);
        }

        [HttpPost]
        // [MultipleButton(Name = "action", Argument = "SaveTrainingTracker")]
        [ValidateInput(false)]
        public JsonResult SaveTrainingTracker(viewTrainingTracker viewTrainingTracker)
        {
            viewTrainingTracker.UserCreated = username;
            viewTrainingTracker.UserUpdated = username;
            //if (viewTrainingTracker.StatusId == 0)
            //{
            //    viewTrainingTracker.StatusDescription = CaseStatus.Open.ToString();
            //}
            //else
            //{

            //}
            viewTrainingTracker.StatusDescription = CaseStatus.Open.ToString();
            int trackingId = CMSService.SaveTrainingTracker(viewTrainingTracker);

            return Json(trackingId, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        //  [MultipleButton(Name = "action", Argument = "SubmitTraining")]
        [ValidateInput(false)]
        public JsonResult SubmitTraining(viewTrainingTracker viewTrainingTracker)
        {
            viewTrainingTracker.UserCreated = username;
            viewTrainingTracker.UserUpdated = username;
            viewTrainingTracker.StatusDescription = CaseStatus.Submitted.ToString();
            int trackingId = CMSService.SaveTrainingTracker(viewTrainingTracker);


            EmailLists emails = CMSService.GetEmails();

            var agencyName = "";
            if (User.IsInRole("CMS_RAAAdmin"))
            {
                var userpsa = APSCaseService.ListOfPSAsByUserName(username).FirstOrDefault();
                agencyName = userpsa.AreaName;
            }
            else if (User.IsInRole("CMS_Supervisor") || User.IsInRole("CMS_Caseworker"))
            {
                agencyName = CMSService.GetContract(contractids.FirstOrDefault()).ContractName;
            }

            List<MailAddress> NewEMails = new List<MailAddress>();
            string MessageNew = "You have received a Training Tracker for Name: "+viewTrainingTracker.WorkerName+", Agency:<u>" + agencyName + "</u>";
            string SubjectNew = "Training Tracker";


            foreach(var e in emails.IDoAEmails)
            {
                NewEMails.Add(new MailAddress(e, "To"));
            }
            
                SendEmail(NewEMails, MessageNew, SubjectNew);
          
            return Json(trackingId, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        //  [MultipleButton(Name = "action", Argument = "SubmitTraining")]
        [ValidateInput(false)]
        public JsonResult ApproveTraining(viewTrainingTracker viewTrainingTracker)
        {
            viewTrainingTracker.UserCreated = username;
            viewTrainingTracker.UserUpdated = username;
            viewTrainingTracker.StatusDescription = CaseStatus.Approved.ToString();
            int trackingId = CMSService.SaveTrainingTracker(viewTrainingTracker);

            return Json(trackingId, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        //  [MultipleButton(Name = "action", Argument = "SubmitIDoATraining")]
        [ValidateInput(false)]
        public ActionResult SubmitIDoATraining(viewTrainingTracker viewTrainingTracker)
        {
            viewTrainingTracker.UserCreated = username;
            viewTrainingTracker.UserUpdated = username;
            viewTrainingTracker.StatusDescription = CaseStatus.Recommended.ToString();
            int trackingId = CMSService.SaveTrainingTracker(viewTrainingTracker);

            return Json(trackingId, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Staff Directory

        public ActionResult EditStaffDirectory(int Id)
        {

            //viewStaffDirectory viewstaffdirectory;
            //if (Id != 0)
            //{
            //    viewstaffdirectory = CMSService.GetStaffDirectory(Id);
            //    viewstaffdirectory.InCompleteErrors.ErrorsInStaffDirectory = false;
            //    //viewstaffdirectory.ContractId = CMSService.ListOfContracts(username).FirstOrDefault().Id;
            //    viewstaffdirectory.ContractDescription = (from contract in CMSService.ListOfAllContracts() where contract.Id == viewstaffdirectory.ContractId select contract.ContractName).FirstOrDefault();
            //}
            //else
            //{
            //    viewstaffdirectory = new viewStaffDirectory();
            //    viewstaffdirectory.InCompleteErrors.ErrorsInStaffDirectory = false;
            //    viewstaffdirectory.ContractId = CMSService.ListOfContracts(username).FirstOrDefault().Id;
            //    viewstaffdirectory.ContractDescription = (from contract in CMSService.ListOfContracts(username) where contract.Id == viewstaffdirectory.ContractId select contract.ContractName).FirstOrDefault();
            //}

            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    viewstaffdirectory.ListPSA = CMSService.ListOfPSA().ToList();
            //    viewstaffdirectory.ListAgencies = CMSService.ListOfAllContracts().ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    viewstaffdirectory.ListPSA = CMSService.ListOfPSA().Where(i => i.Id == PSAId).ToList();
            //    viewstaffdirectory.ListAgencies = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    viewstaffdirectory.ListAgencies = CMSService.ListOfContracts(username).ToList();
            //    viewstaffdirectory.ListPSA = CMSService.ListOfPSA().Where(i => viewstaffdirectory.ListAgencies.Any(a => a.PSAId == i.Id)).ToList();
            //}

            ViewBag.ddlAreaAgencies = GetAreaAgencies();
            ViewBag.ddlFullTimeEquivalents = GetFullTimeEquivalent();
            ViewBag.ddlEmployeeEducation = GetEmployeeEducation();
            ViewBag.ddlPositionTitles = GetPositionTitlesddl();

            //return View(setIncompleteStaffDirectoryErrors(viewstaffdirectory));


            viewStaff_Directory_New sd = new viewStaff_Directory_New();

            sd.InCompleteErrors.ErrorsInStaffDirectory = false;

            sd = CMSService.GetStaffDirectory(Id);
            sd.ListAgencies = CMSService.ListOfContracts(username).ToList();
            sd.ListPSA = CMSService.ListOfPSA().Where(i => sd.ListAgencies.Any(a => a.PSAId == i.Id)).ToList();
            

            return View(setIncompleteStaffDirectoryErrors(sd));



        }

        [HttpPost]
        [MultipleButton(Name = "action", Argument = "SaveStaffDirectory")]
        [ValidateInput(false)]
        public ActionResult SaveStaffDirectory(viewStaff_Directory_New viewstaffdirectory)
        {           

            if (!ModelState.IsValid)
            {
                return RedirectToAction("EditStaffDirectory", new { Id = viewstaffdirectory.Id });
            }

            else
            {
                viewstaffdirectory.StatusDescription = CaseStatus.Open.ToString();

                //if (viewstaffdirectory.User.UserPSA.Id == 0)
                //{
                //    viewstaffdirectory.StatusDescription = CaseStatus.Incomplete.ToString();
                //    ModelState.AddModelError("CustomError", "Please select Area Agency");
                //}

                if (viewstaffdirectory.User.FirstName == null)
                {
                    viewstaffdirectory.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "FirstName is required");
                }

                if (viewstaffdirectory.User.LastName == null)
                {
                    viewstaffdirectory.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "LastName is required");
                }
                //if (viewstaffdirectory.UserName == null)
                //{
                //    viewstaffdirectory.StatusDescription = CaseStatus.Incomplete.ToString();
                //    ModelState.AddModelError("CustomError", "UserName is required");
                //}

                //if (viewstaffdirectory.SSN == null)
                //{
                //    viewstaffdirectory.StatusDescription = CaseStatus.Incomplete.ToString();
                //    ModelState.AddModelError("CustomError", "SSN is required");
                //}

                //if (viewstaffdirectory.RoleId == 0)
                //{
                //    viewstaffdirectory.StatusDescription = CaseStatus.Incomplete.ToString();
                //    ModelState.AddModelError("CustomError", "Please select Position Title");
                //}

                if (viewstaffdirectory.FullTimeEquivalentId == 0)
                {
                    viewstaffdirectory.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select Full Time Equivalent");
                }

                if (viewstaffdirectory.EmployeeEducationId == 0)
                {
                    viewstaffdirectory.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select Employee Education");
                }
                if (viewstaffdirectory.EmployeeHireDate == null)
                {
                    viewstaffdirectory.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Employee Hire Date is required");
                }

                //if (viewstaffdirectory.Id != 0)
                //{
                //    if (viewstaffdirectory.IsRegistryPlacement == true || viewstaffdirectory.IsRepalcementBadge == true)
                //    {
                //        viewstaffdirectory.StatusDescription = CaseStatus.Recommended.ToString();
                //    }

                //    //if (viewstaffdirectory.IsRepalcementBadge == true)
                //    //{
                //    //    viewstaffdirectory.StatusDescription = CaseStatus.Recommended.ToString();
                //    //}
                //}


                //if (viewstaffdirectory. == null)
                //{
                //    viewstaffdirectory.StatusDescription = CaseStatus.Incomplete.ToString();
                //    ModelState.AddModelError("CustomError", "SSN is required");
                //}

                viewstaffdirectory.UserCreated = username;
                viewstaffdirectory.UserUpdated = username;





                int staffdirectoryId = CMSService.SaveStaffDirectory(viewstaffdirectory);


                if(viewstaffdirectory.User.IsActive == false)
                {
                    viewUser user = CMSService.GetUser(viewstaffdirectory.User.UserId);

                    if (user != null)
                    {
                        var username = user.UserName;
                        var allroles = rsac.GetAllRoles(applicationname);

                        foreach (var role in allroles)
                        {
                            if (rsac.IsUserInRole(applicationname, username, role) == 1)
                            {
                                rsac.RemoveUsersFromRoles(applicationname, username, role);
                            }
                        }
                    }

                }
                //return RedirectToAction("ListTrainingTrackersByUserName", "Staff");
                return RedirectToAction("Index", "Staff");

            }

        }

        [HttpPost]
        [MultipleButton(Name = "action", Argument = "SubmitDirectory")]
        [ValidateInput(false)]
        public ActionResult SubmitStaffDirectory(viewStaff_Directory_New viewstaffdirectory)
        {
            //viewStaffDirectory staffdirectory = CMSService.GetStaffDirectory((int)viewstaffdirectory.Id);

            viewstaffdirectory.StatusDescription = CaseStatus.Submitted.ToString();
            int staffdirectoryId = CMSService.SubmitStaffDirectory(viewstaffdirectory);

            var agencyName = "";
            if (User.IsInRole("CMS_RAAAdmin"))
            {
                //var userpsa = APSCaseService.ListOfPSAsByUserName(username).FirstOrDefault();
                viewPSA userpsa = CMSService.GetPSAsByUser(username);
                agencyName = userpsa.PSAName;
            }
            else if (User.IsInRole("CMS_Supervisor"))
            {
                agencyName = CMSService.GetContract(contractids.FirstOrDefault()).ContractName;
            }

            if (viewstaffdirectory.User.FirstName == null)
            {
                viewstaffdirectory.User.FirstName = "";
            }
            if (viewstaffdirectory.User.LastName == null)
            {
                viewstaffdirectory.User.LastName = "";
            }

            //if (viewstaffdirectory.IsRegistryPlacement == true)
            //{
            //    List<MailAddress> NewEMails = new List<MailAddress>();
            //    string MessageNew = "You have received a Staff Directory related to  <u>" + agencyName + "</u> for " + viewstaffdirectory.FirstName + " " + viewstaffdirectory.LastName + " regarding request for placement on the caseworker registry";
            //    string SubjectNew = "Staff Directory";
            //    NewEMails.Add(new MailAddress(System.Configuration.ConfigurationManager.AppSettings["IDoAEmail"], "To"));
            //    SendEmail(NewEMails, MessageNew, SubjectNew);
            //}

            //if (viewstaffdirectory.IsReplacementBadge == true)
            //{
            //    List<MailAddress> NewEMails = new List<MailAddress>();
            //    string MessageNew = "You have received a Staff Directory related to  <u>" + agencyName + "</u> for " + viewstaffdirectory.FirstName + " " + viewstaffdirectory.LastName + " ) regarding request for replacement badge";
            //    string SubjectNew = "Staff Directory";
            //    NewEMails.Add(new MailAddress(System.Configuration.ConfigurationManager.AppSettings["IDoAEmail"], "To"));
            //    SendEmail(NewEMails, MessageNew, SubjectNew);
            //}

            //if (viewstaffdirectory.SpecifyDegree != null)
            //{
            //    List<MailAddress> NewEMails = new List<MailAddress>();
            //    string MessageNew = "You have received a Staff Directory related to  <u>" + agencyName + "</u> for " + viewstaffdirectory.FirstName + " " + viewstaffdirectory.LastName + " regarding request for a qualification waiver.";
            //    string SubjectNew = "Staff Directory";
            //    NewEMails.Add(new MailAddress(System.Configuration.ConfigurationManager.AppSettings["IDoAEmail"], "To"));
            //    SendEmail(NewEMails, MessageNew, SubjectNew);
            //}


            if (viewstaffdirectory.User.IsActive == false)
            {
                viewUser user = CMSService.GetUser(viewstaffdirectory.User.UserId);

                if (user != null)
                {
                    var username = user.UserName;
                    var allroles = rsac.GetAllRoles(applicationname);

                    foreach (var role in allroles)
                    {
                        if (rsac.IsUserInRole(applicationname, username, role) == 1)
                        {
                            rsac.RemoveUsersFromRoles(applicationname, username, role);
                        }
                    }
                }

            }


            //return RedirectToAction("ListTrainingTrackersByUserName", "Staff");
            return RedirectToAction("Index", "Staff");
        }

        [HttpPost]
        [MultipleButton(Name = "action", Argument = "ApproveDirectory")]
        [ValidateInput(false)]
        public ActionResult ApproveStaffDirectory(viewStaff_Directory_New viewstaffdirectory)
        {
            //viewStaffDirectory staffdirectory = CMSService.GetStaffDirectory((int)viewstaffdirectory.Id);
            viewstaffdirectory.StatusDescription = CaseStatus.Approved.ToString();

            int staffdirectoryId = CMSService.ApproveStaffDirectory(viewstaffdirectory);

            //return RedirectToAction("ListTrainingTrackersByUserName", "Staff");
            return RedirectToAction("Index", "Staff");
        }

        public ActionResult ViewStaffDirectory(int Id)
        {

            // viewStaffDirectory viewstaffdirectory;
            //if (Id != 0)
            //{
            //    viewstaffdirectory = CMSService.GetStaffDirectory(Id);
            //    viewstaffdirectory.InCompleteErrors.ErrorsInStaffDirectory = false;
            //    //viewstaffdirectory.ContractId = CMSService.ListOfContracts(username).FirstOrDefault().Id;
            //    viewstaffdirectory.ContractDescription = (from contract in CMSService.ListOfAllContracts() where contract.Id == viewstaffdirectory.ContractId select contract.ContractName).FirstOrDefault();
            //}
            //else
            //{
            //    viewstaffdirectory = new viewStaffDirectory();
            //    viewstaffdirectory.InCompleteErrors.ErrorsInStaffDirectory = false;
            //    viewstaffdirectory.ContractId = CMSService.ListOfContracts(username).FirstOrDefault().Id;
            //    viewstaffdirectory.ContractDescription = (from contract in CMSService.ListOfContracts(username) where contract.Id == viewstaffdirectory.ContractId select contract.ContractName).FirstOrDefault();
            //}

            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    viewstaffdirectory.ListPSA = CMSService.ListOfPSA().ToList();
            //    viewstaffdirectory.ListAgencies = CMSService.ListOfAllContracts().ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    viewstaffdirectory.ListPSA = CMSService.ListOfPSA().Where(i => i.Id == PSAId).ToList();
            //    viewstaffdirectory.ListAgencies = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    viewstaffdirectory.ListAgencies = CMSService.ListOfContracts(username).ToList();
            //    viewstaffdirectory.ListPSA = CMSService.ListOfPSA().Where(i => viewstaffdirectory.ListAgencies.Any(a => a.PSAId == i.Id)).ToList();
            //}

            //ViewBag.ddlAreaAgencies = GetAreaAgencies();
            //ViewBag.ddlFullTimeEquivalents = GetFullTimeEquivalent();
            //ViewBag.ddlEmployeeEducation = GetEmployeeEducation();
            //ViewBag.ddlPositionTitles = GetPositionTitlesddl();



            viewStaff_Directory_New viewstaffdirectory;
            if (Id != 0)
            {
                viewstaffdirectory = CMSService.GetStaffDirectory(Id);
                viewstaffdirectory.InCompleteErrors.ErrorsInStaffDirectory = false;
                //viewstaffdirectory.ContractId = CMSService.ListOfContracts(username).FirstOrDefault().Id;
               // viewstaffdirectory.ContractDescription = (from contract in CMSService.ListOfAllContracts() where contract.Id == viewstaffdirectory.ContractId select contract.ContractName).FirstOrDefault();
            }
            else
            {
                viewstaffdirectory = new viewStaff_Directory_New();
                viewstaffdirectory.InCompleteErrors.ErrorsInStaffDirectory = false;
                //viewstaffdirectory.ContractId = CMSService.ListOfContracts(username).FirstOrDefault().Id;
               // viewstaffdirectory.ContractDescription = (from contract in CMSService.ListOfContracts(username) where contract.Id == viewstaffdirectory.ContractId select contract.ContractName).FirstOrDefault();
            }

            if (User.IsInRole("CMS_IDOAStaff"))
            {
                viewstaffdirectory.ListPSA = CMSService.ListOfPSA().ToList();
                viewstaffdirectory.ListAgencies = CMSService.ListOfAllContracts().ToList();
            }
            else if (User.IsInRole("CMS_RAAAdmin"))
            {
                //var PSAId = CMSService.GetPSAByUserName(username).Id;
                viewPSA psa = CMSService.GetPSAsByUser(username);
                viewstaffdirectory.ListPSA = CMSService.ListOfPSA().Where(i => i.Id == psa.PSAID).ToList();
                viewstaffdirectory.ListAgencies = CMSService.ListOfAllContracts().Where(i => i.PSAId == psa.PSAID).ToList();
            }
            else if (User.IsInRole("CMS_Supervisor"))
            {
                viewstaffdirectory.ListAgencies = CMSService.ListOfContracts(username).ToList();
                viewstaffdirectory.ListPSA = CMSService.ListOfPSA().Where(i => viewstaffdirectory.ListAgencies.Any(a => a.PSAId == i.Id)).ToList();
            }

            ViewBag.ddlAreaAgencies = GetAreaAgencies();
            ViewBag.ddlFullTimeEquivalents = GetFullTimeEquivalent();
            ViewBag.ddlEmployeeEducation = GetEmployeeEducation();
            ViewBag.ddlPositionTitles = GetPositionTitlesddl();

            return View(viewstaffdirectory);
        }


        public ActionResult NewStaffDirectory(int UserId)
        {
            int sId = CMSService.NewStaffDirectory(UserId);

            return RedirectToAction("EditStaffDirectory", "Staff", new { @Id = sId });
        }

        protected viewStaff_Directory_New setIncompleteStaffDirectoryErrors(viewStaff_Directory_New viewstaffdirectory)
        {
            if (viewstaffdirectory.Id > 0)
            {

                if (viewstaffdirectory.User.UserPSA.Id == 0)
                {
                    viewstaffdirectory.InCompleteErrors.ErrorsInStaffDirectory = true;
                    viewstaffdirectory.InCompleteErrors.HasErrorsAreaAgency = true;
                }
                if (viewstaffdirectory.User.FirstName == null)
                {
                    viewstaffdirectory.InCompleteErrors.ErrorsInStaffDirectory = true;
                    viewstaffdirectory.InCompleteErrors.HasErrorsFirstName = true;
                }
                if (viewstaffdirectory.User.LastName == null)
                {
                    viewstaffdirectory.InCompleteErrors.ErrorsInStaffDirectory = true;
                    viewstaffdirectory.InCompleteErrors.HasErrorsLastName = true;
                }
                if (viewstaffdirectory.User.UserName == null)
                {
                    viewstaffdirectory.InCompleteErrors.ErrorsInStaffDirectory = true;
                    viewstaffdirectory.InCompleteErrors.HasErrorsUserName = true;
                }
                if (viewstaffdirectory.SSN == null)
                {
                    viewstaffdirectory.InCompleteErrors.ErrorsInStaffDirectory = true;
                    viewstaffdirectory.InCompleteErrors.HasErrorsSSN = true;
                }
                //if (viewstaffdirectory.RoleId == 0)
                //{
                //    viewstaffdirectory.InCompleteErrors.ErrorsInStaffDirectory = true;
                //    viewstaffdirectory.InCompleteErrors.HasErrorsRoleId = true;
                //}

                if (viewstaffdirectory.FullTimeEquivalentId == 0)
                {
                    viewstaffdirectory.InCompleteErrors.ErrorsInStaffDirectory = true;
                    viewstaffdirectory.InCompleteErrors.HasErrorsFullTimeEquivalent = true;
                }

                if (viewstaffdirectory.EmployeeEducationId == 0)
                {
                    viewstaffdirectory.InCompleteErrors.ErrorsInStaffDirectory = true;
                    viewstaffdirectory.InCompleteErrors.HasErrorsEmployeeEducation = true;
                }
                if (viewstaffdirectory.EmployeeHireDate == null || viewstaffdirectory.EmployeeHireDate == Convert.ToDateTime("1/1/0001"))
                {
                    viewstaffdirectory.InCompleteErrors.ErrorsInStaffDirectory = true;
                    viewstaffdirectory.InCompleteErrors.HasErrorsEmployeeHireDate = true;
                }

            }

            return viewstaffdirectory;
        }

        public ActionResult ListStaffDirectory()
        {
            //viewStaffDirectory viewstaffdirectory = new viewStaffDirectory();

            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    viewstaffdirectory.ListStaffDirectory = CMSService.ListOfStaffDirectoryByUserName().OrderByDescending(v => v.Id).ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    //var userpsa = new APS.Data.Entities.viewPSA();
            //    var userpsa = APSCaseService.ListOfPSAsByUserName(username).FirstOrDefault();

            //    viewstaffdirectory.ListStaffDirectory = CMSService.ListOfStaffDirectoryByUserName().Where(i => userpsa.ListOfContracts.Any(s => s.Id == i.ContractId)).OrderByDescending(v => v.Id).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    viewstaffdirectory.ListStaffDirectory = CMSService.ListOfStaffDirectoryByUserName().Where(i => i.UserCreated == username).OrderByDescending(v => v.Id).ToList();
            //}

            //return View(viewstaffdirectory);



            //if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var userpsa = APSCaseService.ListOfPSAsByUserName(username).FirstOrDefault();
            //    viewStaffList = CMSService.ListOfStaffDirectoryByUserName().Where(i => userpsa.ListOfContracts.Any(s => s.Id == i.ContractId)).OrderByDescending(v => v.Id).ToList();
            //}


            viewStaffDirectoryList viewStaffList = new viewStaffDirectoryList();
            viewStaffList = CMSService.ListOfStaffDirectoryByUserName();
            if (User.IsInRole("CMS_IDOAStaff"))
            {

            }
            else if (User.IsInRole("CMS_Supervisor"))
            {
                Users user = CMSService.GetUserByUserName(username);
                viewUser sup = new viewUser();

                sup = CMSService.GetUser(user.UserId);

                List<int> supContracts = new List<int>();
                foreach (var c in sup.UserContracts)
                {
                    supContracts.Add(c.Id);

                    
                }

                viewStaffList.Staff = viewStaffList.Staff.Where(i => supContracts.Any(s => i.ContractIds.Contains(s))).OrderByDescending(i => i.LastName).ToList();
            }
            else
            {
                return RedirectToAction("Account", "Index");
            }


            return View(viewStaffList);
            


        }

        #endregion

        #region Staff Registry

        public ActionResult ListStaffRegistry()
        {
            //viewStaffRegistry viewstaffregistry = new viewStaffRegistry();

            //viewstaffregistry.ListStaffRegistry = CMSService.ListOfStaffRegistry().OrderByDescending(v => v.Id).ToList();


            viewStaffRegistryList SRlist = new viewStaffRegistryList();

            SRlist = CMSService.ListStaffRegistryItems();

            return View(SRlist);
        }

        public ActionResult ViewStaffRegistry(int Id)
        {
            viewStaffRegistry viewstaffregistry = CMSService.GetStaffRegistry(Id);

            return View(viewstaffregistry);
        }

        #endregion

        #region RAA Activity Tracker

        public ActionResult EditRAATracker(int Id)
        {
            viewRAAActivityTracker viewraatracker;

            if (Id != 0)
            {
                viewraatracker = CMSService.GetRAAActivity(Id);
            }
            else
            {
                viewraatracker = new viewRAAActivityTracker();
            }

            viewraatracker.ListOfActivitiesRAA = codeTable.ListofActivities_RAA;
            viewraatracker.ListOfAPSPAs = CMSService.ListOfPSA().ToList();
            //int UserPSAId = CMSService.GetUserByUserName(username).PSAId;
            //viewraatracker.ListOfAgencies = CMSService.ListOfAgenciesByPSA(UserPSAId).ToList();

            viewraatracker.ListOfAgencies = new List<viewIntakeAgency>();
            var psa = APSCaseService.ListOfPSAsByUserName(username).FirstOrDefault();

            //viewraatracker.ListOfAgencies = CMSService.ListOfAllContracts().Where(i => i.PSAId == psa.Id).ToList();
            foreach (var contract in psa.ListOfContracts)
            {
                viewIntakeAgency agency = new viewIntakeAgency();
                agency.Id = contract.Id;
                agency.ContractName = contract.ContractName;
                agency.ContractId = contract.ContractId;
                agency.DBAName = contract.DBAName;
                viewraatracker.ListOfAgencies.Add(agency);
            }

            return View(viewraatracker);
        }

        [HttpPost]
        public ActionResult SaveRAAActivityTracker(viewRAAActivityTracker viewraatracker)
        {
            viewraatracker.CreatedBy = username;
            viewraatracker.UpdatedBy = username;

            int RAAActivityId = CMSService.SaveRaaActivity(viewraatracker);
            return RedirectToAction("Index", "Staff");
        }

        public ActionResult ViewRAAActivityTracker(int Id)
        {
            viewRAAActivityTracker viewraatracker = CMSService.GetRAAActivity(Id);
            viewraatracker.ListOfActivitiesRAA = codeTable.ListofActivities_RAA;
            viewraatracker.ListOfAPSPAs = CMSService.ListOfPSA().ToList();
            //int UserPSAId = CMSService.GetUserByUserName(username).PSAId;
            //viewraatracker.ListOfAgencies = CMSService.ListOfAgenciesByPSA(UserPSAId).ToList();

            viewraatracker.ListOfAgencies = new List<viewIntakeAgency>();
            var psa = APSCaseService.ListOfPSAsByUserName(username).FirstOrDefault();
            foreach (var contract in psa.ListOfContracts)
            {
                viewIntakeAgency agency = new viewIntakeAgency();
                agency.Id = contract.Id;
                agency.ContractName = contract.ContractName;
                agency.ContractId = contract.ContractId;
                agency.DBAName = contract.DBAName;
                viewraatracker.ListOfAgencies.Add(agency);
            }
            return View(viewraatracker);
        }

        public ActionResult ListRAAActivityTracker()
        {
            viewRAAActivityTracker viewraatracker = new viewRAAActivityTracker();
            viewraatracker.ListOfRAAActivity = CMSService.ListOfRAAActivity().OrderByDescending(v => v.Id).ToList();
            return View(viewraatracker);
        }

        #endregion

        #region Webinars

        public ActionResult ListWebinars()
        {
            viewAdditionalResources viewadditionalresources = new viewAdditionalResources();

            viewadditionalresources.ListAdditionalResourcesURLs = CMSService.ListOfAdditionalResourcesURLs().Where(s => (bool)s.IsWebinar).OrderByDescending(v => v.Id).ToList();

            return View(viewadditionalresources);
        }

        [HttpPost]
        public ActionResult UploadWebinarURL(viewAdditionalResources viewadditionalresources)
        {            
            try
            {
                if (viewadditionalresources.Url != null)
                {
                    viewadditionalresources.UserCreated = username;
                    viewadditionalresources.UserUpdated = username;
                    viewadditionalresources.IsWebinar = true;
                    viewadditionalresources.StatusDescription = CaseStatus.Submitted.ToString();
                    CMSService.SaveAdditionalResourceURL(viewadditionalresources);
                }
                return RedirectToAction("Index", "Staff");
            }
            catch (System.Exception e)
            {
                throw e;
            }
        }

        [HttpPost]
        public ActionResult RemoveURL(int URLId)
        {           
            CMSService.RemoveAdditionalResourceURL(URLId, username);

            return RedirectToAction("Index", "Staff");
        }

        #endregion

        #region User

        public ActionResult EditUsers()
        {
            viewStaffModel staff = new viewStaffModel();

            staff.ListAPSPAUsers = CMSService.ListAllAPSPAUsers().ToList();

            staff.ListRAAUsers = CMSService.ListAllRAAUsers().ToList();

            return View(staff);
        }

        public ActionResult EditAPSPAUser(string userName)
        {
            LoggedInUser user;
            if (userName != null)
            {
                user = CMSService.GetAPSPAUser(userName);
            }
            else
            {
                user = new LoggedInUser();
            }

            user.caselookup = getcaselookup(username, ViewBag.UserContractId);

            return View(user);
        }

        [HttpPost]
        public ActionResult SaveAPSPAUser(LoggedInUser user)
        {
            user.UserUpdated = username;
            CMSService.SaveAPSPAUser(user);
            return Json(user.Id, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ListAllAPSPAUsers()
        {
            List<LoggedInUser> listofusers = new List<LoggedInUser>();

            listofusers = CMSService.ListAllAPSPAUsers().ToList();

            return View(listofusers);
        }

        public ActionResult EditRAAUser(string userName)
        {
            LoggedInUser user;
            if (userName != null)
            {
                user = CMSService.GetRAAUser(userName);
            }
            else
            {
                user = new LoggedInUser();
            }

            user.caselookup = getcaselookup(username, ViewBag.UserContractId);

            return View(user);
        }

        [HttpPost]
        public ActionResult SaveRAAUser(LoggedInUser user)
        {
            user.UserUpdated = username;
            CMSService.SaveRAAUser(user);
            return Json(user.Id, JsonRequestBehavior.AllowGet);
        }

        //public ActionResult ListAllRAAUsers()
        //{
        //    List<LoggedInUser> listofusers = new List<LoggedInUser>();

        //    listofusers = CMSService.ListAllRAAUsers().ToList();

        //    return View(listofusers);
        //}

        #endregion

        #region Helpers

        protected List<SelectListItem> GetAreaAgencies()
        {
            List<SelectListItem> ddlAreaAgencies = new List<SelectListItem>();
            ddlAreaAgencies.Add(new SelectListItem() { Text = "Select", Value = "0" });
            try
            {
                List<viewAreaAgency> lstType = CMSService.ListOfAreaAgencies().ToList();

                foreach (viewAreaAgency type in lstType)
                {
                    SelectListItem item = new SelectListItem() { Text = type.Description, Value = type.Id.ToString() };
                    ddlAreaAgencies.Add(item);
                }
            }
            catch (System.Exception ex)
            {
                throw ex;
            }

            return ddlAreaAgencies;
        }

        protected List<SelectListItem> GetFullTimeEquivalent()
        {
            List<SelectListItem> ddlFullTimeEquivalents = new List<SelectListItem>();
            ddlFullTimeEquivalents.Add(new SelectListItem() { Text = "Select", Value = "0" });
            try
            {
                List<viewFullTimeEquivalent> lstType = CMSService.ListOfFullTimeEquivalent().ToList();

                foreach (viewFullTimeEquivalent type in lstType)
                {
                    //SelectListItem item = new SelectListItem() {Text=type.Name,Value=type.Id.ToString()};
                    SelectListItem item = new SelectListItem() { Text = type.Description, Value = type.Id.ToString() };
                    ddlFullTimeEquivalents.Add(item);
                }
            }
            catch (System.Exception ex)
            {
                throw ex;
            }

            return ddlFullTimeEquivalents;
        }

        protected List<SelectListItem> GetEmployeeEducation()
        {
            List<SelectListItem> ddlEmployeeEducation = new List<SelectListItem>();
            ddlEmployeeEducation.Add(new SelectListItem() { Text = "Select", Value = "0" });
            try
            {
                List<viewEmployeeEducation> lstType = CMSService.ListOfEducation().ToList();

                foreach (viewEmployeeEducation type in lstType)
                {
                    SelectListItem item = new SelectListItem() { Text = type.Description, Value = type.Id.ToString() };
                    ddlEmployeeEducation.Add(item);
                }
            }
            catch (System.Exception ex)
            {
                throw ex;
            }

            return ddlEmployeeEducation;
        }

        protected List<SelectListItem> GetPositionTitlesddl()
        {
            List<SelectListItem> ddlRoles = new List<SelectListItem>();
            ddlRoles.Add(new SelectListItem() { Text = "Select", Value = "0" });
            try
            {
                List<viewPositionTitle> lstType = CMSService.ListofPositionTitles().ToList();

                foreach (viewPositionTitle type in lstType)
                {
                    //SelectListItem item = new SelectListItem() {Text=type.Name,Value=type.Id.ToString()};
                    SelectListItem item = new SelectListItem() { Text = type.Description, Value = type.Id.ToString() };
                    ddlRoles.Add(item);
                }
            }
            catch (System.Exception ex)
            {

                throw ex;

            }

            return ddlRoles;
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
        public class MultipleButtonAttribute : ActionNameSelectorAttribute
        {
            public string Name { get; set; }
            public string Argument { get; set; }

            public override bool IsValidName(ControllerContext controllerContext, string actionName, MethodInfo methodInfo)
            {
                var isValidName = false;
                var keyValue = string.Format("{0}:{1}", Name, Argument);
                var value = controllerContext.Controller.ValueProvider.GetValue(keyValue);

                if (value != null)
                {
                    controllerContext.Controller.ControllerContext.RouteData.Values[Name] = Argument;
                    isValidName = true;
                }

                return isValidName;
            }
        }

        protected AGE.CMS.Data.Models.Intake.viewCaseLookup getcaselookup(string username, int? ContractId)
        {
            AGE.CMS.Data.Models.Intake.viewCaseLookup caselookup = new AGE.CMS.Data.Models.Intake.viewCaseLookup();
            //caselookup.listofagencytypes = new List<viewAgencyType>();

            // caselookup.listofagencytypes = TestService.ListOfAgencyTypes().ToList();
            try
            {
                caselookup.listofcontracts = CMSService.ListOfContracts(username).ToList();
                caselookup.listofreportertypes = codeTable.ListOfReporterTypes;
                caselookup.listofrelations = codeTable.ListOfRelations;
                caselookup.listofallcontracts = CMSService.ListOfAllContracts().ToList();
                //caselookup.listofagencies = CMSService.ListOfAgencies().ToList();
                caselookup.listofpriorities = codeTable.ListOfPriorities;
                //caselookup.listofcaseworkers = CMSService.ListOfWorkers((int)ContractId).ToList();
                caselookup.listofreportstatus = codeTable.ListOfReportStatus;
                caselookup.listoffolders = codeTable.ListOfFolders;
                caselookup.listofemploymenttypes = codeTable.ListOfEmploymentTypes;
                caselookup.listofservices = codeTable.ListOfServices;
                caselookup.listofincomelevels = codeTable.ListOfIncomeLevels;
                caselookup.listofInteragencies = codeTable.ListOfInterAgencies;
                caselookup.listofprimarylanguage = codeTable.ListOfPrimaryLanguages;
                caselookup.listofschoolinglevel = codeTable.ListOfSchoolingLevel;
                caselookup.listoflegalstatus = codeTable.ListOfLegalStatus;
                caselookup.ListOfAAAssociations = codeTable.ListOfAbuserAssociations;
                caselookup.listofAbuserAssociations = codeTable.ListOfAbuserAssociations;
                //caselookup.listofInteragencies = CMSService.ListOfInterAgencies().ToList();
                caselookup.listofservices = codeTable.ListOfServices;
                caselookup.listofailmentconfirmations = codeTable.ListOfAilmentConfirmation;
                caselookup.listofJudgeOutcomes = codeTable.ListOfJudgeOutcomes;
                caselookup.listofVoucherTypes = codeTable.ListOfVoucherTypes;
                caselookup.listofRecordReleaseRequestors = codeTable.ListOfRecordReleaseRequestors;
                caselookup.listofRecordReleaseRequestsReceivedBy = codeTable.ListOfRecordReleaseRequestsReceivedBy;
                caselookup.listofRecordReleaseRequestsReceivers = codeTable.ListOfRecordReleaseRequestReceivers;
                caselookup.listofRecordReleaseTypes = codeTable.ListOfRecordReleaseTypes;
                caselookup.ListClientReceivesServices_Referrals = CMSService.ListClientReceivesServices_Referrals().ToList();
                caselookup.ListOfCaeClosureReasons_IsAble = CMSService.ListCaseClosureReason_IsAble().ToList();
                caselookup.ListOfCaeClosureReasons_IsUnable = CMSService.ListCaseClosureReason_IsUnable().ToList();
                caselookup.ListofClassifications = codeTable.ListofClassications;
                caselookup.ListOfClosureLivingArrangements = codeTable.ListOfClosureLivingArrangements;
                caselookup.ListOfActivitiesRAA = codeTable.ListofActivities_RAA;
                caselookup.ListOfIntakeReportTypes = codeTable.ListOfIntakeReportTypes;
                caselookup.listofveteranstatus = codeTable.ListOfVeteranStatus;
                caselookup.ListMCOs = codeTable.ListOfMCOs;
                caselookup.listofpsas = CMSService.ListOfPSA().ToList();

                //var workers = CMSService.ListOfWorkers((int)ContractId).ToList();
                //foreach (var worker in workers)
                //{
                //    var roles = GetRolesForUser(worker.UserName);
                //}
                using (var PersonService = new PersonProfileWCFServiceClient())
                {
                    var person = PersonService.GetPerson(Guid.Empty);
                    caselookup.listofgender = person.Demographic.DemographicList.ListOfGenderTypes;
                    caselookup.listoflivingstatus = person.Demographic.DemographicList.ListOfLivingStatusTypes;
                    caselookup.listofraces = person.Demographic.DemographicList.ListOfRaceTypes;
                    //var listofveteranstatus = PersonService.ListOfVeteransStatusTypes();
                    //caselookup.listofveteranstatus = new List<dto>(PersonService.ListOfVeteransStatusTypes());            
                    //caselookup.listofcounties = CMSService.ListOfCounties(14).ToList();
                    caselookup.listofgender = new List<PersonProfile.Data.Entities.dtoGenderType>(PersonService.ListOfGenderTypes().ToList());

                    caselookup.listofcounties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();
                    caselookup.listofstates = new List<PersonProfile.Data.Entities.dtoStateType>(PersonService.ListOfStateTypes());
                    caselookup.listofmaritalstatus = new List<PersonProfile.Data.Entities.dtoMaritalStatusType>(PersonService.ListOfMaritalStatusTypes());
                    caselookup.listoflivingarrangements = new List<PersonProfile.Data.Entities.dtoLivingArrangementType>(PersonService.ListOfLivingArrangementTypes());
                    caselookup.ListOfGenderOrientationTypes = new List<PersonProfile.Data.Entities.dtoGenderOrientationType>(PersonService.ListOfGenderOrientationTypes());
                    caselookup.listofdtoethinicities = new List<PersonProfile.Data.Entities.dtoEthnicityType>(PersonService.ListOfEthnicityTypes());
                }
            }
            catch (System.Exception e)
            {
                throw e;
            }
            return caselookup;
        }

        #endregion
    }
}
