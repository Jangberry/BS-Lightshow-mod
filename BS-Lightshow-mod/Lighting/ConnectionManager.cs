using System;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Client;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Timers;
using System.Linq;
using IPA.Utilities;

namespace BS_Lightshow_mod.Lighting
{
    class ConnectionManager
    {
        private static readonly IMqttFactory factory = new MqttFactory();
        private static readonly IMqttClient client = factory.CreateMqttClient();
        private IMqttClientOptions options;
        private readonly Stopwatch stopwatch;
        private byte[] sendStack = new byte[0];

        public ConnectionManager()
        {
            stopwatch = new Stopwatch();
        }

        private Timer timer;

        public float delay = 0;

        // TODO: watchdog
        // TODO: Reconnection method (for handling parameters change)
        async public void Connect(string host, int port, string username, string password)
        {
            options = new MqttClientOptionsBuilder()
                            .WithClientId("Game-" + Guid.NewGuid().ToString())
                            .WithTcpServer(host, port)
                            .WithCredentials(username, password)
                            .WithTls()
                            .WithCleanSession()
                            .Build();

            timer = new Timer(Configuration.PluginConfig.Instance.PingIntervalsMillis);
            timer.Elapsed += Ping;
            timer.AutoReset = true;

            client.UseConnectedHandler(async e =>
            {
#if DEBUG
                Plugin.Log?.Debug("### CONNECTED WITH SERVER ###");
#endif
                timer.Enabled = true;
                timer.Start();

                // Subscribe to "/led/ping"
                await client.SubscribeAsync("/led/ping", MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);

#if DEBUG
                Plugin.Log?.Debug("### SUBSCRIBED ###");
#endif
            });
            client.UseDisconnectedHandler(async e =>
            {
#if DEBUG
                Plugin.Log?.Debug("### LOST CONNECTION ###");
#endif
                timer.Enabled = false;
                await Task.Delay(TimeSpan.FromSeconds(5));

                try
                {
                    await client.ConnectAsync(options);
                }
                catch
                {
#if DEBUG
                    Plugin.Log?.Debug("### RECONNECTION FAILED ###");
#endif
                }
            });
            client.UseApplicationMessageReceivedHandler(Pong);

            await client.ConnectAsync(options);

            await client.PublishAsync(new MqttApplicationMessageBuilder()
                                            .WithTopic("/led")
                                            .WithPayload(new byte[] { 0x2D, 0x00 })
                                            .WithAtLeastOnceQoS()
                                            .WithRetainFlag()
                                            .Build());

        }

        public void Send(byte[] message, bool stream = true)
        {
            if (stream)
            {
                client.PublishAsync(new MqttApplicationMessageBuilder()
                                        .WithTopic("/led/stream")
                                        .WithPayload(message)
                                        .WithAtMostOnceQoS()
                                        .Build());
            }
            else
            {
                client.PublishAsync(new MqttApplicationMessageBuilder()
                                        .WithTopic("/led")
                                        .WithPayload(message)
                                        .WithAtLeastOnceQoS()
                                        .WithRetainFlag()
                                        .Build());
            }
        }
        public void SendStack(byte[] message, bool end = false)
        {
            sendStack = sendStack.Concat(message).ToArray();
            if (end)
            {
                Send(sendStack);
                sendStack = new byte[0];
            }
        }

        async public void Disconnect()
        {
            Task unsub = client.UnsubscribeAsync(new string[] { "/led/ping" });
            Task pub = client.PublishAsync(new MqttApplicationMessageBuilder()
                                                    .WithTopic("/led")
                                                    .WithPayload(new byte[] { 0x00, 0x00, 0x00, 0x00 })
                                                    .WithAtLeastOnceQoS()
                                                    .Build());

            await unsub; await pub;
            await client.DisconnectAsync();
#if DEBUG
            Plugin.Log?.Debug("### WILLFULLY DISCONNECTED FROM SERVER ###");
#endif
        }


        private Task Pong(MqttApplicationMessageReceivedEventArgs message)
        {
            if (message.ApplicationMessage.Topic == "/led/ping" && message.ApplicationMessage.ConvertPayloadToString().Contains("pong"))
            {
                stopwatch.Stop();
                delay = (float)stopwatch.Elapsed.TotalSeconds / 2;  // TODO: integrate a scallable evaluation of the ping

#if DEBUG
                Plugin.Log?.Info("New delay: " + delay.ToString());
#endif          
                Plugin.callbackData.aheadTime = delay;
            }
            return Task.CompletedTask;
        }
        private void Ping(object source, ElapsedEventArgs e)
        {
            stopwatch.Restart();

            client.PublishAsync(new MqttApplicationMessageBuilder()
                                            .WithTopic("/led/ping")
                                            .WithPayload("ping")
                                            .WithAtMostOnceQoS()
                                            .Build());
        }
    }
}
