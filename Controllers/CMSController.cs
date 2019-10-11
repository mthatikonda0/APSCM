using AGE.CMS.Business;
using AGE.CMS.Core;
using AGE.CMS.Core.APSCaseServiceSvcRef;
using AGE.CMS.Core.CaseManagementReportServiceRef;
using AGE.CMS.Core.RoleServiceAgentSvcRef;
using AGE.CMS.Data.Models.Security;
using log4net;
using MimeKit;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Web.Mvc;
using AGE.CMS.Data.Models.Account;

namespace AGE.CMS.Web.Areas.CMS.Controllers
{
    public class CMSController : BaseController
    {
        protected PersonProfileWCFService.PersonProfileWCFServiceClient PersonService;
        protected string env = "**";
        protected CaseManagementService CMSService;
        protected CMSReportsServiceClient CMSReportsService;
        protected APSCaseWCFServiceClient APSCaseService;
        public CodeTable codeTable;

        public CMSController()
        {
            ViewBag.ProcessingMode = "CMS";

            //using (var APSCaseWCFServiceClient = new APSCaseWCFServiceClient())
            //{
            //    //Added by Praneeth
            //    if (ViewBag.RoleGroupName != AGE.CMS.Data.Models.ENums.Roles.Admin && ViewBag.RoleGroupName != AGE.CMS.Data.Models.ENums.Roles.ReportTakerOnly && ViewBag.RoleGroupName != AGE.CMS.Data.Models.ENums.Roles.RAAAdmin)
            //        ViewData["USERAGENCIES"] = APSCaseWCFServiceClient.ListOfAgenciesByUserName(username).ToList();

            //    else if (ViewBag.RoleGroupName == AGE.CMS.Data.Models.ENums.Roles.RAAAdmin)
            //        ViewData["USERAGENCIES"] = APSCaseWCFServiceClient.ListOfPSAsByUserName(username).ToList();
            //}
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            // Session is available
            codeTable = (CodeTable)Session["CODETABLE"];

            var x = Session["USER"];
            var log = 
            PersonService = new PersonProfileWCFService.PersonProfileWCFServiceClient();
            CMSService = new CaseManagementService((LoggedInUser)(Session["USER"]), log4net.LogManager.GetLogger(typeof(MvcApplication)), codeTable);
            CMSReportsService = new CMSReportsServiceClient();
            APSCaseService = new APSCaseWCFServiceClient();
        }

        public void SendEmail(List<MailAddress> ToEmails, string Message, string Subject)
        {

            string FromName = "Illinois Department on Aging APS Case Management System";
            string CMSLink = System.Configuration.ConfigurationManager.AppSettings["_CMSTestLink"];


            var builder = new BodyBuilder();
            //using (StreamReader SourceReader = System.IO.File.OpenText(System.Configuration.ConfigurationManager.AppSettings["_EmailTemplate"]))
            //{
            //    builder.HtmlBody = SourceReader.ReadToEnd();
            //}
            var message = new MailMessage();
            message.Priority = MailPriority.High;                        

            foreach(var email in ToEmails)
            {
                if(email.DisplayName.ToLower() == "to" && email.Address != null)
                {
                    message.To.Add(email.Address);
                }                
                if (email.DisplayName.ToLower() == "cc" && email.Address != null)
                {
                    message.CC.Add(email.Address);
                }
                if(email.DisplayName.ToLower() == "bcc" && email.Address != null)
                {
                    message.Bcc.Add(email.Address);
                }
            }
           
            message.Subject = Subject;
            //message.Body = string.Format(builder.HtmlBody, FromName, Message, CMSLink);
            message.Body = Message;
            message.IsBodyHtml = true;
             //{
            //    DeliveryFormat = SmtpDeliveryFormat.International,
            //    DeliveryMethod = SmtpDeliveryMethod.Network,
            //})
            using (var smtp = new SmtpClient())           
            {
                smtp.DeliveryFormat = SmtpDeliveryFormat.International;

                try
                {
                    log.Info("Sending mail to " + message.To.ToString() + " subject: " + Subject);

                    smtp.Send(message);                    
                }
                catch (System.Exception e)
                {
                    log.Error("ERROR Sending mail to " + message.To.ToString() + " subject: " + Subject,e);

                    throw e;
                }
            }
        }

        public ActionResult ResetSession()
        {
            return Json("Reset Connection", JsonRequestBehavior.AllowGet);
        }
        #region ADFS

        protected DirectoryEntry directoryEntry;
        protected string externalpath = @"LDAP://external.illinois.gov:389/OU=ILEXTUsers,DC=external,DC=Illinois,DC=gov";
        protected string illinoispath = @"LDAP://illinois.gov:389/OU=ILUsers,DC=Illinois,DC=gov";
        protected string aduser = @"AGE.ADRead.Svc";
        protected string adpwd = "criD5faphlnetr3";

        public string GetEmail(string UserName)
        {

            try
            {
                return GetMailFromAD(UserName);
            }
            catch
            {
                throw new System.Exception("LDAP ERROR");
            }


        }

        public List<string> GetEmails(List<string> UserNames)
        {
            List<string> mails = new List<string>();

            foreach (string username in UserNames)
            {
                mails.Add(GetMailFromAD(username));
            }

            return mails;
        }

        public List<string> SearchUsers(string domain, string SearchText)
        {
            List<string> usernames = new List<string>();
            DirectoryEntry directoryEntry = null;
            DirectorySearcher searcher = null;

            switch (domain)
            {
                case "EXTERNAL":
                    directoryEntry = new DirectoryEntry(externalpath, aduser, adpwd);
                    searcher = new DirectorySearcher(directoryEntry);
                    searcher.Filter = "(&(objectClass=user) (cn=*" + SearchText.Trim() + "*))";
                    searcher.PropertiesToLoad.Add("name");
                    break;
                case "ILLINOIS":
                    directoryEntry = new DirectoryEntry(illinoispath, aduser, adpwd);
                    searcher = new DirectorySearcher(directoryEntry);
                    searcher.Filter = "(&(objectClass=user) (cn=*" + SearchText.Trim() + "*))";
                    searcher.PropertiesToLoad.Add("name");
                    break;
            }

            foreach (SearchResult search in searcher.FindAll())
            {
                usernames.Add(search.Properties["name"][0].ToString());
            }

            return usernames;
        }

        protected string GetMailFromAD(string UserName)
        {
            string mail = "";
            if (UserName.StartsWith(@"EXTERNAL\"))
            {
                DirectoryEntry directoryEntry = new DirectoryEntry(externalpath, aduser, adpwd);
                DirectorySearcher searcher = new DirectorySearcher(directoryEntry);
                searcher.Filter = "(&(objectClass=user) (cn=" + UserName.Trim().Substring(9) + "))";
                searcher.PropertiesToLoad.Add("mail");
                SearchResult result = searcher.FindOne();
                if (result != null)
                {
                    mail = result.Properties["mail"][0].ToString();
                }
            }
            if (UserName.StartsWith(@"ILLINOIS\"))
            {
                DirectoryEntry directoryEntry = new DirectoryEntry(illinoispath, aduser, adpwd);
                DirectorySearcher searcher = new DirectorySearcher(directoryEntry);
                searcher.Filter = "(&(objectClass=user) (cn=" + UserName.Trim().Substring(9) + "))";
                searcher.PropertiesToLoad.Add("mail");
                SearchResult result = searcher.FindOne();
                if (result != null)
                {
                    mail = result.Properties["mail"][0].ToString();
                }
            }

            return mail;

        }

        public List<string> GetMailByRole(List<int> ContractIds, string Application, string Role)
        {
            List<string> result = new List<string>();

            foreach (int id in ContractIds)
            {
                using (var APSService = new APSCaseWCFServiceClient())
                {
                    using (var roleServiceAgentClient = new RoleServiceAgentClient())
                    {
                        List<string> roleusers = roleServiceAgentClient.GetUsersInRole(Application, Role).ToList();
                        List<string> agencyusers = CMSService.ListofUsersByContractId(id).ToList();                       

                        List<string> finalraw = new List<string>();
                        foreach (var u in roleusers)
                        {
                            foreach (var au in agencyusers)
                            {
                                if (u.ToUpper() == au.ToUpper())
                                {
                                    finalraw.Add(u);
                                }
                            }
                        }

                        result.AddRange(GetEmails((from u in roleusers where (finalraw.Contains(u)) select u).ToList()).ToList());
                    }
                }
            }

            return result;
        }



        #endregion





    }
}
