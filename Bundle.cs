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
    /// Representation of an OSC Bundle. For doc see README.md.
    /// </summary>
    public class Bundle : Packet
    {
        #region Constants
        /// <summary>Bundle marker</summary>
        public const string BUNDLE_ID = "#bundle";
        #endregion

        #region Properties
        /// <summary>The timetag.</summary>
        public TimeTag TimeTag { get; set; } = new TimeTag();

        /// <summary>Contained messages.</summary>
        public List<Message> Messages { get; private set; } = new List<Message>();

        /// <summary>Parse errors.</summary>
        public List<string> Errors { get; private set; } = new List<string>();
        #endregion

        #region Public functions
        /// <summary>
        /// Format to binary form.
        /// </summary>
        /// <returns></returns>
        public List<byte> Pack()
        {
            List<byte> bytes = new List<byte>();

            try
            {
                // Front matter
                bytes.AddRange(Pack(BUNDLE_ID));
                bytes.AddRange(Pack(TimeTag.Raw));

                // Messages
                foreach (Message m in Messages)
                {
                    List<byte> mb = m.Pack();
                    bytes.AddRange(Pack(mb.Count));
                    bytes.AddRange(mb);
                }

                // Tail
                bytes.Pad();
                bytes.InsertRange(0, Pack(bytes.Count));

            }
            catch (Exception ex)
            {
                Errors.Add($"Exception while packing bundle: {ex.Message}");
                bytes = null;
            }

            return bytes;
        }

        /// <summary>
        /// Factory function.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public bool Unpack(byte[] bytes)
        {
            bool ok = true;

            try
            {
                int index = 0;
                Errors.Clear();
                Messages.Clear();

                // Parse marker.
                string marker = null;
                if (ok)
                {
                    ok = Unpack(bytes, ref index, ref marker);
                }
                if (ok)
                {
                    ok = marker == BUNDLE_ID;
                }
                if (!ok)
                {
                    Errors.Add("Invalid marker string");
                }

                // Parse timetag.
                ulong tt = 0;
                if (ok)
                {
                    ok = Unpack(bytes, ref index, ref tt);
                }
                if (ok)
                {
                    TimeTag = new TimeTag(tt);
                }

                // Parse bundles and messages.
                List<Bundle> bundles = new List<Bundle>();

                if (ok)
                {
                    while (index < bytes.Count() && ok)
                    {
                        if (bytes[index] == '#') // bundle?
                        {
                            Bundle b = new Bundle();
                            if (b.Unpack(bytes))
                            {
                                bundles.Add(b);
                            }
                            else
                            {
                                ok = false;
                                Errors.Add("Couldn't unpack the bundle");
                            }
                        }
                        else // message?
                        {
                            Message m = new Message();
                            if (m.Unpack(bytes))
                            {
                                Messages.Add(m);
                            }
                            else
                            {
                                ok = false;
                                Errors.Add("Couldn't unpack the bundle");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ok = false;
                Errors.Add($"Exception while unpacking bundle: {ex.Message}");
            }

            return ok;
        }
        #endregion
    }
}