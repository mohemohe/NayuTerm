using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
// ReSharper disable InconsistentNaming

namespace NayuTerm.Models
{
    static class RunCommand
    {
        private static readonly string HomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private static readonly string RCFilePath = Path.Combine(HomeDir, @".nayurc");
        public static List<Alias> AliasList = new List<Alias>(); 

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
                var sw = File.CreateText(RCFilePath);
                sw.WriteLine("# NayuTerm config file");
                sw.WriteLine("");
                sw.WriteLine("alias cd=cd /d");
                sw.WriteLine("alias ls=dir");
                sw.WriteLine("alias ls -l=dir /a /q");
                sw.WriteLine("alias pwd=cd");
                sw.Close();
            }
        }

        private static void ParseRCFile()
        {
            var rcLines = File.ReadAllLines(RCFilePath);
            foreach (var rcLine in rcLines)
            {
                if (rcLine.StartsWith("#"))
                {
                    continue;
                }

                var regex = new Regex("alias (?<before>[^=]*)=(?<after>.*)");
                var match = regex.Match(rcLine);
                if (match.Success)
                {
                    AliasList.Add(new Alias(match.Groups["before"].ToString(), match.Groups["after"].ToString()));
                }
            }

            AliasList = AliasList.OrderByDescending(alias => alias.Before.Length).ToList();
        }
    }
}
