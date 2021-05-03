using System;
using System.IO;
using System.Reflection;
using Mono.Options;

namespace DylibBinder {
    public class DylibBinderOptions {

        public OptionSet optionsSet { get; set; }
        public bool PrintHelp { get; set; }
        public string DylibPath { get; set; }
        public string OutputPath { get; set; }
        public string SwiftVersion { get; set; } = "5.0";

        public DylibBinderOptions ()
        {
            optionsSet = new OptionSet {
                { "dylibPath=", "the path to the dylib", dylibPath => {
                    DylibPath = dylibPath;
                } },
                { "outputPath=", "the path to output the xml", outputPath => {
                    OutputPath = outputPath;
                } },
                { "swiftVersion=", "the swift version", swiftVersion => {
                    SwiftVersion = swiftVersion;
                } },
                { "h|?|help", "prints this message", h => {
                    PrintHelp |= h != null;
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
