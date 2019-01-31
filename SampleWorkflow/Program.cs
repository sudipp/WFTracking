#region Using directives

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Workflow.Runtime;
using System.Workflow.Runtime.Hosting;

#endregion

namespace WorkflowProject1
{
    class Program
    {
        static void Main(string[] args)
        {
            using(WorkflowRuntime workflowRuntime = new WorkflowRuntime())
            {
                AutoResetEvent waitHandle = new AutoResetEvent(false);
                workflowRuntime.WorkflowCompleted += delegate(object sender, WorkflowCompletedEventArgs e) {waitHandle.Set();};
                workflowRuntime.WorkflowTerminated += delegate(object sender, WorkflowTerminatedEventArgs e)
                {
                    Console.WriteLine(e.Exception.Message);
                    waitHandle.Set();
                };

                //Attach the Tracking service
                XMLFileTracking.XMLFileTrackingService xmlFts =
                    new XMLFileTracking.XMLFileTrackingService(@"C:\wwflog", string.Empty);
                workflowRuntime.AddService(xmlFts);

                //Start the WF instance
                WorkflowInstance instance = workflowRuntime.CreateWorkflow(typeof(WorkflowProject1.Workflow2));
                instance.Start();

                
                waitHandle.WaitOne();
            }
        }
    }
}
