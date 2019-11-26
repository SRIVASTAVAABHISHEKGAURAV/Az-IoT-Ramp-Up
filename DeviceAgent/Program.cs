using DeviceAgent.Common;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace DeviceAgent
{
    class Program
    {
        private const string deviceConnectionString = "{deviceConnectionString}";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Initializing Band Agent..........");
            var device = DeviceClient.CreateFromConnectionString(deviceConnectionString + ";DeviceId=device-01");
            await device.OpenAsync();
            //var receiveEventsTask = ReceiveEvents(device);
            await device.SetMethodDefaultHandlerAsync(OtherdeviceMethod, null);
            await device.SetMethodHandlerAsync("showMyMessage", ShowMyMessage, null);

            Console.WriteLine("Device connected!");

            Console.WriteLine("Press a key to perform action...");
            Console.WriteLine("i: initialize.");
            Console.WriteLine("q: quitRequest.");

            var random = new Random();
            var quitRequest = false;
            while (!quitRequest)
            {
                Console.Write("Action?  ");
                var input = Console.ReadKey().KeyChar;

                var status = StatusType.NotSpecified;
                var latitude = random.Next(0, 100);
                var longitude = random.Next(0, 100);

                switch (char.ToLower(input))
                {
                    case 'q':
                        quitRequest = true;
                        break;
                    case 'i':
                        status = StatusType.initialize;
                        break;
                }

                var telemetry = new Telemetry
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    Status = status
                };

                var payload = JsonConvert.SerializeObject(telemetry);
                var message = new Message(Encoding.ASCII.GetBytes(payload));

                await device.SendEventAsync(message);

                Console.WriteLine("Message Sent!");
            }

            Console.ReadKey();
        }

        private static Task<MethodResponse> OtherdeviceMethod(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine("***Other Device Method Called***");
            Console.WriteLine($"Method: {methodRequest.Name}");
            Console.WriteLine($"Payload: {methodRequest.DataAsJson}");

            var responsepayload = Encoding.ASCII.GetBytes("{\"response\": \"This method is not found!\"}");
            return Task.FromResult(new MethodResponse(responsepayload, 404));
        }

        private static Task<MethodResponse> ShowMyMessage(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine("***MESSAGE RECEIVED***");
            Console.WriteLine(methodRequest.DataAsJson);

            var response = JsonConvert.SerializeObject(new
            {
                response = "Message Shown by Device!",
                originalMessage = methodRequest.DataAsJson

            });

            //var responsepayload = Encoding.ASCII.GetBytes("{\"response\": \"Message Shown!\"}");
            var responsepayload = Encoding.ASCII.GetBytes(response);

            return Task.FromResult(new MethodResponse(responsepayload, 200));
        }

        private static async Task ReceiveEvents(DeviceClient device)
        {
            while (true)
            {
                var message = await device.ReceiveAsync();

                if (message == null)
                {
                    continue;
                }

                var messageBody = message.GetBytes();
                var payLoad = Encoding.ASCII.GetString(messageBody);

                Console.WriteLine($"Received message from cloud: '{payLoad}'");

                //// respond back to cloud
                //await device.RejectAsync(message);
                //await device.AbandonAsync(message);
                await device.CompleteAsync(message);

            }
        }
    }
}
