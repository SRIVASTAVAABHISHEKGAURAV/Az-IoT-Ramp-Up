using AzureIoTDemo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureIoTDemo.Repository
{
    public interface IManageDevice
    {
        public Task<bool> AddDeviceAsync(string deviceId);
        public Task<bool> RemoveDeviceAsync(string deviceId);
        public Task<bool> UpdateDesiredProperties(string deviceId);
        public Task<IEnumerable<Device>> GetDevicesAsync();
        public Task<bool> AddDeviceWithSelfSignedCertificateAsync(string deviceId);
        public Task<bool> AddDeviceProvisioningServiceClientAsync(string deviceId);
        public bool AddDeviceWithDeviceProvisioningCertificateAsync(string deviceId);
        public bool AddDeviceProvisioningService(string deviceId);
        public Task<bool> SendDeviceToCloudMessageAsync(Message message);
        public Task<CloudToDeviceResult> SendCloudToDeviceMessageAsync(Message message);

    }
}
