using System;
using System.Collections.Generic;
using System.IO;

namespace DeflateCS
{
    public class DeflateWriter
    {
        private const int maxbuf = Deflate.maxdist * 2;
        private const int buflen = maxbuf + Deflate.maxlen;
        private int length, bufstart;
        private byte[] buf = new byte[buflen];
        private int[,] tables = new int[4096, 16];
        private int[] current = new int[4096];

        private void Read(Stream sin, int pos, int len)
        {
            int rlen = sin.Read(buf, pos, len);
            if (rlen < len) length = pos + rlen;
        }

        private int GetHash(int b1, int b2, int b3)
        {
            return (b1 << 4) ^ (b2 << 2) ^ b3;
        }

        private void AddHash(int pos)
        {
            byte b1 = buf[pos], b2 = buf[pos + 1];
            if (b1 != b2)
            {
                int h = GetHash(b1, b2, buf[pos + 2]);
                int c = current[h];
                tables[h, c & 15] = bufstart + pos;
                current[h] = c + 1;
            }
        }

        private void Search(int pos, out int rp, out int rl)
        {
            int maxp = -1, maxl = 2;
            int mlen = Math.Min(Deflate.maxlen, length - pos);
            int last = Math.Max(0, pos - Deflate.maxdist);
            int h = GetHash(buf[pos], buf[pos + 1], buf[pos + 2]);
            int c = current[h];
            int p1 = c < 16 ? 0 : c - 16;
            for (int i = c - 1; i >= p1; i--)
            {
                int p = tables[h, i & 15] - bufstart;
                if (p < last)
                    break;
                else
                {
                    int len = 0;
                    while (len < mlen && buf[p + len] == buf[pos + len])
                        len++;
                    if (len > maxl)
                    {
                        maxp = p;
                        maxl = len;
                    }
                }
            }
            rp = maxp;
            rl = maxl;
        }

        public void Compress(Stream sin, Stream sout)
        {
            length = buflen;
            bufstart = 0;
            Array.Clear(current, 0, current.Length);
            Read(sin, 0, buflen);

            var bw = new BitWriter(sout);
            bw.WriteBit(true);
            bw.WriteBits(2, 1);
            int p = 0;
            while (p < length)
            {
                byte b = buf[p];
                if (p < length - 4 && b == buf[p + 1] && b == buf[p + 2] && b == buf[p + 3])
                {
                    int len = 4;
                    int mlen = Math.Min(Deflate.maxlen + 1, length - p);
                    while (len < mlen && b == buf[p + len])
                        len++;
                    bw.WriteFixedHuffman(b);
                    bw.WriteLen(len - 1);
                    bw.WriteDist(1);
                    p += len;
                }
                else
                {
                    int maxp, maxl;
                    Search(p, out maxp, out maxl);
                    if (maxp < 0)
                    {
                        bw.WriteFixedHuffman(b);
                        AddHash(p);
                        p++;
                    }
                    else
                    {
                        bw.WriteLen(maxl);
                        bw.WriteDist(p - maxp);
                        for (int i = p; i < p + maxl; i++)
                            AddHash(i);
                        p += maxl;
                    }
                }
                if (p > maxbuf)
                {
                    Array.Copy(buf, Deflate.maxdist, buf, 0, Deflate.maxdist + Deflate.maxlen);
                    if (length < buflen)
                        length -= Deflate.maxdist;
                    else
                        Read(sin, Deflate.maxdist + Deflate.maxlen, Deflate.maxdist);
                    p -= Deflate.maxdist;
                    bufstart += Deflate.maxdist;
                }
            }
            bw.WriteFixedHuffman(256);
            bw.Close();
        }
    }
}
