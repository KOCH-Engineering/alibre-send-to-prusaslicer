' KOCH Send to PrusaSlicer - presses "A" (Arrange) in PrusaSlicer once the new parts are loaded
'
' PrusaSlicer has no command-line switch that arranges parts loaded into the GUI, so after a send we
' wait in the background until PrusaSlicer has finished loading (CPU quiet, main window enabled - i.e.
' no dialog such as the multi-part question open) and then press A in its window.
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading

Namespace KochPrusaSlicer

    Public Module Arranger

        <DllImport("user32.dll")> Private Function SetForegroundWindow(h As IntPtr) As Boolean
        End Function
        <DllImport("user32.dll")> Private Function GetForegroundWindow() As IntPtr
        End Function
        <DllImport("user32.dll")> Private Function IsWindowEnabled(h As IntPtr) As Boolean
        End Function
        <DllImport("user32.dll")> Private Function IsIconic(h As IntPtr) As Boolean
        End Function
        <DllImport("user32.dll")> Private Function ShowWindow(h As IntPtr, cmd As Integer) As Boolean
        End Function
        <DllImport("user32.dll")> Private Sub keybd_event(vk As Byte, scan As Byte, flags As UInteger, extra As UIntPtr)
        End Sub

        Private Const VK_A As Byte = &H41
        Private Const KEYUP As UInteger = 2

        ''' <summary>Starts a background wait; returns at once so Alibre stays responsive.</summary>
        Public Sub ArrangeWhenLoaded(exe As String, objectCount As Integer)
            Dim t As New Thread(Sub() Run(exe, objectCount)) With {.IsBackground = True, .Name = "KOCH arrange"}
            t.Start()
        End Sub

        Private Sub Run(exe As String, objectCount As Integer)
            Try
                Dim deadline = DateTime.Now.AddSeconds(90 + objectCount)
                Thread.Sleep(1500)
                Dim started = DateTime.Now
                Dim lastCpu = TimeSpan.Zero
                Dim quiet = 0
                Dim samples = 0
                Dim sawBusy = False
                Do While DateTime.Now < deadline
                    Thread.Sleep(500)
                    Dim p = FindSlicer(exe)
                    If p Is Nothing Then Continue Do
                    Dim cpu As TimeSpan
                    Try
                        p.Refresh()
                        cpu = p.TotalProcessorTime
                    Catch
                        Continue Do
                    End Try
                    Dim busy = (cpu - lastCpu).TotalMilliseconds
                    lastCpu = cpu
                    samples += 1
                    If samples > 1 AndAlso busy > 150 Then sawBusy = True
                    Dim h = p.MainWindowHandle
                    Dim enabled = h <> IntPtr.Zero AndAlso IsWindowEnabled(h)
                    ' quiet = under ~25 % of one core for two samples, and no dialog open (main window enabled)
                    If busy < 120 AndAlso enabled Then quiet += 1 Else quiet = 0
                    If samples Mod 4 = 0 Then Plugin.Log(String.Format("Arrange: pid {0}, window {1}, enabled {2}, cpu {3:0} ms/0.5 s, seen busy {4}",
                                                                         p.Id, h <> IntPtr.Zero, enabled, busy, sawBusy))
                    ' only after PrusaSlicer has actually been busy loading (or after 8 s, for very small parts)
                    If quiet >= 2 AndAlso samples > 2 AndAlso (sawBusy OrElse (DateTime.Now - started).TotalSeconds > 8) Then
                        If IsIconic(h) Then ShowWindow(h, 9)
                        SetForegroundWindow(h)
                        Thread.Sleep(300)
                        If GetForegroundWindow() <> h Then
                            Plugin.Log("Arrange: could not bring the slicer to the front - press A yourself")
                            Return
                        End If
                        keybd_event(VK_A, 0, 0, UIntPtr.Zero)
                        keybd_event(VK_A, 0, KEYUP, UIntPtr.Zero)
                        Plugin.Log("Arrange: pressed A in the slicer")
                        Return
                    End If
                Loop
                Plugin.Log("Arrange: the slicer did not become ready in time")
            Catch ex As Exception
                Plugin.Log("Arrange failed: " & ex.Message)
            End Try
        End Sub

        ''' <summary>The running PrusaSlicer with a main window (the one started from this exe if it can tell).</summary>
        Private Function FindSlicer(exe As String) As Process
            Dim procs = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exe))
            Dim withWindow = procs.Where(Function(p) p.MainWindowHandle <> IntPtr.Zero).ToList()
            For Each p In withWindow
                Try
                    If String.Equals(p.MainModule.FileName, exe, StringComparison.OrdinalIgnoreCase) Then Return p
                Catch
                End Try
            Next
            Return withWindow.FirstOrDefault()
        End Function

    End Module

End Namespace
