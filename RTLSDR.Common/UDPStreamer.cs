using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LoggerService;

namespace RTLSDR.Common
{
    /// <summary>
    /// A class for streaming data over UDP.
    /// </summary>
    public class UDPStreamer
    {
        /// <summary>
        /// The maximum packet size for UDP transmission.
        /// </summary>
        public const int MaxPacketSize = 1400;

        private readonly ILoggingService _log;
        private readonly UdpClient? _UDPClient = null;
        private readonly IPEndPoint? _EndPoint = null;

        private readonly string _ip;
        private readonly int _port = 1235;

        /// <summary>
        /// Gets the IP address for the UDP endpoint.
        /// </summary>
        public string IP
        {
            get
            {
                return _ip;
            }
        }

        /// <summary>
        /// Gets the current UDP client.
        /// </summary>
        public UdpClient? CurrentUDPClient
        {
            get
            {
                return _UDPClient;
            }
        }

        /// <summary>
        /// Gets the current endpoint.
        /// </summary>
        public IPEndPoint? CurrentEndPoint
        {
            get
            {
                return _EndPoint;
            }
        }

        /// <summary>
        /// Gets the port number.
        /// </summary>
        public int Port
        {
            get
            {
                return _port;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UDPStreamer"/> class.
        /// </summary>
        /// <param name="log">The logging service.</param>
        /// <param name="ip">The IP address; defaults to 127.0.0.1 if null.</param>
        /// <param name="port">The port number; finds an available port if -1.</param>
        public UDPStreamer(ILoggingService log, string ip = null, int port = -1)
        {
            _log = log;

            if (ip == null)
            {
                _ip = "127.0.0.1";
            }
            else
            {
                _ip = ip;
            }

            if (port == -1)
            {
                if (IsPortAvailable(1235))
                {
                    port = 1235;
                }
                else
                {
                    if (IsPortAvailable(8000))
                    {
                        port = 8000;
                    }
                    else
                    {
                        port = FindAvailablePort(32000, 33000);
                    }
                }

                if (port == -1)
                {
                    _log.Info($"UDPStreamer: No available port found, binding port 55555");
                    port = 55555;
                }
            }

            _port = port;

            _log.Info($"UDPStreamer: Binding address {IP}:{Port}");

            _UDPClient = new UdpClient();
            _EndPoint = new IPEndPoint(IPAddress.Parse(IP), Port);
        }

        /// <summary>
        /// Sends a byte array over UDP, splitting it into packets if necessary.
        /// </summary>
        /// <param name="array">The byte array to send.</param>
        /// <param name="count">The number of bytes to send.</param>
        public void SendByteArray(byte[] array, int count)
        {
            try
            {
                if (_UDPClient == null || _EndPoint == null)
                    return;

                if (array != null && count > 0)
                {
                    var bufferPart = new byte[MaxPacketSize];
                    var bufferPartSize = 0;
                    var bufferPos = 0;

                    while (bufferPos < count)
                    {
                        if (bufferPos + MaxPacketSize <= count)
                        {
                            bufferPartSize = MaxPacketSize;
                        }
                        else
                        {
                            bufferPartSize = count - bufferPos;
                        }

                        Buffer.BlockCopy(array, bufferPos, bufferPart, 0, bufferPartSize);
                        _UDPClient.Send(bufferPart, bufferPartSize, _EndPoint);
                        bufferPos += bufferPartSize;
                    }
                }

            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        /// <summary>
        /// Gets the local IP address of the machine.
        /// </summary>
        /// <returns>The local IP address, or null if not found.</returns>
        public static IPAddress? GetLocalIPAddress()
        {
            string hostName = Dns.GetHostName();
            var hostEntry = Dns.GetHostEntry(hostName);
            foreach (var ipAddress in hostEntry.AddressList)
            {
                if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ipAddress;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds an available port within the specified range.
        /// </summary>
        /// <param name="startPort">The starting port number.</param>
        /// <param name="endPort">The ending port number.</param>
        /// <returns>The available port number, or -1 if none found.</returns>
        public static int FindAvailablePort(int startPort, int endPort)
        {
            for (int port = startPort; port <= endPort; port++)
            {
                if (IsPortAvailable(port))
                {
                    return port;
                }
            }
            return -1; // No available port found
        }

        /// <summary>
        /// Checks if a port is available for use.
        /// </summary>
        /// <param name="port">The port number to check.</param>
        /// <returns>True if the port is available; otherwise, false.</returns>
        public static bool IsPortAvailable(int port)
        {
            TcpListener? listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Loopback, port);
                {
                    listener.Start();
                    listener.Stop();
                    return true;
                }
            }
            catch (SocketException)
            {
                return false;
            }
        }
    }
}
