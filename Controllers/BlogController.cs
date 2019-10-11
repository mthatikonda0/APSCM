using AGE.CMS.Data.Models.Blog;
using DOAFramework.Core.WebMVC.Filters;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
namespace AGE.CMS.Web.Areas.CMS.Controllers
{
    [Layout("_Layout")]
    public class BlogController : CMSController
    {
        public ActionResult Index()
        {
            List<BlogPostModel> posts = CMSService.GetPosts().ToList();

            return View(posts);
        }

        public ActionResult Create(int Id)
        {
            BlogPostModel post = new BlogPostModel();

            if (Id == 0)
            {
                post = new BlogPostModel();
            }
            else
            {
                post = CMSService.GetPost(Id);
            }

            return View(post);
        }


        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(BlogPostModel post)
        {
            post.UserCreated = username;
            post.UserUpdated = username;
            CMSService.SavePost(post);
            return RedirectToAction("Index");
        }

        public ActionResult GetPost(int? Id)
        {
            //BlogPostModel post = CMSService.GetPost(Id);

            return Json(CMSService.GetPost((int)Id), JsonRequestBehavior.AllowGet);
        }

        public ActionResult DeletePost(int Id)
        {
            //return Json(CMSService.RemovePost(Id, username), JsonRequestBehavior.AllowGet);

            CMSService.RemovePost(Id, username);

            return RedirectToAction("Index", "Blog");
        }

        public ActionResult Comment(int? PostId)
        {
            BlogPostModel post;
            if (PostId == null)
            {
                post = new BlogPostModel();
            }
            else
            {
                post = CMSService.GetPost((int)PostId);
            }

            return View(post);
        }

        public ActionResult DeleteComment(int Id)
        {
            //return Json(CMSService.RemoveComment(Id, username), JsonRequestBehavior.AllowGet);
            CMSService.RemoveComment(Id, username);

            return RedirectToAction("Index", "Blog");
        }

        //[HttpPost]
        public ActionResult SaveComment(int? PostId, int? CommentId, string CommentContent)
        {
            Comment comment = new Comment();
            comment.BlogId = PostId;
            comment.Id = CommentId == null ? 0 : (int)CommentId;
            comment.Content = CommentContent;

            comment.UserCreated = username;
            comment.UserUpdated = username;

            comment.Id = CMSService.SaveComment(comment);



            return Json(comment, JsonRequestBehavior.AllowGet);
        }

    }
}
