using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureIoTDemo.Models;
using AzureIoTDemo.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureIoTDemo.Controllers
{
    [ApiController]
    [Route("api/IoTHub")]
    public class IoTHubController : ControllerBase
    {

        private readonly ILogger<IoTHubController> _logger;
        private IManageDevice _manageDevice;

        public IoTHubController(ILogger<IoTHubController> logger, IManageDevice manageDevice)
        {
            _logger = logger;
            _manageDevice = manageDevice;
        }


        [HttpPost("AddDeviceAsync")]
        public Task<bool> AddDeviceAsync([FromBody]Device device)
        {
            try
            {
                IsValidDeviceId(device.deviceId);
                if (device.IsSelfSigned)
                {
                    return _manageDevice.AddDeviceWithSelfSignedCertificateAsync(device.deviceId.Trim());
                    //return _manageDevice.AddDeviceWithDeviceProvisioningCertificateAsync(device.deviceId.Trim());
                    // return _manageDevice.AddDeviceProvisioningServiceClientAsync(device.deviceId.Trim());
                    //return _manageDevice.AddDeviceProvisioningService(device.deviceId.Trim());
                }
                else
                {
                    return _manageDevice.AddDeviceAsync(device.deviceId.Trim());
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.Message);
                throw ex;
            }
        }


        [HttpPost("UpdateDeviceAsync")]
        public Task<bool> UpdateDeviceAsync([FromBody]Device device)
        {
            try
            {
                IsValidDeviceId(device.deviceId);
                return _manageDevice.UpdateDesiredProperties(device.deviceId.Trim());
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.Message);
                throw ex;
            }
        }

        [HttpPost("RemoveDeviceAsync")]
        public Task<bool> RemoveDeviceAsync([FromBody]Device device)
        {
            try
            {
                IsValidDeviceId(device.deviceId);
                return _manageDevice.RemoveDeviceAsync(device.deviceId.Trim());
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.Message);
                throw ex;
            }

        }

        [HttpGet("GetDevices")]
        public async Task<IEnumerable<Device>> GetDevices()
        {
            var devices = await _manageDevice.GetDevicesAsync();
            return devices.ToArray();
        }

        private static void IsValidDeviceId(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                throw new Exception("Invalid device id");
            }
        }


        [HttpPost("SendDeviceToCloudMessageAsync")]
        public Task<bool> SendDeviceToCloudMessageAsync([FromBody]Message message)
        {
            try
            {
                IsValidDeviceId(message.deviceId);
                return _manageDevice.SendDeviceToCloudMessageAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.Message);
                throw ex;
            }
        }

        [HttpPost("SendCloudToDeviceMessageAsync")]
        public Task<CloudToDeviceResult> SendCloudToDeviceMessageAsync([FromBody]Message message)
        {
            try
            {
                IsValidDeviceId(message.deviceId);
                return _manageDevice.SendCloudToDeviceMessageAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.Message);
                throw ex;
            }
        }
    }
}
