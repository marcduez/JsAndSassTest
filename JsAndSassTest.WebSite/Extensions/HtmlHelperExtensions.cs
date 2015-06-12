using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using JsAndSassTest.WebSite.Services;

namespace JsAndSassTest.WebSite.Extensions
{
    /// <summary>
    /// Extensions for <see cref="HtmlHelper"/>.
    /// </summary>
    public static class HtmlHelperExtensions
    {
        /// <summary>
        /// Renders a tag for the provided script path.
        /// </summary>
        /// <example>
        /// @Html.RenderScript("scripts/myscript.js")
        /// </example>
        /// <param name="htmlHelper">This HTML helper.</param>
        /// <param name="bundlePath">The path of the script to render, relative to the static content directory.</param>
        /// <returns>The HTML of the script tag.</returns>
        public static IHtmlString RenderScriptBundle(this HtmlHelper htmlHelper, string bundlePath)
        {
            return RenderInner(bundlePath, "<script src=\"{0}\"></script>", htmlHelper.ViewContext.HttpContext);
        }

        /// <summary>
        /// Renders a tag for the provided style sheet path.
        /// </summary>
        /// <example>
        /// @Html.RenderStyleSheet("styles/mystyles.css")
        /// </example>
        /// <param name="htmlHelper">This HTML helper.</param>
        /// <param name="bundlePath">The path of the style sheet to render, relative to the static content directory.</param>
        /// <returns>The HTML of the style sheet tag.</returns>
        public static IHtmlString RenderStyleSheetBundle(this HtmlHelper htmlHelper, string bundlePath)
        {
            return RenderInner(bundlePath, "<link href=\"{0}\" rel=\"stylesheet\"/>", htmlHelper.ViewContext.HttpContext);
        }

        /// <summary>
        /// Returns whether content is bundled.
        /// </summary>
        public static bool IsContentBundled(this HtmlHelper htmlHelper)
        {
            return StaticContentPathResolver.Bundle;
        }

        private static IHtmlString RenderInner(string bundlePath, string tagFormat, HttpContextBase httpContext)
        {
            StringBuilder stringBuilder;
            var urls = StaticContentPathResolver.GetBundleUrls(bundlePath).ToArray();
            if (StaticContentPathResolver.Bundle)
            {
                // Return script for each bundle URL.
                stringBuilder = new StringBuilder();
                foreach (var url in urls.Select(x => UrlHelper.GenerateContentUrl(x, httpContext)))
                {
                    stringBuilder.Append(string.Format(tagFormat, HttpUtility.UrlPathEncode(url)));
                    stringBuilder.AppendLine();
                }
                return new HtmlString(stringBuilder.ToString());
            }

            // Return script for each bundle component, with comment at beginning and end.
            stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("<!-- Begin Bundle: {0} -->", bundlePath);
            stringBuilder.AppendLine();
            foreach (var url in urls.Select(x => UrlHelper.GenerateContentUrl(x, httpContext)))
            {
                stringBuilder.Append(string.Format(tagFormat, HttpUtility.UrlPathEncode(url)));
                stringBuilder.AppendLine();
            }
            stringBuilder.AppendFormat("<!-- End Bundle: {0} -->", bundlePath);
            stringBuilder.AppendLine();
            return new HtmlString(stringBuilder.ToString());
        }
    }
}