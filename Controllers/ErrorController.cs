using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DOAFramework.Core.WebMVC.Filters;

namespace AGE.CMS.Web.Areas.CMS.Controllers
{
         [Layout("_Layout")]
    public class ErrorController : Controller
    {
        //
        // GET: /Error/

        public ActionResult UnAuthorized()
        {
            ViewBag.Message = "You are not Authorized to perform this action, Please contact adminstrator.";
            return View();
        }

    }
}
