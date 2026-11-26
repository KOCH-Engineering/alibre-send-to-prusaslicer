' KOCH Send to PrusaSlicer - settings (%APPDATA%\Koch Engineering\PrusaSlicer\settings.ini)
Imports System.Collections.Generic
Imports System.IO

Namespace KochPrusaSlicer

    Public Class Settings
        Public Property SlicerPath As String = ""
        ''' <summary>One object in PrusaSlicer per instance in the assembly (4 bolts -> 4 objects).</summary>
        Public Property CopyPerInstance As Boolean = True
        Public Property IncludeSubAssemblies As Boolean = True
        Public Property SkipSuppressed As Boolean = True
        Public Property SkipHidden As Boolean = False
        ''' <summary>Use the STL quality set in Alibre (File > Export settings); otherwise Alibre's default.</summary>
        Public Property UseAlibreStlSettings As Boolean = True
        ''' <summary>False = temporary folder, True = "Print" folder next to the model.</summary>
        Public Property ExportNextToModel As Boolean = False

        Public Shared ReadOnly Property Folder As String
            Get
                Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Koch Engineering", "PrusaSlicer")
            End Get
        End Property

        Private Shared ReadOnly Property FilePath As String
            Get
                Return Path.Combine(Folder, "settings.ini")
            End Get
        End Property

        Public Shared Function Load() As Settings
            Dim s As New Settings()
            Try
                If Not File.Exists(FilePath) Then Return s
                Dim d As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
                For Each line In File.ReadAllLines(FilePath)
                    Dim i = line.IndexOf("="c)
                    If i > 0 Then d(line.Substring(0, i).Trim()) = line.Substring(i + 1).Trim()
                Next
                Dim v As String = Nothing
                If d.TryGetValue("SlicerPath", v) Then s.SlicerPath = v
                s.CopyPerInstance = Bool(d, "CopyPerInstance", s.CopyPerInstance)
                s.IncludeSubAssemblies = Bool(d, "IncludeSubAssemblies", s.IncludeSubAssemblies)
                s.SkipSuppressed = Bool(d, "SkipSuppressed", s.SkipSuppressed)
                s.SkipHidden = Bool(d, "SkipHidden", s.SkipHidden)
                s.UseAlibreStlSettings = Bool(d, "UseAlibreStlSettings", s.UseAlibreStlSettings)
                s.ExportNextToModel = Bool(d, "ExportNextToModel", s.ExportNextToModel)
            Catch ex As Exception
                Plugin.Log("Reading settings failed: " & ex.Message)
            End Try
            Return s
        End Function

        Public Sub Save()
            Try
                Directory.CreateDirectory(Folder)
                File.WriteAllLines(FilePath, {
                    "SlicerPath=" & SlicerPath,
                    "CopyPerInstance=" & B(CopyPerInstance),
                    "IncludeSubAssemblies=" & B(IncludeSubAssemblies),
                    "SkipSuppressed=" & B(SkipSuppressed),
                    "SkipHidden=" & B(SkipHidden),
                    "UseAlibreStlSettings=" & B(UseAlibreStlSettings),
                    "ExportNextToModel=" & B(ExportNextToModel)})
            Catch ex As Exception
                Plugin.Log("Saving settings failed: " & ex.Message)
            End Try
        End Sub

        Private Shared Function Bool(d As Dictionary(Of String, String), key As String, def As Boolean) As Boolean
            Dim v As String = Nothing
            If Not d.TryGetValue(key, v) Then Return def
            Return v = "1" OrElse v.Equals("true", StringComparison.OrdinalIgnoreCase)
        End Function

        Private Shared Function B(v As Boolean) As String
            Return If(v, "1", "0")
        End Function
    End Class

End Namespace
