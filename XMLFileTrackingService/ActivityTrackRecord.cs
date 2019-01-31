// **********************************************************************************************
// <summary>
//  Contains class: ActivityTrackRecord
//
//  ActivityTrackRecord -  Represents activity record
//
//  Revision History
//
//  Version Date        Name                     Changes
//  -----------------------------------------------------------------------
//  1       4/5/2013  Sudip P                    Added 'WfHost','ThreadId' property in ToString()
// </summary>
// **********************************************************************************************


using System;
using System.Workflow.ComponentModel;

namespace XMLFileTracking
{
    /// <summary>
    /// Represents activity record
    /// </summary>
    [Serializable]
    public class ActivityTrackRecord : BaseTrackRecord
    {        
        private string _Name;
        private ActivityExecutionStatus _WFStatus;
        private string _Type;
        private string _User;
        private object _baseActivityTask;
        private string _DisplayName = string.Empty;

        private string _ApexStatus=string.Empty;
        private string _ErrorCode = string.Empty;
        private string _ErrorMessage = string.Empty;
        private object _updateTaskStatusRequestedTaskStatusInfo;
        

        public static string XMLTagName = "ATR";


        public string User
        {
            set
            {
                _User = value;
            }
            get
            {
                return _User;
            }
        }

        public string Name
        {
            set
            {
                _Name = value;
            }
            get
            {
                return _Name;
            }
        }
               
        public string Type
        {
            set
            {
                _Type = value;
            }
            get
            {
                return _Type;
            }
        }

        /// <summary>
        /// Microsoft WF status
        /// </summary>
        public ActivityExecutionStatus WfStatus
        {
            set
            {
                _WFStatus = value;
            }
            get
            {
                return _WFStatus;
            }
        }

        /// <summary>
        /// Custom WF status from Apex 
        /// </summary>
        public string ApexStatus
        {
            set
            {
                _ApexStatus = value;
            }
            get
            {
                return _ApexStatus;
            }
        }

        public string ErrorCode
        {
            set
            {
                _ErrorCode = value;
            }
            get
            {
                return _ErrorCode;
            }
        }

        public string ErrorMessage
        {
            set
            {
                _ErrorMessage = value;
            }
            get
            {
                return _ErrorMessage;
            }
        }

        /// <summary>
        /// Holds RequestedTaskStatusInfo property vale of UpdateTaskStatus class
        /// </summary>
        public object UpdateTaskStatusRequestedTaskStatusInfo
        {
            set
            {
                _updateTaskStatusRequestedTaskStatusInfo = value;
            }
            get
            {
                return _updateTaskStatusRequestedTaskStatusInfo;
            }
        }

        /// <summary>
        /// Holds task property vale of BaseActivity class
        /// </summary>
        public object BaseActivityTask
        {
            set
            {
                _baseActivityTask = value;
            }
            get
            {
                return _baseActivityTask;
            }
        }
        
        public string DisplayName
        {
            set
            {
                _DisplayName = value;
            }
            get
            {
                return _DisplayName;
            }
        }

        public override string ToString()
        {
            return "<" + XMLTagName + " datetime=\"" + DateTime.ToString("MM-dd-yyyy HH:mm:ss")
                    + "\" Type=\"" + Type +
                    "\" name=\"" + Name
                    + ((DisplayName.Length>0)? "\" displayname=\"" + DisplayName:string.Empty)
                    + "\" wfStatus=\"" + WfStatus.ToString()

                    + ((ApexStatus.Length > 0) ? "\" ApexStatus=\"" + ApexStatus : string.Empty)
                    + ((ErrorCode.Length > 0) ? "\" ErrorCode=\"" + ErrorCode : string.Empty)
                    + ((ErrorMessage.Length > 0) ? "\" ErrorMessage=\"" + ErrorMessage : string.Empty)

                    + "\" WfHost=\"" + WfHost
                    + "\" ThreadId=\"" + ThreadId

                    + "\" order=\"" + Order
                    + "\" user=\"" + User + "\"/>";
        }
    }
}
