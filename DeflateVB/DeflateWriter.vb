' public domain

Imports System.IO

Public Class DeflateWriter
    Private Const maxbuf% = maxdist * 2
    Private Const buflen% = maxbuf + maxlen
    Private length%, bufstart%
    Private buf(buflen - 1) As Byte
    Private tables%(4095, 15), current%(4095)

    Private Sub Read(sin As Stream, pos%, len%)
        Dim rlen = sin.Read(buf, pos, len)
        If rlen < len Then length = pos + rlen
    End Sub

    Private Function GetHash%(b1%, b2%, b3%)
        GetHash = (b1 << 4) Xor (b2 << 2) Xor b3
    End Function

    Private Sub AddHash(pos%)
        Dim h%, c%
        Dim b1 As Byte, b2 As Byte
        b1 = buf(pos)
        b2 = buf(pos + 1)
        If b1 <> b2 Then
            h = GetHash(b1, b2, buf(pos + 2))
            c = current(h)
            tables(h, c And 15) = bufstart + pos
            current(h) = c + 1
        End If
    End Sub

    Private Sub Search(pos%, ByRef rp%, ByRef rl%)
        Dim maxp% = -1, maxl% = 2, mlen%, last%, h%, c%, p1%, i%, p%, len%
        mlen = Math.Min(maxlen, length - pos)
        last = Math.Max(0, pos - maxdist)
        h = GetHash(buf(pos), buf(pos + 1), buf(pos + 2))
        c = current(h)
        p1 = IIf(c < 16, 0, c - 16)
        For i = c - 1 To p1 Step -1
            p = tables(h, i And 15) - bufstart
            If p < last Then
                Exit For
            Else
                len = 0
                While len < mlen And buf(p + len) = buf(pos + len)
                    len = len + 1
                End While
                If len > maxl Then
                    maxp = p
                    maxl = len
                End If
            End If
        Next
        rp = maxp
        rl = maxl
    End Sub

    Public Sub Compress(sin As Stream, sout As Stream)
        length = buflen
        bufstart = 0
        Array.Clear(current, 0, current.Length)
        Read(sin, 0, buflen)

        Dim bw As New BitWriter, b As Byte
        Dim p%, len%, mlen%, maxp%, maxl%, i%
        bw.sout = sout
        bw.WriteBit(True)
        bw.WriteBits(2, 1)
        While p < length
            b = buf(p)
            If p < length - 4 And b = buf(p + 1) And b = buf(p + 2) And b = buf(p + 3) Then
                len = 4
                mlen = Math.Min(maxlen + 1, length - p)
                While len < mlen And b = buf(p + len)
                    len = len + 1
                End While
                bw.WriteFixedHuffman(b)
                bw.WriteLen(len - 1)
                bw.WriteDist(1)
                p = p + len
            Else
                Search(p, maxp, maxl)
                If maxp < 0 Then
                    bw.WriteFixedHuffman(b)
                    AddHash(p)
                    p = p + 1
                Else
                    bw.WriteLen(maxl)
                    bw.WriteDist(p - maxp)
                    For i = p To p + maxl - 1
                        AddHash(i)
                    Next
                    p = p + maxl
                End If
            End If
            If p > maxbuf Then
                Array.Copy(buf, maxdist, buf, 0, maxdist + maxlen)
                If length < buflen Then
                    length = length - maxdist
                Else
                    Read(sin, maxdist + maxlen, maxdist)
                End If
                p = p - maxdist
                bufstart = bufstart + maxdist
            End If
        End While
        bw.WriteFixedHuffman(256)
        bw.Close()
    End Sub
End Class
