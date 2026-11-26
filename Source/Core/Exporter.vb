' KOCH Send to PrusaSlicer - exports the part, or every part of the assembly, to STL or STEP (AlibreX, late-bound)
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Text.RegularExpressions

Namespace KochPrusaSlicer

    Public Module Exporter

        Public Class Result
            ''' <summary>One entry per object for PrusaSlicer (a file may appear several times = copies).</summary>
            Public ReadOnly Files As New List(Of String)
            Public ReadOnly Errors As New List(Of String)
            Public ReadOnly Skipped As New List(Of String)
            Public Exported As Integer
            Public Message As String = ""
            Public Folder As String = ""
        End Class

        Private Class PartEntry
            Public Session As Object
            Public Name As String
            Public FilePath As String
            Public Count As Integer
        End Class

        Public Function Run(session As Object, s As Settings, report As Action(Of String, Integer, Integer)) As Result
            Dim r As New Result()
            Dim kind = KindOf(session)
            If kind = "Drawing" Then
                r.Message = "This is a drawing. Open a part or an assembly and try again."
                Return r
            End If
            If kind = "" Then
                r.Message = "Open a part or an assembly first."
                Return r
            End If

            Dim modelName = SafeName(Str(Function() session.Name, "Model"))
            Dim modelPath = Str(Function() session.FilePath, "")
            r.Folder = TargetFolder(s, modelName, modelPath)
            PrepareFolder(r.Folder)

            Dim parts As New Dictionary(Of String, PartEntry)(StringComparer.OrdinalIgnoreCase)
            If kind = "Part" Then
                parts.Add("part", New PartEntry With {.Session = session, .Name = modelName, .FilePath = modelPath, .Count = 1})
            Else
                Walk(session.RootOccurrence, s, parts, r.Skipped)
                If parts.Count = 0 Then
                    r.Message = If(r.Skipped.Count > 0, "All parts are suppressed or hidden - nothing to send.", "The assembly has no parts.")
                    Return r
                End If
            End If

            Dim used As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
            Dim i = 0
            For Each p In parts.Values
                i += 1
                Dim base = If(p.FilePath <> "", SafeName(Path.GetFileNameWithoutExtension(p.FilePath)), SafeName(p.Name))
                Dim n = 0
                used.TryGetValue(base, n)
                used(base) = n + 1
                If n > 0 Then base &= "_" & (n + 1)
                Dim target = Path.Combine(r.Folder, base & If(s.IsStep, ".stp", ".stl"))
                report?.Invoke(base, i, parts.Count)
                Try
                    If s.Format = "STEP AP214" Then
                        p.Session.ExportAP214(target)
                    ElseIf s.Format = "STEP AP242" Then
                        p.Session.ExportAP242(target)
                    ElseIf s.Format = "STEP AP203" Then
                        p.Session.ExportAP203(target)
                    ElseIf s.UseAlibreStlSettings Then
                        Try
                            p.Session.ExportSTL2(target)
                        Catch
                            p.Session.ExportSTL(target)
                        End Try
                    Else
                        p.Session.ExportSTL(target)
                    End If
                    If Not File.Exists(target) Then Throw New IOException("no file was written")
                    r.Exported += 1
                    Dim copies = If(s.CopyPerInstance, Math.Max(1, p.Count), 1)
                    For c = 1 To copies
                        r.Files.Add(target)
                    Next
                Catch ex As Exception
                    r.Errors.Add(base & ": " & ex.Message)
                    Plugin.Log("Export of " & base & " failed: " & ex.ToString())
                End Try
            Next
            Plugin.Log(String.Format("{0} '{1}': {2} parts exported as " & s.Format & " to {3}, {4} objects, {5} skipped, {6} errors",
                                     kind, modelName, r.Exported, r.Folder, r.Files.Count, r.Skipped.Count, r.Errors.Count))
            Return r
        End Function

        ''' <summary>"Part", "Assembly", "Drawing" or "".</summary>
        Private Function KindOf(session As Object) As String
            Try
                If session.RootOccurrence IsNot Nothing Then Return "Assembly"
            Catch
            End Try
            Try
                Dim sheets = session.Sheets
                If sheets IsNot Nothing Then Return "Drawing"
            Catch
            End Try
            Return "Part"
        End Function

        Private Sub Walk(occ As Object, s As Settings, parts As Dictionary(Of String, PartEntry), skipped As List(Of String))
            For Each child In Items(Function() occ.Occurrences)
                Dim name = Str(Function() child.Name, "?")
                Dim suppressed = Bool(Function() child.IsSuppressed)
                Dim hidden = Bool(Function() child.IsHidden)
                If (s.SkipSuppressed AndAlso suppressed) OrElse (s.SkipHidden AndAlso hidden) Then
                    skipped.Add(name)
                    Continue For
                End If
                Dim kids = Items(Function() child.Occurrences)
                If kids.Count > 0 Then
                    If s.IncludeSubAssemblies Then Walk(child, s, parts, skipped)
                    Continue For
                End If
                Dim ds As Object = Nothing
                Try
                    ds = child.DesignSession
                Catch
                End Try
                If ds Is Nothing Then Continue For
                Dim fp = Str(Function() ds.FilePath, "")
                Dim key = If(fp <> "", fp, "virtual:" & Regex.Replace(name, "<\d+>$", ""))
                Dim e As PartEntry = Nothing
                If parts.TryGetValue(key, e) Then
                    e.Count += 1
                Else
                    parts.Add(key, New PartEntry With {.Session = ds, .Name = name, .FilePath = fp, .Count = 1})
                End If
            Next
        End Sub

        Private Function TargetFolder(s As Settings, modelName As String, modelPath As String) As String
            If s.ExportNextToModel AndAlso modelPath <> "" Then
                Return Path.Combine(Path.GetDirectoryName(modelPath), "Print", modelName)
            End If
            Return Path.Combine(Path.GetTempPath(), "KOCH PrusaSlicer", modelName)
        End Function

        ''' <summary>Creates the folder and removes STL/STEP files from an earlier send, so old parts do not linger.</summary>
        Private Sub PrepareFolder(folder As String)
            Directory.CreateDirectory(folder)
            For Each pattern In {"*.stl", "*.stp", "*.step"}
                For Each f In Directory.GetFiles(folder, pattern)
                    Try
                        File.Delete(f)
                    Catch
                    End Try
                Next
            Next
        End Sub

        Public Function SafeName(n As String) As String
            n = Regex.Replace(If(n, ""), "<\d+>$", "")
            n = Regex.Replace(n, "[\\/:*?""<>|]", "_").Trim()
            Return If(n = "", "Part", n)
        End Function

        ' ---- late-binding helpers (COM collections can be 0- or 1-based)

        Private Function Items(getter As Func(Of Object)) As List(Of Object)
            Dim res As New List(Of Object)
            Dim col As Object = Nothing
            Try
                col = getter()
            Catch
                Return res
            End Try
            If col Is Nothing Then Return res
            Try
                For Each it In col
                    res.Add(it)
                Next
                Return res
            Catch
                res.Clear()
            End Try
            Dim cnt = 0
            Try
                cnt = CInt(col.Count)
            Catch
                Return res
            End Try
            For Each first In {0, 1}
                Try
                    For k = first To first + cnt - 1
                        res.Add(col.Item(k))
                    Next
                    Return res
                Catch
                    res.Clear()
                End Try
            Next
            Return res
        End Function

        Private Function Str(getter As Func(Of Object), def As String) As String
            Try
                Dim v = getter()
                Return If(v Is Nothing, def, CStr(v))
            Catch
                Return def
            End Try
        End Function

        Private Function Bool(getter As Func(Of Object)) As Boolean
            Try
                Return CBool(getter())
            Catch
                Return False
            End Try
        End Function

    End Module

End Namespace
