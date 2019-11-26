using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AzureIoTDemo
{
    public class ProvisioningDeviceClientSample
    {
        ProvisioningDeviceClient _provClient;
        SecurityProvider _security;

        public ProvisioningDeviceClientSample(ProvisioningDeviceClient provisioningDeviceClient, SecurityProvider security)
        {
            _provClient = provisioningDeviceClient;
            _security = security;
        }

        public async Task RunSampleAsync()
        {
            try
            {
                Console.WriteLine($"RegistrationID = {_security.GetRegistrationID()}");
                VerifyRegistrationIdFormat(_security.GetRegistrationID());

                Console.Write("ProvisioningClient RegisterAsync . . . ");
                DeviceRegistrationResult result = await _provClient.RegisterAsync().ConfigureAwait(false);

                Console.WriteLine($"{result.Status}");
                Console.WriteLine($"ProvisioningClient AssignedHub: {result.AssignedHub}; DeviceID: {result.DeviceId}");

                if (result.Status != ProvisioningRegistrationStatusType.Assigned) return;

                Microsoft.Azure.Devices.Client.IAuthenticationMethod auth;
                if (_security is SecurityProviderTpm)
                {
                    Console.WriteLine("Creating TPM DeviceClient authentication.");
                    auth = new DeviceAuthenticationWithTpm(result.DeviceId, _security as SecurityProviderTpm);
                }
                else if (_security is SecurityProviderX509)
                {
                    Console.WriteLine("Creating X509 DeviceClient authentication.");
                    auth = new DeviceAuthenticationWithX509Certificate(result.DeviceId, (_security as SecurityProviderX509).GetAuthenticationCertificate());
                }
                else if (_security is SecurityProviderSymmetricKey)
                {
                    Console.WriteLine("Creating Symmetric Key DeviceClient authenication");
                    auth = new DeviceAuthenticationWithRegistrySymmetricKey(result.DeviceId, (_security as SecurityProviderSymmetricKey).GetPrimaryKey());
                }
                else
                {
                    throw new NotSupportedException("Unknown authentication type.");
                }

                using (DeviceClient iotClient = DeviceClient.Create(result.AssignedHub, auth, Microsoft.Azure.Devices.Client.TransportType.Amqp))
                {
                    Console.WriteLine("DeviceClient OpenAsync.");
                    await iotClient.OpenAsync().ConfigureAwait(false);
                    Console.WriteLine("DeviceClient SendEventAsync.");
                    await iotClient.SendEventAsync(new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes("TestMessage"))).ConfigureAwait(false);
                    Console.WriteLine("DeviceClient CloseAsync.");
                    await iotClient.CloseAsync().ConfigureAwait(false);
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void VerifyRegistrationIdFormat(string v)
        {
            var r = new Regex("^[a-z0-9-]*$");
            if (!r.IsMatch(v))
            {
                throw new FormatException("Invalid registrationId: The registration ID is alphanumeric, lowercase, and may contain hyphens");
            }
        }

    }
}
