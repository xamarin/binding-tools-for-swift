using System;
using System.Diagnostics;

namespace ManualBinderFinder {
    public static class Shell {
        public static string RunBash (string script)
        {
                var process = new Process () {
                        StartInfo = new ProcessStartInfo {
                                FileName = "/bin/bash",
                                Arguments = $"-c \"{script}\"",
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true,
                        }
                };
                process.Start ();
                string result = process.StandardOutput.ReadToEnd ();
                process.WaitForExit ();
                return result;
        }
    }
}
