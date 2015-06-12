using System.Configuration;

namespace JsAndSassTest.WebSite.Configuration
{
    /// <summary>
    /// Element that describes a root path from which static content is served.
    /// </summary>
    public class ContentRootElement : ConfigurationElement
    {
        /// <summary>
        /// The path of this element.
        /// </summary>
        [ConfigurationProperty("path", IsRequired = true)]
        public string Path
        {
            get { return (string)this["path"]; }
            set { this["path"] = value; }
        }
    }
}