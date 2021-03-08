using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Mono.Options;

namespace DylibBinder {
    public class DylibBinderOptions {

        public OptionSet optionsSet { get; set; }
        public bool PrintHelp { get; set; }
        public string SwiftLibPath { get; set; }
        public string ModuleName { get; set; }
        public string DylibPath { get; set; }
        public List<string> TypeDatabasePaths = new List<string>();

        public DylibBinderOptions ()
        {
            // create an option set that will be used to parse the different
            // options of the command line.
            optionsSet = new OptionSet {
                { "swiftLibPath=", "the path to the lib directory that contains the dylib", swiftLibPath => {
                    SwiftLibPath = swiftLibPath;
                } },
                { "dylibPath=", "the path to the dylib", dylibPath => {
                    DylibPath = dylibPath;
                } },
                { "moduleName=", "the name of the module", moduleName => {
                    ModuleName = moduleName;
                } },
                { "type-database-path=", "searches in directory for type database files; can be used multiple times", p => {
                    if (!string.IsNullOrEmpty (p))
                        TypeDatabasePaths.Add (Path.GetFullPath (p));
                }},
                { "h|?|help", "prints this message", h => {
                    PrintHelp |=h != null;
                }}
            };
        }

        public void PrintUsage (TextWriter writer)
        {
            var location = Assembly.GetEntryAssembly ()?.Location;
            string exeName = (location != null) ? Path.GetFileName (location) : "";
            writer.WriteLine ($"Usage:");
            writer.WriteLine ($"\t{exeName} [options]");
            writer.WriteLine ("Options:");
            optionsSet.WriteOptionDescriptions (writer);
            return;
        }
    }
}
