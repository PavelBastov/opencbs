// Octopus MFS is an integrated suite for managing a Micro Finance Institution: 
// clients, contracts, accounting, reporting and risk
// Copyright © 2006,2007 OCTO Technology & OXUS Development Network
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License along
// with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
//
// Website: http://www.opencbs.com
// Contact: contact@opencbs.com

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;

namespace OpenCBS.Shared.Settings
{
    [Serializable]
    public static class TechnicalSettings
    {
        private static Version _version;

        public const string RegistryPathTemplate = @"Software\Open Octopus Ltd\OpenCBS\{0}";

        private static bool _useOnlineMode;
        private static bool _useDebugMode;
        private static string _remotingServer;
        private static int _remotingServerPort;

        private static readonly Dictionary<string, string> Settings = new Dictionary<string, string>();

        private static Version GetVersion()
        {
            if (_version != null) return _version;

            var assembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return (_version = new Version(fileVersionInfo.FileVersion));
        }

        public static string GetDisplayVersion()
        {
            var version = GetVersion();
            var textVersion = string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
            var attribute =
                (AssemblyGitRevision)
                (Assembly.GetExecutingAssembly().GetCustomAttributes(typeof (AssemblyGitRevision), true).FirstOrDefault());
            if (attribute == null) return textVersion;
            var revision = attribute.Revision;
            revision = revision.Length > 7 ? revision.Substring(0, 7) : revision;
            return textVersion + "." + revision;
        }

        public static string DatabaseName
        {
            get { return GetValue("DATABASE_NAME", "Octopus"); }
            set { SetValue("DATABASE_NAME", value); }
        }

        public static string DatabaseServerName
        {
            get { return GetValue("DATABASE_SERVER_NAME", "localhost"); }
            set { SetValue("DATABASE_SERVER_NAME", value); }
        }

        public static string DatabaseLoginName
        {
            get { return GetValue("DATABASE_LOGIN_NAME", "sa"); }
            set { SetValue("DATABASE_LOGIN_NAME", value); }
        }

        public static string DatabasePassword
        {
            get { return GetValue("DATABASE_PASSWORD", "octopus"); }
            set { SetValue("DATABASE_PASSWORD", value); }
        }

        public static string ReportPath
        {
            get { return GetValue("REPORT_PATH", String.Empty); }
            set { SetValue("REPORT_PATH", value); }
        }

        public static List<string> AvailableDatabases
        {
            get
            {
                var retval = new List<string>();
                string val = GetValue("DATABASE_LIST", string.Empty);
                val = val.Replace(" ", "");
                if (string.IsNullOrEmpty(val)) return retval;

                retval.AddRange(val.Split(','));
                return retval;
            }
        }

        public static void AddAvailableDatabase(string database)
        {
            string val = GetValue("DATABASE_LIST", string.Empty);
            if (string.IsNullOrEmpty(val))
            {
                val = database;
            }
            else
            {
                val += "," + database;
            }
            SetValue("DATABASE_LIST", val);
        }

        public static string CurrentVersion
        {
            get 
            { 
                var version = GetVersion();
                return string.Format("{0}.{1}.0.0", version.Major, version.Minor);
            }
        }

        public static string SoftwareVersion
        {
            get { return "v" + CurrentVersion; }
        }

        public static bool UseOnlineMode
        {
            get { return _useOnlineMode; }
            set { _useOnlineMode = value;}
        }

        public static bool UseDebugMode
        {
            get { return _useDebugMode; }
            set { _useDebugMode = value; }
        }

        public static bool SentQuestionnaire
        {
            get { return Convert.ToBoolean(GetValue("SENT_QUESTIONNAIRE", "True")); }
            set { SetValue("SENT_QUESTIONNAIRE", value ? "True" : "False"); }
        }

        public static string RemotingServer
        {
            get
            {
                return _remotingServer;
            }
            set
            {
                _remotingServer = value;
            }
        }

        public static int RemotingServerPort
        {
            get { return _remotingServerPort; }
            set { _remotingServerPort = value; }
        }

        public static int DatabaseTimeout
        {
            get
            {
                string dbTimeoutStr = GetValue("DATABASE_TIMEOUT", "300");
                int dbTimeout;
                return int.TryParse(dbTimeoutStr, out dbTimeout) ? dbTimeout : 300;
            }
        }

        public static int DebugLevel
        {
            get { return 0; }
        }

        public static bool CheckSettings()
        {
            var values = new[]
            {
                DatabaseLoginName,
                DatabaseName,
                DatabasePassword,
            };
            return values.All(value => !string.IsNullOrEmpty(value));
        }

        private static string GetRegistryPath()
        {
            var version = GetVersion();
            var textVersion = string.Format("{0}.{1}.0.0", version.Major, version.Minor);
            return string.Format(RegistryPathTemplate, textVersion);
        }

        private static RegistryKey OpenRegistryKey()
        {
            var path = GetRegistryPath();
            return Registry.CurrentUser.OpenSubKey(path, true) ?? Registry.LocalMachine.OpenSubKey(path, true);
        }

        private static void SetValue(string key, string value)
        {
            Settings[key] = value;
            using (var reg = OpenRegistryKey())
            {
                if (null == reg) return;
                reg.SetValue(key, value);
            }
        }

        private static string GetValue(string key, string defaultValue)
        {
            if (Settings.ContainsKey(key))
            {
                return Settings[key];
            }

            using (var reg = OpenRegistryKey())
            {
                if (null == reg) return defaultValue;
                string value = reg.GetValue(key, defaultValue).ToString();
                Settings[key] = value;
                return value;
            }
        }
    }
}
