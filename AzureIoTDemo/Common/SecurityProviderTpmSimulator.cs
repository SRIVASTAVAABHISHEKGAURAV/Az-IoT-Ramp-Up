using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Provisioning.Security;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Tpm2Lib;

namespace AzureIoTDemo
{
    public class SecurityProviderTpmSimulator : SecurityProviderTpm
    {
        private const string SimulatorAddress = "127.0.0.2";

        private const string WindowsSimulatorExeName = "Simulator.exe";
        private const string LinuxSimulatorExeName = "/usr/local/tpm/build/Simulator";
        private const int SimulatorPort = 2321;

        private static string s_simulatorExeName;
        private SecurityProviderTpmHsm _innerClient;

        public SecurityProviderTpmSimulator(string registrationId) : base(registrationId)
        {
            var tpmDevice = new TcpTpmDevice(SimulatorAddress, SimulatorPort);
            tpmDevice.Connect();
            tpmDevice.PowerCycle();

            using (var tpm2 = new Tpm2(tpmDevice))
            {
                tpm2.Startup(Su.Clear);
            }

            _innerClient = new SecurityProviderTpmHsm(GetRegistrationID(), tpmDevice);
        }

        public override void ActivateIdentityKey(byte[] encryptedKey)
        {
            _innerClient.ActivateIdentityKey(encryptedKey);
        }

        public override byte[] GetEndorsementKey()
        {
            return _innerClient.GetEndorsementKey();
        }

        public override byte[] GetStorageRootKey()
        {
            return _innerClient.GetStorageRootKey();
        }

        public override byte[] Sign(byte[] data)
        {
            return _innerClient.Sign(data);
        }

        public static void StopSimulatorProcess()
        {
            foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(s_simulatorExeName)))
            {
                try
                {
                    process?.Kill();
                }
                catch (Exception)
                {
                }
            }
        }

        public static void StartSimulatorProcess()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                s_simulatorExeName = WindowsSimulatorExeName;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                s_simulatorExeName = LinuxSimulatorExeName;
            }
            else
            {
                throw new PlatformNotSupportedException(
                    "TSS.MSR Simulator.exe is available only for Windows. On Linux, ensure that the simulator is " +
                    $"started and listening to TCP connections on {SimulatorAddress}:{SimulatorPort}.");
            }

            // Exe is found at the exact specified path.
            if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(s_simulatorExeName)).Length > 0) return;
            
            // Search next to the simulator DLL location.
            string[] files = Directory.GetFiles(
                Directory.GetCurrentDirectory(), 
                s_simulatorExeName, 
                SearchOption.AllDirectories);

            if (files.Length == 0)
            {
                files = Directory.GetFiles(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), 
                    s_simulatorExeName, 
                    SearchOption.AllDirectories);
            }

            if (files.Length == 0)
            {
                throw new InvalidOperationException($"TPM Simulator not found : {s_simulatorExeName}");
            }

            var simulatorProcess = new Process
            {
                StartInfo =
                {
                    FileName = files[0],
                    WindowStyle = ProcessWindowStyle.Normal,
                    UseShellExecute = true
                }
            };

            simulatorProcess.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerClient.Dispose();
            }
        }
    }
}
