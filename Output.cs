using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Drawing;
using Ephemera.NBagOfTricks;


namespace Ephemera.NebOsc
{
    /// <summary>
    /// OSC client.
    /// </summary>
    public sealed class Output : IDisposable
    {
        #region Fields
        /// <summary>OSC output device.</summary>
        UdpClient? _udpClient;

        /// <summary>Access synchronizer.</summary>
        readonly object _lock = new();
        #endregion

        #region Events
        /// <inheritdoc />
        public event EventHandler<NotificationEventArgs>? Notification;
        #endregion

        #region Properties
        /// <inheritdoc />
        public string DeviceName { get; init; } = "Invalid";

        /// <summary>Where to?</summary>
        public string RemoteIP { get; init; } = "Invalid";

        /// <summary>Where to?</summary>
        public int RemotePort { get; init; } = -1;

        /// <summary>Trace other than errors.</summary>
        public bool Trace { get; set; } = false;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <returns></returns>
        public Output(string remoteIP, int remotePort)
        {
            RemoteIP = remoteIP;
            RemotePort = remotePort;

            try
            {
                _udpClient = new UdpClient(RemoteIP, RemotePort);
                DeviceName = $"OSCOUT:{RemoteIP}:{RemotePort}";
            }
            catch (Exception ex)
            {
                var s = $"Init OSCOUT failed: {ex.Message}";
                LogMsg(s);
                Dispose();
                throw new InvalidOperationException(s);
            }
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        public void Dispose()
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
            _udpClient = null;
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Send a message to output.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool Send(Message msg)
        {
            bool ok = true;

            // Critical code section.
            lock (_lock)
            {
                if (_udpClient is not null && msg is not null)
                {
                    List<int> msgs = new();

                    List<byte> bytes = msg.Pack();
                    if (bytes is not null)
                    {
                        int num = _udpClient.Send(bytes.ToArray(), bytes.Count);
                    }
                    else
                    {
                        msg.Errors.ForEach(e => LogMsg(e));
                        ok = false;
                    }
                }
            }

            return ok;
        }
        #endregion

        /// <summary>Ask host to do something with this.</summary>
        /// <param name="msg"></param>
        /// <param name="error"></param>
        void LogMsg(string msg, bool error = true)
        {
            Notification?.Invoke(this, new NotificationEventArgs() { Message = msg, IsError = error });
        }
    }
}
