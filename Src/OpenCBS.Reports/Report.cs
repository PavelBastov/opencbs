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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using CrystalDecisions.CrystalReports.Engine;
using ICSharpCode.SharpZipLib.Zip;
using OpenCBS.Shared.Settings;
using System.Collections.Generic;

namespace OpenCBS.Reports
{
    public abstract partial class ReportParamV2
    {
        public string Name { get; set; }
        public string Label { get; set; }
    }

    [Serializable]
    public class ReportPopularityComparer : IComparer<Report>
    {
        public int Compare(Report left, Report right)
        {
            if (left.OpenCount != right.OpenCount) return right.OpenCount - left.OpenCount;
            return left.Title.CompareTo(right.Title);
        }
    }

    public class ReportAbcComparer : IComparer<Report>
    {
        public int Compare(Report left, Report right)
        {
            return left.Title.CompareTo(right.Title);
        }
    }

    [Serializable]
    public enum AttachmentPoint
    {
        None = 0,
        LoanDetails = 1,
        CreditCommittee = 2,
        LoanRepayment = 3,
        SavingsBook = 4,
        Guarantors = 5,
        SavingsDeposit = 6,
        SavingsCompulsory = 7,
        GuaranteeCommittee = 8,
        GuaranteeDetails = 9,
        VillageDetails = 10,
        LoanEvents = 11,
        CorporateDetails = 12,
        GroupDetails = 13,
        PersonDetails = 14
    }

    [Serializable]
    public enum Visibility
    {
        Individual = 1,
        Group = 2,
        Corporate = 3,
        All = 4,
        NonSolidaryGroup = 5
    }

    public enum ReportFilter
    {
        All,
        Starred
    };

    [Flags]
    public enum Flag
    {
        None = 0,
        Standard = 1,
        Internal = 2,
        Consolidated = 4
    }

    [Serializable]
    public class Report
    {
        public readonly string _fileName;
        private string _title;
        private string _description;
        private bool _isLoaded;
        private Guid _guid;
        private List<ReportParamV2> _params;
        private int _order;
        private AttachmentPoint _attachmentPoint = AttachmentPoint.None;
        private Visibility _visibility = Visibility.All;
        private readonly Hashtable _datasources = new Hashtable();
        private bool _manualDatasources;
        private readonly ArrayList _helpers = new ArrayList();
        private readonly string _lang = UserSettings.Language;

        [NonSerialized]
        private XmlDocument _docLabels;

        private Flag _flag = Flag.None;

        private ReportDocument _document;

        public Report(string fileName)
        {
            _fileName = fileName;

            try
            {
                InitializeReport(fileName);
            }
            catch (Exception e)
            {
               Trace.Indent();
               Trace.WriteLine("EXCEPTION: ");
               Trace.WriteLine(e.Message + "\n");
               Debug.WriteLine("Exception while loading report package");
            }
        }

        private void InitializeReport(string fileName)
        {
            Trace.IndentLevel = 1;
            Trace.Indent();
            Trace.WriteLine("Loading " + fileName);
            Trace.Unindent();
            XmlDocument doc = new XmlDocument();
            doc.Load(GetMetaStream());
            XmlElement root = doc.DocumentElement;
            Debug.Assert(root != null, "Root element not found");

            // Get GUID
            XmlNode nodeGuid = root.SelectSingleNode("guid");
            _guid = null == nodeGuid ? Guid.NewGuid() : new Guid(nodeGuid.InnerText);

            // Get title
            XmlNode nodeTitle = root.SelectSingleNode(string.Format("localized[@lang='{0}']/title", _lang));
            nodeTitle = nodeTitle ?? root.SelectSingleNode("title");
            _title = nodeTitle.InnerText;

            // Get description
            XmlNode nodeDesc = root.SelectSingleNode(string.Format("localized[@lang='{0}']/description", _lang));
            nodeDesc = nodeDesc ?? root.SelectSingleNode("description");
            _description = nodeDesc.InnerText;
            
            XmlNode rep = doc.GetElementsByTagName("report")[0];
            XmlAttribute order = rep.Attributes["order"];
            _order = order != null ? int.Parse(order.Value) : 0;
            XmlAttribute ap = rep.Attributes["attachmentPoint"];
            if (ap != null && Enum.IsDefined(typeof (AttachmentPoint), ap.Value))
            {
                _attachmentPoint = (AttachmentPoint) Enum.Parse(typeof (AttachmentPoint), ap.Value, true);
            }
            XmlAttribute vis = rep.Attributes["visibility"];
            if (vis != null && Enum.IsDefined(typeof (Visibility), vis.Value))
            {
                _visibility = (Visibility) Enum.Parse(typeof (Visibility), vis.Value, true);
            }
            //Load parameters
            _params = new List<ReportParamV2>();
            try
            {
                XmlNodeList paramList = root.SelectNodes(string.Format("localized[@lang='{0}']/param", _lang));
                paramList = 0 == paramList.Count ? root.SelectNodes("param") : paramList;
                paramList = 0 == paramList.Count ? root.SelectNodes("params/param") : paramList;
                foreach (XmlNode node in paramList)
                    _params.Add(XmlNodeToParam(node));
            } catch{}


            List<string> tags = new List<string> {Resource.TagAll, Resource.TagStar};
            XmlNode tagsNode = _lang == "en-US"
                                      ? root.SelectSingleNode("tags")
                                      : root.SelectSingleNode(string.Format("localized[@lang='{0}']/tags", _lang));
            if(tagsNode != null)
            {
                XmlNodeList tagNodes = tagsNode.SelectNodes("tag");
                if (tagNodes != null)
                    foreach (XmlNode tagNode in tagNodes)
                    {
                        string tag = tagNode.InnerText;
                        if(!tags.Contains(tag, StringComparer.CurrentCultureIgnoreCase))
                            tags.Add(tag);
                    }
            }
            Tags = tags.ToArray();
            // Get list of utility functions
            foreach (XmlNode node in doc.GetElementsByTagName("helper"))
            {
                XmlAttribute fn = node.Attributes["name"];
                if (null == fn) continue;
                _helpers.Add(fn.Value);
            }
            LoadOpenCount();
            _isLoaded = true;
        }

        private Stream GetMetaStream()
        {
            ZipFile package = new ZipFile(_fileName);
            const string metaDefault = "meta.xml";
            string meta = string.Format(@"meta.{0}.xml", UserSettings.Language);
            ZipEntry entry = package.GetEntry(meta);
            entry = entry ?? package.GetEntry(metaDefault);
            return package.GetInputStream(entry);
        }

        public bool LoadLabels()
        {
            try
            {
                ZipFile package = new ZipFile(_fileName);
                string path = string.Format("labels.{0}.xml", _lang);
                ZipEntry entry = package.GetEntry(path);
                if (null == entry) return false;
                _docLabels = new XmlDocument();
                _docLabels.Load(package.GetInputStream(entry));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GetLabel(string subreport, string name)
        {
            Debug.Assert(_docLabels != null, "Labels not loaded");
            XmlElement root = _docLabels.DocumentElement;
            Debug.Assert(root != null, "Root element not found");
            string path = string.Format("{0}/{1}", subreport, name);
            XmlNode node = root.SelectSingleNode(path);
            return null == node ? string.Empty : node.InnerText;
        }

        public Stream GetReportStream()
        {
            ZipFile package = new ZipFile(_fileName);
            const string entryName = "report.rpt";
            ZipEntry entry = package.GetEntry(entryName);
            Stream stream = package.GetInputStream(entry);
            stream.Flush();
            return stream;
        }

        public Stream GetDatasourceStream(string name)
        {
            ZipFile package = new ZipFile(_fileName);
            string entryName = name + ".sql";
            ZipEntry entry = package.GetEntry(entryName);
            return package.GetInputStream(entry);
        }

        public List<ReportParamV2> Params
        {
            get
            {
                return _params;
            }
        }


        private static ReportParamV2 XmlNodeToParam(XmlNode node)
        {
            ReportParamV2 param;
            XmlAttribute d = node.Attributes["default"];            
            string value = null == d ? string.Empty : d.Value;

            XmlAttribute a;
            switch (node.Attributes["type"].Value)
            {
                case "bool":
                    param = new BoolParam(value);
                    break;

                case "int":
                    param = new IntParam(value);
                    break;

                case "date":
                    param = new DateParam(value);
                    break;

                case "query":
                    var ms = node.Attributes["multiselect"];
                    var multiselect = null != ms && "yes" == ms.Value.ToLower();
                    QueryParam qp = multiselect ? new QueryParamMulti(node.InnerText) : new QueryParam(node.InnerText);
                    a = node.Attributes["hideIfRows"];
                    qp.HideIfrows = null == a ? -1 : int.Parse(a.Value);
                    param = qp;
                    break;

                case "currency":
                    a = node.Attributes["includeAll"];
                    bool includeAll = a != null && "yes" == a.Value;
                    CurrencyParam cp = new CurrencyParam(includeAll);
                    a = node.Attributes["hideIfRows"];
                    cp.HideIfrows = null == a ? -1 : int.Parse(a.Value);
                    param = cp;
                    break;

                case "branch":
                    a = node.Attributes["consoTable"];
                    string consoTable = null == a ? string.Empty : a.Value;
                    param = new BranchParam(consoTable);
                    break;

                default:
                    param = new StringParam(value);
                    break;

            }
            XmlAttribute additionalAttribute = node.Attributes["additional"];
            param.Additional = null != additionalAttribute && bool.Parse(additionalAttribute.Value);

            param.Name = node.Attributes["name"].Value;
            param.Label = node.Attributes["label"].Value;
            return param;
        }

        public object GetParamValueByName(string name)
        {
            name = name.TrimStart(new[] { '@' });
            foreach (ReportParamV2 param in _params)
            {
                if (name == param.Name)
                {
                    return param.Value;
                }

                if (param.Name + "_name" == name)
                {
                    return param.ToString();
                }
            }
            return null;
        }

        public string Title
        {
            get { return _title; }
        }

        public string Description
        {
            get { return _description; }
        }

        public bool IsLoaded
        {
            get { return _isLoaded; }
        }

        public Guid Guid
        {
            get { return _guid; }
        }

        public int Order
        {
            get { return _order; }
        }

        public void AddParam(string name, object value)
        {            
            if (null == _params) _params = new List<ReportParamV2>();

            ReportParamV2 param = GenerateParameter(name, value);
            _params.Add(param);
        }

        private static ReportParamV2 GenerateParameter(string name, object value)
        {
            ReportParamV2 param;
            if (value is bool)
                param = new BoolParam(Convert.ToBoolean(value));
            else if (value is int)
                param = new IntParam(Convert.ToInt32(value));
            else if (value is DateTime)
                param = new DateParam(Convert.ToDateTime(value));
            else
                param = new StringParam(value.ToString());

            param.Name = name;
            return param;
        }

        public void SetParamValue(string name, object value)
        {
            ReportParamV2 param =
                _params.FirstOrDefault(
                    p => p.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)
                    );
            ReportParamV2 newParam = GenerateParameter(name, value);
            if(param != null)
            {
                int paramIndex = _params.IndexOf(param);
                _params[paramIndex] = newParam;
            }
            else _params.Add(newParam);
        }

        public void RemoveParams()
        {
            _params = null;
        }

        public AttachmentPoint AttachmentPoint
        {
            get { return _attachmentPoint; }
        }

        public Visibility Visibility
        {
            get { return _visibility; }
        }

        public void RemoveDatasources()
        {
            _datasources.Clear();
            _manualDatasources = false;
        }

        public void AddDatasource(string name, System.Data.DataSet datasource)
        {
            _datasources.Add(name, datasource);
            _manualDatasources = true;
        }

        public Hashtable GetDatasources()
        {
            return _datasources;
        }

        public bool ManualDatasources
        {
            get { return _manualDatasources; }
        }

        public IEnumerable<string> Helpers
        {
            get
            {
                foreach (string name in _helpers)
                {
                    yield return name;
                }
            }
        }

        private void LoadDocument()
        {
            _document = new ReportDocument();
            lock (_document)
            {
                // Save the .rpt from the package into a temporary file
                string temp = Path.GetTempFileName() + ".rpt";
                Stream input = GetReportStream();
                FileStream output = new FileStream(temp, FileMode.Create);
                const int size = 4096;
                byte[] bytes = new byte[4096];
                int numBytes;
                while ((numBytes = input.Read(bytes, 0, size)) > 0)
                {
                    output.Write(bytes, 0, numBytes);
                }
                output.Close();

                _document.Load(temp);
                File.Delete(temp);
            }
        }

        public ReportDocument GetDocument()
        {
            if (null == _document)
            {
                LoadDocument();
            }
            return _document;
        }

        public string Name
        {
            get
            {
                return Path.GetFileName(_fileName);
            }
        }
        
        public void SetFlag(Flag flag)
        {
            _flag = _flag | flag;
        }

        public void UnsetFlag(Flag flag)
        {
            _flag = _flag ^ flag;
        }

        public bool HasFlag(Flag flag)
        {
            return (_flag & flag) == flag;
        }

        public Flag Flag
        {
            get
            {
                return _flag;
            }
        }

        public bool UseCents { get; set; }

        public IEnumerable<ReportDocument> GetPartIterator()
        {
            ReportDocument doc = GetDocument();
            yield return doc;

            foreach (ReportDocument part in doc.Subreports)
            {
                yield return part;
            }
        }

        public IEnumerable<ReportObject> GetObjectIterator()
        {
            foreach (ReportDocument part in GetPartIterator())
            {
                foreach (ReportObject obj in part.ReportDefinition.ReportObjects)
                {
                    yield return obj;
                }
            }
        }

        public int OpenCount { get; set; }

        public string[] Tags { get; private set; }

        private void LoadOpenCount()
        {
            OpenCount = UserSettings.GetReportOpenCount(Path.GetFileName(_fileName));
        }

        public void SaveOpenCount()
        {
            UserSettings.SetReportOpenCount(Path.GetFileName(_fileName), OpenCount);
        }

        public bool Starred
        {
            get { return Tags.Contains(Resource.TagStar); }
            set
            {
                if (value)
                {
                    if (!Tags.Contains(Resource.TagStar))
                    {

                        List<string> tags = Tags.ToList();
                        tags.Add(Resource.TagStar);
                        Tags = tags.ToArray();
                    }
                }
                else
                    Tags = Tags.Where(t => !t.Equals(Resource.TagStar)).ToArray();
            }
        }
    }
}
