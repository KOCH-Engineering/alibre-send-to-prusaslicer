' KOCH Send to PrusaSlicer - builds the ribbon icon from the PrusaSlicer installed on this PC
'
' Called once by the installer (install.ps1 / setup.exe): takes the icon of the user's own
' prusa-slicer.exe and adds the common KOCH Engineering orange arrow. Nothing of PrusaSlicer is
' shipped with the add-on; if PrusaSlicer is not found, the add-on's own icon is kept.
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Runtime.InteropServices

Namespace KochPrusaSlicer

    Public Module IconMaker

        <DllImport("user32.dll", CharSet:=CharSet.Unicode)>
        Private Function PrivateExtractIcons(file As String, index As Integer, cx As Integer, cy As Integer,
                                             icons As IntPtr(), ids As Integer(), count As Integer, flags As Integer) As Integer
        End Function
        <DllImport("user32.dll")> Private Function DestroyIcon(h As IntPtr) As Boolean
        End Function

        ''' <summary>Writes outIco from the PrusaSlicer icon + orange arrow. Returns "" or an error text.</summary>
        Public Function Build(outIco As String, Optional slicerExe As String = "") As String
            Try
                If slicerExe = "" Then slicerExe = SlicerLocator.Find()
                If slicerExe = "" OrElse Not File.Exists(slicerExe) Then Return "no slicer found"
                Using src = Extract(slicerExe, 256)
                    If src Is Nothing Then Return "No icon in " & slicerExe
                    Dim sizes = {16, 20, 24, 32, 40, 48, 64, 128, 256}
                    Dim pngs As New List(Of Byte())
                    For Each sz In sizes
                        Using bmp = Compose(src, sz), ms As New MemoryStream()
                            bmp.Save(ms, ImageFormat.Png)
                            pngs.Add(ms.ToArray())
                        End Using
                    Next
                    WriteIco(outIco, sizes, pngs)
                End Using
                Return ""
            Catch ex As Exception
                Return ex.Message
            End Try
        End Function

        Private Function Extract(exe As String, size As Integer) As Bitmap
            Dim h(0) As IntPtr
            Dim ids(0) As Integer
            If PrivateExtractIcons(exe, 0, size, size, h, ids, 1, 0) < 1 OrElse h(0) = IntPtr.Zero Then Return Nothing
            Try
                Using ic = Icon.FromHandle(h(0))
                    Return ic.ToBitmap()
                End Using
            Finally
                DestroyIcon(h(0))
            End Try
        End Function

        ''' <summary>PrusaSlicer's icon, slightly smaller towards the top right, with the KOCH arrow bottom left pointing into it.</summary>
        Private Function Compose(src As Bitmap, size As Integer) As Bitmap
            Dim b As New Bitmap(size, size, PixelFormat.Format32bppArgb)
            Using g = Graphics.FromImage(b)
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.InterpolationMode = InterpolationMode.HighQualityBicubic
                g.PixelOffsetMode = PixelOffsetMode.HighQuality
                Dim k = size / 1024.0F
                g.DrawImage(src, 124 * k, 0, 900 * k, 900 * k)
                ' the common arrow (same shape as on Export Parts), bottom left, pointing INTO the slicer
                Dim x0 = 14 * k, y0 = 590 * k, w = 540 * k, h = 400 * k
                Dim sh = h * 0.44F, head = w * 0.46F
                Dim pts = {New PointF(x0, y0 + h / 2 - sh / 2), New PointF(x0 + w - head, y0 + h / 2 - sh / 2), New PointF(x0 + w - head, y0),
                           New PointF(x0 + w, y0 + h / 2), New PointF(x0 + w - head, y0 + h), New PointF(x0 + w - head, y0 + h / 2 + sh / 2),
                           New PointF(x0, y0 + h / 2 + sh / 2)}
                Using fill As New SolidBrush(Color.FromArgb(245, 140, 30)),
                      pen As New Pen(Color.FromArgb(30, 30, 34), Math.Max(1.0F, 28 * k)) With {.LineJoin = LineJoin.Miter}
                    g.FillPolygon(fill, pts)
                    g.DrawPolygon(pen, pts)
                End Using
            End Using
            Return b
        End Function

        ''' <summary>ICO file with PNG-compressed images (Windows Vista and later).</summary>
        Private Sub WriteIco(path As String, sizes As Integer(), pngs As List(Of Byte()))
            Using fs As New FileStream(path, FileMode.Create), w As New BinaryWriter(fs)
                w.Write(CShort(0)) : w.Write(CShort(1)) : w.Write(CShort(sizes.Length))
                Dim offset = 6 + 16 * sizes.Length
                For i = 0 To sizes.Length - 1
                    Dim s = sizes(i)
                    w.Write(CByte(If(s >= 256, 0, s))) : w.Write(CByte(If(s >= 256, 0, s)))
                    w.Write(CByte(0)) : w.Write(CByte(0))
                    w.Write(CShort(1)) : w.Write(CShort(32))
                    w.Write(pngs(i).Length) : w.Write(offset)
                    offset += pngs(i).Length
                Next
                For Each p In pngs
                    w.Write(p)
                Next
            End Using
        End Sub

    End Module

End Namespace
