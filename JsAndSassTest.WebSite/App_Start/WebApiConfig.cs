using System.Web.Http;

namespace JsAndSassTest.WebSite
{
    /// <summary>
    /// Web API configuration.
    /// </summary>
    public static class WebApiConfig
    {
        /// <summary>
        /// Registers this Web API config.
        /// </summary>
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Remove XML formatter.
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            // Web API routes
            config.MapHttpAttributeRoutes();

            //config.Routes.MapHttpRoute(
            //    name: "DefaultApi",
            //    routeTemplate: "api/{controller}/{id}",
            //    defaults: new { id = RouteParameter.Optional }
            //);
        }
    }
}
