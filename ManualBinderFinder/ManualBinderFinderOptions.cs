using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Mono.Options;

namespace ManualBinderFinder {
    public class ManualBinderFinderOptions {

        public OptionSet optionsSet { get; set; }
        public List<string> dylibLibraryList { get; set; }
        public bool PrintHelp { get; set; }

        public ManualBinderFinderOptions ()
        {

            dylibLibraryList = new List<string>();
            // create an option set that will be used to parse the different
            // options of the command line.
            optionsSet = new OptionSet {
                { "library=", "the name of the dylib to inspect", @library => {
					//if (!string.IsNullOrEmpty (library))
					dylibLibraryList.Add (@library);
                } },
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
