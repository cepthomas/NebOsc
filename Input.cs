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
using NBagOfTricks;


namespace NebOsc
{
    /// <summary>
    /// OSC server.
    /// </summary>
    public class Input : IDisposable
    {
        #region Fields
        /// <summary>OSC input device.</summary>
        UdpClient? _udpClient = null;
        #endregion

        #region Events
        /// <summary>Request for logging service. May need Invoke() if client is UI.</summary>
        public event EventHandler<LogEventArgs>? LogEvent;

        /// <summary>Reporting a change to listeners. May need Invoke() if client is UI.</summary>
        public event EventHandler<InputEventArgs>? InputEvent;
        #endregion

        #region Properties
        /// <summary>Name.</summary>
        public string DeviceName { get; private set; } = "Invalid";

        /// <summary>The receive port.</summary>
        public int LocalPort { get; set; } = -1;

        /// <summary>Trace other than errors.</summary>
        public bool Trace { get; set; } = false;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Set up listening for OSC messages.
        /// </summary>
        /// <returns></returns>
        public bool Init()
        {
            bool inited = false;

            try
            {
                _udpClient?.Close();
                _udpClient?.Dispose();

                _udpClient = new(new IPEndPoint(IPAddress.Any, LocalPort));
                _udpClient!.BeginReceive(new AsyncCallback(ReceiveCallback), this);

                inited = true;
                DeviceName = $"OSCIN:{LocalPort}";
            }
            catch (Exception ex)
            {
                inited = false;
                LogMsg($"Init OSCIN failed: {ex.Message}");
            }

            return inited;
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        public void Dispose()
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
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

            if (InputEvent is not null && bytes is not null && bytes.Length > 0)
            {
                InputEventArgs args = new();

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

                InputEvent.Invoke(this, args);
            }

            // Listen again.
            _udpClient?.BeginReceive(new AsyncCallback(ReceiveCallback), this);
        }

        /// <summary>Ask host to do something with this.</summary>
        /// <param name="msg"></param>
        /// <param name="error"></param>
        void LogMsg(string msg, bool error = true)
        {
            LogEvent?.Invoke(this, new LogEventArgs() { Message = msg, IsError = error });
        }
        #endregion
    }
}
