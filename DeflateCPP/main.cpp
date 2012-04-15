#include <windows.h>
#include "Deflate.h"

int main(int argc, char* argv[])
{
    FILE *fin = fopen("C:\\Ruby\\bin\\ruby.exe", "rb");
    if (fin == NULL) return 1;
    FILE *fout = fopen("C:\\cpp-ruby.exe.deflate", "wb");
    if (fout == NULL)
    {
        fclose(fin);
        return 1;
    }
    DWORD t1 = GetTickCount();
    Deflate_Compress(fin, fout);
    DWORD t2 = GetTickCount();
    printf("compress time: %f\n", (t2 - t1) / 1000.0);
	return 0;
}
