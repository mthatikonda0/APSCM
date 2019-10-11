using AGE.CMS.Data.Models.AdditionalResources;
using AGE.CMS.Data.Models.ENums;
using DOAFramework.Core.WebMVC.Filters;
using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace AGE.CMS.Web.Areas.CMS.Controllers
{
    [Layout("_Layout")]
    public class AdditionalResourcesController : CMSController
    {

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult AdditionalResources()
        {
            viewAdditionalResources viewadditionalresources = new viewAdditionalResources();
            //using (var CMSService = new AGE.CMS.Core.CaseManagementServiceRef.CaseManagementServiceClient())
            //{
                viewadditionalresources.ListAdditionalResources = CMSService.ListOfAdditionalResources().ToList();

                viewadditionalresources.ListAdditionalResourcesURLs = CMSService.ListOfAdditionalResourcesURLs().Where(s => !(bool)s.IsWebinar).ToList();

                viewadditionalresources.ListFolderTypes = CMSService.ListOfAdditionalResourcesFolders().ToList();

            //}
            return View(viewadditionalresources);
        }

        public ActionResult CaseResources()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UploadAddtionalResource(viewAdditionalResources model)
        {
            try
            {
                //using (var CMSService = new AGE.CMS.Core.CaseManagementServiceRef.CaseManagementServiceClient())
                //{
                    if (HttpContext.Request.Files.AllKeys.Any())
                    {
                        for (int i = 0; i <= HttpContext.Request.Files.Count; i++)
                        {
                            var file = HttpContext.Request.Files["files" + i];
                            if (file != null)
                            {

                                int folderid = Convert.ToInt32(Request.Form["folderId"]);
                                string folderName = System.Configuration.ConfigurationManager.AppSettings["_AdditionalResources"] + CMSService.ListOfAdditionalResourcesFolders().Where(f => f.Id == folderid).FirstOrDefault().Description.Replace(" ", "");

                                if (!Directory.Exists(folderName))
                                    Directory.CreateDirectory(folderName);

                                var fileSavePath = Path.Combine(folderName, System.IO.Path.GetFileName(file.FileName));
                                file.SaveAs(fileSavePath.Replace(" ", ""));

                                viewAdditionalResources supportdoc = new viewAdditionalResources();
                                supportdoc.FileName = System.IO.Path.GetFileName(file.FileName.Replace(" ", ""));
                                supportdoc.StatusDescription = CaseStatus.Submitted.ToString();
                                supportdoc.FolderTypeId = folderid;
                                supportdoc.UserCreated = username;
                                supportdoc.UserUpdated = username;

                                CMSService.SaveAdditionalResource(supportdoc);
                            }
                        }

                        var redirectUrl = new UrlHelper(Request.RequestContext).Action("AdditionalResources", "AdditionalResources");

                        return Json(new { Url = redirectUrl });
                    }
                    else if (model.Url != null)
                    {
                        model.UserCreated = username;
                        model.UserUpdated = username;
                        model.IsWebinar = false;
                        model.StatusDescription = CaseStatus.Submitted.ToString();
                        CMSService.SaveAdditionalResourceURL(model);
                        //return RedirectToAction("AdditionalResources", "AdditionalResources");
                    }
                //}
                return RedirectToAction("AdditionalResources", "AdditionalResources");

            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpPost]
        public FileResult DownloadAdditonalResource(int Id)
        {
            viewAdditionalResources supportdoc = CMSService.DownloadAdditionalResource(Id);

            string folderName = System.Configuration.ConfigurationManager.AppSettings["_AdditionalResources"] + CMSService.ListOfAdditionalResourcesFolders().Where(f => f.Id == supportdoc.FolderTypeId).FirstOrDefault().Description.Replace(" ", "");

            var fileSavePath = Path.Combine(folderName, supportdoc.FileName);

            byte[] fileBytes = System.IO.File.ReadAllBytes(fileSavePath);

            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, supportdoc.FileName);
        }

        [HttpPost]
        public ActionResult RemoveAdditionalResource(int ResourceId)
        {           
            CMSService.RemoveAdditionalResource(ResourceId, username);

            return RedirectToAction("AdditionalResources", "AdditionalResources");
        }

        [HttpPost]
        public ActionResult RemoveAdditionalResourceURL(int ResourceURLId)
        {
            CMSService.RemoveAdditionalResourceURL(ResourceURLId, username);

            return RedirectToAction("AdditionalResources", "AdditionalResources");
        }

        public ActionResult ViewVideoResource(int VideoResourceId)
        {
            viewAdditionalResources supportdoc = CMSService.DownloadAdditionalResource(VideoResourceId);

            Stream stream = new MemoryStream(supportdoc.InputStream);

            return new FileStreamResult(stream, supportdoc.ContentType);
        }

        protected byte[] ConvertToBytes(Stream InputStream, int contentlength)
        {
            byte[] bytes = null;
            BinaryReader reader = new BinaryReader(InputStream);
            bytes = reader.ReadBytes((int)contentlength);
            return bytes;

        }

        public ActionResult EditLiasionAssignments(int Id)
        {
            viewLiaisonAssignments viewLA = CMSService.GetLiaisonAssignments(Id);


            return Json(viewLA, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DeleteLiasionAssignment(int Id)
        {
            CMSService.DeleteLiaisonAssignments(Id, username);

            var redirectUrl = new UrlHelper(Request.RequestContext).Action("ListLiasionAssignments", "AdditionalResources");
            return Json(redirectUrl, JsonRequestBehavior.AllowGet);
        }

        public ActionResult SaveLiasionAssignments(viewLiaisonAssignments viewlialisonassignment)
        {
            viewlialisonassignment.IsDelete = false;
            var viewLiId = CMSService.SaveLiaisonAssignments(viewlialisonassignment);
            return RedirectToAction("ListLiasionAssignments");
        }

        public ActionResult ListLiasionAssignments()
        {
            viewLiaisonAssignments viewliaisons = new viewLiaisonAssignments();

            viewliaisons.listofLiaisonAssignments = CMSService.ListofLiaisonAssignments().ToList();

            return View(viewliaisons);


        }

        public JsonResult GetLiasionAssignments()
        {
            viewLiaisonAssignments viewliaisons = new viewLiaisonAssignments();

            viewliaisons.listofLiaisonAssignments = CMSService.ListofLiaisonAssignments().ToList();

            return Json(viewliaisons.listofLiaisonAssignments, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public FileResult DownloadCaseResource(string filename)
        {
            string folderpath = System.Configuration.ConfigurationManager.AppSettings["_CaseResources"];

            var fileSavePath = Path.Combine(folderpath, filename);

            byte[] fileBytes = System.IO.File.ReadAllBytes(fileSavePath);

            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, filename);
        }

        #region Office Directory

        public ActionResult ListOfficeDirectory()
        {
            AGE.CMS.Data.Models.Intake.viewPSA psa = new Data.Models.Intake.viewPSA();

            psa.ListOfPSAs = CMSService.ListOfPSA().ToList();

            return View(psa);
        }

        public ActionResult EditContract(int Id)
        {
            AGE.CMS.Data.Models.Intake.viewContract contract = new Data.Models.Intake.viewContract();

            contract = CMSService.GetContract(Id);

            return Json(contract, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}