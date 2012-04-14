#include "Deflate.h"

BitWriter::BitWriter(FILE *fout)
    : fout(fout), bufp(0), cur(0), bit(0)
{
}

BitWriter::~BitWriter()
{
    Close();
}

void BitWriter::WriteByte(unsigned char b)
{
    buf[bufp] = b;
    if (bufp < 4095)
        bufp++;
    else
    {
        fwrite(buf, 1, sizeof(buf), fout);
        bufp = 0;
    }
}

void BitWriter::Close()
{
    if (bit > 0)
    {
        WriteByte(cur);
        cur = 0;
        bit = 0;
    }
    if (bufp > 0)
    {
        fwrite(buf, 1, bufp, fout);
        bufp = 0;
    }
    fflush(fout);
}

void BitWriter::WriteBit(bool b)
{
    if (b) cur |= (unsigned char)(1 << bit);
    if (bit < 7)
        bit++;
    else
    {
        WriteByte(cur);
        cur = 0;
        bit = 0;
    }
}

void BitWriter::WriteBits(int len, int b)
{
    if (len > 0)
    {
        unsigned char *s = sl[b][bit];
        unsigned char v = cur | s[0];
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
                cur = s[1];
                bit = pos - 8;
            }
            else
            {
                WriteByte(s[1]);
                cur = s[2];
                bit = pos - 16;
            }
        }
    }
}

void BitWriter::WriteFixedHuffman(int b)
{
    if (b < 144)
        WriteBits(8, rev[b + 48]);
    else if (b < 256)
    {
        WriteBit(true);
        WriteBits(8, rev[b]);
    }
    else if (b < 280)
        WriteBits(7, rev[(b - 256) << 1]);
    else if (b < 288)
        WriteBits(8, rev[b - 88]);
}

void BitWriter::WriteLen(int length)
{
    int ll = litindex[length - 3];
    WriteFixedHuffman(ll);
    WriteBits(litexlens[ll], length - litlens[ll]);
}

void BitWriter::WriteDist(int d)
{
    int dl = distindex[d - 1];
    WriteBits(5, rev[dl << 3]);
    WriteBits(distexlens[dl], d - distlens[dl]);
}
