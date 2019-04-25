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
    public class Input
    {
        #region Fields
        /// <summary>OSC input device.</summary>
        UdpClient _udpClient = null;

        /// <summary>Resource clean up.</summary>
        bool _disposed = false;
        #endregion

        #region Events
        /// <summary>Request for logging service.</summary>
        public event EventHandler<LogEventArgs> LogEvent;

        /// <summary>Reporting a change to listeners.</summary>
        public event EventHandler<InputEventArgs> InputEvent;
        #endregion

        #region Properties
        /// <summary>Name.</summary>
        public string DeviceName { get; private set; } = "Invalid";

        /// <summary>The local port.</summary>
        public int Port { get; set; } = -1;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public Input()
        {
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public bool Init()
        {
            bool inited = false;

            try
            {
                if (_udpClient != null)
                {
                    _udpClient.Close();
                    _udpClient.Dispose();
                    _udpClient = null;
                }

                _udpClient = new UdpClient(Port);
                inited = true;
                DeviceName = $"OSCIN:{Port}";
            }
            catch (Exception ex)
            {
                inited = false;
                LogMsg(LogCategory.Error, $"Init OSCIN failed: {ex.Message}");
            }

            return inited;
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _udpClient?.Close();
                _udpClient?.Dispose();
                _udpClient = null;

                _disposed = true;
            }
        }
        #endregion

        #region Public functions
        /// <summary>
        /// 
        /// </summary>
        public void Start()
        {
            _udpClient.BeginReceive(new AsyncCallback(Receive), this);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            _udpClient.Close();
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Handle a received message.
        /// </summary>
        /// <param name="ares"></param>
        void Receive(IAsyncResult ares)
        {
            IPEndPoint sender = new IPEndPoint(0, 0);

            // Process input.
            byte[] bytes = _udpClient.EndReceive(ares, ref sender);

            if (InputEvent != null && bytes != null && bytes.Length > 0)
            {
                InputEventArgs args = new InputEventArgs();

                // Unpack - check for bundle or message.
                if (bytes[0] == '#')
                {
                    Bundle b = new Bundle();
                    if(b.Unpack(bytes))
                    {
                        args.Messages.AddRange(b.Messages);
                    }
                    else
                    {
                        b.Errors.ForEach(e => LogMsg(LogCategory.Error, e));
                    }
                }
                else
                {
                    Message m = new Message();
                    if(m.Unpack(bytes))
                    {
                        args.Messages.Add(m);
                    }
                    else
                    {
                        m.Errors.ForEach(e => LogMsg(LogCategory.Error, e));
                    }
                }

                InputEvent.Invoke(this, args);
            }

            // Listen again.
            _udpClient.BeginReceive(new AsyncCallback(Receive), this);
        }

        /// <summary>Ask host to do something with this.</summary>
        /// <param name="cat"></param>
        /// <param name="msg"></param>
        void LogMsg(LogCategory cat, string msg)
        {
            LogEvent?.Invoke(this, new LogEventArgs() { DeviceLogCategory = cat, Message = msg });
        }
        #endregion
    }
}
