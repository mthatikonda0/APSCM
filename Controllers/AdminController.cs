using AGE.CMS.Data.Models.Account;
using AGE.CMS.Data.Models;
using AGE.CMS.Data.Models.Admin;
using AGE.CMS.Data.Models.Case;
using AGE.CMS.Data.Models.CaseRecording;
using AGE.CMS.Data.Models.ClientAssessment;
using AGE.CMS.Data.Models.ClientPreparation;
using AGE.CMS.Data.Models.Dashboard;
using AGE.CMS.Data.Models.ENums;
using AGE.CMS.Data.Models.Intake;
using AGE.CMS.Data.Models.Referral;
using AGE.CMS.Data.Models.Timeline;
using AGE.CMS.Web.PersonProfileWCFService;
using DOAFramework.Core.WebMVC.Filters;
using PersonProfile.Data.Entities;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using AGE.CMS.Core;
using AGE.CMS.Data.APS.Tables;
using AGE.CMS.Core.RoleServiceAgentSvcRef;
using System.Configuration;

namespace AGE.CMS.Web.Areas.CMS.Controllers
{
    [Layout("_Layout")]
    public class AdminController : CMSController
    {
        //
        // GET: /CMS/Admin/
        RoleServiceAgentClient rsac = new RoleServiceAgentClient();


        public ActionResult Index()
        {
            AdminReview ar = new AdminReview();

            ar = CMSService.GetAdminReview();

            return View(ar);
        }


        //Users 

        public ActionResult UserAdmin()
        {

            UserAdministration ua = new UserAdministration();

            ua.UsersToReview = CMSService.GetUsersForReview();
            ua.Users = CMSService.GetUsers();
            ua.UsersDisApproved = CMSService.GetDisApprovedUsers();
            ua.UsersInactive = CMSService.GetInactiveUsers();

            return View(ua);
        }

        public ActionResult SupervisorAdmin()
        {

            Users user = CMSService.GetUserByUserName(username);
            int userId = 0;
            if (user != null)
            {
                userId = user.UserId;
            }
            List<int> ContractIds = new List<int>();

            ContractIds = CMSService.GetContractsByUserId(userId);

            UserAdministration ua = new UserAdministration();



            List<viewUser> UserList = CMSService.GetSupervisorAdmin(ContractIds);

            ua.UsersToReview = UserList.Where(x => x.IsActive == false && x.IsApproved == false).ToList();
            ua.Users = UserList.Where(x => x.IsActive == true && ContractIds.Any(c => x.UserContracts.Any(uc => uc.ContractId == c && uc.IsActive == true))).ToList();
            ua.UsersDisApproved = UserList.Where(x => x.IsDisApproved == true && x.IsActive == false).ToList();
            ua.UsersInactive = UserList.Where(x => x.IsActive == false && x.IsApproved == true).ToList();
            List<int> SupervisorContractIds = CMSService.GetContractsByUserId(user.UserId);
            ua.Contracts = CMSService.GetContracts().Where(c => SupervisorContractIds.Contains(c.Id)).ToList();

            return View(ua);

        }


        public ActionResult RAAAdmin()
        {
            Users user = CMSService.GetUserByUserName(username);
            int userId = 0;
            if (user != null)
            {
                userId = user.UserId;
            }
            List<int> RAAIds = new List<int>();

            RAAIds = CMSService.GetPSAsByUserId(userId);

            UserAdministration ua = new UserAdministration();



            List<viewUser> UserList = CMSService.GetRAAAdmin(RAAIds);

            ua.UsersToReview = UserList.Where(x => x.IsActive == false && x.IsApproved == false).ToList();
            ua.Users = UserList.Where(x => x.IsActive == true && RAAIds.Any(c => x.UserPSA.PSAId == c && x.UserPSA.isActive == true)).ToList();
            ua.UsersDisApproved = UserList.Where(x => x.IsDisApproved == true && x.IsActive == false).ToList();
            ua.UsersInactive = UserList.Where(x => x.IsActive == false && x.IsApproved == true).ToList();
            ua.PSAs = CMSService.GetPSAs().Where(c => RAAIds.Contains(c.Id)).ToList();

            return View(ua);
        }
        [HttpPost]
        public ActionResult ApproveUser(viewUser user)
        {
            CMSService.ApproveUser(user);
            viewUser getUser = CMSService.GetUser(user.UserId);
            if (user.IsDisApproved != true)
            {

               

   

                if (getUser != null)
                {

                    string AMRoleName = CMSService.GetRoles().Where(r => r.Id == getUser.RoleId).FirstOrDefault().Description;


                    CustomRoleProvider crp = new CustomRoleProvider();


                    string appName = ConfigurationManager.AppSettings["ApplicationName"];

                    string un = getUser.UserName;

                    rsac.AddUsersToRoles(appName, un, AMRoleName);

                }


                List<MailAddress> EMails = new List<MailAddress>();

                EMails.Add(new MailAddress(getUser.Email, "to"));

                string Message = "Registration for the user " + getUser.UserName + " has been approved.";

                string Subject = "APS Case Management System Registration: Approved";

                SendEmail(EMails, Message, Subject);


            }
            else
            {
                List<MailAddress> EMails = new List<MailAddress>();

                EMails.Add(new MailAddress(getUser.Email, "to"));

                string Message = "For user:" + getUser.UserName + ". Your access to the Illinois Department on Aging Office (IDoA) of Adult Protective Services Case Management System has been denied for the following reason: " + user.DisApprovalMessage + ". Please contact either your Supervisor or if you are a Supervisor contact your IDoA liaison.";
                    
                    
                    
                    

                string Subject = "APS Case Management System Registration: Denied";

                SendEmail(EMails, Message, Subject);
            }

            return RedirectToAction("UserAdmin");

        }


        public JsonResult AjaxDeActivateUser(int userId)
        {
            CMSService.RemoveUser(userId);

            viewUser user = CMSService.GetUser(userId);

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

            int i = 1;

            return Json(i);

        }


        public ActionResult DeActivateUser(int userId)
        {
            CMSService.RemoveUser(userId);

            viewUser user  = CMSService.GetUser(userId);

            if(user != null)
            {
                var username = user.UserName;
                var allroles = rsac.GetAllRoles(applicationname);

                foreach(var role in allroles)
                {
                    if(rsac.IsUserInRole(applicationname, username, role) == 1)
                    {
                        rsac.RemoveUsersFromRoles(applicationname, username, role);
                    }
                }
            }
            

            return RedirectToAction("UserAdmin");
        }

        public ActionResult ReActivateUser(viewUser user)
        {
            CMSService.ReActivateUser(user);


            viewUser getUser = CMSService.GetUser(user.UserId);

            if (getUser != null)
            {

                string AMRoleName = CMSService.GetRoles().Where(r => r.Id == getUser.RoleId).FirstOrDefault().Description;


                CustomRoleProvider crp = new CustomRoleProvider();


                string appName = ConfigurationManager.AppSettings["ApplicationName"];

                string un = getUser.UserName;

                rsac.AddUsersToRoles(appName, un, AMRoleName);

            }

            return RedirectToAction("UserAdmin");
        }

        public ActionResult EditUserSettings(int userId)
        {
            viewUser user = new viewUser();

            user = CMSService.GetUserSettings(userId);

            viewUserContract newContract = new viewUserContract();

            user.CountOfActiveContracts = user.UserContracts.Where(u => u.IsActive == true).Count();
            user.CountOfExistingContracts = user.UserContracts.Count();

            List<int> contractsToRemove = new List<int>();

            contractsToRemove = user.UserContracts.Select(c => c.ContractId).ToList();

            for(int i = 0; i < 3; i++)
            {
                user.UserContracts.Add(newContract);
            }

            user.PossibleContracts = CMSService.GetContracts();
            user.PossibleContracts = user.PossibleContracts.Where(c => !contractsToRemove.Contains(c.Id)).ToList();
            user.PossibleMCOs = CMSService.GetMCOs();
            user.PossiblePSAs = CMSService.GetPSAs();
            user.Roles = CMSService.GetRoles();

            return View(user);
        }


        public ActionResult SaveUserSettings(viewUser user)
        {


            viewUser getUser = CMSService.GetUser(user.UserId);

            if(getUser.RoleId != user.RoleId)
            {
                //remove

                var username = user.UserName;
                var allroles = rsac.GetAllRoles(applicationname);

                foreach (var role in allroles)
                {
                    if (rsac.IsUserInRole(applicationname, username, role) == 1)
                    {
                        rsac.RemoveUsersFromRoles(applicationname, username, role);
                    }
                }


                //add



                string AMRoleName = CMSService.GetRoles().Where(r => r.Id == user.RoleId).FirstOrDefault().Description;


                CustomRoleProvider crp = new CustomRoleProvider();


                string appName = ConfigurationManager.AppSettings["ApplicationName"];

                string un = user.UserName;

                rsac.AddUsersToRoles(appName, un, AMRoleName);



            }





            CMSService.SaveUserSettings(user);


            






            return RedirectToAction("UserAdmin");
        }

        //Staff Directory

        public ActionResult StaffDirectoryForReview()
        {
            StaffDirectoryAdminList sdList = new StaffDirectoryAdminList();

            sdList = CMSService.GetSDListForReview();

            return View(sdList);
        }

        //Contact Management

        public ActionResult ListExternalContacts(string tab)
        {

            viewExternalContact vContact = new viewExternalContact();
            vContact = CMSService.GetExternalContacts();
            ViewBag.Tab = tab;
            return View(vContact);
        }


        public PartialViewResult EditExternalContact(int contactId)
        {
            viewExternalContactItem contact = CMSService.GetExternalContact(contactId);

            return PartialView(contact);
        }

        public ActionResult SaveExternalContact(viewExternalContactItem contact)
        {

            CMSService.SaveExternalContact(contact);

            return RedirectToAction("ListExternalContacts", new { @tab = "Ext" });
        }


        //Contract Management

        public ActionResult ContractAdministration(string tab)
        {
            viewContractAdministration ConAdmin = new viewContractAdministration();
          
            ConAdmin = CMSService.GetContractAdmin();
            ViewBag.Tab = tab;

            return View(ConAdmin);
        }


        public ActionResult EditContract(int conId)
        {
            viewContract contract = new viewContract();

            contract = CMSService.GetContract(conId);

            if(contract == null)
            {
                contract = new viewContract();
            }

            return PartialView(contract);
        }


        public JsonResult AjaxSaveContract(viewContract contract)
        {
          


                CMSService.SaveContract(contract);

            return Json(contract);

            
            
        }
        public ActionResult SaveContract(viewContract contract)
        {
            if (contract != null)
            {

                CMSService.SaveContract(contract);
            }

            return RedirectToAction("ContractAdministration", new { @tab = "Contract" });
        }

        public ActionResult EditMCO(int mcoId)
        {
            viewMCO mco = new AGE.CMS.Data.Models.Admin.viewMCO();

            mco = CMSService.GetMCO(mcoId);

            if(mco == null)
            {
                mco = new viewMCO();
            }
            return PartialView(mco);
        }

        public ActionResult SaveMCO(viewMCO mco)
        {
            CMSService.SaveMCO(mco);

            return RedirectToAction("ListExternalContacts", new { @tab = "MCO" });
        }



        public ActionResult EditPSA(int psaId)
        {
            viewPSA psa = CMSService.GetPSA(psaId);

            if(psa == null)
            {
                psa = new viewPSA();
            }

            return PartialView(psa);
        }


        public JsonResult AjaxSavePSA(viewPSA psa)
        {
            if (psa != null && psa.Id != null)
            {


                CMSService.SavePSA(psa);
            }
            return Json(psa);
        }
        public ActionResult SavePSA(viewPSA psa)
        {
            CMSService.SavePSA(psa);

            return RedirectToAction("ContractAdministration", new { @tab = "PSA" });
        }
    }


}
