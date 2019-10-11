using AGE.CMS.Data.Models.ClientAssessment;
using AGE.CMS.Data.Models.ClientPreparation;
using AGE.CMS.Data.Models.ENums;
using AGE.CMS.Data.Models.Intake;
using AGE.CMS.Data.Models.Referral;
using AGE.CMS.Web.PersonProfileWCFService;
using DOAFramework.Core.WebMVC.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;


namespace AGE.CMS.Web.Areas.CMS.Controllers
{
    [Layout("_Layout")]
    public class ExternalController : CMSController
    {
        // GET: External
        public ActionResult Index()
        {
            return View();
        }

        #region NoticeOfInvestigations
        public ActionResult NoticeOfInvestigations()
        {
            var viewData = CMSService.GetNoticeOfInvestigationModelData();

            if (User.IsInRole("CMS_DDD"))
            {
                viewData.ListOfNoticeofInvestigations = viewData.ListOfNoticeofInvestigations.Where(i => i.IsDDD == true).ToList();
            }
            else if (User.IsInRole("CMS_DRS"))
            {
                viewData.ListOfNoticeofInvestigations = viewData.ListOfNoticeofInvestigations.Where(i => i.IsDRS == true).ToList();
            }

            return View(viewData);
        }

        public ActionResult ViewNoticeOfInvestigation(int AssessmentPrepId)
        {
            AssessmentPreparationModel asp = CMSService.GetAssessmentForm(AssessmentPrepId);

            asp.viewIntake = new viewIntake();
            asp.viewIntake = CMSService.GetIntake(Convert.ToInt32(asp.IntakeId));
            asp.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)asp.viewIntake.CaseheaderId);
            //if (userrole = IsDDD || userrole == IsDRS)
            //{
            //    asp.StatusDescription = CaseStatus.Verified.ToString();
            //   
            //}
            asp.StatusDescription = CaseStatus.Opened.ToString();

            CMSService.UpdateNoticeofInvestigationStatus((int)asp.NoticeOfInvestigationId, asp.StatusDescription, username);

            return View(asp);

        }

        #endregion

        #region ClientAssessments
        //[OutputCache(Duration = 120 , Location = OutputCacheLocation.Client)]  
        //public ActionResult ClientAssessments()
        //{
        //    viewClientAssesssment ClientAssessment = new viewClientAssesssment();

        //    ClientAssessment.ListClientAssesssment = new List<viewClientAssesssment>();

        //    if (User.IsInRole("CMS_DDD"))
        //    {
        //        ClientAssessment.ListClientAssesssment = CMSService.ListofClientAssessmentsForDDD().Where(i => (i.StatusDescription == CaseStatus.Approved.ToString() || i.viewReportSubstantiation.StatusDescription == CaseStatus.Submitted.ToString() || i.viewReportSubstantiation.StatusDescription == CaseStatus.Open.ToString() || i.viewReportSubstantiation.StatusDescription == CaseStatus.Opened.ToString() || i.StatusDescription == CaseStatus.Opened.ToString())).ToList();
        //    }
        //    else if (User.IsInRole("CMS_DRS"))
        //    {
        //        ClientAssessment.ListClientAssesssment = CMSService.ListofClientAssessmentsForDRS().Where(i => (i.StatusDescription == CaseStatus.Approved.ToString() || i.viewReportSubstantiation.StatusDescription == CaseStatus.Submitted.ToString() || i.viewReportSubstantiation.StatusDescription == CaseStatus.Open.ToString() || i.viewReportSubstantiation.StatusDescription == CaseStatus.Opened.ToString() || i.StatusDescription == CaseStatus.Opened.ToString())).ToList();
        //    }
        //    else if (User.IsInRole("CMS_CCP"))
        //    {
        //        ClientAssessment.ListClientAssesssment = CMSService.ListofClientAssessmentsForCCP().Where(i => (i.StatusDescription == CaseStatus.Approved.ToString() || i.viewReportSubstantiation.StatusDescription == CaseStatus.Submitted.ToString() || i.viewReportSubstantiation.StatusDescription == CaseStatus.Open.ToString() || i.viewReportSubstantiation.StatusDescription == CaseStatus.Opened.ToString() || i.StatusDescription == CaseStatus.Opened.ToString())).ToList();
        //    }
        //    else if (User.IsInRole("CMS_DSCC"))
        //    {
        //        ClientAssessment.ListClientAssesssment = CMSService.ListofClientAssessmentsForDSCC().Where(i => (i.StatusDescription == CaseStatus.Approved.ToString() || i.viewReportSubstantiation.StatusDescription == CaseStatus.Submitted.ToString() || i.viewReportSubstantiation.StatusDescription == CaseStatus.Open.ToString() || i.viewReportSubstantiation.StatusDescription == CaseStatus.Opened.ToString() || i.StatusDescription == CaseStatus.Opened.ToString())).ToList();
        //    }
        //    else if (User.IsInRole("CMS_MCO"))
        //    {
        //        ClientAssessment.ListClientAssesssment = CMSService.ListofClientAssessmentsForMCO().Where(i => contractids.Contains(i.viewReportSubstantiation.MCOId)).ToList();
        //        ClientAssessment.ListClientAssesssment = ClientAssessment.ListClientAssesssment.Where(i =>  (i.StatusDescription == CaseStatus.Approved.ToString() || i.viewReportSubstantiation.StatusDescription == CaseStatus.Submitted.ToString() || i.viewReportSubstantiation.StatusDescription == CaseStatus.Open.ToString() || i.viewReportSubstantiation.StatusDescription == CaseStatus.Opened.ToString() || i.StatusDescription == CaseStatus.Opened.ToString())).ToList();
              
        //    }
        //    else
        //    {
        //        ClientAssessment.ListClientAssesssment = CMSService.ListAllClientAssessments().Where(i => i.StatusDescription == CaseStatus.Approved.ToString() || i.viewReportSubstantiation.StatusDescription == CaseStatus.Submitted.ToString() || i.viewReportSubstantiation.StatusDescription == CaseStatus.Open.ToString() || i.viewReportSubstantiation.StatusDescription == CaseStatus.Opened.ToString() || i.StatusDescription == CaseStatus.Opened.ToString()).ToList();
        //    }
        //    return View(ClientAssessment);
        //}


        public ActionResult ClientAssessments()
        {


            string t;

            if (User.IsInRole("CMS_MCO"))
            {
                t = "MCO";
            }
            else if (User.IsInRole("CMS_DDD"))
            {
                t = "DDD";
            }

            else if (User.IsInRole("CMS_DRS"))
            {
                t = "DRS";
            }

            else if (User.IsInRole("CMS_CCP"))
            {
                t = "CCP";
            }

            else if (User.IsInRole("CMS_DSCC"))
            {
                t = "DSCC";
            }

            else
            {
                t = "ALL";
            }


            return View(CMSService.GetViewAssessmentListModelData(t, contractids));
        }

        public ActionResult ViewClientAssessement(int Id, int IntakeId, int ReportOfSubstantiationId)
        {
            ClientAssessmentModel viewClientAssessment;


            viewClientAssessment = CMSService.GetCA(Id);

            //viewClientAssessment.mode = "summary";

            viewClientAssessment.caselookup = getcaselookup(username, ViewBag.UserContractId);

            viewClientAssessment.viewIntake = new viewIntake();

            viewClientAssessment.viewIntake = CMSService.GetIntake(IntakeId);
            viewClientAssessment.viewIntake.viewCaseHeader = CMSService.GetCaseHeaderById((int)viewClientAssessment.viewIntake.CaseheaderId);
            viewClientAssessment.CaseheaderId = viewClientAssessment.viewIntake.viewCaseHeader.Id;


            CMSService.UpdateReportofSubstantiationStatusNew(ReportOfSubstantiationId, CaseStatus.Opened.ToString(), username);

            return View(viewClientAssessment);
        }

        #endregion

        #region Referrals

        //public ActionResult Referrals()
        //{
        //    viewDHSReferral referral = new viewDHSReferral();

        //    if (User.IsInRole("CMS_DDD"))
        //    {
        //        referral.ListofReferrals = CMSService.ListAllDHSReferrals().Where(i => i.IsDDD == true ).ToList();
        //    }
        //    else if (User.IsInRole("CMS_DRS"))
        //    {
        //        referral.ListofReferrals = CMSService.ListAllDHSReferrals().Where(i => i.IsDRS == true ).ToList();
        //    }
        //    else if (User.IsInRole("CMS_DMH"))
        //    {
        //        referral.ListofReferrals = CMSService.ListAllDHSReferrals().Where(i => i.IsDMH == true ).ToList();
        //    }
        //    else
        //    {
        //        referral.ListofReferrals = CMSService.ListAllDHSReferrals().ToList();
        //    }
        //    return View(referral);
        //}


        public ActionResult Referrals()
        {
            string t = "";

            if (User.IsInRole("CMS_DDD"))
            {
                t = "DDD";
            }
            else if (User.IsInRole("CMS_DRS"))
            {
                t = "DRS";
            }
            else if (User.IsInRole("CMS_DMH"))
            {
                t = "DMH";
            }
            else
            {
                t = "ALL";
            }
            return View(CMSService.GetViewReferralListModelData(t));


        }

        public ActionResult ViewReferrals(int Id)
        {
            viewDHSReferral viewDHSReferral;

            if (Id != 0)
            {
                viewDHSReferral = CMSService.GetDHSReferrals(Id);
              
                viewDHSReferral.caselookup = getcaselookup(username, ViewBag.UserContractId);
                viewDHSReferral.viewCaseHeader = new AGE.CMS.Data.Models.Intake.viewCaseHeader();
                viewDHSReferral.viewCaseHeader = CMSService.GetCaseHeaderById((int)viewDHSReferral.CaseheaderId);
                viewDHSReferral.CaseheaderId = viewDHSReferral.viewCaseHeader.Id;
            }

            else
            {

                viewDHSReferral = new viewDHSReferral();
                viewDHSReferral.DateCreated = DateTime.Now;
            }
            var contractid = 0;
            if (viewDHSReferral != null)
            {
                 contractid = viewDHSReferral.ContractId;

            }
          
           
            if (viewDHSReferral.caselookup.listofcontracts != null && viewDHSReferral.caselookup.listofcontracts.Any() && viewDHSReferral.caselookup.listofcontracts.Count == 1)
            {
                viewDHSReferral.ContractId = viewDHSReferral.caselookup.listofcontracts.FirstOrDefault().Id;
                viewDHSReferral.ContractName = (from contract in viewDHSReferral.caselookup.listofcontracts where contract.Id == viewDHSReferral.ContractId select contract.ContractName).FirstOrDefault();

            }
           
            viewDHSReferral.caselookup = getcaselookup(username, ViewBag.UserContractId);

            viewDHSReferral.StatusDescription = CaseStatus.Opened.ToString();

            CMSService.UpdateDHSReferralStatus(Id, viewDHSReferral.StatusDescription, username);
            viewDHSReferral.ContractAgencyName = CMSService.GetContract(contractid).DisplayContractName;
            return View(viewDHSReferral);
        }

        public ActionResult SaveReferrals(viewDHSReferral viewDHSReferral)
        {
            viewDHSReferral.StatusDescription = CaseStatus.Responded.ToString();
            int ad = CMSService.SaveDHSReferral(viewDHSReferral);

            //string ToEmail = null;

            //string Message = "You have received a DHS Referral from " + CMSService.GetContract(contractids.FirstOrDefault()).ContractName + ".";

            //string Subject = "DHS Referral";

            //if (viewDHSReferral.IsDDD == true)
            //{
            //    ToEmail = System.Configuration.ConfigurationManager.AppSettings["_DDDEmail"];
            //    SendEmail(ToEmail, Message, Subject);
            //}
            //if (viewDHSReferral.IsDRS == true)
            //{
            //    ToEmail = System.Configuration.ConfigurationManager.AppSettings["_DRSEmail"];
            //    SendEmail(ToEmail, Message, Subject);
            //}
            //if (viewDHSReferral.IsDMH == true)
            //{
            //    ToEmail = System.Configuration.ConfigurationManager.AppSettings["_DMHEmail"];
            //    SendEmail(ToEmail, Message, Subject);
            //}

            return Redirect(Url.Action("Referrals", "External"));

        }

        public ActionResult EditReferrals(int Id, int CaseheaderId)
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
            viewDHSReferral.viewCaseHeader = new AGE.CMS.Data.Models.Intake.viewCaseHeader();
            viewDHSReferral.viewCaseHeader = CMSService.GetCaseHeaderById(CaseheaderId);
            viewDHSReferral.CaseheaderId = viewDHSReferral.viewCaseHeader.Id;

            if (viewDHSReferral.caselookup.listofcontracts != null && viewDHSReferral.caselookup.listofcontracts.Any() && viewDHSReferral.caselookup.listofcontracts.Count == 1)
            {
                viewDHSReferral.ContractId = viewDHSReferral.caselookup.listofcontracts.FirstOrDefault().Id;
                viewDHSReferral.ContractName = (from contract in viewDHSReferral.caselookup.listofcontracts where contract.Id == viewDHSReferral.ContractId select contract.ContractName).FirstOrDefault();

            }

            return View(viewDHSReferral);
        }

        #endregion

        #region Helpers

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
                caselookup.listofInteragencies = codeTable.ListOfInterAgencies;
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

                //var workers = CMSService.ListOfWorkers((int)ContractId).ToList();
                //foreach (var worker in workers)
                //{
                //    var roles = GetRolesForUser(worker.UserName);
                //}
                using (var PersonService = new PersonProfileWCFServiceClient())
                {
                    var person = PersonService.GetPerson(Guid.Empty);
                    caselookup.listofgender = person.Demographic.DemographicList.ListOfGenderTypes;
                    caselookup.listofgender = new List<PersonProfile.Data.Entities.dtoGenderType>(PersonService.ListOfGenderTypes().ToList());
                    caselookup.listoflivingstatus = person.Demographic.DemographicList.ListOfLivingStatusTypes;
                    caselookup.listofraces = person.Demographic.DemographicList.ListOfRaceTypes;
                    //var listofveteranstatus = PersonService.ListOfVeteransStatusTypes();
                    //caselookup.listofveteranstatus = new List<dto>(PersonService.ListOfVeteransStatusTypes());            
                    //caselookup.listofcounties = CMSService.ListOfCounties(14).ToList();
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