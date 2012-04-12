' public domain

Imports System.IO

Public Class BitWriter
    Public sout As Stream
    Private buf(4095) As Byte
    Private bufp, cur, bit As Integer

    Public Sub WriteByte(b As Byte)
        buf(bufp) = b
        If bufp < 4095 Then
            bufp = bufp + 1
        Else
            sout.Write(buf, 0, buf.Length)
            bufp = 0
        End If
    End Sub

    Public Sub Close()
        If bit > 0 Then
            WriteByte(cur)
            cur = 0
            bit = 0
        End If
        If bufp > 0 Then
            sout.Write(buf, 0, bufp)
            bufp = 0
        End If
        sout.Flush()
    End Sub

    Public Sub WriteBit(b As Boolean)
        If b Then cur = cur Or sl(1, bit, 0)
        If bit < 7 Then
            bit = bit + 1
        Else
            WriteByte(cur)
            cur = 0
            bit = 0
        End If
    End Sub

    Public Sub WriteBits(len%, b%)
        Dim v%, pos%
        If len > 0 Then
            v = cur Or sl(b, bit, 0)
            pos = bit + len
            If pos < 8 Then
                cur = v
                bit = pos
            Else
                WriteByte(v)
                If pos < 16 Then
                    cur = sl(b, bit, 1)
                    bit = pos - 8
                Else
                    WriteByte(sl(b, bit, 1))
                    cur = sl(b, bit, 2)
                    bit = pos - 16
                End If
            End If
        End If
    End Sub

    Public Sub WriteFixedHuffman(b%)
        If b < 144 Then
            WriteBits(8, rev(b + 48))
        ElseIf b < 256 Then
            WriteBit(True)
            WriteBits(8, rev(b))
        ElseIf b < 280 Then
            WriteBits(7, rev((b - 256) << 1))
        ElseIf b < 288 Then
            WriteBits(8, rev(b - 88))
        End If
    End Sub

    Public Sub WriteLen(length%)
        Dim ll%
        ll = litindex(length - 3)
        WriteFixedHuffman(ll)
        WriteBits(litexlens(ll), length - litlens(ll))
    End Sub

    Public Sub WriteDist(d%)
        Dim dl%
        dl = distindex(d - 1)
        WriteBits(5, rev(dl << 3))
        WriteBits(distexlens(dl), d - distlens(dl))
    End Sub
End Class
