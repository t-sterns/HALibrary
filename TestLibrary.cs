using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HighAvailabilityLib
{
    public class TestLibrary
    {


        public TestLibrary()
        {
            Trace.TraceInformation("New Test Library");

        }
        public void RunCPUTest(TimeSpan duration)
        {
            int COUNT = 10; //How many times to call burn()
            Task[] tasks = new Task[COUNT];
            for (int i = 0; i < COUNT; i++)
            {
                tasks[i] = Task.Factory.StartNew(Burn, duration);
                Trace.TraceInformation("HALib::TestLibrary::RunCPUTest(): start test " + i);
            }
            Task.WaitAll(tasks);
        }

        private void Burn(object obj)
        {
            TimeSpan duration = (TimeSpan) obj;

            int number = 1000000;
            Trace.TraceInformation("HALib::TestLibrary::Burn(): Start running for " + duration.TotalSeconds+ "seconds");
            DateTime dt = DateTime.Now;
            DateTime dtplus = dt.AddSeconds(duration.TotalSeconds);
            var count = 0;
            do
            {
                number *= number;
                count++;
                if (count % 10000000 == 0)
                {
                    DateTime n = DateTime.Now;

                    if (n.CompareTo(dtplus) > 0)
                    {
                        Trace.TraceInformation("CPU Burn Complete");
                        break;
                    }

                }
            } while (true);

        }

        public void MemoryTest(TimeSpan duration)
        {
            int processCount = 70;
            Trace.TraceInformation("HALib::TestLibrary::MemoryTest(): Starting Memory Test. Process Count: " + processCount);
            Task[] tasks = new Task[processCount];

            for (int i = 0; i < processCount; i++)
            {
                tasks[i] = Task.Factory.StartNew(_RunMemoryProcess, duration);
                Trace.TraceInformation("HALib::TestLibrary::MemoryTest(): start test " + i);
            }
            Task.WaitAll(tasks);
            Trace.WriteLine("HALib::TestLibrary::MemoryTest(): FINISHED WITH ALL TASKS");
        }

        private void _RunMemoryProcess(object obj)
        {
            TimeSpan duration = (TimeSpan)obj;
            Process proc = new Process();

            string appPath = AppDomain.CurrentDomain.BaseDirectory.ToString();
            proc.StartInfo.FileName = "memory.exe";
            proc.StartInfo.WorkingDirectory = appPath;
            proc.StartInfo.Arguments = duration.TotalSeconds.ToString();

            try
            {
                proc.Start();
                //Trace.TraceInformation("HALib::TestLibrary::MemoryTest(): Procedure " + i + " Started");
            }
            catch (Exception ex)
            {
                Trace.TraceError("HALib::TestLibrary::MemoryTest: Procedure Start Unsuccessful: " + ex.ToString());
            }
            proc.WaitForExit();
        }

        
        public void KillW3wp(TimeSpan duration)
        {
            try
            {
                Process current = Process.GetCurrentProcess();
                Trace.WriteLine(current.Id);
                 Process [] w3wp = Process.GetProcessesByName("w3wp");
                 if (w3wp.Length == 1)
                 {
                     w3wp[0].Kill();
                 }
                 else if (w3wp.Length > 1)
                 {
                     for (int i = 0; i < w3wp.Length; i++)
                     {
                         Trace.TraceInformation("INSIDE KILLW3WP (" + w3wp[i].Id + "): " + w3wp[0].ToString());
                         if (w3wp[i].Id != current.Id)
                         {
                             w3wp[i].Kill();
                         }
                     }
                 }
                


            }
            catch (Exception ex)
            {

                Trace.TraceInformation("Lib::TestLibrary::killw3wp: EXCEPTION: " + ex.ToString());

            }
        }
        
        
        

    }
}
