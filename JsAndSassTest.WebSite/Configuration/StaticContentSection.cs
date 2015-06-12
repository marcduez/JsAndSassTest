using System.Configuration;

namespace JsAndSassTest.WebSite.Configuration
{
    /// <summary>
    /// Configuration section for static content
    /// </summary>
    public class StaticContentSection : ConfigurationSection
    {
        /// <summary>
        /// Whether or not to bundle files.
        /// </summary>
        [ConfigurationProperty("bundle", DefaultValue = false)]
        public bool Bundle
        {
            get { return (bool) this["bundle"]; }
            set { this["bundle"] = value; }
        }

        /// <summary>
        /// The path to the Ajax Minifier manifest file that describes the static content bundles.
        /// </summary>
        [ConfigurationProperty("appRelativeManifestPath")]
        public string AppRelativeManifestPath
        {
            get { return (string) this["appRelativeManifestPath"]; }
            set { this["appRelativeManifestPath"] = value; }
        }

        /// <summary>
        /// The app-relative directory for static files.
        /// </summary>
        [ConfigurationProperty("appRelativeStaticFileDirectory", DefaultValue = "~")]
        public string AppRelativeStaticFileDirectory
        {
            get { return (string)this["appRelativeStaticFileDirectory"]; }
            set { this["appRelativeStaticFileDirectory"] = value; }
        }

        /// <summary>
        /// Whether or throw when a bundle path cannot be resolved.
        /// </summary>
        [ConfigurationProperty("throwOnBundleNotResolved", DefaultValue = false)]
        public bool ThrowOnPathNotResolved
        {
            get { return (bool)this["throwOnBundleNotResolved"]; }
            set { this["throwOnBundleNotResolved"] = value; }
        }

        /// <summary>
        /// The root paths to use for serving static content.
        /// These paths are pre-pended to all generated content URLS in round-robin fashion.
        /// </summary>
        [ConfigurationProperty("contentRoots", IsDefaultCollection = false)]
        public GenericElementCollection<ContentRootElement> ContentRoots
        {
            get
            {
                var output = (GenericElementCollection<ContentRootElement>)base["contentRoots"];
                if (output.KeySelector == null)
                    output.KeySelector = e => e.Path;
                return output;
            }
        }
    }
}