using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureIoTDemo
{
    public class HubSettings
    {
        public string ConnectionString { get; set; }
        public string DPS_IDSCOPE { get; set; }
        public string IOTHUB_PFX_X509_THUMBPRINT_PRIMARY { get; set; }
        public string ProvisioningConnectionString { get; set; }
        public string GenericDeviceConnectionString { get; set; }
        public string ServiceConnectionString { get; set; }
    }
}
