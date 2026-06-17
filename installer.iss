; TranslateTool Inno Setup Script
; 翻译工具安装包制作脚本

#define MyAppName "翻译工具"
#define MyAppNameEn "TranslateTool"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "TranslateTool"
#define MyAppURL "https://github.com/TranslateTool"
#define MyAppExeName "TranslateTool.exe"

[Setup]
; 注意: AppId 的值用于唯一标识此应用程序。
; 不要在其他应用程序的安装程序中使用相同的 AppId 值。
AppId={{E8A3B5C7-D4F6-4A9B-8C2D-1E3F5A7B9C0D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppNameEn}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; 安装包输出文件
OutputDir=installer
OutputBaseFilename=TranslateTool_Setup_{#MyAppVersion}
; 压缩选项
Compression=lzma2/ultra64
SolidCompression=yes
; 外观选项
WizardStyle=modern
; 权限选项
PrivilegesRequired=lowest
; 语言选项
LanguageDetectionMethod=uilanguage
ShowLanguageDialog=yes
; 其他选项
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} 安装程序
VersionInfoCopyright=Copyright © 2026 {#MyAppPublisher}

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1

[Files]
; 主程序文件
Source: "src\bin\Release\net8.0-windows\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
; 依赖文件
Source: "src\bin\Release\net8.0-windows\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "src\bin\Release\net8.0-windows\*.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "src\bin\Release\net8.0-windows\*.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "src\bin\Release\net8.0-windows\*.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
; OCR 语言包
Source: "src\bin\Release\net8.0-windows\tessdata\*"; DestDir: "{app}\tessdata"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist
; 资源文件
Source: "src\bin\Release\net8.0-windows\Resources\*"; DestDir: "{app}\Resources"; Flags: ignoreversion recursesubdirs createallsubdirs
; 下载脚本
Source: "download-tessdata.ps1"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Dirs]
Name: "{app}\tessdata"; Flags: uninsalwaysuninstall
Name: "{app}\Resources"; Flags: uninsalwaysuninstall

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
; 安装完成后运行
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; 卸载前关闭程序
Filename: "{cmd}"; Parameters: "/C taskkill /F /IM {#MyAppExeName}"; Flags: runhidden

[UninstallDelete]
Type: filesandordirs; Name: "{app}\tessdata"
Type: filesandordirs; Name: "{app}\Resources"
Type: files; Name: "{app}\translation_history.json"
Type: files; Name: "{app}\app_settings.json"

[Registry]
; 开机自启动（可选）
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "TranslateTool"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue noerror

[Code]
// 自定义安装逻辑
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // 安装完成后的操作
  end;
end;

// 检查是否已安装
function InitializeSetup(): Boolean;
var
  Version: String;
begin
  Result := True;
  if RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppId}_is1', 'DisplayVersion', Version) then
  begin
    if MsgBox('检测到已安装版本 ' + Version + '，是否继续安装？', mbConfirmation, MB_YESNO) = IDNO then
      Result := False;
  end;
end;
