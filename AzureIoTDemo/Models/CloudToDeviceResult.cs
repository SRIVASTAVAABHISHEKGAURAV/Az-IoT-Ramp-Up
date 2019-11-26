using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureIoTDemo.Models
{
    public class CloudToDeviceResult
    {
        //
        // Summary:
        //     Gets or sets the status of device method invocation.
        [JsonProperty("status")]
        public int Status { get; set; }

        //
        // Summary:
        //     Get payload as json
        [JsonProperty("payloadjson")]
        public string PayloadJson { get; set; }
    }
}
