using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NayuTerm.Models
{
    class CmdHost
    {
        private ProcessStartInfo _processStartInfo;
        private Process _cmd;

        public CmdHost()
        {
            _processStartInfo = new ProcessStartInfo(@"cmd.exe");
            _processStartInfo.UseShellExecute = false;
            _processStartInfo.RedirectStandardInput = true;
            _processStartInfo.RedirectStandardOutput = true;
            _processStartInfo.RedirectStandardError = true;

            _cmd = Process.Start(_processStartInfo);
            _cmd.OutputDataReceived += CmdDataReceived;
            _cmd.ErrorDataReceived += CmdDataReceived;
        }

        ~CmdHost()
        {
            Dispose();
        }

        public void Dispose()
        {
            _cmd.Dispose();
        }

        private void CmdDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
        {
            throw new NotImplementedException();
        }
    }
}
