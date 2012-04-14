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

    Private buf1 As Byte()

    Sub TestLoop(n%, f As Func(Of Tuple(Of TimeSpan, Byte())))
        Dim times = 0.0
        For i = 1 To n
            Dim result = f(), ts = result.Item1, buf2 = result.Item2
            Dim buf3 = GetDecompress(buf2)
            Dim check = If(CheckBytes(buf1, buf3), "OK", "NG")
            Console.WriteLine("[{0}:{1}] Length: {2}, Time: {3}", i, check, buf2.Length, ts)
            times += ts.TotalSeconds
        Next
        If n > 1 Then Console.WriteLine("[1 - {0}] Times: {1}", n, TimeSpan.FromSeconds(times / n))
    End Sub

    Sub Main()
        Const target_in = "C:\Ruby\bin\ruby.exe"
        Const target_out = "C:\vb-ruby.exe.deflate"

        Console.WriteLine("[target file]")
        Console.WriteLine("Length: {0}", New FileInfo(target_in).Length)

        Console.WriteLine("")
        Console.WriteLine("[myimpl (file I/O)]")

        Do
            Dim ts As TimeSpan
            Using sin = New FileStream(target_in, FileMode.Open),
                sout = New FileStream(target_out, FileMode.Create)
                Dim t1 = DateTime.Now
                Compress(sin, sout)
                ts = DateTime.Now - t1
            End Using
            Dim buf2 = File.ReadAllBytes(target_out)
            Console.WriteLine("Length: {0}, Time: {1}", buf2.Length, ts)
        Loop While False

        buf1 = File.ReadAllBytes(target_in)

        Console.WriteLine("")
        Console.WriteLine("[myimpl (on memory)]")
        TestLoop(5, Function()
                        Dim t1 = DateTime.Now
                        Dim ms = New MemoryStream(buf1)
                        Dim buf2 = Deflate.GetCompressBytes(ms)
                        ms.Close()
                        Return Tuple.Create(DateTime.Now - t1, buf2)
                    End Function)

        Console.WriteLine("")
        Console.WriteLine("[DeflateStream (on memory)]")
        TestLoop(5, Function()
                        Dim t1 = DateTime.Now
                        Dim ms = New MemoryStream()
                        Dim ds = New DeflateStream(ms, CompressionMode.Compress)
                        ds.Write(buf1, 0, buf1.Length)
                        ds.Close()
                        ms.Close()
                        Dim buf2 = ms.ToArray()
                        Return Tuple.Create(DateTime.Now - t1, buf2)
                    End Function)
    End Sub
End Module
