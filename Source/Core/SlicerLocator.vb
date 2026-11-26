' KOCH Send to PrusaSlicer - finds and starts PrusaSlicer
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms
Imports Microsoft.Win32

Namespace KochPrusaSlicer

    Public Module SlicerLocator

        Private Const ExeName As String = "prusa-slicer.exe"

        ''' <summary>The configured path if it still exists, otherwise an automatic search ("" if none).</summary>
        Public Function Resolve(s As Settings) As String
            If s.SlicerPath <> "" AndAlso File.Exists(s.SlicerPath) Then Return s.SlicerPath
            Dim found = Find()
            If found <> "" Then
                s.SlicerPath = found
                s.Save()
                Plugin.Log("Found PrusaSlicer: " & found)
            End If
            Return found
        End Function

        ''' <summary>Looks in the Windows uninstall list and the usual Program Files folders; newest version wins.</summary>
        Public Function Find() As String
            Dim hits As New List(Of String)
            ' 1. Windows "Apps" list
            For Each hive In {RegistryHive.LocalMachine, RegistryHive.CurrentUser}
                For Each view In {RegistryView.Registry64, RegistryView.Registry32}
                    Try
                        Using baseKey = RegistryKey.OpenBaseKey(hive, view),
                              un = baseKey.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\Uninstall")
                            If un Is Nothing Then Continue For
                            For Each name In un.GetSubKeyNames()
                                Using k = un.OpenSubKey(name)
                                    Dim dn = TryCast(k?.GetValue("DisplayName"), String)
                                    If dn Is Nothing OrElse dn.IndexOf("PrusaSlicer", StringComparison.OrdinalIgnoreCase) < 0 Then Continue For
                                    Dim loc = TryCast(k.GetValue("InstallLocation"), String)
                                    If Not String.IsNullOrEmpty(loc) Then hits.Add(Path.Combine(loc.Trim(""""c), ExeName))
                                    Dim icon = TryCast(k.GetValue("DisplayIcon"), String)
                                    If Not String.IsNullOrEmpty(icon) Then
                                        icon = icon.Split(","c)(0).Trim(""""c)
                                        hits.Add(Path.Combine(Path.GetDirectoryName(icon), ExeName))
                                    End If
                                End Using
                            Next
                        End Using
                    Catch
                    End Try
                Next
            Next
            ' 2. Program Files
            For Each pf In {Environment.GetEnvironmentVariable("ProgramW6432"), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\Programs"}
                If String.IsNullOrEmpty(pf) Then Continue For
                hits.Add(Path.Combine(pf, "PrusaSlicer", ExeName))
                Try
                    Dim p3 = Path.Combine(pf, "Prusa3D")
                    If Directory.Exists(p3) Then
                        For Each d In Directory.GetDirectories(p3)
                            hits.Add(Path.Combine(d, ExeName))
                        Next
                    End If
                Catch
                End Try
            Next
            Dim best = hits.Where(Function(h) File.Exists(h)).Distinct(StringComparer.OrdinalIgnoreCase).
                            OrderByDescending(Function(h) FileVer(h)).FirstOrDefault()
            Return If(best, "")
        End Function

        Private Function FileVer(p As String) As Version
            Try
                Dim fv = FileVersionInfo.GetVersionInfo(p)
                Return New Version(fv.FileMajorPart, fv.FileMinorPart, fv.FileBuildPart, fv.FilePrivatePart)
            Catch
                Return New Version(0, 0)
            End Try
        End Function

        Public Function VersionText(p As String) As String
            Try
                Return FileVersionInfo.GetVersionInfo(p).ProductVersion
            Catch
                Return ""
            End Try
        End Function

        Public Function Browse(owner As IWin32Window, current As String) As String
            Using dlg As New OpenFileDialog With {.Title = "Find prusa-slicer.exe", .Filter = "PrusaSlicer|prusa-slicer.exe|Programs (*.exe)|*.exe",
                                                  .FileName = ExeName}
                If current <> "" AndAlso File.Exists(current) Then dlg.InitialDirectory = Path.GetDirectoryName(current)
                If dlg.ShowDialog(owner) = DialogResult.OK Then Return dlg.FileName
            End Using
            Return ""
        End Function

        ''' <summary>
        ''' Opens the files in PrusaSlicer. --single-instance hands them to a PrusaSlicer that is already
        ''' running (and starts one if not). Very long lists are sent in several batches.
        ''' </summary>
        Public Sub Open(exe As String, files As IList(Of String))
            Const maxLen = 30000
            Dim sb As New StringBuilder("--single-instance")
            Dim first = True
            For Each f In files
                Dim arg = " """ & f & """"
                If sb.Length + arg.Length > maxLen Then
                    Start(exe, sb.ToString(), first)
                    first = False
                    sb.Clear().Append("--single-instance")
                End If
                sb.Append(arg)
            Next
            Start(exe, sb.ToString(), first)
        End Sub

        Private Sub Start(exe As String, args As String, first As Boolean)
            If Not first Then Threading.Thread.Sleep(3000)   ' let the first batch start PrusaSlicer
            Dim psi As New ProcessStartInfo(exe, args) With {.UseShellExecute = False, .WorkingDirectory = Path.GetDirectoryName(exe)}
            Process.Start(psi)
        End Sub

    End Module

End Namespace
