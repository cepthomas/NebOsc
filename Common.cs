using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace NebOsc
{
    /// <summary>OSC has received something.</summary>
    public class InputEventArgs : EventArgs
    {
        /// <summary>Received data.</summary>
        public List<Message> Messages { get; set; } = new List<Message>();
    }

    /// <summary>OSC wants to say something meta.</summary>
    public class LogEventArgs : EventArgs
    {
        /// <summary>Category.</summary>
        public bool IsError { get; set; } = true;

        /// <summary>Text to log. Usually</summary>
        public string Message { get; set; } = "???";
    }

    /// <summary>
    /// Bunch of utilities for formatting and parsing.
    /// </summary>
    public static class Utils
    {
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
            StringBuilder sb = new();
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
    }
}