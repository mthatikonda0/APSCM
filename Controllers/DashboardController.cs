using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoIT.Core.ExtensionMethods;
using DoIT.Core.Utils;
using DoIT.Core.Web.Mvc.Attributes;
using AGE.CMS.Resource.Constants;
using AGE.CMS.Core;
using AGE.CMS.Core.Web;
//using AGE.CMS.Web.CVRSvcRef;
//using AGE.CMS.Web.ECCPISSvcRef;
//using AGE.CMS.Web.HFSReceipientsSvcRef;
using DOAFramework.Core.WebMVC.Filters;

namespace AGE.CMS.Web.Areas.CMS.Controllers
{
    [Layout("_Layout")]
    public class DashboardController : CMSController
    {

        public virtual ActionResult IDOAStaff()
         {
            AGE.CMS.Business.CodeTable codeTable = (AGE.CMS.Business.CodeTable)Session["CODETABLE"];

            ViewBag.RoleDescription = codeTable.UserRoleDescription;
            ViewBag.RoleName = codeTable.UserRoleName;
            return View();
         }

         public virtual ActionResult Supervisor()
         {             
             return View();
         }

         public virtual ActionResult Administrator()
         {             
             return View();
         }

         public virtual ActionResult Caseworker()
         {
            AGE.CMS.Business.CodeTable codeTable = (AGE.CMS.Business.CodeTable)Session["CODETABLE"];

            ViewBag.RoleDescription = codeTable.UserRoleDescription;
            ViewBag.RoleName = codeTable.UserRoleName;

            return View();
         }

         public virtual ActionResult RAAAdmin()
         {
             //using (var eCCPISSrvcClient = new ECCPISSrvcClient())
             //{
             //  var clients = eCCPISSrvcClient.GetClientsbySSN("426668562");
             //  var client = (from c in clients orderby c.LastUpdDt descending select c).Take(1).FirstOrDefault();
               

             //  //eCCPIS Client Record Details
             //  //           ClientId:       client.ClientID
             //  //           Last Name:      client.LastName
             //  //           First Name:     client.FirstName
             //  //           Birth Date:     client.DoB
             //  //           Sex:            client.Sex - 1 - Male; 2 - Female


             //}

             //using (var eCCPISSrvcClient = new ECCPISSrvcClient())
             //{
             //    var assessments = eCCPISSrvcClient.GetAssessments(null, "001520789", null);
             //    var assessment = (from a in assessments orderby a.LastUpdDt descending select a).Take(1).FirstOrDefault();

             //    //eCCPIS Client Record Details
             //    //         Last CCU:                          assessment.ClientCCUID     
             //    //         EligibilityDetermination Date:     assessment.EligDeterDt
             //    //         Initial Service Date:              assessment.IniServDt
             //    //         Next Assessment Date:              assessment.NextAssessDt
             //}

             //using (var hFSReceipientsSrvcClient = new HFSReceipientsSrvcClient())
             //{
             //    var receipients = hFSReceipientsSrvcClient.GetRecipientsbySSNorRIN("426668562", null);
             //    var receipient = (from a in receipients orderby a.CreatedDate descending select a).Take(1).FirstOrDefault();
                 
                 
             //    //HFS Receipient Record Details
             //    //         RIN                                 receipient.ID_NUM
             //    //         Last Name                           receipient.LAST_NAME
             //    //         First Name                          receipient.FST_NAME
             //    //         Birth Date                          receipient.BRTH_DTE
             //    //         Sex                                 receipient.SEX_CD - M - Male; F - Female
             //    //         Dual Medicaid/Medicare IND          receipient.DUAL_MEDICAID_MEDICARE_IND
             //    //         Begin Date                          receipient.BEGDTE1 (??? Need to clarify from Jody)
             //    //         End Date                            receipient.ENDDTE1 (??? Need to clarify from Jody)
             //    //         Needed for MCO Name                 receipient.FAC_CODE1  (Need to clarify from Jody which one to pick, there are 5) 

                 

             //}          


             //using (var hFSReceipientsSrvcClient = new HFSReceipientsSrvcClient())
             //{
             //    var obrawaivers = hFSReceipientsSrvcClient.GetOBRAbySSNorRIN("426668562", null);
             //    var obrawaiver = (from a in obrawaivers orderby a.CHANGE_DATE descending select a).Take(1).FirstOrDefault();
                 

             //    //OBRA Waivers Details
             //    //         RIN                                 obrawaiver.RECIP_NUMBER
             //    //         Masterfile Name                     obrawaiver.MASTERFILE_NAME
             //    //         Birth Date                          obrawaiver.BIRTH_DATE
             //    //         Waiver                              obrawaiver.WAIVER
             //    //         Begin Date                          obrawaiver.BEGIN_DATE
             //    //         End Date                            obrawaiver.END_DATE

                                 
             //}

             //using (var eCCPISSrvcClient = new ECCPISSrvcClient())
             //{
             //    var MCOName = eCCPISSrvcClient.GetMCOInfoName("19");

             //    //MCO Information  - parameter 19 = receipient.FAC_CODE1.Substring(0,2)
             //    //         MCO Name                            MCOName
             //    //         Begin Date                          receipient.BEGDTE1 (??? Need to clarify from Jody)
             //    //         End Date                            receipient.ENDDTE1 (??? Need to clarify from Jody)
             //}

             //using (var cVRSrvClient = new CVRSrvClient())
             //{
             //    var deathrecords = cVRSrvClient.GetDeathRecordsBySSN("426668562");
             //    var deathrecord = (from a in deathrecords orderby a.CreatedTS descending select a).Take(1).FirstOrDefault();
             //}

             //using (var cVRSrvClient = new CVRSrvClient())
             //{
             //    var expections = cVRSrvClient.GetDeathExceptionBySSN("426668562");
             //    var expection = (from a in expections orderby a.CreatedDT descending select a).Take(1).FirstOrDefault();
             //}


             ////Death Records Choose the following only if there no exceptions with the given SSN:                 
             ////          Name                       deathrecord.LastName + ", " + deathrecord.FirstName
             ////          Birth Date                 deathrecord.BirthDate
             ////          Sex                        deathrecord.Sex - M - Male; F - Female
             ////          Date Deceased              deathrecord.DeathDate

             
             return View();
         }

         public virtual ActionResult ReportTakerOnly()
         {
             return View();
         }
        
         

    }
}
