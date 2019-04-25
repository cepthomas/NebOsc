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
    /// OSC client.
    /// </summary>
    public class Output
    {
        #region Fields
        /// <summary>OSC output device.</summary>
        UdpClient _udpClient;

        /// <summary>Access synchronizer.</summary>
        object _lock = new object();

        /// <summary>Resource clean up.</summary>
        bool _disposed = false;
        #endregion

        #region Events
        /// <inheritdoc />
        public event EventHandler<LogEventArgs> LogEvent;
        #endregion

        #region Properties
        /// <inheritdoc />
        public string DeviceName { get; private set; } = "Invalid";

        /// <summary>Where to?</summary>
        public string IP { get; set; } = "Invalid";

        /// <summary>Where to?</summary>
        public int Port { get; set; } = -1;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public Output()
        {
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public bool Init()
        {
            bool inited = false;

            DeviceName = "Invalid"; // default

            try
            {
                if (_udpClient != null)
                {
                    _udpClient.Close();
                    _udpClient.Dispose();
                    _udpClient = null;
                }

                _udpClient = new UdpClient(IP, Port);
                inited = true;
                DeviceName = $"OSCOUT:{IP}:{Port}";
            }
            catch (Exception ex)
            {
                LogMsg(LogCategory.Error, $"Init OSCOUT failed: {ex.Message}");
                inited = false;
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
                if (_udpClient != null)
                {
                    List<int> msgs = new List<int>();

                    List<byte> bytes = msg.Pack();
                    if (bytes != null)
                    {
                        if (msg.Errors.Count == 0)
                        {
                            _udpClient.Send(bytes.ToArray(), bytes.Count);
                            LogMsg(LogCategory.Send, msg.ToString());
                        }
                        else
                        {
                            msg.Errors.ForEach(e => LogMsg(LogCategory.Error, e));
                        }
                    }
                    else
                    {
                        LogMsg(LogCategory.Error, msg.ToString());
                        ok = false;
                    }
                }
            }

            return ok;
        }
        #endregion

        #region Private functions
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
