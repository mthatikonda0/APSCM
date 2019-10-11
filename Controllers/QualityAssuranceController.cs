using AGE.CMS.Web.Areas.CMS.Controllers;
using AGE.CMS.Data.Models.QualityAssurance;
using AGE.CMS.Data.Models.SupportDocs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DOAFramework.Core.WebMVC.Filters;

namespace AGE.CMS.Web.Areas.CMS.Controllers
{
    [Layout("_Layout")]
    public class QualityAssuranceController : CMSController
    {

        public ActionResult Index()
        {
            viewQualityAssuranceModel model = new viewQualityAssuranceModel();

            model.viewQualityAssuranceTracker.ListOfQualityAssuranceTracker = CMSService.ListQualityAssuranceTracker().ToList();

            model.viewComplaintTracker.ListComplaintTracker = CMSService.ListComplaintTracker().ToList();

            return View(model);
        }

        public virtual ActionResult EditQualityAssurance(int Id)
        {
            viewQualityAssuranceTracker viewTracker;

            if (Id != 0)
            {
                viewTracker = CMSService.GetQualityAssuranceTracker(Id);

                viewTracker.mode = "edit";

                viewTracker.ListAgencies = CMSService.ListOfAllContracts().ToList();

                //added by Lindsey
                List<int> docIds = new List<int>();

                foreach(var doc in viewTracker.viewReview.ListOfDocuments){
                    docIds.Add(doc.Id);

                }

                viewTracker.viewReview.DocumentIds = docIds.ToArray();
                //end of additions
            }
            else
            {
                viewTracker = new viewQualityAssuranceTracker();
                var ContractId = 0;
                var listofcontracts = CMSService.ListOfContracts(username).ToList();
                if (listofcontracts != null && listofcontracts.Any() && listofcontracts.Count == 1)
                {
                    ContractId = listofcontracts.FirstOrDefault().Id;
                    //viewcaseheader.ContractName = (from contract in viewcaseheader.caselookup.listofcontracts where contract.Id == viewcaseheader.ContractId select contract.ContractName).FirstOrDefault();
                }
                viewTracker.viewReview.ListOfStaff = CMSService.ListOfWorkers(ContractId).ToList();
                viewTracker.viewReview.selectedStaff = new string[viewTracker.viewReview.ListOfStaff.Count()];
                if (viewTracker.viewReview.selectedStaff != null)
                {
                    foreach (var staff in viewTracker.viewReview.ListOfStaff)
                    {
                        for (int i = 0; i < viewTracker.viewReview.selectedStaff.Length; i++)
                        {
                            if (viewTracker.viewReview.selectedStaff[i] == staff.Id.ToString())
                            {
                                staff.IsSelected = true;
                            }
                        }
                    }
                }
            }

            if (User.IsInRole("CMS_IDOAStaff"))
            {
                viewTracker.ListPSA = CMSService.ListOfPSA().ToList();
                viewTracker.ListAgencies = CMSService.ListOfAllContracts().ToList();
            }
            else if (User.IsInRole("CMS_RAAAdmin"))
            {
                viewTracker.PSAId = CMSService.GetPSAByUserName(username).Id;
                viewTracker.ListPSA = CMSService.ListOfPSA().Where(i => i.Id ==viewTracker.PSAId).ToList();
                viewTracker.ListAgencies = CMSService.ListOfAllContracts().Where(i => i.PSAId == viewTracker.PSAId).ToList();
            }
            else if (User.IsInRole("CMS_Supervisor"))
            {
                viewTracker.ListAgencies = CMSService.ListOfContracts(username).ToList();
                viewTracker.ListPSA = CMSService.ListOfPSA().Where(i => viewTracker.ListAgencies.Any(a => a.PSAId == i.Id)).ToList();
            }
            
            viewTracker.ListReviewTypes = CMSService.ListOfReviewTypes().ToList();


            

            return View(viewTracker);
        }

        public virtual ActionResult ViewQualityAssurance(int Id)
        {
            viewQualityAssuranceTracker viewTracker;

            if (Id != 0)
            {
                viewTracker = CMSService.GetQualityAssuranceTracker(Id);

                viewTracker.mode = "edit";

                viewTracker.ListAgencies = CMSService.ListOfAllContracts().ToList();

                //added by Lindsey
                List<int> docIds = new List<int>();

                foreach (var doc in viewTracker.viewReview.ListOfDocuments)
                {
                    docIds.Add(doc.Id);

                }

                viewTracker.viewReview.DocumentIds = docIds.ToArray();
                //end of additions
            }
            else
            {
                viewTracker = new viewQualityAssuranceTracker();
                var ContractId = 0;
                var listofcontracts = CMSService.ListOfContracts(username).ToList();
                if (listofcontracts != null && listofcontracts.Any() && listofcontracts.Count == 1)
                {
                    ContractId = listofcontracts.FirstOrDefault().Id;
                    //viewcaseheader.ContractName = (from contract in viewcaseheader.caselookup.listofcontracts where contract.Id == viewcaseheader.ContractId select contract.ContractName).FirstOrDefault();
                }
                viewTracker.viewReview.ListOfStaff = CMSService.ListOfWorkers(ContractId).ToList();
                viewTracker.viewReview.selectedStaff = new string[viewTracker.viewReview.ListOfStaff.Count()];
                if (viewTracker.viewReview.selectedStaff != null)
                {
                    foreach (var staff in viewTracker.viewReview.ListOfStaff)
                    {
                        for (int i = 0; i < viewTracker.viewReview.selectedStaff.Length; i++)
                        {
                            if (viewTracker.viewReview.selectedStaff[i] == staff.Id.ToString())
                            {
                                staff.IsSelected = true;
                            }
                        }
                    }
                }
            }

            if (User.IsInRole("CMS_IDOAStaff"))
            {
                viewTracker.ListPSA = CMSService.ListOfPSA().ToList();
                viewTracker.ListAgencies = CMSService.ListOfAllContracts().ToList();
            }
            else if (User.IsInRole("CMS_RAAAdmin"))
            {
                viewTracker.PSAId = CMSService.GetPSAByUserName(username).Id;
                viewTracker.ListPSA = CMSService.ListOfPSA().Where(i => i.Id == viewTracker.PSAId).ToList();
                viewTracker.ListAgencies = CMSService.ListOfAllContracts().Where(i => i.PSAId == viewTracker.PSAId).ToList();
            }
            else if (User.IsInRole("CMS_Supervisor"))
            {
                viewTracker.ListAgencies = CMSService.ListOfContracts(username).ToList();
                viewTracker.ListPSA = CMSService.ListOfPSA().Where(i => viewTracker.ListAgencies.Any(a => a.PSAId == i.Id)).ToList();
            }

            viewTracker.ListReviewTypes = CMSService.ListOfReviewTypes().ToList();




            return View(viewTracker);
        }

        [HttpPost]
        public ActionResult SaveQualityAssurance(viewQualityAssuranceTracker viewTracker)
        {




            viewTracker.UserCreated = username;
            viewTracker.UserUpdated = username;



            int viewtrackerId = CMSService.SaveQualityAssuranceTracker(viewTracker);
            //int RAAActivityId = CMSService.SaveRaaActivity(viewraatracker);




            return RedirectToAction("ListQualityAssurance", "QualityAssurance");
        }

        public ActionResult ListQualityAssurance()
        {
            viewQualityAssuranceTracker viewtracker = new viewQualityAssuranceTracker();

            if (User.IsInRole("CMS_IDOAStaff"))
            {
                viewtracker.ListOfQualityAssuranceTracker = CMSService.ListQualityAssuranceTracker().ToList();
            }
            else if (User.IsInRole("CMS_RAAAdmin"))
            {
                int UserPSAId = 0;
                UserPSAId = APSCaseService.ListOfPSAsByUserName(username).FirstOrDefault().Id;

                viewtracker.ListOfQualityAssuranceTracker = CMSService.ListQualityAssuranceTracker().Where(i => i.PSAId == UserPSAId).ToList();
            }
            else if (User.IsInRole("CMS_Supervisor"))
            {
                int UserAgencyId = 0;
                //using (var APSCaseWCFServiceClient = new AGE.CMS.Web.APSCaseWCFService.APSCaseWCFServiceClient())
                //{
                //    UserAgencyId = APSCaseWCFServiceClient.ListOfAgenciesByUserName(username).FirstOrDefault().;
                //}
                UserAgencyId = CMSService.ListOfContracts(username).FirstOrDefault().Id;
                viewtracker.ListOfQualityAssuranceTracker = CMSService.ListQualityAssuranceTracker().Where(i => i.AgencyId == UserAgencyId).ToList();
            }

            

            return View(viewtracker);
        }

        public ActionResult EditComplaintTracker(int Id)
        {
            viewComplaintTracker complaint;

            if (Id != 0)
            {
                complaint = CMSService.GetComplaintTracker(Id);
                complaint.ListContracts = CMSService.ListOfContracts(username).ToList();

                //if (User.IsInRole("CMS_IDOAStaff"))
                //{
                //    complaint.AgencyTypeDescription = "IDoA Staff";
                //}
                //else if (User.IsInRole("CMS_RAAAdmin"))
                //{
                //    var psa = CMSService.GetPSAByUserName(username);
                //    complaint.AgencyTypeDescription = psa.AreaName;
                //    complaint.ContractId = psa.Id;
                //}
                //else if (User.IsInRole("CMS_Supervisor"))
                //{
                //    complaint.ListContracts = CMSService.ListOfContracts(username).ToList();

                //    if (complaint.ListContracts.Count == 1)
                //    {
                //        complaint.AgencyTypeDescription = complaint.ListContracts.FirstOrDefault().ContractName;
                //        complaint.ContractId = complaint.ListContracts.FirstOrDefault().Id;
                //    }
                //}
            }
            else
            {
                complaint = new viewComplaintTracker();

                if (User.IsInRole("CMS_IDOAStaff"))
                {
                    complaint.AgencyTypeDescription = "IDoA Staff";
                }
                else if (User.IsInRole("CMS_RAAAdmin"))
                {
                    var psa =  CMSService.GetPSAByUserName(username);
                    complaint.AgencyTypeDescription = psa.AreaName;
                    complaint.ContractId = psa.Id;
                }
                else if (User.IsInRole("CMS_Supervisor"))
                {
                    complaint.ListContracts = CMSService.ListOfContracts(username).ToList();

                    if (complaint.ListContracts.Count == 1)
                    {
                        complaint.AgencyTypeDescription = complaint.ListContracts.FirstOrDefault().ContractName;
                        complaint.ContractId = complaint.ListContracts.FirstOrDefault().Id;
                    }                    
                }

                complaint.UserCreated = username;
            }
            return View(complaint);
        }

        public ActionResult ViewComplaintTracker(int Id)
        {
            viewComplaintTracker complaint;

            if (Id != 0)
            {
                complaint = CMSService.GetComplaintTracker(Id);
                complaint.ListContracts = CMSService.ListOfContracts(username).ToList();

                if (complaint.ListContracts.Count == 1)
                {
                    complaint.AgencyTypeDescription = complaint.ListContracts.FirstOrDefault().ContractName;
                    complaint.ContractId = complaint.ListContracts.FirstOrDefault().Id;
                }
            }
            else
            {
                complaint = new viewComplaintTracker();

                if (User.IsInRole("CMS_IDOAStaff"))
                {
                    complaint.AgencyTypeDescription = "IDoA Staff";
                }
                else if (User.IsInRole("CMS_RAAAdmin"))
                {
                    var psa = CMSService.GetPSAByUserName(username);
                    complaint.AgencyTypeDescription = psa.AreaName;
                    complaint.ContractId = psa.Id;
                }
                else if (User.IsInRole("CMS_Supervisor"))
                {
                    complaint.ListContracts = CMSService.ListOfContracts(username).ToList();

                    if (complaint.ListContracts.Count == 1)
                    {
                        complaint.AgencyTypeDescription = complaint.ListContracts.FirstOrDefault().ContractName;
                        complaint.ContractId = complaint.ListContracts.FirstOrDefault().Id;
                    }
                }

                complaint.UserCreated = username;
            }
            return View(complaint);
        }

        [HttpPost]
        public ActionResult SaveComplaintTracker(viewComplaintTracker complaint)
        {
            complaint.UserCreated = username;
            complaint.UserUpdated = username;

            if (complaint.StatusId == null)
            {
                complaint.StatusId = 3;
            }

            complaint.Id = CMSService.SaveComplaintTracker(complaint);
            return RedirectToAction("ListComplaintTrackers", "QualityAssurance");
        }

        public ActionResult SubmitComplaintTracker(viewComplaintTracker complaint)
        {
            complaint.UserCreated = username;
            complaint.UserUpdated = username;

            complaint.StatusId = 24;
            

            complaint.Id = CMSService.SaveComplaintTracker(complaint);
            var redirectUrl = new UrlHelper(Request.RequestContext).Action("ListComplaintTrackers", "QualityAssurance");
        
            return Json(redirectUrl, JsonRequestBehavior.AllowGet);
           
        }

        public ActionResult ListComplaintTrackers()
        {
            viewComplaintTracker viewtracker = new viewComplaintTracker();

            if (User.IsInRole("CMS_IDOAStaff"))
            {
                viewtracker.ListComplaintTracker = CMSService.ListComplaintTracker().ToList();
            }
            else if (User.IsInRole("CMS_RAAAdmin"))
            {
                //int UserPSAId = 0;
                //var userpsa = new APS.Data.Entities.viewPSA();
                var userpsa = APSCaseService.ListOfPSAsByUserName(username).FirstOrDefault();

                //viewtracker.ListComplaintTracker = CMSService.ListComplaintTracker().Where(i => userpsa.ListOfContracts.Any(s => s.Id == i.ContractId)).ToList();
                viewtracker.ListComplaintTracker = CMSService.ListComplaintTracker().Where(i => userpsa.Id == i.ContractId || userpsa.ListOfContracts.Any(s => s.Id == i.ContractId)).ToList();
            }
            else if (User.IsInRole("CMS_Supervisor"))
            {
                viewtracker.ListComplaintTracker = CMSService.ListComplaintTracker().Where(i => (i.ContractId != null && i.ContractId != 0 )&& contractids.Contains((int)i.ContractId)).ToList();
            }

            return View(viewtracker);
        }

        public ActionResult UploadSupportDocs()
        {
            //int var = Request.Files.Count;
            //int[] fileIds = new int[var];

            List<SupportingDocs> Files = new List<SupportingDocs>();

            if (HttpContext.Request.Files.AllKeys.Any())
            {
                for (int i = 0; i <= HttpContext.Request.Files.Count; i++)
                {
                    var file = HttpContext.Request.Files["files" + i];
                    if (file != null)
                    {
                        //int folderid = Convert.ToInt32(Request.Form["folderId"]);

                        string folderName = System.Configuration.ConfigurationManager.AppSettings["_QualityAssuranceReviews"];

                        if (!Directory.Exists(folderName))
                            Directory.CreateDirectory(folderName);

                        var fileSavePath = Path.Combine(folderName, System.IO.Path.GetFileName(file.FileName));
                        file.SaveAs(fileSavePath);

                        SupportingDocs supportdoc = new SupportingDocs();
                        supportdoc.FileName =  System.IO.Path.GetFileName(file.FileName);
                        supportdoc.UserCreated = ViewBag.UserName;
                        //supportdoc.StatusDescription = CaseStatus.Submitted.ToString();                           
                        //supportdoc.UserCreated = ViewBag.UserName;
                        //supportdoc.UserUpdated = ViewBag.UserName;

                        int id = CMSService.UploadFile(supportdoc);

                        SupportingDocs doc = new SupportingDocs();

                        doc = CMSService.DownloadFile(id);

                        Files.Add(doc);
                    }
                }
            }

            //for (int i = 0; i < Request.Files.Count; i++)
            //{
            //    var file = Request.Files[i];

            //    SupportingDocs supportdoc = new SupportingDocs();
            //    supportdoc.ContentLength = file.ContentLength;
            //    supportdoc.ContentType = file.ContentType;
            //    supportdoc.FileName = file.FileName;
            //    supportdoc.InputStream = ConvertToBytes(file.InputStream, supportdoc.ContentLength);

            //    fileIds[i] = CMSService.UploadFile(supportdoc);

            //    SupportingDocs doc = new SupportingDocs();

            //    doc = CMSService.DownloadFile(fileIds[i]);

            //    Files.Add(doc);
            //}


            return Json(new { Files, JsonRequestBehavior.AllowGet });
        }

        //[HttpPost]
        //public JsonResult Upload(viewQualityAssuranceTracker mdoel)
        //{
        //    ViewBag.UserName = username;

        //    try
        //    {
        //        if (HttpContext.Request.Files.AllKeys.Any())
        //        {
        //            for (int i = 0; i <= HttpContext.Request.Files.Count; i++)
        //            {
        //                var file = HttpContext.Request.Files["files" + i];
        //                if (file != null)
        //                {
        //                    int folderid = Convert.ToInt32(Request.Form["folderId"]);
        //                    string folderName = System.Configuration.ConfigurationManager.AppSettings["_QualityAssuranceReviewUploads"];

        //                    if (!Directory.Exists(folderName))
        //                        Directory.CreateDirectory(folderName);

        //                    var fileSavePath = Path.Combine(folderName, file.FileName);
        //                    file.SaveAs(fileSavePath);

        //                    SupportingDocs supportdoc = new SupportingDocs();
        //                    supportdoc.FileName = file.FileName;
        //                    supportdoc.UserCreated = ViewBag.UserName;
        //                    //supportdoc.StatusDescription = CaseStatus.Submitted.ToString();                           
        //                    //supportdoc.UserCreated = ViewBag.UserName;
        //                    //supportdoc.UserUpdated = ViewBag.UserName;

        //                    int id = CMSService.UploadFile(supportdoc);  
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        throw e;
        //    }           

        //    return Json(new
        //    {
        //        statusCode = 200,
        //        status = "Success"
        //    }, JsonRequestBehavior.AllowGet);

        //}

        [HttpPost]
        public FileResult DownloadDoc(int Id)
        {
            //SupportingDocs supportdoc = CMSService.DownloadDocument(Id); 
            SupportingDocs supportdoc = CMSService.DownloadFile(Id); // LMD: they were calling the wrong method. 

            string folderName = System.Configuration.ConfigurationManager.AppSettings["_QualityAssuranceReviews"];

            var fileSavePath = Path.Combine(folderName, supportdoc.FileName);

            byte[] fileBytes = System.IO.File.ReadAllBytes(fileSavePath);

            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, supportdoc.FileName);

        }

        //protected byte[] ConvertToBytes(Stream InputStream, int contentlength)
        //{
        //    byte[] bytes = null;
        //    BinaryReader reader = new BinaryReader(InputStream);
        //    bytes = reader.ReadBytes((int)contentlength);
        //    return bytes;

        //}
    }
}
