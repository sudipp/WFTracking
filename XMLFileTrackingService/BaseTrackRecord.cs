// **********************************************************************************************
// <summary>
//  Contains class: BaseTrackRecord
//
//  BaseTrackRecord -  Base class for all types of tracking records
//
//  Revision History
//
//  Version Date        Name                     Changes
//  -----------------------------------------------------------------------
//  1       4/5/2013  Sudip P                    Added 'WfHost','ThreadId' property 
// </summary>
// **********************************************************************************************

using System;
using System.Reflection;
using System.Text.RegularExpressions;


namespace XMLFileTracking
{
    /// <summary>
    /// Base class for all activity records
    /// </summary>
    public class BaseTrackRecord
    {
        private DateTime _DateTime;
        private int _Order;
        private string _wfSvcHost;
        private int _threadId;

        /// <summary>
        /// Name of server which host the WF service
        /// </summary>
        public string WfHost
        {
            set
            {
                _wfSvcHost = value;
            }
            get
            {
                return _wfSvcHost;
            }
        }

        public DateTime DateTime
        {
            set
            {
                _DateTime = value;
            }
            get
            {
                return _DateTime;
            }
        }

        public int Order
        {
            set
            {
                _Order = value;
            }
            get
            {
                return _Order;
            }
        }

        public int ThreadId
        {
            set
            {
                _threadId = value;
            }
            get
            {
                return _threadId;
            }
        }

        /// <summary>
        /// Create record object from serialized record string
        /// </summary>
        /// <param name="recordString">serialized record string</param>
        /// <param name="recordfType">record type to create [WorkFlowTrackRecord or ActivityTrackRecord]</param>
        /// <returns></returns>
        public static BaseTrackRecord CreateObjectFromString(string recordString, Type recordfType)
        {
            BaseTrackRecord newRecord = (BaseTrackRecord)Activator.CreateInstance(recordfType);

            PropertyInfo[] pInfoArr = newRecord.GetType().GetProperties(BindingFlags.IgnoreCase 
                | BindingFlags.Instance | BindingFlags.Public);
            foreach (var propInfo in pInfoArr)
            {
                Regex rx = new Regex(@"" + propInfo.Name + @"(\s*)=(\s*)""([^""]*)""", 
                    RegexOptions.IgnoreCase);

                Match m = rx.Match(recordString);
                if (m.Success && m.Groups.Count>0)
                {
                    string val = m.Groups[m.Groups.Count - 1].ToString().Trim();
                    //get prop type
                    var targetType = TConverter.GetPropType(propInfo);
                    //Convert value to property type
                    object convertedValue = TConverter.ChangeType(targetType, val);
                    //set object value
                    propInfo.SetValue(newRecord, convertedValue, null);
                }
            }
            return newRecord;
        }
    }
}
