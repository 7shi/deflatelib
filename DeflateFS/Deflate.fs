﻿// public domain

module Deflate

open System
open System.Collections.Generic
open System.IO

let maxdist = 32768
let maxlen = 258

let litexlens =
    [| for i in 0..285 -> if i < 265 || i = 285 then 0 else (i - 261) >>> 2 |]

let litindex = Array.zeroCreate<int> (maxlen - 3 + 1)

let litlens =
    let litlens = Array.zeroCreate<int> 286
    let mutable v = 3
    for i = 257 to 284 do
        litlens.[i] <- v
        let p2 = 1 <<< litexlens.[i]
        for j = 1 to p2 do
            litindex.[v - 3] <- i
            v <- v + 1
    litlens.[285] <- maxlen
    litindex.[maxlen - 3] <- 285
    litlens

let distexlens =
    [| for i in 0..29 -> if i < 4 then 0 else (i - 2) >>> 1 |]

let distindex = Array.zeroCreate<int> (maxdist - 1 + 1)

let distlens =
    let distlens = Array.zeroCreate<int> 30
    let mutable v = 1
    for i = 0 to 29 do
        distlens.[i] <- v
        let p2 = 1 <<< distexlens.[i]
        for j = 1 to p2 do
            distindex.[v - 1] <- i
            v <- v + 1
    distlens

let sl = [| for i in 0..8191 ->
             [| for j in 0..7 ->
                 let v = i <<< j
                 [| byte(v); byte(v >>> 8); byte(v >>> 16) |] |] |]

let rev = [| for i in 0..255 ->
              let mutable b = 0
              for j = 0 to 7 do if (i &&& (1 <<< j)) <> 0 then b <- b + (128 >>> j)
              b |]

type BitWriter(sout:Stream) =
    let buf = Array.zeroCreate<byte> 4096
    let mutable bufp = 0
    let mutable cur = 0uy
    let mutable bit = 0
    
    member x.WriteByte(b:byte) =
        buf.[bufp] <- b
        if bufp < 4095 then bufp <- bufp + 1 else
            sout.Write(buf, 0, buf.Length)
            bufp <- 0

    member x.Close() =
        if bit > 0 then
            x.WriteByte(cur)
            cur <- 0uy
            bit <- 0
        if bufp > 0 then
            sout.Write(buf, 0, bufp)
            bufp <- 0
        sout.Flush()
    
    member x.WriteBit(b:bool) =
        if b then cur <- cur ||| (1uy <<< bit)
        if bit < 7 then
            bit <- bit + 1
        else
            x.WriteByte(cur)
            cur <- 0uy
            bit <- 0
    
    member x.WriteBits (len:int) (b:int) =
        if len > 0 then
            let s = sl.[b].[bit]
            let v = cur ||| s.[0]
            let pos = bit + len
            if pos < 8 then
                cur <- v
                bit <- pos
            else
                x.WriteByte(v)
                if pos < 16 then
                    cur <- s.[1]
                    bit <- pos - 8
                else
                    x.WriteByte(s.[1])
                    cur <- s.[2]
                    bit <- pos - 16

    member x.WriteFixedHuffman (b:int) =
        if b < 144 then
            x.WriteBits 8 rev.[b + 0b110000]
        elif b < 256 then
            x.WriteBit true
            x.WriteBits 8 rev.[b]
        elif b < 280 then
            x.WriteBits 7 rev.[(b - 256) <<< 1]
        elif b < 288 then
            x.WriteBits 8 rev.[b - 280 + 0b11000000]
    
    member x.WriteLen (len:int) =
        let ll = litindex.[len - 3]
        x.WriteFixedHuffman ll
        x.WriteBits litexlens.[ll] (len - litlens.[ll])
    
    member x.WriteDist (d:int) =
        let dl = distindex.[d - 1]
        x.WriteBits 5 rev.[dl <<< 3]
        x.WriteBits distexlens.[dl] (d - distlens.[dl])

let maxbuf = maxdist * 2
let buflen = maxbuf + maxlen

let inline getHash (buf:byte[]) pos =
    ((int buf.[pos]) <<< 4) ^^^ ((int buf.[pos + 1]) <<< 2) ^^^ (int buf.[pos + 2])

let inline addHash (tables:int[,]) (counts:int[]) (buf:byte[]) bufstart pos =
    if buf.[pos] <> buf.[pos + 1] then
        let h = getHash buf pos
        let c = counts.[h]
        tables.[h, c &&& 15] <- bufstart + pos
        counts.[h] <- c + 1

type Writer(sin:Stream) =
    let mutable length = buflen
    let mutable bufstart = 0
    let buf = Array.zeroCreate<byte> buflen
    let tables = Array2D.zeroCreate<int> 4096 16
    let counts = Array.zeroCreate<int> 4096
    
    let read pos len =
        let rlen = sin.Read(buf, pos, len)
        if rlen < len then length <- pos + rlen
    
    do
        read 0 buflen
    
    let search (pos:int) =
        let mutable maxp = -1
        let mutable maxl = 2
        let mlen = Math.Min(maxlen, length - pos)
        let last = Math.Max(0, pos - maxdist)
        let h = getHash buf pos
        let c = counts.[h]
        let p1 = Math.Max(0, c - 16)
        let mutable i = c - 1
        while i >= p1 do
            let p = tables.[h, i &&& 15] - bufstart
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
        let bw = new BitWriter(sout)
        bw.WriteBit true
        bw.WriteBits 2 1
        let mutable p = 0
        while p < length do
            let b = buf.[p]
            if p < length - 4 && b = buf.[p + 1] && b = buf.[p + 2] && b = buf.[p + 3] then
                let mutable len = 4
                let mlen = Math.Min(maxlen + 1, length - p)
                while len < mlen && b = buf.[p + len] do
                    len <- len + 1
                bw.WriteFixedHuffman(int b)
                bw.WriteLen(len - 1)
                bw.WriteDist 1
                p <- p + len
            else
                let maxp, maxl = search p
                if maxp < 0 then
                    bw.WriteFixedHuffman(int b)
                    addHash tables counts buf bufstart p
                    p <- p + 1
                else
                    bw.WriteLen maxl
                    bw.WriteDist (p - maxp)
                    for i = p to p + maxl - 1 do
                        addHash tables counts buf bufstart i
                    p <- p + maxl
            if p > maxbuf then
                Array.Copy(buf, maxdist, buf, 0, maxdist + maxlen)
                if length < buflen then length <- length - maxdist else
                    read (maxdist + maxlen) maxdist
                p <- p - maxdist
                bufstart <- bufstart + maxdist
        bw.WriteFixedHuffman 256
        bw.Close()

let Compress (sin:Stream) (sout:Stream) =
    let w = new Writer(sin)
    w.Compress sout

let GetCompressBytes (sin:Stream) =
    let ms = new MemoryStream()
    Compress sin ms
    ms.Close()
    ms.ToArray()
