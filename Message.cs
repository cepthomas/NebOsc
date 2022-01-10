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
        /// <summary>The OSC address.</summary>
        public string Address { get; set; } = "???";

        /// <summary>OSC data elements in the message.</summary>
        public List<object> Data { get; private set; } = new();

        /// <summary>Parse errors.</summary>
        public List<string> Errors { get; private set; } = new();
        #endregion

        #region Public functions
        /// <summary>
        /// Format to binary form.
        /// </summary>
        /// <returns>The byte array or empty if error occurred.</returns>
        public List<byte> Pack()
        {
            List<byte> bytes = new();
            Errors.Clear();

            try
            {
                // Data type string.
                StringBuilder dtype = new();

                // Data values.
                List<byte> dvals = new();

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
                            break;
                    }
                });

                if (Errors.Count == 0)
                {
                    // Put it all together.
                    bytes.AddRange(Pack(Address));
                    dtype.Insert(0, ',');
                    bytes.AddRange(Pack(dtype.ToString()));
                    bytes.AddRange(dvals);
                }
            }
            catch (Exception ex)
            {
                Errors.Add($"Exception while packing message: {ex.Message}");
            }

            return Errors.Count == 0 ? bytes : new();
        }

        /// <summary>
        /// Parser function.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public bool Unpack(byte[] bytes)
        {
            try
            {
                int index = 0;

                Data.Clear();

                // Parse address.
                string address = "???";
                if(Unpack(bytes, ref index, ref address))
                {
                    Address = address;
                }
                else
                {
                    Errors.Add("Invalid address");
                }

                // Parse data types.
                string dtypes = "";
                if(Unpack(bytes, ref index, ref dtypes))
                {
                    if((dtypes.Length >= 1) && (dtypes[0] == ','))
                    {

                    }
                    else
                    {
                        Errors.Add("Invalid data types");
                    }
                }

                // Parse data values. Trim comma.
                dtypes = dtypes.Remove(0, 1);
                for (int i = 0; i < dtypes.Length && Errors.Count == 0; i++)
                {
                    switch (dtypes[i])
                    {
                        case 'i':
                            int di = 0;
                            if (Unpack(bytes, ref index, ref di))
                            {
                                Data.Add(di);
                            }
                            else
                            {
                                Errors.Add($"Invalid int value at index {i}");
                            }
                            break;

                        case 'f':
                            float df = 0;
                            if(Unpack(bytes, ref index, ref df))
                            {
                                Data.Add(df);
                            }
                            else
                            {
                                Errors.Add($"Invalid float value at index {i}");
                            }
                            break;

                        case 's':
                            string ds = "";
                            if(Unpack(bytes, ref index, ref ds))
                            {
                                Data.Add(ds);
                            }
                            else
                            {
                                Errors.Add($"Invalid string value at index {i}");
                            }
                            break;

                        case 'b':
                            List<byte> db = new();
                            if(Unpack(bytes, ref index, ref db))
                            {
                                Data.Add(db);
                            }
                            else
                            {
                                Errors.Add($"Invalid byte[] value at index {i}");
                            }
                            break;

                        default:
                            Errors.Add($"Invalid data type: {dtypes[i]}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Errors.Add($"Exception while unpacking message: {ex.Message}");
            }

            return Errors.Count == 0;
        }

        /// <summary>
        /// Readable.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append($"Address:{Address} Data:");

            Data.ForEach(d =>
            {
                if (d is List<byte> b)
                {
                    sb.Append($"byte[{b.Count}],");
                }
                else
                {
                    sb.Append(d.ToString() + ",");
                }
            });

            return sb.ToString();
        }
        #endregion
    }
}