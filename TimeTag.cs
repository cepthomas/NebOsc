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
    /// <summary>
    /// Representation of an OSC timetag. For doc see README.md.
    /// </summary>
    public class TimeTag
    {
        #region Constants
        /// <summary>DateTime at OSC epoch 1900-01-01 00:00:00.000.</summary>
        static readonly DateTime EPOCH_DT = new DateTime(1900, 1, 1, 0, 0, 0, 0);

        /// <summary>The special case meaning "immediately."</summary>
        static readonly ulong IMMEDIATELY = 0x0000000000000001;
        #endregion

        #region Properties
        /// <summary>Raw time.</summary>
        public ulong Raw { get; private set; } = IMMEDIATELY;

        /// <summary>Left of the decimal point.</summary>
        public uint Seconds { get { return (uint)(Raw >> 32); } }

        /// <summary>Right of the decimal point.</summary>
        public uint Fraction { get { return (uint)(Raw & 0x00000000FFFFFFFF); } }
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor - "immediately". The most common scenario for this client.
        /// </summary>
        public TimeTag()
        {
            Raw = IMMEDIATELY;
        }

        /// <summary>
        /// Constructor from DateTime.
        /// </summary>
        /// <param name="when"></param>
        public TimeTag(DateTime when)
        {
            Raw = FromDateTime(when);
        }

        /// <summary>
        /// Constructor from raw.
        /// </summary>
        /// <param name="raw"></param>
        public TimeTag(ulong raw)
        {
            Raw = raw;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public TimeTag(TimeTag other)
        {
            Raw = other.Raw;
        }
        #endregion

        #region Public functions
        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return Raw == IMMEDIATELY ?
                $"When:Immediate" :
                $"When:{FromRaw(Raw).ToString("yyyy'-'MM'-'dd HH':'mm':'ss.fff")} Seconds:{Seconds} Fraction:{Fraction}";
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Conversion function.
        /// </summary>
        /// <param name="when"></param>
        /// <returns></returns>
        ulong FromDateTime(DateTime when)
        {
            TimeSpan ts = when - EPOCH_DT;
            double seconds = Math.Truncate(ts.TotalSeconds);
            double fraction = ts.Milliseconds / 1000.0 * 0xFFFFFFFF;
            ulong raw = ((ulong)seconds << 32) + (ulong)fraction;
            return raw;
        }

        /// <summary>
        /// Conversion function.
        /// </summary>
        /// <param name="raw"></param>
        /// <returns></returns>
        DateTime FromRaw(ulong raw)
        {
            uint seconds = (uint)(raw >> 32);
            uint fraction = (uint)(raw & 0x00000000FFFFFFFF);
            double dsec = seconds + fraction / 0xFFFFFFFF;
            DateTime dt = EPOCH_DT.AddSeconds(dsec);
            return dt;
        }
        #endregion

        #region Standard overrides and operators for custom classes
        public override bool Equals(object obj)
        {
            return Equals(obj as TimeTag);
        }

        public bool Equals(TimeTag obj)
        {
            return obj != null && ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return Raw.GetHashCode();
        }

        public static bool operator ==(TimeTag t1, TimeTag t2)
        {
            return (object)t1 != null && (object)t2 != null && (t1.Raw == t2.Raw);
        }

        public static bool operator !=(TimeTag t1, TimeTag t2)
        {
            return (object)t1 == null || (object)t2 == null || (t1.Raw != t2.Raw);
        }

        public static bool operator >(TimeTag t1, TimeTag t2)
        {
            return (object)t1 == null || (object)t2 == null || t1.Raw > t2.Raw;
        }

        public static bool operator >=(TimeTag t1, TimeTag t2)
        {
            return (object)t1 == null || (object)t2 == null || t1.Raw >= t2.Raw;
        }

        public static bool operator <(TimeTag t1, TimeTag t2)
        {
            return (object)t1 == null || (object)t2 == null || t1.Raw < t2.Raw;
        }

        public static bool operator <=(TimeTag t1, TimeTag t2)
        {
            return (object)t1 == null || (object)t2 == null || t1.Raw <= t2.Raw;
        }
        #endregion
    }
}