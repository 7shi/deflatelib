#include "Deflate.h"

int litexlens[286], litlens[286], litindex[maxlen - 2];
int distexlens[30], distlens[30], distindex[maxdist];
unsigned char sl[8192][8][3], rev[256];

static void Init()
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
            sl[i][j][0] = (unsigned char)(v & 255);
            sl[i][j][1] = (unsigned char)((v >> 8) & 255);
            sl[i][j][2] = (unsigned char)(v >> 16);
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
        rev[i] = (unsigned char)b;
    }
}

void Deflate_Compress(FILE *fin, FILE *fout)
{
    if (rev[255] == 0) Init();
    DeflateWriter w;
    w.Compress(fin, fout);
}
