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
    /// OSC server.
    /// </summary>
    public sealed class Input : IDisposable
    {
        #region Fields
        /// <summary>OSC input device.</summary>
        UdpClient? _udpClient = null;
        #endregion

        #region Events
        /// <summary>Request for logging service. May need Invoke() if client is UI.</summary>
        public event EventHandler<NotificationEventArgs>? Notification;

        /// <summary>Reporting a change to listeners. May need Invoke() if client is UI.</summary>
        public event EventHandler<InputReceiveEventArgs>? InputReceived;
        #endregion

        #region Properties
        /// <summary>Name.</summary>
        public string DeviceName { get; init; } = "Invalid";

        /// <summary>The receive port.</summary>
        public int LocalPort { get; init; } = -1;

        /// <summary>Trace other than errors.</summary>
        public bool Trace { get; set; } = false;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor. Set up listening for OSC messages.
        /// </summary>
        /// <returns></returns>
        public Input(int localPort)
        {
            LocalPort = localPort;

            try
            {
                _udpClient = new(new IPEndPoint(IPAddress.Any, LocalPort));
                _udpClient!.BeginReceive(new AsyncCallback(ReceiveCallback), this);
                DeviceName = $"OSCIN:{LocalPort}";
            }
            catch (Exception ex)
            {
                var s = $"Init OSCIN failed: {ex.Message}";
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

        #region Private functions
        /// <summary>
        /// Handle a received message.
        /// </summary>
        /// <param name="ar"></param>
        void ReceiveCallback(IAsyncResult ar)
        {
            IPEndPoint? sender = new(IPAddress.Any, LocalPort);

            // Process input.
            byte[] bytes = _udpClient!.EndReceive(ar, ref sender);

            if (InputReceived is not null && bytes is not null && bytes.Length > 0)
            {
                InputReceiveEventArgs args = new();

                // Unpack - check for bundle or message.
                if (bytes[0] == '#')
                {
                    Bundle b = new();
                    if(b.Unpack(bytes))
                    {
                        args.Messages.AddRange(b.Messages);
                    }
                    else
                    {
                        b.Errors.ForEach(e => LogMsg(e));
                    }
                }
                else
                {
                    Message m = new();

                    if (m.Unpack(bytes))
                    {
                        args.Messages.Add(m);
                    }
                    else
                    {
                        m.Errors.ForEach(e => LogMsg(e));
                    }
                }

                InputReceived.Invoke(this, args);
            }

            // Listen again.
            _udpClient?.BeginReceive(new AsyncCallback(ReceiveCallback), this);
        }

        /// <summary>Ask host to do something with this.</summary>
        /// <param name="msg"></param>
        /// <param name="error"></param>
        void LogMsg(string msg, bool error = true)
        {
            Notification?.Invoke(this, new NotificationEventArgs() { Message = msg, IsError = error });
        }
        #endregion
    }
}
