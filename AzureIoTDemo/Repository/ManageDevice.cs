using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Client;
using System.Text;
using Newtonsoft.Json;
using System.Runtime.InteropServices.ComTypes;
using AzureIoTDemo.Models;

namespace AzureIoTDemo.Repository
{
    public class ManageDevice : IManageDevice
    {
        private static string _iotHubConnectionString, _iotHubGenericDeviceConnectionString, _dpsIdScope, _primaryThumbprint, _serviceConnectionString, _provisioningConnectionString;
        private const string GlobalDeviceEndpoint = "global.azure-devices-provisioning.net";
        private static string DeviceIdPrefix = ";DeviceId=";
        private const string RegistrationId = "sample-registrationid-csharp";
        private const string TpmEndorsementKey = "sample-TpmEndorsementKey";

        //private const string OptionalDeviceId = "myCSharpDevice";
        private const ProvisioningStatus OptionalProvisioningStatus = ProvisioningStatus.Enabled;


        //private string assignedHub;
        //private DeviceAuthenticationWithX509Certificate auth;
        public IConfiguration _configuration { get; }

        private static string s_primaryThumbprint = Environment.GetEnvironmentVariable("IOTHUB_PFX_X509_THUMBPRINT");
        private static string s_secondaryThumbprint = Environment.GetEnvironmentVariable("IOTHUB_PFX_X509_THUMBPRINT2");

        private readonly RegistryManager _registryManager;
        private readonly ServiceClient _serviceClient;

        public ManageDevice(IConfiguration configuration)
        {
            _configuration = configuration;
            _iotHubConnectionString = _configuration.GetSection("IoTHub").Get<HubSettings>().ConnectionString;
            _iotHubGenericDeviceConnectionString = _configuration.GetSection("IoTHub").Get<HubSettings>().GenericDeviceConnectionString;
            _dpsIdScope = _configuration.GetSection("DPS").Get<HubSettings>().DPS_IDSCOPE;
            _provisioningConnectionString = _configuration.GetSection("DPS").Get<HubSettings>().ProvisioningConnectionString;
            _primaryThumbprint = _configuration.GetSection("Enrollment").Get<HubSettings>().IOTHUB_PFX_X509_THUMBPRINT_PRIMARY;
            _registryManager = RegistryManager.CreateFromConnectionString(_iotHubConnectionString);
            _serviceConnectionString = _configuration.GetSection("ServiceClient").Get<HubSettings>().ServiceConnectionString;
            _serviceClient = ServiceClient.CreateFromConnectionString(_serviceConnectionString);
        }

        public async Task<IEnumerable<Device>> GetDevicesAsync()
        {
            var _deviceList = new List<Device>();
            var query = _registryManager.CreateQuery("select * from devices");

            while (query.HasMoreResults)
            {
                IEnumerable<Twin> twins = await query.GetNextAsTwinAsync().ConfigureAwait(false);

                foreach (Twin twin in twins)
                {
                    _deviceList.Add(new Device
                    {
                        deviceId = twin.DeviceId,
                        ConnectionState = twin.ConnectionState.ToString(),
                        LastActivityTime = twin.LastActivityTime.ToString()
                    });
                }
            }
            return _deviceList;
        }

        public async Task<bool> AddDeviceAsync(string deviceId)
        {
            try
            {
                var twin = await _registryManager.GetTwinAsync(deviceId);
                if (twin != null)
                {
                    throw new Exception($"The device {deviceId} already exist");
                }
                var device = await _registryManager.AddDeviceAsync(new Microsoft.Azure.Devices.Device(deviceId)).ConfigureAwait(false);
                return true;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

        }

        public async Task<bool> AddDeviceWithSelfSignedCertificateAsync(string deviceId)
        {
            try
            {
                var device = new Microsoft.Azure.Devices.Device(deviceId)
                {
                    Authentication = new AuthenticationMechanism
                    {
                        Type = AuthenticationType.SelfSigned,
                        X509Thumbprint = new X509Thumbprint
                        {
                            PrimaryThumbprint = _primaryThumbprint,
                            SecondaryThumbprint = ""
                        }
                    }
                };

                Console.Write($"Adding device '{deviceId}' with self signed certificate auth . . . ");
                var newDevice = await _registryManager.AddDeviceAsync(device).ConfigureAwait(false);
                Console.WriteLine("DONE");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> AddDeviceProvisioningServiceClientAsync(string OptionalDeviceId)
        {
            try
            {
                Console.WriteLine("Starting sample...");

                using (ProvisioningServiceClient provisioningServiceClient =
                        ProvisioningServiceClient.CreateFromConnectionString(_provisioningConnectionString))
                {
                    #region Create a new individualEnrollment config
                    Console.WriteLine("\nCreating a new individualEnrollment...");
                    Attestation attestation = new TpmAttestation(TpmEndorsementKey);
                    IndividualEnrollment individualEnrollment = new IndividualEnrollment(RegistrationId, attestation)
                    {

                        DeviceId = OptionalDeviceId,
                        ProvisioningStatus = OptionalProvisioningStatus
                    };
                    #endregion

                    #region Create the individualEnrollment
                    Console.WriteLine("\nAdding new individualEnrollment...");
                    IndividualEnrollment individualEnrollmentResult =
                        await provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);
                    Console.WriteLine("\nIndividualEnrollment created with success.");
                    Console.WriteLine(individualEnrollmentResult);
                    #endregion
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

        }

        public bool AddDeviceProvisioningService(string deviceId)
        {
            try
            {
                Console.WriteLine("Starting TPM simulator.");
                using (var security = new SecurityProviderTpmSimulator(RegistrationId))

                using (var transport = new ProvisioningTransportHandlerAmqp())
                {
                    Console.WriteLine("Extracting endorsement key.");
                    string base64EK = Convert.ToBase64String(Encoding.ASCII.GetBytes(TpmEndorsementKey));//security.GetEndorsementKey());

                    Console.WriteLine($"\tRegistration ID: {RegistrationId}");
                    Console.WriteLine($"\tEndorsement key: {base64EK}");
                    Console.WriteLine("\tDevice ID: iothubtpmdevice1");
                    Console.WriteLine();
                    Console.WriteLine("Press ENTER when ready.");
                    Console.ReadLine();

                    ProvisioningDeviceClient provClient =
                        ProvisioningDeviceClient.Create(GlobalDeviceEndpoint, _dpsIdScope, security, transport);

                    var sample = new ProvisioningDeviceClientSample(provClient, security);
                    sample.RunSampleAsync().GetAwaiter().GetResult();

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public bool AddDeviceWithDeviceProvisioningCertificateAsync(string deviceId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_dpsIdScope))
                {
                    Console.WriteLine("ProvisioningDeviceClientX509 <IDScope>");
                    return false;
                }

                var certificate = LoadPrivateKey(@"C:/Users/abgaurav/IOT/solutions/device-provisioning/key.pfx");

                using (var security = new SecurityProviderX509Certificate(certificate))

                using (var transport = new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly))
                {
                    ProvisioningDeviceClient provClient =
                        ProvisioningDeviceClient.Create(GlobalDeviceEndpoint, _dpsIdScope, security, transport);

                    //var result = await provClient.RegisterAsync();

                    //Console.WriteLine($"Provisioning result: {result.Status}");

                    //if (result.Status != ProvisioningRegistrationStatusType.Assigned)
                    //{
                    //    throw new InvalidOperationException("Something went wrong while trying to provision.");
                    //}

                    //this.assignedHub = result.AssignedHub;
                    //this.auth = new DeviceAuthenticationWithX509Certificate(result.DeviceId, security.GetAuthenticationCertificate());

                    var sample = new ProvisioningDeviceClientSample(provClient, security);
                    sample.RunSampleAsync().GetAwaiter().GetResult();
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

        }

        public async Task AddDeviceWithCertificateAuthorityAuthenticationAsync(string deviceId)
        {
            var device = new Microsoft.Azure.Devices.Device(deviceId)
            {
                Authentication = new AuthenticationMechanism
                {
                    Type = AuthenticationType.CertificateAuthority
                }
            };

            Console.Write($"Adding device '{deviceId}' with CA authentication . . . ");
            await _registryManager.AddDeviceAsync(device).ConfigureAwait(false);
            Console.WriteLine("COMPLETED");
        }

        public async Task<bool> RemoveDeviceAsync(string deviceId)
        {
            try
            {
                var twin = await IsDeviceExists(deviceId);
                await _registryManager.RemoveDeviceAsync(deviceId);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> UpdateDesiredProperties(string deviceId)
        {
            try
            {
                var twin = await IsDeviceExists(deviceId);
                var patch =
                    @"{
                    properties: {
                        desired: {
                          customKey: 'customValue'
                        }
                    }
                }";

                await _registryManager.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }


        public async Task<bool> SendDeviceToCloudMessageAsync(Models.Message message)
        {
            try
            {
                var twin = await IsDeviceExists(message.deviceId);
                if (twin.DeviceId != message.deviceId)
                    throw new Exception("Twin device id is different from the message device id.");

                using (var device = DeviceClient.CreateFromConnectionString(GetDeviceConnectionString(twin)))
                {
                    await device.OpenAsync();
                    Console.WriteLine("Device connected!");

                    var payload = JsonConvert.SerializeObject(message);
                    var messagetocloud = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(payload));
                    await device.SendEventAsync(messagetocloud);
                    Console.WriteLine("Message Sent!");
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<CloudToDeviceResult> SendCloudToDeviceMessageAsync(Models.Message message)
        {
            try
            {
                var twin = await IsDeviceExists(message.deviceId);
                if (twin.DeviceId != message.deviceId)
                    throw new Exception("Twin device id is different from the message device id.");

                return await CallDirectMethod(_serviceClient, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        }

        private static async Task<CloudToDeviceResult> CallDirectMethod(ServiceClient serviceClient, Models.Message message)
        {
            try
            {
                var method = new CloudToDeviceMethod("showMyMessage");
                var payload = JsonConvert.SerializeObject(message);
                method.SetPayloadJson(payload);

                var response = await serviceClient.InvokeDeviceMethodAsync(message.deviceId, method);

                var cloudToDeviceResult = new CloudToDeviceResult()
                {
                    Status = response.Status,
                    PayloadJson = response.GetPayloadAsJson()
                };
                Console.WriteLine($"Response Status: '{response.Status}', payLoad: '{response.GetPayloadAsJson()}' ");

                return cloudToDeviceResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        }

        private static string GetDeviceConnectionString(Twin twin)
        {
            return _iotHubGenericDeviceConnectionString + DeviceIdPrefix + twin.DeviceId;
        }

        private async Task<Twin> IsDeviceExists(string deviceId)
        {
            var twin = await _registryManager.GetTwinAsync(deviceId);
            if (twin == null)
            {
                throw new Exception($"The device {deviceId} does not exist");
            }
            return twin;
        }

        private static X509Certificate2 LoadPrivateKey(string key)
        {
            if (!File.Exists(key))
            {
                Console.WriteLine("No private key found.  Generate private key first.");
                Environment.Exit(-1);
            }

            var certificateCollection = new X509Certificate2Collection();
            certificateCollection.Import(key);

            foreach (var element in certificateCollection)
            {
                if (element.HasPrivateKey)
                {
                    return element;
                }
                else
                {
                    element.Dispose();
                }
            }

            throw new InvalidOperationException("No private key found.");
        }
    }
}
