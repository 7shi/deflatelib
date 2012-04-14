open System
open System.IO
open System.IO.Compression

let CheckBytes (buf1:byte[]) (buf2:byte[]) =
    if buf1.Length <> buf2.Length then false else
        let mutable ret = true
        let mutable i = 0
        while ret && i < buf1.Length do
            if buf1.[i] <> buf2.[i] then ret <- false
            i <- i + 1
        ret

let GetDecompress (buf:byte[]) =
    let ms1 = new MemoryStream(buf)
    let ms2 = new MemoryStream()
    let ds = new DeflateStream(ms1, CompressionMode.Decompress)
    let buf = Array.zeroCreate<byte> 4096
    let rec read() =
        let len = ds.Read(buf, 0, buf.Length)
        if len > 0 then
            ms2.Write(buf, 0, len)
            read()
    read()
    ds.Close()
    ms2.Close()
    ms1.Close()
    ms2.ToArray()

let target_in = @"C:\Ruby\bin\ruby.exe";
let target_out = @"C:\fs-ruby.exe.deflate";

printfn "[target file]"
printfn "Length: %d" ((new FileInfo(target_in)).Length)

printfn ""
printfn "[myimpl (file I/O)]"
do
    let ts =
        use sin = new FileStream(target_in, FileMode.Open)
        use sout = new FileStream(target_out, FileMode.Create)
        let t1 = DateTime.Now
        Deflate.Compress sin sout
        DateTime.Now - t1
    let buf2 = File.ReadAllBytes(target_out)
    printfn "Length: %d, Time: %s" buf2.Length (ts.ToString())

let buf1 = File.ReadAllBytes(target_in)

let loop n (f:unit -> TimeSpan * byte[]) =
    let mutable times = 0.0
    for i = 1 to n do
        let ts, buf2 = f()
        let buf3 = GetDecompress buf2
        let check = if CheckBytes buf1 buf3 then "OK" else "NG"
        printfn "[%d:%s] Length: %d, Time: %s" i check buf2.Length (ts.ToString())
        times <- times + (ts.TotalSeconds)
    if n > 1 then printfn "[1 - %d] Time: %s" n (TimeSpan.FromSeconds(times / (float n)).ToString())

printfn ""
printfn "[myimpl (on memory)]"
loop 5 <| fun() ->
    let t1 = DateTime.Now
    let ms = new MemoryStream(buf1)
    let buf2 = Deflate.GetCompressBytes(ms)
    ms.Close()
    DateTime.Now - t1, buf2

printfn ""
printfn "[DeflateStream (on memory)]"
loop 5 <| fun() ->
    let t1 = DateTime.Now
    let ms = new MemoryStream()
    let ds = new DeflateStream(ms, CompressionMode.Compress)
    ds.Write(buf1, 0, buf1.Length)
    ds.Close()
    ms.Close()
    let buf2 = ms.ToArray()
    DateTime.Now - t1, buf2
