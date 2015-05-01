using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

// ReSharper disable InconsistentNaming

namespace NayuTerm.Models
{
    static class RunCommand
    {
        public static event EventHandler Initialized;

        private static readonly string HomeDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
        private static readonly string RCFilePath = Path.Combine(HomeDir, @".nayurc");
        

        public static void Initialize()
        {
            if (!IsExistRCFile())
            {
                Touch();
            }

            ParseRCFile();
        }

        private static bool IsExistRCFile()
        {
            return File.Exists(RCFilePath);
        }

        private static void Touch()
        {
            if (!File.Exists(RCFilePath))
            {
                var res = Application.GetResourceStream(new Uri("default.nayurc", UriKind.Relative));
                var sw = File.CreateText(RCFilePath);
                res.Stream.CopyTo(sw.BaseStream);
                sw.Close();

                res.Stream.Close();
            }
        }

        private static void ParseRCFile()
        {
            var rcLines = File.ReadAllLines(RCFilePath);
            Regex regex;
            Match match;
            foreach (var rcLine in rcLines)
            {
                //MEMO:comment
                if (rcLine.StartsWith("#"))
                {
                    continue;
                }

                regex = new Regex("alias (?<before>[^=]*)=(?<after>.*)");
                match = regex.Match(rcLine);
                if (match.Success)
                {
                    Environment.AliasList.Add(new Alias(match.Groups["before"].ToString(), match.Groups["after"].ToString()));
                }
                Environment.AliasList = Environment.AliasList.OrderByDescending(alias => alias.Before.Length).ToList();

                regex = new Regex("backgroundARGB=(?<value>.*)");
                match = regex.Match(rcLine);
                if (match.Success)
                {
                    Environment.BackgroundColorARGB = match.Groups["value"].ToString();
                }

                regex = new Regex("foregroundARGB=(?<value>.*)");
                match = regex.Match(rcLine);
                if (match.Success)
                {
                    Environment.ForegroundColorARGB = match.Groups["value"].ToString();
                }
            }

            if (Initialized != null)
            {
                Initialized(null, null);
            }
        }

        public static class Environment
        {
            public static List<Alias> AliasList = new List<Alias>();
            public static string BackgroundColorARGB = "#BF000000";
            public static string ForegroundColorARGB = "#FFFFFFFF";
        }
    }
}
