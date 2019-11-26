using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureIoTDemo
{
    public class Device
    {
        public string deviceId { get; set; }
        public string ConnectionState { get; set; }
        public string LastActivityTime { get; set; }
        public bool IsSelfSigned { get; set; }
    }
}
