using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Xml;
using JsAndSassTest.WebSite.Configuration;

namespace JsAndSassTest.WebSite.Services
{
    /// <summary>
    /// Static content path resolver.
    /// TODO: Add file watcher, resets cache.
    /// </summary>
    public static class StaticContentPathResolver
    {
        /// <summary>
        /// Returns the next CDN prefix.
        /// </summary>
        public static string GetNextContentRoot()
        {
            if (StaticContentSection.ContentRoots.Count == 0)
                return null;
            var result = StaticContentSection.ContentRoots[
                Math.Abs(Interlocked.Increment(ref _prefixIndex)) % StaticContentSection.ContentRoots.Count].Path;
            return result.TrimEnd('/') + "/";
        }

        /// <summary>
        /// Returns all CDN prefixes.
        /// </summary>
        public static IEnumerable<string> GetContentRoots()
        {
            return StaticContentSection.ContentRoots.Cast<ContentRootElement>().Select(x => x.Path).ToArray();
        }

        /// <summary>
        /// Returns provided path with hash code appended (if bundling), with one of the configured prefixes applied.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="useContentRoot"></param>
        /// <returns></returns>
        /// <example>
        /// StaticContentPathResolver.GetStaticContentPath("~/images/puppy.gif") => "http://www.cdn1.com/static/images/puppy.dsf87fs4.gif"
        /// </example>
        public static string GetStaticContentPath(string path, bool useContentRoot = true)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            // If path does not start with '~/' then return as-is.
            if (!path.StartsWith("~/"))
                return path;

            // Strip leading '~/' and split from query string.
            string query;
            var contentPathWithoutQuery = StripQuery(path.Substring(2), out query);

            // If no match is found in paths-with-hash, return as-is.
            string hashPath;
            var hashPathsByPath = HashPathsByPath;
            hashPathsByPath.TryGetValue(contentPathWithoutQuery, out hashPath);

            return ((useContentRoot ? GetNextContentRoot() : "~/") + (hashPath ?? contentPathWithoutQuery)).ToLowerInvariant() + query;
        }

        /// <summary>
        /// Returns the URLs to render the bundle with the provided path.
        /// If bundling, echoes back the provided path. Otherwise, returns the paths of the components of the bundle.
        /// </summary>
        /// <param name="bundlePath">The path to the bundle.
        /// E.g. "~/scripts/myscript.min.js"</param>
        /// <returns></returns>
        public static IEnumerable<string> GetBundleUrls(string bundlePath)
        {
            if (bundlePath == null)
                throw new ArgumentNullException("bundlePath");
            if (bundlePath.IndexOf("?", StringComparison.Ordinal) >= 0)
                throw new ArgumentException("Bundle path must not contain query arguments.", "bundlePath");

            // If bundling, or path does not start with '~/', then return as-is.
            if (Bundle || !bundlePath.StartsWith("~/"))
                return new[] {GetStaticContentPath(bundlePath)};

            bundlePath = bundlePath.Substring(2);

            var componentPathsByBundlePaths = ComponentPathsByBundlePath;

            ReadOnlyCollection<string> componentPaths;
            if (!componentPathsByBundlePaths.TryGetValue(bundlePath, out componentPaths))
            {
                // No bundle could be found with the provided path. Return path as-is.

                if (StaticContentSection.ThrowOnPathNotResolved)
                    throw new Exception(string.Format("Could not find bundle with path '{0}'.", bundlePath));
                return new[] {GetStaticContentPath(bundlePath)};
            }

            // If a bundle exists with the provided path, return the component scripts of the bundle.
            return
                componentPaths.Select(x => x.StartsWith("~/") ? x : "~/" + x)
                    .Select(x => GetStaticContentPath(x, false))
                    .ToArray();
        }

        /// <summary>
        /// Whether to bundle scripts and style sheets.
        /// </summary>
        public static bool Bundle
        {
            get { return StaticContentSection.Bundle; }
        }

        private static string StripQuery(string path, out string query)
        {
            var queryIndex = path.IndexOf('?');
            if (queryIndex >= 0)
            {
                query = path.Substring(queryIndex);
                return path.Substring(0, queryIndex);
            }

            query = null;
            return path;
        }

        private static void GetHashPathsRecursive(IDictionary<string, string> hashPathsByPath, string directoryPath, string pathPrefix)
        {
            foreach (var file in Directory.GetFiles(directoryPath))
            {
                var filenameWithHash = Path.GetFileNameWithoutExtension(file);
                Debug.Assert(filenameWithHash != null);
                var extension = Path.GetExtension(file);
                if (!HashPattern.IsMatch(filenameWithHash))
                    continue;

                var filenameWithoutHash = filenameWithHash.Substring(0, filenameWithHash.Length - 9);

                // Strip any known content roots from the filename.
                var contentRoot =
                    StaticContentSection.ContentRoots.Cast<ContentRootElement>()
                        .FirstOrDefault(x => ("~/" + pathPrefix + filenameWithoutHash).StartsWith(x.Path,
                                StringComparison.OrdinalIgnoreCase));
                var key = pathPrefix + filenameWithoutHash + extension;
                var value = pathPrefix + filenameWithHash + extension;
                if (contentRoot != null)
                {
                    key = key.Substring(contentRoot.Path.Length - 2);
                    value = value.Substring(contentRoot.Path.Length - 2);
                }

                hashPathsByPath[key.TrimStart('/')] = value.TrimStart('/');
            }

            foreach (var directory in Directory.GetDirectories(directoryPath))
            {
                GetHashPathsRecursive(hashPathsByPath, directory, pathPrefix + Path.GetFileName(directory) + "/");
            }
        }

        private static IDictionary<string, ReadOnlyCollection<string>> ComponentPathsByBundlePath
        {
            get
            {
                // If it isn't yet time to reconsider the manifest, return exisiting value.
                if (DateTime.UtcNow < _nextBundleCheckUtc)
                    return _componentPathsByBundlePath;

                lock (BundleLock)
                {
                    if (DateTime.UtcNow < _nextBundleCheckUtc)
                        return _componentPathsByBundlePath;

                    // Indicate that manifest doesn't need to be re-evaluated for some period of time.                    
                    _nextBundleCheckUtc = DateTime.UtcNow.AddSeconds(30);

                    // Get timestamp of manifest file. If current parsed manifest is as new as manifest file, return existing.
                    var manifestPath = HttpContext.Current.Server.MapPath(StaticContentSection.AppRelativeManifestPath);
                    var manifestLastUpdatedUtc = File.GetLastWriteTimeUtc(manifestPath);
                    if (_manifestLastUpdatedUtc >= manifestLastUpdatedUtc)
                        return _componentPathsByBundlePath;

                    // Read Ajax Minifier manifest file to determine paths of bundled scripts, indexed by the path of their bundle.
                    var xmlDocument = new XmlDocument();
                    xmlDocument.Load(manifestPath);
                    var componentPathsByBundlePath = new Dictionary<string, ReadOnlyCollection<string>>();
                    // ReSharper disable PossibleNullReferenceException
                    // ReSharper disable AssignNullToNotNullAttribute
                    foreach (var outputNode in xmlDocument.SelectNodes("root/output").Cast<XmlNode>())
                    {
                        componentPathsByBundlePath[outputNode.Attributes["path"].Value] =
                            new ReadOnlyCollection<string>(outputNode.SelectNodes("input")
                                .Cast<XmlNode>()
                                .Select(x => x.Attributes["path"].Value)
                                .ToArray());
                    }
                    // ReSharper restore AssignNullToNotNullAttribute
                    // ReSharper restore PossibleNullReferenceException

                    // Remove any bundles that were configured with zero component paths.
                    var emptyKeys = componentPathsByBundlePath.Keys
                        .Where(x => componentPathsByBundlePath[x].Count == 0)
                        .ToArray();
                    foreach (var emptyKey in emptyKeys)
                    {
                        componentPathsByBundlePath.Remove(emptyKey);
                    }

                    // We've parsed this version of the file. Store that we last saw 
                    _manifestLastUpdatedUtc = manifestLastUpdatedUtc;
                    // Save new index.
                    _componentPathsByBundlePath = componentPathsByBundlePath;

                }

                return _componentPathsByBundlePath;
            }
        }

        private static IDictionary<string, string> HashPathsByPath
        {
            get
            {
                if (_hashPathsByPath != null)
                    return _hashPathsByPath;

                lock (HashPathsByPathLock)
                {
                    if (_hashPathsByPath != null)
                        return _hashPathsByPath;

                    var hashPathsByPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var httpContext = HttpContext.Current;

                    // Resolve app-relative static directory on disk.
                    var directory = httpContext.Server.MapPath(StaticContentSection.AppRelativeStaticFileDirectory);
                    
                    GetHashPathsRecursive(hashPathsByPath, directory, "");

                    return (_hashPathsByPath = hashPathsByPath);
                }
            }
        }

        private static StaticContentSection StaticContentSection
        {
            get { return (StaticContentSection) ConfigurationManager.GetSection("staticContent"); }
        }

        private static volatile IDictionary<string, string> _hashPathsByPath;
        private static readonly object HashPathsByPathLock = new object();
        private static volatile IDictionary<string, ReadOnlyCollection<string>> _componentPathsByBundlePath;
        private static DateTime _nextBundleCheckUtc = DateTime.MinValue;
        private static DateTime _manifestLastUpdatedUtc = DateTime.MinValue;
        private static readonly object BundleLock = new object();
        private static int _prefixIndex;

        private static readonly Regex HashPattern = new Regex(@"\.[a-z0-9]{8}$", RegexOptions.IgnoreCase);
    }
}