' KOCH Send to PrusaSlicer - Alibre Design add-on (loader)
' (c) 2026 KOCH Engineering (Henrik J. Koch, Denmark) - MIT License
'
' This small DLL is what Alibre loads and locks. It reads KochPrusaSlicer.Core.dll into memory
' (so that file is never locked). After installing a new core, "Reload add-on" picks it up
' without restarting Alibre Design.

Option Strict Off

Imports System.IO
Imports System.Reflection
Imports System.Windows.Forms
Imports AlibreAddOn
Imports AlibreX

Namespace AlibreAddOnAssembly

    Public Module AlibreAddOn
        Private _root As IADRoot
        Private _addOn As PrusaSlicerAddOn

        Public Sub AddOnLoad(ByVal hwnd As IntPtr, ByVal pAutomationHook As IAutomationHook, ByVal unused As IntPtr)
            _root = CType(pAutomationHook.Root, IADRoot)
            _addOn = New PrusaSlicerAddOn(_root, hwnd)
        End Sub

        Public Sub AddOnUnload(ByVal hwnd As IntPtr, ByVal forceUnload As Boolean, ByRef cancel As Boolean, ByVal reserved1 As Integer, ByVal reserved2 As Integer)
            If _addOn IsNot Nothing Then _addOn.Shutdown()
            _addOn = Nothing
            _root = Nothing
        End Sub

        Public Function GetRoot() As IADRoot
            Return _root
        End Function

        Public Sub AddOnInvoke(ByVal hwnd As IntPtr, ByVal pAutomationHook As IntPtr, ByVal sessionName As String, ByVal isLicensed As Boolean, ByVal reserved1 As Integer, ByVal reserved2 As Integer)
        End Sub

        Public Function GetAddOnInterface() As IAlibreAddOn
            Return CType(_addOn, IAlibreAddOn)
        End Function
    End Module

    Public Class PrusaSlicerAddOn
        Implements IAlibreAddOn

        Private Const ROOT_ID As Integer = 6100
        Private Const CMD_SEND As Integer = 6101
        Private Const CMD_SETTINGS As Integer = 6102
        Private Const CMD_RELOAD As Integer = 6103
        Private Const TITLE As String = "Send to PrusaSlicer"

        Private ReadOnly _root As IADRoot
        Private ReadOnly _hwnd As IntPtr
        Private ReadOnly _folder As String
        Private _core As Object
        Private _coreVersion As String = "?"

        Public Sub New(root As IADRoot, hwnd As IntPtr)
            _root = root
            _hwnd = hwnd
            _folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            LoadCore()
        End Sub

        ' ---------------------------------------------------------------- core loading

        Private Sub LoadCore()
            Try
                Dim file = Path.Combine(_folder, "KochPrusaSlicer.Core.dll")
                Dim asm = Assembly.Load(IO.File.ReadAllBytes(file))
                _coreVersion = asm.GetName().Version.ToString(3)
                Dim t = asm.GetType("KochPrusaSlicer.Plugin", True)
                Dim logIt As Action(Of String) = AddressOf Log.Write
                _core = Activator.CreateInstance(t, New Object() {_folder, logIt})
            Catch ex As Exception
                _core = Nothing
                Log.Write("Loading the core failed: " & ex.ToString())
            End Try
        End Sub

        Private Sub UnloadCore()
            Try
                If _core IsNot Nothing Then CType(_core, IDisposable).Dispose()
            Catch ex As Exception
                Log.Write("Disposing the core failed: " & ex.Message)
            End Try
            _core = Nothing
        End Sub

        Public Sub Shutdown()
            UnloadCore()
        End Sub

        ' ---------------------------------------------------------------- session

        ''' <summary>The Alibre session the command was clicked in (falls back to the top-most window).</summary>
        Private Function ResolveSession(sessionIdentifier As String) As Object
            Try
                If Not String.IsNullOrEmpty(sessionIdentifier) Then
                    For Each s In _root.Sessions
                        If String.Equals(s.Identifier, sessionIdentifier, StringComparison.OrdinalIgnoreCase) Then Return s
                    Next
                End If
            Catch
            End Try
            Try
                Return _root.TopmostSession
            Catch
                Return Nothing
            End Try
        End Function

        ' ---------------------------------------------------------------- IAlibreAddOn

        Public ReadOnly Property RootMenuItem As Integer Implements IAlibreAddOn.RootMenuItem
            Get
                Return ROOT_ID
            End Get
        End Property

        Public Function HasSubMenus(menuId As Integer) As Boolean Implements IAlibreAddOn.HasSubMenus
            Return menuId = ROOT_ID
        End Function

        Public Function SubMenuItems(menuId As Integer) As Array Implements IAlibreAddOn.SubMenuItems
            If menuId <> ROOT_ID Then Return Nothing
            Return New Integer() {CMD_SEND, CMD_SETTINGS, CMD_RELOAD}
        End Function

        Public Function MenuItemText(menuId As Integer) As String Implements IAlibreAddOn.MenuItemText
            Select Case menuId
                Case ROOT_ID : Return "PrusaSlicer"
                Case CMD_SEND : Return "Send to PrusaSlicer"
                Case CMD_SETTINGS : Return "Settings…"
                Case CMD_RELOAD : Return "Reload add-on"
                Case Else : Return String.Empty
            End Select
        End Function

        Public Function MenuItemToolTip(menuId As Integer) As String Implements IAlibreAddOn.MenuItemToolTip
            Select Case menuId
                Case CMD_SEND : Return "Export the open part - or every part of the open assembly - and open them in PrusaSlicer"
                Case CMD_SETTINGS : Return "PrusaSlicer location, copies, suppressed/hidden parts and export folder"
                Case CMD_RELOAD : Return "Load a newly installed version without restarting Alibre Design"
                Case Else : Return "KOCH Send to PrusaSlicer"
            End Select
        End Function

        Public Function MenuItemState(menuId As Integer, sessionIdentifier As String) As ADDONMenuStates Implements IAlibreAddOn.MenuItemState
            Return ADDONMenuStates.ADDON_MENU_ENABLED
        End Function

        Public Function MenuIcon(menuID As Integer) As String Implements IAlibreAddOn.MenuIcon
            If menuID = ROOT_ID Then Return String.Empty
            Return "SendToPrusaSlicer.ico"
        End Function

        Public Function PopupMenu(menuId As Integer) As Boolean Implements IAlibreAddOn.PopupMenu
            Return False
        End Function

        Public Function InvokeCommand(menuId As Integer, sessionIdentifier As String) As IAlibreAddOnCommand Implements IAlibreAddOn.InvokeCommand
            Dim owner As IWin32Window = New AlibreWindow(_hwnd)
            Try
                If menuId = CMD_RELOAD Then
                    Dim before = _coreVersion
                    UnloadCore()
                    LoadCore()
                    Log.Write("Reloaded core " & before & " -> " & _coreVersion)
                    MessageBox.Show(owner, If(_core Is Nothing, "Reload failed - see the log:" & vbCrLf & Log.FilePath,
                                              "Send to PrusaSlicer reloaded (version " & _coreVersion & ")."), TITLE)
                    Return Nothing
                End If
                If _core Is Nothing Then LoadCore()
                If _core Is Nothing Then
                    MessageBox.Show(owner, "The add-on could not start. See the log:" & vbCrLf & Log.FilePath, TITLE)
                    Return Nothing
                End If
                Select Case menuId
                    Case CMD_SEND : _core.Send(owner, ResolveSession(sessionIdentifier))
                    Case CMD_SETTINGS : _core.ShowSettings(owner)
                End Select
            Catch ex As Exception
                Log.Write("Command " & menuId & " failed: " & ex.ToString())
                MessageBox.Show(owner, "Something went wrong: " & ex.Message & vbCrLf & vbCrLf & "Log: " & Log.FilePath, TITLE)
            End Try
            Return Nothing
        End Function

        Public Function HasPersistentDataToSave(sessionIdentifier As String) As Boolean Implements IAlibreAddOn.HasPersistentDataToSave
            Return False
        End Function

        Public Sub SaveData(pCustomData As IStream, sessionIdentifier As String) Implements IAlibreAddOn.SaveData
        End Sub

        Public Sub LoadData(pCustomData As IStream, sessionIdentifier As String) Implements IAlibreAddOn.LoadData
        End Sub

        Public Function UseDedicatedRibbonTab() As Boolean Implements IAlibreAddOn.UseDedicatedRibbonTab
            Return False
        End Function

        Private Sub IAlibreAddOn_setIsAddOnLicensed(isLicensed As Boolean) Implements IAlibreAddOn.setIsAddOnLicensed
        End Sub
    End Class

    Friend Class AlibreWindow
        Implements IWin32Window
        Private ReadOnly _h As IntPtr
        Public Sub New(h As IntPtr)
            _h = h
        End Sub
        Public ReadOnly Property Handle As IntPtr Implements IWin32Window.Handle
            Get
                Return _h
            End Get
        End Property
    End Class

    Public Module Log
        Public ReadOnly FilePath As String = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Alibre AddOns", "KochPrusaSlicer", "addon.log")
        Public Sub Write(msg As String)
            Try
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath))
                File.AppendAllText(FilePath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "  " & msg & Environment.NewLine)
            Catch
            End Try
        End Sub
    End Module

End Namespace
