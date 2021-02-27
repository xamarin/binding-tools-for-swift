using System;
using System.Diagnostics;

namespace DylibBinder {
    public static class Shell {
        public static string RunBash (string script)
        {
            var process = new Process ();

            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = $"-c \"{script}\"";
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start ();
            string result = process.StandardOutput.ReadToEnd ();
            process.WaitForExit ();
            return result;
        }
    }
}
