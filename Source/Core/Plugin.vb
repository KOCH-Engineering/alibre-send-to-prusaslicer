' KOCH Send to PrusaSlicer - core
' (c) 2026 KOCH Engineering (Henrik J. Koch, Denmark) - MIT License
'
' Created by the loader (KochPrusaSlicer.dll) with the add-on folder and a log callback.
' Alibre objects are used late-bound (Option Strict Off), so the core needs no Alibre references.

Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Windows.Forms

Namespace KochPrusaSlicer

    Public Class Plugin
        Implements IDisposable

        Public Const Title As String = "Send to PrusaSlicer"
        Public Shared Property AddOnFolder As String = ""
        Private Shared _log As Action(Of String)

        Public Shared Sub Log(message As String)
            Try
                _log?.Invoke(message)
            Catch
            End Try
        End Sub

        Public Shared ReadOnly Property Version As String
            Get
                Dim v = GetType(Plugin).Assembly.GetName().Version
                Return v.Major & "." & v.Minor & "." & v.Build
            End Get
        End Property

        Public Sub New(installFolder As String, logIt As Action(Of String))
            AddOnFolder = installFolder
            _log = logIt
            Log("Core " & Version & " loaded")
        End Sub

        Public Sub ShowSettings(owner As IWin32Window)
            Dim s = Settings.Load()
            Using f As New SettingsForm(s)
                If f.ShowDialog(owner) = DialogResult.OK Then s.Save()
            End Using
        End Sub

        ''' <summary>The main command: export the part(s) of the session and open them in PrusaSlicer.</summary>
        Public Sub Send(owner As IWin32Window, session As Object)
            Dim s = Settings.Load()
            If session Is Nothing Then
                MessageBox.Show(owner, "Open a part or an assembly first.", Title, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            ' 1. PrusaSlicer
            Dim exe = SlicerLocator.Resolve(s)
            If exe = "" Then
                If MessageBox.Show(owner, "PrusaSlicer was not found on this computer." & vbCrLf & vbCrLf &
                                   "Do you want to point to prusa-slicer.exe yourself?", Title,
                                   MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
                exe = SlicerLocator.Browse(owner, "")
                If exe = "" Then Return
                s.SlicerPath = exe
                s.RememberSlicer(exe)
                s.Save()
            End If

            ' 2. Export
            Dim result As Exporter.Result
            Using prog As New ProgressForm()
                prog.Show(owner)
                Application.DoEvents()
                result = Exporter.Run(session, s, AddressOf prog.Report)
            End Using

            If result.Files.Count = 0 Then
                Dim msg = If(result.Message <> "", result.Message, "Nothing was exported.")
                If result.Errors.Count > 0 Then msg &= vbCrLf & vbCrLf & String.Join(vbCrLf, result.Errors.Take(10))
                MessageBox.Show(owner, msg, Title, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            ' 3. Open in PrusaSlicer (one object per file argument)
            Try
                SlicerLocator.Open(exe, result.Files)
                Log("Sent " & result.Files.Count & " objects (" & result.Exported & " files) to " & exe)
            Catch ex As Exception
                Log("Starting PrusaSlicer failed: " & ex.ToString())
                MessageBox.Show(owner, "Could not start PrusaSlicer:" & vbCrLf & exe & vbCrLf & vbCrLf & ex.Message, Title,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End Try

            If result.Errors.Count > 0 Then
                MessageBox.Show(owner, result.Exported & " parts were sent to PrusaSlicer, but " & result.Errors.Count & " failed:" & vbCrLf & vbCrLf &
                                String.Join(vbCrLf, result.Errors.Take(15)), Title, MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class

End Namespace
