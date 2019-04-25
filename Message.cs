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
    /// Representation of an OSC Message. For doc see README.md.
    /// </summary>
    public class Message : Packet
    {
        #region Properties
        /// <summary>Storage of address.</summary>
        public string Address { get; set; } = null;

        /// <summary>Data elements in the message.</summary>
        public List<object> Data { get; private set; } = new List<object>();

        /// <summary>Parse errors.</summary>
        public List<string> Errors { get; private set; } = new List<string>();
        #endregion

        #region Public functions
        /// <summary>
        /// Client request to format the message.
        /// </summary>
        /// <returns></returns>
        public List<byte> Pack()
        {
            bool ok = true;
            List<byte> bytes = new List<byte>();

            try
            {
                // Data type string.
                StringBuilder dtype = new StringBuilder();

                // Data values.
                List<byte> dvals = new List<byte>();

                Data.ForEach(d =>
                {
                    switch (d)
                    {
                        case int i:
                            dtype.Append('i');
                            dvals.AddRange(Pack(i));
                            break;

                        case double f:
                            dtype.Append('f');
                            dvals.AddRange(Pack((float)f));
                            break;

                        case float f:
                            dtype.Append('f');
                            dvals.AddRange(Pack(f));
                            break;

                        case string s:
                            dtype.Append('s');
                            dvals.AddRange(Pack(s));
                            break;

                        case List<byte> b:
                            dtype.Append('b');
                            dvals.AddRange(Pack(b));
                            break;

                        default:
                            Errors.Add($"Unknown type: {d.GetType()}");
                            ok = false;
                            break;
                    }
                });

                if (ok)
                {
                    // Put it all together.
                    bytes.AddRange(Pack(Address));
                    dtype.Insert(0, ',');
                    bytes.AddRange(Pack(dtype.ToString()));
                    bytes.AddRange(dvals);
                }
                else
                {
                    bytes = null;
                }

            }
            catch (Exception ex)
            {
                Errors.Add($"Exception while packing message: {ex.Message}");
                bytes = null;
            }

            return bytes;
        }

        /// <summary>
        /// Factory parser function.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public bool Unpack(byte[] bytes)
        {
            bool ok = true;

            try
            {
                int index = 0;

                Data.Clear();

                // Parse address.
                string address = null;
                if (ok)
                {
                    ok = Unpack(bytes, ref index, ref address);
                }
                if (ok)
                {
                    Address = address;
                }
                else
                {
                    Errors.Add("Invalid address string");
                }

                // Parse data types.
                string dtypes = null;
                if (ok)
                {
                    ok = Unpack(bytes, ref index, ref dtypes);
                }
                if (ok)
                {
                    ok = (dtypes.Length >= 1) && (dtypes[0] == ',');
                }

                // Parse data values.
                if (ok)
                {
                    for (int i = 1; i < dtypes.Length && ok; i++)
                    {
                        switch (dtypes[i])
                        {
                            case 'i':
                                int di = 0;
                                ok = Unpack(bytes, ref index, ref di);
                                if (ok)
                                {
                                    Data.Add(di);
                                }
                                break;

                            case 'f':
                                float df = 0;
                                ok = Unpack(bytes, ref index, ref df);
                                if (ok)
                                {
                                    Data.Add(df);
                                }
                                break;

                            case 's':
                                string ds = "";
                                ok = Unpack(bytes, ref index, ref ds);
                                if (ok)
                                {
                                    Data.Add(ds);
                                }
                                break;

                            case 'b':
                                List<byte> db = new List<byte>();
                                ok = Unpack(bytes, ref index, ref db);
                                if (ok)
                                {
                                    Data.Add(db);
                                }
                                break;

                            default:
                                ok = false;
                                Errors.Add($"Invalid data type: {dtypes[i]}");
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Errors.Add($"Exception while unpacking message: {ex.Message}");
                ok = false;
            }

            return ok;
        }

        /// <summary>
        /// Readable.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"Address:{Address} Data:");

            Data.ForEach(o => sb.Append(o.ToString() + " ")); // TODO print bytes better.

            return sb.ToString();
        }
        #endregion
    }
}