using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkHook
{
    /// <summary>
    /// Provides an interface for communicating from the client (target) to the server (injector)
    /// </summary>
    public class ServerInterface : MarshalByRefObject
    {
        DarkReignInterface intf;

        public void IsInstalled(int clientPID)
        {
            Console.WriteLine("DarkReignBootstrap has injected DarkHook into process {0}.\r\n", clientPID);
            intf  = new DarkReignInterface(clientPID);
        }

        /// <summary>
        /// Output messages to the console.
        /// </summary>
        /// <param name="clientPID"></param>
        /// <param name="fileNames"></param>
        public void ReportMessages(int clientPID, string[] messages)
        {
            for (int i = 0; i < messages.Length; i++)
            {
                Console.WriteLine(messages[i]);
            }
        }

        public void ReportScreenshots(int clientPID, string[] screenshots)
        {
            string scenariondir = intf.ScenarioDir;
            scenariondir = scenariondir.Replace(@"\", "_").TrimStart('.').Trim('_');

            int index = 0;
            for (int i = 0; i < screenshots.Length; i++)
            {
                string filename = null;
                do
                {
                    filename = $"{scenariondir}_{DateTime.Now.ToString("yyyy-MM-dd-HH_mm_ss")}_{index}.pcx";
                    index++;
                } while (System.IO.File.Exists(filename));
                System.IO.File.Move(screenshots[i], filename);
            }
        }

        public void ReportMessage(int clientPID, string message)
        {
            Console.WriteLine(message);
        }

        /// <summary>
        /// Report exception
        /// </summary>
        /// <param name="e"></param>
        public void ReportException(Exception e)
        {
            Console.WriteLine("The target process has reported an error:\r\n" + e.ToString());
        }

        /// <summary>
        /// Called to confirm that the IPC channel is still open / host application has not closed
        /// </summary>
        public void Ping()
        {
        }
    }
}
