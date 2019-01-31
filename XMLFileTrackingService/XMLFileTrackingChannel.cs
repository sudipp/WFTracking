// **********************************************************************************************
// <summary>
//  Contains class: XMLFileTrackingChannel
//
//  XMLFileTrackingChannel -  tracking channel which intereacts with the persisstance
//
//  Revision History
//
//  Version Date        Name                     Changes
//  -----------------------------------------------------------------------
//  1       4/5/2013  Sudip P                    Changes made to accommodate 'WfHost','ThreadId' properties, added ConvertToLocalTime()
// </summary>
// **********************************************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Workflow.ComponentModel;
using System.Workflow.Runtime.Tracking;
using System.IO;
using System.Workflow.Runtime;
using System.Xml;

namespace XMLFileTracking
{
    class XMLFileTrackingChannel : TrackingChannel, IPendingWork
	{
        private TrackingParameters _parameters;
        string _logLocation;
        string _wfPersistanceDb;
        string _workflowID;
        private static string _wfHost;
        private XMLFilePersistence _persistanceHelper = null;
        

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="logPath">tracking file path </param>
        /// <param name="wfHost">Workflow service host name</param>
        /// <param name="wfPersistanceDb">connection string, where WF persistance happens</param>
        public XMLFileTrackingChannel(TrackingParameters parameters, string wfHost, string logPath, string wfPersistanceDb)
        {
            _parameters = parameters;
            _wfPersistanceDb = wfPersistanceDb;
            _workflowID = parameters.InstanceId.ToString();
            _logLocation = logPath;
            _wfHost = wfHost;

            if (!Directory.Exists(_logLocation))
            {
                Directory.CreateDirectory(_logLocation);
            }
            
            //Start- Save Wf definition
            if (_persistanceHelper == null)
            {
                _persistanceHelper = new XMLFilePersistence(
                    Path.Combine(_logLocation, _workflowID + "_def.xml"),
                    Path.Combine(_logLocation, _workflowID + ".xml"),
                    _wfPersistanceDb
                    );
                
                //We will save Wf definition, if wf is new or it has been updated
                if(_persistanceHelper.IsNewOrUpdatedWfDefinition(_parameters.RootActivity))
                {
                    DateTime wfCompilationTimestamp=
                        _persistanceHelper.GetWfCompilationTimestamp(_parameters.RootActivity);

                    ActivitySummary rootActivitySummary = BuildRootActivitySummary();
                    String activityXml = BuildActivityXml(rootActivitySummary, wfCompilationTimestamp);
                    _persistanceHelper.PersistWfDefinitionXml(activityXml);
                }
            }
            //End- Save Wf definition
        }
        
        protected override void InstanceCompletedOrTerminated()
        {
            return;
        }

        protected override void Send(TrackingRecord record)
        {
            //Process.GetCurrentProcess().MachineName
            //System.Threading.Thread.CurrentThread.ManagedThreadId

            ActivityTrackingRecord activityTrackingRecord = record as ActivityTrackingRecord;
            if (activityTrackingRecord !=null)
            {
                WorkflowEnvironment.WorkBatch.Add(this,
                    BuildSerializableActivityTrackRecord(activityTrackingRecord));
                return;
            }
            WorkflowTrackingRecord workflowTrackingRecord = record as WorkflowTrackingRecord;
            if (workflowTrackingRecord !=null)
            {
                WorkflowEnvironment.WorkBatch.Add(this,
                    BuildSerializableWorkFlowTrackRecord(workflowTrackingRecord));
                return;
            }


            UserTrackingRecord userTrackingRecord = record as UserTrackingRecord;
            //TrackingRecord userTrackingRecord = record as UserTrackingRecord;
            if (record is UserTrackingRecord)
            {
                WorkflowEnvironment.WorkBatch.Add(this,record);

                //Debug.WriteLine(((UserTrackingRecord)record).UserData);
            }
        }

        private static WorkFlowTrackRecord BuildSerializableWorkFlowTrackRecord(WorkflowTrackingRecord record)
        {
            WorkFlowTrackRecord wtr=new WorkFlowTrackRecord();
            
            //utc date conversion to local 
            wtr.DateTime = ConvertToLocalTime(record.EventDateTime); //record.EventDateTime.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Hours);

            wtr.WfHost = _wfHost;
            wtr.ThreadId = Thread.CurrentThread.ManagedThreadId;

            wtr.Order = record.EventOrder;
            wtr.WfStatus = record.TrackingWorkflowEvent;

            return wtr;
        }

        private static ActivityTrackRecord BuildSerializableActivityTrackRecord(ActivityTrackingRecord record)
        {
            ActivityTrackRecord wtr = new ActivityTrackRecord();

            //utc date conversion to local 
            wtr.DateTime = ConvertToLocalTime(record.EventDateTime);//.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Hours);

            wtr.WfHost = _wfHost;
            wtr.ThreadId = Thread.CurrentThread.ManagedThreadId;


            wtr.Order = record.EventOrder;
            wtr.WfStatus = record.ExecutionStatus;
            wtr.Name = record.QualifiedName;
            wtr.Type = record.ActivityType.Name;

            if (record.Body.Count > 0)
            { 
                var dataItem = GetTrackingDataItemByFieldName(record.Body, "Task");
                if (dataItem != null)
                    wtr.BaseActivityTask = dataItem.Data;

                dataItem = GetTrackingDataItemByFieldName(record.Body, "RequestedTaskStatusInfo");
                if (dataItem != null)
                    wtr.UpdateTaskStatusRequestedTaskStatusInfo = dataItem.Data;
            }

            return wtr;
        }

        private static TrackingDataItem GetTrackingDataItemByFieldName(IList<TrackingDataItem> body, string FieldName)
        {
            foreach (var tDataItem in body)
            {
                if (tDataItem.FieldName.Equals(FieldName))
                {
                    return tDataItem;
                }
            }
            return null;
        }

        #region IPendingWork Members

        public void Commit(System.Transactions.Transaction transaction, System.Collections.ICollection items)
        {
            if (_persistanceHelper==null)
            {
                //_persistanceHelper = new XmlPersistence(Path.Combine(_logLocation, _workflowID + ".xml"));
                _persistanceHelper = new XMLFilePersistence(
                    Path.Combine(_logLocation, _workflowID + "_def.xml"),
                    Path.Combine(_logLocation, _workflowID + ".xml"),
                    _wfPersistanceDb
                    );
            }
            _persistanceHelper.PersistTrackingRecords(items);
        }

        public void Complete(bool succeeded, System.Collections.ICollection items)
        {
            if (_persistanceHelper!=null)
            {
                _persistanceHelper.Dispose();
                _persistanceHelper = null;
            }
        }

        public bool MustCommit(System.Collections.ICollection items)
        {
            return true;
        }

        #endregion

        /// <summary>
        /// convert instance='trackingEventDateTime' to local time zone
        /// </summary>
        /// <param name="trackingEventDateTime"></param>
        /// <returns>date/time converted to local time zone</returns>
        private static DateTime ConvertToLocalTime(DateTime trackingEventDateTime)
        {
            //trackingEventDateTime.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Hours);
            return trackingEventDateTime.ToLocalTime();
        }

        /// <summary>
        /// Build a tree of activity summaries starting with the root activity
        /// of the workflow instance that this channel represents.
        /// </summary>
        /// <returns>
        /// <see cref="ActivitySummary" /> representing the root activity.
        /// </returns>
        private ActivitySummary BuildRootActivitySummary()
        {
            ActivitySummary rootActivitySummary = null;

            Dictionary<String, ActivitySummary> activitySummariesByQualifiedName = new Dictionary<String, ActivitySummary>();
            Queue<Activity> activityQueue = new Queue<Activity>();
            activityQueue.Enqueue(_parameters.RootActivity);

            while (activityQueue.Count > 0)
            {
                Activity currentActivity = activityQueue.Dequeue();
                ActivitySummary currentActivitySummary = new ActivitySummary(
                    currentActivity.GetType(), currentActivity.QualifiedName);

                if (currentActivity.Parent != null && activitySummariesByQualifiedName.ContainsKey(currentActivity.Parent.QualifiedName))
                {
                    ActivitySummary parentActivitySummary = activitySummariesByQualifiedName[currentActivity.Parent.QualifiedName];
                    currentActivitySummary.ParentActivity = parentActivitySummary;
                    parentActivitySummary.ChildActivities.Add(currentActivitySummary);
                }

                if (rootActivitySummary == null)
                    rootActivitySummary = currentActivitySummary;

                CompositeActivity parentActivity = currentActivity as CompositeActivity;

                if (parentActivity != null)
                {
                    activitySummariesByQualifiedName.Add(currentActivitySummary.QualifiedName, currentActivitySummary);
                    foreach (Activity childActivity in parentActivity.Activities)
                    {
                        if (childActivity.Enabled)
                            activityQueue.Enqueue(childActivity);
                    }
                }
            }

            return rootActivitySummary;
        }

        /// <summary>
        /// Build an XML document containing the activities associated with a 
        /// workflow instance.
        /// </summary>
        /// <param name="rootActivity">
        /// <see cref="ActivitySummary" /> representing the root activity of the
        /// workflow instance.
        /// </param>
        /// <param name="wfCompilationTimestamp">
        /// date/time when the WF definartion was last compiled
        /// </param>
        /// <returns>
        /// A String representing the XML document generated from the root activity.
        /// </returns>
        private static string BuildActivityXml(ActivitySummary rootActivity,
            DateTime wfCompilationTimestamp)
        {
            StringBuilder stringBuilder = new StringBuilder();
            using (XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, CreateXmlWriterSettings()))
            {
                xmlWriter.WriteStartDocument();
                //xmlWriter.WriteStartElement("Activities");

                TraverseActivityTree(rootActivity, xmlWriter, wfCompilationTimestamp);

                //xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
            }
            return  stringBuilder.ToString();
        }

        private static void TraverseActivityTree(ActivitySummary rootActivity, 
            XmlWriter xmlWriter,
            DateTime? wfCompilationTimestamp)
        {
            xmlWriter.WriteStartElement("Activity");
            xmlWriter.WriteAttributeString("Type", rootActivity.Type.Name);
            xmlWriter.WriteAttributeString("QualifiedName", rootActivity.QualifiedName);
            
            if (wfCompilationTimestamp != null && wfCompilationTimestamp.HasValue)
                xmlWriter.WriteAttributeString("WfCompliteTime", wfCompilationTimestamp.Value.ToString("MM-dd-yyyy hh:mm:ss tt")); //AM/PM

            foreach (ActivitySummary activity in rootActivity.ChildActivities)
            {
                TraverseActivityTree(activity, xmlWriter, null);
            }
            xmlWriter.WriteEndElement();
        }
        

        private static XmlWriterSettings CreateXmlWriterSettings()
        {
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Indent = true;
            xmlWriterSettings.IndentChars = "\t";
            xmlWriterSettings.OmitXmlDeclaration = true;
            xmlWriterSettings.CloseOutput = true;

            return xmlWriterSettings;
        }


        /*
        private ActivitySummary buildRootActivitySummary_new(Activity rootactivity, 
            ActivitySummary parentActivitySummary,
            Dictionary<String, ActivitySummary> activitySummariesByQualifiedName)
        {
            CompositeActivity prntActivity = rootactivity as CompositeActivity;
            if (prntActivity != null)
            {
                foreach (Activity activity in prntActivity.Activities)
                {
                    ActivitySummary tempParentActivitySummary = parentActivitySummary;

                    if (activity is SequenceActivity)
                    {
                        Activity seqParentAct = GetParentOfSequentialActivity(prntActivity.Activities, activity);
                        
                        //if(!rootactivity.Equals(seqParentAct))
                        //{
                        tempParentActivitySummary = new ActivitySummary(seqParentAct.GetType(), seqParentAct.QualifiedName);
                        //}
                    }
                    else
                    {
                        Activity seqParentAct = activity.Parent;
                        tempParentActivitySummary = new ActivitySummary(seqParentAct.GetType(), seqParentAct.QualifiedName);
                    }

                    ActivitySummary activitySummary = new ActivitySummary(activity.GetType(), activity.QualifiedName);
                    activitySummary.ParentActivity = tempParentActivitySummary;
                    tempParentActivitySummary.ChildActivities.Add(activitySummary);

                    parentActivitySummary.ChildActivities.Add(activitySummary);

                    buildRootActivitySummary_new(activity, tempParentActivitySummary, activitySummariesByQualifiedName);
                }
            }

            return parentActivitySummary;
        }
        */

        /*
        /// <summary>
        /// Build a tree of activity summaries starting with the root activity
        /// of the workflow instance that this channel represents.
        /// </summary>
        /// <returns>
        /// <see cref="ActivitySummary" /> representing the root activity.
        /// </returns>
        private ActivitySummary buildRootActivitySummary_OLD()
        {
            //Start with root activity - workflow root

            //if curentActiviity != CompositeActiviity  (Code,Delay,Send,Terminate,Suspend)
            //      ->Create ActivitySummary with its Parent's ActivitySummary
            //else curentActiviity == CompositeActiviity
            //{
            //      *** curentActiviity.getType().BaseType

            //      if(curentActiviity==ParallelActivity)
            //          -> curentActiviity' all immediate child acticites will have same parent (as curentActiviity)
            //      if(curentActiviity==SequenceActivity)
            //          -> curentActiviity' first child will have parent (as curentActiviity)
            //          -> curentActiviity' (2nd first child) will have parent (as 1st first child)
            //          -> same process till last child
            //      if(curentActiviity==IfElseActivity)
            //          -> curentActiviity' all immediate child acticites will have same parent (as curentActiviity)
            //      if(curentActiviity==WhileActivity)
            //          -> curentActiviity' all immediate child acticites will have same parent (as curentActiviity)
            //}

            ActivitySummary rootActivitySummary = null;

            Dictionary<String, ActivitySummary> activitySummariesByQualifiedName = new Dictionary<String, ActivitySummary>();
            Queue<Activity> activityQueue = new Queue<Activity>();
            activityQueue.Enqueue(_parameters.RootActivity);

            while (activityQueue.Count > 0)
            {
                Activity currentActivity = activityQueue.Dequeue();

                //ActivitySummary currentActivitySummary = new ActivitySummary(
                //    currentActivity.GetType(), currentActivity.QualifiedName);

                //if (currentActivity.Parent != null && activitySummariesByQualifiedName.ContainsKey(currentActivity.Parent.QualifiedName))
                //{
                //    ActivitySummary parentActivitySummary = activitySummariesByQualifiedName[currentActivity.Parent.QualifiedName];
                //    currentActivitySummary.ParentActivity = parentActivitySummary;
                //    parentActivitySummary.ChildActivities.Add(currentActivitySummary);
                //}

                ActivitySummary currentActivitySummary = null;
                if (activitySummariesByQualifiedName.ContainsKey(currentActivity.QualifiedName))
                {
                    currentActivitySummary = activitySummariesByQualifiedName[currentActivity.QualifiedName];
                }
                else
                {
                    currentActivitySummary = new ActivitySummary(currentActivity.GetType(), currentActivity.QualifiedName);
                }
                

                Activity parentByFlow = null;
                if(currentActivity.Parent!=null)
                {
                    parentByFlow = GetParentOfSequentialActivity(currentActivity.Parent.Activities, currentActivity);    
                }
                
                if (parentByFlow != null && activitySummariesByQualifiedName.ContainsKey(parentByFlow.QualifiedName))
                {
                    ActivitySummary parentActivitySummary = activitySummariesByQualifiedName[parentByFlow.QualifiedName];
                    currentActivitySummary.ParentActivity = parentActivitySummary;
                    parentActivitySummary.ChildActivities.Add(currentActivitySummary);
                }

                if (rootActivitySummary == null)
                    rootActivitySummary = currentActivitySummary;


                CompositeActivity parentActivity = currentActivity as CompositeActivity;

                if (parentActivity != null)
                {
                    //activitySummariesByQualifiedName.Add(currentActivitySummary.QualifiedName, currentActivitySummary);
                    foreach (Activity childActivity in parentActivity.Activities)
                    {
                        //**********************
                        Activity parentAct=GetParentOfSequentialActivity(parentActivity.Activities, childActivity);
                        //**********************

                        if (!activitySummariesByQualifiedName.ContainsKey(parentAct.QualifiedName))
                        {
                            ActivitySummary parentActivitySummary = new ActivitySummary(parentAct.GetType(), parentAct.QualifiedName);
                            activitySummariesByQualifiedName.Add(parentActivitySummary.QualifiedName, parentActivitySummary);
                        }

                        if (childActivity.Enabled)
                            activityQueue.Enqueue(childActivity);
                    }
                }
            }
           

            return rootActivitySummary;
        }
         */

        /*
        private static Activity GetParentOfSequentialActivity(ActivityCollection activityCollection, Activity activity)
        {
            int ActivityIndex = activityCollection.IndexOf(activity);

            if (ActivityIndex < 1) //for first child, we will return parent.
            {
                return activity.Parent;
            }
            else
            {
                //Previous activity as parent
                Activity childActivity = activityCollection[ActivityIndex-1];
                return childActivity;
            }
        }
        */

        /*

        /// <summary>
        /// Build an XML document containing the activities associated with a 
        /// workflow instance.
        /// </summary>
        /// <param name="rootActivity">
        /// <see cref="ActivitySummary" /> representing the root activity of the
        /// workflow instance.
        /// </param>
        /// <returns>
        /// A String representing the XML document generated from the root activity.
        /// </returns>
        private static String buildActivityXml_OLD(ActivitySummary rootActivity)
        {
            StringBuilder stringBuilder = new StringBuilder();
            using (XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, CreateXmlWriterSettings()))
            {
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("Activities");

                Queue<ActivitySummary> activityQueue = new Queue<ActivitySummary>();
                activityQueue.Enqueue(rootActivity);
                while (activityQueue.Count > 0)
                {
                    ActivitySummary currentActivity = activityQueue.Dequeue();
                    xmlWriter.WriteStartElement("Activity");
                    xmlWriter.WriteAttributeString("Type", currentActivity.Type.Name);
                    //xmlWriter.WriteElementString("TypeFullName", currentActivity.Type.FullName);
                    
                    //xmlWriter.WriteElementString("AssemblyFullName", currentActivity.Type.Assembly.FullName);
                    xmlWriter.WriteElementString("QualifiedName", currentActivity.QualifiedName);
                    

                    if (currentActivity.ParentActivity != null)
                        xmlWriter.WriteElementString("ParentQualifiedName", currentActivity.ParentActivity.QualifiedName);

                    xmlWriter.WriteEndElement();

                    foreach (ActivitySummary childActivity in currentActivity.ChildActivities)
                        activityQueue.Enqueue(childActivity);
                }

                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
            }

            return stringBuilder.ToString();
        }
        */
    }
}
