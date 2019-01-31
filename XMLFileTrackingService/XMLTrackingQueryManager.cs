// **********************************************************************************************
// <summary>
//  Contains class: XMLTrackingQueryManager
//
//  XMLTrackingQueryManager -  Provides acess to workflow tracking files/records
//                             This class should be used by Tracking client
//  Revision History
//
//  Version Date        Name                     Changes
//  -----------------------------------------------------------------------
//  1       4/5/2013  Sudip P                    Changed namespace to 'XMLFileTracking'
//  2       4/10/2013 Sudip P                    removed some comments
// </summary>
// **********************************************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text.RegularExpressions;
using System.Workflow.Runtime;
using System.Xml;

namespace XMLFileTracking
{
    public class XMLTrackingQueryManager
	{
        private string _xmlTrackingFilePath;

        private static void ConvertXmlNodeAttribValueToObjProp(XmlAttribute att, ref BaseTrackRecord trackRecord)
        {
            PropertyInfo propInfo = trackRecord.GetType().GetProperty(att.Name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);

            if(propInfo!=null){
                //get prop type
                var targetType = TConverter.GetPropType(propInfo);
                //Convert value to property type
                object convertedValue = TConverter.ChangeType(targetType, att.Value);
                //set object value
                propInfo.SetValue(trackRecord, convertedValue, null);
            }
        }

        /// <summary>
        /// get workflow info details for a workflow
        /// </summary>
        /// <param name="xmlTrackingFilePath">path from where to load workflow info details</param>
        /// <returns></returns>
        public WorkFlowInfo GetWorkflow(string xmlTrackingFilePath)
        {

            WorkFlowInfo wInfo = null;
            XmlDocument xDoc = new XmlDocument();

            using(FileStream fs = File.OpenRead(xmlTrackingFilePath))
            {
                xDoc.Load(fs);
            }
            

            XmlNodeList wfRecords = xDoc.GetElementsByTagName(WorkFlowInfo.XMLTagName, string.Empty);
            if (wfRecords.Count > 0)
            {
                _xmlTrackingFilePath = xmlTrackingFilePath;
                wInfo = new WorkFlowInfo(_xmlTrackingFilePath);
                wInfo.WfStatus = (WorkflowStatus)Enum.Parse(typeof(WorkflowStatus), wfRecords[0].Attributes["wfStatus"].Value);
                //wInfo.SetupId = wfRecords[0].Attributes["SetupId"].Value;
                wInfo.InstanceGuid = Path.GetFileNameWithoutExtension(xmlTrackingFilePath);
                wInfo.WfPersistanceDb = (wfRecords[0].Attributes["WfPersistanceDb"] != null) ? wfRecords[0].Attributes["WfPersistanceDb"].Value : string.Empty;
                wInfo.TrackingFileLastWriteTime = File.GetLastWriteTime(xmlTrackingFilePath); 

                GetTrackingData(wfRecords[0], ref wInfo);
            }
            
            xDoc = null;

            return wInfo;
        }


        private static void GetTrackingData(XmlNode wfRecord, ref WorkFlowInfo wInfo)
        {
            foreach (XmlNode xmlNode in wfRecord.ChildNodes)
            {
                if (xmlNode.Name == WorkFlowTrackRecord.XMLTagName)
                {
                    BaseTrackRecord wFTrackRecord = new WorkFlowTrackRecord();

                    foreach (XmlAttribute att in xmlNode.Attributes)
                    {
                        ConvertXmlNodeAttribValueToObjProp(att, ref wFTrackRecord);
                    }
                    //trackingData.WorkFlowRecords.Add((WorkFlowTrackRecord)wFTrackRecord);
                    wInfo.TrackingRecords.Add(wFTrackRecord);
                }
                else if (xmlNode.Name == ActivityTrackRecord.XMLTagName)
                {
                    BaseTrackRecord actTrackRecord = new ActivityTrackRecord();

                    foreach (XmlAttribute att in xmlNode.Attributes)
                    {
                        ConvertXmlNodeAttribValueToObjProp(att, ref actTrackRecord);
                    }
                    //trackingData.ActivityRecords.Add((ActivityTrackRecord)actTrackRecord);
                    wInfo.TrackingRecords.Add(actTrackRecord);
                }
            }
        }
        
        /// <summary>
        /// returns list of Tracking files under trackingFolderPath
        /// </summary>
        /// <param name="trackingFolderPath">tracking folder path</param>
        /// <returns></returns>
        public IList<FileInfo> GetTrackingFiles(string trackingFolderPath)
        {
            IList<FileInfo> trackingFiles=new List<FileInfo>();

            DirectoryInfo darr = new DirectoryInfo(trackingFolderPath);
            if(darr!=null)
            {
                FileInfo[] xmlFiles = darr.GetFiles("*.xml");
                if (xmlFiles.Count() > 0)
                {
                    Regex regx =
                        new Regex(@"[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}.xml");
                    trackingFiles = xmlFiles.Where(fName => regx.IsMatch(fName.Name)).ToList();
                }
            }
            return trackingFiles;
        }

        /// <summary>
        /// Build record objects from record strings
        /// </summary>
        /// <param name="recordStrings">record strings</param>
        /// <returns></returns>
        public IList<BaseTrackRecord> GetTrackRecordsFromRecordString(string[] recordStrings)
        {
            IList<BaseTrackRecord> trackRecords=new List<BaseTrackRecord>();
            foreach (var recordString in recordStrings)
            {
                BaseTrackRecord record = null;
                if(recordString.StartsWith("<" + WorkFlowTrackRecord.XMLTagName))
                    record = BaseTrackRecord.CreateObjectFromString(recordString, typeof(WorkFlowTrackRecord));
                else if(recordString.StartsWith("<" + ActivityTrackRecord.XMLTagName))
                    record = BaseTrackRecord.CreateObjectFromString(recordString, typeof(ActivityTrackRecord));

                if (record!=null)
                    trackRecords.Add(record);
            }
            return trackRecords;
        }
	}

    ///<summary>
    /// Type extension 
    ///</summary>
    internal static class TypeExtensions
    {
        ///<summary>
        /// Determines whether the type is nullable
        ///</summary>
        ///<param name="type"></param>
        ///<returns></returns>
        public static bool IsNullableType(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>));
        }
    }

    /// <summary>
    /// Generic type converter helper
    /// </summary>
    internal static class TConverter
    {
        public static T ChangeType<T>(object value)
        {
            return (T)ChangeType(typeof(T), value);
        }

        public static object ChangeType(Type t, object value)
        {
            TypeConverter tc = TypeDescriptor.GetConverter(t); return tc.ConvertFrom(value);
        }
        public static void RegisterTypeConverter<T, TC>() where TC : TypeConverter
        {
            TypeDescriptor.AddAttributes(typeof(T), new TypeConverterAttribute(typeof(TC)));
        }

        public static Type GetPropType(PropertyInfo info)
        {
            return info.PropertyType.IsNullableType()
                 ? Nullable.GetUnderlyingType(info.PropertyType)
                 : info.PropertyType;
        }
    }


    public static class StringExtensions
    {
        public static string EscapeXML(this string s)
        {
            //if (string.IsNullOrEmpty(s) && (!s.Contains("&"))) 
            //    return s;
            return SecurityElement.Escape(s);
            return !SecurityElement.IsValidText(s) 
                    ? SecurityElement.Escape(s) : s;
        }

        public static string UnescapeXML(this string s) 
        {
            if (string.IsNullOrEmpty(s)) return s;
                string returnString = s;
                returnString = returnString.Replace("&apos;", "'");
                returnString = returnString.Replace("&quot;", "\"");
                returnString = returnString.Replace("&gt;", ">");
                returnString = returnString.Replace("&lt;", "<");
                returnString = returnString.Replace("&amp;", "&");
                return returnString;
        }
    }
}
