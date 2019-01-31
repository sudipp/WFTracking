// **********************************************************************************************
// <summary>
//  Contains class: WorkFlowInfo
//
//  WorkFlowInfo -  Provides access to tracking records
//
//  Revision History
//
//  Version Date        Name                     Changes
//  -----------------------------------------------------------------------
//  1       4/5/2013  Sudip P                    changes made for renaming XMLPersistence to XMLFilePersistence
// </summary>
// **********************************************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Workflow.Runtime;

namespace XMLFileTracking
{
    /// <summary>
    /// Provides access to tracking records
    /// </summary>
    [Serializable]
    public class WorkFlowInfo
    {
        public const String WorkflowInfoStringFormat = "<{0} wfStatus=\"{1}\" WfPersistanceDb=\"{2}\" >";

        public readonly string TrackingFilePath;
        private WorkflowStatus _wfStatus;
        private DateTime _trackingFileLastWriteTime;
        private string _wfPersistanceDB;
        private string _instanceGuid;
        private IList<BaseTrackRecord> _recordsList = new List<BaseTrackRecord>();
        
        public static string XMLTagName = "WorkFlowInfo";
        
        
        public WorkFlowInfo(string trackingFilePath)
        {
            TrackingFilePath = trackingFilePath;
        }

        /// <summary>
        /// Stores when Tracking file was written last
        /// </summary>
        public DateTime TrackingFileLastWriteTime
        {
            get
            {
                return _trackingFileLastWriteTime;
            }
            set
            {
                _trackingFileLastWriteTime = value;
            }
        }
        

        public WorkflowStatus WfStatus
        {
            get
            {
                return _wfStatus;
            }
            set
            {
                _wfStatus = value;
            }
        }
        
        /// <summary>
        /// WF Persistance DB name
        /// </summary>
        public string WfPersistanceDb
        {
            get
            {
                return _wfPersistanceDB;
            }
            set
            {
                _wfPersistanceDB = value;
            }
        }
        
        /// <summary>
        /// Returns all track records (Workflow and Activity)
        /// </summary>
        public IList<BaseTrackRecord> TrackingRecords
        {
            set
            {
                _recordsList = value;
            }
            get
            {
                return _recordsList;
            }
        }

        /// <summary>
        /// Retruns only Workflow track records
        /// </summary>
        public IList<WorkFlowTrackRecord> WorkFlowRecords
        {
            get
            {
                if (TrackingRecords == null) 
                    return new List<WorkFlowTrackRecord>();
                return TrackingRecords.OfType<WorkFlowTrackRecord>().ToList();
            }
        }

        /// <summary>
        /// Retruns only Activity track records
        /// </summary>
        public IList<ActivityTrackRecord> ActivityRecords
        {
            get
            {
                if (TrackingRecords==null) 
                    return new List<ActivityTrackRecord>();
                return TrackingRecords.OfType<ActivityTrackRecord>().ToList();
            }
        }
        
        /// <summary>
        /// Workflow Instance Id
        /// </summary>
        public string InstanceGuid
        {
            get
            {
                return _instanceGuid;
            }
            set
            {
                _instanceGuid = value;
            }
        }

        //Retruns the workflow defition sring from XML 
        public string GetDefinition()
        {
            string FileName = string.Empty;

            try
            {
                string FolderName = Path.GetDirectoryName(TrackingFilePath);
                FileName = Path.GetFileNameWithoutExtension(TrackingFilePath);
                string _wfDefFilePath = Path.Combine(FolderName, FileName + "_def.xml");

                XMLFilePersistence persistance = new XMLFilePersistence(_wfDefFilePath, TrackingFilePath, string.Empty);
                return persistance.GetWorkflowDefinitionString();
            }
            catch
            {
                throw new Exception("Unable to load workflow definition for '" + FileName + "'");
            }
        }
    }
}
