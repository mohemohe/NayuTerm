#region *** copyright ***
/* 
 * Copyright (c) 2012 Hiroaki Goto
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
 * associated documentation files (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial
 * portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
 * NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
 * OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
 * IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
#endregion
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Threading;

namespace StoneDot.BasicLibrary
{
    public class ProcessEventArgs : EventArgs
    {
        public string Command { get; private set; }
        public ProcessEventArgs(string command)
        {
            Command = command;
        }
    }

    public static class ApplicationUtilities
    {
        /// <summary>
        /// ProcessCommand event is called when another instance of this application requests to process the command.
        /// </summary>
        public static event EventHandler<ProcessEventArgs> ProcessCommand = null;

        private static Mutex _mutex;
        private static bool _isFirstInstance;
        private static object _syncObject = new object();

        private static string CurrentGUID
        {
            get
            {
                var asm = Assembly.GetExecutingAssembly();
                var attr = (GuidAttribute)Attribute.GetCustomAttribute(asm, typeof(GuidAttribute));
                return attr.Value;
            }
        }

        private static string PortName
        {
            get { return CurrentGUID; }
        }

        private static string ServiceName
        {
            get { return "ProcessCommand"; }
        }

        private static string IpcUrl
        {
            get { return string.Format("ipc://{0}/{1}", PortName, ServiceName); }
        }

        static ApplicationUtilities()
        {
            _mutex = new Mutex(false, CurrentGUID);
            _isFirstInstance = _mutex.WaitOne(0, false);
            if (_isFirstInstance)
            {
                StartIpcServer();
            }
        }

        private static void StartIpcServer()
        {
            var serverChannel = new IpcServerChannel(PortName);
            ChannelServices.RegisterChannel(serverChannel, true);
            Debug.WriteLine("Listening on {0}", serverChannel.GetChannelUri());
            var delegator = new ProcessDelegator();
            delegator.ProcessCommandInFirstInstance += (sender, e) =>
            {
                if (ProcessCommand != null) ProcessCommand(sender, e);
            };
            RemotingServices.Marshal(delegator, ServiceName, typeof(ProcessDelegator));
        }

        /// <summary>
        /// Check that this application is first instance of this application.
        /// </summary>
        /// <returns><c>true</c> if this application is first instance of this application; otherwise, <c>false</c>.</returns>
        public static bool IsFirstInstance()
        {
            return _isFirstInstance;
        }

        /// <summary>
        /// Delegete processing command to first instance.
        /// </summary>
        public static void DelegeteProcessingCommandToFirstInstance(string command)
        {
            var clientChannel = new IpcClientChannel();
            ChannelServices.RegisterChannel(clientChannel, true);
            var processor = (ProcessDelegator)Activator.GetObject(typeof(ProcessDelegator), IpcUrl);
            processor.ProcessCommand(command);
        }

        /// <summary>
        /// ProcessDelegator class is a proxy object.
        /// </summary>
        private class ProcessDelegator : MarshalByRefObject
        {
            public event EventHandler<ProcessEventArgs> ProcessCommandInFirstInstance;
            public void ProcessCommand(string command)
            {
                if (ProcessCommandInFirstInstance != null)
                    ProcessCommandInFirstInstance(this, new ProcessEventArgs(command));
            }
        }
    }
}
