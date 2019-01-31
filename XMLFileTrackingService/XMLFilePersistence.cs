// **********************************************************************************************
// <summary>
//  Contains class: XMLFilePersistence
//
//  XMLFilePersistence -  responsible for saving tracking records into file
//
//  Revision History
//
//  Version Date        Name                     Changes
//  -----------------------------------------------------------------------
//  1       4/5/2013  Sudip P                    Renamed XMLPersistence to XMLFilePersistence
//  2       4/10/2013 Sudip P                    Added new ctor, and PersistWorkflowHostLog()
// </summary>
// **********************************************************************************************

using System;
using System.IO;
using System.Reflection;
using System.Workflow.ComponentModel;
using System.Workflow.Runtime.Tracking;
using System.Xml;

namespace XMLFileTracking
{
    /// <summary>
    /// XML tracking file persistance 
    /// </summary>
    public class XMLFilePersistence:IDisposable
    {
        private string _trackingFilePath = string.Empty;
        private string _workflowDefPath = string.Empty;
        private string _wfPersistanceDB = string.Empty;
        private string _workflowHostLogPath = string.Empty;
        StreamWriter _logFile = null;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="workflowDefPath">path of the workflow Def file</param>
        /// <param name="trackingFilePath">path of the workflow tracking file</param>
        /// <param name="wfPersistanceDB">Workflow persistance DB Name</param>
        public XMLFilePersistence(string workflowDefPath, string trackingFilePath, string wfPersistanceDB)
        {
            _trackingFilePath = trackingFilePath;
            _workflowDefPath = workflowDefPath;
            _wfPersistanceDB = wfPersistanceDB;
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="workflowHostLogPath">path of the host (which is running the WF service) log file</param>
        public XMLFilePersistence(string workflowHostLogPath)
        {
            _workflowHostLogPath = workflowHostLogPath;
        }

        //private byte[] getByteArryFromString(String str)
        //{
        //    return System.Text.Encoding.ASCII.GetBytes(str);
        //}

        //public virtual string GetSpaces(int count)
        //{
        //    return new string('\u0020', count);
        //}

        ///// <summary>
        ///// Build a tree of activity summaries starting with the root activity
        ///// of the workflow instance that this channel represents.
        ///// </summary>
        ///// <returns>
        ///// <see cref="ActivitySummary" /> representing the root activity.
        ///// </returns>


        /// <summary>
        /// Persist host long
        /// </summary>
        /// <param name="logMessage" />logMessage to persist
        public void PersistWorkflowHostLog(string logMessage)
        {
            try
            {
                if (string.IsNullOrEmpty(_workflowHostLogPath))
                    return;

                using (StreamWriter workflowHostLogWriter = File.AppendText(_workflowHostLogPath))
                {
                    workflowHostLogWriter.WriteLine(logMessage);
                }
            }
            catch{}
        }


        /// <summary>
        /// Persist WF definition xml
        /// </summary>
        /// <param name="activityXml" />xml to persist
        public void PersistWfDefinitionXml(string activityXml)
        {
            StreamWriter definitionXmlFileWriter = null;
            try
            {
                if (File.Exists(_workflowDefPath))
                {
                    FileStream fstream = File.OpenWrite(_workflowDefPath);
                    definitionXmlFileWriter = new StreamWriter(fstream);
                }
                else
                {
                    definitionXmlFileWriter = File.CreateText(_workflowDefPath);
                }
                definitionXmlFileWriter.WriteLine(activityXml);
            }
            finally
            {
                if (null != definitionXmlFileWriter)
                {
                    definitionXmlFileWriter.Close();
                    definitionXmlFileWriter.Dispose();
                }
            }
        }

        /// <summary>
        /// Determines if we need to save the definition.
        /// Logic :: compares wf compilation time, with AsmCreateDate on wf def file
        /// if AsmCreateDate !=Wf assembly compilation time, we will load it
        /// </summary>
        /// <returns>true/false</returns>
        public bool IsNewOrUpdatedWfDefinition(Activity rootActivity)
        {
            if (!File.Exists(_workflowDefPath))
                return true;
            else
            {
                XmlDataDocument xmldoc = new XmlDataDocument();
                xmldoc.LoadXml(GetWorkflowDefinitionString());

                XmlNodeList rootActivities = xmldoc.SelectNodes("/Activity");
                if (rootActivities != null)
                {
                    DateTime savedWfCompliteTime = DateTime.Parse(rootActivities[0].Attributes["WfCompliteTime"].Value);
                    return !GetWfCompilationTimestamp(rootActivity)
                        .Equals(savedWfCompliteTime);
                }
                return true;
            }
        }


        /// <summary>     
        /// Retrieves the linker timestamp for wf assembly file (Determines, compilation time of WF assembly
        /// </summary>     
        /// <param name="rootActivity">root activity of the workflow.</param>     
        /// <returns></returns>     
        /// <remarks>http://www.codinghorror.com/blog/2005/04/determining-build-date-the-hard-way.html</remarks>     
        public DateTime GetWfCompilationTimestamp(Activity rootActivity)
        {
            //const int peHeaderOffset = 60;
            //const int linkerTimestampOffset = 8;
            //var b = new byte[2048];
            //FileStream fs = null;
            //try
            //{
            //    string filePath = rootActivity.GetType().Assembly.Location;
            //    fs = new FileStream(filePath, FileMode.Open, System.IO.FileAccess.Read);
            //    fs.Read(b, 0, 2048);
            //}
            //finally
            //{
            //    if (fs != null)
            //        fs.Close();
            //}
            //var dt = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(BitConverter.ToInt32(b, BitConverter.ToInt32(b, peHeaderOffset)
            //    + linkerTimestampOffset));

            //return dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);

            const int peHeaderOffset = 60;
            const int linkerTimestampOffset = 8;
            byte[] b = new byte[2048];
            System.IO.Stream fs = null;
            try
            {
                string filePath = System.Reflection.Assembly.GetCallingAssembly().Location;
                fs = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                fs.Read(b, 0, 2048);
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
            int i = System.BitConverter.ToInt32(b, peHeaderOffset);
            int secondsSince1970 = System.BitConverter.ToInt32(b, i + linkerTimestampOffset);
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
            return dt;
        }

        /// <summary>
        /// returns workflow definition string
        /// </summary>
        /// <returns></returns>
        public string GetWorkflowDefinitionString()
        {
            StreamReader activityXmlFileReader = null;
            try
            {
                activityXmlFileReader = new StreamReader(_workflowDefPath);
                return activityXmlFileReader.ReadToEnd();
            }
            catch
            {
                throw;
            }
            finally
            {
                if (null != activityXmlFileReader)
                {
                    activityXmlFileReader.Close();
                    activityXmlFileReader.Dispose();
                }
            }
        }


        private static void DetermineWorkflowStatus(WorkFlowTrackRecord workflowTrackingRecord, 
            ref string workflowStatus)
        {
            //string workflowStatus = string.Empty;
            switch (workflowTrackingRecord.WfStatus)
            {
                case TrackingWorkflowEvent.Created:
                    workflowStatus = TrackingWorkflowEvent.Created.ToString();
                    break;
                case TrackingWorkflowEvent.Completed:
                    workflowStatus = TrackingWorkflowEvent.Completed.ToString();
                    break;
                case TrackingWorkflowEvent.Suspended:
                    workflowStatus = TrackingWorkflowEvent.Suspended.ToString();
                    break;
                case TrackingWorkflowEvent.Terminated:
                    workflowStatus = TrackingWorkflowEvent.Terminated.ToString();
                    break;
                case TrackingWorkflowEvent.Persisted:
                    //return previous staus in case of persisted
                    break;
                default: //Running
                    workflowStatus = "Running";
                    break;
            }
        }

        private static string BuildWorkflowInfoString(string wfStatus, string wfPersistanceDb)
        {
            //return string.Format(WorkFlowInfo.WorkflowInfoStringFormat, WorkFlowInfo.XMLTagName, wfStatus, allocationId).PadRight(50);
            return string.Format(WorkFlowInfo.WorkflowInfoStringFormat, WorkFlowInfo.XMLTagName, wfStatus, wfPersistanceDb).PadRight(100);
            
        }

        /// <summary>
        /// persist tracking records
        /// </summary>
        /// <param name="items">tracking records</param>
        public void PersistTrackingRecords(System.Collections.ICollection items)
        {
            if (_logFile == null)
            {
                if (File.Exists(_trackingFilePath))
                {
                    //Basanti
                    //FileStream fstream = File.OpenWrite(_trackingFilePath);
                    //Providing  read access for other threads*****
                    FileStream fstream = File.Open(_trackingFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                    
                    fstream.Position = fstream.Length -("</" + WorkFlowInfo.XMLTagName + ">\n\r").Length;

                    _logFile = new StreamWriter(fstream);
                    //logfileExisted = true;
                }
                else
                {
                    _logFile = File.CreateText(_trackingFilePath);

                    _logFile.WriteLine(BuildWorkflowInfoString("Created", _wfPersistanceDB));
                }
            }
            
            string workflowStatus = string.Empty;
            foreach (object itemToCommit in items)
            {
                WorkFlowTrackRecord workflowTrackingRecord = itemToCommit as WorkFlowTrackRecord;
                if (workflowTrackingRecord != null)
                {
                    //Determine Workflow Status
                    DetermineWorkflowStatus(workflowTrackingRecord, ref workflowStatus);

                    _logFile.WriteLine(workflowTrackingRecord.ToString());
                    continue;
                }

                ActivityTrackRecord activityTrackingRecord = itemToCommit as ActivityTrackRecord;
                if (activityTrackingRecord != null)
                {
                    //Reading ErrorCode, ErrorMessage value from AssociatedRequestedTaskStatusInfo object
                    if (activityTrackingRecord.UpdateTaskStatusRequestedTaskStatusInfo != null)
                    {
                        Type underlyingType = activityTrackingRecord.UpdateTaskStatusRequestedTaskStatusInfo.GetType();
                        PropertyInfo pInfo = underlyingType.GetProperty("UserId");
                        if (pInfo != null)
                        {
                            object value = pInfo.GetValue(activityTrackingRecord.UpdateTaskStatusRequestedTaskStatusInfo, null);
                            if (value != null)
                                activityTrackingRecord.User = value.ToString();
                        }
                        
                        pInfo = underlyingType.GetProperty("TaskStatus");
                        if (pInfo != null)
                        {
                            object value = pInfo.GetValue(activityTrackingRecord.UpdateTaskStatusRequestedTaskStatusInfo, null);
                            if (value != null)
                                activityTrackingRecord.ApexStatus = value.ToString().EscapeXML();
                        }

                        pInfo = underlyingType.GetProperty("ErrorCode");
                        if (pInfo != null)
                        {
                            object value = pInfo.GetValue(activityTrackingRecord.UpdateTaskStatusRequestedTaskStatusInfo, null);
                            if (value != null)
                                activityTrackingRecord.ErrorCode = value.ToString().EscapeXML();
                        }
                        pInfo = underlyingType.GetProperty("ErrorMessage");
                        if (pInfo != null)
                        {
                            object value = pInfo.GetValue(activityTrackingRecord.UpdateTaskStatusRequestedTaskStatusInfo, null);
                            if (value != null)
                                activityTrackingRecord.ErrorMessage = value.ToString().EscapeXML();
                        }
                    }


                    //Reading UserId value from BaseActivityTask object
                    if(activityTrackingRecord.BaseActivityTask!=null){
                        Type underlyingType = activityTrackingRecord.BaseActivityTask.GetType();
                        PropertyInfo pInfo = underlyingType.GetProperty("UserId");
                        if (pInfo!=null){
                            object value = pInfo.GetValue(activityTrackingRecord.BaseActivityTask, null);
                            if (value != null)
                                activityTrackingRecord.User = value.ToString();
                        }
                        pInfo = underlyingType.GetProperty("DisplayName");
                        if (pInfo != null)
                        {
                            object value = pInfo.GetValue(activityTrackingRecord.BaseActivityTask, null);
                            if (value != null)
                                activityTrackingRecord.DisplayName = value.ToString().EscapeXML();
                        }

                    //    pInfo = underlyingType.GetProperty("Status");
                    //    if (pInfo != null)
                    //    {
                    //        object value = pInfo.GetValue(activityTrackingRecord.AssociatedtaskDTO, null);
                    //        if (value != null)
                    //            activityTrackingRecord.ApexStatus = value.ToString().EscapeXML();
                    //    }

                    //    pInfo = underlyingType.GetProperty("ErrorCode");
                    //    if (pInfo != null)
                    //    {
                    //        object value = pInfo.GetValue(activityTrackingRecord.AssociatedtaskDTO, null);
                    //        if (value!=null)
                    //            activityTrackingRecord.ErrorCode = value.ToString().EscapeXML();
                    //    }
                    //    pInfo = underlyingType.GetProperty("ErrorMessage");
                    //    if (pInfo != null)
                    //    {
                    //        object value = pInfo.GetValue(activityTrackingRecord.AssociatedtaskDTO, null);
                    //        if (value != null)
                    //            activityTrackingRecord.ErrorMessage = value.ToString().EscapeXML();
                    //    }

                    //    //pInfo = underlyingType.GetProperty("TaskType");
                    //    //if (pInfo != null)
                    //    //{
                    //    //    object value = pInfo.GetValue(activityTrackingRecord.AssociatedtaskDTO, null);
                    //    //    activityTrackingRecord.TaskType = (WorkflowTaskType)Enum.Parse(typeof(WorkflowTaskType), value.ToString());
                    //    //}
                    }

                    _logFile.WriteLine(activityTrackingRecord.ToString());
                    continue;
                }

                UserTrackingRecord userTrackingRecord = itemToCommit as UserTrackingRecord;
                if (userTrackingRecord != null)
                {
                    _logFile.WriteLine("<" + userTrackingRecord.UserDataKey + ":" + userTrackingRecord.UserData + "/>");
                    continue;
                }

            }
            _logFile.WriteLine("</" + WorkFlowInfo.XMLTagName + ">");
            _logFile.Flush();


            //Finally updating, workflow status & AllocationSetup id at the start of the log file
            if (!string.IsNullOrEmpty(workflowStatus))
            {
                _logFile.BaseStream.Seek(0, SeekOrigin.Begin);
                _logFile.WriteLine(BuildWorkflowInfoString(workflowStatus, _wfPersistanceDB));
            }

            //_logFile.BaseStream.Close();
            _logFile.Close();
            _logFile.Dispose();
            _logFile = null;
        }




        ///<summary>
        /// Clean-up the logFile used by the XmlPersistence.
        ///</summary>
        ///<filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ///<summary>
        /// Clean-up the logFile used by the XmlPersistence.
        ///</summary>
        ///<filterpriority>2</filterpriority>
        protected virtual void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                if (_logFile != null)
                {
                    _logFile.Close();
                    _logFile.Dispose();
                    _logFile = null;
                }
            }
        }
    }
}
