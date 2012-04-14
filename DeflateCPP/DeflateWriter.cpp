#include <string.h>
#include "Deflate.h"

void DeflateWriter::Read(FILE *fin, int pos, int len)
{
    int rlen = fread(&buf[pos], 1, len, fin);
    if (rlen < len) length = pos + rlen;
}

int DeflateWriter::GetHash(int b1, int b2, int b3)
{
    return (b1 << 4) ^ (b2 << 2) ^ b3;
}

void DeflateWriter::AddHash(int pos)
{
    unsigned char b1 = buf[pos], b2 = buf[pos + 1];
    if (b1 != b2)
    {
        int h = GetHash(b1, b2, buf[pos + 2]);
        int c = current[h];
        tables[h][c & 15] = bufstart + pos;
        current[h] = c + 1;
    }
}

void DeflateWriter::Search(int pos, int *rp, int *rl)
{
    int maxp = -1, maxl = 2;
    int mlen = length - pos;
    if (mlen > maxlen) mlen = maxlen;
    int last = pos - maxdist;
    if (last < 0) last = 0;
    int h = GetHash(buf[pos], buf[pos + 1], buf[pos + 2]);
    int c = current[h];
    int p1 = c < 16 ? 0 : c - 16;
    for (int i = c - 1; i >= p1; i--)
    {
        int p = tables[h][i & 15] - bufstart;
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
    *rp = maxp;
    *rl = maxl;
}

void DeflateWriter::Compress(FILE *fin, FILE *fout)
{
    length = buflen;
    bufstart = 0;
    memset(current, 0, sizeof(current));
    Read(fin, 0, buflen);

    BitWriter bw(fout);
    bw.WriteBit(true);
    bw.WriteBits(2, 1);
    int p = 0;
    while (p < length)
    {
        unsigned char b = buf[p];
        if (p < length - 4 && b == buf[p + 1] && b == buf[p + 2] && b == buf[p + 3])
        {
            int len = 4;
            int mlen = length - p;
            if (mlen > maxlen + 1) mlen = maxlen + 1;
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
            Search(p, &maxp, &maxl);
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
            memcpy(buf, &buf[maxdist], maxdist + maxlen);
            if (length < buflen)
                length -= maxdist;
            else
                Read(fin, maxdist + maxlen, maxdist);
            p -= maxdist;
            bufstart += maxdist;
        }
    }
    bw.WriteFixedHuffman(256);
}
