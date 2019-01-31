// **********************************************************************************************
// <summary>
//  Contains class: XMLFileTrackingService
//
//  XMLFileTrackingService -  Provides access to the run-time tracking infrastructure.
//  Revision History
//
//  Version Date        Name                     Changes
//  -----------------------------------------------------------------------
//  1       4/5/2013  Sudip P                    added GetMachineName()
//  2       4/10/2013 Sudip P                    added LogWfHostStartupInfo() and called from ctors
// </summary>
// **********************************************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Workflow.Runtime.Tracking;
using System.Workflow.ComponentModel;
using System.Collections.Specialized;

namespace XMLFileTracking
{
	public class XMLFileTrackingService: TrackingService
	{
        string _logLocation;
	    string _wfPersistanceName;
        readonly Regex _regexWfPersistanceName = new Regex(@"(?<=(\bInitial\s*Catalog\s*=))(.*?)(?=\;)", RegexOptions.IgnoreCase);

        public XMLFileTrackingService(NameValueCollection parameters)
        {
            _logLocation = parameters["logLocation"];
            _wfPersistanceName = _regexWfPersistanceName.Match(parameters["wfPersistanceConnectionString"]).ToString();

            LogWfHostStartupInfo();
        }
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="logLocation">tracking file path</param>
        /// <param name="wfPersistanceConnectionString">connection string, where WF persistance happens</param>
        public XMLFileTrackingService(string logLocation, string wfPersistanceConnectionString)
        {
            _logLocation = logLocation;
            _wfPersistanceName = _regexWfPersistanceName.Match(wfPersistanceConnectionString).ToString().Trim();

            LogWfHostStartupInfo();
        }

        protected override TrackingProfile GetProfile(Guid workflowInstanceId)
        {
            //// just get the same profile for each instance
            //TrackingProfile profile = new TrackingProfile();
            //TryGetProfile(null, out profile);
            //return profile;

            // Does not support reloading/instance profiles
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        protected override TrackingProfile GetProfile(Type workflowType, Version profileVersionId)
        {
            return GetDefaultProfile(workflowType);
        }

        protected override TrackingChannel GetTrackingChannel(TrackingParameters parameters)
        {
            return new XMLFileTrackingChannel(parameters, GetMachineName() + "-" + Process.GetCurrentProcess().ProcessName, _logLocation, _wfPersistanceName);
        }

        #region Tracking Profile Creation

        /// <summary>
        /// Loads create a Default profile
        /// </summary>
        /// <returns></returns>
        private static TrackingProfile GetDefaultProfile(Type workflowType)
        {
            TrackingProfile profile = new TrackingProfile();
            profile.Version = new Version(1, 0, 0, 0);

            // all activity events
            ActivityTrackPoint atp = new ActivityTrackPoint();
            List<ActivityExecutionStatus> executionStatusLists = new List<ActivityExecutionStatus>();
            foreach (ActivityExecutionStatus status in Enum.GetValues(typeof(ActivityExecutionStatus)))
            {
                executionStatusLists.Add(status);
            }
            ActivityTrackingLocation activityTrackingLocation = new ActivityTrackingLocation(typeof(Activity), true, executionStatusLists);
            atp.MatchingLocations.Add(activityTrackingLocation);

            
            WorkflowDataTrackingExtract we = new WorkflowDataTrackingExtract("SetupId");
            atp.Extracts.Add(we);

            profile.ActivityTrackPoints.Add(atp);
            
            // all user TrackData events
            UserTrackPoint utp = new UserTrackPoint();

            UserTrackingLocation userTrackingLocation = new UserTrackingLocation();
            userTrackingLocation.ActivityType = typeof(Activity);
            //userTrackingLocation.MatchDerivedActivityTypes = true;
            userTrackingLocation.ArgumentType = typeof(object);
            //userTrackingLocation.MatchDerivedArgumentTypes = true;
            utp.MatchingLocations.Add(userTrackingLocation);

            profile.UserTrackPoints.Add(utp);

            // all workflow events
            WorkflowTrackPoint wftp = new WorkflowTrackPoint();
            foreach (TrackingWorkflowEvent evt in Enum.GetValues(typeof(TrackingWorkflowEvent)))
            {
                wftp.MatchingLocation.Events.Add(evt);
            }
            profile.WorkflowTrackPoints.Add(wftp);

            return profile;
        }

	    // Reads a file containing an XML representation of a Tracking Profile
        //private static TrackingProfile GetProfile()
        //{
        //    FileStream fileStream = null;
        //    try
        //    {
        //        //System.Reflection.Assembly asm=System.Reflection.Assembly.GetAssembly().CodeBase

        //        string trackingProfileFile = Environment.CurrentDirectory + "\\WFTrckingProfile.xml";

        //        Console.WriteLine("trackingProfileFile is {0}", trackingProfileFile);
        //        if (File.Exists(trackingProfileFile))
        //        {
        //            Console.WriteLine("Reading trackingProfile from {0}", trackingProfileFile);
        //            fileStream = File.OpenRead(trackingProfileFile);
        //            if (null == fileStream)
        //            {
        //                Console.WriteLine("fileStream is null");
        //                return null;
        //            }
        //            StreamReader reader = new StreamReader(fileStream);
        //            TrackingProfile profile;
        //            TrackingProfileSerializer trackingProfileSerializer = new TrackingProfileSerializer();
        //            profile = trackingProfileSerializer.Deserialize(reader);

        //            return profile;
        //        }
        //        else
        //        {
        //            Console.WriteLine("trackingProfileFile {0} doesn't exist", trackingProfileFile);
        //            return null;
        //        }
        //    }
        //    catch (TrackingProfileDeserializationException tpex)
        //    {
        //        Console.WriteLine("Encountered a deserialization exception.");
        //        foreach (ValidationEventArgs validationError in tpex.ValidationEventArgs)
        //        {
        //            Console.WriteLine("Exception Message: {0}", validationError.Message);
        //        }
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Encountered an exception. Exception Source: {0}, Exception Message: {1}", ex.Source, ex.Message);
        //        return null;
        //    }
        //    finally
        //    {
        //        if (fileStream != null)
        //            fileStream.Close();
        //    }
        //}

        #endregion


        protected override bool TryGetProfile(Type workflowType, out TrackingProfile profile)
        {
            profile = GetDefaultProfile(workflowType);
            return true;
        }

        protected override bool TryReloadProfile(Type workflowType, Guid workflowInstanceId, out TrackingProfile profile)
        {
            // we don't support dynamic updates of the profile
            profile = null;
            return false;
        }

        private static string GetMachineName()
        {
            string machineName = Process.GetCurrentProcess().MachineName;
            if (machineName.Equals("."))
                return Environment.MachineName;
            return machineName;
        }

        private void LogWfHostStartupInfo()
        {
            XMLFilePersistence persistanceHelper = new XMLFilePersistence(
                Path.Combine(_logLocation, GetMachineName() + ".log"));

            Process currentProcess = Process.GetCurrentProcess();
            persistanceHelper.PersistWorkflowHostLog(currentProcess.ProcessName + " PID=" + currentProcess.Id + " started @ " + currentProcess.StartTime);
        }
	}
}
