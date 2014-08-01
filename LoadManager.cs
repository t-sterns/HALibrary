using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Net.Cache;
using System.Text;
using System.Threading.Tasks;

namespace HighAvailabilityLib
{
    public class LoadManager
    {
        public LoadManager()
        {

        }

        //will normalize string parameter to start with http:// if it already starts with http or https
        private static string NormalizeRequestUrl(string requestUrl)
        {
            return requestUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || requestUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? requestUrl : "http://" + requestUrl;
        }
        
        //Main Http Load. Will run load that is split between each site in sites for a certain duration and record results after a specifit initial delay
        public string HttpRequestLoad(string[] sites, TimeSpan duration,TimeSpan initialDelay, TestResult.TestType type)
        {
            var sitename = "";
            if (sites.Length > 0)
            {
                for(int i = 0; i < sites.Length; i ++)
                {
                    sites[i] = NormalizeRequestUrl(sites[i]);
                }
                sitename = getSiteName(sites[0]);
            }

            var appPath = AppDomain.CurrentDomain.BaseDirectory.ToString();
            string[] path = { appPath, sitename + "_HttpRequestLoad_"+ type.ToString()+ ".txt" };
            var file = Path.Combine( path );

            Trace.TraceInformation("HALib::LoadManager::HttpRequestLoad: Writing to logfile: " + file);
            var targetSiteCount = 0;
            using (var logfile = new StreamWriter(file, false))
            {
                logfile.WriteLine("TESTTYPE:" + type);
                logfile.WriteLine("DURATION:" + duration.TotalSeconds);
                logfile.WriteLine("SITECOUNT:" + sites.Length);

                //Log type of test
                Trace.TraceInformation("HALib::LoadManager::HttpRequestLoad: Request Load started for type : " + type.ToString());

                //Log a list of targeted sites
                
                foreach (var site in sites)
                {
                    logfile.WriteLine("SITE|" + (targetSiteCount++) + "|" + site);
                }

                //log the date and time of the start of load
                logfile.WriteLine(DateTime.Now.ToString());

            }
            var dt = DateTime.Now;
            var dtplus = dt.AddSeconds(duration.TotalSeconds + initialDelay.TotalSeconds); //represents the time that the load will finish
            var delaytime = dt.AddSeconds(initialDelay.TotalSeconds); //represents the time that the load will start recording
            //HttpWebRequest request;
            var siteNum = 0;
            var count = 0;
            do
            {
                InitiateWebRequest(sites[0], file, delaytime, count);
                /*
                //start timer
                var timer = new Stopwatch();
                timer.Start();


                request = (HttpWebRequest)HttpWebRequest.Create(sites[siteNum]);
                request.Timeout = 1000;
                var noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
                request.CachePolicy = noCachePolicy;

                //value to log indicating success of HTTP request. If 1 then success if 0 then failure
                var success = "0";
                try
                {
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        success = "1";
                        using (var responseStreamReader = new StreamReader(response.GetResponseStream()))
                        {                               
                            var responseText = responseStreamReader.ReadToEnd();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceInformation(ex.ToString());
                }

                timer.Stop();

                //Calculate Latency
                var milliseconds = (timer.ElapsedTicks * 1000.0) / Stopwatch.Frequency;

                //Log latency and success 
                if (DateTime.Now.CompareTo(delaytime) > 0)
                {
                    logfile.WriteLine(DateTime.Now.ToString() + "|" + sites[siteNum] + "|" + Array.IndexOf(sites, request.RequestUri.ToString()) + "|" + milliseconds + "|" + success);
                }
*/
                //Check if load complete
                if (DateTime.Now.CompareTo(dtplus) > 0)
                {
                    Trace.TraceInformation("HALib::LoadManager::HttpRequestLoad(): Load Finished ");
                    do
                    {
                        try
                        {
                            using (StreamWriter log = new StreamWriter(file, true))
                            {
                                log.WriteLine("COMPLETED|" + type);
                                break;
                            }
                        }
                        catch
                        {
                            Thread.Sleep(10);
                        }
                    }
                    while (true);
                    break;
                }

                //Logic for alternating sites if more than one provided
                siteNum++;
                if (siteNum == targetSiteCount)
                    siteNum = 0;

                //Sleep 10ms between requests
                Thread.Sleep(50);
                count++;
            } while (true);   
            
            //return name of logfile
            return file;
        }

        private void InitiateWebRequest(string uri , string logfile, DateTime delayTime, int id)
        {

            var request = HttpWebRequest.Create(uri);
            request.Timeout = 1000;
            var noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            request.CachePolicy = noCachePolicy;
            var reqState = new RequestState(request, id);
            reqState.timer.Start();
            var webTask = Task.Factory.FromAsync<WebResponse>(
              request.BeginGetResponse, request.EndGetResponse, reqState)
              .ContinueWith(
                task =>
                {
                    var success = 0;
                    var response = (HttpWebResponse)task.Result;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        success = 1;
                        reqState.ResponseStream = response.GetResponseStream();
                        using (var responseStreamReader = new StreamReader(reqState.ResponseStream))
                        {
                            var responseText = responseStreamReader.ReadToEnd();
                        } 
                    }
                    else
                    {
                        success = 0;
                    }
                    
                   // TimeSpan milliseconds = DateTime.Now - reqState.start;
                    reqState.timer.Stop();
                    var milliseconds = (reqState.timer.ElapsedTicks * 1000.0) / Stopwatch.Frequency;
                    if (milliseconds > 2000)
                    {
                        success = 0;
                    }
                    
                    if (DateTime.Now.CompareTo(delayTime) > 0)
                    {
                        do
                        {
                            try
                            {
                                using (StreamWriter log = new StreamWriter(logfile, true))
                                {
                                    log.WriteLine(reqState.start.ToString() + "|" + uri + "|" + reqState.id + "|" + milliseconds + "|" + success);
                                    break;
                                }
                            }
                            catch
                            {
                                Thread.Sleep(10);
                            }
                        } 
                        while (true);
                        
                    }
                    
                });
        }

        /*
        public static void ClientGetAsync(string url)
        {
            var request = WebRequest.Create(url);
            request.Timeout= 1000;
            var noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            request.CachePolicy = noCachePolicy;
            var reqState = new RequestState(request);
            Trace.WriteLine("starting request");
            IAsyncResult r = (IAsyncResult)request.BeginGetResponse(new AsyncCallback(RespCallback), reqState);

        }

        public static void RespCallback(IAsyncResult ar)
        {
            Trace.WriteLine("recieving callback");
            var end = DateTime.Now;
            var rs = (RequestState)ar.AsyncState;
            var request = rs.Request;
            Trace.WriteLine("Latency:" + latency.TotalMilliseconds + "\tsuccess:" + ar.IsCompleted);

            
        }*/

        public string getSiteName(string name)
        {
            return (name.Substring(name.IndexOf(':') + 3).Split('.'))[0];
        }

        private class RequestState
        {
            public int id;
            public WebRequest Request;
            public Stream ResponseStream;
            public DateTime start;
            //public Decoder StreamDecode = Encoding.UTF8.GetDecoder();
            public Stopwatch timer;


            public RequestState(WebRequest req, int id)
            {
                this.id = id;
                Request = req;
                //ResponseStream = null;
                timer = new Stopwatch();
                start = DateTime.Now;
            }
        }
    }

    

}
