using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace HighAvailabilityLib
{
    public class MeasurementManager
    {
        public MeasurementManager()
        {

        }

        public SimpleTestResult GetTestResult(string file, TimeSpan total, TimeSpan fault, TimeSpan delay, TimeSpan initdelay)
        {
            try
            {
                TextReader reader = null;
                do
                {
                    try
                    {
                        reader = new StreamReader(file, true);
                        break;
                    }
                    catch
                    {
                        Thread.Sleep(10);
                    }
                }
                while (true);
   
                string line = reader.ReadLine();
                string[] splitLine = line.Split(':');
                if (splitLine.Length < 2)
                {
                    Trace.TraceError("HALib::LoadManager::HttpRequestLoad(): Invalid Format in first line of file");
                    throw new InvalidFormatException("InvalidFileException: file submitted to test is not a valid file");
                }
                TestResult.TestType type = (TestResult.TestType)Enum.Parse(typeof(TestResult.TestType), splitLine[1], true);


                //Get the duration
                line = reader.ReadLine();
                splitLine = line.Split(':');
                if (splitLine.Length < 2)
                {
                    Trace.TraceError("HALib::LoadManager::HttpRequestLoad(): Duration Format Incorrect");
                    throw new InvalidFormatException("InvalidFormatException: file submitted to test is not a valid file");
                }
                TimeSpan duration = new TimeSpan(0, 0, int.Parse(splitLine[1]));
                Trace.TraceInformation("duration read");

                //Get the Number of Sites
                line = reader.ReadLine();
                splitLine = line.Split(':');
                if (splitLine.Length < 2)
                {
                    Trace.TraceError("HALib::LoadManager::HttpRequestLoad(): Number of Sites format incorrect");
                    throw new InvalidFormatException("InvalidFormatException: file submitted to test is not a valid file");
                }
                int siteCount = int.Parse(splitLine[1]);
                Trace.TraceInformation("HALib::LoadManager::HttpRequestLoad():number of sites read");

                //Create SiteTestResult objects for each web site to test
                string[] sites = new string[siteCount];
                for (int i = 0; i < siteCount; i++)
                {
                    line = reader.ReadLine();
                    splitLine = line.Split('|');
                    if (splitLine.Length < 3)
                    {
                        Trace.TraceError("HALib::LoadManager::HttpRequestLoad(): url format invalid");
                        throw new InvalidFormatException("InvalidFormatException: file submitted to test is not a valid file");
                    }
                    sites[i] = splitLine[2];
                }

                //Read in time of test start
                line = reader.ReadLine();
                DateTime start = DateTime.Parse(line);
                start = start.AddSeconds(initdelay.TotalSeconds);

                //Create result object
                TestResult result = new TestResult(type, total, fault, delay, start);
                Trace.TraceInformation("HALib::LoadManager::HttpRequestLoad(): result object created");


                //read in and add records until reach 'COMPLETED'
                do
                {
                    try
                    {
                        line = reader.ReadLine();
                        splitLine = line.Split('|');
                        if (splitLine.Length < 1)
                        {
                            throw new InvalidFormatException("InvalidFormatException: file submitted to test is not a valid file");
                        }
                        else if (splitLine[0].Equals("COMPLETED"))
                        {
                            break;
                        }
                        else if (splitLine.Length !=5)
                        {
                            throw new InvalidFormatException("InvalidFormatException: Error in load recording. Will ignore and Continue");
                        }
                        result.AddRecord(double.Parse(splitLine[3]), int.Parse(splitLine[4]), DateTime.Parse(splitLine[0]), int.Parse(splitLine[2]));
                    }
                    catch (InvalidFormatException ex)
                    {
                        Trace.WriteLine(ex.ToString());
                    }
                    
                } while (true);
                result.CreateIntervalArray();
                reader.Close();

                return new SimpleTestResult(result);
            }
            catch (InvalidFormatException ex)
            {
                return new SimpleTestResult(ex);
            }
            
            
        }


    }

    public class InvalidFormatException : System.Exception
    {
        public InvalidFormatException()
        {
        }

        public InvalidFormatException(string message)
            : base(message)
        {
        }
    }
}
