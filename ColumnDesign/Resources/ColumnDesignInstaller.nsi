; CoretalogueInstaller.nsi
;
; This script remembers the directory, 
; has uninstall support and (optionally) installs start menu shortcuts.
;
; It will install example2.nsi into a directory that the user selects,

;--------------------------------

; The name of the installer
Name "Magma Works Column Design"

; The file to write
OutFile "MWColumnDesign.exe"

; The default installation directory
InstallDir $PROGRAMFILES64\MW_ColumnDesign

; Registry key to check for directory (so if you install again, it will 
; overwrite the old one automatically)
InstallDirRegKey HKLM "Software\MW_ColumnDesign" "Install_Dir"

; Request application privileges for Windows Vista
RequestExecutionLevel admin

;--------------------------------

; Pages

Page components
Page directory
Page instfiles

UninstPage uninstConfirm
UninstPage instfiles

;--------------------------------

; The stuff to install
Section "MW_ColumnDesign (required)"

  SectionIn RO
  
  ; Set output path to the installation directory.
  SetOutPath $INSTDIR
  
  ; Put file there
  File "ColumnDesigner.exe"
  File "DocumentFormat.OpenXml.dll"
  File "ETABSColumnDesign_Plugin.dll"
  File "GenericViewer.dll"
  File "FireDesign.dll"
  File "HelixToolkit.Wpf.dll"
  File "InteractionDiagram3D.dll"
  File "libSkiaSharp.dll"
  File "LiveCharts.dll"
  File "LiveCharts.Wpf.dll"
  File "MathNet.Numerics.dll"
  File "MIConvexHull.dll"
  File "MWGeometry.dll"
  File "Newtonsoft.Json.dll"
  File "OxyPlot.dll"
  File "OxyPlot.Wpf.dll"
  File "Rhino3dmIO.dll"
  File "SkiaSharp.dll"
  File "System.IO.Packaging.dll"
  File "System.ValueTuple.dll"
  File "Triangle.dll"
  File "WpfMath.dll"
  
  SetOutPath "$INSTDIR\Libraries"
  File /nonfatal /a /r "Libraries\"
  
  CreateDirectory "$DOCUMENTS\ColumnDesigner"
  CreateDirectory "$DOCUMENTS\ColumnDesigner\Saved models"
  CreateDirectory "$DOCUMENTS\ColumnDesigner\Temp"
  ; Write the installation path into the registry
  WriteRegStr HKLM SOFTWARE\MW_ColumnDesigner "Install_Dir" "$INSTDIR"
  
  ; Write the uninstall keys for Windows
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\MW_ColumnDesigner" "DisplayName" "Whitby Wood Column Design"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\MW_ColumnDesigner" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\MW_ColumnDesigner" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\MW_ColumnDesigner" "NoRepair" 1
  WriteUninstaller "$INSTDIR\uninstall.exe"
  
SectionEnd

; Optional section (can be disabled by the user)
Section "Start Menu Shortcuts"

  CreateDirectory "$SMPROGRAMS\MW_ColumnDesigner"
  CreateShortcut "$SMPROGRAMS\MW_ColumnDesigner\ColumnDesigner.lnk" "$INSTDIR\ColumnDesigner.exe" "" "$INSTDIR\ColumnDesigner.exe" 0
  
SectionEnd

Section "Desktop Shortcut"

  CreateShortCut "$DESKTOP\ColumnDesigner.lnk" "$INSTDIR\ColumnDesigner.exe"
  
SectionEnd

;--------------------------------

; Uninstaller

Section "Uninstall"

  ; Remove registry keys
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\MW_ColumnDesigner"
  DeleteRegKey HKLM SOFTWARE\MW_ColumnDesigner
  
  ; Remove files and uninstaller

  ; Remove shortcuts, if any
  Delete "$SMPROGRAMS\MW_ColumnDesigner\*.*"
  Delete "$DESKTOP\ColumnDesigner.lnk"

  ; Remove directories used
  RMDir "$SMPROGRAMS\MW_ColumnDesigner"
  RMDir $INSTDIR
  RMDir $INSTDIR\data
  RMDir /r /REBOOTOK $INSTDIR
  RMDir /REBOOTOK $INSTDIR\DLLs

SectionEnd
