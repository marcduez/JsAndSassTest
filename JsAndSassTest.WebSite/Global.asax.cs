using System;
using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace JsAndSassTest.WebSite
{
    /// <summary>
    /// Application.
    /// </summary>
    public class Global : HttpApplication
    {
        /// <summary>
        /// Handler for the start event of the application.
        /// </summary>
        private void Application_Start(object sender, EventArgs e)
        {
            SimpleInjectorInitializer.Initialize();

            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }
}