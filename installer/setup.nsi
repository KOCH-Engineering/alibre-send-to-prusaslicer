Unicode true
; KOCH Send to PrusaSlicer - installer (KOCH Engineering)
; Build: build both projects (Release), then  makensis installer\setup.nsi
!ifndef LOADER
  !define LOADER "..\Source\Loader\bin\Release\net48"
!endif
!ifndef CORE
  !define CORE "..\Source\Core\bin\Release\net48"
!endif
!include "MUI2.nsh"
!include "x64.nsh"
!include "LogicLib.nsh"

!define APPNAME   "KOCH Send to PrusaSlicer (Alibre add-on)"
!define VERSION   "0.1.0"
!define PUBLISHER "KOCH Engineering"
!define ADDONID   "{CD41C50A-D5D1-4801-B9A1-785DEF849D54}"
!define UNINSTKEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\KochPrusaSlicer"

Name "${APPNAME}"
OutFile "KOCH-Send-to-PrusaSlicer-Setup-${VERSION}.exe"
RequestExecutionLevel admin
SetCompressor /SOLID lzma
BrandingText "${PUBLISHER} - Henrik J. Koch, Denmark"

VIProductVersion "${VERSION}.0"
VIAddVersionKey "ProductName" "${APPNAME}"
VIAddVersionKey "CompanyName" "${PUBLISHER}"
VIAddVersionKey "FileDescription" "Installer for ${APPNAME}"
VIAddVersionKey "FileVersion" "${VERSION}"
VIAddVersionKey "LegalCopyright" "(c) 2026 ${PUBLISHER}"
VIAddVersionKey "Comments" "Developed by ${PUBLISHER} (Henrik J. Koch, Denmark)"

!define MUI_ICON   "${LOADER}\SendToPrusaSlicer.ico"
!define MUI_UNICON "${LOADER}\SendToPrusaSlicer.ico"
!define MUI_ABORTWARNING
!define MUI_WELCOMEPAGE_TITLE "KOCH Send to PrusaSlicer for Alibre Design"
!define MUI_WELCOMEPAGE_TEXT "KOCH Engineering - Henrik J. Koch - Denmark - © 2026$\r$\nWith a lot of help from Claude (Anthropic).$\r$\n$\r$\nOne click sends the open part - or every part of the open assembly as separate objects - from Alibre Design to PrusaSlicer.$\r$\n$\r$\nRequires Alibre Design (64-bit) and PrusaSlicer.$\r$\n$\r$\nPlease close Alibre Design before you continue."
!define MUI_FINISHPAGE_TEXT "KOCH Send to PrusaSlicer is installed. Thank you for using a tool from KOCH Engineering.$\r$\n$\r$\nStart Alibre Design and click Add-on > PrusaSlicer > Send to PrusaSlicer"
!define MUI_FINISHPAGE_SHOWREADME "$INSTDIR\README.txt"
!define MUI_FINISHPAGE_SHOWREADME_TEXT "Show README"
!define MUI_FINISHPAGE_SHOWREADME_NOTCHECKED
!define MUI_FINISHPAGE_LINK "KOCH Engineering on LinkedIn"
!define MUI_FINISHPAGE_LINK_LOCATION "https://www.linkedin.com/in/henrikjkoch/"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\LICENSE"
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"

!macro CheckAlibre
  check_again:
  nsExec::ExecToStack 'cmd /c tasklist /FI "IMAGENAME eq Alibre Design.exe" /NH | find /I "Alibre Design.exe"'
  Pop $0
  Pop $1
  ${If} $0 == 0
    MessageBox MB_RETRYCANCEL|MB_ICONEXCLAMATION "Alibre Design is running. Close Alibre Design and click Retry." IDRETRY check_again
    Abort
  ${EndIf}
!macroend

Function .onInit
  SetShellVarContext all
  StrCpy $INSTDIR "$APPDATA\Alibre AddOns\KochPrusaSlicer"
  ${IfNot} ${RunningX64}
    MessageBox MB_ICONSTOP "Alibre Design requires 64-bit Windows."
    Abort
  ${EndIf}
  SetRegView 64
  ; Alibre Design installed?
  StrCpy $R0 0
  FindFirst $R1 $R2 "$PROGRAMFILES64\Alibre Design*"
  loop:
    StrCmp $R2 "" done
    IfFileExists "$PROGRAMFILES64\$R2\Program\AlibreAddOn.dll" 0 +2
      StrCpy $R0 1
    FindNext $R1 $R2
    Goto loop
  done:
  FindClose $R1
  ${If} $R0 == 0
    MessageBox MB_YESNO|MB_ICONQUESTION "No Alibre Design installation was found on this computer. Install anyway?" IDYES +2
    Abort
  ${EndIf}
FunctionEnd

Section "Install"
  !insertmacro CheckAlibre
  SetRegView 64
  SetOutPath "$INSTDIR"
  SetOverwrite on
  File "${LOADER}\KochPrusaSlicer.dll"
  File "${LOADER}\KochPrusaSlicer.adc"
  File "${LOADER}\SendToPrusaSlicer.ico"
  File "${CORE}\KochPrusaSlicer.Core.dll"
  File "${CORE}\linkedin.png"
  File "README.txt"
  File "/oname=LICENSE.txt" "..\LICENSE"

  ; Ribbon icon from the PrusaSlicer on this PC + the KOCH arrow (our own icon stays if PrusaSlicer is not found)
  nsExec::ExecToLog `powershell -NoProfile -ExecutionPolicy Bypass -Command "Add-Type -Path '$INSTDIR\KochPrusaSlicer.Core.dll'; [KochPrusaSlicer.IconMaker]::Build('$INSTDIR\SendToPrusaSlicer.ico', '')"`

  ; Register the add-on with Alibre
  WriteRegStr HKLM "SOFTWARE\Alibre Design Add-Ons" "${ADDONID}" "$INSTDIR"

  ; Uninstall entry in Windows Settings
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  WriteRegStr HKLM "${UNINSTKEY}" "DisplayName" "${APPNAME}"
  WriteRegStr HKLM "${UNINSTKEY}" "DisplayVersion" "${VERSION}"
  WriteRegStr HKLM "${UNINSTKEY}" "Publisher" "${PUBLISHER}"
  WriteRegStr HKLM "${UNINSTKEY}" "URLInfoAbout" "https://github.com/KOCH-Engineering/alibre-send-to-prusaslicer"
  WriteRegStr HKLM "${UNINSTKEY}" "DisplayIcon" "$INSTDIR\SendToPrusaSlicer.ico"
  WriteRegStr HKLM "${UNINSTKEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "${UNINSTKEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegDWORD HKLM "${UNINSTKEY}" "NoModify" 1
  WriteRegDWORD HKLM "${UNINSTKEY}" "NoRepair" 1
  WriteRegDWORD HKLM "${UNINSTKEY}" "EstimatedSize" 600
SectionEnd

Section "Uninstall"
  !insertmacro CheckAlibre
  SetRegView 64
  DeleteRegValue HKLM "SOFTWARE\Alibre Design Add-Ons" "${ADDONID}"
  DeleteRegKey HKLM "${UNINSTKEY}"
  Delete "$INSTDIR\KochPrusaSlicer.dll"
  Delete "$INSTDIR\KochPrusaSlicer.Core.dll"
  Delete "$INSTDIR\KochPrusaSlicer.adc"
  Delete "$INSTDIR\SendToPrusaSlicer.ico"
  Delete "$INSTDIR\linkedin.png"
  Delete "$INSTDIR\README.txt"
  Delete "$INSTDIR\LICENSE.txt"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir  "$INSTDIR"
SectionEnd

Function un.onInit
  SetShellVarContext all
  SetRegView 64
FunctionEnd
