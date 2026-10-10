; Inno Setup 6.3+ (x64compatible support).
#ifndef AppVersion
  #error AppVersion must be supplied with /DAppVersion=...
#endif
#ifndef PublishDirectory
  #define PublishDirectory "..\artifacts\release\IPMan"
#endif
#ifndef PackageDirectory
  #define PackageDirectory "..\artifacts\release"
#endif

[Setup]
AppId={{A65BB4CB-57C4-4D31-A016-6A0B091EB3D6}
AppName=IPMan
AppVersion={#AppVersion}
AppPublisher=IPMan Project Owner
PrivilegesRequired=admin
DefaultDirName={autopf}\IPMan
DefaultGroupName=IPMan
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
LicenseFile=..\LICENSE
SetupIconFile=..\assets\logos\IPMan.ico
UninstallDisplayIcon={app}\IPMan.exe
OutputDir={#PackageDirectory}
OutputBaseFilename=IPMan-{#AppVersion}-win-x64-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDirectory}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\IPMan"; Filename: "{app}\IPMan.exe"
Name: "{autodesktop}\IPMan"; Filename: "{app}\IPMan.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\IPMan.exe"; Description: "{cm:LaunchProgram,IPMan}"; Flags: postinstall skipifsilent shellexec

; No user-data paths or recursive delete entries: uninstall removes installed files
; and shortcuts only. Documents\IPMan and LocalAppData\IPMan remain untouched.
