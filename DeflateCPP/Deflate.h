#ifndef _DEFLATE_H_
#define _DEFLATE_H_

#include <stdio.h>

const int maxlen = 258;
const int maxdist = 32768;
const int maxbuf = maxdist * 2;
const int buflen = maxbuf + maxlen;

extern int litexlens[286], litlens[286], litindex[maxlen - 2];
extern int distexlens[30], distlens[30], distindex[maxdist];
extern unsigned char sl[8192][8][3], rev[256];

extern void Deflate_Compress(FILE *fin, FILE *fout);

class DeflateWriter
{
private:
    int length, bufstart;
    unsigned char buf[buflen];
    int tables[4096][16];
    int current[4096];

    void Read(FILE *fin, int pos, int len);
    int GetHash(int b1, int b2, int b3);
    void AddHash(int pos);
    void Search(int pos, int *rp, int *rl);

public:
    void Compress(FILE *fin, FILE *fout);
};

class BitWriter
{
private:
    FILE *fout;
    unsigned char buf[4096];
    int bufp;
    unsigned char cur;
    int bit;

public:
    BitWriter(FILE *fout);
    ~BitWriter();
    void WriteByte(unsigned char b);
    void Close();
    void WriteBit(bool b);
    void WriteBits(int len, int b);
    void WriteFixedHuffman(int b);
    void WriteLen(int length);
    void WriteDist(int d);
};

#endif
