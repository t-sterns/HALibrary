using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HighAvailabilityLib
{
    public class TestResult
    {
        private TimeSpan _total_time;
        private TimeSpan _fault_time;
        private TimeSpan _fault_delay;

        private int _totalRecordCount;
        private int _insideIntervalCount;
        private int _intervalCount;
        public TestResult.TestType testType { get; set; }
        //public double averageLatency { get; set; }
        private IntervalRecord[] _intervalRecords { get; set; }
        
        private DateTime _start;

        public double averageNoFaultLatency, averageFaultLatency, averageNoFaultSuccessRate, averageFaultSuccessRate, secondsDowntime;

        List<RequestRecord> requestList;

        public enum TestType
        {
            PING,
            CPU,
            MEMORY,
            SERVER,
            NETWORK
        }

        public TestResult(TestResult.TestType type, TimeSpan total, TimeSpan fault, TimeSpan delay, DateTime st)
        {
            testType = type;
            _total_time = total;
            _fault_time = fault;
            _fault_delay = delay;
            _intervalRecords = new IntervalRecord[(int)total.TotalSeconds + 10];
            _totalRecordCount = 0;
           // _insideIntervalCount = 0;
            _intervalCount = 0;
            _intervalRecords[_intervalCount] = new IntervalRecord();
            _start = st;
            secondsDowntime = 0;
            requestList = new List<RequestRecord>();

        }

        public void AddRecord(double latency, int success, DateTime current, int idNum)
        {
            var record = new RequestRecord(current, idNum, latency, success);
            requestList.Add(record);
        }

        public void CreateIntervalArray()
        {
            requestList.Sort();
            RequestRecord[] recordArray = requestList.ToArray();
            for (int i = 0; i < recordArray.Length; i++)
            {
                if (recordArray[i].start.CompareTo(_start.AddSeconds(_intervalCount + 1)) < 0)
                {
                    _intervalRecords[_intervalCount].addRecord(recordArray[i].latency, recordArray[i].success);

                }
                else
                {
                    _intervalRecords[++_intervalCount] = new IntervalRecord();
                    _intervalRecords[_intervalCount].addRecord(recordArray[i].latency, recordArray[i].success);
                }
                _totalRecordCount++;

            }
        }

        public string[] IsSuccess()
        {
            if (testType == TestResult.TestType.CPU | testType == TestResult.TestType.MEMORY)
            {
                averageFaultLatency = GetAverageLatencyforPeriod((int)_fault_delay.TotalSeconds, (int)_fault_delay.TotalSeconds + (int)_fault_time.TotalSeconds);
                averageNoFaultLatency = GetAverageLatencyforPeriod(0, (int)_fault_delay.TotalSeconds); //* _fault_delay.TotalSeconds + GetAverageLatencyforPeriod((int)_fault_delay.TotalSeconds + (int)_fault_time.TotalSeconds, (int)_total_time.TotalSeconds) * (_total_time.TotalSeconds - (_fault_delay.TotalSeconds + _fault_time.TotalSeconds))) / (_total_time.TotalSeconds - _fault_time.TotalSeconds);
                double latencyRatio = averageFaultLatency / averageNoFaultLatency;

                averageFaultSuccessRate = GetAverageSuccessRateforPeriod((int)_fault_delay.TotalSeconds, (int)_fault_delay.TotalSeconds + (int)_fault_time.TotalSeconds);
                averageNoFaultSuccessRate =GetAverageSuccessRateforPeriod(0, (int)_fault_delay.TotalSeconds); //* _fault_delay.TotalSeconds + GetAverageSuccessRateforPeriod((int)_fault_delay.TotalSeconds + (int)_fault_time.TotalSeconds, (int)_total_time.TotalSeconds) * (_total_time.TotalSeconds - (_fault_delay.TotalSeconds + _fault_time.TotalSeconds))) / (_total_time.TotalSeconds - _fault_time.TotalSeconds);
                double successRateRatio = averageFaultSuccessRate / averageNoFaultSuccessRate;

                if (successRateRatio < .3 && latencyRatio > 10)
                {
                    return new string[] {"FAILURE", "This represents a total failure, as almost all of the requests failed. " };
                }
                else if (latencyRatio > 10)
                {
                    return new string[] {"WARNING", "Most of the HTTP requests were able to complete during the fault, however the average latency was very high." };
                }
                else if (successRateRatio < .8 || latencyRatio > 3)
                {
                    return new string[] {"WARNING", "Most of the HTTP requests were able to complete during the fault, however the average latency was high." };
                }
                else
                {
                    return new string[] {"SUCCESS", "The HTTP request success rate and latency were close enough to expected values to constitute a success." + 
                    "The test results show that the application tested is able to maintain function even with a fault injected." };
                }
            }
            else if (testType == TestResult.TestType.SERVER)
            {
                //secondsDowntime = getDowntime();
                if (secondsDowntime > 5)
                {
                    return new string[] { "FAILURE", "You're site was unreachable for more than five seconds. This is considered a failure and represents what would occur if the server on which your site was hosted was to crash. "};
                }
                else if (secondsDowntime > 1)
                {
                    return new string[] {"WARNING", "The results show that your site was down for a few seconds. The downtime was not too severe, though in the event of a server crash it would affect any users currently using your site."};
                }
                else
                {
                    return new string[] {"SUCCESS", "Success! Your site experienced virtually no downtime. This is very important if you care a lot about a consistent error free experience for your users."};
                }
            }
            else
            {
                return new string[] { "FAILURE" };
            }
            

        }

        public string[] getReccomendation(TestResult.TestType type)
        {
            return new string[] { "You can scale up you site easily using Azure Web Sites to. Simply go to the scale tab in the azure portal and easily deploy your web site to multiple instances." + 
            " This can help ensure that even with high traffic your site is still highly available. <a href='http://azure.microsoft.com/en-us/documentation/articles/web-sites-scale/'>Learn How Here.</a>" , "Using a traffic manager is another way to increase high availability on a global scale." + 
            "You can deploy your site to multiple azure web sites across different regions while using a single url and traffic manager will do the rest. More information can be found" + 
            " <a href='http://azure.microsoft.com/en-us/documentation/services/traffic-manager/'> here</a>"};
        }

        private double getDowntime()
        {
           // var downtimeControl = true; //this ensures that only initial downtime is measured
            var downtimeSeconds = 0.0;
            var latency = GetLatencyArray();
            var highest = 0;
            for (int i = 0; i < latency.Length; i++)
            {
                 
            }
            return downtimeSeconds;
        }

        private double GetAverageLatencyforPeriod(int start, int finish)
        {
            if(finish > start){
                double avg=0;
                double[] latency = GetLatencyArray();
                int i;
                if (finish > latency.Length)
                {
                    finish = latency.Length;
                }
                for(i = start; i< finish ; i++){
                    avg+= latency[i];
                }
                return avg/(i-start);
            }
            return 0;
        }

        private double GetAverageSuccessRateforPeriod(int start, int finish)
        {
            if(finish > start){
                double avg=0;
                double[] successRate = GetSuccessArray();
                int i;
                if (finish > successRate.Length)
                {
                    finish = successRate.Length;
                }
                for(i = start; i< finish ; i++){
                    avg+= successRate[i];
                }
                return avg/(i-start);
            }
            return 0;
        }

        public double[] GetLatencyArray()
        {
            var highest = 0.0;
            double[] array = new double[_intervalCount];
            for (int i = 0; i < _intervalCount; i++)
            {
                array[i] = _intervalRecords[i].averageLatency;
                if (_intervalRecords[i].averageLatency > highest && i>=_fault_delay.TotalSeconds-1)
                {
                    highest = _intervalRecords[i].averageLatency;
                }
            }
            secondsDowntime = highest/1000;
            return array;
        }

        public double[] GetSuccessArray()
        {
            double[] array = new double[_intervalCount];
            for (int i = 0; i < _intervalCount; i++)
            {
                array[i] = _intervalRecords[i].getPercentSuccess() * 100;
            }
            return array;
        }

        public int[] GetCount()
        {
            int[] array = new int[_intervalCount];
            for (int i = 0; i < _intervalCount; i++)
            {
                array[i] = _intervalRecords[i].count;
            }
            return array;
        }

        //each second is one interval record
        private class IntervalRecord
        {
            public double averageLatency;
            private int _successCount;
            public int count;

            public double getPercentSuccess()
            {
                if (count == 0)
                {
                    return 0;
                }
               // Trace.WriteLine("_successCount=" + _successCount + "   count=" + count);
                return (double)_successCount / (double)count;
            }

            public IntervalRecord()
            {
                count = 0;
                _successCount = 0;
                averageLatency = 0;
            }

            public void addRecord(double latency, int success)
            {
                _successCount += success;
                averageLatency = averageLatency * count / (count + 1) + latency / (count + 1);
                count++;
                //Trace.WriteLine("adding record to interval success = " + _successCount + " and count = " + count);
            }
        }

        private class RequestRecord: IComparable<RequestRecord>
        {
            public DateTime start;
            public int id;
            public double latency;
            public int success;

            public RequestRecord(DateTime s, int i, double l, int suc){
                start = s;
                id = i;
                latency = l;
                success = suc;
            }

            public int CompareTo(RequestRecord r)
            {
                if (r == null)
                {
                    return 1;
                }
                return this.id.CompareTo(r.id);
            }
        }


        
    }

  
    
}
