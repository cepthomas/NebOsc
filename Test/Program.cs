using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;
using Ephemera.NebOsc;


namespace Ephemera.NebOsc.Test
{
    class Program
    {
        static void Main()
        {
            TestRunner runner = new(OutputFormat.Readable);
            string[] torun = new string[] { "OSC" };
            runner.RunSuites(torun);
            var fn = Path.Combine(MiscUtils.GetSourcePath(), "test_out.txt");
            File.WriteAllLines(fn, runner.Context.OutputLines);
        }

        public class OSC_TimeTag : TestSuite
        {
            public override void RunSuite()
            {
                // Make some test objects.
                DateTime dt1 = new(2005, 5, 9, 15, 47, 39, 123);
                DateTime dt2 = new(2022, 11, 24, 7, 29, 6, 801);

                TimeTag ttImmediate = new(); // default constructor
                TimeTag tt1 = new(dt1); // specific constructor
                TimeTag tt2 = new(dt2);
                TimeTag tt1raw = new(tt1.Raw); // constructor from raw
                TimeTag tt2copy = new(new TimeTag(dt2)); // copy constructor

                // Check them all.
                Assert(ttImmediate.ToString() == "When:Immediate");
                Assert(tt1.ToString() == "When:2005-05-09 15:47:39.000 Seconds:3324642459 Fraction:528280977");
                Assert(tt2.ToString() == "When:2022-11-24 07:29:06.000 Seconds:3878263746 Fraction:3440268803");

                Assert(tt1.Equals(tt1));
                Assert(!ttImmediate.Equals(tt2));
                Assert(!tt1raw.Equals(tt1));

                Assert(tt1 == tt1raw);
                Assert(tt2 == tt2copy);
                Assert(tt1 != tt2);

                Assert(tt2 >= tt1);
                Assert(tt2 > tt1);

                Assert(tt1 <= tt2);
                Assert(tt1 < tt2);
            }
        }

        public class OSC_PackUnpack : TestSuite
        {
            public override void RunSuite()
            {
                List<byte> bytes;
                bool ok;
                int start;

                // pack
                bytes = Packet.Pack("Abe*-88= XXXq");
                Assert(bytes.Count == 16);
                // unpack
                string sval = "";
                start = 0;
                ok = Packet.Unpack(bytes.ToArray(), ref start, ref sval);
                Assert(ok);
                Assert(sval == "Abe*-88= XXXq");

                // pack
                bytes = Packet.Pack(193082);
                Assert(bytes.Count == 4);
                // unpack
                int ival = 0;
                start = 0;
                ok = Packet.Unpack(bytes.ToArray(), ref start, ref ival);
                Assert(ok);
                Assert(ival == 193082);

                // pack
                bytes = Packet.Pack(7340912L);
                Assert(bytes.Count == 8);
                // unpack
                ulong uval = 0;
                start = 0;
                ok = Packet.Unpack(bytes.ToArray(), ref start, ref uval);
                Assert(ok);
                Assert(uval == (ulong)7340912);

                // pack
                bytes = Packet.Pack(2965.8345f);
                Assert(bytes.Count == 4);
                // unpack
                float fval = 0;
                start = 0;
                ok = Packet.Unpack(bytes.ToArray(), ref start, ref fval);
                Assert(ok);
                Assert(fval == 2965.8345f);

                // pack
                bytes = Packet.Pack(new List<byte>() { 11, 28, 205, 68, 137, 251, 59, 71, 184 });
                Assert(bytes.Count == 16);
                // unpack
                List<byte> bval = new();
                start = 0;
                ok = Packet.Unpack(bytes.ToArray(), ref start, ref bval);
                Assert(ok);
                Assert(bval.Count == 9);
                Assert(bval[3] == 68);
                Assert(bval[8] == 184);
            }
        }

        public class OSC_Message : TestSuite
        {
            public override void RunSuite()
            {
                Message m1 = new() { Address = @"/foo/bar" };

                m1.Data.Add(919);
                m1.Data.Add("some text");
                m1.Data.Add(83.743);
                m1.Data.Add(new List<byte>() { 11, 28, 205, 68, 137, 251 });

                var packed = m1.Pack();

                Assert(packed.Count != 0);
                Assert(packed.Count == 52);

                Message m2 = new();
                bool valid = m2.Unpack(packed.ToArray());

                Assert(valid);
                Assert(m2.Address == m1.Address);
                Assert(m2.Data.Count == m1.Data.Count);
                Assert(m2.Data[0].ToString()! == m1.Data[0].ToString()!);
                Assert(m2.Data[1].ToString()! == m1.Data[1].ToString()!);
                Assert(m2.Data[2].ToString()! == m1.Data[2].ToString()!);
                Assert(m2.Data[3].ToString()! == m1.Data[3].ToString()!);

                // Add some invalid data.
                m1.Data.Add(new List<double>());
                packed = m1.Pack();
                Assert(packed.Count == 0);
                Assert(m1.Errors.Count == 1);
                Assert(m1.Errors[0] == "Unknown type: System.Collections.Generic.List`1[System.Double]");
            }
        }

        public class OSC_Bundle : TestSuite
        {
            public override void RunSuite()
            {
                //DateTime dt = new(2005, 5, 9, 15, 47, 39, 123);
                //TimeTag tt = new(dt);
                //Bundle b = new() { TimeTag = tt };
            }
        }

        public class OSC_SendAndReceive : TestSuite
        {
            public override void RunSuite()
            {
                List<Message> rxMsgs = new();

                List<string> logs = new();

                Input nin = new(9700);
                Output nout = new("127.0.0.1", 9700);

                nin.InputReceived += (_, e) => rxMsgs.AddRange(e.Messages);
                nin.Notification += (_, e) => logs.Add(e.Message);
                nout.Notification += (_, e) => logs.Add(e.Message);

                // Send some messages to myself.
                Message m1 = new() { Address = "/foo/bar/" };
                m1.Data.Add(82828);
                m1.Data.Add(new List<byte>() { 22, 44, 77, 0, 211 });
                m1.Data.Add(199.44);
                m1.Data.Add("Snafu boss-man");
                bool ok = nout.Send(m1);
                Assert(ok);

                // Wait a bit.
                System.Threading.Thread.Sleep(500);

                // What happened.
                Assert(logs.Count == 0);
                logs.ForEach(l => Info(l));
                Assert(rxMsgs.Count == 1);
            }
        }
    }
}
