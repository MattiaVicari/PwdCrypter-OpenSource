; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "PwdCrypter Native Messaging Host"
#define MyAppVersion "<YOUR_VERSION>"
#define MyAppPublisher "<YOUR_NAME>"
#define MyAppURL "<YOUR_WEBSITE>"
#define MyAppExeName "PwdCrypterNativeMessagingHost.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{81F6E8E8-4244-43E7-8A96-6FD4EA5510DB}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={commonpf}\PwdCrypter
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=license-English.txt
OutputDir=setup
OutputBaseFilename=pwdcrypter-nmh-setup-{#MyAppVersion}
SetupIconFile=icons\PwdCrypter.ico
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"; LicenseFile: "license-English.txt"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"; LicenseFile: "license-Italian.txt"

[Files]
Source: "bin\Release\PwdCrypterNativeMessagingHost.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\System.Runtime.WindowsRuntime.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "nmh_manifest.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "nmh_manifest_firefox.json"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

[Registry]
Root: HKLM32; Subkey: "Software\Google\Chrome\NativeMessagingHosts\<YOUR_PACKAGE_NAME>"; Flags: createvalueifdoesntexist uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "{app}\nmh_manifest.json"; Check: IsWin64;
Root: HKLM64; Subkey: "Software\Google\Chrome\NativeMessagingHosts\<YOUR_PACKAGE_NAME>"; Flags: createvalueifdoesntexist uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "{app}\nmh_manifest.json"; Check: "not IsWin64";
; For Firefox, we must set the key in the 32bit section
Root: HKLM64; Subkey: "Software\Mozilla\NativeMessagingHosts\<YOUR_PACKAGE_NAME>"; Flags: createvalueifdoesntexist uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "{app}\nmh_manifest_firefox.json"

