' KOCH Send to PrusaSlicer - progress window and settings dialog
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

Namespace KochPrusaSlicer

    Friend Class ProgressForm
        Inherits Form

        Private ReadOnly _label As New Label With {.Location = New Point(16, 14), .Size = New Size(348, 20), .AutoEllipsis = True}
        Private ReadOnly _bar As New ProgressBar With {.Location = New Point(16, 40), .Size = New Size(348, 16)}

        Public Sub New()
            Text = Plugin.Title
            FormBorderStyle = FormBorderStyle.FixedDialog
            ControlBox = False : ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            Font = New Font("Segoe UI", 9.0F)
            ClientSize = New Size(380, 72)
            _label.Text = "Preparing…"
            Controls.Add(_label)
            Controls.Add(_bar)
        End Sub

        Protected Overrides Sub OnShown(e As EventArgs)
            MyBase.OnShown(e)
            If Owner Is Nothing Then CenterToScreen()
        End Sub

        Public Sub Report(name As String, index As Integer, total As Integer)
            _label.Text = String.Format("Exporting {0} of {1}: {2}", index, total, name)
            _bar.Maximum = Math.Max(1, total)
            _bar.Value = Math.Min(index, _bar.Maximum)
            Application.DoEvents()
        End Sub
    End Class

    Friend Class SettingsForm
        Inherits Form

        Private ReadOnly _s As Settings
        Private ReadOnly _path As New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDown}
        Private ReadOnly _found As New Label With {.ForeColor = Color.DimGray, .AutoSize = False, .AutoEllipsis = True}
        Private ReadOnly _copies As New CheckBox With {.Text = "One object per instance (4 identical bolts in the assembly = 4 objects)", .AutoSize = True}
        Private ReadOnly _sub As New CheckBox With {.Text = "Include parts in sub-assemblies", .AutoSize = True}
        Private ReadOnly _supp As New CheckBox With {.Text = "Skip suppressed parts", .AutoSize = True}
        Private ReadOnly _hidden As New CheckBox With {.Text = "Skip hidden parts", .AutoSize = True}
        Private ReadOnly _format As New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 150}
        Private ReadOnly _quality As New CheckBox With {.Text = "STL: use my STL export settings from Alibre (mesh quality)", .AutoSize = True}
        Private ReadOnly _temp As New RadioButton With {.Text = "Temporary folder", .AutoSize = True}
        Private ReadOnly _model As New RadioButton With {.Text = "A ""Print"" folder next to the model", .AutoSize = True}

        Private Shared ReadOnly Accent As Color = Color.FromArgb(&H18, &H5F, &HA5)

        Public Sub New(s As Settings)
            _s = s
            Text = "Send to PrusaSlicer – Settings"
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False : MinimizeBox = False : ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            BackColor = Color.White
            Font = New Font("Segoe UI", 9.0F)
            ClientSize = New Size(560, 562)

            Controls.Add(New Label With {.Text = "Send to PrusaSlicer", .Font = New Font("Segoe UI", 13.0F, FontStyle.Bold),
                                         .Location = New Point(18, 16), .AutoSize = True})
            Credits()

            Dim y = 80
            Section("PrusaSlicer", y) : y += 26
            _path.Location = New Point(20, y) : _path.Size = New Size(360, 24)
            For Each p In SlicerLocator.FindAll()
                _path.Items.Add(p)
            Next
            _path.Text = s.SlicerPath
            Controls.Add(_path)
            ' show the version next to each path in the list
            _path.DrawMode = DrawMode.OwnerDrawFixed
            _path.DropDownWidth = 520
            AddHandler _path.DrawItem, Sub(snd, ev)
                                           ev.DrawBackground()
                                           If ev.Index >= 0 Then
                                               Dim it = CStr(_path.Items(ev.Index))
                                               Dim v = SlicerLocator.VersionText(it)
                                               TextRenderer.DrawText(ev.Graphics, v & If(SlicerLocator.IsPreRelease(it), "  (pre-release)", "") & "   –   " & it,
                                                                     _path.Font, ev.Bounds, ev.ForeColor, TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis)
                                           End If
                                       End Sub
            Dim browse = Btn("Browse…", 388, y - 1, 74)
            AddHandler browse.Click, Sub()
                                         Dim p = SlicerLocator.Browse(Me, _path.Text)
                                         If p <> "" Then _path.Text = p
                                     End Sub
            Dim find = Btn("Find", 468, y - 1, 72)
            AddHandler find.Click, Sub()
                                       Dim p = SlicerLocator.Find()
                                       If p = "" Then
                                           MessageBox.Show(Me, "PrusaSlicer was not found. Use Browse… to point to prusa-slicer.exe.", Text)
                                       Else
                                           _path.Text = p
                                       End If
                                   End Sub
            y += 30
            _found.Location = New Point(20, y) : _found.Size = New Size(520, 18)
            Controls.Add(_found)
            AddHandler _path.TextChanged, Sub() UpdateFound()
            AddHandler _path.SelectedIndexChanged, Sub() UpdateFound()
            UpdateFound()

            y += 34
            Section("Assemblies", y) : y += 26
            For Each cb In {_copies, _sub, _supp, _hidden}
                cb.Location = New Point(22, y) : Controls.Add(cb) : y += 26
            Next
            y += 10
            Section("Export", y) : y += 26
            Controls.Add(New Label With {.Text = "File format:", .Location = New Point(22, y + 3), .AutoSize = True})
            _format.Items.AddRange(Settings.Formats)
            _format.Location = New Point(110, y) : Controls.Add(_format)
            Controls.Add(New Label With {.Text = "STEP: PrusaSlicer makes the mesh itself (smooth curves).", .ForeColor = Color.DimGray,
                                         .Location = New Point(268, y + 3), .AutoSize = True})
            y += 32
            _quality.Location = New Point(22, y) : Controls.Add(_quality) : y += 30
            AddHandler _format.SelectedIndexChanged, Sub() _quality.Enabled = (CStr(_format.SelectedItem) = "STL")
            Controls.Add(New Label With {.Text = "Save the files in:", .Location = New Point(22, y), .AutoSize = True}) : y += 22
            _temp.Location = New Point(36, y) : Controls.Add(_temp) : y += 24
            _model.Location = New Point(36, y) : Controls.Add(_model) : y += 24

            _copies.Checked = s.CopyPerInstance
            _sub.Checked = s.IncludeSubAssemblies
            _supp.Checked = s.SkipSuppressed
            _hidden.Checked = s.SkipHidden
            _quality.Checked = s.UseAlibreStlSettings
            _format.SelectedItem = s.Format
            _quality.Enabled = Not s.IsStep
            _model.Checked = s.ExportNextToModel
            _temp.Checked = Not s.ExportNextToModel

            Dim tip = New Label With {.Text = "Tip: in PrusaSlicer, tick Preferences › ""Allow just a single PrusaSlicer instance"" so new parts are added to the open window.",
                                      .ForeColor = Color.DimGray, .Location = New Point(20, y + 8), .Size = New Size(520, 34)}
            Controls.Add(tip)

            Dim ok = Btn("OK", 372, ClientSize.Height - 44, 80)
            Dim cancel = Btn("Cancel", 460, ClientSize.Height - 44, 80)
            ok.DialogResult = DialogResult.None
            cancel.DialogResult = DialogResult.Cancel
            AddHandler ok.Click, AddressOf OnOk
            AcceptButton = ok : CancelButton = cancel
        End Sub

        Private Sub UpdateFound()
            Dim p = _path.Text.Trim()
            If p = "" Then
                _found.Text = "Not set – the newest stable version found is used."
            ElseIf File.Exists(p) Then
                Dim v = SlicerLocator.VersionText(p)
                _found.Text = "✓ PrusaSlicer " & v & If(SlicerLocator.IsPreRelease(p), "  (pre-release)", "") &
                              If(_path.Items.Count > 1, "   –   " & _path.Items.Count & " versions found, pick one in the list", "")
                _found.ForeColor = Color.SeaGreen
                Return
            Else
                _found.Text = "File not found."
            End If
            _found.ForeColor = Color.DimGray
        End Sub

        Private Sub OnOk(sender As Object, e As EventArgs)
            Dim p = _path.Text.Trim()
            If p <> "" AndAlso Not File.Exists(p) Then
                MessageBox.Show(Me, "prusa-slicer.exe was not found at that path.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            _s.SlicerPath = p
            _s.CopyPerInstance = _copies.Checked
            _s.IncludeSubAssemblies = _sub.Checked
            _s.SkipSuppressed = _supp.Checked
            _s.SkipHidden = _hidden.Checked
            _s.UseAlibreStlSettings = _quality.Checked
            _s.Format = CStr(_format.SelectedItem)
            _s.ExportNextToModel = _model.Checked
            DialogResult = DialogResult.OK
        End Sub

        Private Sub Section(text As String, y As Integer)
            Controls.Add(New Label With {.Text = text, .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold), .ForeColor = Accent,
                                         .Location = New Point(18, y), .AutoSize = True})
        End Sub

        Private Function Btn(text As String, x As Integer, y As Integer, w As Integer) As Button
            Dim b As New Button With {.Text = text, .Location = New Point(x, y), .Size = New Size(w, 28), .FlatStyle = FlatStyle.System}
            Controls.Add(b)
            Return b
        End Function

        ''' <summary>KOCH Engineering / Henrik J. Koch (LinkedIn) / Denmark © 2026 - top right, centred.</summary>
        Private Sub Credits()
            Const bw = 200
            Dim bx = ClientSize.Width - 16 - bw
            Dim by = 10
            Dim tip As New ToolTip()
            Dim openLinkedIn As Action = Sub()
                                             Try
                                                 Diagnostics.Process.Start("https://www.linkedin.com/in/henrikjkoch/")
                                             Catch
                                             End Try
                                         End Sub
            For Each ln In {"KOCH Engineering", "Henrik J. Koch", "Denmark © 2026"}
                Dim bold = (ln = "KOCH Engineering")
                Dim f = If(bold, New Font("Segoe UI", 9.0F, FontStyle.Bold), New Font("Segoe UI", 8.25F))
                Dim h = TextRenderer.MeasureText(ln, f).Height
                Dim lk As New LinkLabel With {.Text = ln, .Font = f, .AutoSize = False, .TextAlign = ContentAlignment.MiddleCenter,
                                              .Location = New Point(bx, by), .Size = New Size(bw, h), .LinkBehavior = LinkBehavior.HoverUnderline,
                                              .LinkColor = If(bold, Color.FromArgb(&H33, &H33, &H33), Accent), .ActiveLinkColor = Accent,
                                              .ForeColor = Color.Gray}
                If ln = "Denmark © 2026" Then
                    lk.LinkArea = New LinkArea(0, 0)
                Else
                    AddHandler lk.LinkClicked, Sub() openLinkedIn()
                    tip.SetToolTip(lk, "KOCH Engineering · Henrik J. Koch · Denmark – version " & Plugin.Version)
                End If
                If ln = "Henrik J. Koch" Then
                    Dim tw = TextRenderer.MeasureText(ln, f).Width
                    Dim pic As New PictureBox With {.Size = New Size(17, 14), .Cursor = Cursors.Hand, .SizeMode = PictureBoxSizeMode.Zoom,
                                                    .Location = New Point(bx + (bw + tw) \ 2 + 2, by + (h - 14) \ 2 - 2)}
                    Try
                        Dim li = Path.Combine(Plugin.AddOnFolder, "linkedin.png")
                        If File.Exists(li) Then
                            Using src = Image.FromFile(li)
                                pic.Image = New Bitmap(src)
                            End Using
                        End If
                    Catch
                    End Try
                    AddHandler pic.Click, Sub() openLinkedIn()
                    tip.SetToolTip(pic, "Open LinkedIn")
                    If pic.Image IsNot Nothing Then Controls.Add(pic)
                End If
                Controls.Add(lk)
                by += h + 4
            Next
        End Sub
    End Class

End Namespace
