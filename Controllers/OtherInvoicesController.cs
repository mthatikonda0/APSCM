using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AGE.CMS.Data.Models.OtherInvoices;
using DOAFramework.Core.WebMVC.Filters;

namespace AGE.CMS.Web.Areas.CMS.Controllers
{
         [Layout("_Layout")]
    public class OtherInvoicesController : CMSController
    {
        //
        // GET: /OtherInvoices/

        public ActionResult Index()
        {
            return View();
        }


        public ActionResult EditBsafe(int Id)
        {
            viewBSafe bsafe;
            if (Id == 0)
            {
                 bsafe = new viewBSafe();
            }
            else
            {
                bsafe = CMSService.GetBsafe(Id);
            }

            return View(bsafe);
        }


        public ActionResult SaveBsafe(viewBSafe viewbsafe)
        {
            
            viewbsafe.UserCreated = User.Identity.Name;
            int id = CMSService.SaveBsafe(viewbsafe);

            return View();
        }



        [HttpPost]
        public ActionResult UploadAddtionalResource(viewBSafe model)
        {           
            //try
            //{
            //    if (HttpContext.Request.Files.AllKeys.Any())
            //    {
            //        for (int i = 0; i <= HttpContext.Request.Files.Count; i++)
            //        {
            //            var file = HttpContext.Request.Files["files" + i];
            //            if (file != null)
            //            {
            //                int folderid = Convert.ToInt32(Request.Form["folderId"]);
            //                string folderName = System.Configuration.ConfigurationManager.AppSettings["_AdditionalResources"] + CMSService.ListOfAdditionalResourcesFolders().Where(f => f.Id == folderid).FirstOrDefault().Description.Replace(" ", "");

            //                if (!Directory.Exists(folderName))
            //                    Directory.CreateDirectory(folderName);

            //                var fileSavePath = Path.Combine(folderName, file.FileName);
            //                file.SaveAs(fileSavePath);

            //                viewAdditionalResources supportdoc = new viewAdditionalResources();
            //                supportdoc.FileName = file.FileName;
            //                supportdoc.StatusDescription = CaseStatus.Submitted.ToString();
            //                supportdoc.FolderTypeId = folderid;
            //                supportdoc.UserCreated = ViewBag.UserName;
            //                supportdoc.UserUpdated = ViewBag.UserName;

            //                CMSService.SaveAdditionalResource(supportdoc);
            //            }
            //        }
            //    }
            //    else if (model.Url != null)
            //    {
            //        model.UserCreated = ViewBag.UserName;
            //        model.UserUpdated = ViewBag.UserName;
            //        model.IsWebinar = false;
            //        model.StatusDescription = CaseStatus.Submitted.ToString();
            //        CMSService.SaveAdditionalResourceURL(model);
            //    }
            //    return RedirectToAction("AdditionalResources", "AdditionalResources");
            //}
            //catch (Exception e)
            //{
            //    throw e;
            //}

            return View();
        }




























    }
}
