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


namespace NebOsc
{
    /// <summary>OSC has received something.</summary>
    public class InputEventArgs : EventArgs
    {
        /// <summary>Received data.</summary>
        public List<Message> Messages { get; set; } = new List<Message>();
    }

    /// <summary>OSC wants to send something.</summary>
    public class LogEventArgs : EventArgs
    {
        /// <summary>Category.</summary>
        public LogCategory DeviceLogCategory { get; set; } = LogCategory.Info;

        /// <summary>Text to log.</summary>
        public string Message { get; set; } = null;
    }

    /// <summary>Category types.</summary>
    public enum LogCategory { Info, Send, Recv, Error }

    /// <summary>
    /// Bunch of utilities for formatting and parsing.
    /// </summary>
    public static class Utils
    {
        #region Utilities
        /// <summary>
        /// Add 0s to make multiple of 4.
        /// </summary>
        /// <param name="bytes"></param>
        public static void Pad(this List<byte> bytes)
        {
            for (int i = 0; i < bytes.Count % 4; i++)
            {
                bytes.Add(0);
            }
        }

        /// <summary>
        /// Handle endianness.
        /// </summary>
        /// <param name="bytes">Data in place.</param>
        public static void FixEndian(this List<byte> bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                bytes.Reverse();
            }
        }

        /// <summary>
        /// Make readable string.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="delim"></param>
        /// <returns></returns>
        public static string Dump(this List<byte> bytes, string delim = "")
        {
            StringBuilder sb = new StringBuilder();
            bytes.ForEach(b => { if (IsReadable(b)) sb.Append((char)b); else sb.AppendFormat(@"{0}{1:000}", delim, b); });
            return sb.ToString();
        }

        /// <summary>
        /// Test for readable char.
        /// </summary>
        /// <param name="b"></param>
        /// <returns>True/false</returns>
        public static bool IsReadable(byte b)
        {
            return b >= 32 && b <= 126;
        }
        #endregion
    }
}