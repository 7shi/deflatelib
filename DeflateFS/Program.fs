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

let buf1 = File.ReadAllBytes(@"C:\Ruby\bin\ruby.exe")
printfn "buf1.Length: %d" buf1.Length

let t1 = DateTime.Now
let ms1 = new MemoryStream()
let ds1 = new DeflateStream(ms1, CompressionMode.Compress)
ds1.Write(buf1, 0, buf1.Length)
ds1.Close()
ms1.Close()
let buf2 = ms1.ToArray()
let t2 = DateTime.Now
printfn "compress time: %s" ((t2 - t1).ToString())
printfn "buf2.Length: %d (DeflateStream)" buf2.Length

let buf3 = GetDecompress buf2
printfn "buf3.Length: %d" buf3.Length
printfn "buf1 = buf3: %b" (CheckBytes buf1 buf3)

let t3 = DateTime.Now
let ms2 = new MemoryStream(buf1)
let buf4, _ = Deflate.GetCompressBytes(ms2)
ms2.Close()
let t4 = DateTime.Now
printfn "compress time: %s" ((t4 - t3).ToString())
printfn "buf4.Length: %d (original)" buf4.Length

let buf5 = GetDecompress buf4
printfn "buf5.Length: %d" buf5.Length
printfn "buf1 = buf5: %b" (CheckBytes buf1 buf5)
