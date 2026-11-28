' KOCH Send to Slicer - finds and starts PrusaSlicer, Bambu Studio and OrcaSlicer
'
' Bambu Studio is built on PrusaSlicer and OrcaSlicer on Bambu Studio, so all three take the files to
' open on the command line and arrange with "A". Unzipped/portable copies are found from running
' processes, from the programs Windows has seen run (MuiCache, Compatibility Assistant), from
' "Open with" associations and from typical folders - and anything found or browsed to is remembered.
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Windows.Forms
Imports Microsoft.Win32

Namespace KochPrusaSlicer

    Public Module SlicerLocator

        ''' <summary>Known slicers in default-preference order: exe name, display name, name in Windows' app list.</summary>
        Private ReadOnly Kinds As (Exe As String, Name As String, Match As String)() = {
            ("prusa-slicer.exe", "PrusaSlicer", "PrusaSlicer"),
            ("bambu-studio.exe", "Bambu Studio", "Bambu Studio"),
            ("orca-slicer.exe", "OrcaSlicer", "OrcaSlicer")}

        ' ---------------------------------------------------------------- describing an exe

        ' 0 = PrusaSlicer, 1 = Bambu Studio, 2 = OrcaSlicer, -1 = not a slicer. Accepts renamed exes too
        ' (e.g. "PrusaSlicer.exe" in an alpha zip) but not the G-code viewer, console or installer helpers.
        Public Function KindIndex(p As String) As Integer
            Dim f = Path.GetFileName(If(p, "")).ToLowerInvariant()
            For i = 0 To Kinds.Length - 1
                If f = Kinds(i).Exe Then Return i
            Next
            If Not f.EndsWith(".exe") OrElse f.Contains("gcode") OrElse f.Contains("viewer") OrElse f.Contains("console") OrElse
               f.Contains("unins") OrElse f.Contains("setup") OrElse f.Contains("install") Then Return -1
            If f.Contains("prusa") Then Return 0
            If f.Contains("bambu") Then Return 1
            If f.Contains("orca") Then Return 2
            Return -1
        End Function

        Public Function SlicerName(p As String) As String
            Dim i = KindIndex(p)
            Return If(i >= 0, Kinds(i).Name, "Slicer")
        End Function

        Public Function IsPrusa(p As String) As Boolean
            Return KindIndex(p) = 0
        End Function

        ''' <summary>"2.9.7" or "3.0.0-alpha5" - just the version, even when the exe says "PrusaSlicer-2.9.7".</summary>
        Public Function VersionText(p As String) As String
            Try
                Dim fv = FileVersionInfo.GetVersionInfo(p)
                Dim v = If(fv.ProductVersion, "").Trim()
                If v = "" Then v = If(fv.FileVersion, "")
                Dim m = Regex.Match(v, "\d+(\.\d+)+([-+ ]?(alpha|beta|rc|dev|nightly)[\w.]*)?", RegexOptions.IgnoreCase)
                Return If(m.Success, m.Value, v)
            Catch
                Return ""
            End Try
        End Function

        ''' <summary>True for alpha, beta, RC, nightly and dev builds - by version text, product name or folder name.</summary>
        Public Function IsPreRelease(p As String) As Boolean
            Dim t = Path.GetDirectoryName(p)
            Try
                Dim fv = FileVersionInfo.GetVersionInfo(p)
                t &= " " & fv.ProductVersion & " " & fv.FileVersion & " " & fv.ProductName
            Catch
            End Try
            Return Regex.IsMatch(t.ToLowerInvariant(), "alpha|beta|nightly|(^|[^a-z])dev([^a-z]|$)|(^|[^a-z])rc\d*([^a-z]|$)")
        End Function

        ''' <summary>"PrusaSlicer 2.9.7" or "PrusaSlicer 3.0.0-alpha5 (pre-release)".</summary>
        Public Function Describe(p As String) As String
            Return SlicerName(p) & " " & VersionText(p) & If(IsPreRelease(p), "  (pre-release)", "")
        End Function

        ' ---------------------------------------------------------------- choosing

        ''' <summary>The configured slicer if it still exists, otherwise the default (and remembers it).</summary>
        Public Function Resolve(s As Settings) As String
            If s.SlicerPath <> "" AndAlso File.Exists(s.SlicerPath) Then Return s.SlicerPath
            Dim found = Find(s)
            If found <> "" Then
                s.SlicerPath = found
                s.Save()
                Plugin.Log("Using " & Describe(found) & ": " & found)
            End If
            Return found
        End Function

        ''' <summary>Default when nothing is chosen: newest stable PrusaSlicer, then Bambu Studio, then OrcaSlicer; pre-releases only if nothing else.</summary>
        Public Function Find(Optional s As Settings = Nothing) As String
            Dim all = FindAll(s)
            Dim stable = all.Where(Function(p) Not IsPreRelease(p)).FirstOrDefault()
            Return If(stable, If(all.FirstOrDefault(), ""))
        End Function

        ''' <summary>Every slicer found, sorted PrusaSlicer / Bambu Studio / OrcaSlicer, stable before pre-release, newest first.
        ''' Remembers new finds in the settings so portable copies stay listed.</summary>
        Public Function FindAll(Optional s As Settings = Nothing) As List(Of String)
            Dim hits As New List(Of String)
            If s IsNot Nothing Then hits.AddRange(s.ExtraSlicers.Split({";"c}, StringSplitOptions.RemoveEmptyEntries))
            Safe(Sub() FromUninstallList(hits))
            Safe(Sub() FromProgramFolders(hits))
            Safe(Sub() FromRunning(hits))
            Safe(Sub() FromRunHistory(hits))
            Safe(Sub() FromOpenWith(hits))
            Safe(Sub() FromTypicalFolders(hits))
            Dim found = hits.Where(Function(h) KindIndex(h) >= 0 AndAlso File.Exists(h)).
                             Select(Function(h) Path.GetFullPath(h)).
                             Distinct(StringComparer.OrdinalIgnoreCase).
                             OrderBy(Function(h) KindIndex(h)).
                             ThenBy(Function(h) If(IsPreRelease(h), 1, 0)).
                             ThenByDescending(Function(h) FileVer(h)).ToList()
            ' remember copies outside the normal install places (portable / unzipped)
            If s IsNot Nothing Then
                For Each h In found
                    If Not IsInstalledLocation(h) Then s.RememberSlicer(h)
                Next
                ' forget remembered copies that are gone
                s.ExtraSlicers = String.Join(";", s.ExtraSlicers.Split({";"c}, StringSplitOptions.RemoveEmptyEntries).Where(Function(x) File.Exists(x)))
            End If
            Return found
        End Function

        Private Sub Safe(a As Action)
            Try
                a()
            Catch
            End Try
        End Sub

        Private Function IsInstalledLocation(p As String) As Boolean
            Dim pf1 = Environment.GetEnvironmentVariable("ProgramW6432")
            Dim pf2 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            Return (Not String.IsNullOrEmpty(pf1) AndAlso p.StartsWith(pf1, StringComparison.OrdinalIgnoreCase)) OrElse
                   (Not String.IsNullOrEmpty(pf2) AndAlso p.StartsWith(pf2, StringComparison.OrdinalIgnoreCase))
        End Function

        ' 1. Windows "Apps" list
        Private Sub FromUninstallList(hits As List(Of String))
            For Each hive In {RegistryHive.LocalMachine, RegistryHive.CurrentUser}
                For Each view In {RegistryView.Registry64, RegistryView.Registry32}
                    Try
                        Using baseKey = RegistryKey.OpenBaseKey(hive, view),
                              un = baseKey.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\Uninstall")
                            If un Is Nothing Then Continue For
                            For Each name In un.GetSubKeyNames()
                                Using k = un.OpenSubKey(name)
                                    Dim dn = TryCast(k?.GetValue("DisplayName"), String)
                                    If dn Is Nothing Then Continue For
                                    For Each kind In Kinds
                                        If dn.IndexOf(kind.Match, StringComparison.OrdinalIgnoreCase) < 0 Then Continue For
                                        Dim loc = TryCast(k.GetValue("InstallLocation"), String)
                                        If Not String.IsNullOrEmpty(loc) Then hits.Add(Path.Combine(loc.Trim(""""c), kind.Exe))
                                        Dim icon = TryCast(k.GetValue("DisplayIcon"), String)
                                        If Not String.IsNullOrEmpty(icon) Then
                                            icon = icon.Split(","c)(0).Trim(""""c)
                                            hits.Add(Path.Combine(Path.GetDirectoryName(icon), kind.Exe))
                                        End If
                                    Next
                                End Using
                            Next
                        End Using
                    Catch
                    End Try
                Next
            Next
        End Sub

        ' 2. Program Files and %LOCALAPPDATA%\Programs, two levels deep
        Private Sub FromProgramFolders(hits As List(Of String))
            For Each pf In {Environment.GetEnvironmentVariable("ProgramW6432"), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs")}
                If String.IsNullOrEmpty(pf) OrElse Not Directory.Exists(pf) Then Continue For
                For Each pattern In {"*Prusa*", "*Bambu*", "*Orca*"}
                    Try
                        For Each d In Directory.GetDirectories(pf, pattern)
                            AddExesIn(d, hits)
                            For Each d2 In Directory.GetDirectories(d)
                                AddExesIn(d2, hits)
                            Next
                        Next
                    Catch
                    End Try
                Next
            Next
        End Sub

        ' 3. a slicer that is running right now
        Private Sub FromRunning(hits As List(Of String))
            For Each p In Process.GetProcesses()
                Try
                    Dim n = p.ProcessName.ToLowerInvariant()
                    Dim title = If(p.MainWindowTitle, "")
                    If n.Contains("prusa") OrElse n.Contains("bambu") OrElse n.Contains("orca") OrElse
                       title.IndexOf("PrusaSlicer", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                       title.IndexOf("Bambu Studio", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                       title.IndexOf("OrcaSlicer", StringComparison.OrdinalIgnoreCase) >= 0 Then
                        Dim f = p.MainModule.FileName
                        Plugin.Log("Find: running " & p.ProcessName & " (" & title & ") -> " & f)
                        hits.Add(f)
                    End If
                Catch ex As Exception
                    Plugin.Log("Find: could not read process " & p.ProcessName & ": " & ex.Message)
                End Try
            Next
        End Sub

        ' 4. programs Windows has seen run (also unzipped ones)
        Private Sub FromRunHistory(hits As List(Of String))
            For Each keyPath In {"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache",
                                 "Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store"}
                Try
                    Using k = Registry.CurrentUser.OpenSubKey(keyPath)
                        If k Is Nothing Then Continue For
                        For Each v In k.GetValueNames()
                            ' MuiCache value names look like "C:\…\prusa-slicer.exe.FriendlyAppName"
                            Dim p = Regex.Replace(v, "\.(FriendlyAppName|ApplicationCompany)$", "", RegexOptions.IgnoreCase)
                            If KindIndex(p) >= 0 Then hits.Add(p)
                        Next
                    End Using
                Catch
                End Try
            Next
        End Sub

        ' 5. "Open with" associations
        Private Sub FromOpenWith(hits As List(Of String))
            For Each kind In Kinds
                For Each root In {Registry.CurrentUser, Registry.ClassesRoot}
                    Try
                        Dim sub1 = If(root Is Registry.CurrentUser, "Software\Classes\", "") & "Applications\" & kind.Exe & "\shell\open\command"
                        Using k = root.OpenSubKey(sub1)
                            Dim cmd = TryCast(k?.GetValue(""), String)
                            If String.IsNullOrEmpty(cmd) Then Continue For
                            Dim m = Regex.Match(cmd, "^\s*""([^""]+)""|^\s*(\S+)")
                            hits.Add(If(m.Groups(1).Success, m.Groups(1).Value, m.Groups(2).Value))
                        End Using
                    Catch
                    End Try
                Next
            Next
        End Sub

        ' 6. typical places for unzipped copies, two levels deep
        Private Sub FromTypicalFolders(hits As List(Of String))
            Dim home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            For Each f In {Path.Combine(home, "Downloads"), Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                           Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "C:\Tools", "C:\Apps"}
                If String.IsNullOrEmpty(f) OrElse Not Directory.Exists(f) Then Continue For
                Try
                    For Each d In Directory.GetDirectories(f)
                        Dim n = Path.GetFileName(d).ToLowerInvariant()
                        If Not (n.Contains("prusa") OrElse n.Contains("bambu") OrElse n.Contains("orca") OrElse n.Contains("slicer")) Then Continue For
                        AddExesIn(d, hits)
                        For Each d2 In Directory.GetDirectories(d)
                            AddExesIn(d2, hits)
                        Next
                    Next
                Catch
                End Try
            Next
        End Sub

        Private Sub AddExesIn(dir As String, hits As List(Of String))
            For Each kind In Kinds
                hits.Add(Path.Combine(dir, kind.Exe))
            Next
        End Sub

        Private Function FileVer(p As String) As Version
            Try
                Dim fv = FileVersionInfo.GetVersionInfo(p)
                Return New Version(fv.FileMajorPart, fv.FileMinorPart, fv.FileBuildPart, fv.FilePrivatePart)
            Catch
                Return New Version(0, 0)
            End Try
        End Function

        ' ---------------------------------------------------------------- browse / open

        Public Function Browse(owner As IWin32Window, current As String) As String
            Using dlg As New OpenFileDialog With {
                    .Title = "Find your slicer",
                    .Filter = "Slicers (prusa-slicer, bambu-studio, orca-slicer)|prusa-slicer.exe;bambu-studio.exe;orca-slicer.exe|Programs (*.exe)|*.exe"}
                If current <> "" AndAlso File.Exists(current) Then dlg.InitialDirectory = Path.GetDirectoryName(current)
                If dlg.ShowDialog(owner) = DialogResult.OK Then Return dlg.FileName
            End Using
            Return ""
        End Function

        ''' <summary>
        ''' Opens the files in the slicer. PrusaSlicer gets --single-instance so the files go to a running window;
        ''' Bambu Studio and OrcaSlicer use their own "only one instance" preference. Long lists are sent in batches.
        ''' </summary>
        Public Sub Open(exe As String, files As IList(Of String))
            Const maxLen = 30000
            Dim prefix = If(IsPrusa(exe), "--single-instance", "")
            Dim sb As New StringBuilder(prefix)
            Dim first = True
            For Each f In files
                Dim arg = " """ & f & """"
                If sb.Length + arg.Length > maxLen Then
                    Start(exe, sb.ToString().Trim(), first)
                    first = False
                    sb.Clear().Append(prefix)
                End If
                sb.Append(arg)
            Next
            Start(exe, sb.ToString().Trim(), first)
        End Sub

        Private Sub Start(exe As String, args As String, first As Boolean)
            If Not first Then Threading.Thread.Sleep(3000)   ' let the first batch start the slicer
            Dim psi As New ProcessStartInfo(exe, args) With {.UseShellExecute = False, .WorkingDirectory = Path.GetDirectoryName(exe)}
            Process.Start(psi)
        End Sub

    End Module

End Namespace
