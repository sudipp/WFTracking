// **********************************************************************************************
// <summary>
//  Contains class: WorkFlowTrackRecord
//
//  WorkFlowTrackRecord -  Represents workflow record
//
//  Revision History
//
//  Version Date        Name                     Changes
//  -----------------------------------------------------------------------
//  1       4/5/2013  Sudip P                    Added 'WfHost','ThreadId' property in ToString()
// </summary>
// **********************************************************************************************

using System;
using System.Workflow.Runtime.Tracking;

namespace XMLFileTracking
{
    /// <summary>
    /// Represents workflow record
    /// </summary>
    [Serializable]
    public class WorkFlowTrackRecord : BaseTrackRecord
    {
        private TrackingWorkflowEvent _Status;

        public static string XMLTagName = "WFTR";

        public TrackingWorkflowEvent WfStatus
        {
            set
            {
                _Status = value;
            }
            get
            {
                return _Status;
            }
        }

        public override string ToString()
        {
            return "<" + XMLTagName + " datetime=\"" + DateTime.ToString("MM-dd-yyyy HH:mm:ss")
                    + "\" wfStatus=\"" + WfStatus.ToString()
                    + "\" WfHost=\"" + WfHost
                    + "\" ThreadId=\"" + ThreadId
                    + "\" order=\"" + Order + "\"/>";
        }
    }
}
