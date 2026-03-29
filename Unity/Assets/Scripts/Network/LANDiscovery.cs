using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace QBlockyFighter.Network
{
    /// <summary>
    /// 局域网发现工具 - 自动发现同局域网内的游戏服务器
    /// </summary>
    public class LANDiscovery : MonoBehaviour
    {
        public static LANDiscovery Instance { get; private set; }

        [Header("发现配置")]
        public int broadcastPort = 47800;
        public float broadcastInterval = 3f;

        private UdpClient udpClient;
        private bool isListening;
        private string localIP;
        private float broadcastTimer;

        public event System.Action<string, string> OnServerFound; // ip, name

        void Awake()
        {
            Instance = this;
            localIP = GetLocalIPAddress();
        }

        /// <summary>开始广播自己的服务器</summary>
        public void StartBroadcast(string serverName)
        {
            try
            {
                udpClient = new UdpClient();
                udpClient.EnableBroadcast = true;

                string message = $"QBF_SERVER:{serverName}:{localIP}:3000";
                byte[] data = System.Text.Encoding.UTF8.GetBytes(message);

                // 广播到整个子网
                var endpoint = new IPEndPoint(IPAddress.Parse("255.255.255.255"), broadcastPort);
                udpClient.Send(data, data.Length, endpoint);

                Debug.Log($"[LAN] 开始广播服务器: {serverName} @ {localIP}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LAN] 广播失败: {ex.Message}");
            }
        }

        /// <summary>开始监听局域网内的服务器</summary>
        public void StartListening()
        {
            try
            {
                udpClient = new UdpClient(broadcastPort);
                isListening = true;
                udpClient.BeginReceive(OnReceive, null);
                Debug.Log($"[LAN] 开始监听局域网服务器...");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LAN] 监听失败: {ex.Message}");
            }
        }

        public void StopListening()
        {
            isListening = false;
            udpClient?.Close();
            udpClient = null;
        }

        private void OnReceive(System.IAsyncResult result)
        {
            if (!isListening) return;

            try
            {
                var endpoint = new IPEndPoint(IPAddress.Any, broadcastPort);
                byte[] data = udpClient.EndReceive(result, ref endpoint);
                string message = System.Text.Encoding.UTF8.GetString(data);

                if (message.StartsWith("QBF_SERVER:"))
                {
                    var parts = message.Split(':');
                    if (parts.Length >= 4)
                    {
                        string serverName = parts[1];
                        string serverIP = parts[2];
                        string serverPort = parts[3];

                        Debug.Log($"[LAN] 发现服务器: {serverName} @ {serverIP}:{serverPort}");
                        OnServerFound?.Invoke($"ws://{serverIP}:{serverPort}", serverName);
                    }
                }

                // 继续监听
                if (isListening)
                    udpClient.BeginReceive(OnReceive, null);
            }
            catch (System.ObjectDisposedException) { }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LAN] 接收异常: {ex.Message}");
            }
        }

        /// <summary>获取本机局域网IP</summary>
        public static string GetLocalIPAddress()
        {
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType != NetworkInterfaceType.Wireless80211 &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Ethernet) continue;

                    foreach (var ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ip.Address.ToString();
                        }
                    }
                }
            }
            catch { }
            return "127.0.0.1";
        }

        void OnDestroy()
        {
            StopListening();
        }
    }
}
