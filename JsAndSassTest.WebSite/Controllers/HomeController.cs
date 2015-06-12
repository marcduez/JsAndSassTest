using System.Web.Mvc;

namespace JsAndSassTest.WebSite.Controllers
{
    /// <summary>
    /// Home controller.
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// Index.
        /// </summary>
        [Route("")]
        public ActionResult Index()
        {
            return View();
        }
    }
}