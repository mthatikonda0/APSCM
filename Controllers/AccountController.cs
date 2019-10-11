using AGE.CMS.Data.Models.Account;
using AGE.CMS.Data.Models;
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
using AGE.CMS.Business;
using AGE.CMS.Core.SecurityServiceSvcRef;


namespace AGE.CMS.Web.Areas.CMS.Controllers

{   [ApplicationAuthorization]
    [Layout("_Layout")]
    public class AccountController : CMSController
    {
        //
        // GET: /CMS/Account/
        
    


        public ActionResult Index()
        {
            return RedirectToAction("UserProfile", "Account");
        }


        public ActionResult RegisterUser()
        {


            Users user = CMSService.GetUserByUserName(username);




            CodeTable cT = new CodeTable();

            Session["CODETABLE"] = cT;


            viewUser newUser = new viewUser();

            if (user != null)
            {
                if (user.IsActive == true)
                {
                    using (var securityWCFServiceClient = new SecurityWCFServiceClient())
                    {
                        var nav = securityWCFServiceClient.GetDefaultNavigationByRole(applicationname, codeTable.UserRoleName);
                        return RedirectToAction(nav.ActionName, nav.ControllerName, new { Area = areaname });
                    }
                }
                else
                {
                    newUser = CMSService.GetUser(user.UserId);
                    newUser.UserContracts = newUser.UserContracts.Where(c => c.EndDate == null).ToList();
                    newUser.IsPending = true;

                    if (newUser.UserConfidentiality == null || newUser.UserConfidentiality.UserSignatureDate == null)
                    {
                        return RedirectToAction("UserConfidentiality", "Account", new { @userId = user.UserId });
                    }
                }
            

            }
            else
            {


                newUser.UserName = username;

                newUser.Roles = CMSService.GetRoles();
                newUser.PossibleContracts = CMSService.GetContracts();

                for (int i = 0; i < 3; i++)
                {
                    viewUserContract newContract = new viewUserContract();

                    newUser.UserContracts.Add(newContract);
                }

                newUser.PossiblePSAs = CMSService.GetPSAs();

                newUser.PossibleMCOs = CMSService.GetMCOs();

                newUser.IsAcceptCon = false;

            }





            return View(newUser);

        }


        public ActionResult SubmitUser(viewUser user)
        {



            int id = CMSService.SaveNewUser(user);




            return RedirectToAction("UserConfidentiality", "Account", new { @userId = id }); 

        }

        public ActionResult UserConfidentiality(int userId)
        {
            viewUserConfidentiality uc = new viewUserConfidentiality();

            viewUser user = CMSService.GetUser(userId);

            uc.UserId = user.UserId;
            uc.UserName = user.UserName;
            uc.Date = DateTime.Now;
            foreach(var c in user.UserContracts)
            {
                uc.UserAgencyNames.Add(c.ContractName);
            }
            uc.UserAgencyNames.Add(user.UserMCO.Name);
            uc.UserAgencyNames.Add(user.UserPSA.PSAName);
            uc.UserRoleName = user.RoleDescription;
            uc.UserFirstName = user.FirstName;
            uc.UserLastName = user.LastName;


            return View(uc);
        }


        public ActionResult NewUserConfidentiality(string username)
        {
            viewUserConfidentiality uc = new viewUserConfidentiality();

            Users u = CMSService.GetUserByUserName(username);

            viewUser user = CMSService.GetUser(u.UserId);

            uc.UserId = user.UserId;
            uc.UserName = user.UserName;
            uc.Date = DateTime.Now;
            foreach (var c in user.UserContracts)
            {
                uc.UserAgencyNames.Add(c.ContractName);
            }
            uc.UserAgencyNames.Add(user.UserMCO.Name);
            uc.UserAgencyNames.Add(user.UserPSA.PSAName);
            uc.UserRoleName = user.RoleDescription;
            uc.UserFirstName = user.FirstName;
            uc.UserLastName = user.LastName;


            return View("UserConfidentiality", uc);
        }


        public ActionResult SubmitUserConfidentiality(viewUserConfidentiality uc)
        {

            CMSService.SaveUserConfidentiality(uc);

            

            return RedirectToAction("RegisterUser", "Account");
        }

        public ActionResult UserProfile()
        {
            Users user = CMSService.GetUserByUserName(username);

            viewUser newUser = new viewUser();

            if (user != null)
            {
                newUser = CMSService.GetUser(user.UserId);

                newUser.UserContracts = newUser.UserContracts.Where(i => i.IsActive == true).ToList();
            }

            return View(newUser);
        }

        public ActionResult UserChangeAgency(int userId)
        {
            viewUser user = CMSService.GetUser(userId);
            user.IsPending = false;

            user.UserContracts = user.UserContracts.Where(c => c.EndDate == null).ToList();


            int u = 3 - user.UserContracts.Count();
            for (int i = 0; i < u; i++)
            {
                viewUserContract newContract = new viewUserContract();

                user.UserContracts.Add(newContract);
            }


            user.Roles = CMSService.GetRoles();
            user.PossibleContracts = CMSService.GetContracts();
            user.PossiblePSAs = CMSService.GetPSAs();

            user.PossibleMCOs = CMSService.GetMCOs();
            return View(user);
        }

        public ActionResult SubmitUserChangeAgency(viewUser user)
        {
            CMSService.SaveNewUser(user);
            return RedirectToAction("RegisterUser", "Account");
        }
    }

}
