using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NayuTerm.Models
{
    static class ShellHost
    {
        private static readonly string HomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private static ProcessStartInfo _processStartInfo;
        public static Process Shell { get; private set; }

        public static void Initialize(string shell)
        {
            _processStartInfo = new ProcessStartInfo(shell)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = HomeDir
            };

            Shell = Process.Start(_processStartInfo);
            Shell.EnableRaisingEvents = true;
            Shell.Exited += (sender, args) => Environment.Exit(0);
        }

        public static void StartReadStdOut()
        {
            Shell.BeginOutputReadLine();
            Shell.BeginErrorReadLine();
            Shell.StandardInput.WriteLine();
        }

        public static void Dispose()
        {
            Shell.Dispose();
        }
    }
}
