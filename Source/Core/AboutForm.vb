' KOCH Send to PrusaSlicer - About window (KOCH Engineering, Henrik J. Koch, Denmark) - common to all KOCH add-ons
Imports System.Drawing
Imports System.Windows.Forms

Namespace KochPrusaSlicer

    Friend Class AboutForm
        Inherits Form

        Private Const LinkedIn As String = "https://www.linkedin.com/in/henrikjkoch/"
        Private Const GitHub As String = "https://github.com/KOCH-Engineering/alibre-send-to-prusaslicer"

        Public Sub New()
            Text = "About"
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False : MinimizeBox = False : ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            BackColor = Color.White
            Font = New Font("Segoe UI", 9.0F)
            ClientSize = New Size(360, 330)

            Dim y = 18
            Try
                ' the chosen slicer's logo + arrow, drawn now (so it follows Settings at once); else the icon file
                Dim img As Image = IconMaker.Preview(SlicerLocator.Resolve(Settings.Load()), 64)
                If img Is Nothing Then
                    Dim ico = IO.Path.Combine(Plugin.AddOnFolder, IconMaker.RibbonIcon)
                    If IO.File.Exists(ico) Then
                        Using i As New Icon(ico, 64, 64)
                            img = i.ToBitmap()
                        End Using
                    End If
                End If
                If img IsNot Nothing Then
                    Controls.Add(New PictureBox With {.Image = img, .SizeMode = PictureBoxSizeMode.CenterImage,
                                                      .Size = New Size(64, 64), .Location = New Point((ClientSize.Width - 64) \ 2, y)})
                    y += 72
                End If
            Catch
            End Try

            y = Centered("KOCH Send to Slicer", New Font("Segoe UI", 12.0F, FontStyle.Bold), Color.Black, y) + 2
            y = Centered("for Alibre Design · version " & Plugin.Version, Font, Color.Gray, y) + 16
            y = CenteredLink("KOCH Engineering", New Font("Segoe UI", 11.0F, FontStyle.Bold), LinkedIn, y)
            y = CenteredLink("Henrik J. Koch", New Font("Segoe UI", 10.0F), LinkedIn, y)
            y = Centered("Denmark", New Font("Segoe UI", 10.0F), Color.DimGray, y)
            y = Centered("© 2026 KOCH Engineering", Font, Color.DimGray, y + 6) + 10
            y = CenteredLink("Source code and user guide on GitHub", Font, GitHub, y)
            y = Centered("With a lot of help from Claude (Anthropic)", New Font("Segoe UI", 8.0F, FontStyle.Italic), Color.Gray, y + 4)

            Dim ok As New Button With {.Text = "Close", .Size = New Size(84, 28), .DialogResult = DialogResult.OK}
            ok.Location = New Point((ClientSize.Width - ok.Width) \ 2, y + 12)
            Controls.Add(ok)
            AcceptButton = ok : CancelButton = ok
            ClientSize = New Size(ClientSize.Width, ok.Bottom + 14)
        End Sub

        Private Function Centered(text As String, f As Font, c As Color, y As Integer) As Integer
            Dim l As New Label With {.Text = text, .Font = f, .ForeColor = c, .AutoSize = False,
                                     .TextAlign = ContentAlignment.MiddleCenter, .Location = New Point(0, y),
                                     .Size = New Size(ClientSize.Width, TextRenderer.MeasureText(text, f).Height + 2)}
            Controls.Add(l)
            Return l.Bottom
        End Function

        Private Function CenteredLink(text As String, f As Font, url As String, y As Integer) As Integer
            Dim l As New LinkLabel With {.Text = text, .Font = f, .AutoSize = False, .LinkBehavior = LinkBehavior.HoverUnderline,
                                         .LinkColor = Color.FromArgb(&H18, &H5F, &HA5), .TextAlign = ContentAlignment.MiddleCenter,
                                         .Location = New Point(0, y), .Size = New Size(ClientSize.Width, TextRenderer.MeasureText(text, f).Height + 2)}
            AddHandler l.LinkClicked, Sub()
                                          Try
                                              Diagnostics.Process.Start(url)
                                          Catch
                                          End Try
                                      End Sub
            Controls.Add(l)
            Return l.Bottom
        End Function
    End Class

End Namespace
