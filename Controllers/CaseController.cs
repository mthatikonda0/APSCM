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
using AGE.CMS.Data.Models.Admin;
using AGE.CMS.Data.Models.Email;


namespace AGE.CMS.Web.Areas.CMS.Controllers
{
    [Layout("_Layout")]
    public class CaseController : CMSController
    {
        public ActionResult refresh()
        {

            //string[] myCookies = Request.Cookies.AllKeys;
            //foreach (string cookie in myCookies)
            //{

            //    Response.Cookies[cookie].Expires = DateTime.Now.AddDays(-1);
            //}

            var result = "ok";

            return Json(result, JsonRequestBehavior.AllowGet);

        }

        #region Index

        public ActionResult Index(string mode)
        {
            if (mode == "agency")
            {

                viewCaseLookup caselookup = getcaselookup(username, ViewBag.UserContractId);
                int contractId = caselookup.listofcontracts.FirstOrDefault().Id;
                return RedirectToAction("ListOfAssignedIntakesToAgency", new { ContractId = contractId });
            }

            else if (mode == "worker")
            {

                viewCaseLookup caselookup = getcaselookup(username, ViewBag.UserContractId);
                int contractId = caselookup.listofcontracts.FirstOrDefault().Id;
                return RedirectToAction("ListOfAssignedIntakesToCaseWorker", new { ContractId = contractId, UserName = username });
            }
            return RedirectToAction("UnAuthorized", "Error");
        }


        #endregion

        #region Client

        public ActionResult EditClient(int Id, int CaseheaderId, string Mode)
        {
            viewClientCMS viewclient = new viewClientCMS();

            if (Id != 0)
            {
                viewclient = CMSService.GetviewClientCMS(Id);
            }
            else
            {
                viewclient = new viewClientCMS();
            }
            viewclient.CaseheaderId = CaseheaderId;
            viewclient.caselookup = getcaselookup(username, ViewBag.UserContractId);
            viewclient.mode = Mode;

            return View(viewclient);
        }

        [HttpPost]
        public ActionResult SaveClient(viewClientCMS viewclient)
        {
            viewclient.Person.CreatedBy = username;
            viewclient.Person.UpdatedBy = username;
            viewclient.Person.Demographic.CreatedBy = username;
            viewclient.Person.Demographic.UpdatedBy = username;
            viewclient.Person.Address.CreatedBy = username;
            viewclient.Person.Address.UpdatedBy = username;

            try
            {
                dtoPerson dtopersonBefore = new dtoPerson();
                dtopersonBefore = CMSService.GetPerson((Guid)viewclient.PersonKey);
                //using (var PersonService = new PersonProfileWCFServiceClient())
                //{
                //    dtopersonBefore = PersonService.GetPerson((Guid)viewclient.PersonKey);
                //}
                if (dtopersonBefore != null)
                {
                    viewclient.Person.CreatedBy = username;
                    viewclient.Person.UpdatedBy = username;
                    viewclient.Person.Id = dtopersonBefore.Id;
                    viewclient.Person.PersonKey = dtopersonBefore.PersonKey;
                }
                dtoPerson dtoperson = new dtoPerson();
                dtoperson = CMSService.Save(viewclient.Person, true);

                //using (var PersonService = new PersonProfileWCFServiceClient())
                //{
                //    dtoperson = PersonService.Save(viewclient.Person, true);
                //}
                viewclient.Person = dtoperson;
                viewclient.PersonKey = dtoperson.PersonKey;
                viewclient.UserCreated = username;
                viewclient.UserUpdated = username;
                viewclient.Person.Demographic = dtoperson.Demographic == null ? null : dtoperson.Demographic;
                viewclient.Person.Address.StateType = dtoperson.Address != null && dtoperson.Address.StateType != null ? dtoperson.Address.StateType : null;
                viewclient.Person.Address.CountyType = dtoperson.Address != null && dtoperson.Address.CountyType != null ? dtoperson.Address.CountyType : null;
                if (viewclient.Person.Address.CountyType.Id != 0)
                {
                    using (var PersonService = new PersonProfileWCFServiceClient())
                    {
                        viewclient.Person.Address.CountyType = (from county in PersonService.ListOfCountyTypes(14).Where(i => i.Id == viewclient.Person.Address.CountyType.Id) select county).FirstOrDefault();
                    }
                }
                viewclient.Person.updated = dtoperson.updated;
                viewclient.Id = CMSService.SaveClientCMS(viewclient);
                //if (dtopersonBefore.Id > 0)
                    //using (var PersonService = new PersonProfileWCFServiceClient())
                    //{
                    //    PersonService.SaveLog(dtopersonBefore, true);
                    //}
            }
            catch (System.Exception e)
            {
                throw new System.Exception("Error with Updating Client Data", e);
            }

            return RedirectToAction("ManageCase", "Case", new { CaseheaderId = viewclient.CaseheaderId });
        }

        #endregion

        #region Manage Case

        public ActionResult ManageCase(int CaseheaderId)
        {
            viewCaseHeader viewcase = new viewCaseHeader();
            //tlh
                //            using (var CaseManagementServiceClient = new AGE.CMS.Core.CaseManagementServiceRef.CaseManagementServiceClient())
                //            {
                viewcase = CMSService.GetCaseHeaderById(CaseheaderId);
                if (viewcase.ClosureAtAssessment != null && viewcase.ClosureAtAssessment == true)
                {
                    CMSService.CloseCase(viewcase.Id, username);
                }

//            }

            viewcase.caselookup = getcaselookup(username, ViewBag.UserContractId);
            viewcase.Client.CaseheaderId = CaseheaderId;

            viewcase.UserContractIds = contractids;
            return View(viewcase);
        }

        public ActionResult OpenClosedCase(int CaseheaderId)
        {
            CMSService.OpenClosedCase(CaseheaderId);
            return RedirectToAction("ListOfCasesInAgency", "Case");
        }

        public ActionResult RemoveCase(int CaseheaderId)
        {
            CMSService.RemoveCase(CaseheaderId);
            return RedirectToAction("ListOfCasesInAgency", "Case");
        }

        #endregion

        #region Transfer Case

        [HttpPost]
        //[MultipleButton(Name = "action", Argument = "TransferCase")]
        public ActionResult TransferCase(int Id, int ContractId)
        {

            CMSService.TransferCase(Id, ContractId, username);

            var contract = CMSService.GetContract(ContractId);

            var usercontract = CMSService.GetContract(contractids.FirstOrDefault());

            List<MailAddress> EMails = new List<MailAddress>();

            EMails.Add(new MailAddress(contract.Email, "To"));

            string Message = "You have received an APS transfer case from " + usercontract.ContractName + ".";

            string Subject = "Transferred Case";

            SendEmail(EMails, Message, Subject);

            //return RedirectToAction("ListOfCasesInAgency", "Case");
            var redirectUrl = new UrlHelper(Request.RequestContext).Action("ListOfCasesInAgency", "Case");

            return Json(new { Url = redirectUrl }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region ToCheckForms

        public ActionResult ToCheckForms(int caseheaderid, List<string> selectedIds)
        {
            var caseid = caseheaderid;

            var selectedformIds = new List<int>();

            if (selectedIds != null && selectedIds.Count() > 0)
            {
                for (var i = 0; i < selectedIds.Count(); i++)
                {
                    selectedformIds.Add(Int32.Parse(selectedIds[i]));
                }
            }

            CMSService.AddCaseActions(caseid, selectedformIds, username);

            if (Session["CURRENTVIEWCASEMODEL"] != null)
            {
                ViewOpenClosedTransferedCasesModel tmpViewCaseModel = (ViewOpenClosedTransferedCasesModel)Session["CURRENTVIEWCASEMODEL"];

                ViewOpenClosedTransferedCasesListItem theCase = (from oc in tmpViewCaseModel.AllOpenCases where oc.Id == caseheaderid select oc).FirstOrDefault();

                if (theCase != null)
                {
                    theCase.ListOfFormsToCheck.Clear();

                    foreach (int f in selectedformIds)
                    {
                        AGE.CMS.Data.APS.Tables.Caseheader_ToCheckForms tcf = new AGE.CMS.Data.APS.Tables.Caseheader_ToCheckForms();
                        tcf.CaseheaderId = caseheaderid;
                        tcf.FormId = f;

                        theCase.ListOfFormsToCheck.Add(tcf);
                    }
                }
            }

            return Json("ok", JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region For Billing

        public ActionResult ValidateCase(int IntakeId)
        {
            var valid = CMSService.ValidateCase(IntakeId);

            return View(valid);
        }

        [HttpPost]
        public ActionResult SubmitToBilling(int IntakeId)
        {
           var Intake =  CMSService.GetIntake(IntakeId);
           //if (Intake.IsSN == "y")
           {
               CMSService.FillAbuserWithClientForSN(IntakeId);
           }
            CMSService.UpdateIntakeStatus(IntakeId, CaseStatus.BillingInitaited.ToString(), username);

            var redirectUrl = new UrlHelper(Request.RequestContext).Action("ListOfCasesInAgency", "Case");

            return Json(redirectUrl, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region List Of Cases in Agency

        public ActionResult FilterAgencyCaseLists(string FirstName, string LastName, string DOB, string SSN, string CW, bool IsAbuser)
        {
            if (Session["CURRENTVIEWCASEMODEL"] != null)
            {
                ViewOpenClosedTransferedCasesModel tmpViewCaseModel = (ViewOpenClosedTransferedCasesModel)Session["CURRENTVIEWCASEMODEL"];

                tmpViewCaseModel.FilterFirstName = "";
                tmpViewCaseModel.FilterLastName = "";
                tmpViewCaseModel.FilterDOB = "";
                tmpViewCaseModel.FilterSSN = "";
                tmpViewCaseModel.FilterCWLastName = "";
                tmpViewCaseModel.IsAbuserSearch = IsAbuser;

                if (FirstName != null)
                {
                    tmpViewCaseModel.FilterFirstName = FirstName.Trim();
                }

                if (LastName != null)
                {
                    tmpViewCaseModel.FilterLastName = LastName.Trim();
                }

                if (DOB != null)
                {
                    tmpViewCaseModel.FilterDOB = DOB.Trim();
                }

                if (SSN != null)
                {
                    tmpViewCaseModel.FilterSSN = SSN.Trim();
                    tmpViewCaseModel.FilterSSN = CMSService.RemoveNonNumbers(tmpViewCaseModel.FilterSSN);
                }

                if (CW != null)
                {
                    tmpViewCaseModel.FilterCWLastName= CW.Trim();
                }


            }

            return RedirectToAction("ListOfCasesInAgency", "case");
        }


        public ActionResult ListOfCasesInAgency(int page = 0, string listName = "")
        {
            // NOTE when parameter "page" is zero, this should reset everyting back 

            // Are we back for some page data.
            if (page > 0 && Session["CURRENTVIEWCASEMODEL"] != null && (listName != "ALL" || listName == ""))
            {

                ViewOpenClosedTransferedCasesModel tmpViewCaseModel = (ViewOpenClosedTransferedCasesModel)Session["CURRENTVIEWCASEMODEL"];


                if (tmpViewCaseModel.IsFiltered())
                {
                    // filter all records, shouldn't matter if we are switch page and refilter, 
                    // same filter same records.   If "Filter" button is hit it reloads everything by sending page=0
                    //Open Cases



                    if (tmpViewCaseModel.IsAbuserSearch == true)
                    {
                        if (tmpViewCaseModel.FilterLastName != null && tmpViewCaseModel.FilterLastName != "")
                        {
                            tmpViewCaseModel.AllOpenCases = tmpViewCaseModel.AllOpenCases.Where(c => c.AbuserLastName != null && c.AbuserLastName != null && c.AbuserLastName.ToLower().Contains(tmpViewCaseModel.FilterLastName)).ToList();
                        }


                        if (tmpViewCaseModel.FilterFirstName != null && tmpViewCaseModel.FilterFirstName != "")
                        {
                            tmpViewCaseModel.AllOpenCases = tmpViewCaseModel.AllOpenCases.Where(c => c.AbuserFirstName != null && c.AbuserFirstName != null && c.AbuserFirstName.ToLower().Contains(tmpViewCaseModel.FilterFirstName)).ToList();
                        }


                        if (tmpViewCaseModel.FilterSSN != null && tmpViewCaseModel.FilterSSN != "")
                        {
                            tmpViewCaseModel.AllOpenCases = tmpViewCaseModel.AllOpenCases.Where(c => c.AbuserSSN != null && c.AbuserSSN != null && c.AbuserSSN.Contains(tmpViewCaseModel.FilterSSN)).ToList();
                        }
                    }
                    else
                    {

                        if (tmpViewCaseModel.FilterLastName != null && tmpViewCaseModel.FilterLastName != "")
                        {
                            tmpViewCaseModel.AllOpenCases = tmpViewCaseModel.AllOpenCases.Where(c => c.LastName != null && c.LastName != null && c.LastName.ToLower().Contains(tmpViewCaseModel.FilterLastName)).ToList();
                        }


                        if (tmpViewCaseModel.FilterFirstName != null && tmpViewCaseModel.FilterFirstName != "")
                        {
                            tmpViewCaseModel.AllOpenCases = tmpViewCaseModel.AllOpenCases.Where(c => c.FirstName != null && c.FirstName != null && c.FirstName.ToLower().Contains(tmpViewCaseModel.FilterFirstName)).ToList();
                        }


                        if (tmpViewCaseModel.FilterSSN != null && tmpViewCaseModel.FilterSSN != "")
                        {
                            tmpViewCaseModel.AllOpenCases = tmpViewCaseModel.AllOpenCases.Where(c => c.SSN != null && c.SSN != null && c.SSN.Contains(tmpViewCaseModel.FilterSSN)).ToList();
                        }

                    }



                    if (tmpViewCaseModel.FilterCWLastName != null && tmpViewCaseModel.FilterCWLastName != "")
                    {
                        tmpViewCaseModel.AllOpenCases = tmpViewCaseModel.AllOpenCases.Where(c => c.CaseWorkerNameOnly != null && c.CaseWorkerNameOnly != null && c.CaseWorkerNameOnly.ToLower().Contains(tmpViewCaseModel.FilterCWLastName.ToLower())).ToList();
                    }




                    //Closed Cases

                    if (tmpViewCaseModel.IsAbuserSearch == true)
                    {
                        if (tmpViewCaseModel.FilterLastName != null && tmpViewCaseModel.FilterLastName != "")
                        {
                            tmpViewCaseModel.AllClosedCases = tmpViewCaseModel.AllClosedCases.Where(c => c.AbuserLastName != null && c.AbuserLastName != null && c.AbuserLastName.ToLower().Contains(tmpViewCaseModel.FilterLastName)).ToList();
                        }


                        if (tmpViewCaseModel.FilterFirstName != null && tmpViewCaseModel.FilterFirstName != "")
                        {
                            tmpViewCaseModel.AllClosedCases = tmpViewCaseModel.AllClosedCases.Where(c => c.AbuserFirstName != null && c.AbuserFirstName != null && c.AbuserFirstName.ToLower().Contains(tmpViewCaseModel.FilterFirstName)).ToList();
                        }


                        if (tmpViewCaseModel.FilterSSN != null && tmpViewCaseModel.FilterSSN != "")
                        {
                            tmpViewCaseModel.AllClosedCases = tmpViewCaseModel.AllClosedCases.Where(c => c.AbuserSSN != null && c.AbuserSSN != null && c.AbuserSSN.Contains(tmpViewCaseModel.FilterSSN)).ToList();
                        }
                    }
                    else
                    {

                        if (tmpViewCaseModel.FilterLastName != null && tmpViewCaseModel.FilterLastName != "")
                        {
                            tmpViewCaseModel.AllClosedCases = tmpViewCaseModel.AllClosedCases.Where(c => c.LastName != null && c.LastName != null && c.LastName.ToLower().Contains(tmpViewCaseModel.FilterLastName)).ToList();
                        }


                        if (tmpViewCaseModel.FilterFirstName != null && tmpViewCaseModel.FilterFirstName != "")
                        {
                            tmpViewCaseModel.AllClosedCases = tmpViewCaseModel.AllClosedCases.Where(c => c.FirstName != null && c.FirstName != null && c.FirstName.ToLower().Contains(tmpViewCaseModel.FilterFirstName)).ToList();
                        }


                        if (tmpViewCaseModel.FilterSSN != null && tmpViewCaseModel.FilterSSN != "")
                        {
                            tmpViewCaseModel.AllClosedCases = tmpViewCaseModel.AllClosedCases.Where(c => c.SSN != null && c.SSN != null && c.SSN.Contains(tmpViewCaseModel.FilterSSN)).ToList();
                        }

                    }



                    if (tmpViewCaseModel.FilterCWLastName != null && tmpViewCaseModel.FilterCWLastName != "")
                    {
                        tmpViewCaseModel.AllClosedCases = tmpViewCaseModel.AllClosedCases.Where(c => c.CaseWorkerNameOnly != null && c.CaseWorkerNameOnly != null && c.CaseWorkerNameOnly.ToLower().Contains(tmpViewCaseModel.FilterCWLastName.ToLower())).ToList();
                    }

                    //Transferred 


                    if (tmpViewCaseModel.IsAbuserSearch == true)
                    {
                        if (tmpViewCaseModel.FilterLastName != null && tmpViewCaseModel.FilterLastName != "")
                        {
                            tmpViewCaseModel.AllTransferedCases = tmpViewCaseModel.AllTransferedCases.Where(c => c.AbuserLastName != null && c.AbuserLastName != null && c.AbuserLastName.ToLower().Contains(tmpViewCaseModel.FilterLastName)).ToList();
                        }


                        if (tmpViewCaseModel.FilterFirstName != null && tmpViewCaseModel.FilterFirstName != "")
                        {
                            tmpViewCaseModel.AllTransferedCases = tmpViewCaseModel.AllTransferedCases.Where(c => c.AbuserFirstName != null && c.AbuserFirstName != null && c.AbuserFirstName.ToLower().Contains(tmpViewCaseModel.FilterFirstName)).ToList();
                        }


                        if (tmpViewCaseModel.FilterSSN != null && tmpViewCaseModel.FilterSSN != "")
                        {
                            tmpViewCaseModel.AllTransferedCases = tmpViewCaseModel.AllTransferedCases.Where(c => c.AbuserSSN != null && c.AbuserSSN != null && c.AbuserSSN.Contains(tmpViewCaseModel.FilterSSN)).ToList();
                        }
                    }
                    else
                    {

                        if (tmpViewCaseModel.FilterLastName != null && tmpViewCaseModel.FilterLastName != "")
                        {
                            tmpViewCaseModel.AllTransferedCases = tmpViewCaseModel.AllTransferedCases.Where(c => c.LastName != null && c.LastName != null && c.LastName.ToLower().Contains(tmpViewCaseModel.FilterLastName)).ToList();
                        }


                        if (tmpViewCaseModel.FilterFirstName != null && tmpViewCaseModel.FilterFirstName != "")
                        {
                            tmpViewCaseModel.AllTransferedCases = tmpViewCaseModel.AllTransferedCases.Where(c => c.FirstName != null && c.FirstName != null && c.FirstName.ToLower().Contains(tmpViewCaseModel.FilterFirstName)).ToList();
                        }


                        if (tmpViewCaseModel.FilterSSN != null && tmpViewCaseModel.FilterSSN != "")
                        {
                            tmpViewCaseModel.AllTransferedCases = tmpViewCaseModel.AllTransferedCases.Where(c => c.SSN != null && c.SSN != null && c.SSN.Contains(tmpViewCaseModel.FilterSSN)).ToList();
                        }

                    }



                    if (tmpViewCaseModel.FilterCWLastName != null && tmpViewCaseModel.FilterCWLastName != "")
                    {
                        tmpViewCaseModel.AllTransferedCases = tmpViewCaseModel.AllTransferedCases.Where(c => c.CaseWorkerNameOnly != null && c.CaseWorkerNameOnly != null && c.CaseWorkerNameOnly.ToLower().Contains(tmpViewCaseModel.FilterCWLastName.ToLower())).ToList();
                    }


                    if (tmpViewCaseModel.IsAbuserSearch == true)
                    {

                        try
                        {
                            if (tmpViewCaseModel.FilterDOB.Length > 0)
                            {
                                DateTime dobToDate = Convert.ToDateTime(tmpViewCaseModel.FilterDOB);
                                tmpViewCaseModel.AllOpenCases = tmpViewCaseModel.AllOpenCases.Where(c => c.DOB == dobToDate).ToList();
                                tmpViewCaseModel.AllTransferedCases = tmpViewCaseModel.AllTransferedCases.Where(c => c.DOB == dobToDate).ToList();
                                tmpViewCaseModel.AllClosedCases = tmpViewCaseModel.AllClosedCases.Where(c => c.DOB == dobToDate).ToList();
                            }
                        }
                        catch { }

                    }

                    else

                    {
                        try
                        {
                            if (tmpViewCaseModel.FilterDOB.Length > 0)
                            {
                                DateTime dobToDate = Convert.ToDateTime(tmpViewCaseModel.FilterDOB);
                                tmpViewCaseModel.AllOpenCases = tmpViewCaseModel.AllOpenCases.Where(c => c.AbuserDOB == dobToDate).ToList();
                                tmpViewCaseModel.AllTransferedCases = tmpViewCaseModel.AllTransferedCases.Where(c => c.AbuserDOB == dobToDate).ToList();
                                tmpViewCaseModel.AllClosedCases = tmpViewCaseModel.AllClosedCases.Where(c => c.AbuserDOB == dobToDate).ToList();
                            }
                        }
                        catch { }
                    }
                }

                if (listName.ToUpper() == "OPEN")
                {
                    tmpViewCaseModel.OpenCases = tmpViewCaseModel.GetOpenCasePageData(page, 10);
                }

                if (listName.ToUpper() == "CLOSED")
                {
                    tmpViewCaseModel.ClosedCases = tmpViewCaseModel.GetClosedCasePageData(page, 10);
                }

                if (listName.ToUpper() == "TRANSFERED")
                {
                    tmpViewCaseModel.TransferedCases = tmpViewCaseModel.GetTransferedCasePageData(page, 10);
                }


                //tlh here
                // Update the forms that are checked.
                List<viewCaseHeader> formsToCheckCases = CMSService.ListofAllCasesFormsToCheck(tmpViewCaseModel.AllContracts.Select(c => c.Id).ToList());
                List<int> OpenCasesIds = (from oc in tmpViewCaseModel.AllOpenCases select oc.Id).ToList();
                List<int> formCaseIds = (from fc in formsToCheckCases select fc.Id).ToList();
                List<int> noFormsCheckedCases = (from x in OpenCasesIds where !formCaseIds.Contains(x) select x).ToList();

                //LMD - Realized that the record is deleted from the database once the forms is not checked
                //so the form wasn't being wasn't found in the list/loop below and so wasn't cleared if cleared on caseworker side. Added this loop. 
                foreach (var caseNoForm in tmpViewCaseModel.AllOpenCases.Where(c => noFormsCheckedCases.Contains(c.Id)).ToList())
                {
                    caseNoForm.ListOfFormsToCheck.Clear();
                        foreach (var c in caseNoForm.ListOfFormNames)
                        {
                            
                                c.IsChecked = false;
                            
                        }
                    
                }
                
                
                foreach (var casesRec in formsToCheckCases)
                {
                    ViewOpenClosedTransferedCasesListItem theCase = (from oc in tmpViewCaseModel.AllOpenCases where oc.Id == casesRec.Id select oc).FirstOrDefault();
                    viewCaseHeader theCurrentCaseFromDB = (from oc in formsToCheckCases where oc.Id == casesRec.Id select oc).FirstOrDefault();




                   if (theCase != null && theCurrentCaseFromDB != null)
                    {
                        theCase.ListOfFormsToCheck.Clear();

                        theCase.ListOfFormsToCheck.AddRange(theCurrentCaseFromDB.ListOfFormsToCheck);
                        //lmd
                        foreach(var l in theCase.ListOfFormsToCheck)
                        {
                            foreach(var c in theCase.ListOfFormNames)
                            {
                                if(l.FormId == c.Id)
                                {
                                    c.IsChecked = true;
                                }
                            }
                        }
                    }
                }




                return View(tmpViewCaseModel);

            }


            // Getting here, will create a brand new ViewOpenClosedTransferedCasesModel and put it in session, that will reset all paging and filters.
            ViewOpenClosedTransferedCasesModel viewCaseModel = new ViewOpenClosedTransferedCasesModel();
            List<int> allContractsProcessing = new List<int>();


            //LMD Quick Fix
            if (codeTable.ContractIds != null && codeTable.ContractIds.Count != 0)
            {
                viewCaseModel.ContractId = codeTable.ContractIds.FirstOrDefault();
                viewCaseModel.ContractIds = codeTable.ContractIds; //LMD added this so we're getting the list instead of the single contract Id
            }
            viewCaseModel.AllContracts = CMSService.ListOfAllContracts().ToList();

            // add single contract
            allContractsProcessing.Add(viewCaseModel.ContractId);

            if (User.IsInRole("CMS_IDOAStaff"))
            {
                // replace with list of all contracts
                allContractsProcessing = viewCaseModel.AllContracts.Select(c => c.Id).ToList();
            }
            else if (User.IsInRole("CMS_RAAAdmin"))
            {
                //var userpsa = APSCaseService.ListOfPSAsByUserName(username).FirstOrDefault();
                var userpsa = CMSService.GetPSAsByUser(username);
                allContractsProcessing = (from contract in userpsa.ListOfContracts select contract.Id).ToList();
            }
            else if (User.IsInRole("CMS_Supervisor") || User.IsInRole("CMS_Caseworker"))
            {
                //allContractsProcessing.Add(viewCaseModel.ContractId); LMD
                foreach(var c in viewCaseModel.ContractIds)
                {
                    allContractsProcessing.Add(c);
                }
            }

            //  viewCaseModel.CaseWorkers = CMSService.ListOfWorkersByContracts(codeTable.ContractIds).ToList();

            viewCaseModel.CaseWorkers = CMSService.GetListOfCaseworkersByContract(codeTable.ContractIds);
          // viewCaseModel.AllContracts = viewCaseModel.AllContracts.Where(c => allContractsProcessing.Contains(c.Id)).ToList();

            List<viewCaseHeader> viewCases = CMSService.ListofAllCases(allContractsProcessing);

            // Open = 3 
            // Closed = 4
            // Transfered = 5
            var caseList = viewCases.Where(c => c.StatusId == 3 || c.StatusId == 4 || c.StatusId == 5).ToList();
            foreach (var caseRec in caseList)
            {
                ViewOpenClosedTransferedCasesListItem item = new ViewOpenClosedTransferedCasesListItem();

                item.Id = caseRec.Id;
                item.ClientId = caseRec.ClientId.Value;
                item.FirstName = caseRec.FirstName;
                item.LastName = caseRec.LastName;
                item.DateIntake = caseRec.IntakeDate;
                item.Status = codeTable.ListOfStateTypes.Where(s => s.Id == caseRec.StatusId).FirstOrDefault().Description;
                item.ContractDescription = caseRec.ContractDescription;
                if (caseRec.CaseWorker != null)
                {
                    item.CaseWorkerName = CleanCaseworker(caseRec.CaseWorker); // LMD - what is the difference between these two?
                    item.CaseWorkerNameOnly = CleanCaseworker(caseRec.CaseWorker);
                    //item.CaseWorkerNameOnly = caseRec.CaseWorker.ToString().Remove(0, 9).Replace(".", " ").ToUpperInvariant();
                }
                else
                {
                    item.CaseWorkerName = "";
                    item.CaseWorkerNameOnly = "";
                }
                item.DateCreated = caseRec.DateCreated.Value;
                item.DateClosed = caseRec.ClosureDate.Value;
                item.DateIntake = caseRec.IntakeDate;
                item.TransferredToAgencyDescription = caseRec.TransferredToAgencyDescription;
                item.SSN = caseRec.SSN;
                item.ListOfFormNames = caseRec.ListOfFormNames;
                item.ListOfFormsToCheck = caseRec.ListOfFormsToCheck;
                //item.AbuserLastName = caseRec.AbuserLastName;
                //item.AbuserFirstName = caseRec.AbuserFirstName;
                //item.AbuserDOB = caseRec.AbuserDOB;
               // item.AbuserSSN = caseRec.AbuserSSN;
                if(caseRec.DOB != null)
                {
                    item.DOB = caseRec.DOB.Value;
                }
                if (caseRec.DateTransferred != null)
                {
                    item.DateTransferred = caseRec.DateTransferred.Value;
                }
                if (caseRec.DateUpdated != null)
                {
                    item.DateUpdated = caseRec.DateUpdated.Value;
                }

                switch (caseRec.StatusId)
                    {
                    case 3:
                        viewCaseModel.AllOpenCases.Add(item);
                        break;
                    case 4:
                        viewCaseModel.AllClosedCases.Add(item);
                        break;
                    case 5:
                        viewCaseModel.AllTransferedCases.Add(item);
                        break; 
                }
            }

            // If we are here, we have loaded from scratch the viewCaseModel, so see if we had a previous.    This could cause the filters to stick around if you leave and come back to this screen.   Not sure where to clear the session variable.
            if (Session["CURRENTVIEWCASEMODEL"] != null)
            {
                ViewOpenClosedTransferedCasesModel tmpViewCaseModel = (ViewOpenClosedTransferedCasesModel)Session["CURRENTVIEWCASEMODEL"];

                if (page == -1)
                {
                    tmpViewCaseModel.FilterLastName = "";
                    tmpViewCaseModel.FilterFirstName = "";
                    tmpViewCaseModel.FilterDOB = "";
                    tmpViewCaseModel.FilterSSN = "";
                    tmpViewCaseModel.FilterCWLastName = "";
                    tmpViewCaseModel.IsAbuserSearch = false;

                }
                if (tmpViewCaseModel.IsFiltered())
                {
                    // filter all records, shouldn't matter if we are switch page and refilter, 
                    // same filter same records.   If "Filter" button is hit it reloads everything by sending page=0
             
                    viewCaseModel.AllOpenCases = viewCaseModel.AllOpenCases;

                    viewCaseModel.AllClosedCases = viewCaseModel.AllClosedCases;

                    viewCaseModel.AllTransferedCases = viewCaseModel.AllTransferedCases;



                    //Open Cases



                    if (tmpViewCaseModel.IsAbuserSearch == true)
                    {
                        if (tmpViewCaseModel.FilterLastName != null && tmpViewCaseModel.FilterLastName != "")
                        {
                            viewCaseModel.AllOpenCases = viewCaseModel.AllOpenCases.Where(c => c.AbuserLastName != null && c.AbuserLastName != null && c.AbuserLastName.ToLower().Contains(tmpViewCaseModel.FilterLastName.ToLower())).ToList();
                        }


                        if (tmpViewCaseModel.FilterFirstName != null && tmpViewCaseModel.FilterFirstName != "")
                        {
                            viewCaseModel.AllOpenCases = viewCaseModel.AllOpenCases.Where(c => c.AbuserFirstName != null && c.AbuserFirstName != null && c.AbuserFirstName.ToLower().Contains(tmpViewCaseModel.FilterFirstName.ToLower())).ToList();
                        }


                        if (tmpViewCaseModel.FilterSSN != null && tmpViewCaseModel.FilterSSN != "")
                        {
                            viewCaseModel.AllOpenCases = viewCaseModel.AllOpenCases.Where(c => c.AbuserSSN != null && c.AbuserSSN != null && c.AbuserSSN.Contains(tmpViewCaseModel.FilterSSN)).ToList();
                        }
                    }
                    else
                    {

                        if (tmpViewCaseModel.FilterLastName != null && tmpViewCaseModel.FilterLastName != "")
                        {
                            viewCaseModel.AllOpenCases = viewCaseModel.AllOpenCases.Where(c => c.LastName != null && c.LastName != null && c.LastName.ToLower().Contains(tmpViewCaseModel.FilterLastName.ToLower())).ToList();
                        }


                        if (tmpViewCaseModel.FilterFirstName != null && tmpViewCaseModel.FilterFirstName != "")
                        {
                            viewCaseModel.AllOpenCases = viewCaseModel.AllOpenCases.Where(c => c.FirstName != null && c.FirstName != null && c.FirstName.ToLower().Contains(tmpViewCaseModel.FilterFirstName.ToLower())).ToList();
                        }


                        if (tmpViewCaseModel.FilterSSN != null && tmpViewCaseModel.FilterSSN != "")
                        {
                            viewCaseModel.AllOpenCases = viewCaseModel.AllOpenCases.Where(c => c.SSN != null && c.SSN != null && c.SSN.Contains(tmpViewCaseModel.FilterSSN)).ToList();
                        }

                    }



                    if (tmpViewCaseModel.FilterCWLastName != null && tmpViewCaseModel.FilterCWLastName != "")
                    {
                        viewCaseModel.AllOpenCases = viewCaseModel.AllOpenCases.Where(c => c.CaseWorkerNameOnly != null && c.CaseWorkerNameOnly != null && c.CaseWorkerNameOnly.ToLower().Contains(tmpViewCaseModel.FilterCWLastName.ToLower())).ToList();
                    }




                    //Closed Cases

                    if (tmpViewCaseModel.IsAbuserSearch == true)
                    {
                        if (tmpViewCaseModel.FilterLastName != null && tmpViewCaseModel.FilterLastName != "")
                        {
                            viewCaseModel.AllClosedCases = viewCaseModel.AllClosedCases.Where(c => c.AbuserLastName != null && c.AbuserLastName != null && c.AbuserLastName.ToLower().Contains(tmpViewCaseModel.FilterLastName.ToLower())).ToList();
                        }


                        if (tmpViewCaseModel.FilterFirstName != null && tmpViewCaseModel.FilterFirstName != "")
                        {
                            viewCaseModel.AllClosedCases = viewCaseModel.AllClosedCases.Where(c => c.AbuserFirstName != null && c.AbuserFirstName != null && c.AbuserFirstName.ToLower().Contains(tmpViewCaseModel.FilterFirstName.ToLower())).ToList();
                        }


                        if (tmpViewCaseModel.FilterSSN != null && tmpViewCaseModel.FilterSSN != "")
                        {
                            viewCaseModel.AllClosedCases = viewCaseModel.AllClosedCases.Where(c => c.AbuserSSN != null && c.AbuserSSN != null && c.AbuserSSN.Contains(tmpViewCaseModel.FilterSSN)).ToList();
                        }
                    }
                    else
                    {

                        if (tmpViewCaseModel.FilterLastName != null && tmpViewCaseModel.FilterLastName != "")
                        {
                            viewCaseModel.AllClosedCases = viewCaseModel.AllClosedCases.Where(c => c.LastName != null && c.LastName != null && c.LastName.ToLower().Contains(tmpViewCaseModel.FilterLastName.ToLower())).ToList();
                        }


                        if (tmpViewCaseModel.FilterFirstName != null && tmpViewCaseModel.FilterFirstName != "")
                        {
                            viewCaseModel.AllClosedCases = viewCaseModel.AllClosedCases.Where(c => c.FirstName != null && c.FirstName != null && c.FirstName.ToLower().Contains(tmpViewCaseModel.FilterFirstName.ToLower())).ToList();
                        }


                        if (tmpViewCaseModel.FilterSSN != null && tmpViewCaseModel.FilterSSN != "")
                        {
                            viewCaseModel.AllClosedCases = viewCaseModel.AllClosedCases.Where(c => c.SSN != null && c.SSN != null && c.SSN.Contains(tmpViewCaseModel.FilterSSN)).ToList();
                        }

                    }



                    if (tmpViewCaseModel.FilterCWLastName != null && tmpViewCaseModel.FilterCWLastName != "")
                    {
                        viewCaseModel.AllClosedCases = viewCaseModel.AllClosedCases.Where(c => c.CaseWorkerNameOnly != null && c.CaseWorkerNameOnly != null && c.CaseWorkerNameOnly.ToLower().Contains(tmpViewCaseModel.FilterCWLastName.ToLower())).ToList();
                    }

                    //Transferred 


                    if (tmpViewCaseModel.IsAbuserSearch == true)
                    {
                        if (tmpViewCaseModel.FilterLastName != null && tmpViewCaseModel.FilterLastName != "")
                        {
                            viewCaseModel.AllTransferedCases = viewCaseModel.AllTransferedCases.Where(c => c.AbuserLastName != null && c.AbuserLastName != null && c.AbuserLastName.ToLower().Contains(tmpViewCaseModel.FilterLastName.ToLower())).ToList();
                        }


                        if (tmpViewCaseModel.FilterFirstName != null && tmpViewCaseModel.FilterFirstName != "")
                        {
                            viewCaseModel.AllTransferedCases = viewCaseModel.AllTransferedCases.Where(c => c.AbuserFirstName != null && c.AbuserFirstName != null && c.AbuserFirstName.ToLower().Contains(tmpViewCaseModel.FilterFirstName.ToLower())).ToList();
                        }


                        if (tmpViewCaseModel.FilterSSN != null && tmpViewCaseModel.FilterSSN != "")
                        {
                            viewCaseModel.AllTransferedCases = viewCaseModel.AllTransferedCases.Where(c => c.AbuserSSN != null && c.AbuserSSN != null && c.AbuserSSN.Contains(tmpViewCaseModel.FilterSSN)).ToList();
                        }
                    }
                    else
                    {

                        if (tmpViewCaseModel.FilterLastName != null && tmpViewCaseModel.FilterLastName != "")
                        {
                            viewCaseModel.AllTransferedCases = viewCaseModel.AllTransferedCases.Where(c => c.LastName != null && c.LastName != null && c.LastName.ToLower().Contains(tmpViewCaseModel.FilterLastName.ToLower())).ToList();
                        }


                        if (tmpViewCaseModel.FilterFirstName != null && tmpViewCaseModel.FilterFirstName != "")
                        {
                            viewCaseModel.AllTransferedCases = viewCaseModel.AllTransferedCases.Where(c => c.FirstName != null && c.FirstName != null && c.FirstName.ToLower().Contains(tmpViewCaseModel.FilterFirstName.ToLower())).ToList();
                        }


                        if (tmpViewCaseModel.FilterSSN != null && tmpViewCaseModel.FilterSSN != "")
                        {
                            viewCaseModel.AllTransferedCases = viewCaseModel.AllTransferedCases.Where(c => c.SSN != null && c.SSN != null && c.SSN.Contains(tmpViewCaseModel.FilterSSN)).ToList();
                        }

                    }



                    if (tmpViewCaseModel.FilterCWLastName != null && tmpViewCaseModel.FilterCWLastName != "")
                    {
                        viewCaseModel.AllTransferedCases = viewCaseModel.AllTransferedCases.Where(c => c.CaseWorkerNameOnly != null && c.CaseWorkerNameOnly != null && c.CaseWorkerNameOnly.ToLower().Contains(tmpViewCaseModel.FilterCWLastName.ToLower())).ToList();
                    }


                    if (tmpViewCaseModel.IsAbuserSearch == false)
                    {

                        try
                        {
                            if (tmpViewCaseModel.FilterDOB.Length > 0)
                            {
                                DateTime dobToDate = Convert.ToDateTime(tmpViewCaseModel.FilterDOB);
                                viewCaseModel.AllOpenCases = viewCaseModel.AllOpenCases.Where(c => c.DOB == dobToDate).ToList();
                                viewCaseModel.AllTransferedCases = viewCaseModel.AllTransferedCases.Where(c => c.DOB == dobToDate).ToList();
                                viewCaseModel.AllClosedCases = viewCaseModel.AllClosedCases.Where(c => c.DOB == dobToDate).ToList();
                            }
                        }
                        catch { }

                    }

                    else

                    {
                        try
                        {
                            if (tmpViewCaseModel.FilterDOB.Length > 0)
                            {
                                DateTime dobToDate = Convert.ToDateTime(tmpViewCaseModel.FilterDOB);
                                viewCaseModel.AllOpenCases = viewCaseModel.AllOpenCases.Where(c => c.AbuserDOB == dobToDate).ToList();
                                viewCaseModel.AllTransferedCases = viewCaseModel.AllTransferedCases.Where(c => c.AbuserDOB == dobToDate).ToList();
                                viewCaseModel.AllClosedCases = viewCaseModel.AllClosedCases.Where(c => c.AbuserDOB == dobToDate).ToList();
                            }
                        }
                        catch { }


                    }


                    viewCaseModel.FilterFirstName = tmpViewCaseModel.FilterFirstName;
                    viewCaseModel.FilterLastName = tmpViewCaseModel.FilterLastName;
                    viewCaseModel.FilterDOB = tmpViewCaseModel.FilterDOB;
                    viewCaseModel.FilterSSN = tmpViewCaseModel.FilterSSN;
                    viewCaseModel.FilterCWLastName = tmpViewCaseModel.FilterCWLastName;
                    viewCaseModel.IsAbuserSearch = tmpViewCaseModel.IsAbuserSearch;
                }
            }

            viewCaseModel.OpenCases = viewCaseModel.GetOpenCasePageData(1, 10);
            viewCaseModel.ClosedCases = viewCaseModel.GetClosedCasePageData(1, 10);
            viewCaseModel.TransferedCases = viewCaseModel.GetTransferedCasePageData(1, 10);

            Session["CURRENTVIEWCASEMODEL"] = viewCaseModel;

            return View(viewCaseModel);
        }


        public string CleanCaseworker(string cw)
        {
            string caseworker = cw.Replace("ILLINOIS\\", "");
            caseworker = caseworker.Replace("EXTERNAL\\", "");
            caseworker = caseworker.Replace(".", " ");
            caseworker = caseworker.Replace("2", "");

            return caseworker;

        }
        #endregion

        #region List Client Cases


        public ActionResult ListCases(int Id)
        {
            viewClientCMS viewclient = CMSService.GetviewClientCMS(Id);

            viewclient.listofintakes = CMSService.ListOfIntakes(Id).ToList();

            viewclient.ListofClientCases = CMSService.ListofClientCases(Id).ToList();

            viewclient.Id = Id;

            return View(viewclient);
        }

        #endregion

        #region List Of Intakes Assigned to Agency

        public ActionResult ListOfAssignedIntakesToAgency()
        {
            viewIntake viewintake = new viewIntake();
            viewintake.caselookup = getcaselookup(username, ViewBag.ContractId);

            //if (viewintake.caselookup.listofcontracts != null && viewintake.caselookup.listofcontracts.Any() && viewintake.caselookup.listofcontracts.Count == 1)
            //{
            //    viewintake.ContractId = viewintake.caselookup.listofcontracts.FirstOrDefault().Id == 0 ? 0 : viewintake.caselookup.listofcontracts.FirstOrDefault().Id;
            //    viewintake.ContractName = (from contract in viewintake.caselookup.listofcontracts where contract.Id == viewintake.ContractId select contract.ContractName).FirstOrDefault();
            //}

            viewintake.listofintakes = new List<viewIntake>();

            if (User.IsInRole("CMS_IDOAStaff"))
            {
                //contractids = new List<int>();
                viewintake.listofintakes = CMSService.ListAllSubmittedIntakes(contractids).ToList();
            }
            else
            {
                viewintake.listofintakes = CMSService.ListAllSubmittedIntakes(contractids).ToList();
            }

            return View(viewintake);
        }

        #endregion

        #region Assign Intake

        [HttpPost]
        public virtual ActionResult AssignIntake(int Id, int CaseWorkerId)
        {
            viewIntake viewintake = CMSService.GetIntake(Id);

            viewintake.CaseWorkerId = CaseWorkerId;

            viewintake.viewClient = CMSService.GetviewClient(Convert.ToInt32(viewintake.TempClientId));
            //viewintake.viewClient.CreatedBy = viewintake.UserCreated;
            //viewintake.viewClient.CreatedDateTime = DateTime.Now;
            viewintake.StatusDescription = CaseStatus.Assigned.ToString();
            //TestService.TransferIntake(viewintake);
            viewintake.mode = "assign";
            int intakeid = CMSService.SaveIntake(viewintake, true);

            return RedirectToAction("ListAllIntakes", "Intake", new { UserName = username });
        }

        #endregion

        #region Assign Case

        [HttpPost]
        public virtual ActionResult AssignCase(int Id, int CaseworkerId)
        {

           CMSService.AssignCaseworker(Id, CaseworkerId);

           return RedirectToAction("ListOfCasesInAgency", "Case");
           
        }

        #endregion

        #region List Of Intakes Assigned to Caseworker

        public ActionResult ListOfAssignedIntakesToCaseWorker(int ContractId, string UserName)
        {
            viewIntake viewintake = new viewIntake();
            //viewintake.listofintakes = CMSService.ListAllSubmittedIntakes(ContractId).ToList();
            viewintake.listofintakes = CMSService.ListofAssignedIntakes(ContractId, UserName).ToList();
            return View(viewintake);
        }

        #endregion

        #region Verify Case

        public ActionResult VerifyCase(int Id, int ClientId, string FirstName, string LastName)
        {
            viewIntake viewintake = new viewIntake();
            //viewintake.listofintakes = TestService.ListAllAssignedIntakes(ContractId).ToList();
            viewintake.Id = Id;
            viewintake.viewClient = new viewClient();
            viewintake.viewClient.Id = ClientId;
            viewintake.viewClient.FirstName = FirstName;
            viewintake.viewClient.LastName = LastName;
            return View(viewintake);
        }

        #endregion

        #region Intake

        [HttpPost]
        public ActionResult UpdateIntakeReportType(int Id, int IntakeReportTypeId, int IsTimelineChanges)
        {
            CMSService.UpdateIntakeWithIntakeTypeId(Id, IntakeReportTypeId, IsTimelineChanges);

            int CaseheaderId = CMSService.GetCaseHeaderIdByIntake(Id);

            var redirectUrl = new UrlHelper(Request.RequestContext).Action("ManageCase", "Case", new { CaseheaderId = CaseheaderId });

            return Json(new { Url = redirectUrl }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Assessment Preparation

        //[HttpGet]
        //public ActionResult EditAssessmentPreparation(int Id, int CaseheaderId, int? IntakeId, int PrepId)
        //{

        //    viewClientCMS viewclient = CMSService.GetviewClientCMS(Id);


        //    if (PrepId != 0)
        //    {
        //        viewclient.PrepForm = CMSService.GetAssessmentForm(PrepId);
        //    }

        //    else
        //    {
        //        viewclient.PrepForm = new AssessmentPreparationModel();
        //        //viewclient.PrepForm = CMSService.RetrieveAssessmentForm((int)IntakeId);
        //        //viewclient.PrepForm.AssessmentPreparation.person = person;
        //        //viewclient.PrepForm.Abusers = CMSService.GetListDHSDRSAbuser((int)IntakeId).ToList();
        //        viewclient.PrepForm.Allegations = codeTable.ListOfAbuseTypes;
        //        viewclient.PrepForm.Abusers = CMSService.ListOfAbusersByClient(Id).ToList();

        //        foreach (var abuser in viewclient.PrepForm.Abusers)
        //        {
        //            abuser.ListOfAllegedAbuserRelationship = CMSService.ListOfAllegedAbuserRelationShip().ToList();
        //        }
        //        int? CaseworkerId = CMSService.GetAssignedCaseworker_Caseheader(CaseheaderId);
        //        viewclient.PrepForm.AssignedCaseWorker = CaseworkerId == null ? null : CMSService.GetUserByID((int)CaseworkerId).DisplayName;
        //        viewclient.PrepForm.DateCreated = DateTime.Now;
        //    }

        //    viewclient.PrepForm.IntakeId = (int)IntakeId;
        //    viewclient.PrepForm.IntakeDate = CMSService.GetIntakeDateById((int)IntakeId);

        //    viewclient.PrepForm.ClientId = Id;
        //    viewclient.PrepForm.IntakeId = (int)IntakeId;

        //    viewclient.PrepForm.ListMCOs = CMSService.ListOfMCOs().ToList();


        //    using (var eCCPISSrvcClient = new ECCPISSrvcClient())
        //    {
        //        var clients = eCCPISSrvcClient.GetClientsbySSN(viewclient.Person.SSN);
        //        var client = (from c in clients orderby c.LastUpdDt descending select c).Take(1).FirstOrDefault();

        //        if (client != null)
        //        {
        //            viewclient.PrepForm.viewPSS.eCCPIS.ClientID = client.ClientID;
        //            viewclient.PrepForm.viewPSS.eCCPIS.LastName = client.LastName;
        //            viewclient.PrepForm.viewPSS.eCCPIS.FirstName = client.FirstName;
        //            viewclient.PrepForm.viewPSS.eCCPIS.MiddleInitial = client.MiddleInitial;
        //            viewclient.PrepForm.viewPSS.eCCPIS.DoB = client.DoB;
        //            viewclient.PrepForm.viewPSS.eCCPIS.Sex = client.Sex == "1" ? "Male" : "Female";
        //            viewclient.PrepForm.viewPSS.eCCPIS.Race = client.RaceCode;


        //            var assessments = eCCPISSrvcClient.GetAssessments(null, client.ClientID, null);
        //            var assessment = (from a in assessments orderby a.LastUpdDt descending select a).Take(1).FirstOrDefault();

        //            if (assessment != null)
        //            {
        //                viewclient.PrepForm.viewPSS.eCCPIS.ClientCCUID = assessment.ClientCCUID;
        //                viewclient.PrepForm.viewPSS.eCCPIS.EligDeterDt = assessment.EligDeterDt;
        //                viewclient.PrepForm.viewPSS.eCCPIS.IniServDt = assessment.IniServDt;
        //                viewclient.PrepForm.viewPSS.eCCPIS.NextAssessDt = assessment.NextAssessDt;
        //                viewclient.PrepForm.viewPSS.eCCPIS.IsFound = true;
        //            }
        //            else
        //            {
        //                viewclient.PrepForm.viewPSS.eCCPIS.IsFound = false;
        //            }
        //        }

        //    }

        //    using (var hFSReceipientsSrvcClient = new HFSReceipientsSrvcClient())
        //    {

        //        #region HFS Recipient

        //        var receipients = hFSReceipientsSrvcClient.GetRecipientsbySSNorRIN(viewclient.Person.SSN, null);
        //        var receipient = (from a in receipients orderby a.CreatedDate descending select a).Take(1).FirstOrDefault();

        //        if (receipient != null)
        //        {
        //            viewclient.PrepForm.viewPSS.HFSRecipient.ID_NUM = receipient.ID_NUM;
        //            viewclient.PrepForm.viewPSS.HFSRecipient.LAST_NAME = receipient.LAST_NAME;
        //            viewclient.PrepForm.viewPSS.HFSRecipient.FST_NAME = receipient.FST_NAME;
        //            viewclient.PrepForm.viewPSS.HFSRecipient.BRTH_DTE = receipient.BRTH_DTE;
        //            viewclient.PrepForm.viewPSS.HFSRecipient.SEX_CD = receipient.SEX_CD == "M" ? "Male" : "Female";
        //            viewclient.PrepForm.viewPSS.HFSRecipient.DUAL_MEDICAID_MEDICARE_IND = receipient.DUAL_MEDICAID_MEDICARE_IND;
        //            viewclient.PrepForm.viewPSS.HFSRecipient.BEGDTE1 = receipient.BEG_DTE1;
        //            viewclient.PrepForm.viewPSS.HFSRecipient.ENDDTE1 = receipient.END_DTE1;
        //            viewclient.PrepForm.viewPSS.HFSRecipient.FAC_CODE = receipient.FAC_CODE1;
        //            viewclient.PrepForm.viewPSS.HFSRecipient.IsFound = true;


        //            #region MCO Information

        //            using (var eCCPISSrvcClient = new ECCPISSrvcClient())
        //            {
        //                if (receipient.FAC_CODE1 != null && receipient.FAC_CODE1 != "000000")
        //                {
        //                    String FName = eCCPISSrvcClient.GetMCOInfoName(receipient.FAC_CODE1.Substring(0, 2)).Trim();

        //                    CMS.Models.Case.MCOInfo mcoinfo = new CMS.Models.Case.MCOInfo();

        //                    mcoinfo.MCOName = receipient.FAC_CODE1 + " - " + FName;
        //                    mcoinfo.BeginDate = receipient.BEG_DTE1;
        //                    mcoinfo.EndDate = receipient.END_DTE1;

        //                    viewclient.PrepForm.viewPSS.ListMCOInfo.Add(mcoinfo);
        //                }

        //                if (receipient.FAC_CODE2 != null && receipient.FAC_CODE2 != "000000")
        //                {
        //                    String FName = eCCPISSrvcClient.GetMCOInfoName(receipient.FAC_CODE2.Substring(0, 2)).Trim();

        //                    CMS.Models.Case.MCOInfo mcoinfo = new CMS.Models.Case.MCOInfo();

        //                    mcoinfo.MCOName = receipient.FAC_CODE2 + " - " + FName;
        //                    mcoinfo.BeginDate = receipient.BEG_DTE2;
        //                    mcoinfo.EndDate = receipient.END_DTE2;

        //                    viewclient.PrepForm.viewPSS.ListMCOInfo.Add(mcoinfo);
        //                }

        //                if (receipient.FAC_CODE3 != null && receipient.FAC_CODE3 != "000000")
        //                {
        //                    String FName = eCCPISSrvcClient.GetMCOInfoName(receipient.FAC_CODE3.Substring(0, 2)).Trim();

        //                    CMS.Models.Case.MCOInfo mcoinfo = new CMS.Models.Case.MCOInfo();

        //                    mcoinfo.MCOName = receipient.FAC_CODE3 + " - " + FName;
        //                    mcoinfo.BeginDate = receipient.BEG_DTE3;
        //                    mcoinfo.EndDate = receipient.END_DTE3;

        //                    viewclient.PrepForm.viewPSS.ListMCOInfo.Add(mcoinfo);
        //                }

        //                if (receipient.FAC_CODE4 != null && receipient.FAC_CODE4 != "000000")
        //                {
        //                    String FName = eCCPISSrvcClient.GetMCOInfoName(receipient.FAC_CODE4.Substring(0, 2)).Trim();

        //                    CMS.Models.Case.MCOInfo mcoinfo = new CMS.Models.Case.MCOInfo();

        //                    mcoinfo.MCOName = receipient.FAC_CODE4 + " - " + FName;
        //                    mcoinfo.BeginDate = receipient.BEG_DTE4;
        //                    mcoinfo.EndDate = receipient.END_DTE4;

        //                    viewclient.PrepForm.viewPSS.ListMCOInfo.Add(mcoinfo);
        //                }

        //                if (receipient.FAC_CODE5 != null && receipient.FAC_CODE5 != "000000")
        //                {
        //                    String FName = eCCPISSrvcClient.GetMCOInfoName(receipient.FAC_CODE5.Substring(0, 2)).Trim();

        //                    CMS.Models.Case.MCOInfo mcoinfo = new CMS.Models.Case.MCOInfo();

        //                    mcoinfo.MCOName = receipient.FAC_CODE5 + " - " + FName;
        //                    mcoinfo.BeginDate = receipient.BEG_DTE5;
        //                    mcoinfo.EndDate = receipient.END_DTE5;

        //                    viewclient.PrepForm.viewPSS.ListMCOInfo.Add(mcoinfo);
        //                }
        //            }

        //            #endregion
        //        }
        //        else
        //        {
        //            viewclient.PrepForm.viewPSS.HFSRecipient.IsFound = false;
        //        }

        //        #endregion

        //        #region OBRA Waiver

        //        var obrawaivers = hFSReceipientsSrvcClient.GetOBRAbySSNorRIN(viewclient.Person.SSN, null);
        //        //var obrawaivers = hFSReceipientsSrvcClient.GetOBRAbySSNorRIN(null, "077557486");
        //        var obrawaiver = (from a in obrawaivers orderby a.CHANGE_DATE descending select a).Take(1).FirstOrDefault();

        //        if (obrawaiver != null)
        //        {
        //            viewclient.PrepForm.viewPSS.OBRAWaiver.RECIP_NUMBER = obrawaiver.RECIP_NUMBER;
        //            viewclient.PrepForm.viewPSS.OBRAWaiver.MASTERFILE_NAME = obrawaiver.MASTERFILE_NAME;
        //            viewclient.PrepForm.viewPSS.OBRAWaiver.BIRTH_DATE = obrawaiver.BIRTH_DATE;
        //            viewclient.PrepForm.viewPSS.OBRAWaiver.WAIVER = OBRADesc(obrawaiver.WAIVER);
        //            viewclient.PrepForm.viewPSS.OBRAWaiver.BEGIN_DATE = obrawaiver.BEGIN_DATE;
        //            viewclient.PrepForm.viewPSS.OBRAWaiver.END_DATE = obrawaiver.END_DATE;
        //            viewclient.PrepForm.viewPSS.OBRAWaiver.IsFound = true;
        //        }
        //        else
        //        {
        //            viewclient.PrepForm.viewPSS.OBRAWaiver.IsFound = false;
        //        }

        //        #endregion

        //    }


        //    //using (var eCCPISSrvcClient = new ECCPISSrvcClient())
        //    //{
        //    //    var MCOName = eCCPISSrvcClient.GetMCOInfoName("19");



        //    //    //MCO Information  - parameter 19 = receipient.FAC_CODE1.Substring(0,2)
        //    //    //         MCO Name                            MCOName
        //    //    //         Begin Date                          receipient.BEGDTE1 (??? Need to clarify from Jody)
        //    //    //         End Date                            receipient.ENDDTE1 (??? Need to clarify from Jody)
        //    //}

        //    using (var cVRSrvClient = new CVRSrvClient())
        //    {
        //        var expections = cVRSrvClient.GetDeathExceptionBySSN(viewclient.Person.SSN);
        //        var expection = (from a in expections orderby a.CreatedDT descending select a).Take(1).FirstOrDefault();

        //        if (expection == null)
        //        {
        //            var deathrecords = cVRSrvClient.GetDeathRecordsBySSN(viewclient.Person.SSN);
        //            var deathrecord = (from a in deathrecords orderby a.CreatedTS descending select a).Take(1).FirstOrDefault();

        //            if (deathrecord != null)
        //            {
        //                viewclient.PrepForm.viewPSS.DeathRecord.FullName = deathrecord.LastName + "," + deathrecord.FirstName;
        //                viewclient.PrepForm.viewPSS.DeathRecord.BirthDate = deathrecord.BirthDate;
        //                viewclient.PrepForm.viewPSS.DeathRecord.Sex = deathrecord.Sex;
        //                viewclient.PrepForm.viewPSS.DeathRecord.DeathDate = deathrecord.DeathDate;
        //                viewclient.PrepForm.viewPSS.DeathRecord.IsFound = true;
        //            }
        //            else
        //            {
        //                viewclient.PrepForm.viewPSS.DeathRecord.IsFound = false;
        //            }
        //        }
        //        else
        //        {
        //            viewclient.PrepForm.viewPSS.DeathRecord.IsFound = false;
        //        }
        //    }



        //    return View(setIncompleteAssessmentPrepErrors(viewclient));
        //}


        public ActionResult EditAssessmentPreparation(int Id, int IntakeId)
        {
            AssessmentPreparationModel PrepForm = new AssessmentPreparationModel();
            PrepForm = CMSService.GetAssessmentForm(Id);

            if(PrepForm.Id ==  0)
            {

            


                PrepForm.viewIntake = new viewIntake();
                PrepForm.viewIntake = CMSService.GetIntake(IntakeId);
                PrepForm.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)PrepForm.viewIntake.CaseheaderId);



                PrepForm.client = CMSService.GetviewClientCMS(Convert.ToInt32(PrepForm.viewIntake.viewCaseHeader.ClientId));

                
                PrepForm.IsUnsafeForAV = false;
                PrepForm.IsContactUnwelcome = false;
                PrepForm.InvestigationStrategies = false;
                PrepForm.Intake = false;
                PrepForm.DangerToClientOrWorker = false;
                PrepForm.Priority = false;
                PrepForm.Resources = false;



                //PrepForm.caselookup = getcaselookup(username, ViewBag.UserContractId);
                //PrepForm.AssignedCaseWorker = PrepForm.viewIntake.viewCaseHeader.CaseWorker;
                PrepForm.DateCreated = DateTime.Now;

                PrepForm.IntakeId = PrepForm.viewIntake.Id;
                //PrepForm.IntakeDate = PrepForm.viewIntake.DateIntake;

                //PrepForm.ClientId = PrepForm.viewIntake.viewCaseHeader.Client.Id;


                PrepForm.NOI.Abusers = CMSService.ListOfAbusersByIntakeId(Convert.ToInt32(PrepForm.IntakeId)).ToList();
                foreach (var abuser in PrepForm.NOI.Abusers)
                {
                    abuser.ListOfAllegedAbuserRelationship = codeTable.ListOfAllegedAbuserRelationShip;
                };

                PrepForm.AssignedCaseWorker = PrepForm.viewIntake.viewCaseHeader.CaseWorker;

          

            }

            PrepForm.ListMCOs = codeTable.ListOfMCOs;

            //Set the SaveNOI bool based on the status. 
            //If Submitted, NULL, or Opened: do no let them save the Notice unless they make changes
            if(PrepForm.NOI.StatusId == 24 || PrepForm.NOI.StatusId == 29 || PrepForm.NOI.StatusId == null)
            {
                PrepForm.NOI.SaveNOI = false;
            } else
            {
                PrepForm.NOI.SaveNOI = true;
            }
            
            
            return View(setIncompleteAssessmentPrepErrors(PrepForm));
            //return View(PrepForm);
        }

        public ActionResult ParticipantSearch(int ClientId)
        {
            var SSN = CMSService.GetviewClientCMS(ClientId).Person.SSN;

            viewParticipantSearch viewPSS = new viewParticipantSearch();

            using (var eCCPISSrvcClient = new AGE.CMS.Web.ECCPISSvcRef.ECCPISSrvcClient())
            {
                var clients = eCCPISSrvcClient.GetClientsbySSN(SSN);
                var client = (from c in clients orderby c.LastUpdDt descending select c).Take(1).FirstOrDefault();

                if (client != null)
                {
                    viewPSS.eCCPIS.ClientID = client.ClientID;
                    viewPSS.eCCPIS.LastName = client.LastName;
                    viewPSS.eCCPIS.FirstName = client.FirstName;
                    viewPSS.eCCPIS.MiddleInitial = client.MiddleInitial;
                    viewPSS.eCCPIS.DoB = client.DoB;
                    viewPSS.eCCPIS.Sex = client.Sex == "1" ? "Male" : "Female";
                    viewPSS.eCCPIS.Race = client.RaceCode;


                    var assessments = eCCPISSrvcClient.GetAssessments(null, client.ClientID, null);
                    var assessment = (from a in assessments orderby a.EligDeterDt descending select a).Take(1).FirstOrDefault();


                    if (assessment != null)
                    {
                        viewPSS.eCCPIS.ClientCCUID = assessment.ClientCCUID;
                        viewPSS.eCCPIS.EligDeterDt = assessment.EligDeterDt;
                        viewPSS.eCCPIS.IniServDt = assessment.IniServDt;
                        viewPSS.eCCPIS.NextAssessDt = assessment.NextAssessDt;
                        viewPSS.eCCPIS.IsFound = true;
                        var clientservices = eCCPISSrvcClient.GetClientServiceByAssessmentID(assessment.AssessID.ToString());
                        viewPSS.eCCPIS.ClientServices = new List<vieweCCPISClientServices>();
                        foreach (var service in clientservices)
                        {
                            vieweCCPISClientServices ser = new vieweCCPISClientServices();
                            var et = "09/09/9999";
                            ser.Action = service.Action;
                            ser.CCUContNum = service.CCUContNum + " - " + eCCPISSrvcClient.GetContractsByContNum(service.CCUContNum).FirstOrDefault().ContName;
                            ser.EndDt = service.EndDt == null || service.EndDt == Convert.ToDateTime(et) ? "None" : service.EndDt.ToShortDateString();
                            ser.ProvContNum = service.ProvContNum + " - " + eCCPISSrvcClient.GetContractsByContNum(service.ProvContNum).FirstOrDefault().ContName;
                            ser.ServiceCode = service.ServiceCode + " - " + eCCPISSrvcClient.GetServiceCodeDescription(service.ServiceCode);
                            ser.ServMaxUnits = service.ServMaxUnits;
                            ser.StartDt = service.StartDt == null || service.StartDt == Convert.ToDateTime(et) ? "None" : service.StartDt.ToShortDateString();

                            viewPSS.eCCPIS.ClientServices.Add(ser);

                        }
                    }
                    else
                    {
                        viewPSS.eCCPIS.IsFound = false;
                    }
                }

            }

            using (var hFSReceipientsSrvcClient = new AGE.CMS.Web.HFSReceipientsSvcRef.HFSReceipientsSrvcClient())
            {

                #region HFS Recipient

                var receipients = hFSReceipientsSrvcClient.GetRecipientsbySSNorRIN(SSN, null);
                var receipient = (from a in receipients orderby a.CreatedDate descending select a).Take(1).FirstOrDefault();

                if (receipient != null)
                {
                    viewPSS.HFSRecipient.ID_NUM = receipient.ID_NUM;
                    viewPSS.HFSRecipient.LAST_NAME = receipient.LAST_NAME;
                    viewPSS.HFSRecipient.FST_NAME = receipient.FST_NAME;
                    viewPSS.HFSRecipient.BRTH_DTE = receipient.BRTH_DTE;
                    viewPSS.HFSRecipient.SEX_CD = receipient.SEX_CD == "M" ? "Male" : "Female";
                    viewPSS.HFSRecipient.DUAL_MEDICAID_MEDICARE_IND = receipient.DUAL_MEDICAID_MEDICARE_IND;
                    viewPSS.HFSRecipient.BEGDTE1 = receipient.BEG_DTE1;
                    viewPSS.HFSRecipient.ENDDTE1 = receipient.END_DTE1;
                    viewPSS.HFSRecipient.FAC_CODE = receipient.FAC_CODE1;
                    viewPSS.HFSRecipient.IsFound = true;


                    #region MCO Information

                    using (var eCCPISSrvcClient = new AGE.CMS.Web.ECCPISSvcRef.ECCPISSrvcClient())
                    {
                        if (receipient.FAC_CODE1 != null && receipient.FAC_CODE1 != "000000")
                        {
                            String FName = eCCPISSrvcClient.GetMCOInfoName(receipient.FAC_CODE1.Substring(0, 2)).Trim();

                            MCOInfo mcoinfo = new MCOInfo();

                            mcoinfo.MCOName = receipient.FAC_CODE1 + " - " + FName;
                            mcoinfo.BeginDate = receipient.BEG_DTE1;
                            mcoinfo.EndDate = receipient.END_DTE1;

                            viewPSS.ListMCOInfo.Add(mcoinfo);
                        }

                        if (receipient.FAC_CODE2 != null && receipient.FAC_CODE2 != "000000")
                        {
                            String FName = eCCPISSrvcClient.GetMCOInfoName(receipient.FAC_CODE2.Substring(0, 2)).Trim();

                            MCOInfo mcoinfo = new MCOInfo();

                            mcoinfo.MCOName = receipient.FAC_CODE2 + " - " + FName;
                            mcoinfo.BeginDate = receipient.BEG_DTE2;
                            mcoinfo.EndDate = receipient.END_DTE2;

                            viewPSS.ListMCOInfo.Add(mcoinfo);
                        }

                        if (receipient.FAC_CODE3 != null && receipient.FAC_CODE3 != "000000")
                        {
                            String FName = eCCPISSrvcClient.GetMCOInfoName(receipient.FAC_CODE3.Substring(0, 2)).Trim();

                            MCOInfo mcoinfo = new MCOInfo();

                            mcoinfo.MCOName = receipient.FAC_CODE3 + " - " + FName;
                            mcoinfo.BeginDate = receipient.BEG_DTE3;
                            mcoinfo.EndDate = receipient.END_DTE3;

                            viewPSS.ListMCOInfo.Add(mcoinfo);
                        }

                        if (receipient.FAC_CODE4 != null && receipient.FAC_CODE4 != "000000")
                        {
                            String FName = eCCPISSrvcClient.GetMCOInfoName(receipient.FAC_CODE4.Substring(0, 2)).Trim();

                            MCOInfo mcoinfo = new MCOInfo();

                            mcoinfo.MCOName = receipient.FAC_CODE4 + " - " + FName;
                            mcoinfo.BeginDate = receipient.BEG_DTE4;
                            mcoinfo.EndDate = receipient.END_DTE4;

                            viewPSS.ListMCOInfo.Add(mcoinfo);
                        }

                        if (receipient.FAC_CODE5 != null && receipient.FAC_CODE5 != "000000")
                        {
                            String FName = eCCPISSrvcClient.GetMCOInfoName(receipient.FAC_CODE5.Substring(0, 2)).Trim();

                            MCOInfo mcoinfo = new MCOInfo();

                            mcoinfo.MCOName = receipient.FAC_CODE5 + " - " + FName;
                            mcoinfo.BeginDate = receipient.BEG_DTE5;
                            mcoinfo.EndDate = receipient.END_DTE5;

                            viewPSS.ListMCOInfo.Add(mcoinfo);
                        }
                    }

                    #endregion
                }
                else
                {
                    viewPSS.HFSRecipient.IsFound = false;
                }

                #endregion

                #region OBRAWaiver

                var obrawaivers = hFSReceipientsSrvcClient.GetOBRAbySSNorRIN(SSN, null);
                //var obrawaivers = hFSReceipientsSrvcClient.GetOBRAbySSNorRIN(null, "077557486");
                var obrawaiver = (from a in obrawaivers orderby a.CHANGE_DATE descending select a).Take(1).FirstOrDefault();

                if (obrawaiver != null)
                {
                    viewPSS.OBRAWaiver.RECIP_NUMBER = obrawaiver.RECIP_NUMBER;
                    viewPSS.OBRAWaiver.MASTERFILE_NAME = obrawaiver.MASTERFILE_NAME;
                    viewPSS.OBRAWaiver.BIRTH_DATE = obrawaiver.BIRTH_DATE;
                    viewPSS.OBRAWaiver.WAIVER = OBRADesc(obrawaiver.WAIVER);
                    viewPSS.OBRAWaiver.BEGIN_DATE = obrawaiver.BEGIN_DATE;
                    viewPSS.OBRAWaiver.END_DATE = obrawaiver.END_DATE;
                    viewPSS.OBRAWaiver.IsFound = true;
                }
                else
                {
                    viewPSS.OBRAWaiver.IsFound = false;
                }

                #endregion

            }


            //using (var eCCPISSrvcClient = new ECCPISSrvcClient())
            //{
            //    var MCOName = eCCPISSrvcClient.GetMCOInfoName("19");



            //    //MCO Information  - parameter 19 = receipient.FAC_CODE1.Substring(0,2)
            //    //         MCO Name                            MCOName
            //    //         Begin Date                          receipient.BEGDTE1 (??? Need to clarify from Jody)
            //    //         End Date                            receipient.ENDDTE1 (??? Need to clarify from Jody)
            //}

            using (var cVRSrvClient = new AGE.CMS.Web.CVRSvcRef.CVRSrvClient())
            {
                var expections = cVRSrvClient.GetDeathExceptionBySSN(SSN);
                var expection = (from a in expections orderby a.CreatedDT descending select a).Take(1).FirstOrDefault();

                if (expection == null)
                {
                    var deathrecords = cVRSrvClient.GetDeathRecordsBySSN(SSN);
                    var deathrecord = (from a in deathrecords orderby a.CreatedTS descending select a).Take(1).FirstOrDefault();

                    if (deathrecord != null)
                    {
                        viewPSS.DeathRecord.FullName = deathrecord.LastName + "," + deathrecord.FirstName;
                        viewPSS.DeathRecord.BirthDate = deathrecord.BirthDate;
                        viewPSS.DeathRecord.Sex = deathrecord.Sex;
                        viewPSS.DeathRecord.DeathDate = deathrecord.DeathDate;
                        viewPSS.DeathRecord.IsFound = true;
                    }
                    else
                    {
                        viewPSS.DeathRecord.IsFound = false;
                    }
                }
                else
                {
                    viewPSS.DeathRecord.IsFound = false;
                }
            }

            return PartialView(viewPSS);
        }

        [HttpPost]
        public ActionResult SaveAssessmentPreparation(AssessmentPreparationModel PrepForm)
        {

            try
            {
                
                    PrepForm.StatusDescription = CaseStatus.Open.ToString();              
                

                if (PrepForm.IsIDoA_OCCS_CCP_Client == null || PrepForm.IsDHSorDRSclient == null || PrepForm.IsDHS_DDD_Client == null ||
                    PrepForm.IsMCO == null || PrepForm.IsDSCC_Client == null || PrepForm.DiscussedWithReportTaker == null || PrepForm.DiscussedWithReporter == null ||
                    PrepForm.CollateralsContacted == null || PrepForm.PreviousReportsReceived == null)
                {
                    PrepForm.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Answer Yes/No or Unknown/NA if applicable");
                }

                if (PrepForm.IsMCO == "y" && PrepForm.MCO == null)
                {
                    PrepForm.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select MCO");
                }
                if (PrepForm.DiscussedWithReportTaker == "n" && PrepForm.ReportTakerIsCaseWorker == null)
                {
                    PrepForm.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select whether Caseworker and Report Taker are same person");
                }
                if (PrepForm.DateContactedReporter == null && (PrepForm.IsUnsafeForAV == false && PrepForm.IsContactUnwelcome == false))
                {
                    PrepForm.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select Exceptions for entering date");
                }

                if (User.IsInRole("CMS_Supervisor") || User.IsInRole("CMS_IDOAStaff"))
                {
                    if (PrepForm.ConsultedWithSupervisor == null)
                    {
                        PrepForm.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select whether Caseworker consulted with Supervisor or not");
                    }
                    if (PrepForm.ConsultedWithSupervisor == "n" && PrepForm.Explain == null)
                    {
                        PrepForm.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please explain why Caseworker didn't consult with Supervisor");
                    }
                    if (PrepForm.Intake == false && PrepForm.InvestigationStrategies == false && PrepForm.DangerToClientOrWorker == false
                        && PrepForm.Priority == false && PrepForm.Resources == false)
                    {
                        PrepForm.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "You have to discuss all the topics mentioned at the end");
                    }
                }
                PrepForm.UserCreated = username;
                PrepForm.UserUpdated = username;

                AssPrepIds ListIds = CMSService.SaveAssessmentForm(PrepForm);
                PrepForm.CaseheaderId = ListIds.CHID;

                
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
                throw;
            }

            return RedirectToAction("ManageCase", "Case", new { Caseheaderid = PrepForm.CaseheaderId });
        }

        [HttpPost]
        public JsonResult SaveAssessmentPreparationAjax(AssessmentPreparationModel PrepForm)
        {
            AssPrepIds ListIds = new AssPrepIds();
            try
            {
                    PrepForm.StatusDescription = CaseStatus.Open.ToString();


                if (PrepForm.IsIDoA_OCCS_CCP_Client == null || PrepForm.IsDHSorDRSclient == null || PrepForm.IsDHS_DDD_Client == null ||
                    PrepForm.IsMCO == null || PrepForm.IsDSCC_Client == null || PrepForm.DiscussedWithReportTaker == null || PrepForm.DiscussedWithReporter == null ||
                    PrepForm.CollateralsContacted == null || PrepForm.PreviousReportsReceived == null)
                {
                    PrepForm.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Answer Yes/No or Unknown/NA if applicable");
                }

                if (PrepForm.IsMCO == "y" && PrepForm.MCO == null)
                {
                    PrepForm.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select MCO");
                }
                if (PrepForm.DiscussedWithReportTaker == "n" && PrepForm.ReportTakerIsCaseWorker == null)
                {
                    PrepForm.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select whether Caseworker and Report Taker are same person");
                }
                if (PrepForm.DateContactedReporter == null && (PrepForm.IsUnsafeForAV == false && PrepForm.IsContactUnwelcome == false))
                {
                    PrepForm.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select Exceptions for entering date");
                }

                if (User.IsInRole("Supervisor") || User.IsInRole("IDOAStaff"))
                {
                    if (PrepForm.ConsultedWithSupervisor == null)
                    {
                        PrepForm.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select whether Caseworker consulted with Supervisor or not");
                    }
                    if (PrepForm.ConsultedWithSupervisor == "n" && PrepForm.Explain == null)
                    {
                        PrepForm.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please explain why Caseworker didn't consult with Supervisor");
                    }
                    if (PrepForm.Intake == false && PrepForm.InvestigationStrategies == false && PrepForm.DangerToClientOrWorker == false
                        && PrepForm.Priority == false && PrepForm.Resources == false)
                    {
                        PrepForm.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "You have to discuss all the topics mentioned at the end");
                    }
                }
                PrepForm.UserCreated = username;
                PrepForm.UserUpdated = username;

                ListIds = CMSService.SaveAssessmentForm(PrepForm);

            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
                throw;
            }


            return Json(ListIds);
            
        }


        public ActionResult ViewAssessementPreperation(int Id, int intakeId)
        {
            AssessmentPreparationModel PrepForm = CMSService.GetAssessmentForm(Id);

            //PrepForm.viewIntake = new viewIntake();
            //PrepForm.viewIntake = CMSService.GetIntake(intakeId);
            //PrepForm.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)PrepForm.viewIntake.CaseheaderId);

            //PrepForm.AssignedCaseWorker = PrepForm.viewIntake.viewCaseHeader.CaseWorker;

            //PrepForm.IntakeId = PrepForm.viewIntake.Id;
            //PrepForm.IntakeDate = PrepForm.viewIntake.DateIntake;

            //PrepForm.ClientId = PrepForm.viewIntake.viewCaseHeader.Client.Id;

            //PrepForm.ListMCOs = codeTable.ListOfMCOs;

            return View(PrepForm);
        }

        public JsonResult ApproveAssessmentPreparation(AssessmentPreparationModel PrepForm)
        {

            PrepForm.StatusDescription = PrepForm.StatusDescription = CaseStatus.Approved.ToString();
            AssPrepIds getIds = new AssPrepIds();
            getIds = CMSService.SaveAssessmentForm(PrepForm);

            return Json(getIds);
        }

        #region Helper

        protected AssessmentPreparationModel setIncompleteAssessmentPrepErrors(AssessmentPreparationModel PrepForm)
        {
            if (PrepForm.Id > 0)
            {
                if (PrepForm.IsIDoA_OCCS_CCP_Client == null || PrepForm.IsDHSorDRSclient == null || PrepForm.IsDHS_DDD_Client == null ||
                        PrepForm.IsMCO == null || PrepForm.IsDSCC_Client == null || PrepForm.DiscussedWithReportTaker == null || PrepForm.DiscussedWithReporter == null ||
                        PrepForm.CollateralsContacted == null || PrepForm.PreviousReportsReceived == null)
                {
                    PrepForm.InCompleteErrors.ErrorsInAssessmentPrep = true;
                    PrepForm.InCompleteErrors.HasErrorsInRadios = true;
                }

                if (PrepForm.IsMCO == "y" && PrepForm.MCO == null)
                {
                    PrepForm.InCompleteErrors.ErrorsInAssessmentPrep = true;
                    PrepForm.InCompleteErrors.HasErrorsMCO = true;
                }
                if (PrepForm.DiscussedWithReportTaker == "n" && PrepForm.ReportTakerIsCaseWorker == null)
                {
                    PrepForm.InCompleteErrors.ErrorsInAssessmentPrep = true;
                    PrepForm.InCompleteErrors.HasErrorsReportTakerIsCaseWorker = true;
                }
                if (PrepForm.DateContactedReporter == null && (PrepForm.IsUnsafeForAV == false && PrepForm.IsContactUnwelcome == false))
                {
                    PrepForm.InCompleteErrors.ErrorsInAssessmentPrep = true;
                    PrepForm.InCompleteErrors.HasErrorsDateContactedReporter = true;
                }
                if (User.IsInRole("Supervisor") || User.IsInRole("IDOAStaff"))
                {
                    if (PrepForm.ConsultedWithSupervisor == null)
                    {
                        PrepForm.InCompleteErrors.ErrorsInAssessmentPrep = true;
                        PrepForm.InCompleteErrors.HasErrorsConsultedWithSupervisor = true;
                    }

                    if (PrepForm.ConsultedWithSupervisor == "n" && PrepForm.Explain == null)
                    {
                        PrepForm.InCompleteErrors.ErrorsInAssessmentPrep = true;
                        PrepForm.InCompleteErrors.HasErrorsExplain = true;
                    }
                    if (PrepForm.Intake == false && PrepForm.InvestigationStrategies == false && PrepForm.DangerToClientOrWorker == false
                        && PrepForm.Priority == false && PrepForm.Resources == false)
                    {
                        PrepForm.InCompleteErrors.ErrorsInAssessmentPrep = true;
                        PrepForm.InCompleteErrors.HasErrorsInTopics = true;
                    }
                }
            }
            return PrepForm;
        }

        protected string OBRADesc(string WAIVER)
        {
            string strret = WAIVER;
            switch (WAIVER)
            {
                case "A0":
                    strret = "Aging";
                    break;
                case "D0":
                    strret = "DHDD";
                    break;
                case "C0":
                    strret = "DHS";
                    break;
                case "B0":
                    strret = "DHS";
                    break;
                case "H0":
                    strret = "DHS";
                    break;
                case "XM-MCO":
                    strret = "Opt Out";
                    break;
                case "F0":
                    strret = "SLF";
                    break;
                case "AY":
                    strret = "Aging MFP";
                    break;
                case "MY-MH":
                    strret = "MFP";
                    break;
                case "HY-DHS":
                    strret = "MFP";
                    break;
            }
            return strret;
        }

        //public ActionResult ViewAssessementPreperation(int Id, int intakeId)
        //{
        //    AssessmentPreparationModel PrepForm = CMSService.GetAssessmentForm(Id);
        //      PrepForm.Allegations = codeTable.ListOfAbuseTypes;
        //    PrepForm.viewIntake = new viewIntake();
        //    PrepForm.viewIntake = CMSService.GetIntake(intakeId);
        //    PrepForm.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)PrepForm.viewIntake.CaseheaderId);
        //    PrepForm.Abusers = PrepForm.viewIntake.listofabusers.ToList();

        //    foreach (var abuser in PrepForm.Abusers)
        //    {
        //        abuser.ListOfAllegedAbuserRelationship = CMSService.ListOfAllegedAbuserRelationShip().ToList();
        //    }

        //    PrepForm.viewIntake = new viewIntake();
        //    PrepForm.viewIntake = CMSService.GetIntake(intakeId);
        //    PrepForm.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)PrepForm.viewIntake.CaseheaderId);

        //    PrepForm.AssignedCaseWorker = PrepForm.viewIntake.viewCaseHeader.CaseWorker;
        //    PrepForm.DateCreated = DateTime.Now;

        //    PrepForm.IntakeId = PrepForm.viewIntake.Id;
        //    PrepForm.IntakeDate = PrepForm.viewIntake.DateIntake;

        //    PrepForm.ClientId = PrepForm.viewIntake.viewCaseHeader.Client.Id;       

        //    PrepForm.ListMCOs = CMSService.ListOfMCOs().ToList();

        //    return View(setIncompleteAssessmentPrepErrors(PrepForm));
        //}
        #endregion

        #endregion

        #region Notice Of Investigation

        [HttpPost]
        public ActionResult SaveNoticeOfInvestigation(AssessmentPreparationModel PrepForm)
        {

            //PrepForm.NOI.UserCreated = username;
            PrepForm.NOI.UserUpdated = username;
            PrepForm.StatusDescription = CaseStatus.Incomplete.ToString();

            PrepForm.mode = "Notice";

            //CMSService.SaveNoticeOfInvestigation(PrepForm);

           AssPrepIds Idlist = CMSService.SaveAssessmentForm(PrepForm);

            

            return Json(Idlist, JsonRequestBehavior.AllowGet);
        }


 

        [HttpPost]
        public JsonResult SubmitNoticeOfInvestigation(AssessmentPreparationModel PrepForm)
        {

            //PrepForm.UserCreated = username;
            //PrepForm.UserUpdated = username;
            PrepForm.NOI.DateSubmitted = DateTime.Now;
            PrepForm.NOI.UserSubmitted = username;
            PrepForm.NOI.StatusId = 24;
            //if (PrepForm.IsApproveasp == true)
            //{
            //    PrepForm.StatusDescription = CaseStatus.Approved.ToString();
            //}
            //if (PrepForm.Issubmitasp == true)
            //{
            //    PrepForm.StatusDescription = CaseStatus.Submitted.ToString();
            //}
            AssPrepIds aids = CMSService.SaveAssessmentForm(PrepForm);
            aids.IntId = (int)PrepForm.IntakeId;
            int noticeId = (int)aids.NOID;
            //if (PrepForm.IssubmitNotice == true)
            //{

            if (PrepForm.NOI.SaveNOI) { 
            viewExternalContact contact = new viewExternalContact();


                contact = CMSService.GetExternalContacts();
                ////Types
                ////DDD = 1
                ////DRS = 2
                ////DMH = 3
                ////CCP = 4
                ////DSCC = 5
                ////HFS = 6
                ////BCC = 7

                List<string> DDDEmails = contact.Contacts.Where(c => c.TypeId == 1 && c.IsActive == true).Select(c => c.ContactEmail).ToList();
                List<string> DRSEmails = contact.Contacts.Where(c => c.TypeId == 2 && c.IsActive == true).Select(c => c.ContactEmail).ToList();
                List<string> BCCEmails = contact.Contacts.Where(c => c.TypeId == 7 && c.IsActive == true).Select(c => c.ContactEmail).ToList();

                List<MailAddress> EMails = new List<MailAddress>();

                List<int> PreviousIds = CMSService.GetPreviousNOIIds(noticeId);



                string Message = "You have received a Notice Of Investigation (Id: " + noticeId + ") from " + CMSService.GetContract(contractids.FirstOrDefault()).ContractName + ".";


                if (PreviousIds != null && PreviousIds.Any())
                {
                    Message = Message + "This Notice of Investigation was previously submitted with the following Id(s): ";
                    for (int i = 0; i < PreviousIds.Count; i++)
                    {
                        if (i == PreviousIds.Count - 1)
                        {
                            Message = Message + PreviousIds[i] + ".";
                        }
                        else
                        {
                            Message = Message + PreviousIds[i] + ", ";
                        }
                    }
                }


                string Subject;

                if (PrepForm.NOI.IsDDD == true)
                {
                    Subject = "Notice Of Investigation - DDD";
                    foreach (var e in DDDEmails)
                    {
                        EMails.Add(new MailAddress(e, "to"));

                    }
                    foreach (var e in BCCEmails)
                    {
                        EMails.Add(new MailAddress(e, "bcc"));
                    }

                    //EMails.Add(new MailAddress(System.Configuration.ConfigurationManager.AppSettings["_DDDEmail"], "To"));

                    SendEmail(EMails, Message, Subject);
                }
                if (PrepForm.NOI.IsDRS == true)
                {

                    Subject = "Notice Of Investigation - DRS";


                    EMails = new List<MailAddress>();

                    foreach (var e in DRSEmails)
                    {
                        EMails.Add(new MailAddress(e, "to"));
                    }
                    foreach (var e in BCCEmails)
                    {
                        EMails.Add(new MailAddress(e, "bcc"));
                    }

                    //EMails.Add(new MailAddress(System.Configuration.ConfigurationManager.AppSettings["_DRSEmail"], "To"));
                    SendEmail(EMails, Message, Subject);
                }

            

            PrepForm.NoticeStatusDescription = CaseStatus.Submitted.ToString();

            }


                PrepForm.mode = "Notice";
            
            var redirectUrl = new UrlHelper(Request.RequestContext).Action("ManageCase", "Case", new { CaseheaderId = PrepForm.CaseheaderId });
            //int Id = CMSService.SaveNoticeOfInvestigation(PrepForm);
            //SaveNoticeOfInvestigation(PrepForm);
            //CMSService.SubmitSaveNOI(PrepForm.NOI);
            //List<int> Listids = CMSService.SaveAssessmentForm(PrepForm);
            //foreach (var id in Listids)
            //{
            //    PrepForm.Id = id;
            //    break;
            //}

           
            return Json(aids, JsonRequestBehavior.AllowGet);
        }


        #endregion

        #region Case Recording Form

        //[CMSRoleAuthorize(Roles = "IDOAStaff,Supervisor,Caseworker")]

        public ActionResult EditCaseRecording(int Id, int CaseheaderId)
        {

            ViewBag.Success = false;
            ViewBag.Submitted = false;

            CaseRecordingModel CaseRecording = new CaseRecordingModel();

            if (Id != 0 && CaseheaderId != 0)
            {
                CaseRecording = CMSService.GetCaseRecording((int)CaseheaderId);
            }

            else
            {
                CaseRecording = new CaseRecordingModel();

                CaseRecording.helper = new CaseRecordingModel_Helper();
                CaseRecording.helper.Invoices = new List<CaseRecordingInvoice>();
                CaseRecording.helper.Invoices.Add(new CaseRecordingInvoice());

                CaseRecording.helper.DraftCaseRecordings.Add(CaseRecording.helper);

                //viewclient.CaseRecording.listhelper = new List<CaseRecordingModel_Helper>();

                //viewclient.CaseRecording.listhelper.Add(new CaseRecordingModel_Helper());

                //foreach (var helper in viewclient.CaseRecording.listhelper)
                //{
                //    helper.DraftCaseRecordings.Add(new CaseRecordingModel_Helper());
                //}

            }
            CaseRecording.viewCaseHeader = new viewCaseHeader();
            CaseRecording.viewCaseHeader = CMSService.GetCaseHeaderById(CaseheaderId);
            CaseRecording.CaseheaderId = CaseRecording.viewCaseHeader.Id;

            ViewBag.CaseRecordingTypes = GetCaseRecordingTypesddl(false);
            //ViewBag.PaymentTypes = GetPaymentTypesddl();
            ViewBag.SubstantiationTypes = GetSubstantiationTypesddl();

            //Comment added by Satheesh
            ViewBag.FiscalPeriodddl = GetFiscalPeriodddl();
            //Comment ended  
            //ViewBag.Wavierddl = GetWavierddl();
            //ViewBag.Serviceddl = GetServiceddl();
            return View(CaseRecording);
        }

        public PartialViewResult EditCaseRecordingHelper(int Id)
        {
            CaseRecordingModel_Helper helper = new CaseRecordingModel_Helper();

            helper = CMSService.GetCaseRecordingHelper(Id);

            return PartialView(helper);
        }


        public JsonResult AjaxSaveCaseRecording(CaseRecordingModel CaseRecording)
        {

            ViewBag.Submitted = true;
            ViewBag.Success = true;

            //CaseRecording.CaseheaderId = viewclient.CaseheaderId;            
            CaseRecording.UserCreated = username;
            CaseRecording.UserUpdated = username;

            int caseid = 0;
            caseid = CMSService.SaveCaseRecording(CaseRecording, false);

            ViewBag.CaseRecordingTypes = GetCaseRecordingTypesddl(false);
            //ViewBag.PaymentTypes = GetPaymentTypesddl();
            ViewBag.SubstantiationTypes = GetSubstantiationTypesddl();
            //Comment added by Satheesh
            ViewBag.FiscalPeriodddl = GetFiscalPeriodddl();
            //Comment ended    
            //if(CaseRecording.helper.Type)
            var closurecount = 0;
            foreach (var draft in CaseRecording.helper.DraftCaseRecordings)
            {
                if (draft.Type == "Closure At Assessement" && draft.IsSupervisoragrees == true)
                {
                    closurecount++;

                }
            }

            if (closurecount > 0)
            {
                CMSService.CloseCase((int)CaseRecording.CaseheaderId, username);

            }
            //CMSService.CloseCase(CaseRecording.viewCaseHeader.Id,"mounish");
            List<int> data = new List<int> { };
            data.Add(caseid);
            data.Add((int)CaseRecording.CaseheaderId);
            return Json(data);
            //return RedirectToAction("EditCaseRecording", "Case", new { Id = caseid, CaseheaderId = CaseRecording.CaseheaderId });
        }

        public ActionResult SaveCaseRecording(CaseRecordingModel CaseRecording)
        {

            ViewBag.Submitted = true;
            ViewBag.Success = true;

            //CaseRecording.CaseheaderId = viewclient.CaseheaderId;            
            CaseRecording.UserCreated = username;
            CaseRecording.UserUpdated = username;

            int caseid = 0;
            caseid = CMSService.SaveCaseRecording(CaseRecording, false);

            ViewBag.CaseRecordingTypes = GetCaseRecordingTypesddl(false);
            //ViewBag.PaymentTypes = GetPaymentTypesddl();
            ViewBag.SubstantiationTypes = GetSubstantiationTypesddl();
            //Comment added by Satheesh
            ViewBag.FiscalPeriodddl = GetFiscalPeriodddl();
            //Comment ended    
            //if(CaseRecording.helper.Type)
            var closurecount = 0;
            foreach (var draft in CaseRecording.helper.DraftCaseRecordings)
            {
                if (draft.Type == "Closure At Assessement" && draft.IsSupervisoragrees == true)
                {
                    closurecount++;

                }
            }

            if (closurecount > 0)
            {
                CMSService.CloseCase((int)CaseRecording.CaseheaderId, username);

            }
            //CMSService.CloseCase(CaseRecording.viewCaseHeader.Id,"mounish");
            List<int> data = new List<int> { };
            data.Add(caseid);
            data.Add((int)CaseRecording.CaseheaderId);
            if (CaseRecording.MakeSessionLive == true)
            {
                return RedirectToAction("EditCaseRecording", "Case", new { Id = caseid, CaseheaderId = CaseRecording.CaseheaderId });
            }
            else
            {

                return RedirectToAction("ManageCase", "Case", new { CaseheaderId = CaseRecording.CaseheaderId });
            }
        }

        public ActionResult PrintCaseRecording(int Id)
        {
            CaseRecordingModel CaseRecording = new CaseRecordingModel();

            if (Id != 0)
            {
                CaseRecording = CMSService.GetCaseRecordingById(Id);
            }
            CaseRecording.viewCaseHeader = new viewCaseHeader();
            CaseRecording.viewCaseHeader = CMSService.GetCaseHeaderById((int)CaseRecording.CaseheaderId);
            CaseRecording.CaseheaderId = CaseRecording.viewCaseHeader.Id;

            ViewBag.CaseRecordingTypes = GetCaseRecordingTypesddl(true);
            //ViewBag.PaymentTypes = GetPaymentTypesddl();
            ViewBag.SubstantiationTypes = GetSubstantiationTypesddl();

            return View(CaseRecording);
        }


        public ActionResult PrintCaseRecordingPost(FormCollection fc, viewClientCMS viewclientCMS)
        {
            string fromdate = fc["FromDate"].ToString();
            string todate = fc["ToDate"].ToString();
            string select = fc["Select"].ToString();
            string casetype = fc["CaseType"].ToString();

            viewClientCMS viewclient = CMSService.GetviewClientCMS(viewclientCMS.CaseRecording.ClientId);

            viewclient.CaseRecording.IntakeId = viewclientCMS.CaseRecording.IntakeId;

            List<CaseRecordingModel_Helper> lstCRM = CMSService.GetCaseRecordingsByParameter(fromdate, todate, select, casetype, viewclient.CaseRecording.IntakeId).ToList();

            foreach (CaseRecordingModel_Helper CRM in lstCRM)
            {
                if (CRM.CaseStatus.ToUpper().ToString() == "DRAFT")
                {
                    viewclient.CaseRecording.helper.DraftCaseRecordings.Add(CRM);

                }
                else if (CRM.CaseStatus.ToUpper().ToString() == "APPROVED")
                {
                    viewclient.CaseRecording.helper.ApprovedCaseRecordings.Add(CRM);
                }
                else if (CRM.CaseStatus.ToUpper().ToString() == "PEND")
                {
                    viewclient.CaseRecording.helper.PendingCaseRecordings.Add(CRM);
                }
            }
            //CaseRecording.CaseRecordings.Add(new CaseRecordingModel());
            ViewBag.CaseRecordingTypes = GetCaseRecordingTypesddl(true);
            //ViewBag.PaymentTypes = GetPaymentTypesddl();
            ViewBag.SubstantiationTypes = GetSubstantiationTypesddl();
            return View(viewclient);
        }

        #endregion

        #region SuspiciousDeath

        public ActionResult SuspiciousDeath(int Id, int IntakeId)
        {
            viewSuspiciousDeath viewsuspiciousdeath;

            if (Id != 0)
            {
                viewsuspiciousdeath = CMSService.GetSuspiciousDeath(Id);
            }
            else
            {
                viewsuspiciousdeath = new viewSuspiciousDeath();
            }

            viewsuspiciousdeath.viewIntake = new viewIntake();
            viewsuspiciousdeath.viewIntake = CMSService.GetIntake(IntakeId);
            viewsuspiciousdeath.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)viewsuspiciousdeath.viewIntake.CaseheaderId);
            viewsuspiciousdeath.viewIntake.caselookup = getcaselookup(username, ViewBag.UserContractId);

            return View(viewsuspiciousdeath);
        }

        [HttpPost]
        public virtual ActionResult SaveSuspiciousDeath(viewSuspiciousDeath viewsuspiciousdeath)
        {
            viewsuspiciousdeath.UserCreated = username;
            viewsuspiciousdeath.UserUpdated = username;
            viewsuspiciousdeath.StatusDescription = CaseStatus.Incomplete.ToString();


            if (viewsuspiciousdeath.IsAjax == true)
            {
                viewsuspiciousdeath.Id = CMSService.SaveSuspiciousDeath(viewsuspiciousdeath);
                return Json(viewsuspiciousdeath.Id, JsonRequestBehavior.AllowGet);
            }
            else
            {
                viewsuspiciousdeath.CaseheaderId = CMSService.SaveSuspiciousDeath(viewsuspiciousdeath);
                return Redirect(Url.Action("ManageCase", "Case", new { CaseheaderId = viewsuspiciousdeath.CaseheaderId }));
            }
        }

        [HttpPost]
        public virtual ActionResult SubmitSuspiciousDeath(viewSuspiciousDeath viewsuspiciousdeath)
        {
            viewsuspiciousdeath.UserCreated = username;
            viewsuspiciousdeath.UserUpdated = username;
            viewsuspiciousdeath.StatusDescription = CaseStatus.Submitted.ToString();

            viewsuspiciousdeath.Id = CMSService.SaveSuspiciousDeath(viewsuspiciousdeath);
            var redirectUrl = new UrlHelper(Request.RequestContext).Action("ManageCase", "Case", new { CaseheaderId = viewsuspiciousdeath.viewIntake.CaseheaderId });
            var URLandId = new { url = redirectUrl, Id = viewsuspiciousdeath.Id };
            return Json(URLandId, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public virtual ActionResult ApproveSuspiciousDeath(viewSuspiciousDeath viewsuspiciousdeath)
        {
            viewsuspiciousdeath.UserCreated = username;
            viewsuspiciousdeath.UserUpdated = username;
            viewsuspiciousdeath.StatusDescription = CaseStatus.Approved.ToString();

            viewsuspiciousdeath.Id = CMSService.SaveSuspiciousDeath(viewsuspiciousdeath);
            var redirectUrl = new UrlHelper(Request.RequestContext).Action("ManageCase", "Case", new { CaseheaderId = viewsuspiciousdeath.viewIntake.CaseheaderId });
            var URLandId = new { url = redirectUrl, Id = viewsuspiciousdeath.Id };
            return Json(URLandId, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ViewSuspiciousDeath(int Id)
        {
            viewSuspiciousDeath viewsuspiciousdeath = CMSService.GetSuspiciousDeath(Id);

            viewsuspiciousdeath.viewIntake = new viewIntake();
            viewsuspiciousdeath.viewIntake = CMSService.GetIntake((int)viewsuspiciousdeath.IntakeId);
            viewsuspiciousdeath.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)viewsuspiciousdeath.viewIntake.CaseheaderId);
            viewsuspiciousdeath.viewIntake.caselookup = getcaselookup(username, ViewBag.UserContractId);

            return View(viewsuspiciousdeath);
        }

        #endregion

        #region LawEnforcement

        public ActionResult LawEnforcement(int Id, int IntakeId)
        {
            viewLawEnforcement viewlawenforcement;

            if (Id != 0)
            {
                viewlawenforcement = CMSService.GetLawEnforcement(Id);
            }
            else
            {
                viewlawenforcement = new viewLawEnforcement();
                viewlawenforcement.ListOfAbusetypes = codeTable.ListOfAbuseTypes;
            }

            viewlawenforcement.viewIntake = new viewIntake();
            viewlawenforcement.viewIntake = CMSService.GetIntake(IntakeId);
            viewlawenforcement.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)viewlawenforcement.viewIntake.CaseheaderId);
            viewlawenforcement.CaseheaderId = viewlawenforcement.viewIntake.viewCaseHeader.Id;
            viewlawenforcement.viewIntake.caselookup = getcaselookup(username, ViewBag.UserContractId);
            return View(viewlawenforcement);
        }

        [HttpPost]
        public virtual ActionResult SaveLawEnforcement(viewLawEnforcement viewlawenforcement)
        {
            viewlawenforcement.UserCreated = username;
            viewlawenforcement.UserUpdated = username;
            viewlawenforcement.StatusDescription = CaseStatus.Incomplete.ToString();



            if (viewlawenforcement.IsAjax == true)
            {
                viewlawenforcement.Id = CMSService.SaveLawEnforcement(viewlawenforcement);
                return Json(viewlawenforcement.Id, JsonRequestBehavior.AllowGet);
            }
            else
            {
                viewlawenforcement.CaseheaderId = CMSService.SaveLawEnforcement(viewlawenforcement);
                return Redirect(Url.Action("ManageCase", "Case", new { CaseheaderId = viewlawenforcement.CaseheaderId }));
            }
        }
        [HttpPost]
        public virtual ActionResult SubmitLawEnforcement(viewLawEnforcement viewlawenforcement)
        {
            viewlawenforcement.UserCreated = username;
            viewlawenforcement.UserUpdated = username;
            viewlawenforcement.StatusDescription = CaseStatus.Submitted.ToString();

            viewlawenforcement.Id = CMSService.SaveLawEnforcement(viewlawenforcement);
            var redirectUrl = new UrlHelper(Request.RequestContext).Action("ManageCase", "Case", new { CaseheaderId = viewlawenforcement.CaseheaderId });
            var URLandId = new { url = redirectUrl, Id = viewlawenforcement.Id };
            return Json(URLandId, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public virtual ActionResult ApproveLawEnforcement(viewLawEnforcement viewlawenforcement)
        {
            viewlawenforcement.UserCreated = username;
            viewlawenforcement.UserUpdated = username;
            viewlawenforcement.StatusDescription = CaseStatus.Approved.ToString();

            viewlawenforcement.Id = CMSService.SaveLawEnforcement(viewlawenforcement);
            var redirectUrl = new UrlHelper(Request.RequestContext).Action("ManageCase", "Case", new { CaseheaderId = viewlawenforcement.CaseheaderId });
            var URLandId = new { url = redirectUrl, Id = viewlawenforcement.Id };
            return Json(URLandId, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ViewLawEnforcement(int Id)
        {

            viewLawEnforcement viewlawenforcement = CMSService.GetLawEnforcement(Id);

            viewlawenforcement.viewIntake = new viewIntake();
            viewlawenforcement.viewIntake = CMSService.GetIntake((int)viewlawenforcement.IntakeId);
            viewlawenforcement.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)viewlawenforcement.viewIntake.CaseheaderId);
            viewlawenforcement.viewIntake.caselookup = getcaselookup(username, ViewBag.UserContractId);

            return View(viewlawenforcement);
        }
        #endregion

        #region Abuser

        public PartialViewResult PartialListAbuser()
        {

            return PartialView("PartialListAbusers");
        }

        public ActionResult EditAbuser(int Id, int IntakeId)
        {
            viewAbuserInformation viewabuserinfo;

            if (Id != 0)
            {
                viewabuserinfo = CMSService.GetAbuser(Id);

            }
            else
            {
                viewabuserinfo = new viewAbuserInformation();
                viewabuserinfo.LegalStatus = codeTable.ListOfLegalStatus;
                viewabuserinfo.Barriers = codeTable.ListOfAbuserBarriers;
                viewabuserinfo.Services = codeTable.ListOfServices;
                viewabuserinfo.MedicalHistory = codeTable.ListOfAbsuerMedicalHistory;

            }

            viewabuserinfo.viewIntake = new viewIntake();
            viewabuserinfo.viewIntake = CMSService.GetIntake(IntakeId);
            viewabuserinfo.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)viewabuserinfo.viewIntake.CaseheaderId);
            viewabuserinfo.CaseheaderId = viewabuserinfo.viewIntake.viewCaseHeader.Id;
            //Comment by Satheesh
            viewabuserinfo.caselookup = getcaselookup(username, ViewBag.UserContractId);
            //Comment Ended

            return View(viewabuserinfo);
        }


        public virtual ActionResult SaveAbuser(viewAbuserInformation viewabuserinfo)
        {

            viewabuserinfo.UserCreated = username;
            viewabuserinfo.UserUpdated = username;
            viewabuserinfo.AbuserType = "Case";
            int abuserId = CMSService.SaveAbuser(viewabuserinfo);
            if (viewabuserinfo.IsAjax == true)
            {
                var IdandintakeId = new { Id = abuserId, IntakeId = viewabuserinfo.IntakeId };
                return Json(IdandintakeId, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Redirect(Url.Action("ManageCase", "Case", new { CaseheaderId = viewabuserinfo.CaseheaderId }));
            }
        }

        [HttpPost]
        //[MultipleButton(Name = "action", Argument = "SubmitAANumber")]
        public virtual ActionResult SubmitAANumber(viewClientCMS viewclient)
        {



            CMSService.SubmitAANumber(viewclient.viewabuserinfo);
            //viewIntake viewintake = CMSService.GetIntake(Id);

            //viewintake.ReferralAgencyTypeId = ReferralAgencyTypeID;
            //viewintake.mode = "T";
            //viewintake.viewClient = CMSService.GetviewClient(viewintake.TempClientId);
            ////viewintake.viewClient.CreatedBy = viewintake.UserCreated;
            ////viewintake.viewClient.CreatedDateTime = DateTime.Now;
            //viewintake.StatusDescription = CaseStatus.Submitted.ToString();
            ////TestService.TransferIntake(viewintake);
            //int intakeid = CMSService.SaveIntake(viewintake, true);

            return RedirectToAction("ManageCase", new { Id = viewclient.Id, CaseheaderId = viewclient.CaseheaderId });
        }

        public ActionResult ViewAbuser(int Id, int IntakeId)
        {
            viewAbuserInformation viewabuserinfo;
            viewabuserinfo = CMSService.GetAbuser(Id);

            viewabuserinfo.viewIntake = new viewIntake();
            viewabuserinfo.viewIntake = CMSService.GetIntake(IntakeId);
            viewabuserinfo.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)viewabuserinfo.viewIntake.CaseheaderId);
            viewabuserinfo.CaseheaderId = viewabuserinfo.viewIntake.viewCaseHeader.Id;
            //Comment by Satheesh
            viewabuserinfo.caselookup = getcaselookup(username, ViewBag.UserContractId);

            return View(viewabuserinfo);
        }


        //public ActionResult SearchAbuser()
        //{
        //    viewAbuserInformation viewabuser = new viewAbuserInformation();

        //    viewabuser.ListOfAbusers = CMSService.ListofAllAbusers().ToList();

        //    return View(viewabuser);
        //}

        #endregion

        #region OIRA

        public virtual ActionResult EditOIRA(int Id, int CaseheaderId)
        {
            viewOIRA OIRA = new viewOIRA();

            if (Id != 0)
            {
                OIRA = CMSService.GetOIRA(Id);
            }
            else
            {
                OIRA = new viewOIRA();
                OIRA.CaseheaderId = CaseheaderId;
                OIRA.CaseworkerSignatureDate = DateTime.Now;
            }
            OIRA.viewCaseHeader = new viewCaseHeader();
            OIRA.viewCaseHeader = CMSService.GetCaseHeaderById(CaseheaderId);

            return View(setIncompleteOIRAErrors(OIRA));
        }

        [HttpPost]
        public virtual ActionResult SaveOIRA(viewOIRA OIRA)
        {
            try
            {


                if (!ModelState.IsValid)
                {
                    return RedirectToAction("EditOIRA", "Case", new { Id = OIRA.Id, CaseheaderId = OIRA.CaseheaderId });
                }

                else
                {
                    if (!ModelState.IsValid)
                    {
                        return RedirectToAction("EditOIRA", "Case", new { Id = OIRA.Id, CaseheaderId = OIRA.CaseheaderId });
                    }
                    else
                    {
                        OIRA.StatusDescription = CaseStatus.Open.ToString();

                        OIRA.UserCreated = username;
                        OIRA.UserUpdated = username;

                        #region

                        if (OIRA.DateFTF == null)
                        {
                            OIRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select FTF Date");
                        }

                        if (OIRA.IsNeedshelpwithADLs == null)
                        {
                            OIRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select either Yes/No/Unknown");
                        }
                        else if (OIRA.IsNeedshelpwithADLs == "y" && OIRA.NeedshelpwithADLs == null)
                        {
                            OIRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select risk for  Functional Abilities");
                        }

                        if (OIRA.IsAppearsConfused == null)
                        {
                            OIRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select either Yes/No/Unknown");
                        }
                        else if (OIRA.IsAppearsConfused == "y" && OIRA.AppearsConfused == null)
                        {
                            OIRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select risk for  Mental Abilities");
                        }

                        if (OIRA.IsEnvironmentUnsafe == null)
                        {
                            OIRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select either Yes/No/Unknown");
                        }
                        else if (OIRA.IsEnvironmentUnsafe == "y" && OIRA.EnvironmentUnsafe == null)
                        {
                            OIRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select risk for  Environment");
                        }

                        if (OIRA.IsEndangeringBehaviors == null)
                        {
                            OIRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select either Yes/No/Unknown");
                        }
                        else if (OIRA.IsEndangeringBehaviors == "y" && OIRA.EndangeringBehaviors == null)
                        {
                            OIRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select risk for  Endangering Behaviors");
                        }

                        if (OIRA.IsResourcesMisused == null)
                        {
                            OIRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select either Yes/No/Unknown");
                        }
                        else if (OIRA.IsResourcesMisused == "y" && OIRA.ResourcesMisused == null)
                        {
                            OIRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select risk for  Financial Resources");
                        }

                        if (OIRA.IsBasicNeedsMet == null)
                        {
                            OIRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select either Yes/No/Unknown");
                        }
                        else if (OIRA.IsBasicNeedsMet == "y" && OIRA.BasicNeedsMet == null)
                        {
                            OIRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select risk for Support services");
                        }

                        if (OIRA.Rationale == null)
                        {
                            OIRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please enter Rationale");
                        }

                        if (OIRA.IsLowANE == false && OIRA.IsMediumANE == false && OIRA.IsHighANE == false && OIRA.IsANENA == false)
                        {
                            OIRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select ANE");
                        }

                        if (OIRA.IsLowSN == false && OIRA.IsMediumSN == false && OIRA.IsHighSN == false && OIRA.IsSNNA == false)
                        {
                            OIRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select SN");
                        }

                        if (OIRA.CaseworkerSignature == null)
                        {
                            OIRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please Sign");
                        }

                        if (OIRA.CaseworkerSignatureDate == null)
                        {
                            OIRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please enter Date");
                        }

                        #endregion

                        OIRA.Id = CMSService.SaveOIRA(OIRA);

                        //viewCaseHeader viewcaseheader = CMSService.GetCaseHeader(OIRA.ClientId);
                        //viewCaseDetail viewcasedetail = new viewCaseDetail();
                        //viewcasedetail.CaseId = viewcaseheader.Id;
                        //viewcasedetail.ClientId = OIRA.ClientId;
                        //viewcasedetail.IntakeId = OIRA.IntakeId;
                        //viewcasedetail.OIRAId = oiraId;
                        //viewcasedetail.UserCreated = username;
                        //viewcasedetail.UserUpdated = username;
                        //viewcasedetail.ContractId = viewcaseheader.ContractId;
                        //int casedetailid = CMSService.SaveCaseDetail(viewcasedetail);

                        if (!ModelState.IsValid)
                        {
                            return Redirect(Url.Action("ManageCase", "Case", new { CaseheaderId = OIRA.CaseheaderId }));
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw ex;
            }

            if (OIRA.isSessionMakeLive == true)
            {
                return RedirectToAction("EditOIRA", "Case", new { Id = OIRA.Id, CaseheaderId = OIRA.CaseheaderId });
            }
            else
            {
                return RedirectToAction("ManageCase", "Case", new { CaseheaderId = OIRA.CaseheaderId });
            }

        }

        public virtual ActionResult ViewOIRA(int Id, int CaseheaderId)
        {
            viewOIRA OIRA = CMSService.GetOIRA(Id);
            OIRA.viewCaseHeader = new viewCaseHeader();
            OIRA.viewCaseHeader = CMSService.GetCaseHeaderById(CaseheaderId);

            return View(OIRA);
        }

        #region Helper

        protected viewOIRA setIncompleteOIRAErrors(viewOIRA OIRA)
        {
            if (OIRA.Id > 0)
            {
                if (OIRA.DateFTF == null)
                {
                    OIRA.InCompleteErrors.ErrorsInOIRA = true;
                    OIRA.InCompleteErrors.HasErrorsDateFTF = true;
                }

                if (OIRA.IsNeedshelpwithADLs == null)
                {
                    OIRA.InCompleteErrors.ErrorsInOIRA = true;
                    OIRA.InCompleteErrors.HasErrorsIsFunctionalAbilities = true;
                }
                else if (OIRA.IsNeedshelpwithADLs == "y" && OIRA.NeedshelpwithADLs == null)
                {
                    OIRA.InCompleteErrors.ErrorsInOIRA = true;
                    OIRA.InCompleteErrors.HasErrorsFunctionalAbilities = true;
                }

                if (OIRA.IsAppearsConfused == null)
                {
                    OIRA.InCompleteErrors.ErrorsInOIRA = true;
                    OIRA.InCompleteErrors.HasErrorsIsMentalAbilities = true;
                }
                else if (OIRA.IsAppearsConfused == "y" && OIRA.AppearsConfused == null)
                {
                    OIRA.InCompleteErrors.ErrorsInOIRA = true;
                    OIRA.InCompleteErrors.HasErrorsMentalAbilities = true;
                }

                if (OIRA.IsEnvironmentUnsafe == null)
                {
                    OIRA.InCompleteErrors.ErrorsInOIRA = true;
                    OIRA.InCompleteErrors.HasErrorsIsEnvironment = true;
                }
                else if (OIRA.IsEnvironmentUnsafe == "y" && OIRA.EnvironmentUnsafe == null)
                {
                    OIRA.InCompleteErrors.ErrorsInOIRA = true;
                    OIRA.InCompleteErrors.HasErrorsEnvironment = true;
                }

                if (OIRA.IsEndangeringBehaviors == null)
                {
                    OIRA.InCompleteErrors.ErrorsInOIRA = true;
                    OIRA.InCompleteErrors.HasErrorsIsEndangeringBehaviors = true;
                }
                else if (OIRA.IsEndangeringBehaviors == "y" && OIRA.EndangeringBehaviors == null)
                {
                    OIRA.InCompleteErrors.ErrorsInOIRA = true;
                    OIRA.InCompleteErrors.HasErrorsEndangeringBehaviors = true;
                }

                if (OIRA.IsResourcesMisused == null)
                {
                    OIRA.InCompleteErrors.ErrorsInOIRA = true;
                    OIRA.InCompleteErrors.HasErrorsIsFinancialResources = true;
                }
                else if (OIRA.IsResourcesMisused == "y" && OIRA.ResourcesMisused == null)
                {
                    OIRA.InCompleteErrors.ErrorsInOIRA = true;
                    OIRA.InCompleteErrors.HasErrorsFinancialResources = true;
                }

                if (OIRA.IsBasicNeedsMet == null)
                {
                    OIRA.InCompleteErrors.ErrorsInOIRA = true;
                    OIRA.InCompleteErrors.HasErrorsIsSupportServices = true;
                }
                else if (OIRA.IsBasicNeedsMet == "y" && OIRA.BasicNeedsMet == null)
                {
                    OIRA.InCompleteErrors.ErrorsInOIRA = true;
                    OIRA.InCompleteErrors.HasErrorsSupportServices = true;
                }

                if (OIRA.Rationale == null)
                {
                    OIRA.InCompleteErrors.ErrorsInOIRA = true;
                    OIRA.InCompleteErrors.HasErrorsRationale = true;
                }

                if (OIRA.IsLowANE == false && OIRA.IsMediumANE == false && OIRA.IsHighANE == false && OIRA.IsANENA == false && OIRA.IsLowSN == false && OIRA.IsMediumSN == false && OIRA.IsHighSN == false && OIRA.IsSNNA == false)
                {
                    OIRA.InCompleteErrors.ErrorsInOIRA = true;
                    OIRA.InCompleteErrors.HasErrorsRisk = true;
                }

                if (OIRA.CaseworkerSignature == null)
                {
                    OIRA.InCompleteErrors.ErrorsInOIRA = true;
                    OIRA.InCompleteErrors.HasErrorsCaseworkerSignature = true;
                }

                if (OIRA.CaseworkerSignatureDate == null)
                {
                    OIRA.InCompleteErrors.ErrorsInOIRA = true;
                    OIRA.InCompleteErrors.HasErrorsCaseworkerSignatureDate = true;
                }
            }

            return OIRA;
        }

        #endregion

        #endregion

        #region ORA

        public virtual ActionResult EditORA(int Id, int IntakeId)
        {
            viewORA viewORA;

            if (Id != 0)
            {
                viewORA = CMSService.GetORA(Id);
            }
            else
            {
                viewORA = new viewORA();

            }
            viewORA.caselookup = getcaselookup(username, ViewBag.UserContractId);
            viewORA.IntakeId = IntakeId;
            viewORA.viewIntake = new viewIntake();
            viewORA.viewIntake = CMSService.GetIntake(IntakeId);
            viewORA.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)viewORA.viewIntake.CaseheaderId);
            viewORA.ListCasePlanTime = CMSService.ListOfCasePlanTimes().ToList();

            return View(setIncompleteORAErrors(viewORA));
        }

        [HttpPost]
        public virtual ActionResult SaveORA(viewORA viewORA)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return RedirectToAction("EditORA", "Case", new { Id = viewORA.Id, IntakeId = viewORA.IntakeId });
                }

                else
                {
                    if (!ModelState.IsValid)
                    {
                        return RedirectToAction("EditORA", "Case", new { Id = viewORA.Id, IntakeId = viewORA.IntakeId });
                    }
                    else
                    {
                        viewORA.StatusDescription = CaseStatus.Open.ToString();

                        viewORA.UserCreated = username;
                        viewORA.UserUpdated = username;
                        viewORA.IntakeId = viewORA.IntakeId;

                        #region

                        if (viewORA.DateFTF == null)
                        {
                            viewORA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select FTF Date");
                        }

                        if (viewORA.IsFunctionalAbilities == false && viewORA.FunctionalAbilities == null)
                        {
                            viewORA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please enter Functional Abilities");
                        }

                        if (viewORA.IsMentalAbilities == false && viewORA.MentalAbilities == null)
                        {
                            viewORA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please enter Mental Abilities");
                        }

                        if (viewORA.IsEnvironment == false && viewORA.Environment == null)
                        {
                            viewORA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please enter Environment");
                        }

                        if (viewORA.IsSubstanceAbuse == false && viewORA.SubstanceAbuse == null)
                        {
                            viewORA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please enter Substance Abuse");
                        }

                        if (viewORA.IsFinancialResources == false && viewORA.FinancialResources == null)
                        {
                            viewORA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please enter Financial Resources");
                        }

                        if (viewORA.IsFormalAndInformalServices == false && viewORA.FormalAndInformalServices == null)
                        {
                            viewORA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please enter Formal and Informal Services");
                        }

                        if (viewORA.IsAbuserFactors == false && viewORA.AbuserFactors == null)
                        {
                            viewORA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please enter Abuser Factors");
                        }

                        if (viewORA.Rationale == null)
                        {
                            viewORA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please enter Rationale");
                        }

                        //if (viewORA.IsLowANE == null && viewORA.IsMediumANE == null && viewORA.IsHighANE == null)
                        //{
                        //    viewORA.StatusDescription = CaseStatus.Incomplete.ToString();
                        //    ModelState.AddModelError("CustomError", "Please select ANE");
                        //}

                        //if (viewORA.IsLowSN == null && viewORA.IsMediumSN == null && viewORA.IsHighSN == null)
                        //{
                        //    viewORA.StatusDescription = CaseStatus.Incomplete.ToString();
                        //    ModelState.AddModelError("CustomError", "Please select SN");
                        //}

                        if (viewORA.CaseworkerSignature == null)
                        {
                            viewORA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please Sign");
                        }

                        if (viewORA.CaseworkerSignatureDate == null)
                        {
                            viewORA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please enter Date");
                        }

                        #endregion

                        viewORA.CaseheaderId = CMSService.SaveORA(viewORA);
                        if (!ModelState.IsValid)
                        {
                            return Redirect(Url.Action("ManageCase", "Case", new { CaseheaderId = viewORA.CaseheaderId }));
                        }
                    }
                }
            }


            catch (System.Exception ex)
            {
                throw ex;
            }

            return Redirect(Url.Action("ManageCase", "Case", new { CaseheaderId = viewORA.CaseheaderId }));
        }

        public virtual ActionResult ViewORA(int Id)
        {
            viewORA viewORA = CMSService.GetORA(Id);
            viewORA.caselookup = getcaselookup(username, ViewBag.UserContractId);
            viewORA.viewIntake = new viewIntake();
            viewORA.viewIntake = CMSService.GetIntake((int)viewORA.IntakeId);
            viewORA.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)viewORA.viewIntake.CaseheaderId);
            viewORA.ListCasePlanTime = CMSService.ListOfCasePlanTimes().ToList();

            return View(viewORA);
        }

        #region Helper

        protected viewORA setIncompleteORAErrors(viewORA viewORA)
        {
            if (viewORA.Id > 0)
            {
                if (viewORA.DateFTF == null)
                {
                    viewORA.InCompleteErrors.ErrorsInORA = true;
                    viewORA.InCompleteErrors.HasErrorsDateFTF = true;
                }
                if (viewORA.IsFunctionalAbilities == false && viewORA.FunctionalAbilities == null)
                {
                    viewORA.InCompleteErrors.ErrorsInORA = true;
                    viewORA.InCompleteErrors.HasErrorsFunctionalAbilities = true;
                }

                if (viewORA.IsMentalAbilities == false && viewORA.MentalAbilities == null)
                {
                    viewORA.InCompleteErrors.ErrorsInORA = true;
                    viewORA.InCompleteErrors.HasErrorsMentalAbilities = true;
                }

                if (viewORA.IsEnvironment == false && viewORA.Environment == null)
                {
                    viewORA.InCompleteErrors.ErrorsInORA = true;
                    viewORA.InCompleteErrors.HasErrorsEnvironment = true;
                }

                if (viewORA.IsSubstanceAbuse == false && viewORA.SubstanceAbuse == null)
                {
                    viewORA.InCompleteErrors.ErrorsInORA = true;
                    viewORA.InCompleteErrors.HasErrorsSubstanceAbuse = true;
                }

                if (viewORA.IsFinancialResources == false && viewORA.FinancialResources == null)
                {
                    viewORA.InCompleteErrors.ErrorsInORA = true;
                    viewORA.InCompleteErrors.HasErrorsFinancialResources = true;
                }

                if (viewORA.IsFormalAndInformalServices == false && viewORA.FormalAndInformalServices == null)
                {
                    viewORA.InCompleteErrors.ErrorsInORA = true;
                    viewORA.InCompleteErrors.HasErrorsFormalAndInformalServices = true;
                }

                if (viewORA.IsAbuserFactors == false && viewORA.AbuserFactors == null)
                {
                    viewORA.InCompleteErrors.ErrorsInORA = true;
                    viewORA.InCompleteErrors.HasErrorsAbuserFactors = true;
                }

                if (viewORA.Rationale == null)
                {
                    viewORA.InCompleteErrors.ErrorsInORA = true;
                    viewORA.InCompleteErrors.HasErrorsRationale = true;
                }

                if (viewORA.IsANENA == false && viewORA.IsLowANE == false && viewORA.IsMediumANE == false && viewORA.IsHighANE == false && viewORA.IsSNNA == false && viewORA.IsLowSN == false && viewORA.IsMediumSN == false && viewORA.IsHighSN == false)
                {
                    viewORA.InCompleteErrors.ErrorsInORA = true;
                    viewORA.InCompleteErrors.HasErrorsRisk = true;
                }
                               
                if (viewORA.CaseworkerSignature == null)
                {
                    viewORA.InCompleteErrors.ErrorsInORA = true;
                    viewORA.InCompleteErrors.HasErrorsCaseworkerSignature = true;
                }

                if (viewORA.CaseworkerSignatureDate == null)
                {
                    viewORA.InCompleteErrors.ErrorsInORA = true;
                    viewORA.InCompleteErrors.HasErrorsCaseworkerSignatureDate = true;
                }


            }

            return viewORA;
        }

        #endregion

        #endregion

        #region OSRA

        public virtual ActionResult EditOSRA(int Id, int IntakeId)
        {
            viewOSRA viewOSRA;

            if (Id != 0)
            {
                viewOSRA = CMSService.GetOSRA(Id);
            }
            else
            {
                viewOSRA = new viewOSRA();
            }
            viewOSRA.caselookup = getcaselookup(username, ViewBag.UserContractId);

            viewOSRA.IntakeId = IntakeId;
            viewOSRA.viewIntake = new viewIntake();
            viewOSRA.viewIntake = CMSService.GetIntake(IntakeId);
            viewOSRA.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)viewOSRA.viewIntake.CaseheaderId);

            return View(setIncompleteOSRAErrors(viewOSRA));
        }

        [HttpPost]
        public virtual ActionResult SaveOSRA(viewOSRA viewOSRA)
        {

            try
            {

                if (!ModelState.IsValid)
                {
                    return RedirectToAction("EditOSRA", "Case", new { Id = viewOSRA.Id, IntakeId = viewOSRA.IntakeId });
                }

                else
                {
                    if (!ModelState.IsValid)
                    {
                        return RedirectToAction("EditOSRA", "Case", new { Id = viewOSRA.Id, IntakeId = viewOSRA.IntakeId });
                    }
                    else
                    {

                        viewOSRA.StatusDescription = CaseStatus.Open.ToString();

                        if (viewOSRA.DateFTF == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select FTF Date");
                        }

                        #region Functional

                        if (viewOSRA.IsPhysicalDisability == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Functional Abilities - Please select Physical Disabilities");
                        }
                        else if (viewOSRA.IsPhysicalDisability == "y" && (viewOSRA.IsPhysicalDisabilitySevere == false && viewOSRA.IsPhysicalDisabilityModerate == false && viewOSRA.IsPhysicalDisabilityMild == false))
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Functional Abilities - Please check all that apply under Physical Disabilities Options");
                        }

                        if (viewOSRA.IsPhysicalCapacity == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Functional Abilities - Please select Physical Capacity");
                        }

                        if (viewOSRA.IsClientCanReact == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Functional Abilities - Please select whether client can react");
                        }
                        else if (viewOSRA.IsClientCanReact == "y" && (viewOSRA.IsClientCanReactForSelf == false && viewOSRA.IsClientCanReactDial911 == false))
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Functional Abilities - Please check all that apply under Client Reaction Options");
                        }

                        if (viewOSRA.IsClientNeedsDressing == null && viewOSRA.IsClientNeedsBathing == null && viewOSRA.IsClientNeedsGrooming == null
                            && viewOSRA.IsClientNeedsFoodShopping == null && viewOSRA.IsClientNeedsMealsPreparing == null && viewOSRA.IsClientNeedsEating == null
                            && viewOSRA.IsClientNeedsUsingToilet == null && viewOSRA.IsClientNeedsHouseKeeping == null && viewOSRA.IsClientNeedsTransportation == null
                            && viewOSRA.IsClientNeedsAppliance == null && viewOSRA.IsClientNeedsTelephone == null && viewOSRA.IsClientNeedsMedicalCare == null
                            && viewOSRA.IsClientNeedsSelfAdminMedication == null && viewOSRA.IsClientNeedsOther == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Functional Abilities - Please select either one of Met/Unmet");
                        }
                        else if (viewOSRA.IsClientNeedsDressing == "y" && viewOSRA.ClientNeedsDressingAssistant == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Functional Abilities - Please select Assistant");
                        }
                        else if (viewOSRA.IsClientNeedsBathing == "y" && viewOSRA.ClientNeedsBathingAssistant == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Functional Abilities - Please select Assistant");
                        }
                        else if (viewOSRA.IsClientNeedsGrooming == "y" && viewOSRA.ClientNeedsGroomingAssistant == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Functional Abilities - Please select Assistant");
                        }
                        else if (viewOSRA.IsClientNeedsFoodShopping == "y" && viewOSRA.ClientNeedsFoodShoppingAssistant == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Functional Abilities - Please select Assistant");
                        }
                        else if (viewOSRA.IsClientNeedsMealsPreparing == "y" && viewOSRA.ClientNeedsMealsPreparingAssistant == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Functional Abilities - Please select Assistant");
                        }
                        else if (viewOSRA.IsClientNeedsEating == "y" && viewOSRA.ClientNeedsEatingAssistant == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Functional Abilities - Please select Assistant");
                        }
                        else if (viewOSRA.IsClientNeedsUsingToilet == "y" && viewOSRA.ClientNeedsUsingToiletAssistant == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Functional Abilities - Please select Assistant");
                        }
                        else if (viewOSRA.IsClientNeedsAppliance == "y" && viewOSRA.ClientNeedsApplianceAssistant == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Functional Abilities - Please select Assistant");
                        }
                        else if (viewOSRA.IsClientNeedsSelfAdminMedication == "y" && viewOSRA.ClientNeedsSelfAdminMedicationAssistant == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Functional Abilities - Please select Assistant");
                        }
                        else if (viewOSRA.IsClientNeedsHouseKeeping == "y" && viewOSRA.ClientNeedsHouseKeepingAssistant == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Functional Abilities - Please select Assistant");
                        }
                        else if (viewOSRA.IsClientNeedsTelephone == "y" && viewOSRA.ClientNeedsTelephoneAssistant == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Functional Abilities - Please select Assistant");
                        }
                        else if (viewOSRA.IsClientNeedsOther == "y" && viewOSRA.ClientNeedsOtherAssistant == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Functional Abilities - Please select Assistant");
                        }
                        else if (viewOSRA.IsClientNeedsTransportation == "y" && viewOSRA.ClientNeedsTransportationAssistant == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Functional Abilities - Please select Assistant");
                        }
                        else if (viewOSRA.IsClientNeedsMedicalCare == "y" && viewOSRA.ClientNeedsMedicalCareAssistant == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Functional Abilities - Please select Assistant");
                        }

                        #endregion

                        #region Emotional

                        if (viewOSRA.IsMentalCondition == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Mental Conditions - MentalCondition.Please select atleast one from Yes/No/Unknown");
                        }
                        else if (viewOSRA.IsMentalCondition == "y" && (viewOSRA.IsMentalConditionProfound == false && viewOSRA.IsMentalConditionModerate == false))
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Mental Conditions - MentalCondition.Please check all that apply");
                        }

                        if (viewOSRA.IsMentalCapacity == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Mental Conditions - MentalCapacity.Please select atleast one from Yes/No/Unknown");
                        }
                        else if (viewOSRA.IsMentalCapacity == "n" && (viewOSRA.IsMentalCapacityCognitiveImpairement == false && viewOSRA.IsMentalCapacityDevelopmentalDisability == false
                                                                                  && viewOSRA.IsMentalCapacityEmotionalProblems == false && viewOSRA.IsMentalCapacityTraumaticBrainInjury == false
                                                                                  && viewOSRA.IsMentalCapacityOther == false))
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Mental Conditions - MentalCapacity.Please check all that apply");
                        }

                        if (viewOSRA.IsMentalCapacityOther == true && viewOSRA.MentalCapacityOther == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Mental Conditions - MentalCapacity.Please enter Other");
                        }

                        #endregion

                        #region Environment

                        if (viewOSRA.IsClientLivingSituation == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Environment - Please select atleast one from Yes/No/Unknown");
                        }
                        else if (viewOSRA.IsClientLivingSituation == "n" && viewOSRA.ClientLivingSituation == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Environment - Please enter living situation");
                        }

                        if (viewOSRA.IsClientPlaceSafe == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Environment - Please select atleast one from Yes/No/Unknown");
                        }
                        else if (viewOSRA.IsClientPlaceSafe == "n" && (viewOSRA.IsClientPlaceUtilities == false && viewOSRA.IsClientPlacePosesBarriers == false
                                                                                    && viewOSRA.IsClientPlaceSettingUnsafe == false && viewOSRA.IsClientPlaceOther == false))
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Environment - Please check all that apply");
                        }

                        if (viewOSRA.IsClientPlaceOther == true && viewOSRA.ClientPlaceOther == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Environment - Please enter other place");
                        }

                        if (viewOSRA.IsClientResidenceMeetsStandards == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Environment - Please select atleast one from Yes/No/Unknown");
                        }
                        else if (viewOSRA.IsClientResidenceMeetsStandards == "n" && (viewOSRA.IsClientResidenceTrashDisposed == false && viewOSRA.IsClientResidenceGrossHealthViolations == false))
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Environment - Please check all that apply");
                        }

                        if (viewOSRA.IsClutterImageRatingNotApplicable == false && viewOSRA.IsClutterImageRatingKitchen == false
                            && viewOSRA.IsClutterImageRatingBedRoom == false && viewOSRA.IsClutterImageRatingLivingRoom == false)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Environment - Please check all that apply");
                        }
                        //else if(viewOSRA.IsClutterImageRatingNotApplicable = 0 && viewOSRA.IsClutterImageRatingNotApplicable = 0 &&
                        //    viewOSRA.IsClutterImageRatingNotApplicable = 0 )
                        //{
                        //      viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                        // ModelState.AddModelError("CustomError", "Please");
                        //}


                        #endregion

                        #region Substance Abuse and Other Endangering Behavior

                        if (viewOSRA.IsClientSuffersSubstanceAbuse == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Substance Abuse - Please select atleast one from Yes/No/Unknown");
                        }
                        else if (viewOSRA.IsClientSuffersSubstanceAbuse == "y" && (viewOSRA.IsClientSuffersActiveSubstanceAbuse == false && viewOSRA.IsClientSuffersPeriodicSubstanceAbuse == false))
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Substance Abuse - Please check all that apply");
                        }

                        if (viewOSRA.IsClientCheminallyAbusive == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Substance Abuse - Please select atleast one from Yes/No/Unknown");
                        }
                        else if (viewOSRA.IsClientCheminallyAbusive == "y" && (viewOSRA.IsClientChemicalThreatensHealth == false && viewOSRA.IsClientSuffersThreatensMedication == false
                                                                                  && viewOSRA.IsClientEnrolledInSubstanceAbuseTreatment == false))
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Substance Abuse - Please check all that apply");
                        }

                        #endregion

                        #region Income/ Financial

                        if (viewOSRA.EmploymentStatusId == 0)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Financial Resources - Please select Employment status");
                        }

                        if (viewOSRA.IsClientHaveFinancialResources == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Financial Resources - Please select atleast one from Yes/No/Unknown");
                        }

                        if (viewOSRA.IsCaregiverProvidingBasicNeeds == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Financial Resources - Please select atleast one from Yes/No/Unknown");
                        }
                        else if (viewOSRA.IsCaregiverProvidingBasicNeeds == "n" && (viewOSRA.IsClienthasInsufficientAssets == false && viewOSRA.IsCaregiverNotUtilizeServices == false
                                                                                                && viewOSRA.IsNoResourcesAvailable == false))
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Financial Resources - Please check all that apply");
                        }

                        if (viewOSRA.IsClientAssetsProperlyManaged == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Financial Resources - Please select atleast one from Yes/No/Unknown");
                        }
                        else if (viewOSRA.IsClientAssetsProperlyManaged == "n" && viewOSRA.ClientAssetsProperlyManaged == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Financial Resources - Please explain");
                        }

                        #endregion

                        #region Formal and Informal Support Services

                        if (viewOSRA.IsClientHasSupportiveFriendship == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Formal and Informal Services - Please select atleast one from Yes/No/Unknown");
                        }

                        if (viewOSRA.IsNeighborsArrangeForServices == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Formal and Informal Services - Please select atleast one from Yes/No/Unknown");
                        }
                        else if (viewOSRA.IsNeighborsArrangeForServices == "n" && (viewOSRA.IsNoDependableFamily == false && viewOSRA.IsLacksAdequateInformalCare == false
                                                                                                && viewOSRA.IsLacksAdequateFormalCare == false && viewOSRA.IsIsolated == false))
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Formal and Informal Services - Please check all that apply");
                        }

                        if (viewOSRA.IsClientReceivingCommunitySupport == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Formal and Informal Services - Please select atleast one from Yes/No/Unknown");
                        }
                        //else if (viewOSRA.IsClientReceivingCommunitySupport == "y" && viewOSRA.ClientReceivingCommunitySupport == null)
                        //{
                        //    viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                        //    ModelState.AddModelError("CustomError", "Formal and Informal Services - Please explain");
                        //}

                        if (viewOSRA.IsClientHasAdvocate == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Formal and Informal Services - Please select atleast one from Yes/No/Unknown");
                        }
                        else if (viewOSRA.IsClientHasAdvocate == "n" && (viewOSRA.IsClientCaregiverToOthers == false && viewOSRA.IsClientReliesOnOtherToCommunicate == false
                                                                                                && viewOSRA.IsFormalother == false))
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Formal and Informal Services - Please check all that apply");
                        }

                        if (viewOSRA.IsFormalother == true && viewOSRA.Formalother == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Formal and Informal Services - Please enter other");
                        }

                        #endregion

                        #region Abuser Factors

                        //if (viewOSRA.IsAbuserEmotionallyDependentOnClient == false && viewOSRA.IsAbuserFinanciallyDependentOnClient == false && viewOSRA.IsAbuserUnreliable == false && viewOSRA.IsAbuserPoorCaregivingSkills == false
                        //    && viewOSRA.IsAbuserPoorCopingSkills == false && viewOSRA.IsAbuserIsolatesClient == false && viewOSRA.IsAbuserCognitiveLimitations == false && viewOSRA.IsAbuserPhysicalLimitations == false
                        //    && viewOSRA.IsAbuserSubstanceAbuseAlcohol == false && viewOSRA.IsAbuserSubstanceAbuseDrugs == false && viewOSRA.IsAbuserPresentsOverburdend == false && viewOSRA.IsAbuserWillNotPermitServices == false
                        //    && viewOSRA.IsAbuserUncooperative == false && viewOSRA.IsAbuserUndueInfluence == false && viewOSRA.IsAbuserUnrestricted == false && viewOSRA.IsAbuserHistoryOfAbuse == false
                        //    && viewOSRA.IsAbuserPetAbuse == false && viewOSRA.IsAbuserPreviousAbuser == false && viewOSRA.IsAbuserCriminalHistory == false && viewOSRA.IsAbuserNone == false
                        //    && viewOSRA.IsAbuserUnknown == false && viewOSRA.IsAbuserOther == false)
                        //{
                        //    viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                        //    ModelState.AddModelError("CustomError", "Abusers Factors - Please check all that apply");
                        //}


                        if (viewOSRA.IsAbuserOther == true && viewOSRA.AbuserOther == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Abusers Factors - Please enter other");
                        }

                        #endregion

                        #region Risk

                        if (viewOSRA.Rationale == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Overall Risk Assessment - Please enter Rationale");
                        }

                        if (viewOSRA.IsANENA == false && viewOSRA.IsLowANE == false && viewOSRA.IsMediumANE == false && viewOSRA.IsHighANE == false && viewOSRA.IsSNNA == false &&  viewOSRA.IsLowSN == false && viewOSRA.IsMediumSN == false && viewOSRA.IsHighSN == false)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Overall Risk Assessment - Please select Risk level");
                        }

                        //if (viewOSRA.IsLowSN == false && viewOSRA.IsMediumSN == false && viewOSRA.IsHighSN == false)
                        //{
                        //    viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                        //    ModelState.AddModelError("CustomError", "Overall Risk Assessment - Please select SN");
                        //}

                        #endregion


                        //if (viewOSRA.StatusDescription == "Submitted")
                        //{
                        //    if (viewOSRA.SupervisorSignature == null)
                        //    {
                        //        viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                        //        ModelState.AddModelError("CustomError", "Supervisor - Please sign");
                        //    }

                        //    if (viewOSRA.SupervisorSignatureDate == null)
                        //    {
                        //        viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                        //        ModelState.AddModelError("CustomError", "Supervisor - Please enter Date");
                        //    }
                        //}

                        if (viewOSRA.CaseworkerSignature == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Caseworker - Please sign");
                        }
                        //else
                        //{
                        //    viewOSRA.StatusDescription = CaseStatus.Submitted.ToString();
                        //}

                        if (viewOSRA.CaseworkerSignatureDate == null)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Caseworker - Please enter Date");
                        }
                        if (viewOSRA.IsSubmit == true)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Submitted.ToString();

                        }

                        if (viewOSRA.IsApprove == true)
                        {
                            viewOSRA.StatusDescription = CaseStatus.Approved.ToString();

                        }

                        viewOSRA.UserCreated = username;
                        viewOSRA.UserUpdated = username;

                        viewOSRA.IntakeId = viewOSRA.IntakeId;

                        viewOSRA.Id = CMSService.SaveOSRA(viewOSRA);
                    }
                }
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
                throw;
            }

            //viewCaseHeader viewcaseheader = CMSService.GetCaseHeader(viewclient.OIRA.ClientId);
            //viewCaseDetail viewcasedetail = new viewCaseDetail();
            //viewcasedetail.CaseheaderId = viewcaseheader.Id;
            //viewcasedetail.ClientId = viewclient.OIRA.ClientId;
            //viewcasedetail.IntakeId = viewclient.OIRA.IntakeId;
            //viewcasedetail.OIRAId = osraId;
            //viewcasedetail.UserCreated = username;
            //viewcasedetail.UserUpdated = username;
            //viewcasedetail.ContractId = viewcaseheader.ContractId;
            //int casedetailid = CMSService.SaveCaseDetail(viewcasedetail);
            ////viewIntake.viewClient = new viewClient();
            ////viewIntake.viewClient.Id = viewIntake.TempClientId;
            //////viewIntake.viewClient.PersonKey = Guid.NewGuid();
            ////viewIntake.viewClient.CreatedBy = viewIntake.UserCreated;
            ////viewIntake.viewClient.CreatedDateTime = DateTime.Now;

            ////viewIntake.TempClientId = CMSService.SaveTempClient(viewIntake.viewClient);
            ////viewIntake.reporterinfoId = CMSService.saveReporterInfo(intake.Id, viewIntake.viewreporterinfo);
            //return RedirectToAction("ManageCase", "Case", new { Id = viewclient.Id });

            if (viewOSRA.IsAjax == true)
            {
                var redirectUrl = new UrlHelper(Request.RequestContext).Action("ManageCase", "Case", new { CaseheaderId = viewOSRA.viewIntake.CaseheaderId });
                var URLandId = new { url = redirectUrl, Id = viewOSRA.Id };
                return Json(URLandId, JsonRequestBehavior.AllowGet);
                //return Json(viewOSRA.Id, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Redirect(Url.Action("ManageCase", "Case", new { CaseheaderId = viewOSRA.viewIntake.CaseheaderId }));
            }
        }

        public virtual ActionResult ViewOSRA(int Id)
        {
            viewOSRA viewOSRA = CMSService.GetOSRA(Id);
            viewOSRA.caselookup = getcaselookup(username, ViewBag.UserContractId);
            viewOSRA.viewIntake = new viewIntake();
            viewOSRA.viewIntake = CMSService.GetIntake((int)viewOSRA.IntakeId);
            viewOSRA.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)viewOSRA.viewIntake.CaseheaderId);

            return View(viewOSRA);
        }

        #region Helper

        protected viewOSRA setIncompleteOSRAErrors(viewOSRA viewOSRA)
        {
            if (viewOSRA.Id > 0)
            {
                if (viewOSRA.DateFTF == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsDateFTF = true;
                }

                #region Functional

                if (viewOSRA.IsPhysicalDisability == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsPhysicalDisability = true;
                }
                else if (viewOSRA.IsPhysicalDisability == "y" && (viewOSRA.IsPhysicalDisabilitySevere == false && viewOSRA.IsPhysicalDisabilityModerate == false && viewOSRA.IsPhysicalDisabilityMild == false))
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsPhysicalDisabilityOptions = true;
                }

                if (viewOSRA.IsPhysicalCapacity == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsPhysicalIsPhysicalCapacity = true;
                }

                if (viewOSRA.IsClientCanReact == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientCanReact = true;
                }
                else if (viewOSRA.IsClientCanReact == "y" && (viewOSRA.IsClientCanReactForSelf == false && viewOSRA.IsClientCanReactDial911 == false))
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientCanReactOptions = true;
                }

                if (viewOSRA.IsClientNeedsDressing == null && viewOSRA.IsClientNeedsBathing == null && viewOSRA.IsClientNeedsGrooming == null
                    && viewOSRA.IsClientNeedsFoodShopping == null && viewOSRA.IsClientNeedsMealsPreparing == null && viewOSRA.IsClientNeedsEating == null
                    && viewOSRA.IsClientNeedsUsingToilet == null && viewOSRA.IsClientNeedsHouseKeeping == null && viewOSRA.IsClientNeedsTransportation == null
                    && viewOSRA.IsClientNeedsAppliance == null && viewOSRA.IsClientNeedsTelephone == null && viewOSRA.IsClientNeedsMedicalCare == null
                    && viewOSRA.IsClientNeedsSelfAdminMedication == null && viewOSRA.IsClientNeedsOther == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientNeedsDressingOptions = true;
                }

                else if (viewOSRA.IsClientNeedsDressing == "y" && viewOSRA.ClientNeedsDressingAssistant == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientNeedsDressingOptionsAssistant = true;
                }
                else if (viewOSRA.IsClientNeedsBathing == "y" && viewOSRA.ClientNeedsBathingAssistant == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientNeedsDressingOptionsAssistant = true;
                }
                else if (viewOSRA.IsClientNeedsGrooming == "y" && viewOSRA.ClientNeedsGroomingAssistant == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientNeedsDressingOptionsAssistant = true;
                }
                else if (viewOSRA.IsClientNeedsFoodShopping == "y" && viewOSRA.ClientNeedsFoodShoppingAssistant == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientNeedsDressingOptionsAssistant = true;
                }
                else if (viewOSRA.IsClientNeedsMealsPreparing == "y" && viewOSRA.ClientNeedsMealsPreparingAssistant == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientNeedsDressingOptionsAssistant = true;
                }
                else if (viewOSRA.IsClientNeedsEating == "y" && viewOSRA.ClientNeedsEatingAssistant == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientNeedsDressingOptionsAssistant = true;
                }
                else if (viewOSRA.IsClientNeedsUsingToilet == "y" && viewOSRA.ClientNeedsUsingToiletAssistant == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientNeedsDressingOptionsAssistant = true;
                }
                else if (viewOSRA.IsClientNeedsAppliance == "y" && viewOSRA.ClientNeedsApplianceAssistant == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientNeedsDressingOptionsAssistant = true;
                }
                else if (viewOSRA.IsClientNeedsSelfAdminMedication == "y" && viewOSRA.ClientNeedsSelfAdminMedicationAssistant == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientNeedsDressingOptionsAssistant = true;
                }
                else if (viewOSRA.IsClientNeedsHouseKeeping == "y" && viewOSRA.ClientNeedsHouseKeepingAssistant == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientNeedsDressingOptionsAssistant = true;
                }
                else if (viewOSRA.IsClientNeedsTelephone == "y" && viewOSRA.ClientNeedsTelephoneAssistant == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientNeedsDressingOptionsAssistant = true;
                }
                else if (viewOSRA.IsClientNeedsOther == "y" && viewOSRA.ClientNeedsOtherAssistant == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientNeedsDressingOptionsAssistant = true;
                }
                else if (viewOSRA.IsClientNeedsTransportation == "y" && viewOSRA.ClientNeedsTransportationAssistant == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientNeedsDressingOptionsAssistant = true;
                }
                else if (viewOSRA.IsClientNeedsMedicalCare == "y" && viewOSRA.ClientNeedsMedicalCareAssistant == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientNeedsDressingOptionsAssistant = true;
                }


                #endregion

                #region Emotional

                if (viewOSRA.IsMentalCondition == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsMentalCondition = true;
                }
                else if (viewOSRA.IsMentalCondition == "y" && (viewOSRA.IsMentalConditionProfound == false && viewOSRA.IsMentalConditionModerate == false))
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsMentalConditionOptions = true;
                }

                if (viewOSRA.IsMentalCapacity == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsMentalCapacityNo = true;
                }
                else if (viewOSRA.IsMentalCapacity == "n" && (viewOSRA.IsMentalCapacityCognitiveImpairement == false && viewOSRA.IsMentalCapacityDevelopmentalDisability == false
                                                                          && viewOSRA.IsMentalCapacityEmotionalProblems == false && viewOSRA.IsMentalCapacityTraumaticBrainInjury == false
                                                                          && viewOSRA.IsMentalCapacityOther == false))
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsMentalCapacityNoOptions = true;
                }

                if (viewOSRA.IsMentalCapacityOther == true && viewOSRA.MentalCapacityOther == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsMentalCapacityNoOptionsOther = true;
                }

                #endregion

                #region Environment

                if (viewOSRA.IsClientLivingSituation == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientLivingSituation = true;
                }
                else if (viewOSRA.IsClientLivingSituation == "n" && viewOSRA.ClientLivingSituation == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientLivingSituationNoOptions = true;
                }

                if (viewOSRA.IsClientPlaceSafe == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientPlaceSafe = true;
                }
                else if (viewOSRA.IsClientPlaceSafe == "n" &&
                        (viewOSRA.IsClientPlaceUtilities == false && viewOSRA.IsClientPlacePosesBarriers == false
                            && viewOSRA.IsClientPlaceSettingUnsafe == false && viewOSRA.IsClientPlaceOther == false))
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientPlaceSafeNoOptions = true;
                }

                if (viewOSRA.IsClientPlaceOther == true && viewOSRA.ClientPlaceOther == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientPlaceSafeNoOptionsOther = true;
                }

                if (viewOSRA.IsClientResidenceMeetsStandards == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientPlacePosesBarriers = true;
                }
                else if (viewOSRA.IsClientResidenceMeetsStandards == "n" && (viewOSRA.IsClientResidenceTrashDisposed == false && viewOSRA.IsClientResidenceGrossHealthViolations == false))
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientPlacePosesBarriersNoOptions = true;
                }

                if (viewOSRA.IsClutterImageRatingNotApplicable == false && viewOSRA.IsClutterImageRatingKitchen == false
                    && viewOSRA.IsClutterImageRatingBedRoom == false && viewOSRA.IsClutterImageRatingLivingRoom == false)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClutterImageRatingNotApplicableOptions = true;
                }
                //else if(viewOSRA.IsClutterImageRatingNotApplicable = 0 && viewOSRA.IsClutterImageRatingNotApplicable = 0 &&
                //    viewOSRA.IsClutterImageRatingNotApplicable = 0 )
                //{
                //     viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                //    viewOSRA.InCompleteErrors.HasErrorsIsClutterImageRatingValues = true;
                //}


                #endregion

                #region Substance Abuse and Other Endangering Behavior

                if (viewOSRA.IsClientSuffersSubstanceAbuse == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientSuffersSubstanceAbuse = true;
                }
                else if (viewOSRA.IsClientSuffersSubstanceAbuse == "y" && (viewOSRA.IsClientSuffersActiveSubstanceAbuse == false && viewOSRA.IsClientSuffersPeriodicSubstanceAbuse == false))
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientSuffersSubstanceAbuseOptions = true;
                }

                if (viewOSRA.IsClientCheminallyAbusive == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientCheminallyAbusive = true;
                }
                else if (viewOSRA.IsClientCheminallyAbusive == "y" && (viewOSRA.IsClientChemicalThreatensHealth == false && viewOSRA.IsClientSuffersThreatensMedication == false
                                                                          && viewOSRA.IsClientEnrolledInSubstanceAbuseTreatment == false))
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientCheminallyAbusiveOptions = true;
                }

                #endregion

                #region Income/ Financial

                if (viewOSRA.EmploymentStatusId == 0)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsEmploymentStatusId = true;
                }

                if (viewOSRA.IsClientHaveFinancialResources == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientHaveFinancialResources = true;
                }

                if (viewOSRA.IsCaregiverProvidingBasicNeeds == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsCaregiverProvidingBasicNeeds = true;
                }
                else if (viewOSRA.IsCaregiverProvidingBasicNeeds == "n" && (viewOSRA.IsClienthasInsufficientAssets == false && viewOSRA.IsCaregiverNotUtilizeServices == false
                                                                                        && viewOSRA.IsNoResourcesAvailable == false))
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsCaregiverProvidingBasicNeedsNoOptions = true;
                }

                if (viewOSRA.IsClientAssetsProperlyManaged == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientAssetsProperlyManaged = true;
                }
                else if (viewOSRA.IsClientAssetsProperlyManaged == "n" && viewOSRA.ClientAssetsProperlyManaged == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientAssetsProperlyManagedNoExplain = true;
                }

                #endregion

                #region Formal and Informal Support Services

                if (viewOSRA.IsClientHasSupportiveFriendship == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientHasSupportiveFriendship = true;
                }

                if (viewOSRA.IsNeighborsArrangeForServices == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsNeighborsArrangeForServices = true;
                }
                else if (viewOSRA.IsNeighborsArrangeForServices == "n" && (viewOSRA.IsNoDependableFamily == false && viewOSRA.IsLacksAdequateInformalCare == false
                                                                                        && viewOSRA.IsLacksAdequateFormalCare == false && viewOSRA.IsIsolated == false))
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsNeighborsArrangeForServicesOptions = true;
                }

                if (viewOSRA.IsClientReceivingCommunitySupport == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientReceivingCommunitySupport = true;
                }
                else if (viewOSRA.IsClientReceivingCommunitySupport == "y" && viewOSRA.ClientReceivingCommunitySupport == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientReceivingCommunitySupportNoExplain = true;
                }

                if (viewOSRA.IsClientHasAdvocate == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientHasAdvocate = true;
                }
                else if (viewOSRA.IsClientHasAdvocate == "n" && (viewOSRA.IsClientCaregiverToOthers == false && viewOSRA.IsClientReliesOnOtherToCommunicate == false
                                                                                        && viewOSRA.IsFormalother == false))
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientHasAdvocateNoOptions = true;
                }

                if (viewOSRA.IsFormalother == true && viewOSRA.Formalother == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsClientHasAdvocateNoOptionsOther = true;
                }

                #endregion

                #region Abuser Factors

                //if (viewOSRA.IsAbuserEmotionallyDependentOnClient == false && viewOSRA.IsAbuserFinanciallyDependentOnClient == false && viewOSRA.IsAbuserUnreliable == false && viewOSRA.IsAbuserPoorCaregivingSkills == false
                //    && viewOSRA.IsAbuserPoorCopingSkills == false && viewOSRA.IsAbuserIsolatesClient == false && viewOSRA.IsAbuserCognitiveLimitations == false && viewOSRA.IsAbuserPhysicalLimitations == false
                //    && viewOSRA.IsAbuserSubstanceAbuseAlcohol == false && viewOSRA.IsAbuserSubstanceAbuseDrugs == false && viewOSRA.IsAbuserPresentsOverburdend == false && viewOSRA.IsAbuserWillNotPermitServices == false
                //    && viewOSRA.IsAbuserUncooperative == false && viewOSRA.IsAbuserUndueInfluence == false && viewOSRA.IsAbuserUnrestricted == false && viewOSRA.IsAbuserHistoryOfAbuse == false
                //    && viewOSRA.IsAbuserPetAbuse == false && viewOSRA.IsAbuserPreviousAbuser == false && viewOSRA.IsAbuserCriminalHistory == false && viewOSRA.IsAbuserNone == false
                //    && viewOSRA.IsAbuserUnknown == false && viewOSRA.IsAbuserOther == false)
                //{
                //    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                //    viewOSRA.InCompleteErrors.HasErrorsIsAbuserOptions = true;
                //}


                if (viewOSRA.IsAbuserOther == true && viewOSRA.AbuserOther == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsIsAbuserOptionsOther = true;
                }

                #endregion

                #region Risk

                if (viewOSRA.Rationale == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsRationale = true;
                }

                if (viewOSRA.IsLowANE == false && viewOSRA.IsMediumANE == false && viewOSRA.IsHighANE == false && viewOSRA.IsLowSN == false && viewOSRA.IsMediumSN == false && viewOSRA.IsHighSN == false)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsANE = true;
                }

                //if (viewOSRA.IsLowSN == false && viewOSRA.IsMediumSN == false && viewOSRA.IsHighSN == false)
                //{
                //    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                //    viewOSRA.InCompleteErrors.HasErrorsSN = true;
                //}

                #endregion

                if (viewOSRA.CaseworkerSignature == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsCaseworkerSignature = true;
                }

                if (viewOSRA.CaseworkerSignatureDate == null)
                {
                    viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                    viewOSRA.InCompleteErrors.HasErrorsCaseworkerSignatureDate = true;
                }

                if (viewOSRA.StatusDescription == "Submitted")
                {

                    if (viewOSRA.SupervisorSignature == null)
                    {
                        viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                        viewOSRA.InCompleteErrors.HasErrorsSupervisorSignature = true;
                    }

                    if (viewOSRA.SupervisorSignatureDate == null)
                    {
                        viewOSRA.InCompleteErrors.ErrorsInOSRA = true;
                        viewOSRA.InCompleteErrors.HasErrorsSupervisorSignatureDate = true;
                    }
                }

            }


            return viewOSRA;
        }

        #endregion

        #endregion

        #region Client Status

        public virtual ActionResult EditClientStatus(int Id, int CaseheaderId)
        {
            viewClientStatus viewClientStatus;


            if (Id != 0)
            {
                viewClientStatus = CMSService.GetClientStatus(Id);
            }
            else
            {
                viewClientStatus = new viewClientStatus();
                viewClientStatus.ClientId = Id;

                viewInvolvedAgencies involvedagency = new viewInvolvedAgencies();
                viewClientStatus.ListInvolvedAgencies.Add(involvedagency);

                viewOrdersOfProtection ordersofprotection = new viewOrdersOfProtection();
                viewClientStatus.ListOrdersOfProtection.Add(ordersofprotection);

                viewListOfServices servicesatstart = new viewListOfServices();
                viewClientStatus.ListOfServicesAtStart.Add(servicesatstart);

                viewListOfServices servicesreferredbyAPS = new viewListOfServices();
                viewClientStatus.ListOfServicesReferredByAPS.Add(servicesreferredbyAPS);

                viewOthersInHousehold othersinhousehold = new viewOthersInHousehold();
                viewClientStatus.ListOthersInHousehold.Add(othersinhousehold);

                viewOthersNotInHousehold othersnotinhousehold = new viewOthersNotInHousehold();
                viewClientStatus.ListOthersNotInHousehold.Add(othersnotinhousehold);

                viewPhysician physician = new viewPhysician();
                viewClientStatus.ListPhysicians.Add(physician);

                viewHealthInsurnace healthinsurance = new viewHealthInsurnace();
                viewClientStatus.ListHealthInsurnaces.Add(healthinsurance);

                viewMedication medication = new viewMedication();
                viewClientStatus.ListMedications.Add(medication);

                viewBanking banking = new viewBanking();
                viewClientStatus.ListBankings.Add(banking);

                viewIncomeHelper income = new viewIncomeHelper();
                viewClientStatus.ListIncomeHelpers.Add(income);

                viewClientStatus.MedicalHistory = codeTable.ListOfClientMedicalHistory;
                viewClientStatus.Barriers = codeTable.ListOfClientBarriers;
                viewClientStatus.Benefits = codeTable.ListOfBenefits;
                viewClientStatus.LivingArrangements = codeTable.ListOfLivingArrangements;
                viewClientStatus.LegalStatus = codeTable.ListOfLegalStatus;

                viewLegalStatusHelper guardianofestate = new viewLegalStatusHelper();
                viewLegalStatusHelper guardianofperson = new viewLegalStatusHelper();
                viewLegalStatusHelper poahealthcare = new viewLegalStatusHelper();
                viewLegalStatusHelper poafinances = new viewLegalStatusHelper();
                viewLegalStatusHelper representativepayee = new viewLegalStatusHelper();
                viewLegalStatusHelper guardiannonspecific = new viewLegalStatusHelper();

                for (int i = 0; i < viewClientStatus.LegalStatus.Count; i++)
                {
                    viewClientStatus.LegalStatus[i].GuardiansofEstate.Add(guardianofestate);
                    viewClientStatus.LegalStatus[i].GuardiansofPerson.Add(guardianofperson);
                    viewClientStatus.LegalStatus[i].POAHealthCare.Add(poahealthcare);
                    viewClientStatus.LegalStatus[i].POAFinances.Add(poafinances);
                    viewClientStatus.LegalStatus[i].RepresentativePayee.Add(representativepayee);
                    viewClientStatus.LegalStatus[i].GuardianNonSpecific.Add(guardiannonspecific);
                }

            }

            viewClientStatus.caselookup = getcaselookup(username, ViewBag.UserContractId);
            viewClientStatus.viewCaseHeader = new viewCaseHeader();
            viewClientStatus.CaseheaderId = CaseheaderId;
            viewClientStatus.viewCaseHeader = CMSService.GetCaseHeaderById(CaseheaderId);

            ViewBag.InterAgencies = GetInterAgencyddl();
            ViewBag.Services = GetServices();
            ViewBag.Relations = GetRelations();
            ViewBag.AilmentConfirmation = GetAilmentConfirmation();
            ViewBag.VeteranStatus = GetVeteranStatus();

            return View(setIncompleteClientStatusErrors(viewClientStatus));
        }

        public virtual ActionResult SaveClientStatus(viewClientStatus viewClientStatus)
        {

            try
            {

                if (!ModelState.IsValid)
                {
                    foreach (ModelState state in ViewData.ModelState.Values.Where(x => x.Errors.Count > 0))
                    {
                        Console.WriteLine(state.Value);
                    }
                    return RedirectToAction("EditClientStatus", "Case", new { Id = viewClientStatus.Id, CaseheaderId = viewClientStatus.CaseheaderId });
                }

                else
                {

                    viewClientStatus.StatusDescription = CaseStatus.Open.ToString();


                    if (viewClientStatus.VeteranStatusId == null)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select Veteran Status");
                    }
                    if (viewClientStatus.PrimaryLanguageId == null)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select Primary Language");
                    }

                    if (viewClientStatus.SchoolingLevelId == null)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select Schooling Level");
                    }

                    int legalstatuscount = 0;
                    foreach (var legalstatus in viewClientStatus.LegalStatus)
                    {
                        if (legalstatus.IsChecked == true)
                        {
                            legalstatuscount = legalstatuscount + 1;
                        }
                        //if (legalstatus.IsChecked == true && legalstatus.GuardiansofEstate.Count > 0 )
                        //{
                        //    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                        //    viewClientStatus.InCompleteErrors.HasErrorsLegalStatus = true;
                        //}
                    }

                    if (legalstatuscount == 0)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select atelast one under Legal Status at Start");
                    }

                    if (viewClientStatus.IsOrdersOfProtection == null)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select either Yes/No under Orders of Protection");
                    }

                    if (viewClientStatus.IsPhotos == true && viewClientStatus.PhotosDate == null)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select date under Photos");
                    }

                    if (viewClientStatus.IsPhotos == true && viewClientStatus.IsPhotosROI == null)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select whether ROI is Yes/No under Photos");
                    }

                    if (viewClientStatus.IsPhotosROI == "y" && (viewClientStatus.IsPhotosROIVerbal == false && viewClientStatus.IsPhotosROIWritten == false))
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select whether ROI is verbal/written under Photos");
                    }

                    if (viewClientStatus.IsCompetencyQuestioned == null)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please whether Client Competency is questioned or not");
                    }

                    if (viewClientStatus.IsCompetencyQuestioned == "y" && viewClientStatus.MMSEScore == null)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please enter MMSE Score for Yes of Client Competency");
                    }
                    if (viewClientStatus.IsCompetencyQuestioned == "y" && viewClientStatus.MMSEDate == null)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please enter MMSE Date for Yes of Client Competency");
                    }

                    if (viewClientStatus.IsCompetencyQuestioned == "n" && viewClientStatus.IsCompetencyUnableReason == null)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please enter reason for No of Client Competency");
                    }
                    if (viewClientStatus.IsClientIncapacity == null)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select whether client consent for ROI is taken");
                    }

                    int livingarrangementcount = 0;
                    foreach (var livingarrangement in viewClientStatus.LivingArrangements)
                    {
                        if (livingarrangement.IsChecked == true)
                        {
                            livingarrangementcount = livingarrangementcount + 1;
                        }
                        //if (legalstatus.IsChecked == true && legalstatus.GuardiansofEstate.Count > 0 )
                        //{
                        //    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                        //    viewClientStatus.InCompleteErrors.HasErrorsLegalStatus = true;
                        //}
                    }

                    if (livingarrangementcount == 0)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select atleast one under Living arrangements");
                    }

                    if (viewClientStatus.IsDaytimeLocHome == false && viewClientStatus.IsDaytimeLocWorkShopSite == false && viewClientStatus.IsDaytimeLocWork == false
                        && viewClientStatus.IsDaytimeLocDayTraining == false && viewClientStatus.IsDaytimeLocAdultDayService == false && viewClientStatus.IsDaytimeLocOther == false)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select atleast one under Daytime Locations");
                    }

                    if (viewClientStatus.IsDaytimeLocWorkShopSite == true && viewClientStatus.DaytimeLocWorkShopSite == null)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please enter Work Shop Site Location");
                    }

                    if (viewClientStatus.IsDaytimeLocWork == true && viewClientStatus.DaytimeLocWork == null)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please enter Work Location");
                    }

                    if (viewClientStatus.IsDaytimeLocDayTraining == true && viewClientStatus.DaytimeLocDayTraining == null)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please enter Day Training Location");
                    }

                    if (viewClientStatus.IsDaytimeLocAdultDayService == true && viewClientStatus.DaytimeLocAdultDayService == null)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please enter Adult Day Service Location");
                    }

                    if (viewClientStatus.IsOthersInHousehold == null)
                    {
                        foreach (var other in viewClientStatus.ListOthersInHousehold)
                        {
                            if (other.OthersName == null)
                            {
                                viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please select options under Others in Household or enter details");
                            }
                        }
                    }

                    if (viewClientStatus.IsOthersNotInHousehold == null)
                    {
                        foreach (var other in viewClientStatus.ListOthersNotInHousehold)
                        {
                            if (other.OthersName == null)
                            {
                                viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please select options under Others not in Household or enter details");
                            }
                        }
                    }

                    int medicalhistorycount = 0;
                    foreach (var medicalhistory in viewClientStatus.MedicalHistory)
                    {
                        if (medicalhistory.IsChecked == true)
                        {
                            medicalhistorycount = medicalhistorycount + 1;
                        }
                        //if (legalstatus.IsChecked == true && legalstatus.GuardiansofEstate.Count > 0 )
                        //{
                        //    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                        //    viewClientStatus.InCompleteErrors.HasErrorsLegalStatus = true;
                        //}
                    }

                    if (medicalhistorycount == 0)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select atleast one under Medical History");
                    }

                    int barrierscount = 0;
                    foreach (var barrier in viewClientStatus.Barriers)
                    {
                        if (barrier.IsChecked == true)
                        {
                            barrierscount = barrierscount + 1;
                        }
                        //if (legalstatus.IsChecked == true && legalstatus.GuardiansofEstate.Count > 0 )
                        //{
                        //    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                        //    viewClientStatus.InCompleteErrors.HasErrorsLegalStatus = true;
                        //}
                    }

                    if (barrierscount == 0)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select atleast one under Barriers");
                    }

                    if (viewClientStatus.IsPhysician == null)
                    {
                        foreach (var physician in viewClientStatus.ListPhysicians)
                        {
                            if (physician.PhysicianName == null)
                            {
                                viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please select options under bannking or enter banking details");
                            }
                        }
                    }

                    if (viewClientStatus.IsHealthInsurnace == null)
                    {
                        foreach (var insurance in viewClientStatus.ListHealthInsurnaces)
                        {
                            if (insurance.InsuranceName == null)
                            {
                                viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please select options under Health Insurance or enter insurance details");
                            }
                        }
                    }

                    int benefitscount = 0;
                    foreach (var benefit in viewClientStatus.Benefits)
                    {
                        if (benefit.IsChecked == true)
                        {
                            benefitscount = benefitscount + 1;

                            if (benefit.Id == 1 && benefit.Rin == null)
                            {
                                viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please enter RIN number under Benefits");
                            }
                            else if (benefit.Id == 9 && benefit.Other == null)
                            {
                                viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please enter others under benefits");
                            }
                        }

                    }

                    if (benefitscount == 0)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please check atleast one under benefits");
                    }

                    if (viewClientStatus.IsMedication == null)
                    {
                        foreach (var medication in viewClientStatus.ListMedications)
                        {
                            if (medication.MedicationName == null)
                            {
                                viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please select options under medication or enter medication details");
                            }
                        }
                    }

                    if (viewClientStatus.IsRecentHospitilization == null && viewClientStatus.RecentHospitilization == null)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select option under Recent Hospitilization or explain");
                    }

                    if (viewClientStatus.EmploymentStatusId == null)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select Employment Status");
                    }

                    if (viewClientStatus.IncomeLevelId == null)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select income level");
                    }

                    if (viewClientStatus.IsBanking == null)
                    {
                        foreach (var banking in viewClientStatus.ListBankings)
                        {
                            if (banking.BankName == null)
                            {
                                viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please select options under banking or enter banking details");
                            }
                        }
                    }

                    if (viewClientStatus.IsClientHasMMS == null)
                    {
                        viewClientStatus.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select whether client is receiveing Money Management Services");
                    }



                    viewClientStatus.UserCreated = username;
                    viewClientStatus.UserUpdated = username;


                    viewClientStatus.Id = CMSService.SaveClientStatus(viewClientStatus);
                    //if (!ModelState.IsValid)
                    //{
                    //    return Redirect(Url.Action("ManageCase", "Case", new { CaseheaderId = viewClientStatus.CaseheaderId }));
                    //}
                    if (viewClientStatus.MakeSessionLive == true)
                    {
                        return RedirectToAction("EditClientStatus", "Case", new { Id = viewClientStatus.Id, CaseheaderId = viewClientStatus.CaseheaderId });
                    }

                    else
                    {
                        return Redirect(Url.Action("ManageCase", "Case", new { CaseheaderId = viewClientStatus.CaseheaderId }));
                    }
                }
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
                throw;
            }

            catch (System.Exception ex)
            {
                throw ex;
            }

            //return Redirect(Url.Action("ManageCase", "Case", new { CaseheaderId = viewClientStatus.CaseheaderId }));
            //return RedirectToAction("ManageCase", "Case", new { Id = Id });

        }

        public virtual ActionResult ViewClientStatus(int Id, int CaseheaderId)
        {
            viewClientStatus viewclientstatus = CMSService.GetClientStatus(Id);

            viewclientstatus.caselookup = getcaselookup(username, ViewBag.UserContractId);
            viewclientstatus.viewCaseHeader = new viewCaseHeader();
            viewclientstatus.viewCaseHeader = CMSService.GetCaseHeaderById(CaseheaderId);
            viewclientstatus.CaseheaderId = CaseheaderId;

            ViewBag.InterAgencies = GetInterAgencyddl();
            ViewBag.Services = GetServices();
            ViewBag.Relations = GetRelations();
            ViewBag.AilmentConfirmation = GetAilmentConfirmation();
            ViewBag.VeteranStatus = GetVeteranStatus();

            return View(viewclientstatus);
        }


        public FileResult PrintClientStatus(string html)
        {

            var pdfBytes = (new NReco.PdfGenerator.HtmlToPdfConverter()).GeneratePdf(html);

            return File(pdfBytes, System.Net.Mime.MediaTypeNames.Application.Octet, "NEwFile");
        }
        #region Helper

        protected viewClientStatus setIncompleteClientStatusErrors(viewClientStatus viewClientStatus)
        {
            if (viewClientStatus.Id > 0)
            {
                if (viewClientStatus.VeteranStatusId == null)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsVeteranStatusId = true;
                }
                if (viewClientStatus.PrimaryLanguageId == null)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsPrimaryLanguageId = true;
                }

                if (viewClientStatus.SchoolingLevelId == null)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsSchoolingLevelId = true;
                }

                int legalstatuscount = 0;
                foreach (var legalstatus in viewClientStatus.LegalStatus)
                {
                    if (legalstatus.IsChecked == true)
                    {
                        legalstatuscount = legalstatuscount + 1;
                    }
                    //if (legalstatus.IsChecked == true && legalstatus.GuardiansofEstate.Count > 0 )
                    //{
                    //    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    //    viewClientStatus.InCompleteErrors.HasErrorsLegalStatus = true;
                    //}
                }

                if (legalstatuscount == 0)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsLegalStatus = true;
                }

                if (viewClientStatus.IsOrdersOfProtection == null)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsIsOrdersOfProtection = true;
                }

                if (viewClientStatus.IsPhotos == true && viewClientStatus.PhotosDate == null)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsPhotosDate = true;
                }

                if (viewClientStatus.IsPhotos == true && viewClientStatus.IsPhotosROI == null)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsIsPhotosROI = true;
                }

                if (viewClientStatus.IsPhotosROI == "y" && (viewClientStatus.IsPhotosROIVerbal == false && viewClientStatus.IsPhotosROIWritten == false))
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsIsPhotosROIOptions = true;
                }

                if (viewClientStatus.IsCompetencyQuestioned == null)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsIsCompetencyQuestioned = true;
                }

                if (viewClientStatus.IsCompetencyQuestioned == "y" && viewClientStatus.MMSEScore == null)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsMMSEScore = true;
                }
                if (viewClientStatus.IsCompetencyQuestioned == "y" && viewClientStatus.MMSEDate == null)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsMMSEDate = true;
                }

                if (viewClientStatus.IsCompetencyQuestioned == "n" && viewClientStatus.IsCompetencyUnableReason == null)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsIsCompetencyQuestionedReason = true;
                }
                if (viewClientStatus.IsClientIncapacity == null)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsIsClientIncapacity = true;
                }

                int livingarrangementcount = 0;
                foreach (var livingarrangement in viewClientStatus.LivingArrangements)
                {
                    if (livingarrangement.IsChecked == true)
                    {
                        livingarrangementcount = livingarrangementcount + 1;
                    }
                    //if (legalstatus.IsChecked == true && legalstatus.GuardiansofEstate.Count > 0 )
                    //{
                    //    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    //    viewClientStatus.InCompleteErrors.HasErrorsLegalStatus = true;
                    //}
                }

                if (livingarrangementcount == 0)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsLivingArrangements = true;
                }

                if (viewClientStatus.IsDaytimeLocHome == false && viewClientStatus.IsDaytimeLocWorkShopSite == false && viewClientStatus.IsDaytimeLocWork == false
                    && viewClientStatus.IsDaytimeLocDayTraining == false && viewClientStatus.IsDaytimeLocAdultDayService == false && viewClientStatus.IsDaytimeLocOther == false)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsDaytimeLocation = true;
                }

                if (viewClientStatus.IsDaytimeLocWorkShopSite == true && viewClientStatus.DaytimeLocWorkShopSite == null)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsDaytimeLocationOther = true;
                }

                if (viewClientStatus.IsDaytimeLocWork == true && viewClientStatus.DaytimeLocWork == null)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsDaytimeLocationOther = true;
                }

                if (viewClientStatus.IsDaytimeLocDayTraining == true && viewClientStatus.DaytimeLocDayTraining == null)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsDaytimeLocationOther = true;
                }

                if (viewClientStatus.IsDaytimeLocAdultDayService == true && viewClientStatus.DaytimeLocAdultDayService == null)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsDaytimeLocationOther = true;
                }

                if (viewClientStatus.IsOthersInHousehold == null)
                {
                    foreach (var other in viewClientStatus.ListOthersInHousehold)
                    {
                        if (other.OthersName == null)
                        {
                            viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                            viewClientStatus.InCompleteErrors.HasErrorsIsOthersInHousehold = true;
                        }
                    }
                }

                if (viewClientStatus.IsOthersNotInHousehold == null)
                {
                    foreach (var other in viewClientStatus.ListOthersNotInHousehold)
                    {
                        if (other.OthersName == null)
                        {
                            viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                            viewClientStatus.InCompleteErrors.HasErrorsIsOthersNotInHousehold = true;
                        }
                    }
                }

                int medicalhistorycount = 0;
                foreach (var medicalhistory in viewClientStatus.MedicalHistory)
                {
                    if (medicalhistory.IsChecked == true)
                    {
                        medicalhistorycount = medicalhistorycount + 1;
                    }
                    //if (legalstatus.IsChecked == true && legalstatus.GuardiansofEstate.Count > 0 )
                    //{
                    //    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    //    viewClientStatus.InCompleteErrors.HasErrorsLegalStatus = true;
                    //}
                }

                if (medicalhistorycount == 0)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsMedicalHistory = true;
                }

                int barrierscount = 0;
                foreach (var barrier in viewClientStatus.Barriers)
                {
                    if (barrier.IsChecked == true)
                    {
                        barrierscount = barrierscount + 1;
                    }
                    //if (legalstatus.IsChecked == true && legalstatus.GuardiansofEstate.Count > 0 )
                    //{
                    //    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    //    viewClientStatus.InCompleteErrors.HasErrorsLegalStatus = true;
                    //}
                }

                if (barrierscount == 0)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsBarriers = true;
                }

                if (viewClientStatus.IsPhysician == null)
                {
                    foreach (var physician in viewClientStatus.ListPhysicians)
                    {
                        if (physician.PhysicianName == null)
                        {
                            viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                            viewClientStatus.InCompleteErrors.HasErrorsIsPhysician = true;
                        }
                    }
                }

                if (viewClientStatus.IsHealthInsurnace == null)
                {
                    foreach (var insurance in viewClientStatus.ListHealthInsurnaces)
                    {
                        if (insurance.InsuranceName == null)
                        {
                            viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                            viewClientStatus.InCompleteErrors.HasErrorsIsHealthInsurnace = true;
                        }
                    }
                }

                int benefitscount = 0;
                foreach (var benefit in viewClientStatus.Benefits)
                {
                    if (benefit.IsChecked == true)
                    {
                        benefitscount = benefitscount + 1;

                        if (benefit.Id == 1 && benefit.Rin == null)
                        {
                            viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                            viewClientStatus.InCompleteErrors.HasErrorsBenefitsRIN = true;
                        }
                        else if (benefit.Id == 9 && benefit.Other == null)
                        {
                            viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                            viewClientStatus.InCompleteErrors.HasErrorsBenefitsOther = true;
                        }
                    }

                }

                if (benefitscount == 0)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsBenefits = true;
                }

                if (viewClientStatus.IsMedication == null)
                {
                    foreach (var medication in viewClientStatus.ListMedications)
                    {
                        if (medication.MedicationName == null)
                        {
                            viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                            viewClientStatus.InCompleteErrors.HasErrorsIsMedication = true;
                        }
                    }
                }

                if (viewClientStatus.IsRecentHospitilization == null && viewClientStatus.RecentHospitilization == null)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsIsRecentHospitilization = true;
                }

                if (viewClientStatus.EmploymentStatusId == null)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsEmploymentStatusId = true;
                }

                if (viewClientStatus.IncomeLevelId == null)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsIncomeLevelId = true;
                }

                if (viewClientStatus.IsBanking == null)
                {
                    foreach (var banking in viewClientStatus.ListBankings)
                    {
                        if (banking.BankName == null)
                        {
                            viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                            viewClientStatus.InCompleteErrors.HasErrorsIsBanking = true;
                        }
                    }
                }

                if (viewClientStatus.IsClientHasMMS == null)
                {
                    viewClientStatus.InCompleteErrors.ErrorsInClientStatus = true;
                    viewClientStatus.InCompleteErrors.HasErrorsIsClientHasMMS = true;
                }
            }

            return viewClientStatus;
        }

        #endregion

        #endregion

        #region Client Assessement

        public ActionResult EditCA(int Id, int IntakeId, string mode)
        {
            ClientAssessmentModel model;

            if (Id != 0)
            {

                model = CMSService.GetCA(Id);

                model.mode = mode;


            }
            else
            {

                model = new ClientAssessmentModel();

                model.IntakeId = IntakeId;
                model.ListOfAbuseTypes = codeTable.ListOfAbuseTypes;

                model.viewAssessmentStatus = new ClientAssessment_AssessmentStatusModel();
                model.viewAssessmentStatus.ClientReceivesServices = CMSService.ListClientReceivesServices().ToList();
                model.viewIntake = new viewIntake();
                model.viewIntake = CMSService.GetIntake(IntakeId);
                model.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)model.viewIntake.CaseheaderId);
                model.CaseheaderId = model.viewIntake.viewCaseHeader.Id;

                foreach (var abuse in model.ListOfAbuseTypes)
                {
                    abuse.ListofAbusers = model.viewIntake.ListofCaseAbusers.ToList();

                }

                foreach (var abuse in model.ListOfAbuseTypes)
                {
                    foreach (var abuser in abuse.ListofAbusers)
                    {
                        //abuser.ListSubject = new List<viewSubject>();
                        if (Id > 0)
                        {
                            //var listsubject = CMSService.ListSubject(Id);

                            //foreach (var subject in listsubject)
                            //{
                            //    if (subject.AbuseSectionId == abuse.Id)
                            //    {
                            //        abuser.ListSubject.Add(subject);
                            //    }
                            //}
                        }

                    }
                }

                ViewBag.SubstantiationTypes = GetSubstantiationTypesddl();
                model.viewReportSubstantiation = new viewReportOfSubstantiation();

                model.viewReportSubstantiation.DateOfIntake = model.viewIntake.DateIntake;

                model.viewReportSubstantiation.ListofAbusers = model.viewIntake.ListofCaseAbusers.ToList();
                model.viewReportSubstantiation.ListOfAbuseTypes = codeTable.ListOfAbuseTypes;

                foreach (var abusertype in model.viewReportSubstantiation.ListOfAbuseTypes)
                {
                    abusertype.ListofAbusers = model.viewIntake.ListofCaseAbusers.ToList();
                }

            }
            model.caselookup = getcaselookup(username, ViewBag.UserContractId);

            return View(model);
        }

        [ValidateInput(false)]
        public ActionResult SaveCA(ClientAssessmentModel viewClientAssessment)
        {

            viewClientAssessment.UserCreated = username;
            viewClientAssessment.UserUpdated = username;

            if (viewClientAssessment.mode == "assessement")
            {
                if (viewClientAssessment.StatusDescription == CaseStatus.Submitted.ToString())
                {
                    viewClientAssessment.StatusDescription = CaseStatus.Submitted.ToString();
                }
                else
                {
                    viewClientAssessment.StatusDescription = CaseStatus.Open.ToString();
                }
            }
            else
            {
                if (viewClientAssessment.StatusDescription == CaseStatus.Submitted.ToString())
                {
                    viewClientAssessment.StatusDescription = CaseStatus.Submitted.ToString();
                }
                else
                {
                    viewClientAssessment.StatusDescription = CaseStatus.Incomplete.ToString();
                }
            }
            viewClientAssessment.caselookup = getcaselookup(username, ViewBag.UserContractId);

            viewClientAssessment.Id = CMSService.SaveCA(viewClientAssessment);

            if (viewClientAssessment.mode == null || viewClientAssessment.mode == "initial")
            {
                if (viewClientAssessment.isAjax == true)
                {
                    var redirectUrl = new UrlHelper(Request.RequestContext).Action("EditCA", "Case", new { Id = viewClientAssessment.Id, IntakeId = viewClientAssessment.IntakeId, mode = "assessement" });
                    var URLandId = new { url = redirectUrl, Id = viewClientAssessment.Id };
                    return Json(URLandId, JsonRequestBehavior.AllowGet);

                }
                else
                {
                    return RedirectToAction("EditCA", "Case", new { Id = viewClientAssessment.Id, IntakeId = viewClientAssessment.IntakeId, mode = "initial" });
                }
            }
            else
            {
                if (viewClientAssessment.isAjax == true)
                {

                    var redirectUrl = new UrlHelper(Request.RequestContext).Action("EditCA", "Case", new { Id = viewClientAssessment.Id, IntakeId = viewClientAssessment.IntakeId, mode = "assessement" });
                    //var redirectUrl = new UrlHelper(Request.RequestContext).Action("ManageCase", "Case", new { CaseheaderId = viewClientAssessment.viewIntake.CaseheaderId });
                    var URLandId = new { url = redirectUrl, Id = viewClientAssessment.Id };
                    return Json(URLandId, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    //return Redirect(Url.Action("ManageCase", "Case", new { CaseheaderId = viewClientAssessment.viewIntake.CaseheaderId }));
                    //var Id = viewClientAssessment.Id;
                    //return Json(Id, JsonRequestBehavior.AllowGet);

                    var redirectUrl = new UrlHelper(Request.RequestContext).Action("ManageCase", "Case", new { CaseheaderId = viewClientAssessment.viewIntake.CaseheaderId });
                    var URLandId = new { url = redirectUrl, Id = viewClientAssessment.Id };
                    return Json(URLandId, JsonRequestBehavior.AllowGet);




                }
            }

        }

        [ValidateInput(false)]
        public ActionResult ClientAssessmentPostSubmit(ClientAssessmentModel viewClientAssessment)
        {
            viewClientAssessment.UserCreated = username;
            viewClientAssessment.UserUpdated = username;
            viewClientAssessment.StatusDescription = CaseStatus.Submitted.ToString();
            viewClientAssessment.Id = CMSService.SaveCA(viewClientAssessment);

            var redirectUrl = new UrlHelper(Request.RequestContext).Action("ManageCase", "Case", new { CaseheaderId = viewClientAssessment.viewIntake.CaseheaderId });
            var URLandId = new { url = redirectUrl, Id = viewClientAssessment.Id };
            return Json(URLandId, JsonRequestBehavior.AllowGet);
        }


        [ValidateInput(false)]
        public ActionResult ClientAssessmentSubmitToExternal(ClientAssessmentModel viewClientAssessment)
        {
            viewClientAssessment.UserCreated = username;
            viewClientAssessment.UserUpdated = username;
            viewClientAssessment.StatusDescription = CaseStatus.NewReport.ToString();




            viewClientAssessment.Id = CMSService.SaveCA(viewClientAssessment);


            if (viewClientAssessment.viewReportSubstantiation.StatusId != 24)
            {
                SubmitReportOfSubstantiation(viewClientAssessment);
            }


            var redirectUrl = new UrlHelper(Request.RequestContext).Action("ManageCase", "Case", new { CaseheaderId = viewClientAssessment.viewIntake.CaseheaderId });
            var URLandId = new { url = redirectUrl, Id = viewClientAssessment.Id };





            return Json(URLandId, JsonRequestBehavior.AllowGet);
        }

        [ValidateInput(false)]
        public ActionResult ClientAssessmentPostApprove(ClientAssessmentModel viewClientAssessment)
        {
            viewClientAssessment.UserCreated = username;
            viewClientAssessment.UserUpdated = username;
            viewClientAssessment.StatusDescription = CaseStatus.Approved.ToString();
            viewClientAssessment.Id = CMSService.SaveCA(viewClientAssessment);
            viewClientAssessment.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)viewClientAssessment.viewIntake.CaseheaderId);
            var redirectUrl = new UrlHelper(Request.RequestContext).Action("ManageCase", "Case", new { CaseheaderId = viewClientAssessment.viewIntake.CaseheaderId });
            var URLandId = new { url = redirectUrl, Id = viewClientAssessment.Id };

            EmailLists emails = CMSService.GetEmails();

            try
            {
                if (viewClientAssessment.viewAssessmentStatus.ISAbuserAgingEmployee == true)
                {
                    List<MailAddress> EMails = new List<MailAddress>();

                    string Message = CMSService.GetContract(contractids.FirstOrDefault()).ContractName + " has submitted " + viewClientAssessment.viewIntake.viewCaseHeader.Client.Person.FirstName + " " + viewClientAssessment.viewIntake.viewCaseHeader.Client.Person.LastName + " Record for the review related to the abuser being an aging network employee";
                    string Subject = "Network Employee Record Review";

                    foreach(var i in emails.IDoAEmails)
                    {
                        EMails.Add(new MailAddress(i, "To"));
                    }

                    foreach (var e in emails.BCCEmails)
                    {
                        EMails.Add(new MailAddress(e, "BCC"));
                    }

                    SendEmail(EMails, Message, Subject);
                }

                if (viewClientAssessment.viewAssessmentStatus.IsReferAbuserToIDOA == "y")
                {

                    List<MailAddress> EMails = new List<MailAddress>();
                    var agencyName = CMSService.GetContract(contractids.FirstOrDefault()).ContractName;
                    string Message = "A verified APS case has been submitted by (APS PA:" + agencyName + " ) , Client Named(" + viewClientAssessment.viewIntake.viewCaseHeader.Client.Person.FirstName + " " + viewClientAssessment.viewIntake.viewCaseHeader.Client.Person.LastName + ") with Intake Date (" + viewClientAssessment.viewIntake.DateIntake + ") for IDoA Registry review.";
                    string Subject = "Registry Review";

                    foreach (var i in emails.IDoAEmails)
                    {
                        EMails.Add(new MailAddress(i, "To"));


                    }

                    foreach (var e in emails.BCCEmails)
                    {
                        EMails.Add(new MailAddress(e, "BCC"));
                    }

                    SendEmail(EMails, Message, Subject);
                }

                //Commented out below by LMD, 9-20-19. Have no idea why an email is being sent on CA Approval. 

                #region Submitting Email
                //List<MailAddress> NewEMails = new List<MailAddress>();
                //string MessageNew = "You have received a Report Of Substantiation (Id " + viewClientAssessment.Id + ") from <u>" + CMSService.GetContract(contractids.FirstOrDefault()).ContractName + "</u>";
                //string SubjectNew = "Report Of Substantiation";

                //if (viewClientAssessment.viewReportSubstantiation.IsDDD == true)
                //{


                //    foreach(var e in emails.DDDEmails)
                //    {
                //        NewEMails.Add(new MailAddress(e, "To"));
                //    }

                //    foreach(var e in emails.HFSEmails)
                //    {
                //        NewEMails.Add(new MailAddress(e, "CC"));
                //    }

                //    foreach (var e in emails.BCCEmails)
                //    {
                //        NewEMails.Add(new MailAddress(e, "BCC"));
                //    }



                //    SendEmail(NewEMails, MessageNew, SubjectNew);
                //}
                //if (viewClientAssessment.viewReportSubstantiation.IsDRS == true)
                //{
                //    NewEMails = new List<MailAddress>();


                //    foreach (var e in emails.DRSEmails)
                //    {
                //        NewEMails.Add(new MailAddress(e, "To"));
                //    }

                //    foreach (var e in emails.HFSEmails)
                //    {
                //        NewEMails.Add(new MailAddress(e, "CC"));
                //    }

                //    foreach (var e in emails.BCCEmails)
                //    {
                //        NewEMails.Add(new MailAddress(e, "BCC"));
                //    }

                //    SendEmail(NewEMails, MessageNew, SubjectNew);
                //}
                //if (viewClientAssessment.viewReportSubstantiation.IsCCP == true)
                //{
                //    NewEMails = new List<MailAddress>();

                //    foreach (var e in emails.CCPEmails)
                //    {
                //        NewEMails.Add(new MailAddress(e, "To"));
                //    }

                //    foreach (var e in emails.HFSEmails)
                //    {
                //        NewEMails.Add(new MailAddress(e, "CC"));
                //    }


                //    foreach (var e in emails.BCCEmails)
                //    {
                //        NewEMails.Add(new MailAddress(e, "BCC"));
                //    }


                //    SendEmail(NewEMails, MessageNew, SubjectNew);
                //}
                //if (viewClientAssessment.viewReportSubstantiation.IsDSCC == true)
                //{
                //    NewEMails = new List<MailAddress>();

                //    foreach (var e in emails.DSCCEmails)
                //    {
                //        NewEMails.Add(new MailAddress(e, "To"));
                //    }

                //    foreach (var e in emails.BCCEmails)
                //    {
                //        NewEMails.Add(new MailAddress(e, "BCC"));
                //    }

                //    SendEmail(NewEMails, MessageNew, SubjectNew);
                //}
                //if (viewClientAssessment.viewReportSubstantiation.IsMCO == true && viewClientAssessment.viewReportSubstantiation.MCOId != 0)
                //{

                //    NewEMails = new List<MailAddress>();

                //    NewEMails.Add(new MailAddress(codeTable.ListOfMCOs.Where(i => i.Id == viewClientAssessment.viewReportSubstantiation.MCOId).FirstOrDefault().Email, "To"));


                //    foreach (var e in emails.HFSEmails)
                //    {
                //        NewEMails.Add(new MailAddress(e, "CC"));
                //    }

                //    foreach (var e in emails.BCCEmails)
                //    {
                //        NewEMails.Add(new MailAddress(e, "BCC"));
                //    }

                //    SendEmail(NewEMails, MessageNew, SubjectNew);
                //}

                #endregion
            }
            catch (Exception)
            {

                return Json(URLandId, JsonRequestBehavior.AllowGet);
            }

            return Json(URLandId, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ViewClientAssessementNew(int Id, int IntakeId)
        {
            ClientAssessmentModel viewClientAssessment;


            viewClientAssessment = CMSService.GetCA(Id);

            //viewClientAssessment.mode = "summary";

            viewClientAssessment.caselookup = getcaselookup(username, ViewBag.UserContractId);

            viewClientAssessment.viewIntake = new viewIntake();

            viewClientAssessment.viewIntake = CMSService.GetIntake(IntakeId);
            viewClientAssessment.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)viewClientAssessment.viewIntake.CaseheaderId);
            viewClientAssessment.CaseheaderId = viewClientAssessment.viewIntake.viewCaseHeader.Id;


            return View("ViewClientAssessementNew", viewClientAssessment);
        }

        public ActionResult SaveReportOfSubstantiationAjax(ClientAssessmentModel viewClientAssessment)
        {

            viewClientAssessment.viewReportSubstantiation.StatusDescription = CaseStatus.Open.ToString();

            viewClientAssessment.viewReportSubstantiation.UserCreated = username;
            viewClientAssessment.viewReportSubstantiation.UserUpdated = username;
            viewClientAssessment.viewReportSubstantiation.ClientAssessmentId = viewClientAssessment.Id;

            if (viewClientAssessment.viewReportSubstantiation.Id != 0)
            {
                //viewClientAssessment.viewReportSubstantiation.StatusDescription = CaseStatus.Responded.ToString();

                if (viewClientAssessment.viewReportSubstantiation.IsMedicaid == null)
                {
                    viewClientAssessment.viewReportSubstantiation.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select whether Medicaid/Non-Medicaid/Unknown");
                }

                if (viewClientAssessment.viewReportSubstantiation.ClassificationId == 0)
                {
                    viewClientAssessment.viewReportSubstantiation.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select Substantiation Decision");
                }
                if (viewClientAssessment.viewReportSubstantiation.DateofSubstantiation == null)
                {
                    viewClientAssessment.viewReportSubstantiation.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select Date of Substantiation Decision");
                }
                if (viewClientAssessment.viewReportSubstantiation.CompletedBy == null)
                {
                    viewClientAssessment.viewReportSubstantiation.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please enter who completed the Report of Substantiation");
                }
                if (viewClientAssessment.viewReportSubstantiation.Email == null)
                {
                    viewClientAssessment.viewReportSubstantiation.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please enter email");
                }
                if (viewClientAssessment.viewReportSubstantiation.AgencyName == null)
                {
                    viewClientAssessment.viewReportSubstantiation.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select enter APS PA");
                }

            }

            CAIds caIds = new CAIds();
            caIds = CMSService.SaveReportOfSubstantiationNew(viewClientAssessment.viewReportSubstantiation);

            return Json(caIds, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult SubmitReportOfSubstantiation(ClientAssessmentModel viewClientAssessment)
        {
            viewClientAssessment.viewReportSubstantiation.UserCreated = username;
            viewClientAssessment.viewReportSubstantiation.UserUpdated = username;
            viewClientAssessment.viewReportSubstantiation.UserSubmitted = username;
            viewClientAssessment.viewReportSubstantiation.DateSubmitted = DateTime.Now;
            viewClientAssessment.viewReportSubstantiation.ClientAssessmentId = viewClientAssessment.Id;

            viewClientAssessment.viewReportSubstantiation.StatusDescription = CaseStatus.Submitted.ToString();


            EmailLists emails = CMSService.GetEmails();

            //viewClientAssessment.mode = "Notice";

           //var ReportOfSubstantiationId = CMSService.SaveReportOfSubstantiationNew(viewClientAssessment.viewReportSubstantiation);
           CAIds getIds = CMSService.SaveReportOfSubstantiationNew(viewClientAssessment.viewReportSubstantiation);

            List<int> PreviousIds = CMSService.GetPreviousROSIds(Convert.ToInt32(getIds.ROSId));

            List<MailAddress> EMails = new List<MailAddress>();

            string Message = "You have received a Report Of Substantiation (Id: " + getIds.ROSId + " from <u>" + CMSService.GetContract(contractids.FirstOrDefault()).ContractName + "</u>";



            if (PreviousIds != null && PreviousIds.Any())
            {
                Message = Message + "This Report of Substantiation was previously submitted with the following Id(s): ";
                for (int i = 0; i < PreviousIds.Count; i++)
                {
                    if (i == PreviousIds.Count - 1)
                    {
                        Message = Message + PreviousIds[i] + ".";
                    }
                    else
                    {
                        Message = Message + PreviousIds[i] + ", ";
                    }
                }
            }

            string Subject;

            if (viewClientAssessment.viewReportSubstantiation.IsDDD == true)
            {
                Subject = "Report Of Substantiation" + " - DDD";

                foreach (var e in emails.DDDEmails)
                {
                    EMails.Add(new MailAddress(e, "To"));
                }

                foreach (var e in emails.HFSEmails)
                {
                    EMails.Add(new MailAddress(e, "CC"));
                }

                foreach (var e in emails.BCCEmails)
                {
                    EMails.Add(new MailAddress(e, "BCC"));
                }

                SendEmail(EMails, Message, Subject);
            }
            if (viewClientAssessment.viewReportSubstantiation.IsDRS == true)
            {
                Subject = "Report Of Substantiation" + " - DRS";
                EMails.Clear();


                foreach (var e in emails.DRSEmails)
                {
                    EMails.Add(new MailAddress(e, "To"));
                }

                foreach (var e in emails.HFSEmails)
                {
                    EMails.Add(new MailAddress(e, "CC"));
                }

                foreach (var e in emails.BCCEmails)
                {
                    EMails.Add(new MailAddress(e, "BCC"));
                }

                SendEmail(EMails, Message, Subject);
            }
            if (viewClientAssessment.viewReportSubstantiation.IsCCP == true)
            {

                Subject = "Report Of Substantiation" + " - CCP";
                EMails.Clear();


                foreach (var e in emails.CCPEmails)
                {
                    EMails.Add(new MailAddress(e, "To"));
                }

                foreach (var e in emails.HFSEmails)
                {
                    EMails.Add(new MailAddress(e, "CC"));
                }


                foreach (var e in emails.BCCEmails)
                {
                    EMails.Add(new MailAddress(e, "BCC"));
                }

                SendEmail(EMails, Message, Subject);
            }
            if (viewClientAssessment.viewReportSubstantiation.IsDSCC == true)
            {
                Subject = "Report Of Substantiation" + " - DSCC";
                EMails.Clear();


                foreach (var e in emails.DSCCEmails)
                {
                    EMails.Add(new MailAddress(e, "To"));
                }

                foreach (var e in emails.BCCEmails)
                {
                    EMails.Add(new MailAddress(e, "BCC"));
                }

                SendEmail(EMails, Message, Subject);
            }
            if (viewClientAssessment.viewReportSubstantiation.IsMCO == true)
            {
                Subject = "Report Of Substantiation" + " - MCO";
                EMails.Clear();
                EMails.Add(new MailAddress(codeTable.ListOfMCOs.Where(i => i.Id == viewClientAssessment.viewReportSubstantiation.MCOId).FirstOrDefault().Email, "To"));
                foreach (var e in emails.HFSEmails)
                {
                    EMails.Add(new MailAddress(e, "CC"));
                }


                foreach (var e in emails.BCCEmails)
                {
                    EMails.Add(new MailAddress(e, "BCC"));
                }
                SendEmail(EMails, Message, Subject);
            }

            return Json(getIds, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Case Closure

        public virtual ActionResult EditCaseClosure(int Id, int CaseheaderId)
        {

            viewCaseClosure viewcaseclosure;

            if (Id != 0)
            {
                viewcaseclosure = CMSService.GetCaseClosure(Id);
            }

            else
            {

                viewcaseclosure = new viewCaseClosure();

                //viewclient.viewcaseclosure.ClientClosureInfo.LegalStatus = CMSService.ListOfLegalStatus().ToList();
                viewcaseclosure.ClientClosureInfo.Services = codeTable.ListOfServices;


                viewcaseclosure.ListAbusers = CMSService.ListOfSubstantiatedAbusers(CaseheaderId).ToList();

                foreach (var abuser in viewcaseclosure.ListAbusers)
                {
                    abuser.Services = codeTable.ListOfServices;
                    abuser.LegalStatus = codeTable.ListOfLegalStatus;
                    abuser.LegalRemedies = codeTable.ListOfLegalRemedies;
                    abuser.Outcomes = codeTable.ListOfJudgeOutcomes;
                }

                viewcaseclosure.ClientClosureInfo.LegalStatus = codeTable.ListOfLegalStatus;

                viewLegalStatusHelper helper = new viewLegalStatusHelper();

                for (int i = 0; i < viewcaseclosure.ClientClosureInfo.LegalStatus.Count; i++)
                {
                    viewcaseclosure.ClientClosureInfo.LegalStatus[i].GuardiansofEstate.Add(helper);
                    viewcaseclosure.ClientClosureInfo.LegalStatus[i].GuardiansofPerson.Add(helper);
                    viewcaseclosure.ClientClosureInfo.LegalStatus[i].POAHealthCare.Add(helper);
                    viewcaseclosure.ClientClosureInfo.LegalStatus[i].POAFinances.Add(helper);
                    viewcaseclosure.ClientClosureInfo.LegalStatus[i].RepresentativePayee.Add(helper);
                    viewcaseclosure.ClientClosureInfo.LegalStatus[i].GuardianNonSpecific.Add(helper);
                }

            }



            viewcaseclosure.caselookup = getcaselookup(username, ViewBag.UserContractId);
            viewcaseclosure.viewCaseHeader = new viewCaseHeader();
            viewcaseclosure.viewCaseHeader = CMSService.GetCaseHeaderById(CaseheaderId);

            viewcaseclosure.CaseheaderId = viewcaseclosure.viewCaseHeader.Id;

            return View(setIncompleteCaseClosureErrors(viewcaseclosure));
        }

        [HttpPost]
        public virtual ActionResult SaveCaseClosure(viewCaseClosure viewcaseclosure)
        {

            if (!ModelState.IsValid)
            {
                return RedirectToAction("EditCaseClosure", new { Id = viewcaseclosure.Id, CaseheaderId = viewcaseclosure.CaseheaderId });
            }

            else
            {
                if (viewcaseclosure.IsAjax == true)
                {
                    viewcaseclosure.StatusDescription = CaseStatus.Submitted.ToString();
                }
                else
                {
                    viewcaseclosure.StatusDescription = CaseStatus.Open.ToString();

                }

                if (viewcaseclosure.ClientClosureInfo.DateCaseClosure == null)
                {
                    viewcaseclosure.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Case Closure Date is required");
                }

                if (viewcaseclosure.ClientClosureInfo.ClosureReasonId == 0)
                {
                    viewcaseclosure.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select Case Closure Reason");
                }

                if (viewcaseclosure.ClientClosureInfo.LivingArrangementId == 0)
                {
                    viewcaseclosure.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select living setting at close");
                }

                var hasLegalStatusChecked = false;
                for (var i = 0; i < viewcaseclosure.ClientClosureInfo.LegalStatus.Count; i++)
                {
                    if (viewcaseclosure.ClientClosureInfo.LegalStatus[i].IsChecked == true)
                    {
                        hasLegalStatusChecked = true;
                        break;
                    }
                }
                if (hasLegalStatusChecked == false)
                {
                    viewcaseclosure.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select atleast one substitute decision maker at close");
                }

                var hasServicesChecked = false;
                for (var i = 0; i < viewcaseclosure.ClientClosureInfo.Services.Count; i++)
                {
                    if (viewcaseclosure.ClientClosureInfo.Services[i].IsChecked == true)
                    {
                        hasServicesChecked = true;
                        break;
                    }
                }
                if (hasServicesChecked == false)
                {
                    viewcaseclosure.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select atleast one service at close");
                }


                if (viewcaseclosure.ClientClosureInfo.IsLivingWithAbuser == null)
                {
                    viewcaseclosure.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select whether client is living with Abuser or not");
                }


                foreach (var abuser in viewcaseclosure.ListAbusers)
                {
                    if (abuser.IsEmploymentTerminated == null)
                    {
                        viewcaseclosure.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select whether abuser employment terminated or not");
                    }
                    if (abuser.AAAssociationsId == 0)
                    {
                        viewcaseclosure.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select Abuser Assosciation Id");
                    }

                    if (abuser.IsReferredToCountyStatesAttorney == null)
                    {
                        viewcaseclosure.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select whether client's case is referred to County States Attorney or not");
                    }
                    if (abuser.IsClientsGuardian == null)
                    {
                        viewcaseclosure.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select whether the notification to Probate judge completed or not");
                    }

                    var hasAbuserLegalStatusChecked = false;
                    for (var i = 0; i < abuser.LegalStatus.Count; i++)
                    {
                        if (abuser.LegalStatus[i].IsChecked == true)
                        {
                            hasAbuserLegalStatusChecked = true;
                            break;
                        }
                    }
                    if (hasAbuserLegalStatusChecked == false)
                    {
                        viewcaseclosure.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select atleast one Subsititute Decision Maker at close");
                    }

                    var hasAbuserServicesChecked = false;
                    for (var i = 0; i < abuser.Services.Count; i++)
                    {
                        if (abuser.Services[i].IsChecked == true)
                        {
                            hasAbuserServicesChecked = true;
                            break;
                        }
                    }
                    if (hasAbuserServicesChecked == false)
                    {
                        viewcaseclosure.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select atleast one Service at close");
                    }

                    var hasAbuserLegalRemediesChecked = false;
                    for (var i = 0; i < abuser.Services.Count; i++)
                    {
                        if (abuser.Services[i].IsChecked == true)
                        {
                            hasAbuserLegalRemediesChecked = true;
                            break;
                        }
                    }
                    if (hasAbuserLegalRemediesChecked == false)
                    {
                        viewcaseclosure.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please select atleast one legal remedy at close");
                    }
                }




                viewcaseclosure.UserCreated = username;
                viewcaseclosure.UserUpdated = username;


                int caseclosureid = CMSService.SaveCaseClosure(viewcaseclosure);

                if (viewcaseclosure.IsAjax == true)
                {
                    var redirectUrl = new UrlHelper(Request.RequestContext).Action("ManageCase", "Case", new { CaseheaderId = viewcaseclosure.CaseheaderId });
                    var URLandId = new { url = redirectUrl, Id = caseclosureid };
                    return Json(URLandId, JsonRequestBehavior.AllowGet);
                }
                else
                {

                    return Redirect(Url.Action("ManageCase", "Case", new { CaseheaderId = viewcaseclosure.CaseheaderId }));
                }
            }

        }

        [HttpPost]
        public virtual ActionResult ApproveandCloseCaseClosure(AGE.CMS.Data.Models.Case.viewCaseClosure viewcaseclosure)
        {

            viewcaseclosure.StatusDescription = CaseStatus.Closed.ToString();
            viewcaseclosure.UserCreated = username;
            viewcaseclosure.UserUpdated = username;

            CMSService.SaveCaseClosure(viewcaseclosure);
            var ListOfErrors = CMSService.ValidateCaseClosing((int)viewcaseclosure.CaseheaderId);
            var isbillingcaseexist = APSCaseService.CaseExists((int)viewcaseclosure.viewCaseHeader.ClientId, CaseStatus.Open.ToString());
            if (isbillingcaseexist)
            {
                ListOfErrors.Add("Please close Billing Case");
            }
            if (ListOfErrors.Count > 0)
            {
                return Json(ListOfErrors, JsonRequestBehavior.AllowGet);
            }
            else
            {
                CMSService.CloseCase((int)viewcaseclosure.CaseheaderId, username);



                var redirectUrl = new UrlHelper(Request.RequestContext).Action("ManageCase", "Case", new { CaseheaderId = viewcaseclosure.CaseheaderId });
                var URLandId = new { url = redirectUrl, hell = 12 };
                return Json(URLandId, JsonRequestBehavior.AllowGet);


            }

            //return Json(viewclient, JsonRequestBehavior.AllowGet);

        }


        public ActionResult ViewCaseClosure(int Id, int CaseheaderId)
        {
            viewCaseClosure viewcaseclosure = CMSService.GetCaseClosure(Id);
            viewcaseclosure.caselookup = getcaselookup(username, ViewBag.UserContractId);
            viewcaseclosure.viewCaseHeader = new viewCaseHeader();
            viewcaseclosure.viewCaseHeader = CMSService.GetCaseHeaderById(CaseheaderId);
            viewcaseclosure.CaseheaderId = viewcaseclosure.viewCaseHeader.Id;



            return View(viewcaseclosure);
        }

        #region Helper

        protected viewCaseClosure setIncompleteCaseClosureErrors(viewCaseClosure viewcaseclosure)
        {
            if (viewcaseclosure.Id > 0)
            {
                if (viewcaseclosure.ClientClosureInfo.DateCaseClosure == null)
                {
                    viewcaseclosure.InCompleteErrors.ErrorsInCaseClosure = true;
                    viewcaseclosure.InCompleteErrors.HasErrorsDateCaseClosure = true;
                }

                if (viewcaseclosure.ClientClosureInfo.ClosureReasonId == 0)
                {
                    viewcaseclosure.InCompleteErrors.ErrorsInCaseClosure = true;
                    viewcaseclosure.InCompleteErrors.HasErrorsClosureReasonId = true;
                }

                if (viewcaseclosure.ClientClosureInfo.LivingArrangementId == 0)
                {
                    viewcaseclosure.InCompleteErrors.ErrorsInCaseClosure = true;
                    viewcaseclosure.InCompleteErrors.HasErrorsLivingArrangementId = true;
                }

                var hasLegalStatusChecked = false;
                for (var i = 0; i < viewcaseclosure.ClientClosureInfo.LegalStatus.Count; i++)
                {
                    if (viewcaseclosure.ClientClosureInfo.LegalStatus[i].IsChecked == true)
                    {
                        hasLegalStatusChecked = true;
                        break;
                    }
                }
                if (hasLegalStatusChecked == false)
                {
                    viewcaseclosure.InCompleteErrors.ErrorsInCaseClosure = true;
                    viewcaseclosure.InCompleteErrors.HasErrorsClientLegalStatus = true;
                }

                var hasServicesChecked = false;
                for (var i = 0; i < viewcaseclosure.ClientClosureInfo.Services.Count; i++)
                {
                    if (viewcaseclosure.ClientClosureInfo.Services[i].IsChecked == true)
                    {
                        hasServicesChecked = true;
                        break;
                    }
                }
                if (hasServicesChecked == false)
                {
                    viewcaseclosure.InCompleteErrors.ErrorsInCaseClosure = true;
                    viewcaseclosure.InCompleteErrors.HasErrorsClientServices = true;
                }


                if (viewcaseclosure.ClientClosureInfo.IsLivingWithAbuser == null)
                {
                    viewcaseclosure.InCompleteErrors.ErrorsInCaseClosure = true;
                    viewcaseclosure.InCompleteErrors.HasErrorsIsLivingWithAbuser = true;
                }


                foreach (var abuser in viewcaseclosure.ListAbusers)
                {
                    if (abuser.IsEmploymentTerminated == null)
                    {
                        viewcaseclosure.InCompleteErrors.ErrorsInCaseClosure = true;
                        viewcaseclosure.InCompleteErrors.HasErrorsIsEmploymentTerminated = true;
                    }
                    if (abuser.AAAssociationsId == 0)
                    {
                        viewcaseclosure.InCompleteErrors.ErrorsInCaseClosure = true;
                        viewcaseclosure.InCompleteErrors.HasErrorsAAAssociationsId = true;
                    }

                    if (abuser.IsReferredToCountyStatesAttorney == null)
                    {
                        viewcaseclosure.InCompleteErrors.ErrorsInCaseClosure = true;
                        viewcaseclosure.InCompleteErrors.HasErrorsIsReferredToCountyStatesAttorney = true;
                    }
                    if (abuser.IsClientsGuardian == null)
                    {
                        viewcaseclosure.InCompleteErrors.ErrorsInCaseClosure = true;
                        viewcaseclosure.InCompleteErrors.HasErrorsIsClientsGuardian = true;
                    }

                    var hasAbuserLegalStatusChecked = false;
                    for (var i = 0; i < abuser.LegalStatus.Count; i++)
                    {
                        if (abuser.LegalStatus[i].IsChecked == true)
                        {
                            hasAbuserLegalStatusChecked = true;
                            break;
                        }
                    }
                    if (hasAbuserLegalStatusChecked == false)
                    {
                        viewcaseclosure.InCompleteErrors.ErrorsInCaseClosure = true;
                        viewcaseclosure.InCompleteErrors.HasErrorsLegalStatus = true;
                    }

                    var hasAbuserServicesChecked = false;
                    for (var i = 0; i < abuser.Services.Count; i++)
                    {
                        if (abuser.Services[i].IsChecked == true)
                        {
                            hasAbuserServicesChecked = true;
                            break;
                        }
                    }
                    if (hasAbuserServicesChecked == false)
                    {
                        viewcaseclosure.InCompleteErrors.ErrorsInCaseClosure = true;
                        viewcaseclosure.InCompleteErrors.HasErrorsAbuserServices = true;
                    }

                    var hasAbuserLegalRemediesChecked = false;
                    for (var i = 0; i < abuser.Services.Count; i++)
                    {
                        if (abuser.Services[i].IsChecked == true)
                        {
                            hasAbuserLegalRemediesChecked = true;
                            break;
                        }
                    }
                    if (hasAbuserLegalRemediesChecked == false)
                    {
                        viewcaseclosure.InCompleteErrors.ErrorsInCaseClosure = true;
                        viewcaseclosure.InCompleteErrors.HasErrorsLegalRemedies = true;
                    }
                }



            }

            return viewcaseclosure;
        }

        #endregion

        #endregion

        #region Reconciliation

        public ActionResult EditReconciliation(int Id, int CaseheaderId)
        {


            viewReconciliation viewReconciliation;

            if (Id != 0)
            {
                viewReconciliation = CMSService.GetReconciliation(Id);
            }
            else
            {
                viewReconciliation = new viewReconciliation();

                viewReconciliation.ClientId = Id;
                //viewReconciliation.DateIntake = CMSService.GetIntake((int)IntakeId).DateIntake.GetValueOrDefault();
                viewReconciliation.ReconciliationVouchers = CMSService.ListOfReconciliationVoucherTypes().ToList();
            }

            viewReconciliation.caselookup = getcaselookup(username, ViewBag.UserContractId);
            viewReconciliation.viewCaseHeader = new viewCaseHeader();
            viewReconciliation.viewCaseHeader = CMSService.GetCaseHeaderById(CaseheaderId);
            //viewclient.viewReconciliation.ReconciliationVouchers = GetCaseRecordingTypesddl(false);

            ViewBag.IntakeDates = GetIntakeDates(viewReconciliation.viewCaseHeader.ListofIntakeForms);
            return View(setIncompleteReconciliationErrors(viewReconciliation));
        }

        [HttpPost]
        public ActionResult SaveReconciliation(viewReconciliation viewReconciliation)
        {

            if (!ModelState.IsValid)
            {
                return RedirectToAction("EditReconciliation", new { Id = viewReconciliation.Id, CaseheaderId = viewReconciliation.CaseheaderId });
            }

            else
            {

                viewReconciliation.StatusDescription = CaseStatus.Open.ToString();
                if (viewReconciliation.DateFirstFTF == null)
                {
                    viewReconciliation.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please enter Status Date");
                }
                if (viewReconciliation.DateBill == null)
                {
                    viewReconciliation.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please enter Bill Date");
                }

                if (viewReconciliation.NeedForReconciliation == null)
                {
                    viewReconciliation.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please explain need for Reconciliation");
                }

                int count = 0;
                foreach (var voucher in viewReconciliation.ReconciliationVouchers)
                {
                    if (voucher.IsChecked == true)
                    {
                        count = count + 1;
                    }
                    if (voucher.IsChecked == true && voucher.VoucherAmount == 0)
                    {
                        viewReconciliation.StatusDescription = CaseStatus.Incomplete.ToString();
                        ModelState.AddModelError("CustomError", "Please enter voucher amount");
                    }
                }

                if (count == 0)
                {
                    viewReconciliation.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select atleast one voucher");
                }

                if (viewReconciliation.SupervisorSignature == null)
                {
                    viewReconciliation.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Supervisor - Please sign");
                }

                if (viewReconciliation.SupervisorSignatureDate == null)
                {
                    viewReconciliation.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Supervisor - Pleass enter date");
                }

                viewReconciliation.UserCreated = username;
                viewReconciliation.UserUpdated = username;

                int reconciliationId = CMSService.SaveReconciliation(viewReconciliation);

                return Redirect(Url.Action("ManageCase", "Case", new { CaseheaderId = viewReconciliation.CaseheaderId }));
            }
            //return RedirectToAction("ManageCase", "Case", new { Id = Id });
        }

        public ActionResult ViewReconciliation(int Id, int CaseheaderId)
        {

            viewReconciliation viewReconciliation = CMSService.GetReconciliation(Id);
            viewReconciliation.viewCaseHeader = new viewCaseHeader();
            viewReconciliation.viewCaseHeader = CMSService.GetCaseHeaderById(CaseheaderId);

            return View(viewReconciliation);
        }

        #region Helper

        protected viewReconciliation setIncompleteReconciliationErrors(viewReconciliation viewReconciliation)
        {
            if (viewReconciliation.Id > 0)
            {
                if (viewReconciliation.DateFirstFTF == null)
                {
                    viewReconciliation.InCompleteErrors.ErrorsinReconciliation = true;
                    viewReconciliation.InCompleteErrors.HasErrorsDateFTF = true;
                }
                if (viewReconciliation.DateBill == null)
                {
                    viewReconciliation.InCompleteErrors.ErrorsinReconciliation = true;
                    viewReconciliation.InCompleteErrors.HasErrorsIsDateBill = true;
                }

                if (viewReconciliation.NeedForReconciliation == null)
                {
                    viewReconciliation.InCompleteErrors.ErrorsinReconciliation = true;
                    viewReconciliation.InCompleteErrors.HasErrorsNeedForReconciliation = true;
                }

                int count = 0;
                foreach (var voucher in viewReconciliation.ReconciliationVouchers)
                {
                    if (voucher.IsChecked == true)
                    {
                        count = count + 1;
                    }
                    if (voucher.IsChecked == true && voucher.VoucherAmount == 0)
                    {
                        viewReconciliation.InCompleteErrors.ErrorsinReconciliation = true;
                        viewReconciliation.InCompleteErrors.HasErrorsVoucherAmount = true;
                    }
                }

                if (count == 0)
                {
                    viewReconciliation.InCompleteErrors.ErrorsinReconciliation = true;
                    viewReconciliation.InCompleteErrors.HasErrorsVoucher = true;
                }

                if (viewReconciliation.SupervisorSignature == null)
                {
                    viewReconciliation.InCompleteErrors.ErrorsinReconciliation = true;
                    viewReconciliation.InCompleteErrors.HasErrorsSupervisorSignature = true;
                }

                if (viewReconciliation.SupervisorSignatureDate == null)
                {
                    viewReconciliation.InCompleteErrors.ErrorsinReconciliation = true;
                    viewReconciliation.InCompleteErrors.HasErrorsSupervisorSignatureDate = true;
                }


            }

            return viewReconciliation;
        }

        #endregion


        #endregion

        #region Record Release or Subpoena

        public virtual ActionResult EditRecordRelease(int Id, int CaseheaderId)
        {

            viewRecordRelease viewRecordRelease;

            if (Id != 0)
            {
                viewRecordRelease = CMSService.GetRecordRelease(Id);
                //viewRecordRelease.DateIntake = viewRecordRelease.DateIntake.Value.ToShortDateString();
                viewRecordRelease.InCompleteErrors.ErrorsInRelease = false;
            }
            else
            {
                viewRecordRelease = new viewRecordRelease();

                viewRecordRelease.ClientId = Id;
                //viewRecordRelease.DateIntake = CMSService.GetIntake((int)IntakeId).DateIntake.GetValueOrDefault();
                viewRecordRelease.InCompleteErrors.ErrorsInRelease = false;

            }

            viewRecordRelease.caselookup = getcaselookup(username, ViewBag.UserContractId);
            viewRecordRelease.viewCaseHeader = new viewCaseHeader();
            viewRecordRelease.viewCaseHeader = CMSService.GetCaseHeaderById(CaseheaderId);

            viewRecordRelease.CaseheaderId = viewRecordRelease.viewCaseHeader.Id;

            ViewBag.IntakeDates = GetIntakeDates(viewRecordRelease.viewCaseHeader.ListofIntakeForms);

            return View(setIncompleteReleaseRecordErrors(viewRecordRelease));
        }

        [HttpPost]
        public ActionResult SaveRecordRelease(viewRecordRelease viewRecordRelease)
        {


            if (!ModelState.IsValid)
            {
                return RedirectToAction("EditRecordRelease", new { Id = viewRecordRelease.Id, CaseheaderId = viewRecordRelease.CaseheaderId });
            }

            else
            {

                viewRecordRelease.StatusDescription = CaseStatus.Open.ToString();

                if (viewRecordRelease.DateIntake == null)
                {
                    viewRecordRelease.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Intake Date is required.");
                }
                if (viewRecordRelease.RecordReleaseTypeId == 0)
                {
                    viewRecordRelease.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select type of Record Release");
                }
                if (viewRecordRelease.RecordReleaseRequestReceiverId == 0)
                {
                    viewRecordRelease.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select type of agency release request recevied");
                }
                if (viewRecordRelease.DateRequestReceived == Convert.ToDateTime("1/1/0001 12:00:00 AM"))
                {
                    viewRecordRelease.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Date of Record request is required.");
                }
                if (viewRecordRelease.RecordReleaseRequestReceivedById == 0)
                {
                    viewRecordRelease.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select type of request for release received by");
                }
                if (viewRecordRelease.RecordReleaseRequestorId == 0)
                {
                    viewRecordRelease.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select type of Release Requestor");
                }
                if (viewRecordRelease.IsRecordReleaseRequestGranted == null)
                {
                    viewRecordRelease.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select Yes/ No for release of requested granted or not");
                }
                if (viewRecordRelease.DateRecordReleaseRequestGranted == Convert.ToDateTime("1/1/0001 12:00:00 AM"))
                {
                    viewRecordRelease.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Date of record released is required");
                }

                if (viewRecordRelease.RecordReleaseRequestCompletedId == 0)
                {
                    viewRecordRelease.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please select type of agency record release completed by");
                }

                if (viewRecordRelease.ReleasePreparedBy == null)
                {
                    viewRecordRelease.StatusDescription = CaseStatus.Incomplete.ToString();
                    ModelState.AddModelError("CustomError", "Please enter person name who prepared release");
                }


                viewRecordRelease.UserCreated = username;
                viewRecordRelease.UserUpdated = username;

                int recordreleaseId = CMSService.SaveRecordRelease(viewRecordRelease);
                return Redirect(Url.Action("ManageCase", "Case", new { CaseheaderId = viewRecordRelease.CaseheaderId }));
            }


        }

        public ActionResult ViewRecordRelease(int Id, int CaseheaderId)
        {

            viewRecordRelease viewRecordRelease = CMSService.GetRecordRelease(Id);
            viewRecordRelease.viewCaseHeader = new viewCaseHeader();
            viewRecordRelease.viewCaseHeader = CMSService.GetCaseHeaderById(CaseheaderId);
            viewRecordRelease.caselookup = getcaselookup(username, ViewBag.UserContractId);
            return View(viewRecordRelease);
        }

        #region Redacted Releases

        public ActionResult FileUpload()
        {
            List<viewRedactedReleases> listofdocs = new List<viewRedactedReleases>();

            return View();
        }

        [HttpPost]
        public ActionResult UploadRedactedReleases(viewCaseHeader model)
        {
            int caseheaderid = 0;

            if (HttpContext.Request.Files.AllKeys.Any())
            {
                for (int i = 0; i <= HttpContext.Request.Files.Count; i++)
                {
                    var file = HttpContext.Request.Files["redactedfiles" + i];
                    if (file != null)
                    {

                        caseheaderid = Convert.ToInt32(Request.Form["caseheaderid"]);

                        string folderName = System.Configuration.ConfigurationManager.AppSettings["_RedactedReleases"] + caseheaderid;

                        if (!Directory.Exists(folderName))
                            Directory.CreateDirectory(folderName);

                        var fileSavePath = Path.Combine(folderName, System.IO.Path.GetFileName(file.FileName));
                        file.SaveAs(fileSavePath.Replace(" ", ""));

                        viewRedactedReleases supportdoc = new viewRedactedReleases();
                        supportdoc.FileName = System.IO.Path.GetFileName(file.FileName.Replace(" ", ""));
                        supportdoc.CaseheaderId = caseheaderid;
                        CMSService.SaveRedactedReleaseDetails(supportdoc);
                    }
                }
            }

            var redirectUrl = new UrlHelper(Request.RequestContext).Action("ManageCase", "Case", new { CaseheaderId = caseheaderid });

            return Json(new { Url = redirectUrl });
        }

        [HttpPost]
        public FileResult DownloadRedactedRelease(int Id, int CaseheaderId)
        {
            viewRedactedReleases supportdoc = CMSService.DownloadRedactedRelease(Id);

            string folderName = System.Configuration.ConfigurationManager.AppSettings["_RedactedReleases"] + CaseheaderId;

            var fileSavePath = Path.Combine(folderName, supportdoc.FileName);

            byte[] fileBytes = System.IO.File.ReadAllBytes(fileSavePath);

            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, supportdoc.FileName);
        }


        #endregion

        #region Helper

        protected viewRecordRelease setIncompleteReleaseRecordErrors(viewRecordRelease viewRecordRelease)
        {
            if (viewRecordRelease.Id > 0)
            {

                if (viewRecordRelease.RecordReleaseTypeId == 0)
                {
                    viewRecordRelease.InCompleteErrors.ErrorsInRelease = true;
                    viewRecordRelease.InCompleteErrors.HasErrorsReleaseType = true;
                }
                if (viewRecordRelease.RecordReleaseRequestReceiverId == 0)
                {
                    viewRecordRelease.InCompleteErrors.ErrorsInRelease = true;
                    viewRecordRelease.InCompleteErrors.HasErrorsReceiver = true;
                }

                if (viewRecordRelease.DateRequestReceived == Convert.ToDateTime("1/1/0001"))
                {
                    viewRecordRelease.InCompleteErrors.ErrorsInRelease = true;
                    viewRecordRelease.InCompleteErrors.HasErrorsDateRequestReceived = true;
                }

                if (viewRecordRelease.RecordReleaseRequestReceivedById == 0)
                {
                    viewRecordRelease.InCompleteErrors.ErrorsInRelease = true;
                    viewRecordRelease.InCompleteErrors.HasErrorsReceviedBy = true;
                }
                if (viewRecordRelease.RecordReleaseRequestorId == 0)
                {
                    viewRecordRelease.InCompleteErrors.ErrorsInRelease = true;
                    viewRecordRelease.InCompleteErrors.HasErrorsRequestor = true;
                }

                //if ((viewRecordRelease.IsRecordReleaseRequestGranted = "y" )|| (viewRecordRelease.IsRecordReleaseRequestGranted = "n"))
                //{
                //    viewRecordRelease.InCompleteErrors.ErrorsInRelease = true;
                //    viewRecordRelease.InCompleteErrors.HasErrorsIsRequestedGranted = true;                   
                //}

                if (viewRecordRelease.DateRecordReleaseRequestGranted == Convert.ToDateTime("1/1/0001"))
                {
                    viewRecordRelease.InCompleteErrors.ErrorsInRelease = true;
                    viewRecordRelease.InCompleteErrors.HasErrorsDateGranted = true;
                }

                if (viewRecordRelease.RecordReleaseRequestCompletedId == 0)
                {
                    viewRecordRelease.InCompleteErrors.ErrorsInRelease = true;
                    viewRecordRelease.InCompleteErrors.HasErrorsCompletedBy = true;
                }

                if (viewRecordRelease.ReleasePreparedBy == null)
                {
                    viewRecordRelease.InCompleteErrors.ErrorsInRelease = true;
                    viewRecordRelease.InCompleteErrors.HasErrorsPreparedBy = true;
                }

            }

            return viewRecordRelease;
        }

        #endregion

        #endregion

        #region Caseplan

        public virtual ActionResult EditCasePlan(int Id, int IntakeId)
        {

            //viewClientCMS viewclient = CMSService.GetviewClientCMS(Id);


            viewCasePlan viewcaseplan;

            if (Id != 0)
            {
                viewcaseplan = CMSService.GetCasePlan(Id);
            }
            else
            {
                viewcaseplan = new viewCasePlan();

                //viewcaseplan.ClientId = Id;
                viewcaseplan.DateIntake = CMSService.GetIntake((int)IntakeId).DateIntake.GetValueOrDefault();
                viewcaseplan.ListCasePlanTime = CMSService.ListOfCasePlanTimes().ToList();

                foreach (var caseplantime in viewcaseplan.ListOfCasePlanTimesChecked)
                {
                    caseplantime.Risk = CMSService.ListOfRiskCategories().ToList();

                    foreach (var risk in caseplantime.Risk)
                    {
                        viewGoals goal = new viewGoals();
                        risk.Goals.Add(goal);

                        foreach (var goals in risk.Goals)
                        {
                            viewIntervention intervention = new viewIntervention();
                            goals.Interventions.Add(intervention);
                        }
                    }
                }

                foreach (var caseplantime in viewcaseplan.ListCasePlanTime)
                {
                    caseplantime.Risk = CMSService.ListOfRiskCategories().ToList();

                    foreach (var risk in caseplantime.Risk)
                    {
                        viewGoals goal = new viewGoals();
                        risk.Goals.Add(goal);

                        foreach (var goals in risk.Goals)
                        {
                            viewIntervention intervention = new viewIntervention();
                            goals.Interventions.Add(intervention);
                        }
                    }
                }
            }

            viewcaseplan.caselookup = getcaselookup(username, ViewBag.UserContractId);
            viewcaseplan.IntakeId = IntakeId;
            viewcaseplan.viewIntake = new viewIntake();
            viewcaseplan.viewIntake = CMSService.GetIntake(IntakeId);
            viewcaseplan.DateIntake = viewcaseplan.viewIntake.DateIntake;
            viewcaseplan.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)viewcaseplan.viewIntake.CaseheaderId);



            return View(viewcaseplan);
        }

        public virtual ActionResult ViewCasePlan(int Id, int IntakeId)
        {
            ViewBag.Title = "View CasePlan";


            viewCasePlan viewcaseplan;
            viewcaseplan = CMSService.GetCasePlan(Id);

            viewcaseplan.caselookup = getcaselookup(username, ViewBag.UserContractId);
            viewcaseplan.IntakeId = IntakeId;
            viewcaseplan.viewIntake = new viewIntake();
            viewcaseplan.viewIntake = CMSService.GetIntake(IntakeId);
            viewcaseplan.DateIntake = viewcaseplan.viewIntake.DateIntake;
            viewcaseplan.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)viewcaseplan.viewIntake.CaseheaderId);
            return View(viewcaseplan);


        }
        //public ActionResult EditCasePlanHelper()
        //{
        //   viewCasePlan  obj = TempData["tempcaseplan"] as viewCasePlan;

        //    viewClientCMS viewclient = CMSService.GetviewClientCMS((int)obj.ClientId);
        //    viewclient.caselookup = getcaselookup(username,  ViewBag.UserContractId);
        //    viewclient.viewcaseplan = new viewCasePlan();
        //    viewclient.viewcaseplan.ClientId = (int)obj.ClientId;
        //    viewclient.viewcaseplan.IntakeId = obj.IntakeId;
        //    viewclient.viewcaseplan.DateIntake = CMSService.GetIntake((int)obj.IntakeId).DateIntake.GetValueOrDefault();
        //    viewclient.viewcaseplan.ListCasePlanTime = CMSService.ListOfCasePlanTimes().ToList();


        //        for (int i = 0; i < viewclient.viewcaseplan.ListCasePlanTime.Count; i++)
        //        {
        //            foreach (var type in obj.ListOfCasePlanTimesChecked)
        //            {
        //                if (type.Id == viewclient.viewcaseplan.ListCasePlanTime[i].Id)
        //                {
        //                    viewclient.viewcaseplan.ListCasePlanTime[i].IsChecked = true;
        //                }

        //            }

        //        }

        //        foreach (var check in obj.ListOfCasePlanTimesChecked)
        //        {
        //            viewclient.viewcaseplan.ListOfCasePlanTimesChecked.Add(check);
        //        }

        //        foreach (var caseplantime in viewclient.viewcaseplan.ListOfCasePlanTimesChecked)
        //        {
        //            caseplantime.Risk = CMSService.ListOfRiskCategories().ToList();

        //            foreach (var risk in caseplantime.Risk)
        //            {
        //                viewGoals goal = new viewGoals();
        //                risk.Goals.Add(goal);

        //                foreach (var goals in risk.Goals)
        //                {
        //                    viewIntervention intervention = new viewIntervention();
        //                    goals.Interventions.Add(intervention);
        //                }
        //            }
        //         }

        //    //viewclient.viewReconciliation.ReconciliationVouchers = GetCaseRecordingTypesddl(false);
        //   return View("EditCasePlan",viewclient);
        //}     

        [HttpPost]
        public ActionResult SaveCasePlan(viewCasePlan viewcaseplan)
        {

            viewcaseplan.UserCreated = username;
            viewcaseplan.UserUpdated = username;
            //viewclient.viewcaseplan.ClientId = viewclient.Id;
            //viewclient.viewcaseplan.CaseheaderId = viewclient.CaseheaderId;
            //viewclient.viewcaseplan.IntakeId = viewclient.IntakeId;


            //viewclient.viewcaseplan.CaseheaderId = CMSService.GetCaseHeaderId(viewclient.Id);

            viewcaseplan.CaseheaderId = CMSService.SaveCasePlan(viewcaseplan);

            return Redirect(Url.Action("ManageCase", "Case", new { CaseheaderId = viewcaseplan.CaseheaderId }));
        }

        #endregion

        #region Services Summary

        public ActionResult GetServiceSummary(int Id, int CaseheaderId)
        {
            viewClientCMS viewclient = CMSService.GetviewClientCMS(Id);

            viewclient.viewServiceSummary = new viewServiceSummary();

            viewclient.viewServiceSummary.ServicesAtStart = CMSService.ServicesAtStart(CaseheaderId).ToList();

            viewclient.viewServiceSummary.ServicesReferredByAPS = CMSService.ServicesReferredByAPS(CaseheaderId).ToList();

            viewclient.viewServiceSummary.ServicesAtClosure = CMSService.ServicesAtClosure(CaseheaderId).ToList();

            viewclient.viewServiceSummary.AbuserServices = CMSService.AbuserServicesAtStart(CaseheaderId).ToList();

            //viewclient.viewServiceSummary.AbuserServicesAtClosure = CMSService.AbuserServicesAtClosure(Id, CaseheaderId).ToList();

            return View(viewclient);
        }

        #endregion

        #region Risk Summary

        public ActionResult GetRiskSummary(int Id, int CaseheaderId)
        {
            viewClientCMS viewclient = CMSService.GetviewClientCMS(Id);

            viewclient.viewRiskSummary = CMSService.GetRiskSummary(Id);

            return View(viewclient);
        }

        #endregion

        #region Referrals

        public ActionResult EditReferrals(int Id, int CaseheaderId, int intakeID)
        {

            viewDHSReferral viewDHSReferral;

            if (Id != 0)
            {
                viewDHSReferral = CMSService.GetDHSReferrals(Id);
            }

            else
            {

                viewDHSReferral = new viewDHSReferral();
                viewDHSReferral.DateCreated = DateTime.Now;
            }


            viewDHSReferral.caselookup = getcaselookup(username, ViewBag.UserContractId);
            viewDHSReferral.viewCaseHeader = new viewCaseHeader();
            viewDHSReferral.viewCaseHeader = CMSService.GetCaseHeaderById(CaseheaderId);
            viewDHSReferral.CaseheaderId = viewDHSReferral.viewCaseHeader.Id;

            if (viewDHSReferral.caselookup.listofcontracts != null && viewDHSReferral.caselookup.listofcontracts.Any() && viewDHSReferral.caselookup.listofcontracts.Count == 1)
            {
                viewDHSReferral.ContractId = viewDHSReferral.caselookup.listofcontracts.FirstOrDefault().Id;
                viewDHSReferral.ContractName = (from contract in viewDHSReferral.caselookup.listofcontracts where contract.Id == viewDHSReferral.ContractId select contract.ContractName).FirstOrDefault();

            }
            viewDHSReferral.viewCaseHeader.Intake = CMSService.GetIntake(intakeID);
            return View(viewDHSReferral);
        }

        public ActionResult SaveReferrals(viewDHSReferral viewDHSReferral)
        {
            viewDHSReferral.StatusDescription = CaseStatus.Open.ToString();
            int ad = CMSService.SaveDHSReferral(viewDHSReferral);

            var redirectUrl = new UrlHelper(Request.RequestContext).Action("ManageCase", "Case", new { CaseheaderId = viewDHSReferral.CaseheaderId });


            return Json(redirectUrl, JsonRequestBehavior.AllowGet);

        }

        public ActionResult SubmitReferrals(viewDHSReferral viewDHSReferral)
        {
            viewDHSReferral.StatusDescription = CaseStatus.Submitted.ToString();
            viewDHSReferral.DateSubmitted = DateTime.Now;
            viewDHSReferral.UserSubmitted = username;
            EmailLists email = CMSService.GetEmails();

            bool sendToDDD = false;
            bool sendToDRS = false;
            bool sendToDMH = false;

            if (viewDHSReferral.StatusId == 24 || viewDHSReferral.StatusId == 29 || viewDHSReferral.StatusId == 30)
            {

                ReferralChangeModel change = new ReferralChangeModel();

                change = CMSService.HasReferralChanged(viewDHSReferral);



                if(change.hasDataChanged == true)
                {
                    sendToDDD = viewDHSReferral.IsDDD;
                    sendToDRS = viewDHSReferral.IsDRS;
                    sendToDMH = viewDHSReferral.IsDMH;

                }
                else
                {
                    if (change.hasDDDBeenAdded)
                    {
                        sendToDDD = true;
                    }
                    if (change.hasDRSBeenAdded)
                    {
                        sendToDRS = true;
                    }
                    if (change.hasDMHBeenAdded)
                    {
                        sendToDMH = true;
                    }

                }

            }
            else
            {
                sendToDDD = viewDHSReferral.IsDDD;
                sendToDRS = viewDHSReferral.IsDRS;
                sendToDMH = viewDHSReferral.IsDMH;
            }




            int ad = CMSService.SaveDHSReferral(viewDHSReferral);




            List<int> PreviousReferrals = CMSService.GetPreviousReferralIds(ad); 


            List<MailAddress> EMails = new List<MailAddress>();

            string Message = "You have received a DHS Referral (Id: " + ad + ") from " + CMSService.GetContract(contractids.FirstOrDefault()).ContractName + ".";

            if(PreviousReferrals != null && PreviousReferrals.Any())
            {
                Message = Message + "This referral was previously submitted with the following Id(s): ";
                for(int i = 0; i < PreviousReferrals.Count; i++)
                {
                    if(i == PreviousReferrals.Count - 1)
                    {
                        Message = Message + PreviousReferrals[i] + ".";
                    }
                    else
                    {
                        Message = Message + PreviousReferrals[i] + ", ";
                    }
                }
            }

            string Subject;

            if (sendToDDD == true)
            {
                Subject = "DHS Referral - DDD";

                foreach (var e in email.DDDEmails)
                {
                    EMails.Add(new MailAddress(e, "To"));
                }
                foreach(var e in email.BCCEmails)
                {
                    EMails.Add(new MailAddress(e, "BCC"));
                }
                

                SendEmail(EMails, Message, Subject);
            }
            if (sendToDRS == true)
            {
                EMails.Clear();

                Subject = "DHS Referral - DRS";

                foreach (var e in email.DRSEmails)
                {
                    EMails.Add(new MailAddress(e, "To"));
                }
                foreach (var e in email.BCCEmails)
                {
                    EMails.Add(new MailAddress(e, "BCC"));
                }
                SendEmail(EMails, Message, Subject);
            }
            if (sendToDMH == true)
            {
                Subject = "DHS Referral - DMH";

                EMails.Clear();

                foreach (var e in email.DMHEmails)
                {
                    EMails.Add(new MailAddress(e, "To"));
                }
                foreach (var e in email.BCCEmails)
                {
                    EMails.Add(new MailAddress(e, "BCC"));
                }

                SendEmail(EMails, Message, Subject);
            }

            return Redirect(Url.Action("ManageCase", "Case", new { CaseheaderId = viewDHSReferral.CaseheaderId }));

        }


        public ActionResult ViewReferrals(int Id, int CaseheaderId, int intakeID)
        {

            viewDHSReferral viewDHSReferralt = CMSService.GetDHSReferrals(Id);
            viewDHSReferralt.viewCaseHeader = new AGE.CMS.Data.Models.Intake.viewCaseHeader();
            viewDHSReferralt.viewCaseHeader = CMSService.GetCaseHeaderById(CaseheaderId);
            viewDHSReferralt.caselookup = getcaselookup(username, ViewBag.UserContractId);


            viewDHSReferralt.viewCaseHeader.Intake = CMSService.GetIntake(intakeID);

            return View(viewDHSReferralt);
        }

        #endregion

        #region Case Activity Tracker

        public ActionResult EditCaseActivityTracker(int Id, int IntakeId)
        {
            viewCaseActivityTracker CaseActivityTracker;

            if (Id != 0)
            {
                CaseActivityTracker = CMSService.GetCaseActivityTracker(Id);
            }
            else
            {
                CaseActivityTracker = new viewCaseActivityTracker();
            }
            //viewORA.caselookup = getcaselookup(username, ViewBag.UserContractId);
            CaseActivityTracker.IntakeId = IntakeId;
            CaseActivityTracker.viewIntake = new viewIntake();
            CaseActivityTracker.viewIntake = CMSService.GetIntake(IntakeId);
            CaseActivityTracker.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)CaseActivityTracker.viewIntake.CaseheaderId);

            ViewBag.Exceptionsddl = GetExceptions();
            ViewBag.SubstantiationTypes = GetSubstantiationTypesddl();
            ViewBag.PriorityTypes = GetPriorities();
            return View(CaseActivityTracker);
        }

        public ActionResult SaveCaseActivityTracker(viewCaseActivityTracker viewtracker)
        {
            viewtracker.UserCreated = username;
            viewtracker.UserUpdated = username;

            viewtracker.Id = CMSService.SaveCaseActivityTracker(viewtracker);

            //return RedirectToAction("Index", "Case");
            return Redirect(Url.Action("ManageCase", "Case", new { CaseheaderId = viewtracker.viewIntake.viewCaseHeader.Id }));
        }

        public ActionResult ViewCaseActivityTracker(int Id, int IntakeId)
        {
            viewCaseActivityTracker CaseActivityTracker;

            if (Id != 0)
            {
                CaseActivityTracker = CMSService.GetCaseActivityTracker(Id);
            }
            else
            {
                CaseActivityTracker = new viewCaseActivityTracker();
            }
            //viewORA.caselookup = getcaselookup(username, ViewBag.UserContractId);
            CaseActivityTracker.IntakeId = IntakeId;
            CaseActivityTracker.viewIntake = new viewIntake();
            CaseActivityTracker.viewIntake = CMSService.GetIntake(IntakeId);
            CaseActivityTracker.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)CaseActivityTracker.viewIntake.CaseheaderId);

            ViewBag.Exceptionsddl = GetExceptions();
            ViewBag.SubstantiationTypes = GetSubstantiationTypesddl();
            ViewBag.PriorityTypes = GetPriorities();
            return View(CaseActivityTracker);
        }

        #endregion

        #region Timeline

        public ActionResult GetTimeline(int CaseheaderId)
        {
            viewTimeLine timeline = CMSService.GetTimeline(CaseheaderId);

            return View(timeline);
        }

        public ActionResult GetTimelineData()
        {
            return Json(CMSService.GetTimelines(username), JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region WaiversExtension

        public ActionResult EditWaiverExtension(int Id, int IntakeId, string mode)
        {

            viewWaiverExtension waiver;

            if (Id > 0)
            {
                waiver = CMSService.GetWaiverExtension(Id);
            }
            else
            {
                waiver = new viewWaiverExtension();
                waiver.ListAssessmentDelayReason = codeTable.ListOfAssessmentDelayReason;
                waiver.ListService = codeTable.ListOfEISServices;
            }
            waiver.viewIntake = new viewIntake();
            waiver.viewIntake = CMSService.GetIntake(IntakeId);
            waiver.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)waiver.viewIntake.CaseheaderId);
            waiver.CaseheaderId = waiver.viewIntake.viewCaseHeader.Id;


            waiver.viewIntake.mode = mode;


            return View(waiver);
        }

        public ActionResult SaveWaiverExtension(viewWaiverExtension waiver)
        {

            waiver.UserCreated = username;
            waiver.UserUpdated = username;
            waiver.StatusDescription = CaseStatus.Open.ToString();

            waiver.Id = CMSService.SaveWwaiverExtension(waiver);

            return Redirect(Url.Action("ManageCase", "Case", new { CaseheaderId = waiver.CaseheaderId }));
        }

        [HttpPost]
        public ActionResult SubmitWaiverExtension(viewWaiverExtension waiver)
        {
            waiver.UserCreated = username;
            waiver.UserUpdated = username;
            waiver.StatusDescription = CaseStatus.Submitted.ToString();

            waiver.Id = CMSService.SaveWwaiverExtension(waiver);

            EmailLists emails = CMSService.GetEmails();

            var contract = CMSService.GetContract(contractids.FirstOrDefault());

            if (waiver.WaiverType == "Administrative")
            {
                string Message = "An Administrative Waiver has been submitted by  " + contract.ContractName + " for your review for " + waiver.viewIntake.viewCaseHeader.Client.Person.FirstName + " " + waiver.viewIntake.viewCaseHeader.Client.Person.LastName;
                string Subject = "Administrative Waiver";
                List<MailAddress> EMails = new List<MailAddress>();
                foreach(var e in emails.IDoAEmails)
                {
                    EMails.Add(new MailAddress(e, "To"));
                }
                foreach(var e in emails.BCCEmails)
                {
                    EMails.Add(new MailAddress(e, "BCC"));
                }
                
                SendEmail(EMails, Message, Subject);
            }
            else if (waiver.WaiverType == "EIS")
            {
                string Message = "An EIS Waiver has been submitted by  " + contract.ContractName + " for your review for " + waiver.viewIntake.viewCaseHeader.Client.Person.FirstName + " " + waiver.viewIntake.viewCaseHeader.Client.Person.LastName;
                string Subject = "EIS Waiver";

                if (waiver.IsGreaterThan1000 == true)
                {
                    List<MailAddress> EMails = new List<MailAddress>();
                    EMails.Add(new MailAddress(contract.PSA.Email, "To"));
                    foreach (var e in emails.BCCEmails)
                    {
                        EMails.Add(new MailAddress(e, "BCC"));
                    }

                    SendEmail(EMails, Message, Subject);

                }
                if (waiver.IsGreaterThan2000 == true)
                {
                    List<MailAddress> EMails = new List<MailAddress>();
                    foreach (var e in emails.IDoAEmails)
                    {
                        EMails.Add(new MailAddress(e, "To"));
                    }
                    foreach (var e in emails.BCCEmails)
                    {
                        EMails.Add(new MailAddress(e, "BCC"));
                    }

                    SendEmail(EMails, Message, Subject);
                }
            }


            var redirectUrl = new UrlHelper(Request.RequestContext).Action("ManageCase", "Case", new { CaseheaderId = waiver.CaseheaderId });
            var URLandId = new { url = redirectUrl, Id = waiver.Id };
            return Json(URLandId, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult ApproveWaiverExtension(viewWaiverExtension waiver)
        {

            waiver.UserCreated = username;
            waiver.UserUpdated = username;
            waiver.StatusDescription = CaseStatus.Approved.ToString();

            waiver.Id = CMSService.SaveWwaiverExtension(waiver);

            //Trigger Timeline Changes here - By Praneeth
            var redirectUrl = "";
            if (waiver.viewIntake.mode == "ManageCase")
            {
                redirectUrl = new UrlHelper(Request.RequestContext).Action("ManageCase", "Case", new { CaseheaderId = waiver.CaseheaderId });
            }
            if (waiver.viewIntake.mode == "Waiver")
            {
                redirectUrl = new UrlHelper(Request.RequestContext).Action("ListAllWaiverExtensions", "Case");
            }
            var URLandId = new { url = redirectUrl, Id = waiver.Id };
            return Json(URLandId, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ViewWaiverExtension(int Id, string mode)
        {
            viewWaiverExtension waiver = CMSService.GetWaiverExtension(Id);

            waiver.viewIntake = new viewIntake();
            waiver.viewIntake = CMSService.GetIntake((int)waiver.IntakeId);
            waiver.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)waiver.viewIntake.CaseheaderId);
            waiver.CaseheaderId = waiver.viewIntake.viewCaseHeader.Id;


            waiver.viewIntake.mode = mode;

            return View(waiver);


        }

        public ActionResult ListAllWaiverExtensions()
        {
            viewWaiverExtension waiver = new viewWaiverExtension();

            if (User.IsInRole("CMS_RAAAdmin"))
            {
                var userpsa = CMSService.GetPSAsByUser(username);


                waiver.ListWaiverExtensions = CMSService.ListAllWaiverExtensions().Where(i => userpsa.ListOfContracts.Any(s => s.Id == i.viewIntake.viewCaseHeader.ContractId && i.Id != 474)).ToList();
            }
            else
            {
                waiver.ListWaiverExtensions = CMSService.ListAllWaiverExtensions().ToList();
            }

            return View(waiver);
        }

        #endregion

        #region Helpers

        protected viewCaseLookup getcaselookup(string username, int? ContractId)
        {
            AGE.CMS.Web.MvcApplication.log.Debug("in getcaselookup");

            //tlh
            AGE.CMS.Business.CodeTable codeTable = (AGE.CMS.Business.CodeTable)(Session["CODETABLE"]);

            viewCaseLookup caselookup = new viewCaseLookup();
            //caselookup.listofagencytypes = new List<viewAgencyType>();

            // caselookup.listofagencytypes = TestService.ListOfAgencyTypes().ToList();

            //:???????? not codes
            caselookup.listofcontracts = CMSService.ListOfContracts(username).ToList();
            caselookup.listofallcontracts = CMSService.ListOfAllContracts().ToList();
            caselookup.listofcaseworkers = CMSService.ListOfWorkersByContracts(codeTable.ContractIds);
            caselookup.listofreportertypes = codeTable.ListOfReporterTypes;
            caselookup.listofrelations = codeTable.ListOfRelations;
            caselookup.listofagencies = codeTable.ListOfAgencies;
            caselookup.listofpriorities = codeTable.ListOfPriorities;
            caselookup.listofreportstatus = codeTable.ListOfReportStatus;
            caselookup.listoffolders = codeTable.ListOfFolders;
            caselookup.listofemploymenttypes = codeTable.ListOfEmploymentTypes;
            caselookup.listofservices = codeTable.ListOfServices;
            caselookup.listofincomelevels = codeTable.ListOfIncomeLevels;
            caselookup.listofInteragencies = codeTable.ListOfInterAgencies;
            caselookup.listofprimarylanguage = codeTable.ListOfPrimaryLanguages;
            caselookup.listofschoolinglevel = codeTable.ListOfSchoolingLevel;
            caselookup.listoflegalstatus = codeTable.ListOfLegalStatus;

            // TLH this is loading from the same table????
            //caselookup.ListOfAAAssociations = CMSService.ListOfAbuserAssociations().ToList();
            //caselookup.listofAbuserAssociations = CMSService.ListOfAbuserAssociations().ToList();
            caselookup.ListOfAAAssociations = codeTable.ListOfAbuserAssociations;
            caselookup.listofAbuserAssociations = codeTable.ListOfAbuserAssociations;

            ////caselookup.listofInteragencies = CMSService.ListOfInterAgencies().ToList();

            //caselookup.listofailmentconfirmations = CMSService.ListOfAilmentConfirmation().ToList();
            caselookup.listofailmentconfirmations = codeTable.ListOfAilmentConfirmation;

            //caselookup.listofJudgeOutcomes = CMSService.ListOfJudgeOutcomes().ToList();
            caselookup.listofJudgeOutcomes = codeTable.ListOfJudgeOutcomes;

            //caselookup.listofVoucherTypes = CMSService.ListOfVoucherTypes().ToList();
            caselookup.listofVoucherTypes = codeTable.ListOfVoucherTypes;

            //caselookup.listofRecordReleaseRequestors = CMSService.ListOfRecordReleaseRequestors().ToList();
            caselookup.listofRecordReleaseRequestors = codeTable.ListOfRecordReleaseRequestors;

            //caselookup.listofRecordReleaseRequestsReceivedBy = CMSService.ListOfRecordReleaseRequestsReceivedBy().ToList();
            caselookup.listofRecordReleaseRequestsReceivedBy = codeTable.ListOfRecordReleaseRequestsReceivedBy;

            //caselookup.listofRecordReleaseRequestsReceivers = CMSService.ListOfRecordReleaseRequestReceivers().ToList();
            caselookup.listofRecordReleaseRequestsReceivers = codeTable.ListOfRecordReleaseRequestReceivers;

            //caselookup.listofRecordReleaseTypes = CMSService.ListOfRecordReleaseTypes().ToList();
            caselookup.listofRecordReleaseTypes = codeTable.ListOfRecordReleaseTypes;

            //caselookup.ListClientReceivesServices_Referrals = CMSService.ListClientReceivesServices_Referrals().ToList();
            caselookup.ListClientReceivesServices_Referrals = codeTable.ListOfClientReceivesServices_Referrals;


            //caselookup.ListOfCaeClosureReasons_IsAble = CMSService.ListCaseClosureReason_IsAble().ToList();
            caselookup.ListOfCaeClosureReasons_IsAble = codeTable.ListOfCaseClosureReason_IsAble;

            //caselookup.ListOfCaeClosureReasons_IsUnable = CMSService.ListCaseClosureReason_IsUnable().ToList();
            caselookup.ListOfCaeClosureReasons_IsUnable = codeTable.ListOfCaseClosureReason_IsUnable;

            //caselookup.ListofClassifications = CMSService.ListofClassications().ToList();
            caselookup.ListofClassifications = codeTable.ListofClassications;

            //caselookup.ListOfClosureLivingArrangements = CMSService.ListOfClosureLivingArrangements().ToList();
            caselookup.ListOfClosureLivingArrangements = codeTable.ListOfClosureLivingArrangements;

            //caselookup.ListOfActivitiesRAA = CMSService.ListofActivities_RAA().ToList();
            caselookup.ListOfActivitiesRAA = codeTable.ListofActivities_RAA;

            //caselookup.ListOfIntakeReportTypes = CMSService.ListOfIntakeReportTypes().ToList();
            caselookup.ListOfIntakeReportTypes = codeTable.ListOfIntakeReportTypes;

            //caselookup.listofveteranstatus = CMSService.ListOfVeteranStatus().ToList();
            caselookup.listofveteranstatus = codeTable.ListOfVeteranStatus;

            //caselookup.ListMCOs = CMSService.ListOfMCOs().ToList();
            caselookup.ListMCOs = codeTable.ListOfMCOs;

            //caselookup.listofgender = CMSService.ListOfGenderTypes();
            caselookup.listofgender = codeTable.ListOfGenderTypes;
             
            //caselookup.listofdtoethinicities = CMSService.ListOfEthinicityTypes();
            caselookup.listofdtoethinicities = codeTable.ListOfEthinicityTypes;

            //caselookup.ListOfGenderOrientationTypes = CMSService.ListOfGenderOrientationTypes();
            caselookup.ListOfGenderOrientationTypes = codeTable.ListOfGenderOrientationTypes;

            //caselookup.listoflivingstatus = CMSService.ListOfLivingStatusTypes();
            caselookup.listoflivingstatus = codeTable.ListOfLivingStatusTypes;

            //caselookup.listofraces = CMSService.ListOfRaceTypes();
            caselookup.listofraces = codeTable.ListOfRaceTypes;

            //caselookup.listofcounties = CMSService.ListOfCountyTypes();
            caselookup.listofcounties = codeTable.ListOfCountyTypes;

            //caselookup.listofstates = CMSService.ListOfStateTypes();
            caselookup.listofstates = codeTable.ListOfStateTypes;

            //caselookup.listofmaritalstatus = CMSService.ListOfMaritalStatusTypes();
            caselookup.listofmaritalstatus = codeTable.ListOfMaritalStatusTypes;

            //caselookup.listoflivingarrangements = CMSService.ListOfLivingArrangementTypes();
            caselookup.listoflivingarrangements = codeTable.ListOfLivingArrangementTypes;

            var person = CMSService.GetPerson(Guid.Empty);
            //var workers = CMSService.ListOfWorkers((int)ContractId).ToList();
            //foreach (var worker in workers)
            //{
            //    var roles = GetRolesForUser(worker.UserName);
            //}

            //   using (var PersonService = new PersonProfileWCFServiceClient())
            //     {
            //         //caselookup.ListOfGenderOrientationTypes = new List<dtoGenderOrientationType>(PersonService.ListOfGenderOrientationTypes());
            //         //caselookup.listofdtoethinicities = new List<dtoEthnicityType>(PersonService.ListOfEthnicityTypes());

            //        // caselookup.listofgender = person.Demographic.DemographicList.ListOfGenderTypes;
            //         //caselookup.listoflivingstatus = person.Demographic.DemographicList.ListOfLivingStatusTypes;
            //         //caselookup.listofraces = person.Demographic.DemographicList.ListOfRaceTypes;
            //         //var ethinicitylist = PersonService.ListOfEthnicityTypes();
            //         //var listofveteranstatus = PersonService.ListOfVeteransStatusTypes();
            //         //caselookup.listofveteranstatus = new List<dto>(PersonService.ListOfVeteransStatusTypes());            
            //         //caselookup.listofcounties = CMSService.ListOfCounties(14).ToList();
            // //caselookup.listofcounties = new List<dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();
            //// caselookup.listofstates = new List<dtoStateType>(PersonService.ListOfStateTypes());
            // //caselookup.listofmaritalstatus = new List<dtoMaritalStatusType>(PersonService.ListOfMaritalStatusTypes());
            // //caselookup.listoflivingarrangements = new List<dtoLivingArrangementType>(PersonService.ListOfLivingArrangementTypes());
            //     }



            return caselookup;
        }

        public List<SelectListItem> GetInterAgencyddl()
        {
            List<SelectListItem> ddlInterAgencies = new List<SelectListItem>();
            try
            {
                List<viewInteragencyCoordination> lstAgency = codeTable.ListOfInterAgencies;

                foreach (viewInteragencyCoordination agency in lstAgency)
                {
                    SelectListItem item = new SelectListItem() { Text = agency.Description, Value = agency.Code.ToString() };
                    //SelectListItem item = new SelectListItem() { Text = type.Name, Value = type.Name };
                    ddlInterAgencies.Add(item);
                }
            }
            catch (System.Exception ex)
            {
                throw ex;
            }

            return ddlInterAgencies;
        }

        public List<SelectListItem> GetFiscalPeriodddl()
        {
            List<SelectListItem> ddlFiscalPeriod = new List<SelectListItem>();

            int CurrentFiscalYear = (DateTime.Now.Month > 6 ? DateTime.Now.Year + 1 : DateTime.Now.Year);

            // fiscalperiods = new List<dtoPeriod>();
            int startmonth = 7;
            int startyear = (DateTime.Now.Month > 6 ? DateTime.Now.Year : DateTime.Now.Year - 1);

            startyear = startyear - 1;

            List<string> monthnames = new List<string>() { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

            int yearbegin = Math.Max(2016, CurrentFiscalYear - 4);

            for (int i = 0; i < 24; i++)
            {
                //(startyear.ToString() + " - " + monthnames[startmonth - 1]).ToString(); // this will give result as "2016 - Jul"
                //(startyear * 100 + startmonth).ToString(); // this will give result as "201607"

                SelectListItem ddl = new SelectListItem() { Text = (startyear.ToString() + " - " + monthnames[startmonth - 1]).ToString(), Value = (startyear.ToString() + " - " + monthnames[startmonth - 1]).ToString() };
                ddlFiscalPeriod.Add(ddl);

                startmonth++;
                if (startmonth > 12)
                {
                    startmonth = 1;
                    startyear++;
                }
            }

            return ddlFiscalPeriod;
        }

        public List<SelectListItem> GetServices()
        {
            List<SelectListItem> ddlServices = new List<SelectListItem>();
            try
            {
                List<viewServices> lstServices = codeTable.ListOfServices;

                foreach (viewServices service in lstServices)
                {
                    SelectListItem item = new SelectListItem() { Text = service.Description, Value = service.Code.ToString() };
                    //SelectListItem item = new SelectListItem() { Text = type.Name, Value = type.Name };
                    ddlServices.Add(item);
                }
            }
            catch (System.Exception ex)
            {
                throw ex;
            }

            return ddlServices;
        }

        public List<SelectListItem> GetRelations()
        {
            List<SelectListItem> ddlRelations = new List<SelectListItem>();
            try
            {
                var lstRelations = codeTable.ListOfRelations;

                foreach (var relation in lstRelations)
                {
                    SelectListItem item = new SelectListItem() { Text = relation.Description, Value = relation.Code.ToString() };
                    //SelectListItem item = new SelectListItem() { Text = type.Name, Value = type.Name };
                    ddlRelations.Add(item);
                }
            }
            catch (System.Exception ex)
            {
                throw ex;
            }

            return ddlRelations;
        }

        public List<SelectListItem> GetAilmentConfirmation()
        {
            List<SelectListItem> ddlAilmentConfirmation = new List<SelectListItem>();
            try
            {
                List<viewAilmentConfirmation> lstAIlmentConfirmations = codeTable.ListOfAilmentConfirmation;

                foreach (viewAilmentConfirmation confirmation in lstAIlmentConfirmations)
                {
                    SelectListItem item = new SelectListItem() { Text = confirmation.Description, Value = confirmation.Code.ToString() };
                    //SelectListItem item = new SelectListItem() { Text = type.Name, Value = type.Name };
                    ddlAilmentConfirmation.Add(item);
                }
            }
            catch (System.Exception ex)
            {
                throw ex;
            }

            return ddlAilmentConfirmation;
        }

        public List<SelectListItem> GetVeteranStatus()
        {
            List<SelectListItem> ddlVeteranStatus = new List<SelectListItem>();
            try
            {
                //List<CMS.Models.Intake.viewVeteranStatus> lstVeteranStatus = CMSService.ListOfVeteranStatus().ToList();

                var lstVeteranStatus = new List<dtoVeteransStatusType>();
                using (var PersonService = new PersonProfileWCFServiceClient())
                {
                    lstVeteranStatus = PersonService.ListOfVeteransStatusTypes().OrderBy(v => v.Id).ToList();

                }
                foreach (var status in lstVeteranStatus)
                {
                    SelectListItem item = new SelectListItem() { Text = status.Description, Value = status.Id.ToString() };
                    ddlVeteranStatus.Add(item);
                }
            }
            catch (System.Exception ex)
            {
                throw ex;
            }

            return ddlVeteranStatus;
        }



        public List<SelectListItem> GetSubstantiationTypesddl()
        {
            List<SelectListItem> ddlTypes = new List<SelectListItem>();
            try
            {
                var lstType = codeTable.ListOfReportStatus;

                foreach (var type in lstType)
                {
                    SelectListItem item = new SelectListItem() { Text = type.Description, Value = type.Id.ToString() };
                    //SelectListItem item = new SelectListItem() { Text = type.Name, Value = type.Name };
                    ddlTypes.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return ddlTypes;
        }

        public List<SelectListItem> GetCaseRecordingTypesddl(bool addAll)
        {
            List<SelectListItem> ddlTypes = new List<SelectListItem>();
            try
            {
                List<AGE.CMS.Data.Models.CaseRecording.Type> lstType = CMSService.GetCaseRecordingTypes().ToList();

                if (addAll)
                {
                    ddlTypes.Add(new SelectListItem() { Text = "All", Value = "All" });
                }

                foreach (AGE.CMS.Data.Models.CaseRecording.Type type in lstType)
                {
                    //SelectListItem item = new SelectListItem() { Text = type.Name, Value = type.Id.ToString() };
                    SelectListItem item = new SelectListItem() { Text = type.Name, Value = type.Description };
                    ddlTypes.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return ddlTypes;
        }

        public List<SelectListItem> GetIntakeReportType()
        {
            List<SelectListItem> ddlTypes = new List<SelectListItem>();
            try
            {
                var lstType = codeTable.ListOfIntakeReportTypes;

                foreach (var type in lstType)
                {
                    SelectListItem item = new SelectListItem() { Text = type.Description, Value = type.Id.ToString() };
                    //SelectListItem item = new SelectListItem() { Text = type.Name, Value = type.Name };
                    ddlTypes.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return ddlTypes;
        }
        public List<SelectListItem> GetExceptions()
        {
            List<SelectListItem> ddlTypes = new List<SelectListItem>();
            try
            {
                var lstType = CMSService.ListofExceptions().ToList();

                foreach (var type in lstType)
                {
                    SelectListItem item = new SelectListItem() { Text = type.Description, Value = type.Id.ToString() };
                    //SelectListItem item = new SelectListItem() { Text = type.Name, Value = type.Name };
                    ddlTypes.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return ddlTypes;
        }

        public List<SelectListItem> GetPriorities()
        {
            List<SelectListItem> ddlTypes = new List<SelectListItem>();
            try
            {
                var lstType = codeTable.ListOfPriorities;

                foreach (var type in lstType)
                {
                    SelectListItem item = new SelectListItem() { Text = type.Description, Value = type.Id.ToString() };
                    //SelectListItem item = new SelectListItem() { Text = type.Name, Value = type.Name };
                    ddlTypes.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return ddlTypes;
        }

        public List<SelectListItem> GetClutterImageRatingddl()
        {
            List<SelectListItem> ClutterImageRating = new List<SelectListItem>();
            try
            {

                ClutterImageRating.Add(new SelectListItem { Text = "Select Rating", Value = "null" });
                ClutterImageRating.Add(new SelectListItem { Text = "0", Value = "0" });
                ClutterImageRating.Add(new SelectListItem { Text = "1", Value = "1" });
                ClutterImageRating.Add(new SelectListItem { Text = "2", Value = "2" });
                ClutterImageRating.Add(new SelectListItem { Text = "3", Value = "3" });
                ClutterImageRating.Add(new SelectListItem { Text = "4", Value = "4" });
                ClutterImageRating.Add(new SelectListItem { Text = "5", Value = "5" });
                ClutterImageRating.Add(new SelectListItem { Text = "6", Value = "6" });
                ClutterImageRating.Add(new SelectListItem { Text = "7", Value = "7" });
                ClutterImageRating.Add(new SelectListItem { Text = "8", Value = "8" });
                ClutterImageRating.Add(new SelectListItem { Text = "9", Value = "9" });

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return ClutterImageRating;
        }
        public List<SelectListItem> GetIntakeDates(List<viewIntake> ListofIntakes)
        {
            List<SelectListItem> ddlTypes = new List<SelectListItem>();
            try
            {
                var lstType = ListofIntakes.GroupBy(x => x.Id).Select(g => g.First()).ToList();

                foreach (var type in lstType)
                {
                    var date = (DateTime)type.DateIntake;

                    SelectListItem item = new SelectListItem() { Text = date.ToString("MM/dd/yyyy"), Value = date.ToString("MM/dd/yyyy") };
                    //SelectListItem item = new SelectListItem() { Text = type.Name, Value = type.Name };
                    ddlTypes.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return ddlTypes;
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

        protected int GetAge(DateTime DOB)
        {
            DateTime now = DateTime.Today;
            int age = now.Year - DOB.Year;
            if (now < DOB.AddYears(age)) age--;

            return age;
        }




        #endregion

        #region Search

        public ActionResult Search()
        {
            return PartialView("SearchClient");
        }

        //[HttpPost]
        public PartialViewResult SearchClient(string LastName, string FirstName, string DOB, string SSN, bool IsPartial)
        {
            viewClientCMS client = new viewClientCMS();

            DateTime? newDOB = new DateTime();
            if (DOB != null && DOB != "")
            {
                newDOB = DateTime.Parse(DOB);
            }
            else
            {
                newDOB = null;
            }

            if (SSN != null && SSN != "")
            {
                SSN = SSN.Replace("-", "");
            }
            else
            {
                SSN = null;
            }

            
            viewCaseLookup caselookup = getcaselookup(username, codeTable.ContractIds.FirstOrDefault());

            client.ListOfClients = CMSService.SearchClients(LastName, FirstName, SSN, newDOB, IsPartial, caselookup).ToList();

            ViewBag.LastName = LastName;
            ViewBag.FirstName = FirstName;
            ViewBag.DOB = newDOB;
            ViewBag.SSN = SSN;
            ViewBag.IsPartial = IsPartial;

            return PartialView("ClientSearchResults", client);
            //return Json(client, JsonRequestBehavior.AllowGet);
        }

        //[HttpPost]
        public ActionResult SearchAbuser(string LastName, string FirstName, string DOB, string SSN, bool IsPartial)
        {
            viewAbuserInformation viewAbuser = new viewAbuserInformation();

            DateTime? newDOB = new DateTime();
            if (DOB != null && DOB != "")
            {
                newDOB = DateTime.Parse(DOB);
            }
            else
            {
                newDOB = null;
            }

            if (SSN != null && SSN != "")
            {
                SSN = SSN.Replace("-", "");
            }
            else
            {
                SSN = null;
            }
            viewCaseLookup caselookup = getcaselookup(username, contractids.FirstOrDefault());

            viewAbuser.ListOfAbusers = CMSService.SearchAbusers(LastName, FirstName, SSN, newDOB, IsPartial, caselookup).ToList();

            ViewBag.LastName = LastName;
            ViewBag.FirstName = FirstName;
            ViewBag.DOB = newDOB;
            ViewBag.SSN = SSN;
            ViewBag.IsPartial = IsPartial;

            return PartialView("AbuserSearchResults", viewAbuser);
            //return Json(viewAbuser, JsonRequestBehavior.AllowGet);
        }

        //[HttpPost]
        public PartialViewResult FilterCaseLists(string LastName, string FirstName)
        {
            viewClientCMS client = new viewClientCMS();

            return PartialView("ClientSearchResults", client);
        }

        #endregion
    }
}