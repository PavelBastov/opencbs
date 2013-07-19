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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System.Data;
using OpenCBS.CoreDomain;
using OpenCBS.Enums;
using OpenCBS.Shared.Settings;

namespace OpenCBS.Reports 
{
    using IReportsIterator = IEnumerable<Report>;
    using IPartsIterator = IEnumerable<ReportDocument>;
    using ReportList = List<Report>;

    public class ReportService
    {
        private readonly static ReportList Reports = new ReportList();
        private static ReportService _instance;

        public static ReportService GetInstance()
        {
            return _instance ?? (_instance = new ReportService());
        }

        private static ReportManager Manager
        {
            get
            {
                return ReportManager.GetInstance();
            }
        }

        private static string GetReportsDir()
        {
            string dir = TechnicalSettings.ReportPath;
            if (string.IsNullOrEmpty(dir)) dir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(dir, "Reports");
        }

        private static void LoadFromDir(string dir, Flag defaultFlag)
        {
            DirectoryInfo di = new DirectoryInfo(dir);
            List<string> unstarred = Manager.GetUnstarredBulk();
            foreach (FileInfo fi in di.GetFileSystemInfos("*.zip"))
            {
                Report report = new Report(fi.FullName);
                if (!report.IsLoaded) continue;

                report.SetFlag(defaultFlag);
                if (unstarred.Exists(r => r == report.Name))
                    report.Starred = false;
                Reports.Add(report);
            }
        }

        public void LoadReports()
        {
            string dir = GetReportsDir();
            string dirStandard = Path.Combine(dir, "Standard");
            string dirInternal = Path.Combine(dir, "Internal");
            string dirConsolidated = Path.Combine(dir, "Consolidated");
            string dirMain = GetReportsDir();

            if (!Directory.Exists(dirStandard))
                Directory.CreateDirectory(dirStandard);
            if (!Directory.Exists(dirInternal))
                Directory.CreateDirectory(dirInternal);
            if (!Directory.Exists(dirConsolidated))
                Directory.CreateDirectory(dirConsolidated);

            LoadFromDir(dirStandard, Flag.Standard);
            LoadFromDir(dirInternal, Flag.Internal);
            LoadFromDir(dirConsolidated, Flag.Consolidated);
        }

        public ReportList GetReports(string tag, OReportSortOrder sortOrder)
        {
            List<Report> reports = new List<Report>();
            IComparer<Report> comparer = null;
            switch (sortOrder)
            {
                case OReportSortOrder.Alphabet:
                    comparer = new ReportAbcComparer();
                    break;

                case OReportSortOrder.Popularity:
                    comparer = new ReportPopularityComparer();
                    break;
            }
            foreach (Report report in Reports)
            {
                if((report.Flag & Flag.Internal) != 0) continue;
                string[] reportTags = report.Tags;
                if (!reportTags.Contains(tag, StringComparer.CurrentCultureIgnoreCase)) continue;
                reports.Add(report);
            }
            reports.Sort(comparer);
            return reports;
        }

        public string[] GetTags()
        {
            List<string> result = new List<string>();
            
            result.AddRange(new [] {Resource.TagAll, Resource.TagStar});
            foreach (Report report in Reports)
                foreach (string tag in report.Tags)
                    if(!result.Contains(tag, StringComparer.InvariantCultureIgnoreCase))
                        result.Add(tag);

            return result.ToArray();
        }

        public ReportList GetReportsByTag(string _tag, bool exist, Flag _type)
        {
            ReportList re = new ReportList();
            foreach (Report report in Reports)
            {
                if (report.Flag == _type)
                {
                    if (report.Tags.Contains(_tag) == exist)
                    {
                        re.Add(report);
                    }
                }
            }
            return re;
        }

        public ReportList GetInternalReports(AttachmentPoint point, Visibility visibility)
        {
            List<Report> reports = new List<Report>();
            foreach (Report report in Reports)
            {
                if (!report.HasFlag(Flag.Internal)) continue;
                if (report.AttachmentPoint != point) continue;
                bool visible = report.Visibility == visibility;
                visible = visible || Visibility.All == report.Visibility;
                if (!visible) continue;
                reports.Add(report);
            }
            return reports;
        }

        public Report GetReport(Guid guid)
        {
            Report report = Reports.Find(r => r.Guid == guid);
            Debug.Assert(report != null, "Report not found");
            return report;
        }

        public Report GetReport(string guid)
        {
            return GetReport(new Guid(guid));
        }

        public Report GetReportByName(string name)
        {
            return Reports.Find(r => r.Name == name);
        }

        public ReportDocument LoadReport(Report report)
        {
            ReportDocument doc = report.GetDocument();
            // TODO: remove the retarded ManualDatasources flag
            Hashtable datasources = report.ManualDatasources ? report.GetDatasources() : GetDatasources(report);

            bool labelsLoaded = report.LoadLabels();
            foreach (ReportDocument part in report.GetPartIterator())
            {
                // Set up data sources
                foreach (Table table in part.Database.Tables)
                {
                    table.SetDataSource(datasources[table.Name]);
                }

                if (!labelsLoaded) continue;

                // Localize formulae
                foreach (FormulaFieldDefinition ff in part.DataDefinition.FormulaFields)
                {
                    // There is a trick here:
                    // only the formulae whose name starts with 'fn_'
                    // will be localized
                    if (!ff.Name.StartsWith("fn_")) continue;

                    string name = part.Name;
                    name = string.IsNullOrEmpty(name) ? "MAIN_REPORT" : name;

                    string label = report.GetLabel(name, ff.Name);
                    if (string.IsNullOrEmpty(label)) continue;
                    ff.Text = label;
                }

                // Localize labels
                foreach (ReportObject obj in part.ReportDefinition.ReportObjects)
                {
                    if (!(obj is TextObject)) continue;

                    TextObject tobj = obj as TextObject;
                    string name = part.Name;
                    name = string.IsNullOrEmpty(name) ? "MAIN_REPORT" : name;

                    string label = report.GetLabel(name, tobj.Name);
                    if (string.IsNullOrEmpty(label)) continue;
                    tobj.Text = label;
                }
            }

            report.UseCents = true;
            object cur = report.GetParamValueByName("display_in");
            if (cur != null)
            {
                report.UseCents = Manager.GetUseCents(Convert.ToInt32(cur));
            }

            return doc;
        }

        public ParameterFields GetParameterFields(Report report)
        {
            ParameterFields retval = new ParameterFields();
            ReportDocument doc = report.GetDocument();
            foreach (ParameterField pf in doc.ParameterFields)
            {
                object value;

                switch (pf.Name)
                {
                    case "@LANGUAGE":
                    case "LANGUAGE":
                        value = UserSettings.Language;
                        break;

                    case "user_id":
                    case "@user_id":
                    case "USER_ID":
                    case "@USER_ID":
                        value = User.CurrentUser.Id;
                        break;

                    case "@USER_NAME":
                    case "USER_NAME":
                        value = User.CurrentUser.FirstName + " " + User.CurrentUser.LastName;
                        break;

                    case "@BRANCH_NAME":
                    case "BRANCH_NAME":
                        value = "";
                        break;

                    case "@MFI_NAME":
                    case "MFI_NAME":
                        value = "";
                        break;

                    default:
                        value = report.GetParamValueByName(pf.Name);
                        break;
                }

                if (null == value) continue;

                ParameterField newPf = pf;
                newPf.CurrentValues.Clear();
                newPf.CurrentValues.Add(new ParameterDiscreteValue {Value = value});
                retval.Add(newPf);
            }
            return retval;
        }

        private Hashtable GetDatasources(Report report)
        {
            // Make sure that all the utility functions (helpers) this report
            // relies upon are loaded
            Manager.LoadHelpers(report);

            // Get names of all stored procedures
            List<string> names = new List<string>();
            foreach (ReportDocument part in report.GetPartIterator())
            {
                foreach (Table table in part.Database.Tables)
                {
                    string name = table.Name;
                    if (names.Contains(name)) continue;
                    names.Add(name);
                }
            }

            // Execute each stored procedure and return resulting recordsets
            Hashtable ds = new Hashtable();
            foreach (string name in names)
            {
                DataTable dt = Manager.GetDatasource(name, report);
                ds.Add(name, dt);
            }
            return ds;
        }

        public List<KeyValuePair<object, object>> GetQueryResult(string query)
        {
            return Manager.GetQueryResult(query);
        }

        public void SetStarred(string name, bool starred)
        {
            Manager.SetStarred(name, starred);
        }
    }
}
