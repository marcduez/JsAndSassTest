using System;
using System.IO;

namespace JsAndSassTest.SassCompiler
{
    /// <summary>
    /// Program.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Main method.
        /// </summary>
        private static void Main(string[] args)
        {
            var sourceFile = args[0];
            var destinationFile = args[1];

            Directory.SetCurrentDirectory(Path.GetDirectoryName(sourceFile));

            var source = File.ReadAllText(sourceFile);
            Console.WriteLine("SASS compile: '{0}' -> '{1}'", sourceFile, destinationFile);
            File.WriteAllText(destinationFile, new NSass.SassCompiler().Compile(source, sourceComments: false));
        }
    }
}
