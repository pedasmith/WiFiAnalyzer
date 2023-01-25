using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SpeedTests
{

#if SPEEDTEST_SERVER_LIST
https://speedtest-api.samknows.com/targets?target_set=stackpath-us

[{"hostname":"sp1-vm-seattle-us.samknows.com",
"location":"Seattle, WA, US",
"latitude":"47.620499",
"longitude":"-122.350876",
"distance":17.82},
{"hostname":"sp1-vm-sanjose-us.samknows.com","location":"San Jose, CA, US","latitude":"37.339390","longitude":"-121.894960","distance":1149.11},{"hostname":"sp1-vm-losangeles-us.samknows.com","location":"Los Angeles, CA, US","latitude":"34.054935","longitude":"-118.244476","distance":1548.42},{"hostname":"sp1-vm-denver-us.samknows.com","location":"Denver, CO, US","latitude":"39.739154","longitude":"-104.984703","distance":1630.47},{"hostname":"sp1-vm-dallas-us.samknows.com","location":"Dallas, TX, US","latitude":"32.762041","longitude":"-96.779007","distance":2695.59},{"hostname":"sp1-vm-chicago-us.samknows.com","location":"Chicago, IL, US","latitude":"41.875555","longitude":"-87.624421","distance":2773.97},{"hostname":"sp2-vm-chicago-us.samknows.com","location":"Chicago, IL, US","latitude":"41.875555","longitude":"-87.624421","distance":2773.97},{"hostname":"sp1-vm-atlanta-us.samknows.com","location":"Atlanta, GA, US","latitude":"33.749099","longitude":"-84.390185","distance":3491.96},{"hostname":"sp1-vm-ashburn-us.samknows.com","location":"Ashburn, VA, US","latitude":"39.030643","longitude":"-77.469985","distance":3681.28},{"hostname":"sp2-vm-ashburn-us.samknows.com","location":"Ashburn, VA, US","latitude":"39.030643","longitude":"-77.469985","distance":3681.28}]
#endif
    class SamKnowsServers
    {
        public static string ExampleJson = @"[{""hostname"":""sp1-vm-seattle-us.samknows.com"",""location"":""Seattle, WA, US"",""latitude"":""47.620499"",""longitude"":""-122.350876"",""distance"":17.82},{""hostname"":""sp1-vm-sanjose-us.samknows.com"",""location"":""San Jose, CA, US"",""latitude"":""37.339390"",""longitude"":""-121.894960"",""distance"":1149.11},{""hostname"":""sp1-vm-losangeles-us.samknows.com"",""location"":""Los Angeles, CA, US"",""latitude"":""34.054935"",""longitude"":""-118.244476"",""distance"":1548.42},{""hostname"":""sp1-vm-denver-us.samknows.com"",""location"":""Denver, CO, US"",""latitude"":""39.739154"",""longitude"":""-104.984703"",""distance"":1630.47},{""hostname"":""sp1-vm-dallas-us.samknows.com"",""location"":""Dallas, TX, US"",""latitude"":""32.762041"",""longitude"":""-96.779007"",""distance"":2695.59},{""hostname"":""sp1-vm-chicago-us.samknows.com"",""location"":""Chicago, IL, US"",""latitude"":""41.875555"",""longitude"":""-87.624421"",""distance"":2773.97},{""hostname"":""sp2-vm-chicago-us.samknows.com"",""location"":""Chicago, IL, US"",""latitude"":""41.875555"",""longitude"":""-87.624421"",""distance"":2773.97},{""hostname"":""sp1-vm-atlanta-us.samknows.com"",""location"":""Atlanta, GA, US"",""latitude"":""33.749099"",""longitude"":""-84.390185"",""distance"":3491.96},{""hostname"":""sp1-vm-ashburn-us.samknows.com"",""location"":""Ashburn, VA, US"",""latitude"":""39.030643"",""longitude"":""-77.469985"",""distance"":3681.28},{""hostname"":""sp2-vm-ashburn-us.samknows.com"",""location"":""Ashburn, VA, US"",""latitude"":""39.030643"",""longitude"":""-77.469985"",""distance"":3681.28}]";
        public string hostname { get; set; }
        public string location { get; set; }
        public string latitude {  get; set; }
        public string longitude { get; set; }
        public double distance { get; set; }

        public static List<SamKnowsServers> GetExampleServers()
        {
            var retval = JsonSerializer.Deserialize<List<SamKnowsServers>>(ExampleJson);
            return retval;
        }
    }
}
