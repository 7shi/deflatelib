using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace DeflateCS
{
    class Program
    {
        static bool CheckBytes(byte[] data1, byte[] data2)
        {
            if (data1.Length != data2.Length) return false;
            for (int i = 0; i < data2.Length; i++)
                if (data1[i] != data2[i]) return false;
            return true;
        }

        static byte[] GetDecompress(byte[] data)
        {
            var ms1 = new MemoryStream(data);
            var ms2 = new MemoryStream();
            var ds = new DeflateStream(ms1, CompressionMode.Decompress);
            var buf = new byte[4096];
            int len;
            while ((len = ds.Read(buf, 0, buf.Length)) != 0)
                ms2.Write(buf, 0, len);
            ds.Close();
            ms2.Close();
            ms1.Close();
            return ms2.ToArray();
        }

        static byte[] buf1;

        static void TestLoop(int n, Func<Tuple<TimeSpan, byte[]>> f)
        {
            double times = 0.0;
            for (int i = 0; i < n; i++)
            {
                var result = f();
                var ts = result.Item1;
                var buf2 = result.Item2;
                var buf3 = GetDecompress(buf2);
                var check = CheckBytes(buf1, buf3) ? "OK" : "NG";
                Console.WriteLine("[{0}:{1}] Length: {2}, Time: {3}", i, check, buf2.Length, ts);
                times += ts.TotalSeconds;
            }
            if (n > 1)
                Console.WriteLine("[1 - {0}] Times: {1}", n, TimeSpan.FromSeconds(times / n));
        }

        static void Main(string[] args)
        {
            const string target_in = @"C:\Ruby\bin\ruby.exe";
            const string target_out = @"C:\cs-ruby.exe.deflate";

            Console.WriteLine("[target file]");
            Console.WriteLine("Length: {0}", new FileInfo(target_in).Length);

            Console.WriteLine("");
            Console.WriteLine("[myimpl (file I/O)]");

            {
                TimeSpan ts;
                using (var sin = new FileStream(target_in, FileMode.Open))
                using (var sout = new FileStream(target_out, FileMode.Create))
                {
                    var t1 = DateTime.Now;
                    Deflate.Compress(sin, sout);
                    ts = DateTime.Now - t1;
                }
                var buf2 = File.ReadAllBytes(target_out);
                Console.WriteLine("Length: {0}, Time: {1}", buf2.Length, ts);
            }

            buf1 = File.ReadAllBytes(target_in);

            Console.WriteLine("");
            Console.WriteLine("[myimpl (on memory)]");
            TestLoop(5, () =>
            {
                var t1 = DateTime.Now;
                var ms = new MemoryStream(buf1);
                var buf2 = Deflate.GetCompressBytes(ms);
                ms.Close();
                return Tuple.Create(DateTime.Now - t1, buf2);
            });

            Console.WriteLine("");
            Console.WriteLine("[DeflateStream (on memory)]");
            TestLoop(5, () =>
            {
                var t1 = DateTime.Now;
                var ms = new MemoryStream();
                var ds = new DeflateStream(ms, CompressionMode.Compress);
                ds.Write(buf1, 0, buf1.Length);
                ds.Close();
                ms.Close();
                var buf2 = ms.ToArray();
                return Tuple.Create(DateTime.Now - t1, buf2);
            });
        }
    }
}
