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

        static void Main(string[] args)
        {
            var buf1 = File.ReadAllBytes(@"C:\Ruby\bin\ruby.exe");
            Console.WriteLine("buf1.Length: {0}", buf1.Length);

            var t1 = DateTime.Now;
            var ms1 = new MemoryStream();
            var ds1 = new DeflateStream(ms1, CompressionMode.Compress);
            ds1.Write(buf1, 0, buf1.Length);
            ds1.Close();
            ms1.Close();
            var buf2 = ms1.ToArray();
            var t2 = DateTime.Now;
            Console.WriteLine("compress time: {0}", t2 - t1);
            Console.WriteLine("buf2.Length: {0} (DeflateStream)", buf2.Length);

            var buf3 = GetDecompress(buf2);
            Console.WriteLine("buf3.Length: {0}", buf3.Length);
            Console.WriteLine("buf1 = buf3: {0}", CheckBytes(buf1, buf3));

            var t3 = DateTime.Now;
            var ms2 = new MemoryStream(buf1);
            var buf4 = Deflate.GetCompressBytes(ms2);
            ms2.Close();
            var t4 = DateTime.Now;
            Console.WriteLine("compress time: {0}", t4 - t3);
            Console.WriteLine("buf4.Length: {0} (original)", buf4.Length);

            var buf5 = GetDecompress(buf4);
            Console.WriteLine("buf5.Length: {0}", buf5.Length);
            Console.WriteLine("buf1 = buf5: {0}", CheckBytes(buf1, buf5));
        }
    }
}
