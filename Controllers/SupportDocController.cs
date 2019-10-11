using AGE.CMS.Data.Models.Intake;
using AGE.CMS.Data.Models.SupportDocs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using DOAFramework.Core.WebMVC.Filters;

namespace AGE.CMS.Web.Areas.CMS.Controllers
{
         [Layout("_Layout")]
    public class SupportDocController : CMSController
    {
        public ActionResult FileUpload()
        {
            List<SupportingDocs> listofdocs = new List<SupportingDocs>();

            return View();
        }

        [HttpPost]
        public ActionResult UploadSupportDocs(viewCaseHeader model)
        {
            int clientid = 0;
            int caseheaderid = 0;
            try
            {


                if (HttpContext.Request.Files.AllKeys.Any())
                {
                    for (int i = 0; i <= HttpContext.Request.Files.Count; i++)
                    {
                        var file = HttpContext.Request.Files["files" + i];
                        if (file != null)
                        {
                            int folderid = Convert.ToInt32(Request.Form["folderId"]);
                            caseheaderid = Convert.ToInt32(Request.Form["caseheaderId"]);
                            viewCaseHeader viewcaseheader = CMSService.GetCaseHeader(clientid);

                            string folderName = System.Configuration.ConfigurationManager.AppSettings["_SupportDocuments"] + caseheaderid + '\\' + codeTable.ListOfFolders.Where(f => f.Id == folderid).FirstOrDefault().Description.Replace(" ", "");


                            if (!Directory.Exists(folderName))
                                Directory.CreateDirectory(folderName);

                            var fileSavePath = Path.Combine(folderName, System.IO.Path.GetFileName(file.FileName));
                            file.SaveAs(fileSavePath.Replace(" ", ""));


                            SupportingDocs supportdoc = new SupportingDocs();
                            supportdoc.FileName = System.IO.Path.GetFileName(file.FileName.Replace(" ", ""));                            
                            supportdoc.FolderTypeId = folderid;
                            supportdoc.CaseheaderId = caseheaderid;
                          
                            CMSService.SaveFileDetails(supportdoc);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }


            //return RedirectToAction("ManageCase", "Case", new { Id = clientid, CaseheaderId = caseheaderid });

            var redirectUrl = new UrlHelper(Request.RequestContext).Action("ManageCase", "Case", new { CaseheaderId = caseheaderid });

            return Json(new { Url = redirectUrl });
        }

        [HttpPost]
        public FileResult DownloadDoc(int Id, int CaseheaderId)
        {
            SupportingDocs supportdoc = CMSService.DownloadDocument(Id);

            string folderName = System.Configuration.ConfigurationManager.AppSettings["_SupportDocuments"] + CaseheaderId + '\\' + codeTable.ListOfFolders.Where(f => f.Id == supportdoc.FolderTypeId).FirstOrDefault().Description.Replace(" ", "");

            var fileSavePath = Path.Combine(folderName, supportdoc.FileName);

            byte[] fileBytes = System.IO.File.ReadAllBytes(fileSavePath);

            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, supportdoc.FileName);
        }
    }
}
