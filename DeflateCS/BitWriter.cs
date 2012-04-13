// public domain

using System;
using System.Collections.Generic;
using System.IO;

namespace DeflateCS
{
    public class BitWriter
    {
        private Stream sout;
        private byte[] buf = new byte[4096];
        private int bufp;
        private byte cur;
        private int bit;

        public BitWriter(Stream sout)
        {
            this.sout = sout;
        }

        public void WriteByte(byte b)
        {
            buf[bufp] = b;
            if (bufp < 4095)
                bufp++;
            else
            {
                sout.Write(buf, 0, buf.Length);
                bufp = 0;
            }
        }

        public void Close()
        {
            if (bit > 0)
            {
                WriteByte(cur);
                cur = 0;
                bit = 0;
            }
            if (bufp > 0)
            {
                sout.Write(buf, 0, bufp);
                bufp = 0;
            }
            sout.Flush();
        }

        public void WriteBit(bool b)
        {
            if (b) cur |= (byte)(1 << bit);
            if (bit < 7)
                bit++;
            else
            {
                WriteByte(cur);
                cur = 0;
                bit = 0;
            }
        }

        public void WriteBits(int len, int b)
        {
            if (len > 0)
            {
                var v = (byte)(cur | Deflate.sl[b, bit, 0]);
                int pos = bit + len;
                if (pos < 8)
                {
                    cur = v;
                    bit = pos;
                }
                else
                {
                    WriteByte(v);
                    if (pos < 16)
                    {
                        cur = Deflate.sl[b, bit, 1];
                        bit = pos - 8;
                    }
                    else
                    {
                        WriteByte(Deflate.sl[b, bit, 1]);
                        cur = Deflate.sl[b, bit, 2];
                        bit = pos - 16;
                    }
                }
            }
        }

        public void WriteFixedHuffman(int b)
        {
            if (b < 144)
                WriteBits(8, Deflate.rev[b + 48]);
            else if (b < 256)
            {
                WriteBit(true);
                WriteBits(8, Deflate.rev[b]);
            }
            else if (b < 280)
                WriteBits(7, Deflate.rev[(b - 256) << 1]);
            else if (b < 288)
                WriteBits(8, Deflate.rev[b - 88]);
        }

        public void WriteLen(int length)
        {
            int ll = Deflate.litindex[length - 3];
            WriteFixedHuffman(ll);
            WriteBits(Deflate.litexlens[ll], length - Deflate.litlens[ll]);
        }

        public void WriteDist(int d)
        {
            int dl = Deflate.distindex[d - 1];
            WriteBits(5, Deflate.rev[dl << 3]);
            WriteBits(Deflate.distexlens[dl], d - Deflate.distlens[dl]);
        }
    }
}
