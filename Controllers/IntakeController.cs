using AGE.CMS.Business;
using AGE.CMS.Data.Models.ENums;
using AGE.CMS.Data.Models.Intake;
using AGE.CMS.Data.Models.Security;
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
using System.ServiceModel;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace AGE.CMS.Web.Areas.CMS.Controllers
{
    [Layout("_Layout")]
    public class IntakeController : CMSController
    {

        #region Index

        public virtual ActionResult Index()
        {

            int Id = 0, ClientId = 0;

            return RedirectToAction("EditIntake", new { ClientId = ClientId, Id = Id });
        }
        //public virtual ActionResult Index(string txtSearch, string message)
        //{


        //    //APSCaseService.TestCreatePayment(70, 2015, 8, 2, "satheesh15", true);

        //    //ViewBag.defaultnavigationname = SecurityService.GetDefaultNavigationHead(@"EXTERNAL\ruby.marquez", "ROOT_APS", ViewBag.PortalName);

        //    //ViewBag.nav = SecurityService.GetDefaultAuthorizedNavigation(@"EXTERNAL\ruby.marquez", ViewBag.defaultnavigationname);

        //    ViewBag.Message = message;

        //    //for (int i 

        //    ViewBag.Title = "Manage Case";
        //    ViewBag.txtSearch = txtSearch;
        //    ViewBag.IsPartial = false;

        //    viewIntake vintake = new viewIntake();
        //    vintake.viewClient = new viewClient();
        //    vintake.viewClient.UserCreated = username;
        //    vintake.viewClient.UserUpdated = username;
        //    //vintake.viewClient.Person = PersonService.GetPerson(Guid.Empty);

        //    vintake.listofintakes = TestService.ListAllIntakes((string)username).ToList();
        //    //vintake.ListOfRecentClients = APSCaseService.ListOfRecentClients((string)username);

        //    //if (txtSearch != null && txtSearch.Length > 0)
        //    //    vintake.viewClient.ListOfClients = APSCaseService.ListOfClients(PersonService.SearchPersons(txtSearch).ToList(), (string)username);

        //    return View(vintake);
        //}
        #endregion

        #region Search PersonProfile

        public virtual ActionResult Search(string txtSearch, string message)
        {
            ViewBag.Message = message;
            ViewBag.Title = "Manage Case";
            ViewBag.txtSearch = txtSearch;
            ViewBag.IsPartial = false;

            viewIntake vintake = new viewIntake();
            vintake.viewClient = new viewClient();
            vintake.viewClient.UserCreated = username;
            vintake.viewClient.UserUpdated = username;
            return View(vintake);
        }

        public virtual ActionResult SearchResults(string LastName, string FirstName, DateTime? DOB, bool IsPartial)
        {
            viewIntake viewintake = new viewIntake();

            viewClient vclient = new viewClient();
            vclient.UserCreated = username;
            vclient.UserUpdated = username;
            vclient.ListOfClients = CMSService.ListOfTempClients(LastName.Trim(), FirstName.Trim(), DOB, IsPartial, getcaselookup(username)).ToList();

            vclient.ListOfClientsfromPP = CMSService.ListOfClientsFromPP(LastName.Trim(), FirstName.Trim(), DOB, IsPartial).ToList();
            vclient.caselookup = getcaselookup(username);

            vclient.Person = new dtoPerson();
            vclient.Person.Demographic = new dtoDemographic();
            vclient.Person.Demographic.GenderType = new dtoGenderType();
            vclient.Person.Demographic.LivingArrangementType = new dtoLivingArrangementType();
            vclient.Person.Demographic.LivingStatusType = new dtoLivingStatusType();
            vclient.Person.Demographic.RaceType = new dtoRaceType();

            ViewBag.LastName = LastName;
            ViewBag.FirstName = FirstName;
            ViewBag.DOB = DOB;
            ViewBag.IsPartial = IsPartial;

            return View(vclient);
        }

        public virtual ActionResult PersonProfileSearchResults(string LastName, string FirstName, DateTime? DOB, bool IsPartial)
        {
            viewClient vclient = new viewClient();
            vclient.UserCreated = username;
            vclient.UserUpdated = username;
            vclient.ListOfClients = CMSService.ListOfTempClients(LastName.Trim(), FirstName.Trim(), DOB, IsPartial, getcaselookup(username)).ToList();

            vclient.caselookup = getcaselookup(username);

            vclient.Person = new dtoPerson();
            vclient.Person.Demographic = new dtoDemographic();
            vclient.Person.Demographic.GenderType = new dtoGenderType();
            vclient.Person.Demographic.LivingArrangementType = new dtoLivingArrangementType();
            vclient.Person.Demographic.LivingStatusType = new dtoLivingStatusType();
            vclient.Person.Demographic.RaceType = new dtoRaceType();

            ViewBag.LastName = LastName;
            ViewBag.FirstName = FirstName;
            ViewBag.DOB = DOB;
            ViewBag.IsPartial = IsPartial;

            return View(vclient);
        }

        #endregion

        #region Verify Intake

        public ActionResult VerifyIntake(int Id)
        {
            viewVerifyClient verifyClient = new viewVerifyClient();
            verifyClient.viewIntake = CMSService.GetIntake(Id);
            //using (var CMSService = new AGE.CMS.Core.CaseManagementServiceRef.CaseManagementServiceClient())
            //{
            //    verifyClient.viewIntake = CMSService.GetIntake(Id);
            //}
            string LastName;
            string FirstName;
            DateTime? DOB;
            string SSN;
            bool IsPartial;
            ViewBag.LastName = LastName = verifyClient.viewIntake.viewClient.LastName;
            ViewBag.FirstName = FirstName = verifyClient.viewIntake.viewClient.FirstName;
            ViewBag.DOB = DOB = verifyClient.viewIntake.viewClient.DOB;
            ViewBag.SSN = SSN = verifyClient.viewIntake.viewClient.SSN;
            ViewBag.IsPartial = IsPartial = false;
            viewCaseLookup caselookup = getcaselookup(username);
            verifyClient.ListOfClients = CMSService.ListOfTempClients(LastName, FirstName, DOB, IsPartial, caselookup).OrderByDescending(i=>i.IntakeId).ToList();
            //using (var CMSService = new AGE.CMS.Core.CaseManagementServiceRef.CaseManagementServiceClient())
            //{
            //    verifyClient.ListOfClients = CMSService.ListOfTempClients(LastName, FirstName, DOB, IsPartial, caselookup).ToList();

            //    IsPartial = true;
            //    verifyClient.ListOfPersons = CMSService.SearchPersonsByNameDOB(LastName, FirstName, SSN, DOB, IsPartial, caselookup).ToList();

            //}
            if (SSN != null && SSN != "")
            {
                var persons = new List<dtoPerson>();
                using (var PersonService = new PersonProfileWCFServiceClient())
                {
                    persons = PersonService.SearchPersons(SSN).ToList();
                }

                if (persons.Count > 0)
                {
                    verifyClient.ListOfPersons = persons;
                }
                else
                {
                    //using (var PersonService = new PersonProfileWCFServiceClient())
                    //{
                    //    verifyClient.ListOfPersons = PersonService.SearchPersonsByNameDOB(LastName.Trim(), FirstName.Trim(), DOB, IsPartial).ToList();
                    //}
                    verifyClient.ListOfPersons = CMSService.SearchPersonsByNameDOB(LastName.Trim(), FirstName.Trim(), SSN, DOB, IsPartial, getcaselookup(username)).ToList();
                    foreach (var person in verifyClient.ListOfPersons)
                    {
                        // if (person.Address.CountyType.Id != 0 && person.Address.CountyType.Id != null)
                        if (person.Address.CountyType.Id != 0)
                        {
                            using (var PersonProfileWCFServiceClient = new PersonProfileWCFServiceClient())
                            {
                                person.Address.CountyType = PersonProfileWCFServiceClient.ListOfCountyTypes(14).Where(i => i.Id == person.Address.CountyType.Id).FirstOrDefault();
                            }
                        }
                    }
                }
            }
            else
            {
                //using (var PersonService = new PersonProfileWCFServiceClient())
                //{
                //    verifyClient.ListOfPersons = PersonService.SearchPersonsByNameDOB(LastName.Trim(), FirstName.Trim(), DOB, IsPartial).ToList();
                //}
                verifyClient.ListOfPersons = CMSService.SearchPersonsByNameDOB(LastName.Trim(), FirstName.Trim(), SSN, DOB, IsPartial, getcaselookup(username)).ToList();
                //using (var CMSService = new AGE.CMS.Core.CaseManagementServiceRef.CaseManagementServiceClient())
                //{
                //    verifyClient.ListOfPersons = CMSService.SearchPersonsByNameDOB(LastName.Trim(), FirstName.Trim(), SSN, DOB, IsPartial, getcaselookup(username)).ToList();
                //}
            }

            foreach (var p in verifyClient.ListOfPersons)
            {
                //using (var CMSService = new AGE.CMS.Core.CaseManagementServiceRef.CaseManagementServiceClient())
                {
                    viewClientCMS Client = CMSService.GetviewClientCMSByPersonKey(p.PersonKey);
                    var opencase = CMSService.GetClientOpenCase(Client.Id);

                    if (opencase != null && opencase.Id > 0)
                    {
                        p.rank = opencase.ContractId;
                        p.UpdatedBy = opencase.ContractDescription;
                    }
                    else
                    {
                        p.rank = 0;
                        p.UpdatedBy = null;
                    }
                }
            }

            verifyClient.contractids = contractids;

            return View(verifyClient);
        }

        [HttpPost]
        //[MultipleButton(Name = "action", Argument = "VerifyIntake")]
        public ActionResult Verify(int Id, int? ClientId, string mode)
        {
            viewIntake viewintake = CMSService.GetIntake(Id);

            if (ClientId != 0)
            {
                if (mode == "pp")
                {
                    dtoPerson person = new dtoPerson();
                    //using (var PersonService = new PersonProfileWCFServiceClient())
                    //{
                    //    person = PersonService.GetPersonById((int)ClientId);
                    //}

                    person = CMSService.GetPersonById((int)ClientId);
                    Guid key = person.PersonKey;
                    viewClientCMS Client = CMSService.GetviewClientCMSByPersonKey(key);
                    if (Client.Id == 0)
                    {
                        Client = new viewClientCMS();
                        Client.PersonKey = key;
                        Client.UserCreated = username;
                        Client.UserUpdated = username;
                        Client.Person = person;

                        Client.Id = CMSService.SaveClientCMS(Client);
                    }
                    viewintake.ClientId = Client.Id;

                }

                else if (mode == "temp")
                {
                    viewClient viewclient = CMSService.GetviewClient((int)ClientId);
                    viewClientCMS viewclientCMS = new viewClientCMS();

                    viewclientCMS.Person = new dtoPerson();
                    viewclientCMS.Person.CreatedBy = viewclientCMS.UserCreated = username;
                    viewclientCMS.Person.UpdatedBy = viewclientCMS.UserCreated = username;
                    viewclientCMS.Person.FirstName = viewclient.FirstName;
                    viewclientCMS.Person.LastName = viewclient.LastName;
                    viewclientCMS.Person.DOB = viewclient.DOB;
                    viewclientCMS.Person.Email = viewclient.Email;
                    viewclientCMS.Person.MiddleName = viewclient.MiddleName;
                    viewclientCMS.Person.RIN = viewclient.RIN;
                    viewclientCMS.Person.SSN = viewclient.SSN;
                    viewclientCMS.AgeRangeId = viewclient.AgeRangeId;
                    viewclientCMS.IsClientAgeEstimate = (bool)viewclient.IsClientAgeEstimate;

                    viewclientCMS.Person.Address = new dtoAddress();
                    viewclientCMS.Person.Address.AddressLine1 = viewclient.AddressLine1;
                    viewclientCMS.Person.Address.AddressLine2 = viewclient.AddressLine2;
                    viewclientCMS.Person.Address.City = viewclient.City;
                    viewclientCMS.Person.Address.CountyType = new dtoCountyType();
                    viewclientCMS.Person.Address.CountyType.Id = (int)viewclient.CountyTypeId;
                    viewclientCMS.Person.Address.StateType = new dtoStateType();
                    viewclientCMS.Person.Address.StateType.Id = (int)viewclient.StateTypeId;
                    viewclientCMS.Person.Address.Zip4 = viewclient.Zip4;
                    viewclientCMS.Person.Address.Zip5 = viewclient.Zip5;
                    viewclientCMS.Person.Address.CreatedBy = username;
                    viewclientCMS.Person.Address.UpdatedBy = username;

                    viewclientCMS.Person.Phone = new dtoPhone();
                    viewclientCMS.Person.Phone.PhoneNumber = viewclient.PhoneNumber == null ? null : viewclient.PhoneNumber.Replace("-", "").Trim();

                    viewclientCMS.Person.AlternatePhone = new dtoPhone();
                    viewclientCMS.Person.AlternatePhone.PhoneNumber = viewclient.AlternatePhone == null ? null : viewclient.AlternatePhone.Replace("-", "").Trim();

                    viewclientCMS.Person.Demographic = new dtoDemographic();
                    viewclientCMS.Person.Demographic.LivingArrangementType = new dtoLivingArrangementType();
                    viewclientCMS.Person.Demographic.LivingArrangementType.Id = viewclient.LivingArrangementTypeId;
                    viewclientCMS.Person.Demographic.LivingStatusType = new dtoLivingStatusType();
                    viewclientCMS.Person.Demographic.LivingStatusType.Id = viewclient.LivingStatusTypeId;
                    viewclientCMS.Person.Demographic.MaritalStatusType = new dtoMaritalStatusType();
                    viewclientCMS.Person.Demographic.MaritalStatusType.Id = viewclient.MaritalStatusTypeId;
                    viewclientCMS.Person.Demographic.RaceType = new dtoRaceType();
                    viewclientCMS.Person.Demographic.RaceType.Id = viewclient.RaceTypeId;
                    viewclientCMS.Person.Demographic.GenderType = new dtoGenderType();
                    viewclientCMS.Person.Demographic.GenderType.Id = viewclient.GenderTypeId;
                    viewclientCMS.Person.Demographic.CreatedBy = username;
                    viewclientCMS.Person.Demographic.UpdatedBy = username;

                    try
                    {
                        if (viewclient.PersonKey != null)
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
                        }
                        dtoPerson dtoperson = new dtoPerson();
                        dtoperson = CMSService.Save(viewclientCMS.Person, true);
                        //using (var PersonService = new PersonProfileWCFServiceClient())
                        //{
                        //    dtoperson = PersonService.Save(viewclientCMS.Person, true);
                        //}
                        viewclientCMS.Person = dtoperson;
                        viewclientCMS.PersonKey = dtoperson.PersonKey;
                        viewclientCMS.UserCreated = username;
                        viewclientCMS.UserUpdated = username;
                        viewintake.ClientId = CMSService.SaveClientCMS(viewclientCMS);
                    }

                    catch (System.Exception e)
                    {
                        throw new System.Exception("Error with creating Client", e);
                    }
                }

                viewCaseHeader viewcaseheader = CMSService.GetCaseHeader((int)viewintake.ClientId);

                viewCaseDetail viewcasedetail = new viewCaseDetail();
                TimeSpan? datediff = viewintake.DateIntake - viewcaseheader.DateCreated;

                if (viewcaseheader.Id == 0)
                {
                    viewcaseheader = new viewCaseHeader();
                    viewcaseheader.ClientId = viewintake.ClientId;
                    //viewcaseheader.CaseWorker = UserName;
                    viewcaseheader.ContractId = (int)viewintake.ReferralAgencyTypeId;
                    viewcaseheader.UserCreated = username;
                    viewcaseheader.DateCreated = viewintake.DateIntake;
                    viewcaseheader.ClosureDate = viewintake.DateIntake.Value.AddDays(464);
                    viewcaseheader.UserUpdated = username;
                    viewcaseheader.StatusDescription = CaseStatus.Open.ToString();

                    viewcasedetail.CaseId = CMSService.SaveCaseHeader(viewcaseheader);

                }
                else if (viewcaseheader.Id != 0 && datediff.Value.TotalDays >= 90)
                {
                    viewcaseheader.DateUpdated = viewintake.DateIntake;
                    viewcaseheader.ClosureDate = viewintake.DateIntake.Value.AddDays(464);

                    viewcasedetail.CaseId = CMSService.SaveCaseHeader(viewcaseheader);

                }

                else
                {
                    viewcasedetail.CaseId = viewcaseheader.Id;
                }

                CMSService.UpdateIntakeWithCaseheaderId(viewintake.Id, viewcasedetail.CaseId);

                viewintake.CaseheaderId = viewcasedetail.CaseId;
            }

            var redirectUrl = new UrlHelper(Request.RequestContext).Action("ManageCase", "Case", new { CaseheaderId = viewintake.CaseheaderId });

            return Json(redirectUrl, JsonRequestBehavior.AllowGet);
        }

        protected int GetAge(DateTime DOB)
        {
            DateTime now = DateTime.Today;
            int age = now.Year - DOB.Year;
            if (now < DOB.AddYears(age)) age--;
            return age;
        }

        #endregion

        #region List Intakes

        public ActionResult ListIntake(int Id, string message)
        {
            ViewBag.Title = "Intakes List";
            ViewBag.Message = message;

            viewIntake intakelist = new viewIntake();

            try
            {
                intakelist.viewClient = CMSService.GetviewClient(Id);

                if (intakelist.viewClient == null)
                {
                    intakelist.viewClient = new viewClient();
                    intakelist.viewClient.Id = Id;
                }

                else
                {
                    intakelist.viewClient = CMSService.GetviewClient(Id);
                }

                intakelist.listofintakes = CMSService.ListOfIntakes(Id).ToList();

            }
            catch (ApplicationException ex)
            {
                ViewBag.Error = "Data Not Available for this Client: " + (string)ex.Data["Message"];
            }

            return PartialView(intakelist);
        }

        public ActionResult ListAllIntakes()
        {
            ViewBag.Title = "List Of All Intakes";

            viewIntake intakelist = new viewIntake();
            intakelist.caselookup = getcaselookup(username);

            try
            {
                if (User.IsInRole("CMS_ReportTakerSupervisor") || User.IsInRole("CMS_Supervisor"))
                {
                    intakelist.listofintakes = CMSService.ListAllIntakesByContracts(contractids).OrderByDescending(s => s.Id).ToList();
                }
                else if (User.IsInRole("CMS_RAAAdmin"))
                {

                    intakelist.currentUsername = username;
                    var psa = CMSService.GetPSAsByUser(username);
                    var psacontractids = (from contract in psa.ListOfContracts select contract.Id).ToList();
                    psacontractids.Add((int)psa.Id);
                    //var psacontractids = new List<int>();
                    //psacontractids.Add((int)psa.Id);

                    intakelist.listofintakes = new List<viewIntake>();
                    List<int> psaID = new List<int>();
                    psaID.Add(psa.PSAID);
                    List<viewIntake> psaIntakes = CMSService.ListOfIntakesByPSAs(psaID);

                    foreach(var i in psaIntakes)
                    {
                        intakelist.listofintakes.Add(i);
                    }

                    intakelist.listofintakes = intakelist.listofintakes.OrderByDescending(s => s.Id).ToList();
                }
                else
                {
                    //intakelist.listofintakes = CMSService.ListAllIntakesByCreatedUser(username).Where(i => contractids.Contains((int)i.ContractId)).ToList();
                    intakelist.listofintakes = CMSService.ListAllIntakesByContracts(contractids).Where(s => s.UserCreated == username || s.UserUpdated == username).OrderByDescending(s => s.Id).ToList();
                }
            }
            catch (System.Exception e)
            {
                throw (e);
            }

            return View(intakelist);
        }

        #endregion

        #region View Intake

        public virtual ActionResult ViewIntake(int Id, string mode)
        {
            ViewBag.Title = "Intake - View Intake";

            viewIntake viewintake;

            if (Id > 0)
            {
                viewintake = CMSService.GetIntake(Id);
                viewintake.caselookup = getcaselookup(username);
                viewintake.InCompleteErrors.ErrorsInIntake = false;

            }
            else
            {
                viewintake = new viewIntake();
            }

            viewintake.mode = mode;
            return View(viewintake);
        }

        #endregion

        #region Print Intake

        public ActionResult PrintIntake(int ClientId, int Id)
        {
            viewIntake viewintake;

            if (Id > 0)
                viewintake = CMSService.GetIntake(Id);

            else
                return RedirectToAction("ListIntake", new { Id = ClientId, message = " Case cannot be found" });

            viewintake.caselookup = getcaselookup(username);
            viewintake.viewClient = CMSService.GetviewClient(ClientId);
            return View(viewintake);
        }

        #endregion

        #region Edit Intake

        public ActionResult EditIntake(int Id, string message, string mode)
        {
           

            ViewBag.Title = "Edit Intake";

            viewIntake viewIntake;

            ViewBag.Message = message;
            //using (var CMSService = new AGE.CMS.Core.CaseManagementServiceRef.CaseManagementServiceClient())
            //{
                if (Id > 0)
                {

                    viewIntake = CMSService.GetIntake(Id);


                    viewIntake.caselookup = getcaselookup(username);

                    viewIntake.InCompleteErrors.ErrorsInIntake = false;

                    //if (User.IsInRole("CMS_RAAAdmin"))
                    if(viewIntake.ContractId < 1677)
                    {
                        var psa = CMSService.ListOfPSA().Where(i => i.Id == viewIntake.ContractId).FirstOrDefault();

                        viewIntake.ContractName = psa.AreaName;
                        viewIntake.ContractId = psa.Id;
                        viewIntake.AgencyTypeId = 2;
                        viewIntake.AgencyTypeDescription = "RAA";

                    }
                    else
                    {
                        viewIntake.ContractName = (from contract in viewIntake.caselookup.listofcontracts where contract.Id == viewIntake.ContractId select contract.ContractName).FirstOrDefault();
                        viewIntake.AgencyTypeId = (from contract in viewIntake.caselookup.listofcontracts where contract.Id == viewIntake.ContractId select contract.AgencyType.Id).FirstOrDefault();
                        viewIntake.AgencyTypeDescription = (from contract in viewIntake.caselookup.listofcontracts where contract.Id == viewIntake.ContractId select contract.AgencyType.Description).FirstOrDefault();
                    }

                    viewIntake.ReferralAgencyName = (from contract in viewIntake.caselookup.listofcontracts where contract.Id == viewIntake.ReferralAgencyTypeId select contract.ContractName).FirstOrDefault();
                }

                else
                {
                    viewIntake = new viewIntake();

                    viewIntake.viewClient = new viewClient();
                    viewAllegations allegations = new viewAllegations();
                    viewIntake.Allegations = codeTable.getAbuseTypes();
                    viewIntake.viewreporterinfo = new viewReporterInformation();
                    viewAbuserInformation abuserinfo = new viewAbuserInformation();
                    viewIntake.listofabusers.Add(abuserinfo);
                    viewOthersWithInformation otherinfo = new viewOthersWithInformation();
                    viewIntake.listofothers.Add(otherinfo);
                    viewIntake.ReferralTime = DateTime.Now;
                    viewIntake.UserUpdated = null;
                    viewIntake.UserCreated = username;
                    viewIntake.caselookup = getcaselookup(username);

                    if (User.IsInRole("CMS_RAAAdmin"))
                    {
                        var psa = CMSService.GetPSAsByUser(username);
                        var psacontractids = (from contract in psa.ListOfContracts select contract.Id).ToList();

                        viewIntake.ContractName = psa.AreaName;
                        viewIntake.ContractId = psa.PSAID;
                        viewIntake.AgencyTypeId = 2;
                        viewIntake.AgencyTypeDescription = "RAA";

                    }
                    else
                    {
                        if (contractids.Count > 1)
                        {

                        }
                        else
                        {
                            viewIntake.ContractId = contractids.FirstOrDefault();

                            var contract = CMSService.GetContract((int)viewIntake.ContractId);
                            viewIntake.ContractName = contract.ContractName;

                            viewIntake.AgencyTypeId = contract.AgencyTypeId;
                            viewIntake.AgencyTypeDescription = contract.AgencyType.Description;

                            viewIntake.ReferralAgencyTypeId = contractids.FirstOrDefault();
                        }
                    }

                }
            //}
            return View(setIncompleteIntakeErrors(viewIntake));
        }

        #endregion

        #region Save Intake

        public JsonResult SaveIntakeAjax(viewIntake viewIntake)
        {
            
                        viewIntake.StatusDescription = CaseStatus.Open.ToString();

                        #region Part A

                        if (viewIntake.ContractId == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select Agency");
                        }

                        if (viewIntake.DateIntake == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Intake Date is required.");
                        }

                        if (viewIntake.TimeIntake == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Intake Time is required.");
                        }

                        #endregion

                        #region Part B

                        if (viewIntake.viewClient.FirstName == null && viewIntake.viewClient.LastName == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please enter Name");
                        }

                        //if (viewIntake.viewClient.Age == null)
                        //{
                        //    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                        //    ModelState.AddModelError("CustomError", "Please enter Age");
                        //}

                        if (viewIntake.viewClient.GenderTypeId == 0)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select Gender");
                        }

                        if (viewIntake.viewClient.IsLimitedEnglishSpeaking == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select if client has Limited English Speaking");
                        }
                        else if (viewIntake.viewClient.IsLimitedEnglishSpeaking == "y" && viewIntake.viewClient.PrimaryLanguageId == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select Primary Language");
                        }

                        if (viewIntake.viewClient.AddressLine1 == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please enter address in Address Line 1");
                        }

                        if (viewIntake.viewClient.City == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select City");
                        }

                        if (viewIntake.viewClient.CountyTypeId == 0)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select County");
                        }

                        if (viewIntake.viewClient.Zip5 == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select Zip");
                        }

                        //if (viewIntake.viewClient.PhoneNumber == null)
                        //{
                        //    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                        //    ModelState.AddModelError("CustomError", "Please enter Phone Number");
                        //}

                        if (viewIntake.viewClient.Directions == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please enter Directions");
                        }

                        if (viewIntake.viewClient.BestCommunicationMethod == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please enter Best Communication Method");
                        }

                        #endregion

                        #region Part C

                        int allegationcount = 0;

                        if (viewIntake.IsANE == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please Select If it is ANE or N/A");
                        }

                        else if (viewIntake.IsANE == "y")
                        {
                            foreach (var allegation in viewIntake.Allegations)
                            {
                                if (allegation.IsChecked == true)
                                {
                                    allegationcount++;
                                }
                            }

                            if (allegationcount == 0)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please select atleast one Allegation");
                            }

                            if (viewIntake.IsANEUnderAgeWithDisability == null)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please select whether underage or not");
                            }
                            if (viewIntake.IsAAExists == false)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please check if AA Exists");
                            }
                            if (viewIntake.IsANEDomesticSetting == false)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please check if Domesting Setting");
                            }
                            if (viewIntake.IsAllegationsConstituteAbuse == false)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please check if Allegations Constitute Abuse");
                            }

                            foreach (var allegation in viewIntake.Allegations)
                            {
                                if (allegation.IsChecked && allegation.Specify == null)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please Expalin Allegation");
                                }
                            }

                        }

                        if (viewIntake.IsSN == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please Select If it is SN or N/A");
                        }

                        else if (viewIntake.IsSN == "y")
                        {
                            if (viewIntake.IsImmediateMedicalNeed == false && viewIntake.IsAloneUnableCareSelf == false && viewIntake.IsJudgmentGrosslyImpaired == false &&
                                viewIntake.IsLackFoodClothingShelter == false && viewIntake.IsSignificantStructuralHazard == false && viewIntake.IsOtherReportedPrblms == false)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please select atleast one Reported Problem");
                            }

                            if (viewIntake.IsSNUnderAgeWithDisability == null)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please select whether underage or not");
                            }

                            if (viewIntake.IsSNDomesticSetting == false)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please check if Domesting Setting");
                            }

                            if (viewIntake.IsImmediateMedicalNeed == true && viewIntake.ImmediateMedicalNeed == null)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please explain Immediate Medical Need");
                            }
                            if (viewIntake.IsAloneUnableCareSelf == true && viewIntake.AloneUnableCareSelf == null)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please explain Alone and Unable to Care Self");
                            }
                            if (viewIntake.IsJudgmentGrosslyImpaired == true && viewIntake.JudgmentGrosslyImpaired == null)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please explain Judgment Grossly Impaired");
                            }
                            if (viewIntake.IsLackFoodClothingShelter == true && viewIntake.LackFoodClothingShelter == null)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please explain Lack of Food Clothing Shelter");
                            }
                            if (viewIntake.IsSignificantStructuralHazard == true && viewIntake.SignificantStructuralHazard == null)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please explain Significant Structural Hazard");
                            }
                            if (viewIntake.IsOtherReportedPrblms == true && viewIntake.OtherReportedPrblms == null)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please explain Other Reported Problems");
                            }
                        }

                        if (viewIntake.PriorityId == 0)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select Priority");
                        }

                        if (viewIntake.IsClientAwareOfReport == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please answer to whether Client aware of report");
                        }


                        if (viewIntake.IsClientInImmediateDanger == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please answer to whether Client is in immediate danger");
                        }


                        if (viewIntake.ClientCondition == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please enter Client's condition");
                        }

                        if (viewIntake.IsClientInNeedOfImmediateAssistance == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please answer to whether Client need immediate assistance");
                        }


                        if (viewIntake.IsClientInNeedOfSpecialAccom == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please answer to whether Client need special accomodation");
                        }
                        else if (viewIntake.IsClientInNeedOfSpecialAccom == "y")
                        {
                            if (viewIntake.Hearing == false && viewIntake.Speech == false && viewIntake.Vision == false &&
                                viewIntake.Motor == false && viewIntake.Communication == false && viewIntake.IsOtherSpecialAccom == false)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please select atleast one special accomodation type");
                            }
                        }

                        if (viewIntake.IsInDangerousSituation == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please answer to whether Client is in Dangerious Situation");
                        }

                        else if (viewIntake.IsInDangerousSituation == "y")
                        {
                            if (viewIntake.Neighborhood == false && viewIntake.Animals == false && viewIntake.MentalIllness == false &&
                                viewIntake.DrugsOrAlcohol == false && viewIntake.Weapons == false && viewIntake.EnvironmnetOrCodeViolations == false &&
                                viewIntake.Infestation == false && viewIntake.IsOtherDangerousSituation == false)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please select atleast one dangerous situaion type");
                            }
                        }


                        #endregion

                        #region Part D
                        if (viewIntake.IsNoAbusers == false)
                        {
                            if (viewIntake.viewreporterinfo.FirstName == null && viewIntake.viewreporterinfo.LastName == null)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please enter Reporters Name");
                            }

                            if (viewIntake.viewreporterinfo.WillProvideFurInfo == null)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please select if reporter is willing to provide further information");
                            }
                            else
                            {
                                if (viewIntake.viewreporterinfo.Phone == null)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please enter phone number");
                                }

                            }

                            if (viewIntake.viewreporterinfo.ReporterTypeId == 0)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please select reporter type");
                            }
                        }
                        #endregion

                        #region Part E

                        if (viewIntake.IsNoAbusers != true)
                        {
                            foreach (var abuser in viewIntake.listofabusers)
                            {
                                if (abuser.RelationId == 0)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please select Relation type");
                                }

                                if (abuser.IsAbuserAwareOfReport == null)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please select if abuser aware of report or not");
                                }

                                if (abuser.Age == null)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please select Age");
                                }

                                if (abuser.ContactTime == null)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please select Contact time or place");
                                }

                                if (abuser.MentalCondition == null)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please enter abuser's Mental/Physical Condition");
                                }

                            }
                        }


                        #endregion

                        #region Part F

                        if (viewIntake.IsNoOthers != true)
                        {
                            foreach (var other in viewIntake.listofothers)
                            {
                                if (other.RelationId == 0)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please select Relation type");
                                }

                                if (other.IsAwareOfReport == null)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please select if other aware of report or not");
                                }

                                if (other.FirstName == null && other.LastName == null)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please enter name");
                                }

                                if (other.Phone == null)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please enter Phone number");
                                }

                                if (other.ContactTime == null)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please enter Contact time or place");
                                }
                            }
                        }


                        #endregion

                        #region Part G


                        //if (viewIntake.ReferralDate == null)
                        //{
                        //    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                        //    ModelState.AddModelError("CustomError", "Please enter Referral Date");
                        //}


                        #endregion

                        viewIntake.UserCreated = username;
                        viewIntake.UserUpdated = username;
                        viewIntake.mode = "edit";
                        // int intakeid = new int();
                        //using (var CMSService = new CaseManagementServiceClient())
                        //{

                        //}
                        int intakeid = 0;
                        //tlh
                        //using (var CMSService = new AGE.CMS.Core.CaseManagementServiceRef.CaseManagementServiceClient())
                        //{
                        intakeid = CMSService.SaveIntake(viewIntake, true);
            //}


            return Json(intakeid);


            //return RedirectToAction("ListAllIntakes", new { message = " intake Saved Successfully" });
        }

        [ValidateInput(false)]
        public ActionResult SaveIntake(viewIntake viewIntake)
        {
            if (viewIntake == null)
            {
                return RedirectToAction("ListAllIntakes", "Intake");
            }
            try
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).ToArray();

                if (!ModelState.IsValid)
                {

                    if (viewIntake.caselookup == null) viewIntake.caselookup = getcaselookup(username);
                    if (viewIntake.viewabuserinfo == null) viewIntake.viewabuserinfo = getabuserinfo();
                    viewIntake.ContractName = (from contract in viewIntake.caselookup.listofcontracts where contract.Id == viewIntake.ContractId select contract.ContractName).FirstOrDefault();

                    return RedirectToAction("ListAllIntakes", "Intake");
                }

                else
                {
                    if (!ModelState.IsValid)
                    {
                        if (viewIntake.caselookup == null) viewIntake.caselookup = getcaselookup(username);
                        if (viewIntake.viewabuserinfo == null) viewIntake.viewabuserinfo = getabuserinfo();
                        viewIntake.ContractName = (from contract in viewIntake.caselookup.listofcontracts where contract.Id == viewIntake.ContractId select contract.ContractName).FirstOrDefault();
                        return RedirectToAction("ListAllIntakes", "Intake");
                    }
                    else
                    {
                        viewIntake.StatusDescription = CaseStatus.Open.ToString();

                        #region Part A

                        if (viewIntake.ContractId == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select Agency");
                        }

                        if (viewIntake.DateIntake == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Intake Date is required.");
                        }

                        if (viewIntake.TimeIntake == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Intake Time is required.");
                        }

                        #endregion

                        #region Part B

                        if (viewIntake.viewClient.FirstName == null && viewIntake.viewClient.LastName == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please enter Name");
                        }

                        //if (viewIntake.viewClient.Age == null)
                        //{
                        //    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                        //    ModelState.AddModelError("CustomError", "Please enter Age");
                        //}

                        if (viewIntake.viewClient.GenderTypeId == 0)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select Gender");
                        }

                        if (viewIntake.viewClient.IsLimitedEnglishSpeaking == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select if client has Limited English Speaking");
                        }
                        else if (viewIntake.viewClient.IsLimitedEnglishSpeaking == "y" && viewIntake.viewClient.PrimaryLanguageId == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select Primary Language");
                        }

                        if (viewIntake.viewClient.AddressLine1 == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please enter address in Address Line 1");
                        }

                        if (viewIntake.viewClient.City == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select City");
                        }

                        if (viewIntake.viewClient.CountyTypeId == 0)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select County");
                        }

                        if (viewIntake.viewClient.Zip5 == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select Zip");
                        }

                        //if (viewIntake.viewClient.PhoneNumber == null)
                        //{
                        //    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                        //    ModelState.AddModelError("CustomError", "Please enter Phone Number");
                        //}

                        if (viewIntake.viewClient.Directions == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please enter Directions");
                        }

                        if (viewIntake.viewClient.BestCommunicationMethod == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please enter Best Communication Method");
                        }

                        #endregion

                        #region Part C

                        int allegationcount = 0;

                        if (viewIntake.IsANE == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please Select If it is ANE or N/A");
                        }

                        else if (viewIntake.IsANE == "y")
                        {
                            foreach (var allegation in viewIntake.Allegations)
                            {
                                if (allegation.IsChecked == true)
                                {
                                    allegationcount++;
                                }
                            }

                            if (allegationcount == 0)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please select atleast one Allegation");
                            }

                            if (viewIntake.IsANEUnderAgeWithDisability == null)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please select whether underage or not");
                            }
                            if (viewIntake.IsAAExists == false)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please check if AA Exists");
                            }
                            if (viewIntake.IsANEDomesticSetting == false)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please check if Domesting Setting");
                            }
                            if (viewIntake.IsAllegationsConstituteAbuse == false)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please check if Allegations Constitute Abuse");
                            }

                            foreach (var allegation in viewIntake.Allegations)
                            {
                                if (allegation.IsChecked && allegation.Specify == null)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please Expalin Allegation");
                                }
                            }

                        }

                        if (viewIntake.IsSN == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please Select If it is SN or N/A");
                        }

                        else if (viewIntake.IsSN == "y")
                        {
                            if (viewIntake.IsImmediateMedicalNeed == false && viewIntake.IsAloneUnableCareSelf == false && viewIntake.IsJudgmentGrosslyImpaired == false &&
                                viewIntake.IsLackFoodClothingShelter == false && viewIntake.IsSignificantStructuralHazard == false && viewIntake.IsOtherReportedPrblms == false)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please select atleast one Reported Problem");
                            }

                            if (viewIntake.IsSNUnderAgeWithDisability == null)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please select whether underage or not");
                            }

                            if (viewIntake.IsSNDomesticSetting == false)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please check if Domesting Setting");
                            }

                            if (viewIntake.IsImmediateMedicalNeed == true && viewIntake.ImmediateMedicalNeed == null)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please explain Immediate Medical Need");
                            }
                            if (viewIntake.IsAloneUnableCareSelf == true && viewIntake.AloneUnableCareSelf == null)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please explain Alone and Unable to Care Self");
                            }
                            if (viewIntake.IsJudgmentGrosslyImpaired == true && viewIntake.JudgmentGrosslyImpaired == null)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please explain Judgment Grossly Impaired");
                            }
                            if (viewIntake.IsLackFoodClothingShelter == true && viewIntake.LackFoodClothingShelter == null)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please explain Lack of Food Clothing Shelter");
                            }
                            if (viewIntake.IsSignificantStructuralHazard == true && viewIntake.SignificantStructuralHazard == null)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please explain Significant Structural Hazard");
                            }
                            if (viewIntake.IsOtherReportedPrblms == true && viewIntake.OtherReportedPrblms == null)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please explain Other Reported Problems");
                            }
                        }

                        if (viewIntake.PriorityId == 0)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please select Priority");
                        }

                        if (viewIntake.IsClientAwareOfReport == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please answer to whether Client aware of report");
                        }


                        if (viewIntake.IsClientInImmediateDanger == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please answer to whether Client is in immediate danger");
                        }


                        if (viewIntake.ClientCondition == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please enter Client's condition");
                        }

                        if (viewIntake.IsClientInNeedOfImmediateAssistance == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please answer to whether Client need immediate assistance");
                        }


                        if (viewIntake.IsClientInNeedOfSpecialAccom == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please answer to whether Client need special accomodation");
                        }
                        else if (viewIntake.IsClientInNeedOfSpecialAccom == "y")
                        {
                            if (viewIntake.Hearing == false && viewIntake.Speech == false && viewIntake.Vision == false &&
                                viewIntake.Motor == false && viewIntake.Communication == false && viewIntake.IsOtherSpecialAccom == false)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please select atleast one special accomodation type");
                            }
                        }

                        if (viewIntake.IsInDangerousSituation == null)
                        {
                            viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                            ModelState.AddModelError("CustomError", "Please answer to whether Client is in Dangerious Situation");
                        }

                        else if (viewIntake.IsInDangerousSituation == "y")
                        {
                            if (viewIntake.Neighborhood == false && viewIntake.Animals == false && viewIntake.MentalIllness == false &&
                                viewIntake.DrugsOrAlcohol == false && viewIntake.Weapons == false && viewIntake.EnvironmnetOrCodeViolations == false &&
                                viewIntake.Infestation == false && viewIntake.IsOtherDangerousSituation == false)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please select atleast one dangerous situaion type");
                            }
                        }


                        #endregion

                        #region Part D
                        if (viewIntake.IsNoAbusers == false)
                        {
                            if (viewIntake.viewreporterinfo.FirstName == null && viewIntake.viewreporterinfo.LastName == null)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please enter Reporters Name");
                            }

                            if (viewIntake.viewreporterinfo.WillProvideFurInfo == null)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please select if reporter is willing to provide further information");
                            }
                            else
                            {
                                if (viewIntake.viewreporterinfo.Phone == null)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please enter phone number");
                                }

                            }

                            if (viewIntake.viewreporterinfo.ReporterTypeId == 0)
                            {
                                viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                ModelState.AddModelError("CustomError", "Please select reporter type");
                            }
                        }
                        #endregion

                        #region Part E

                        if (viewIntake.IsNoAbusers != true)
                        {
                            foreach (var abuser in viewIntake.listofabusers)
                            {
                                if (abuser.RelationId == 0)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please select Relation type");
                                }

                                if (abuser.IsAbuserAwareOfReport == null)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please select if abuser aware of report or not");
                                }

                                if (abuser.Age == null)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please select Age");
                                }

                                if (abuser.ContactTime == null)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please select Contact time or place");
                                }

                                if (abuser.MentalCondition == null)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please enter abuser's Mental/Physical Condition");
                                }

                            }
                        }


                        #endregion

                        #region Part F

                        if (viewIntake.IsNoOthers != true)
                        {
                            foreach (var other in viewIntake.listofothers)
                            {
                                if (other.RelationId == 0)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please select Relation type");
                                }

                                if (other.IsAwareOfReport == null)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please select if other aware of report or not");
                                }

                                if (other.FirstName == null && other.LastName == null)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please enter name");
                                }

                                if (other.Phone == null)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please enter Phone number");
                                }

                                if (other.ContactTime == null)
                                {
                                    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                                    ModelState.AddModelError("CustomError", "Please enter Contact time or place");
                                }
                            }
                        }


                        #endregion

                        #region Part G


                        //if (viewIntake.ReferralDate == null)
                        //{
                        //    viewIntake.StatusDescription = CaseStatus.Incomplete.ToString();
                        //    ModelState.AddModelError("CustomError", "Please enter Referral Date");
                        //}


                        #endregion

                        viewIntake.UserCreated = username;
                        viewIntake.UserUpdated = username;
                        viewIntake.mode = "edit";
                        // int intakeid = new int();
                        //using (var CMSService = new CaseManagementServiceClient())
                        //{

                        //}
                        int intakeid = 0;
                        //tlh
                        //using (var CMSService = new AGE.CMS.Core.CaseManagementServiceRef.CaseManagementServiceClient())
                        //{
                            intakeid = CMSService.SaveIntake(viewIntake, true);
                        //}




                        if (viewIntake.isAjax == true)
                        {
                            bool NoErrorsInIntake = ValidateIntake(intakeid);

                            var listofvalues = new List<string>();
                            listofvalues.Add(intakeid.ToString());
                            listofvalues.Add(NoErrorsInIntake.ToString());
                            return Json(listofvalues, JsonRequestBehavior.AllowGet);
                        }
                        else if (viewIntake.isSessionMakeLive == true)
                        {
                            return RedirectToAction("EditIntake", "Intake", new { Id = intakeid });
                        }

                        else
                        {
                            return RedirectToAction("ListAllIntakes", "Intake");
                        }
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

            //return RedirectToAction("ListAllIntakes", new { message = " intake Saved Successfully" });
        }

        #endregion

        #region Submit Intake

        [HttpPost]
        //[MultipleButton(Name = "action", Argument = "SubmitIntake")]
        public virtual JsonResult SubmitIntake(int Id, int ReferralAgencyTypeID)
        {
            viewIntake viewintake = CMSService.GetIntake(Id);

            viewintake.ReferralAgencyTypeId = ReferralAgencyTypeID;
            viewintake.ReferralDate = DateTime.Now;
            viewintake.mode = "T";
            var contract = CMSService.GetContract(ReferralAgencyTypeID);
            viewintake.StatusDescription = CaseStatus.Submitted.ToString();

            CMSService.SaveIntake(viewintake, true);

            List<int> ContractIds = new List<int>();
            ContractIds.Add(contract.Id);


            string Message = "An APS Intake (Id:" + Id + ") and Client (Name: " + viewintake.viewClient.FirstName + " " + viewintake.viewClient.LastName + ") has been submitted for " + contract.ContractName + ".";
            string Subject = "New APS Intake";

            List<MailAddress> EMails = new List<MailAddress>();
            EMails.Add(new MailAddress(contract.Email, "To"));
            //List<string> emailids = GetMailByRole(ContractIds, "AGE.CMS", Roles.CMS_Supervisor.ToString());

            //if (emailids.Count > 0)
            //{
            //    foreach (var email in emailids)
            //    {
            //        EMails.Add(new MailAddress(email, "To", System.Text.Encoding.UTF8));
            //    }
            //    SendEmail(EMails, Message, Subject);
            //}
            SendEmail(EMails, Message, Subject);
            var redirectUrl = new UrlHelper(Request.RequestContext).Action("ListAllIntakes", "Intake");
            return Json(redirectUrl, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region RemoveIntake

        public ActionResult RemoveIntake(int Id)
        {
            CMSService.UpdateIntakeStatus(Id, CaseStatus.Unserviced.ToString(), username);
            return Json("Intake removed successfully!");
        }

        #endregion

        #region CloseIntake
        
        public virtual ActionResult CloseIntake(int Id)
        {
            var ListOfErrors = CMSService.ValidateIntakeClosing(Id);
            if (ListOfErrors.Count > 0)
            {
                return Json(ListOfErrors, JsonRequestBehavior.AllowGet);
            }
            else
            {
                int caseheaderId = CMSService.IntakeClosure(Id);
                var redirectUrl = new UrlHelper(Request.RequestContext).Action("ManageCase", "Case", new { CaseheaderId = caseheaderId });
                var URLandId = new { url = redirectUrl, hell = 12 };
                return Json(URLandId, JsonRequestBehavior.AllowGet);
            }
        
        }
        #endregion

        #region Transfer Intake

        public ActionResult Transfer(int Id, int ReferralAgencyTypeID)
        {
            viewIntake viewintake = CMSService.GetIntake(Id);

            viewintake.ReferralAgencyTypeId = ReferralAgencyTypeID;
            viewintake.viewClient = CMSService.GetviewClient(Convert.ToInt32(viewintake.TempClientId));
            viewintake.viewClient.CreatedBy = viewintake.UserCreated;
            viewintake.viewClient.CreatedDateTime = DateTime.Now;

            return View(viewintake);
        }

        #endregion

        #region Email

        public ActionResult Contact()
        {
            EmailForm form = new EmailForm();

            form.ToEmail = "mounish.thatikonda@illinois.gov";
            form.FromName = "Department on Aging";
            form.Subject = "New Intake is added";
            form.FromEmail = "praneeth.bommineni@illinois.gov";
            form.Message = "Hi Folks, new Intake is added to the system, please review";

            return RedirectToAction("ContactPost", form);
        }

        public async Task<ActionResult> ContactPost(EmailForm model)
        {
            var body = "<p>Email From: {0} ({1})</p><p>Message:</p><p>{2}</p>";
            var message = new MailMessage();
            message.Priority = MailPriority.High;

            message.To.Add(model.ToEmail);
            message.Subject = model.Subject;
            message.Body = string.Format(body, model.FromName, model.FromEmail, model.Message);
            message.IsBodyHtml = true;
            if (model.Upload != null && model.Upload.ContentLength > 0)
            {
                message.Attachments.Add(new Attachment(model.Upload.InputStream, Path.GetFileName(model.Upload.FileName)));
            }

            using (var smtp = new SmtpClient())
            {
                await smtp.SendMailAsync(message);
                TempData["Message"] = "IntakeSubmitted";
                return RedirectToAction("ListAllIntakes", new { UserName = username });
            }

        }

        [HttpPost]
        public async Task<ActionResult> Contact(EmailForm model)
        {
            if (ModelState.IsValid)
            {
                var body = "<p>Email From: {0} ({1})</p><p>Message:</p><p>{2}</p>";
                var message = new MailMessage();
                message.Priority = MailPriority.High;

                message.To.Add(model.ToEmail);
                message.Subject = model.Subject;
                message.Body = string.Format(body, model.FromName, model.FromEmail, model.Message);
                message.IsBodyHtml = true;
                if (model.Upload != null && model.Upload.ContentLength > 0)
                {
                    message.Attachments.Add(new Attachment(model.Upload.InputStream, Path.GetFileName(model.Upload.FileName)));
                }

                using (var smtp = new SmtpClient())
                {
                    //var credential = new NetworkCredential
                    //{
                    //    UserName = "praneethbommineni@gmail.com",  // replace with valid value
                    //    Password = "password"  // replace with valid value
                    //};
                    //smtp.Credentials = credential;
                    //smtp.Host = "smtp.gmail.com";
                    //smtp.Port = 465;
                    //smtp.EnableSsl = true;
                    await smtp.SendMailAsync(message);
                    //smtp.Send(message);
                    return RedirectToAction("ListAllIntakes", new { UserName = username });
                }
            }
            return View(model);

        }

        public ActionResult Sent()
        {
            return View();
        }

        #endregion

        #region EditClient

        public ActionResult EditClient(int Id, string mode)
        {
            if (mode == "case")
            {
                viewClientCMS client = CMSService.GetviewClientCMS((int)Id);
                viewClientCMS result = new viewClientCMS();
                if (client != null && client.Id > 0)
                {
                    result.Id = client.Id;
                    result.PersonKey = client.PersonKey;
                    result.BestCommunicationMethod = client.BestCommunicationMethod;
                    using (var PersonService = new PersonProfileWCFServiceClient())
                    {
                        result.Person = PersonService.GetPerson((Guid)client.PersonKey);
                    }
                    client.UserUpdated = username;

                    client.caselookup = getcaselookup(username);
                    return View(client);
                }
                else
                {
                    using (var PersonService = new PersonProfileWCFServiceClient())
                    {
                        result.Person = PersonService.GetPerson(Guid.Empty);
                    }
                    result.Id = Id;
                    result.PersonKey = Guid.Empty;
                    result.UserCreated = username;
                    result.UserUpdated = null;

                    result.UserCreated = username;
                    result.caselookup = getcaselookup(username);

                    return View(result);
                }
            }

            else
            {
                viewClient client = CMSService.GetviewClient(Convert.ToInt32(Id));
                viewClient result = new viewClient();

                if (client != null && client.Id > 0)
                {
                    client.UserUpdated = username;
                    client.caselookup = getcaselookup(username);

                    return View(client);
                }
                else
                {
                    result.UserUpdated = null;
                    result.UserCreated = username;
                    result.caselookup = getcaselookup(username);

                    return View(result);
                }
            }

        }

        #endregion

        #region SaveClient

        [HttpPost]
        public ActionResult SaveClient(viewClientCMS viewclient)
        {
            ViewBag.Title = "Edit Client";
            try
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                if (!ModelState.IsValid)
                {
                    dtoPerson person = new dtoPerson();
                    using (var PersonService = new PersonProfileWCFServiceClient())
                    {
                        person = PersonService.GetPerson(Guid.Empty);
                    }
                    viewclient.Person.Address.ListOfStateTypes = person.Address.ListOfStateTypes;

                    if (viewclient.Person.Demographic == null)
                    {
                        viewclient.Person.Demographic = new dtoDemographic();

                    }

                    viewclient.Person.Demographic.DemographicList = new dtoDemographicList();
                    viewclient.Person.Demographic.DemographicList.ListOfGenderTypes = person.Demographic.DemographicList.ListOfGenderTypes;
                    viewclient.Person.Demographic.DemographicList.ListOfLivingArrangementTypes = person.Demographic.DemographicList.ListOfLivingArrangementTypes;
                    viewclient.Person.Demographic.DemographicList.ListOfLivingStatusTypes = person.Demographic.DemographicList.ListOfLivingStatusTypes;
                    viewclient.Person.Demographic.DemographicList.ListOfMaritalStatusTypes = person.Demographic.DemographicList.ListOfMaritalStatusTypes;
                    viewclient.Person.Demographic.DemographicList.ListOfRaceTypes = person.Demographic.DemographicList.ListOfRaceTypes;

                    ModelState.AddModelError("CustomError", "Invalid values entered: Please correct");

                    return View("EditClient", viewclient);
                }
                else
                {
                    viewclient.UserCreated = username;
                    viewclient.DateCreated = DateTime.Now;
                    viewclient.Id = CMSService.SaveClientCMS(viewclient);
                }
            }
            catch (FaultException ex)
            {
                dtoPerson person = new dtoPerson();
                using (var PersonService = new PersonProfileWCFServiceClient())
                {
                    person = PersonService.GetPerson(Guid.Empty);

                }
                viewclient.Person.Address.ListOfStateTypes = person.Address.ListOfStateTypes;

                if (viewclient.Person.Demographic == null)
                    viewclient.Person.Demographic = new dtoDemographic();


                viewclient.Person.Demographic.DemographicList = new dtoDemographicList();
                viewclient.Person.Demographic.DemographicList.ListOfGenderTypes = person.Demographic.DemographicList.ListOfGenderTypes;
                viewclient.Person.Demographic.DemographicList.ListOfLivingArrangementTypes = person.Demographic.DemographicList.ListOfLivingArrangementTypes;
                viewclient.Person.Demographic.DemographicList.ListOfLivingStatusTypes = person.Demographic.DemographicList.ListOfLivingStatusTypes;
                viewclient.Person.Demographic.DemographicList.ListOfMaritalStatusTypes = person.Demographic.DemographicList.ListOfMaritalStatusTypes;
                viewclient.Person.Demographic.DemographicList.ListOfRaceTypes = person.Demographic.DemographicList.ListOfRaceTypes;

                ModelState.AddModelError("CustomError", (string)ex.Message);
                return View("EditClient", viewclient);
            }

            catch (ApplicationException ex)
            {
                dtoPerson person = new dtoPerson();
                using (var PersonService = new PersonProfileWCFServiceClient())
                {
                    person = PersonService.GetPerson(Guid.Empty);
                }

                viewclient.Person.Address.ListOfStateTypes = person.Address.ListOfStateTypes;

                if (viewclient.Person.Demographic == null)
                    viewclient.Person.Demographic = new dtoDemographic();


                viewclient.Person.Demographic.DemographicList = new dtoDemographicList();
                viewclient.Person.Demographic.DemographicList.ListOfGenderTypes = person.Demographic.DemographicList.ListOfGenderTypes;
                viewclient.Person.Demographic.DemographicList.ListOfLivingArrangementTypes = person.Demographic.DemographicList.ListOfLivingArrangementTypes;
                viewclient.Person.Demographic.DemographicList.ListOfLivingStatusTypes = person.Demographic.DemographicList.ListOfLivingStatusTypes;
                viewclient.Person.Demographic.DemographicList.ListOfMaritalStatusTypes = person.Demographic.DemographicList.ListOfMaritalStatusTypes;
                viewclient.Person.Demographic.DemographicList.ListOfRaceTypes = person.Demographic.DemographicList.ListOfRaceTypes;

                ModelState.AddModelError("CustomError", (string)ex.Data["Message"]);
                return View("SearchResults", viewclient);
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ModelState.AddModelError("", ex.ToString());

                if (ex.InnerException != null)
                    ModelState.AddModelError("", ex.InnerException.ToString() + "<br> System");

                dtoPerson person = new dtoPerson();
                using (var PersonService = new PersonProfileWCFServiceClient())
                {
                    person = PersonService.GetPerson(Guid.Empty);
                }

                viewclient.Person.Address.ListOfStateTypes = person.Address.ListOfStateTypes;

                if (viewclient.Person.Demographic == null)
                    viewclient.Person.Demographic = new dtoDemographic();


                viewclient.Person.Demographic.DemographicList = new dtoDemographicList();
                viewclient.Person.Demographic.DemographicList.ListOfGenderTypes = person.Demographic.DemographicList.ListOfGenderTypes;
                viewclient.Person.Demographic.DemographicList.ListOfLivingArrangementTypes = person.Demographic.DemographicList.ListOfLivingArrangementTypes;
                viewclient.Person.Demographic.DemographicList.ListOfLivingStatusTypes = person.Demographic.DemographicList.ListOfLivingStatusTypes;
                viewclient.Person.Demographic.DemographicList.ListOfMaritalStatusTypes = person.Demographic.DemographicList.ListOfMaritalStatusTypes;
                viewclient.Person.Demographic.DemographicList.ListOfRaceTypes = person.Demographic.DemographicList.ListOfRaceTypes;

                return View("EditClient", viewclient);
            }

            if (viewclient.IntakeId == 0)
                return RedirectToAction("SearchResults", new { LastName = viewclient.Person.LastName, FirstName = viewclient.Person.FirstName, DOB = viewclient.Person.DOB, IsPartial = false, message = " Client Saved Successfully" });

            else
                return RedirectToAction("EditIntake", new { ClientId = viewclient.Id, Id = viewclient.IntakeId });

        }
        #endregion

        #region Helpers

        protected AGE.CMS.Data.Models.Intake.viewCaseLookup getcaselookup(string username)
        {
            AGE.CMS.Data.Models.Intake.viewCaseLookup caselookup = new AGE.CMS.Data.Models.Intake.viewCaseLookup();

            CodeTable codeTable = (CodeTable)Session["CODETABLE"];

            CaseManagementService CMSService = new CaseManagementService((LoggedInUser)(Session["USER"]), log4net.LogManager.GetLogger(typeof(MvcApplication)), codeTable);

                caselookup.listofcontracts = CMSService.ListOfContracts(username).ToList(); 
                
                caselookup.listofallcontracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code != "05").ToList();

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
                caselookup.listofgender = codeTable.ListOfGenderTypes;
                caselookup.listoflivingstatus = codeTable.ListOfLivingStatusTypes;
                caselookup.listofraces = codeTable.ListOfRaceTypes;
                caselookup.listofcounties = codeTable.ListOfCountyTypes;
                caselookup.listofstates = codeTable.ListOfStateTypes;
                caselookup.listofmaritalstatus = codeTable.ListOfMaritalStatusTypes;
                caselookup.listoflivingarrangements = codeTable.ListOfLivingArrangementTypes;


            //using (var PersonProfileWCFServiceClient = new PersonProfileWCFServiceClient())
            //{


            //    caselookup.listoflivingstatus = person.Demographic.DemographicList.ListOfLivingStatusTypes;
            //    caselookup.listofraces = person.Demographic.DemographicList.ListOfRaceTypes;
            //    caselookup.listofgender = new List<PersonProfile.Data.Entities.dtoGenderType>(PersonProfileWCFServiceClient.ListOfGenderTypes());
            //    caselookup.listofgender = new List<PersonProfile.Data.Entities.dtoGenderType>(PersonProfileWCFServiceClient.ListOfGenderTypes());

            //    caselookup.listofcounties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonProfileWCFServiceClient.ListOfCountyTypes(14)).ToList();
            //    caselookup.listofstates = new List<PersonProfile.Data.Entities.dtoStateType>(PersonProfileWCFServiceClient.ListOfStateTypes());
            //    caselookup.listofmaritalstatus = new List<PersonProfile.Data.Entities.dtoMaritalStatusType>(PersonProfileWCFServiceClient.ListOfMaritalStatusTypes());
            //    caselookup.listoflivingarrangements = new List<PersonProfile.Data.Entities.dtoLivingArrangementType>(PersonProfileWCFServiceClient.ListOfLivingArrangementTypes());
            //}

            return caselookup;
        }

        protected viewAbuserInformation getabuserinfo()
        {
            viewAbuserInformation viewabuserinfo = new viewAbuserInformation();
            return viewabuserinfo;
        }

        protected viewOthersWithInformation getothersinfo()
        {
            viewOthersWithInformation viewOthersWithInfo = new viewOthersWithInformation();
            return viewOthersWithInfo;
        }

        [HttpPost]
        public JsonResult GetAgencyType(int contractId)
        {
            var type = CMSService.ListOfAllContracts().Where(i => i.Id == contractId).FirstOrDefault().AgencyType.Description;

            return Json(type);
        }

        protected bool ValidateIntake(int IntakeId)
        {
            var vintake = new viewIntake();
            //using (var CMSService = new AGE.CMS.Core.CaseManagementServiceRef.CaseManagementServiceClient())
            //{
                vintake = CMSService.GetIntake(IntakeId);
            //}

            if (vintake.Id > 0)
            {
                #region Part A

                if (vintake.ContractId == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsContractId = true;
                }
                //if (vintake.AgencyTypeId == null)
                //{
                //    vintake.InCompleteErrors.ErrorsInIntake = true;
                //    vintake.InCompleteErrors.HasErrorsAgencyTypeId = true;
                //}

                //if (vintake.DateIntake == Convert.ToDateTime("1/1/0001"))
                if (vintake.DateIntake == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsDateIntake = true;
                }

                if (vintake.TimeIntake == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTimeIntake = true;
                }

                #endregion

                #region Part B

                if (vintake.viewClient.FirstName == null && vintake.viewClient.LastName == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTempClientName = true;
                }

                if (vintake.viewClient.Age == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTempClientAge = true;
                }

                if (vintake.viewClient.GenderTypeId == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTempClientGender = true;
                }

                if (vintake.viewClient.IsLimitedEnglishSpeaking == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTempClientLimitedEnglishSpeaking = true;
                }
                else if (vintake.viewClient.IsLimitedEnglishSpeaking == "y" && vintake.viewClient.PrimaryLanguageId == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTempClientPrimaryLanguage = true;
                }

                if (vintake.viewClient.AddressLine1 == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTempClientAddressline1 = true;
                }

                if (vintake.viewClient.City == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTempClientCity = true;
                }

                if (vintake.viewClient.CountyTypeId == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTempClientCounty = true;
                }

                if (vintake.viewClient.Zip5 == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTempClientZip = true;
                }

                //if (vintake.viewClient.PhoneNumber == null)
                //{
                //    vintake.InCompleteErrors.ErrorsInIntake = true;
                //    vintake.InCompleteErrors.HasErrorsTempClientPhone = true;
                //}

                if (vintake.viewClient.Directions == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTempClientDirections = true;
                }

                if (vintake.viewClient.BestCommunicationMethod == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTempClientbestCommunication = true;
                }


                #endregion

                #region Part C

                int allegationcount = 0;

                if (vintake.IsANE == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsANE = true;
                }

                else if (vintake.IsANE == "y")
                {
                    foreach (var allegation in vintake.Allegations)
                    {
                        if (allegation.IsChecked == true)
                        {
                            allegationcount++;
                        }
                    }

                    if (allegationcount == 0)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsInANEAllegations = true;
                    }

                    if (vintake.IsANEUnderAgeWithDisability == null)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsANEAge = true;
                    }
                    if (vintake.IsAAExists == false)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsANEAAExists = true;
                    }
                    if (vintake.IsANEDomesticSetting == false)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsANEDomesticSetting = true;
                    }
                    if (vintake.IsAllegationsConstituteAbuse == false)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsAllegationsConstituteAbuse = true;
                    }
                    foreach (var allegation in vintake.Allegations)
                    {
                        if (allegation.IsChecked && allegation.Specify == null)
                        {
                            vintake.InCompleteErrors.ErrorsInIntake = true;
                            vintake.InCompleteErrors.HasErrorsInAllegationsSpecify = true;
                        }
                    }
                }

                if (vintake.IsSN == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsSN = true;
                }

                else if (vintake.IsSN == "y")
                {
                    if (vintake.IsImmediateMedicalNeed == false && vintake.IsAloneUnableCareSelf == false && vintake.IsJudgmentGrosslyImpaired == false &&
                        vintake.IsLackFoodClothingShelter == false && vintake.IsSignificantStructuralHazard == false && vintake.IsOtherReportedPrblms == false)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsInSNReportedProblems = true;
                    }

                    if (vintake.IsSNUnderAgeWithDisability == null)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsSNAge = true;
                    }
                    if (vintake.IsSNDomesticSetting == false)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsSNDomesticSetting = true;
                    }

                    if (vintake.IsImmediateMedicalNeed == true && vintake.ImmediateMedicalNeed == null)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsSNAllegationSpecify = true;
                    }
                    if (vintake.IsAloneUnableCareSelf == true && vintake.AloneUnableCareSelf == null)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsSNAllegationSpecify = true;
                    }
                    if (vintake.IsJudgmentGrosslyImpaired == true && vintake.JudgmentGrosslyImpaired == null)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsSNAllegationSpecify = true;
                    }
                    if (vintake.IsLackFoodClothingShelter == true && vintake.LackFoodClothingShelter == null)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsSNAllegationSpecify = true;
                    }
                    if (vintake.IsSignificantStructuralHazard == true && vintake.SignificantStructuralHazard == null)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsSNAllegationSpecify = true;
                    }
                    if (vintake.IsOtherReportedPrblms == true && vintake.OtherReportedPrblms == null)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsSNAllegationSpecify = true;
                    }
                }

                if (vintake.PriorityId == 0)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsPriority = true;
                }

                if (vintake.IsClientAwareOfReport == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsIsClientAwareOfReport = true;
                }


                if (vintake.IsClientInImmediateDanger == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsIsClientInImmediateDanger = true;
                }


                if (vintake.ClientCondition == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsClientCondition = true;
                }

                if (vintake.IsClientInNeedOfImmediateAssistance == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsIsClientInNeedOfImmediateAssistance = true;
                }


                if (vintake.IsClientInNeedOfSpecialAccom == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsIsClientInNeedOfSpecialAccom = true;
                }
                else if (vintake.IsClientInNeedOfSpecialAccom == "y")
                {
                    if (vintake.Hearing == false && vintake.Speech == false && vintake.Vision == false &&
                        vintake.Motor == false && vintake.Communication == false && vintake.IsOtherSpecialAccom == false)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsClientInNeedOfSpecialAccomRelated = true;
                    }
                }

                if (vintake.IsInDangerousSituation == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsIsInDangerousSituation = true;
                }

                else if (vintake.IsInDangerousSituation == "y")
                {
                    if (vintake.Neighborhood == false && vintake.Animals == false && vintake.MentalIllness == false &&
                        vintake.DrugsOrAlcohol == false && vintake.Weapons == false && vintake.EnvironmnetOrCodeViolations == false &&
                        vintake.Infestation == false && vintake.IsOtherDangerousSituation == false)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsInDangerousSituationRelated = true;
                    }
                }

                #endregion

                #region Part D

                if (vintake.viewreporterinfo.FirstName == null && vintake.viewreporterinfo.LastName == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsReporterName = true;
                }

                if (vintake.viewreporterinfo.WillProvideFurInfo == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsReporterWillProvideFurInfo = true;
                }
                else if (vintake.viewreporterinfo.WillProvideFurInfo == "y")
                {
                    if (vintake.viewreporterinfo.Phone == null)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsReporterPhoneWillProvideFurInfo = true;
                    }
                }

                if (vintake.viewreporterinfo.ReporterTypeId == 0)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsReporterReporterTypeId = true;
                }


                #endregion

                #region Part E

                if (vintake.IsNoAbusers != true)
                {
                    foreach (var abuser in vintake.listofabusers)
                    {
                        if (abuser.RelationId == 0)
                        {
                            vintake.InCompleteErrors.ErrorsInIntake = true;
                            vintake.InCompleteErrors.HasErrorsAbuserRelationId = true;
                        }

                        if (abuser.IsAbuserAwareOfReport == null)
                        {
                            vintake.InCompleteErrors.ErrorsInIntake = true;
                            vintake.InCompleteErrors.HasErrorsAbuserIsAbuserAwareOfReport = true;
                        }

                        if (abuser.Age == null)
                        {
                            vintake.InCompleteErrors.ErrorsInIntake = true;
                            vintake.InCompleteErrors.HasErrorsAbuserAge = true;
                        }

                        if (abuser.ContactTime == null)
                        {
                            vintake.InCompleteErrors.ErrorsInIntake = true;
                            vintake.InCompleteErrors.HasErrorsAbuserContactTime = true;
                        }

                        if (abuser.MentalCondition == null)
                        {
                            vintake.InCompleteErrors.ErrorsInIntake = true;
                            vintake.InCompleteErrors.HasErrorsAbuserMentalCondition = true;
                        }

                    }
                }

                #endregion

                #region Part F

                if (vintake.IsNoOthers != true)
                {
                    foreach (var other in vintake.listofothers)
                    {
                        if (other.RelationId == 0)
                        {
                            vintake.InCompleteErrors.ErrorsInIntake = true;
                            vintake.InCompleteErrors.HasErrorsOthersRelationId = true;
                        }

                        if (other.IsAwareOfReport == null)
                        {
                            vintake.InCompleteErrors.ErrorsInIntake = true;
                            vintake.InCompleteErrors.HasErrorsOthersIsAwareOfReport = true;
                        }

                        if (other.Phone == null)
                        {
                            vintake.InCompleteErrors.ErrorsInIntake = true;
                            vintake.InCompleteErrors.HasErrorsOthersPhone = true;
                        }

                        if (other.ContactTime == null)
                        {
                            vintake.InCompleteErrors.ErrorsInIntake = true;
                            vintake.InCompleteErrors.HasErrorsOthersContactTime = true;
                        }

                        if (other.FirstName == null && other.LastName == null)
                        {
                            vintake.InCompleteErrors.ErrorsInIntake = true;
                            vintake.InCompleteErrors.HasErrorsOthersName = true;
                        }
                    }
                }

                #endregion


            }

            return vintake.InCompleteErrors.ErrorsInIntake;
        }

        protected viewIntake setIncompleteIntakeErrors(viewIntake vintake)
        {
            if (vintake.Id > 0)
            {
                #region Part A

                if (vintake.ContractId == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsContractId = true;
                }
                //if (vintake.AgencyTypeId == null)
                //{
                //    vintake.InCompleteErrors.ErrorsInIntake = true;
                //    vintake.InCompleteErrors.HasErrorsAgencyTypeId = true;
                //}

                //if (vintake.DateIntake == Convert.ToDateTime("1/1/0001"))
                if (vintake.DateIntake == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsDateIntake = true;
                }

                if (vintake.TimeIntake == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTimeIntake = true;
                }

                #endregion

                #region Part B

                if (vintake.viewClient.FirstName == null && vintake.viewClient.LastName == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTempClientName = true;
                }

                if (vintake.viewClient.Age == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTempClientAge = true;
                }

                if (vintake.viewClient.GenderTypeId == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTempClientGender = true;
                }

                if (vintake.viewClient.IsLimitedEnglishSpeaking == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTempClientLimitedEnglishSpeaking = true;
                }
                else if (vintake.viewClient.IsLimitedEnglishSpeaking == "y" && vintake.viewClient.PrimaryLanguageId == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTempClientPrimaryLanguage = true;
                }

                if (vintake.viewClient.AddressLine1 == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTempClientAddressline1 = true;
                }

                if (vintake.viewClient.City == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTempClientCity = true;
                }

                if (vintake.viewClient.CountyTypeId == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTempClientCounty = true;
                }

                if (vintake.viewClient.Zip5 == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTempClientZip = true;
                }

                //if (vintake.viewClient.PhoneNumber == null)
                //{
                //    vintake.InCompleteErrors.ErrorsInIntake = true;
                //    vintake.InCompleteErrors.HasErrorsTempClientPhone = true;
                //}

                if (vintake.viewClient.Directions == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTempClientDirections = true;
                }

                if (vintake.viewClient.BestCommunicationMethod == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsTempClientbestCommunication = true;
                }


                #endregion

                #region Part C

                int allegationcount = 0;

                if (vintake.IsANE == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsANE = true;
                }

                else if (vintake.IsANE == "y")
                {
                    foreach (var allegation in vintake.Allegations)
                    {
                        if (allegation.IsChecked == true)
                        {
                            allegationcount++;
                        }
                    }

                    if (allegationcount == 0)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsInANEAllegations = true;
                    }

                    if (vintake.IsANEUnderAgeWithDisability == null)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsANEAge = true;
                    }
                    if (vintake.IsAAExists == false)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsANEAAExists = true;
                    }
                    if (vintake.IsANEDomesticSetting == false)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsANEDomesticSetting = true;
                    }
                    if (vintake.IsAllegationsConstituteAbuse == false)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsAllegationsConstituteAbuse = true;
                    }
                    foreach (var allegation in vintake.Allegations)
                    {
                        if (allegation.IsChecked && allegation.Specify == null)
                        {
                            vintake.InCompleteErrors.ErrorsInIntake = true;
                            vintake.InCompleteErrors.HasErrorsInAllegationsSpecify = true;
                        }
                    }
                }

                if (vintake.IsSN == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsSN = true;
                }

                else if (vintake.IsSN == "y")
                {
                    if (vintake.IsImmediateMedicalNeed == false && vintake.IsAloneUnableCareSelf == false && vintake.IsJudgmentGrosslyImpaired == false &&
                        vintake.IsLackFoodClothingShelter == false && vintake.IsSignificantStructuralHazard == false && vintake.IsOtherReportedPrblms == false)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsInSNReportedProblems = true;
                    }

                    if (vintake.IsSNUnderAgeWithDisability == null)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsSNAge = true;
                    }
                    if (vintake.IsSNDomesticSetting == false)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsSNDomesticSetting = true;
                    }
                    if (vintake.IsImmediateMedicalNeed == true && vintake.ImmediateMedicalNeed == null)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsSNAllegationSpecify = true;
                    }
                    if (vintake.IsAloneUnableCareSelf == true && vintake.AloneUnableCareSelf == null)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsSNAllegationSpecify = true;
                    }
                    if (vintake.IsJudgmentGrosslyImpaired == true && vintake.JudgmentGrosslyImpaired == null)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsSNAllegationSpecify = true;
                    }
                    if (vintake.IsLackFoodClothingShelter == true && vintake.LackFoodClothingShelter == null)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsSNAllegationSpecify = true;
                    }
                    if (vintake.IsSignificantStructuralHazard == true && vintake.SignificantStructuralHazard == null)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsSNAllegationSpecify = true;
                    }
                    if (vintake.IsOtherReportedPrblms == true && vintake.OtherReportedPrblms == null)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsSNAllegationSpecify = true;
                    }
                }

                if (vintake.PriorityId == 0)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsPriority = true;
                }

                if (vintake.IsClientAwareOfReport == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsIsClientAwareOfReport = true;
                }


                if (vintake.IsClientInImmediateDanger == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsIsClientInImmediateDanger = true;
                }


                if (vintake.ClientCondition == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsClientCondition = true;
                }

                if (vintake.IsClientInNeedOfImmediateAssistance == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsIsClientInNeedOfImmediateAssistance = true;
                }


                if (vintake.IsClientInNeedOfSpecialAccom == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsIsClientInNeedOfSpecialAccom = true;
                }
                else if (vintake.IsClientInNeedOfSpecialAccom == "y")
                {
                    if (vintake.Hearing == false && vintake.Speech == false && vintake.Vision == false &&
                        vintake.Motor == false && vintake.Communication == false && vintake.IsOtherSpecialAccom == false)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsClientInNeedOfSpecialAccomRelated = true;
                    }
                }

                if (vintake.IsInDangerousSituation == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsIsInDangerousSituation = true;
                }

                else if (vintake.IsInDangerousSituation == "y")
                {
                    if (vintake.Neighborhood == false && vintake.Animals == false && vintake.MentalIllness == false &&
                        vintake.DrugsOrAlcohol == false && vintake.Weapons == false && vintake.EnvironmnetOrCodeViolations == false &&
                        vintake.Infestation == false && vintake.IsOtherDangerousSituation == false)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsIsInDangerousSituationRelated = true;
                    }
                }

                #endregion

                #region Part D

                if (vintake.viewreporterinfo.FirstName == null && vintake.viewreporterinfo.LastName == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsReporterName = true;
                }

                if (vintake.viewreporterinfo.WillProvideFurInfo == null)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsReporterWillProvideFurInfo = true;
                }
                else if (vintake.viewreporterinfo.WillProvideFurInfo == "y")
                {
                    if (vintake.viewreporterinfo.Phone == null)
                    {
                        vintake.InCompleteErrors.ErrorsInIntake = true;
                        vintake.InCompleteErrors.HasErrorsReporterPhoneWillProvideFurInfo = true;
                    }
                }

                if (vintake.viewreporterinfo.ReporterTypeId == 0)
                {
                    vintake.InCompleteErrors.ErrorsInIntake = true;
                    vintake.InCompleteErrors.HasErrorsReporterReporterTypeId = true;
                }


                #endregion

                #region Part E

                if (vintake.IsNoAbusers != true)
                {
                    foreach (var abuser in vintake.listofabusers)
                    {
                        if (abuser.RelationId == 0)
                        {
                            vintake.InCompleteErrors.ErrorsInIntake = true;
                            vintake.InCompleteErrors.HasErrorsAbuserRelationId = true;
                        }

                        if (abuser.IsAbuserAwareOfReport == null)
                        {
                            vintake.InCompleteErrors.ErrorsInIntake = true;
                            vintake.InCompleteErrors.HasErrorsAbuserIsAbuserAwareOfReport = true;
                        }

                        if (abuser.Age == null)
                        {
                            vintake.InCompleteErrors.ErrorsInIntake = true;
                            vintake.InCompleteErrors.HasErrorsAbuserAge = true;
                        }

                        if (abuser.ContactTime == null)
                        {
                            vintake.InCompleteErrors.ErrorsInIntake = true;
                            vintake.InCompleteErrors.HasErrorsAbuserContactTime = true;
                        }

                        if (abuser.MentalCondition == null)
                        {
                            vintake.InCompleteErrors.ErrorsInIntake = true;
                            vintake.InCompleteErrors.HasErrorsAbuserMentalCondition = true;
                        }

                    }
                }

                #endregion

                #region Part F

                if (vintake.IsNoOthers != true)
                {
                    foreach (var other in vintake.listofothers)
                    {
                        if (other.RelationId == 0)
                        {
                            vintake.InCompleteErrors.ErrorsInIntake = true;
                            vintake.InCompleteErrors.HasErrorsOthersRelationId = true;
                        }

                        if (other.IsAwareOfReport == null)
                        {
                            vintake.InCompleteErrors.ErrorsInIntake = true;
                            vintake.InCompleteErrors.HasErrorsOthersIsAwareOfReport = true;
                        }

                        if (other.Phone == null)
                        {
                            vintake.InCompleteErrors.ErrorsInIntake = true;
                            vintake.InCompleteErrors.HasErrorsOthersPhone = true;
                        }

                        if (other.ContactTime == null)
                        {
                            vintake.InCompleteErrors.ErrorsInIntake = true;
                            vintake.InCompleteErrors.HasErrorsOthersContactTime = true;
                        }

                        if (other.FirstName == null && other.LastName == null)
                        {
                            vintake.InCompleteErrors.ErrorsInIntake = true;
                            vintake.InCompleteErrors.HasErrorsOthersName = true;
                        }
                    }
                }

                #endregion

                //#region Part G


                //if (vintake.ReferralDate == null)
                //{
                //    vintake.InCompleteErrors.ErrorsInIntake = true;
                //    vintake.InCompleteErrors.HasErrorsReferralDate = true;
                //}


                //#endregion
            }

            return vintake;
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


        #endregion

    }
}
