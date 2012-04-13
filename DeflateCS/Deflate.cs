// public domain

using System;
using System.Collections.Generic;
using System.IO;

namespace DeflateCS
{
    public static class Deflate
    {
        public const int maxlen = 258;
        public const int maxdist = 32768;

        public static int[] litexlens = new int[286];
        public static int[] litlens = new int[286];
        public static int[] litindex = new int[maxlen - 2];
        public static int[] distexlens = new int[30];
        public static int[] distlens = new int[30];
        public static int[] distindex = new int[maxdist];
        public static byte[, ,] sl = new byte[8192, 8, 3];
        public static byte[] rev = new byte[256];

        static Deflate()
        {
            for (int i = 265; i <= 284; i++)
                litexlens[i] = (i - 261) >> 2;

            int v = 3;
            for (int i = 257; i <= 284; i++)
            {
                litlens[i] = v;
                int p2 = 1;
                for (int j = 1; j <= litexlens[i]; j++)
                    p2 <<= 1;
                for (int j = 1; j <= p2; j++, v++)
                    litindex[v - 3] = i;
            }
            litlens[285] = maxlen;
            litindex[maxlen - 3] = 285;

            for (int i = 4; i < 30; i++)
                distexlens[i] = (i - 2) >> 1;

            v = 1;
            for (int i = 0; i < 30; i++)
            {
                distlens[i] = v;
                int p2 = 1;
                for (int j = 1; j <= distexlens[i]; j++)
                    p2 <<= 1;
                for (int j = 1; j <= p2; j++, v++)
                    distindex[v - 1] = i;
            }

            for (int i = 0; i < 8192; i++)
            {
                int p2 = 1;
                for (int j = 0; j < 8; j++)
                {
                    v = i * p2;
                    sl[i, j, 0] = (byte)(v & 255);
                    sl[i, j, 1] = (byte)((v >> 8) & 255);
                    sl[i, j, 2] = (byte)(v >> 16);
                    p2 += p2;
                }
            }

            for (int i = 0; i < 256; i++)
            {
                int p2 = 1;
                int p2r = 128;
                int b = 0;
                for (int j = 0; j < 8; j++)
                {
                    if ((i & p2) != 0) b += p2r;
                    p2 <<= 1;
                    p2r >>= 1;
                }
                rev[i] = (byte)b;
            }
        }

        public static byte[] GetCompressBytes(Stream sin)
        {
            var ms = new MemoryStream();
            var w = new DeflateWriter();
            w.Compress(sin, ms);
            ms.Close();
            return ms.ToArray();
        }
    }
}
