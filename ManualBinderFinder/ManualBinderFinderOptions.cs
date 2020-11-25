using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Mono.Options;

namespace ManualBinderFinder {
    public class ManualBinderFinderOptions {

        public OptionSet optionsSet { get; set; }
        public List<string> dylibLibraryList { get; set; }
        public string platform { get; set; }
        public string architecture { get; set; }
        public bool PrintHelp { get; set; }

        public string[] validPlatform = new string[9]
        {
            "all",
            "clang",
            "watchos",
            "iphoneos",
            "iphonesimulator",
            "watchsimulator",
            "appletvsimulator",
            "appletvos",
            "macosx",
        };

        public string[] validArchitecture = new string[6]
        {
            "all",
            "arm64",
            "arm64e",
            "armv7",
            "x86_64",
            "i386",
        };

        public ManualBinderFinderOptions ()
        {

            dylibLibraryList = new List<string>();

            // create an option set that will be used to parse the different
            // options of the command line.
            optionsSet = new OptionSet {
                { "library=", "the name of the dylib to inspect\n(defaults to all)\n[all, <name of dylib>]", @library => {
					dylibLibraryList.Add (@library);
                } },
                { "platform=", "the name of the platform to inspect\n(defaults to all)\n[all, clang, watchOS, iOS, iphoneSimulator, watchSimulator, appletvSimulator, tvOS, macOS]", p => {
                    platform = p;
                } },
                { "architecture=", "the name of the architecture to inspect\n(defaults to all)\n[all, arm64, arm64e, armv7, x86_64, i386]", a => {
                    architecture = a;
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
