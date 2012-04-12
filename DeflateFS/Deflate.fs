// public domain

module Deflate

open System
open System.Collections.Generic
open System.IO

let maxbuf = 32768
let maxlen = 258

let getLitExLen v = if v < 265 || v = 285 then 0 else (v - 261) >>> 2
let getDistExLen d = if d < 4 then 0 else (d - 2) >>> 1

let litlens =
    let litlens = Array.zeroCreate<int> 286
    let mutable v = 3
    for i = 257 to 284 do
        litlens.[i] <- v
        v <- v + (1 <<< (getLitExLen i))
    litlens.[285] <- maxlen
    litlens.[257..285]

let distlens =
    let distlens = Array.zeroCreate<int> 30
    let mutable v = 1
    for i = 0 to 29 do
        distlens.[i] <- v
        v <- v + (1 <<< (getDistExLen i))
    distlens

type BitWriter(sout:Stream) =
    let mutable bit = 0
    let mutable cur = 0uy
    
    member x.Skip() =
        if bit > 0 then
            sout.WriteByte(cur)
            bit <- 0
            cur <- 0uy
    
    interface IDisposable with
        member x.Dispose() =
            x.Skip()
            sout.Flush()
    
    member x.WriteBit(b:int) =
        cur <- cur ||| ((byte b) <<< bit)
        bit <- bit + 1
        if bit = 8 then
            sout.WriteByte(cur)
            bit <- 0
            cur <- 0uy
    
    member x.WriteLE (len:int) (b:int) =
        for i = 0 to len - 1 do
            x.WriteBit <| if (b &&& (1 <<< i)) = 0 then 0 else 1
    
    member x.WriteBE (len:int) (b:int) =
        for i = len - 1 downto 0 do
            x.WriteBit <| if (b &&& (1 <<< i)) = 0 then 0 else 1
    
    member x.WriteBytes(data:byte[]) =
        x.Skip()
        sout.Write(data, 0, data.Length)

type FixedHuffmanWriter(bw:BitWriter) =
    member x.Write (b:int) =
        if b < 144 then
            bw.WriteBE 8 (b + 0b110000)
        elif b < 256 then
            bw.WriteBE 9 (b - 144 + 0b110010000)
        elif b < 280 then
            bw.WriteBE 7 (b - 256)
        elif b < 288 then
            bw.WriteBE 8 (b - 280 + 0b11000000)
    
    member x.WriteLen (len:int) =
        if len < 3 || len > maxlen then
            failwith <| sprintf "不正な長さ: %d" len
        let mutable ll = 285
        while len < litlens.[ll - 257] do
            ll <- ll - 1
        x.Write ll
        bw.WriteLE (getLitExLen ll) (len - litlens.[ll - 257])
    
    member x.WriteDist (d:int) =
        if d < 1 || d > maxbuf then
            failwith <| sprintf "不正な距離: %d" d
        let mutable dl = 29
        while d < distlens.[dl] do
            dl <- dl - 1
        bw.WriteBE 5 dl
        bw.WriteLE (getDistExLen dl) (d - distlens.[dl])

let maxbuf2 = maxbuf * 2
let buflen = maxbuf2 + maxlen

let inline getHash (buf:byte[]) pos =
    ((int buf.[pos]) <<< 4) ^^^ ((int buf.[pos + 1]) <<< 2) ^^^ (int buf.[pos + 2])

let inline addHash (hash:List<int>[]) (buf:byte[]) pos =
    if buf.[pos] <> buf.[pos + 1] then
        hash.[getHash buf pos].Add pos

let inline addHash2 (tables:int[,]) (counts:int[]) (buf:byte[]) pos =
    if buf.[pos] <> buf.[pos + 1] then
        let h = getHash buf pos
        let c = counts.[h]
        tables.[h, c &&& 15] <- pos
        counts.[h] <- c + 1

type Writer(t:int, sin:Stream) =
    let mutable length = buflen
    let buf = Array.zeroCreate<byte> buflen
    let tables, counts =
        if t = 2 then Array2D.zeroCreate<int> 4096 16, Array.create 4096 0 else null, null
    let hash = if tables = null then [| for _ in 0..4095 -> new List<int>() |] else null
    
    let read pos len =
        let rlen = sin.Read(buf, pos, len)
        if rlen < len then length <- pos + rlen
        if hash <> null then
            for list in hash do list.Clear()
        else
            Array.fill counts 0 counts.Length 0
    
    do
        read 0 buflen
    
    let search (pos:int) =
        let mutable maxp = -1
        let mutable maxl = 2
        let mlen = Math.Min(maxlen, length - pos)
        let last = Math.Max(0, pos - maxbuf)
        let h = getHash buf pos
        if hash <> null then
            let list = hash.[h]
            let mutable i = list.Count - 1
            while i >= 0 do
                let p = list.[i]
                if p < last then i <- 0 else
                    let mutable len = 0
                    while len < mlen && buf.[p + len] = buf.[pos + len] do
                        len <- len + 1
                    if len > maxl then
                        maxp <- p
                        maxl <- len
                i <- i - 1
        else
            let c = counts.[h]
            let p1, p2 = if c < 16 then 0, c - 1 else c + 1, c + 16
            let mutable i = p2
            while i >= p1 do
                let p = tables.[h, i &&& 15]
                if p < last then i <- 0 else
                    let mutable len = 0
                    while len < mlen && buf.[p + len] = buf.[pos + len] do
                        len <- len + 1
                    if len > maxl then
                        maxp <- p
                        maxl <- len
                i <- i - 1
        maxp, maxl

    member x.Compress (sout:Stream) =
        use bw = new BitWriter(sout)
        bw.WriteBit 1
        bw.WriteLE 2 1
        let hw = new FixedHuffmanWriter(bw)
        let mutable p = 0
        match t with
        | 2 ->
            while p < length do
                let b = buf.[p]
                if p < length - 4 && b = buf.[p + 1] && b = buf.[p + 2] && b = buf.[p + 3] then
                    let mutable len = 4
                    let mlen = Math.Min(maxlen + 1, length - p)
                    while len < mlen && b = buf.[p + len] do
                        len <- len + 1
                    hw.Write(int b)
                    hw.WriteLen(len - 1)
                    hw.WriteDist 1
                    p <- p + len
                else
                    let maxp, maxl = search p
                    if maxp < 0 then
                        hw.Write(int b)
                        addHash2 tables counts buf p
                        p <- p + 1
                    else
                        hw.WriteLen maxl
                        hw.WriteDist (p - maxp)
                        for i = p to p + maxl - 1 do
                            addHash2 tables counts buf i
                        p <- p + maxl
                if p > maxbuf2 then
                    Array.Copy(buf, maxbuf, buf, 0, maxbuf + maxlen)
                    if length < buflen then length <- length - maxbuf else
                        read (maxbuf + maxlen) maxbuf
                    p <- p - maxbuf
                    for i = 0 to p - 1 do
                        addHash2 tables counts buf i
        | 1 ->
            while p < length do
                let b = buf.[p]
                if p < length - 4 && b = buf.[p + 1] && b = buf.[p + 2] && b = buf.[p + 3] then
                    let mutable len = 4
                    let mlen = Math.Min(maxlen + 1, length - p)
                    while len < mlen && b = buf.[p + len] do
                        len <- len + 1
                    hw.Write(int b)
                    hw.WriteLen(len - 1)
                    hw.WriteDist 1
                    p <- p + len
                else
                    let maxp, maxl = search p
                    if maxp < 0 then
                        hw.Write(int b)
                        addHash hash buf p
                        p <- p + 1
                    else
                        hw.WriteLen maxl
                        hw.WriteDist (p - maxp)
                        for i = p to p + maxl - 1 do
                            addHash hash buf i
                        p <- p + maxl
                if p > maxbuf2 then
                    Array.Copy(buf, maxbuf, buf, 0, maxbuf + maxlen)
                    if length < buflen then length <- length - maxbuf else
                        read (maxbuf + maxlen) maxbuf
                    p <- p - maxbuf
                    for i = 0 to p - 1 do
                        addHash hash buf i
        | _ ->
            while p < length do
                let maxp, maxl = search p
                if maxp < 0 then
                    hw.Write(int buf.[p])
                    hash.[getHash buf p].Add p
                    p <- p + 1
                else
                    hw.WriteLen maxl
                    hw.WriteDist (p - maxp)
                    for i = p to p + maxl - 1 do
                        hash.[getHash buf i].Add i
                    p <- p + maxl
                if p > maxbuf2 then
                    Array.Copy(buf, maxbuf, buf, 0, maxbuf + maxlen)
                    if length < buflen then length <- length - maxbuf else
                        read (maxbuf + maxlen) maxbuf
                    p <- p - maxbuf
                    for i = 0 to p - 1 do
                        hash.[getHash buf i].Add i
        hw.Write 256

let GetCompressBytes (sin:Stream) =
    let ms = new MemoryStream()
    let w = new Writer(2, sin)
    w.Compress ms
    ms.Close()
    ms.ToArray()
