using System.Configuration;
using System.Reflection;
using System.Web.Http;
using System.Web.Mvc;
using JsAndSassTest.WebSite.Configuration;
using SimpleInjector;
using SimpleInjector.Integration.Web.Mvc;
using SimpleInjector.Integration.WebApi;

namespace JsAndSassTest.WebSite
{
    public static class SimpleInjectorInitializer
    {
        /// <summary>
        /// 
        /// </summary>
        public static void Initialize()
        {
            var container = new Container();
            
            InitializeContainer(container);

            container.RegisterMvcControllers(Assembly.GetExecutingAssembly());
            container.RegisterWebApiControllers(GlobalConfiguration.Configuration);
            
            container.Verify();
            
            DependencyResolver.SetResolver(new SimpleInjectorDependencyResolver(container));
            GlobalConfiguration.Configuration.DependencyResolver =
                new SimpleInjectorWebApiDependencyResolver(container);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="container"></param>
        private static void InitializeContainer(Container container)
        {
            container.RegisterSingle((StaticContentSection) ConfigurationManager.GetSection("staticContent"));
        }
    }
}