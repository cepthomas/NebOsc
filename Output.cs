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
        readonly object _lock = new object();

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
        public string RemoteIP { get; set; } = "Invalid";

        /// <summary>Where to?</summary>
        public int RemotePort { get; set; } = -1;

        /// <summary>Trace other than errors.</summary>
        public bool Trace { get; set; } = false;
        #endregion

        #region Lifecycle
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

                _udpClient = new UdpClient(RemoteIP, RemotePort);
                inited = true;
                DeviceName = $"OSCOUT:{RemoteIP}:{RemotePort}";
            }
            catch (Exception ex)
            {
                LogMsg($"Init OSCOUT failed: {ex.Message}");
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
            LogEvent?.Invoke(this, new LogEventArgs() { Message = msg, IsError = error });
        }
    }
}
