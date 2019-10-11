using AGE.CMS.Data.Models.ENums;
using AGE.CMS.Data.Models.Intake;
using AGE.CMS.Data.Models.ReportDto;
using Microsoft.Reporting.WebForms;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;
//using iTextSharp.text;
//using iTextSharp.text.pdf;
//using iTextSharp.text.html.simpleparser;

using System.Data;
using System.Configuration;
using System.Text;
using DOAFramework.Core.WebMVC.Filters;


namespace AGE.CMS.Web.Areas.CMS.Controllers
{
    [Layout("_Layout")]
    public class ReportController : CMSController
    {

        #region Properties
        public List<viewAgeRange> ListAgeRange
        {
            get
            {
                return CMSReportsService.ListOfAgeRanges().ToList();
            }
        }
        //public List<viewContract> ListContract
        //{
        //    get
        //    {

        //        return CacheRuntimeExtensions.GetOrStore<List<viewContract>>("ListContract", () => CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList());
        //    }
        //}
        //public List<viewContract> ListUserContract
        //{
        //    get
        //    {
        //        return CMSService.ListOfLookupContractsByUserName((string)ViewBag.UserName).ToList();
        //    }
        //}
        public List<string> ListAreaCode
        {
            get
            {

                return (from psa in CMSService.ListOfPSA() orderby psa.AreaCode select psa.AreaCode).ToList();
            }
        }
        #endregion

        public ActionResult Index()
        {
            //var reportViewer = new ReportViewer()
            //{
            //    ProcessingMode = ProcessingMode.Remote,
            //    SizeToReportContent = true,
            //    Width = Unit.Percentage(100),
            //    Height = Unit.Percentage(100),
            //};
            //reportViewer.LocalReport.ReportPath = Request.MapPath(Request.ApplicationPath) + @"Reports\Report1.rdlc";
            //reportViewer.LocalReport.DataSources.Add(new ReportDataSource("AbuserAge"));

            //ViewBag.ReportViewer = reportViewer;

            return View();
        }

        #region Abuser Age

        public ActionResult AbuserAge(dtoStatisticalReportParent vm)
        {
            ViewBag.Title = " Abuser Age ";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.AbuserAge;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();
                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }

            List<viewAbuserInformation> datalist = new List<viewAbuserInformation>();
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
           //if (User.IsInRole("CMS_IDOAStaff"))
           // {
           //     vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
           // }
           // else if (User.IsInRole("CMS_RAAAdmin"))
           // {
           //     var PSAId = CMSService.GetPSAByUserName(username).Id;
           //     vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
           // }
           // else if (User.IsInRole("CMS_Supervisor"))
           // {
           //     vm.contracts = CMSService.ListOfContracts(username).ToList();
           // }
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }
            datalist = CMSReportsService.ListOfAbuserAgeByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            vm.dtoStatisticalReportData = (from a in datalist
                                           where a.Age != null
                                           group a by new
                                           {
                                               Description = (from r in ListAgeRange
                                                              where a.Age != null &&
                                                                    (bool)r.IsVictim &&
                                                                  r.MinAge.CompareTo(int.Parse(a.Age)) < 1 &&
                                                                  r.MaxAge.CompareTo(int.Parse(a.Age)) > -1
                                                              select r).First().Description,
                                               Sequence = (from r in ListAgeRange
                                                           where a.Age != null &&
                                                                    (bool)r.IsVictim &&
                                                               r.MinAge.CompareTo(int.Parse(a.Age)) < 1 &&
                                                               r.MaxAge.CompareTo(int.Parse(a.Age)) > -1
                                                           select r).First().Id
                                           }
                                               into gcai
                                               select new dtoStatisticalReport() { Description = gcai.Key.Description, Count = gcai.Count(), Sequence = gcai.Key.Sequence }).OrderBy(x => x.Sequence).ToList();

            int totalcount = vm.dtoStatisticalReportData.Sum(s => s.Count);

            foreach (dtoStatisticalReport sr in vm.dtoStatisticalReportData)
            {
                sr.Percentage = (decimal)((sr.Count * 100.0) / totalcount);
            }


            //if (vm.dtoStatisticalReportData.Any())
            //    ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            //else
            //    ViewBag.YMax = 0;

            ViewBag.Action = "AbuserAge";


            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Abuser Age Regional

        public virtual ActionResult AbuserAgeRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Types of Abuser Age Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.AbuserAgeRegional;

            vRRCS.AreaCodes = ListAreaCode;

            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }
            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0) { list.Add(vRRCS.ContractId); }
            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfAbuserAgeRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            vRRCS.ListOfdtoRegionalReportCountbyType = (from s in vRRCS.ListOfdtoRegionalReportCountbyType
                                                        group s by (from r in ListAgeRange
                                                                    where r.IsVictim &&
                                                                        r.MinAge.CompareTo(int.Parse(s.Description)) < 1 &&
                                                                        r.MaxAge.CompareTo(int.Parse(s.Description)) > -1
                                                                    select r).First().Description
                                                            into grp
                                                            select new dtoRegionalReportCountbyType()
                                                            {
                                                                Description = grp.Key,
                                                                ListOfdtoRegionalReportCountbyRegion =
                                                                                (from r in grp
                                                                                 from s in r.ListOfdtoRegionalReportCountbyRegion
                                                                                 group s by s.AreaCode
                                                                                     into grp2
                                                                                     select new dtoRegionalReportCountbyRegion()
                                                                                     {
                                                                                         AreaCode = grp2.Key,
                                                                                         Count = (from p in grp
                                                                                                  from x in p.ListOfdtoRegionalReportCountbyRegion
                                                                                                  where x.AreaCode == grp2.Key
                                                                                                  select (x.Count == 0 ? 0 : x.Count)).Sum()
                                                                                     }).ToList(),
                                                                Total = grp.Select(g => g.Total).Sum()
                                                            }).ToList();

            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
            }


            ViewBag.Action = "AbuserAgeRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Abuser Association

        public virtual ActionResult AbuserAssociation(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = " Abuser Association ";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.AbuserCaregiver;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();
                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }

            List<viewAbuserInformation> datalist = new List<viewAbuserInformation>();
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfAbuserAssociationByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "AbuserAssociation";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Abuse Association Regional

        public virtual ActionResult AbuserAssociationRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Abuse Association Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.AbuserCaregiverRegional;

            vRRCS.AreaCodes = ListAreaCode;

            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();
                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }
            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0) { list.Add(vRRCS.ContractId); }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfAbuserAssociationRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
            }


            ViewBag.Action = "AbuserAssociationRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Abuser Barrier

        public virtual ActionResult AbuserBarrier(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = " Abuser Barrier ";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.AbuserBarrier;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();
                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }

            List<viewAbuserInformation> datalist = new List<viewAbuserInformation>();
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }


            //if (ViewBag.RoleGroupName != APS.Data.Entities.ENums.APSRoles.RAA)
            //{
            //    bool RightsToAllContracts = IsRightToAllProviders();

            //    if (RightsToAllContracts)
            //        vm.contracts = ListContract;
            //    else
            //        vm.contracts = ListUserContract;

            //    vm.counties = ListCountyType;

            //    vm.dtoStatisticalReportData = APSReportService.ListOfAbuserBarrierByUserName((string)ViewBag.UserName, vm.DateStart, vm.DateEnd, vm.ContractId, vm.CountyId, RightsToAllContracts).ToList();
            //}
            //else
            //{
            //    List<viewContract> listofcontracts = new List<viewContract>();
            //    if (vm.ContractId > 0)
            //        listofcontracts = APSCaseService.ListOfContractsFromIds(new List<int>() { vm.ContractId }.ToArray()).ToList();
            //    else
            //        listofcontracts = APSCaseService.ListOfPSAContracts((from psa in (List<viewPSA>)ViewData["USERAGENCIES"] select psa.Id).ToArray()).ToList();
            //    List<int> CountyIds = APSCaseService.ListOfContractCountyIds(listofcontracts.Select(s => s.Id).ToArray()).ToList();

            //    vm.counties = ListCountyType.Where(s => CountyIds.Contains(s.Id)).ToList();
            //    vm.contracts = APSCaseService.ListOfLookupContractsFromPSAs((from psa in (List<viewPSA>)ViewData["USERAGENCIES"] select psa.Id).ToArray()).ToList();
            //    vm.dtoStatisticalReportData = APSReportService.ListOfAbuserBarrierByContractIds(listofcontracts.Select(s => s.Id).ToArray(), vm.DateStart, vm.DateEnd, vm.CountyId).ToList();
            //}

            vm.dtoStatisticalReportData = CMSReportsService.ListOfAbuserBarrierByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();


            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "AbuserBarrier";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Abuser Barrier Regional

        public virtual ActionResult AbuserBarrierRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Abuse Barrier Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.AbuserBarrierRegional;

            vRRCS.AreaCodes = ListAreaCode;

            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0) { list.Add(vRRCS.ContractId); }
            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfAbuserBarrierRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
            }


            ViewBag.Action = "AbuserBarrierRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Abuser Relation

        public virtual ActionResult AbuserRelation(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = " Abuser Relation ";

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = AGE.CMS.Data.Models.ENums.ReportTypes.StatisticalReport;
            vm.Report = AGE.CMS.Data.Models.ENums.Reports.AbuserRelation;
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();
            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfAbuserRelationByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();
            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "AbuserRelation";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Abuser Relation Regional

        public virtual ActionResult AbuserRelationRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Abuse Relation Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.AbuserRelationRegional;

            vRRCS.AreaCodes = ListAreaCode;

            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0) { list.Add(vRRCS.ContractId); }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfAbuserRelationRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
            }


            ViewBag.Action = "AbuserRelationRegional";
            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Abuser Cohabitation With Client

        public virtual ActionResult AbuserCohabitationWithClient(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Abuser Cohabitation With Client";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.AbuserCohabitationWithClient;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfAbuserCohabitationWithClientByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "AbuserCohabitationWithClient";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Abuser Cohabitation With Client Regional

        public virtual ActionResult AbuserCohabitationWithClientRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Abuser Cohabitation With Client Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.AbuserCohabitationWithClientRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfAbuserCohabitationWithClientRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "AbuserCohabitationWithClientRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Abuser Interviewed

        public virtual ActionResult AbuserInterviewed(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Abuser Interviewed";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.AbuserInterviewed;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfAbuserInterviewedByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "AbuserInterviewed";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Abuser Interviewed Regional

        public virtual ActionResult AbuserInterviewedRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Abuser Interviewed Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.AbuserInterviewedRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }
            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();

            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfAbuserInterviewedRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "AbuserInterviewedRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Abuser Schooling Level

        public virtual ActionResult AbuserSchoolingLevel(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Abuser Schooling Level";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.AbuserSchoolingLevel;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfAbuserSchoolingLevelByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "AbuserSchoolingLevel";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Abuser Schooling Level Regional

        public virtual ActionResult AbuserSchoolingLevelRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Abuser Schooling Level Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.AbuserSchoolingLevelRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfAbuserSchoolingLevelRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "AbuserSchoolingLevelRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Client Filed Police Report Against AA

        public virtual ActionResult ClientFiledPoliceReportAgainstAA(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Client Filed Police Report Against AA";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.ClientFiledPoliceReportAgainstAA;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfClientFiledPoliceReportAgainstAAByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "ClientFiledPoliceReportAgainstAA";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Client Filed Police Report Against AA Regional

        public virtual ActionResult ClientFiledPoliceReportAgainstAARegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Client Filed Police Report Against AA Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.ClientFiledPoliceReportAgainstAARegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }
            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();

            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfClientFiledPoliceReportAgainstAARegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "ClientFiledPoliceReportAgainstAARegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Abuser Services At Start

        public virtual ActionResult AbuserServicesAtStart(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Abuser Services At Start";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.AbuserServicesAtStart;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfAbuserServicesAtStartByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "AbuserServicesAtStart";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Abuser Services At Start Regional

        public virtual ActionResult AbuserServicesAtStartRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Abuser Services At Start Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.AbuserServicesAtStartRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfAbuserServicesAtStartRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "AbuserServicesAtStartRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Abuser Services At Closure

        public virtual ActionResult AbuserServicesAtClosure(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Abuser Services At Closure";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.AbuserServicesAtClosure;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfAbuserServicesAtClosureByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "AbuserServicesAtClosure";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Abuser Services At Closure Regional

        public virtual ActionResult AbuserServicesAtClosureRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Abuser Services At Closure Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.AbuserServicesAtClosureRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfAbuserServicesAtClosureRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "AbuserServicesAtClosureRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Abuser Association At Closure

        public virtual ActionResult AbuserAssociationAtClosure(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Abuser Association At Closure";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.AbuserAssociationAtClosure;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfAbuserAssociationAtClosureByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "AbuserAssociationAtClosure";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Abuser Association At Closure Regional

        public virtual ActionResult AbuserAssociationAtClosureRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Abuser Association At Closure Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.AbuserAssociationAtClosureRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfAbuserAssociationAtClosureRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "AbuserAssociationAtClosureRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Abuser Subsititute Decision Maker At Closure

        public virtual ActionResult AbuserSubsitituteDecisionMakerAtClosure(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Abuser Subsititute Decision Maker At Closure";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.AbuserSubsitituteDecisionMakerAtClosure;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfAbuserSubsitituteDecisionMakerAtClosureByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "AbuserSubsitituteDecisionMakerAtClosure";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Abuser Subsititute Decision Maker At Closure Regional

        public virtual ActionResult AbuserSubsitituteDecisionMakerAtClosureRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Abuser Subsititute Decision Maker At Closure Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.AbuserSubsitituteDecisionMakerAtClosureRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfAbuserSubsitituteDecisionMakerAtClosureRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "AbuserSubsitituteDecisionMakerAtClosureRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Letter to Probate Judges

        public virtual ActionResult LettertoProbateJudges(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Letter to Probate Judges";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.LettertoProbateJudges;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfLettertoProbateJudgesByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "LettertoProbateJudges";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Letter to Probate Judges Regional

        public virtual ActionResult LettertoProbateJudgesRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Letter to Probate Judges Regional";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.LettertoProbateJudgesRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfLettertoProbateJudgesRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "LettertoProbateJudgesRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Abuser Legal Remedy At Closure

        public virtual ActionResult AbuserLegalRemedyAtClosure(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Abuser Legal Remedy At Closure";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.AbuserLegalRemedyAtClosure;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfAbuserLegalRemedyAtClosureByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "AbuserLegalRemedyAtClosure";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Abuser Legal Remedy At Closure Regional

        public virtual ActionResult AbuserLegalRemedyAtClosureRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Abuser Legal Remedy At Closure Regional";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.AbuserLegalRemedyAtClosureRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }
            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();

            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfAbuserLegalRemedyAtClosureRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "AbuserLegalRemedyAtClosureRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Substantiated Abuser In Aging Network

        public virtual ActionResult SubstantiatedAbuserInAgingNetwork(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Substantiated Abuser In Aging Network";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.SubstantiatedAbuserInAgingNetwork;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfSubstantiatedAbuserInAgingNetworkByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "SubstantiatedAbuserInAgingNetwork";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Substantiated Abuser In Aging Network Regional

        public virtual ActionResult SubstantiatedAbuserInAgingNetworkRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Substantiated Abuser In Aging Network Regional";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.SubstantiatedAbuserInAgingNetworkRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfSubstantiatedAbuserInAgingNetworkRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "SubstantiatedAbuserInAgingNetworkRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Substantiated Abuser In DHS Network

        public virtual ActionResult SubstantiatedAbuserInDHSNetwork(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Substantiated Abuser In DHS Network";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.SubstantiatedAbuserInDHSNetwork;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfSubstantiatedAbuserInDHSNetworkByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "SubstantiatedAbuserInAgingNetwork";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Substantiated Abuser In DHS Network Regional

        public virtual ActionResult SubstantiatedAbuserInDHSNetworkRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Substantiated Abuser In DHS Network Regional";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.SubstantiatedAbuserInDHSNetworkRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfSubstantiatedAbuserInDHSNetworkRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "SubstantiatedAbuserInDHSNetworkRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Number of Abusers Referred to IDoA For Registry Review

        public virtual ActionResult NumberOfAbusersReferredToIDoAForRegistryReview(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Number of Abusers Referred to IDoA For Registry Review";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.NumberOfAbusersReferredToIDoAForRegistryReview;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfNumberOfAbusersReferredToIDoAForRegistryReviewByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "NumberOfAbusersReferredToIDoAForRegistryReview";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Number of Abusers Referred to IDoA For Registry Review Regional

        public virtual ActionResult NumberOfAbusersReferredToIDoAForRegistryReviewRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Number of Abusers Referred to IDoA For Registry Review Regional";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.NumberOfAbusersReferredToIDoAForRegistryReviewRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfNumberOfAbusersReferredToIDoAForRegistryReviewRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "NumberOfAbusersReferredToIDoAForRegistryReviewRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Victim Age

        public virtual ActionResult VictimAge(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = " Victim Age ";
         

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = AGE.CMS.Data.Models.ENums.ReportTypes.StatisticalReport;
            vm.Report = AGE.CMS.Data.Models.ENums.Reports.VictimsAge;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }

            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();
          

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfVictimAgeByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();
            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "VictimAge";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }
        }

        #endregion

        #region Victim Age Regional

        public virtual ActionResult VictimAgeRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Victims Age Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType =ReportTypes.RegionalReport;
            vRRCS.Report = Reports.VictimsAgeRegional;

            vRRCS.AreaCodes = ListAreaCode;

            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }
            //if (ViewBag.RoleGroupName != APS.Data.Entities.ENums.APSRoles.RAA)
            //{

            //    bool RightsToAllContracts = IsRightToAllProviders();

            //    if (RightsToAllContracts)
            //        vRRCS.contracts = ListContract;
            //    else
            //        vRRCS.contracts = ListUserContract;

            //    vRRCS.counties = ListCountyType;

            //    vRRCS.ListOfdtoRegionalReportCountbyType = APSReportService.ListOfVictimAgeRegionalByUserName((string)ViewBag.UserName, vRRCS.DateStart, vRRCS.DateEnd, vRRCS.ContractId, vRRCS.CountyId, RightsToAllContracts).ToList();
            //}
            //else
            //{
            //    List<viewContract> listofcontracts = new List<viewContract>();
            //    if (vRRCS.ContractId > 0)
            //        listofcontracts = APSCaseService.ListOfContractsFromIds(new List<int>() { vRRCS.ContractId }.ToArray()).ToList();
            //    else
            //        listofcontracts = APSCaseService.ListOfPSAContracts((from psa in (List<viewPSA>)ViewData["USERAGENCIES"] select psa.Id).ToArray()).ToList();
            //    List<int> CountyIds = APSCaseService.ListOfContractCountyIds(listofcontracts.Select(s => s.Id).ToArray()).ToList();

            //    vRRCS.counties = ListCountyType.Where(s => CountyIds.Contains(s.Id)).ToList();
            //    vRRCS.contracts = APSCaseService.ListOfLookupContractsFromPSAs((from psa in (List<viewPSA>)ViewData["USERAGENCIES"] select psa.Id).ToArray()).ToList();

            //    vRRCS.ListOfdtoRegionalReportCountbyType = APSReportService.ListOfVictimAgeRegionalByContractIds(listofcontracts.Select(s => s.Id).ToArray(), vRRCS.DateStart, vRRCS.DateEnd, vRRCS.CountyId).ToList();
            //}

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfVictimAgeRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();

            vRRCS.ListOfdtoRegionalReportCountbyType = (from s in vRRCS.ListOfdtoRegionalReportCountbyType
                                                        where s.Description != null
                                                        group s by s.Description
                                                            into grp
                                                            select new dtoRegionalReportCountbyType()
                                                            {
                                                                Description = grp.Key,
                                                                ListOfdtoRegionalReportCountbyRegion =
                                                                                (from r in grp
                                                                                 from s in r.ListOfdtoRegionalReportCountbyRegion
                                                                                 group s by s.AreaCode
                                                                                     into grp2
                                                                                     select new dtoRegionalReportCountbyRegion()
                                                                                     {
                                                                                         AreaCode = grp2.Key,
                                                                                         Count = (from p in grp
                                                                                                  from x in p.ListOfdtoRegionalReportCountbyRegion
                                                                                                  where x.AreaCode == grp2.Key
                                                                                                  select x.Count).Sum()
                                                                                     }).ToList(),
                                                                Total = grp.Select(g => g.Total).Sum()
                                                            }).ToList();
            //vRRCS.ListOfdtoRegionalReportCountbyType = (from s in vRRCS.ListOfdtoRegionalReportCountbyType
            //                                            where s.Description != null
            //                                            group s by (from r in ListAgeRange
            //                                                        where s.Description != null &&
            //                                                            r.IsVictim &&
            //                                                            r.MinAge.CompareTo(int.Parse(s.Description)) < 1 &&
            //                                                            r.MaxAge.CompareTo(int.Parse(s.Description)) > -1
            //                                                        select r).First().Description
            //                                                into grp
            //                                                select new dtoRegionalReportCountbyType()
            //                                                {
            //                                                    Description = grp.Key,
            //                                                    ListOfdtoRegionalReportCountbyRegion =
            //                                                                    (from r in grp
            //                                                                     from s in r.ListOfdtoRegionalReportCountbyRegion
            //                                                                     group s by s.AreaCode
            //                                                                         into grp2
            //                                                                         select new dtoRegionalReportCountbyRegion()
            //                                                                         {
            //                                                                             AreaCode = grp2.Key,
            //                                                                             Count = (from p in grp
            //                                                                                      from x in p.ListOfdtoRegionalReportCountbyRegion
            //                                                                                      where x.AreaCode == grp2.Key
            //                                                                                      select x.Count).Sum()
            //                                                                         }).ToList(),
            //                                                    Total = grp.Select(g => g.Total).Sum()
            //                                                }).ToList();

            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "VictimAgeRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Victim Barrier

        public virtual ActionResult VictimBarrier(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = " Victim Barrier ";
         
            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = AGE.CMS.Data.Models.ENums.ReportTypes.StatisticalReport;
            vm.Report = AGE.CMS.Data.Models.ENums.Reports.VictimBarrier;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }

            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();
          

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfVictimBarrierByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();
            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "VictimBarrier";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }
        }

        #endregion

        #region Victim Barrier Regional

        public virtual ActionResult VictimBarrierRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Victim Barrier Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.VictimBarrierRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }
            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();

            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            //if (ViewBag.RoleGroupName != APS.Data.Entities.ENums.APSRoles.RAA)
            //{
            //    bool RightsToAllContracts = IsRightToAllProviders();

            //    if (RightsToAllContracts)
            //        vRRCS.contracts = ListContract;
            //    else
            //        vRRCS.contracts = ListUserContract;

            //    vRRCS.counties = ListCountyType;

            //    vRRCS.ListOfdtoRegionalReportCountbyType = APSReportService.ListOfVictimBarrierRegionalByUserName((string)ViewBag.UserName, vRRCS.DateStart, vRRCS.DateEnd, vRRCS.ContractId, vRRCS.CountyId, RightsToAllContracts).ToList();

            //}
            //else
            //{
            //    List<viewContract> listofcontracts = new List<viewContract>();
            //    if (vRRCS.ContractId > 0)
            //        listofcontracts = APSCaseService.ListOfContractsFromIds(new List<int>() { vRRCS.ContractId }.ToArray()).ToList();
            //    else
            //        listofcontracts = APSCaseService.ListOfPSAContracts((from psa in (List<viewPSA>)ViewData["USERAGENCIES"] select psa.Id).ToArray()).ToList();
            //    List<int> CountyIds = APSCaseService.ListOfContractCountyIds(listofcontracts.Select(s => s.Id).ToArray()).ToList();

            //    vRRCS.counties = ListCountyType.Where(s => CountyIds.Contains(s.Id)).ToList();
            //    vRRCS.contracts = APSCaseService.ListOfLookupContractsFromPSAs((from psa in (List<viewPSA>)ViewData["USERAGENCIES"] select psa.Id).ToArray()).ToList();

            //    vRRCS.ListOfdtoRegionalReportCountbyType = APSReportService.ListOfVictimBarrierRegionalByContractIds(listofcontracts.Select(s => s.Id).ToArray(), vRRCS.DateStart, vRRCS.DateEnd, vRRCS.CountyId).ToList();
            //}



            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfVictimBarrierRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();

            //vRRCS.ListOfdtoRegionalReportCountbyType = (from s in vRRCS.ListOfdtoRegionalReportCountbyType
            //                                            where s.Description != null
            //                                            group s by s.Description
            //                                                into grp
            //                                                select new dtoRegionalReportCountbyType()
            //                                                {
            //                                                    Description = grp.Key,
            //                                                    ListOfdtoRegionalReportCountbyRegion =
            //                                                                    (from r in grp
            //                                                                     from s in r.ListOfdtoRegionalReportCountbyRegion
            //                                                                     group s by s.AreaCode
            //                                                                         into grp2
            //                                                                         select new dtoRegionalReportCountbyRegion()
            //                                                                         {
            //                                                                             AreaCode = grp2.Key,
            //                                                                             Count = (from p in grp
            //                                                                                      from x in p.ListOfdtoRegionalReportCountbyRegion
            //                                                                                      where x.AreaCode == grp2.Key
            //                                                                                      select x.Count).Sum()
            //                                                                         }).ToList(),
            //                                                    Total = grp.Select(g => g.Total).Sum()
            //                                                }).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "VictimBarrierRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Victim LivingArrangement

        public virtual ActionResult VictimLivingArrangement(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = " Victim LivingArrangement ";
          

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = AGE.CMS.Data.Models.ENums.ReportTypes.StatisticalReport;
            vm.Report = AGE.CMS.Data.Models.ENums.Reports.VictimsLivingArrangements;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }

            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfVictimLivingArrangementByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();
            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "VictimLivingArrangement";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }
        }

        #endregion

        #region Victim Living Arrangements Regional

        public virtual ActionResult VictimLivingArrangementsRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = " Victim Living Arrangements Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.VictimsLivingArrangementsRegional;

            vRRCS.AreaCodes = ListAreaCode;

            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }
           
            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfVictimLivingArrangementsRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();

            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "VictimLivingArrangementsRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Victim Living Status

        public virtual ActionResult VictimLivingStatus(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = " Victim Living Status ";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.VictimsLivingStatus;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }

            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfVictimLivingStatusByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "VictimLivingStatus";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Victim Living Status Regional

        public virtual ActionResult VictimLivingStatusRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {

            ViewBag.Title = " Victim Living Status Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.VictimsLivingStatusRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }
            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();

            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfVictimsLivingStatusRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();

            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "VictimLivingStatusRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Victim Marital Status

        public virtual ActionResult VictimMaritalStatus(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = " Victim Marital Status ";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.VictimsMaritalStatus;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }

            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfVictimMaritalStatusByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "VictimMaritalStatus";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Victim Marital Status Regional

        public virtual ActionResult VictimMaritalStatusRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = " Victim Marital Status Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.VictimsMaritalStatusRegional;

            vRRCS.AreaCodes = ListAreaCode;

            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfVictimMaritalStatusRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "VictimMaritalStatusRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Victim Race

        public virtual ActionResult VictimRace(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = " Victim Race ";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.VictimsRace;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfVictimRaceByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "VictimRace";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Victim Race Regional

        public virtual ActionResult VictimRaceRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {

            ViewBag.Title = " Victim Race Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.VictimsRaceRegional;

            vRRCS.AreaCodes = ListAreaCode;

            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfVictimRaceRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "VictimRaceRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Victim Sex

        public virtual ActionResult VictimSex(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Victim Sex";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.VictimsSex;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfVictimGenderByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "VictimSex";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Victim Sex Regional

        public virtual ActionResult VictimSexRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Victim Sex Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.VictimsSexRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfVictimGenderRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();

            
            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "VictimSexRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Victim Interviewed about Allegations

        public virtual ActionResult VictimInterviewedANE(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = " Victim Interviewed about ANE Allegations ";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = AGE.CMS.Data.Models.ENums.ReportTypes.StatisticalReport;
            vm.Report = AGE.CMS.Data.Models.ENums.Reports.VictimInterviewed;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfVictimInterviewedAllegationsByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId, true).ToList();
            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "VictimInterviewedANE";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }
        }

        public virtual ActionResult VictimInterviewedSN(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = " Victim Interviewed about SN Allegations ";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = AGE.CMS.Data.Models.ENums.ReportTypes.StatisticalReport;
            vm.Report = AGE.CMS.Data.Models.ENums.Reports.VictimInterviewed;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfVictimInterviewedAllegationsByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId, false).ToList();
            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "VictimInterviewedSN";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }
        }

        #endregion

        #region Victim Interviewed about Allegations Regional

        public virtual ActionResult VictimInterviewedRegionalANE(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = " Victim Interviewed about ANE Allegations Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.VictimInterviewedRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
                vRRCS.IsANE = false;
            }
            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();

            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfVictimInterviewedAllegationsRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId, true).ToList();            

            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "VictimInterviewedRegionalANE";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        public virtual ActionResult VictimInterviewedRegionalSN(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = " Victim Interviewed about SN Allegations Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.VictimInterviewedRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
                vRRCS.IsANE = false;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfVictimInterviewedAllegationsRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId, false).ToList();

            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "VictimInterviewedRegionalSN";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Victim Primary Language

        public virtual ActionResult VictimPrimaryLanguage(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Victim Primary Language";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.VictimPrimaryLanguage;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfVictimPrimaryLanguageByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "VictimPrimaryLanguage";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Victim Primary Language Regional

        public virtual ActionResult VictimPrimaryLanguageRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Victim Primary Language Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.VictimPrimaryLanguageRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }
            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();

            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfVictimPrimaryLanguageRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "VictimPrimaryLanguageRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Victim Police Report Made

        public virtual ActionResult VictimPoliceReportMade(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Victim Police Report Made";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.VictimPoliceReportMade;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfVictimPoliceReportMadeByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "VictimPoliceReportMade";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Victim Police Report Made Regional

        public virtual ActionResult VictimPoliceReportMadeRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Victim Police Report Made Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.VictimPoliceReportMadeRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfVictimPoliceReportMadeRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "VictimPoliceReportMadeRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Working In Clients Interest

        public virtual ActionResult WorkingInClientsInterest(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Working In Clients Interest";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.WorkingInClientsInterest;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfWorkingInClientsInterestByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "WorkingInClientsInterest";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Working In Clients Interest Regional

        public virtual ActionResult WorkingInClientsInterestRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Working In Clients Interest Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.WorkingInClientsInterestRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfWorkingInClientsInterestRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "WorkingInClientsInterestRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Victim Services At Start

        public virtual ActionResult VictimServicesAtStart(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Victim Services At Start";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.VictimServicesAtStart;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfVictimServicesAtStartByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "VictimServicesAtStart";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Victim Services At Start Regional

        public virtual ActionResult VictimServicesAtStartRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Victim Services At Start Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.VictimServicesAtStartRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }
            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();

            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfVictimServicesAtStartRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "VictimServicesAtStartRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Victim Services Referred By APS

        public virtual ActionResult VictimServicesReferredByAPS(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Victim Services Referred By APS";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.VictimServicesReferredByAPS;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfVictimServicesReferredByAPSByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "VictimServicesReferredByAPS";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Victim Services Referred By APS Regional

        public virtual ActionResult VictimServicesReferredByAPSRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Victim Services Referred By APS Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.VictimServicesReferredByAPSRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfVictimServicesReferredByAPSRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "VictimServicesReferredByAPSRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Victim Services At Closure

        public virtual ActionResult VictimServicesAtClosure(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Victim Services At Closure";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.VictimServicesAtClosure;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfVictimServicesAtClosureByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "VictimServicesAtClosure";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Victim Services At Closure Regional

        public virtual ActionResult VictimServicesAtClosureRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Victim Services At Closure Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.VictimServicesAtClosureRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfVictimServicesAtClosureRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "VictimServicesAtClosureRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Victim Living Setting At Closure

        public virtual ActionResult VictimLivingSettingAtClosure(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Victim Living Setting At Closure";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.VictimLivingSettingAtClosure;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfVictimLivingSettingAtClosureByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "VictimLivingSettingAtClosure";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Victim Living Setting At Closure Regional

        public virtual ActionResult VictimLivingSettingAtClosureRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Victim Living Setting At Closure Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.VictimLivingSettingAtClosureRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfVictimLivingSettingAtClosureRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "VictimLivingSettingAtClosureRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Victim Cohabitation At Closure

        public virtual ActionResult VictimCohabitationAtClosure(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Victim Cohabitation At Closure";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.VictimCohabitationAtClosure;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfVictimCohabitationAtClosureByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "VictimCohabitationAtClosure";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Victim Cohabitation At Closure Regional

        public virtual ActionResult VictimCohabitationAtClosureRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Victim Cohabitation At Closure Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.VictimCohabitationAtClosureRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfVictimCohabitationAtClosureRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "VictimCohabitationAtClosureRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Victim Referred To Agencies

        public virtual ActionResult VictimReferredToAgencies(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Victim Referred To Agencies";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.VictimReferredToAgencies;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfVictimReferredToAgenciesByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "VictimReferredToAgencies";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Victim Referred To Agencies Regional

        public virtual ActionResult VictimReferredToAgenciesRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Victim Referred To Agencies Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.VictimReferredToAgenciesRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfVictimReferredToAgenciesRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "VictimReferredToAgenciesRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Victim Medical History

        public virtual ActionResult VictimMedicalHistory(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Victim Medical History";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.VictimMedicalHistory;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfVictimMedicalHistoryByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "VictimMedicalHistory";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Victim Medical History Regional

        public virtual ActionResult VictimMedicalHistoryRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Victim Medical History Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.VictimMedicalHistoryRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfVictimMedicalHistoryRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "VictimMedicalHistoryRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Victim Benefits

        public virtual ActionResult VictimBenefits(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Victim Benefits";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.VictimBenefits;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfVictimBenefitsByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "VictimBenefits";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Victim Benefits Regional

        public virtual ActionResult VictimBenefitsRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Victim Benefits Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.VictimBenefitsRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfVictimBenefitsRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "VictimBenefitsRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Victim Employment

        public virtual ActionResult VictimEmployment(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Victim Employment";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.VictimEmployment;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfVictimEmploymentByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "VictimEmployment";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Victim Employment Regional

        public virtual ActionResult VictimEmploymentRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Victim Employment Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.VictimEmploymentRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfVictimEmploymentRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "VictimEmploymentRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region Victim Income Level

        public virtual ActionResult VictimIncomeLevel(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "Victim Income Level";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.VictimIncomeLevel;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfVictimIncomeLevelByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "VictimIncomeLevel";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Victim Income Regional

        public virtual ActionResult VictimIncomeLevelRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Victim Income Level Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.VictimIncomeLevelRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfVictimIncomeLevelRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "VictimIncomeLevelRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region ReportSource

        public virtual ActionResult ReportSource(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = " Report Source ";


            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = AGE.CMS.Data.Models.ENums.ReportTypes.StatisticalReport;
            vm.Report = AGE.CMS.Data.Models.ENums.Reports.ReportSource;
           
            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }

            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfReportSourceByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();
            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "ReportSource";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }
        }

        #endregion

        #region Abuse Type

        public virtual ActionResult SustantiationByAbuseType(dtoMultipleStatisticalReportParent parent)
        {
           
            ViewBag.Title = "Abuse Type";

            if (parent == null) parent = new dtoMultipleStatisticalReportParent();
            parent.ReportType = ReportTypes.MultipleStatisticalReport;
            parent.Report = Reports.SustantiationByAbuseType;           

            if (parent.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                parent.DateStart = DateTime.Now.AddYears(-1);
                parent.DateEnd = DateTime.Now;
            }

            parent.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    parent.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    parent.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    parent.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            parent.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (parent.ContractId != 0)
            {
            list.Add(parent.ContractId);
            }
           
            parent.dtoMultipleStatisticalReportData = CMSReportsService.SubstantiationbyAbuseType(list.ToArray(), (DateTime)parent.DateStart, (DateTime)parent.DateEnd, parent.CountyId);

            if (parent.dtoMultipleStatisticalReportData.ReportData.Any())
                ViewBag.YMax = parent.dtoMultipleStatisticalReportData.ReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "SustantiationByAbuseType";

            if (parent.mode == "partial")
            {
                return Json(parent, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MultipleMatrixReport", parent);
            }

        }

        #endregion

        #region Abuse Type Regional

        public virtual ActionResult SustantiationByAbuseTypeRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
          

            ViewBag.Title = " Abuse Type Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = AGE.CMS.Data.Models.ENums.ReportTypes.RegionalReport;
            vRRCS.Report = AGE.CMS.Data.Models.ENums.Reports.SubstantiationByAbuseTypeRegional;

            vRRCS.AreaCodes = ListAreaCode;

            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }
            //if (ViewBag.RoleGroupName != AGE.CMS.Data.Models.ENums.Roles.RAAAdmin)
            //{
            //    bool RightsToAllContracts = IsRightToAllProviders();

            //    if (RightsToAllContracts)
            //        vRRCS.contracts = ListContract;
            //    else
            //        vRRCS.contracts = ListUserContract;

            //    vRRCS.counties = ListCountyType;

            //    vRRCS.ListOfdtoRegionalReportCountbyType = APSReportService.ListOfSubstantiatedByAbuseTypeRegionalByUserName((string)ViewBag.UserName, vRRCS.DateStart, vRRCS.DateEnd, vRRCS.ContractId, vRRCS.CountyId, RightsToAllContracts).ToList();
            //}
            //else
            //{
            //    List<viewContract> listofcontracts = new List<viewContract>();
            //    if (vRRCS.ContractId > 0)
            //        listofcontracts = APSCaseService.ListOfContractsFromIds(new List<int>() { vRRCS.ContractId }.ToArray()).ToList();
            //    else
            //        listofcontracts = APSCaseService.ListOfPSAContracts((from psa in (List<viewPSA>)ViewData["USERAGENCIES"] select psa.Id).ToArray()).ToList();
            //    List<int> CountyIds = APSCaseService.ListOfContractCountyIds(listofcontracts.Select(s => s.Id).ToArray()).ToList();

            //    vRRCS.counties = ListCountyType.Where(s => CountyIds.Contains(s.Id)).ToList();
            //    vRRCS.contracts = APSCaseService.ListOfLookupContractsFromPSAs((from psa in (List<viewPSA>)ViewData["USERAGENCIES"] select psa.Id).ToArray()).ToList();

            //    vRRCS.ListOfdtoRegionalReportCountbyType = APSReportService.ListOfSubstantiatedByAbuseTypeRegionalByContractIds(listofcontracts.Select(s => s.Id).ToArray(), vRRCS.DateStart, vRRCS.DateEnd, vRRCS.CountyId).ToList();
            //}


            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfANESNTypeRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "SustantiationByAbuseTypeRegional";


            if (vRRCS.mode == "partial")
            {
                //return PartialView("PartialMatrixReport", vm);
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
            //return View("RegionalReport", vRRCS);
        }

        #endregion

        #region AbuseSection

        public virtual ActionResult AbuseSection(dtoAbuseSectionReportParent vm = null)
        {
            ViewBag.Title = " Sex/Relationship of Abuser by Abuse Type ";

            if (vm == null) vm = new dtoAbuseSectionReportParent();
            vm.ReportType = ReportTypes.AbuseSectionReport;
            vm.Report = Reports.AbuseSection;

            vm.AbuseSections = (from a in codeTable.ListOfAbuseTypes
                                select new viewAbuseType { Id = a.Id, Description = a.Description }).ToList();

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }


            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();

            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();
            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoAbuseSectionReportData = CMSReportsService.ListOfAbuseSectionReport(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId, vm.AbuseSectionId).ToList();

            if (vm.dtoAbuseSectionReportData.Any())
                ViewBag.YMax = vm.dtoAbuseSectionReportData.Max(s => s.Total);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "AbuseSection";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("AbuseSectionReport", vm);
            }

        }


        #endregion

        #region Priority Type By VictimAge

        public virtual ActionResult PriorityTypeByVictimAge(dtoMultipleStatisticalReportParent parent)
        {

            ViewBag.Title = "Priority Type";

            if (parent == null) parent = new dtoMultipleStatisticalReportParent();
            parent.ReportType = ReportTypes.MultipleStatisticalReport;
            parent.Report = Reports.Priority;

            if (parent.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                parent.DateStart = DateTime.Now.AddYears(-1);
                parent.DateEnd = DateTime.Now;
            }

            parent.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    parent.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    parent.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    parent.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            parent.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (parent.ContractId != 0)
            {
                list.Add(parent.ContractId);
            }

            parent.dtoMultipleStatisticalReportData = CMSReportsService.PriorityReportByContractIds(list.ToArray(), (DateTime)parent.DateStart, (DateTime)parent.DateEnd, parent.CountyId);

            if (parent.dtoMultipleStatisticalReportData.ReportData.Any())
                ViewBag.YMax = parent.dtoMultipleStatisticalReportData.ReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "PriorityTypeByVictimAge";

            if (parent.mode == "partial")
            {
                return Json(parent, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MultipleMatrixReport", parent);
            }

        }

        #endregion

        #region Abuse Type Regional

        public virtual ActionResult PriorityTypeByVictimAgeRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {


            ViewBag.Title = " Priority Type Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.PriorityRegional;

            vRRCS.AreaCodes = ListAreaCode;

            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.PriorityReportRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
        }

            ViewBag.Action = "PriorityTypeByVictimAgeRegional";


            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region VictimSection

        public virtual ActionResult VictimSection(dtoAbuseSectionReportParent vm = null)
        {
            ViewBag.Title = " Sex of Victim by Abuse Type ";

            if (vm == null) vm = new dtoAbuseSectionReportParent();
            vm.ReportType = ReportTypes.AbuseSectionReport;
            vm.Report = Reports.AbuseSection;

            vm.AbuseSections = (from a in codeTable.ListOfAbuseTypes
                                select new viewAbuseType { Id = a.Id, Description = a.Description }).ToList();

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }

            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();


            // if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();
            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoAbuseSectionReportData = CMSReportsService.ListOfVictimSectionReportByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId, vm.AbuseSectionId).ToList();
           

            ViewBag.Action = "VictimSection";


            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("AbuseSectionReport", vm);
            }

        }


        #endregion

        #region Abuse Indicators

        public virtual ActionResult AbuseIndicators(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = " Leading Abuse Indicators ";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.AbuseIndicator;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }

            

            if (User.IsInRole("CMS_IDOAStaff"))
            {
                vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            }
            else if (User.IsInRole("CMS_RAAAdmin"))
            {
                var PSAId = CMSService.GetPSAByUserName(username).Id;
                vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            }
            else if (User.IsInRole("CMS_Supervisor"))
            {
                vm.contracts = CMSService.ListOfContracts(username).ToList();
            }

            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();
            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfAbuserIndicatorByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();


            ViewBag.Action = "AbuseIndicators";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReportCode", vm);
            }

        }

        #endregion

        #region Assessment Status

        public virtual ActionResult AssessmentStatus(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = " Assessment Status ";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.AssessmentStatus;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();

            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();
            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfAssessmentStatusByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();


            ViewBag.Action = "AssessmentStatus";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Risk Assessment Scores Regional

        public virtual ActionResult RiskAssessmentScoresRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {

            ViewBag.Title = "Risk Assessment Scores";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.RiskAssessmentScoresRegional;

           
            List<string> InitialNoRisks = new List<string>() { "00", "01" };

            vRRCS.AreaCodes = new List<string>() { "Low/No" };

            vRRCS.AreaCodes.AddRange((from psa in APSCaseService.ListOfRisks() where !InitialNoRisks.Contains(psa.Code) select psa.Description).ToList());


            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }
            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();
            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfRiskAssessmentScoresRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();




            vRRCS.ShowPercent = false;
            vRRCS.ShowTotal = false;
            vRRCS.IsRiskAssessment = true;

            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "RiskAssessmentScoresRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RiskAssessmentScoresRegionalReport", vRRCS);
            }
        }

        #endregion
    
        #region NumberofIntakesReceived

        public virtual ActionResult NumberofIntakesReceived(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = "APS Reports Received";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.NumberofIntakesReceived;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }

            vm.dtoStatisticalReportData = CMSReportsService.ListOfNumberofIntakesReceivedByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();

            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "NumberofIntakesReceived";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }


        #endregion

        #region NumberofIntakesReceived Regional

        public virtual ActionResult NumberofIntakesReceivedRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "APS Reports Received Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.NumberofIntakesReceivedRegional;

            vRRCS.AreaCodes = ListAreaCode;
            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }

            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}

            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();


            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0)
            {
                list.Add(vRRCS.ContractId);
            }

            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfNumberofIntakesReceivedRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            if (Agregatetotal > 0)
            {
                foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
                {
                    vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
                }
            }

            ViewBag.Action = "NumberofIntakesReceivedRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion

        #region AgencyHours

        public virtual ActionResult AgencyHours(dtoStatisticalReportParent vm = null)
        {
            ViewBag.Title = " Agency Hours ";

            if (vm == null) vm = new dtoStatisticalReportParent();
            vm.ReportType = ReportTypes.StatisticalReport;
            vm.Report = Reports.AgencyHours;

            if (vm.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();
                vm.DateStart = DateTime.Now.AddYears(-1);
                vm.DateEnd = DateTime.Now;
            }

            List<viewAbuserInformation> datalist = new List<viewAbuserInformation>();
            vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vm.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vm.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vm.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vm.ContractId != 0) { list.Add(vm.ContractId); }            

            vm.dtoStatisticalReportData = CMSReportsService.ListOfAgencyHoursByContractIds(list.ToArray(), (DateTime)vm.DateStart, (DateTime)vm.DateEnd, vm.CountyId).ToList();


            if (vm.dtoStatisticalReportData.Any())
                ViewBag.YMax = vm.dtoStatisticalReportData.Max(s => s.Count);
            else
                ViewBag.YMax = 0;

            ViewBag.Action = "AgencyHours";

            if (vm.mode == "partial")
            {
                return Json(vm, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("MatrixReport", vm);
            }

        }

        #endregion

        #region Agency Hours Regional

        public virtual ActionResult AgencyHoursRegional(dtoRegionalReportCountbySummary vRRCS = null)
        {
            ViewBag.Title = "Agency Hours Regional ";

            if (vRRCS == null) vRRCS = new dtoRegionalReportCountbySummary();
            vRRCS.ReportType = ReportTypes.RegionalReport;
            vRRCS.Report = Reports.AbuserBarrierRegional;

            vRRCS.AreaCodes = ListAreaCode;

            if (vRRCS.mode != "partial")
            {
                List<viewContract> listofcontracts = new List<viewContract>();

                vRRCS.DateStart = DateTime.Now.AddYears(-1);
                vRRCS.DateEnd = DateTime.Now;
            }
            vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //if (User.IsInRole("CMS_IDOAStaff"))
            //{
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.AgencyType.Code.Trim() == "03").ToList();
            //}
            //else if (User.IsInRole("CMS_RAAAdmin"))
            //{
            //    var PSAId = CMSService.GetPSAByUserName(username).Id;
            //    vRRCS.contracts = CMSService.ListOfAllContracts().Where(i => i.PSAId == PSAId).ToList();
            //}
            //else if (User.IsInRole("CMS_Supervisor"))
            //{
            //    vRRCS.contracts = CMSService.ListOfContracts(username).ToList();
            //}
            vRRCS.counties = new List<PersonProfile.Data.Entities.dtoCountyType>(PersonService.ListOfCountyTypes(14)).ToList();

            List<int> list = new List<int>();
            if (vRRCS.ContractId != 0) { list.Add(vRRCS.ContractId); }
            vRRCS.ListOfdtoRegionalReportCountbyType = CMSReportsService.ListOfAbuserBarrierRegionalByContractIds(list.ToArray(), (DateTime)vRRCS.DateStart, (DateTime)vRRCS.DateEnd, vRRCS.CountyId).ToList();


            var Agregatetotal = 0;

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                Agregatetotal = Agregatetotal + vrrc.Total;
            }

            foreach (var vrrc in vRRCS.ListOfdtoRegionalReportCountbyType)
            {
                vrrc.Percentage = Math.Round((decimal)(((decimal)vrrc.Total / (decimal)Agregatetotal) * 100), 2);
            }


            ViewBag.Action = "AgencyHoursRegional";

            if (vRRCS.mode == "partial")
            {
                return Json(vRRCS, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return View("RegionalReport", vRRCS);
            }
        }

        #endregion
    }
}
