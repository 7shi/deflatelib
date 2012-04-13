Imports System.IO
Imports System.IO.Compression

Module Module1
    Function CheckBytes(data1 As Byte(), data2 As Byte()) As Boolean
        If data1.Length <> data2.Length Then Return False
        For i = 0 To data1.Length - 1
            If data1(i) <> data2(i) Then Return False
        Next
        Return True
    End Function

    Function GetDecompress(data As Byte()) As Byte()
        Dim ms1 = New MemoryStream(data)
        Dim ms2 = New MemoryStream()
        Dim ds = New DeflateStream(ms1, CompressionMode.Decompress)
        Dim buf(4095) As Byte
        Do
            Dim len = ds.Read(buf, 0, buf.Length)
            If len = 0 Then Exit Do
            ms2.Write(buf, 0, len)
        Loop
        ds.Close()
        ms2.Close()
        ms1.Close()
        Return ms2.ToArray
    End Function

    Sub Main()
        Dim buf1 = File.ReadAllBytes("C:\Ruby\bin\ruby.exe")
        Console.WriteLine("buf1.Length: {0}", buf1.Length)

        Dim t1 = DateTime.Now
        Dim ms1 = New MemoryStream()
        Dim ds1 = New DeflateStream(ms1, CompressionMode.Compress)
        ds1.Write(buf1, 0, buf1.Length)
        ds1.Close()
        ms1.Close()
        Dim buf2 = ms1.ToArray()
        Dim t2 = DateTime.Now
        Console.WriteLine("compress time: {0}", t2 - t1)
        Console.WriteLine("buf2.Length: {0} (DeflateStream)", buf2.Length)

        Dim buf3 = GetDecompress(buf2)
        Console.WriteLine("buf3.Length: {0}", buf3.Length)
        Console.WriteLine("buf1 = buf3: {0}", CheckBytes(buf1, buf3))

        Dim t3 = DateTime.Now
        Dim ms2 = New MemoryStream(buf1)
        Dim buf4 = Deflate.GetCompressBytes(ms2)
        ms2.Close()
        Dim t4 = DateTime.Now
        Console.WriteLine("compress time: {0}", t4 - t3)
        Console.WriteLine("buf4.Length: {0} (original)", buf4.Length)

        Dim buf5 = GetDecompress(buf4)
        Console.WriteLine("buf5.Length: {0}", buf5.Length)
        Console.WriteLine("buf1 = buf5: {0}", CheckBytes(buf1, buf5))
    End Sub
End Module
