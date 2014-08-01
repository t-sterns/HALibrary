using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HighAvailabilityLib
{
    public class SimpleTestResult
    {
        TestResult.TestType testType;
        public double[] latency, successRate;
        public int[] count;
        public string resultCode, explaination;
        public string[] reccomendation;
        public double averageNoFaultLatency, averageFaultLatency, averageNoFaultSuccessRate, averageFaultSuccessRate, secondsDowntime;
        public Exception invalidFormat;
        public int reccomendationCount;

        public SimpleTestResult(TestResult result)
        {
            testType = result.testType;
            latency = result.GetLatencyArray();
            successRate = result.GetSuccessArray();
            count = result.GetCount();
            string[] temp = result.IsSuccess();
            resultCode = temp[0];
            explaination = temp[1];
            if (!resultCode.Equals("SUCCESS"))
            {
                reccomendation = result.getReccomendation(testType);
                reccomendationCount = reccomendation.Length;
            }
            this.averageFaultLatency = result.averageFaultLatency;
            this.averageFaultSuccessRate = result.averageFaultSuccessRate;
            this.averageNoFaultLatency = result.averageNoFaultLatency;
            this.averageNoFaultSuccessRate = result.averageNoFaultSuccessRate;
            invalidFormat = null;
            secondsDowntime = result.secondsDowntime;

        }

        public SimpleTestResult(Exception ex)
        {
            invalidFormat = ex;

        }
    }
}
