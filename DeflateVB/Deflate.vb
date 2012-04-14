' public domain

Imports System.IO

Module Deflate
    Public Const maxlen = 258, maxdist = 32768
    Public litexlens(285), litlens(285), litindex(maxlen - 3) As Integer
    Public distexlens(29), distlens(29), distindex(maxdist - 1) As Integer
    Public sl(8191, 7, 2) As Byte, rev(255) As Byte

    Public Sub Init()
        Dim I, J, V, P2 As Integer
        Dim P2R As Byte, B As Byte

        For I = 265 To 284
            litexlens(I) = (I - 261) >> 2
        Next I

        V = 3
        For I = 257 To 284
            litlens(I) = V
            P2 = 1
            For J = 1 To litexlens(I)
                P2 <<= 1
            Next J
            For J = 1 To P2
                litindex(V - 3) = I
                V += 1
            Next J
        Next I
        litlens(285) = maxlen
        litindex(maxlen - 3) = 285

        For I = 4 To 29
            distexlens(I) = (I - 2) \ 2
        Next I

        V = 1
        For I = 0 To 29
            distlens(I) = V
            P2 = 1
            For J = 1 To distexlens(I)
                P2 <<= 1
            Next
            For J = 1 To P2
                distindex(V - 1) = I
                V += 1
            Next J
        Next I

        For I = 0 To 8191
            P2 = 1
            For J = 0 To 7
                V = I * P2
                sl(I, J, 0) = V And 255
                sl(I, J, 1) = (V >> 8) And 255
                sl(I, J, 2) = V >> 16
                P2 = P2 + P2
            Next J
        Next I

        For I = 0 To 255
            P2 = 1
            P2R = 128
            B = 0
            For J = 0 To 7
                If I And P2 Then B = B + P2R
                P2 <<= 1
                P2R >>= 1
            Next J
            rev(I) = B
        Next I
    End Sub

    Public Sub Compress(sin As Stream, sout As Stream)
        Dim w = New DeflateWriter
        w.Compress(sin, sout)
    End Sub

    Public Function GetCompressBytes(sin As Stream) As Byte()
        If rev(255) = 0 Then Init()
        Dim ms = New MemoryStream
        Compress(sin, ms)
        ms.Close()
        GetCompressBytes = ms.ToArray()
    End Function
End Module
