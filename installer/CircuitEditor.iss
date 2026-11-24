#if Ver < EncodeVer(6, 3, 0) || Ver >= EncodeVer(7, 0, 0)
  #error Circuit Editor requires Inno Setup 6.3 or a later 6.x release.
#endif
#define AppName "Circuit Editor"
#define AppExe "CircuitEditor.App.exe"
#define RuntimeName "desktop-runtime-x64.exe"
#define RuntimePage "https://dotnet.microsoft.com/download/dotnet/8.0"

[Setup]
AppId={{CBF69278-6CC9-451F-9878-15B33518F13C}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Abduseliov Ismail
VersionInfoVersion={#AppVersion}
DefaultDirName={autopf}\CircuitEditor
DefaultGroupName=Circuit Editor
UninstallDisplayIcon={app}\{#AppExe}
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64os
MinVersion=10.0.14393
PrivilegesRequired=admin
DisableWelcomePage=no
DisableDirPage=no
DisableProgramGroupPage=no
LicenseFile=..\LICENSE
CloseApplications=yes
CloseApplicationsFilter=*.exe,*.dll
RestartApplications=no
Compression=lzma2/ultra
SolidCompression=yes
OutputDir={#SetupOutput}
OutputBaseFilename=setup

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"

[Messages]
en.WindowsVersionNotSupported=This installer requires Windows 10 version 1607 or later, or Windows 11, on an x64 PC.
ru.WindowsVersionNotSupported=Для установки требуется Windows 10 версии 1607 или новее либо Windows 11 на компьютере x64.

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#RuntimeInstaller}"; DestName: "{#RuntimeName}"; Flags: dontcopy
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent runasoriginaluser; Check: CanLaunchApp

[CustomMessages]
en.RuntimePrompt=Circuit Editor requires .NET 8 Desktop Runtime (x64). Install it now from this setup file?
ru.RuntimePrompt=Для работы Circuit Editor нужен .NET 8 Desktop Runtime (x64). Установить сейчас из этого файла?
en.RuntimeProgress=Installing .NET Desktop Runtime 8 (x64)
ru.RuntimeProgress=Установка .NET Desktop Runtime 8 (x64)
en.RuntimeWait=Please wait for the Microsoft installer to finish.
ru.RuntimeWait=Дождитесь завершения установщика Microsoft.
en.RuntimeMissing=Circuit Editor is installed. It needs .NET Desktop Runtime 8 (x64) before it can run.
ru.RuntimeMissing=Circuit Editor установлен. Для запуска нужен .NET Desktop Runtime 8 (x64).
en.RuntimeFailed=The Microsoft installer returned code %1.
ru.RuntimeFailed=Установщик Microsoft вернул код %1.
en.RuntimeException=The embedded runtime installer could not be extracted or started.
ru.RuntimeException=Не удалось извлечь или запустить встроенный установщик .NET.
en.RuntimeRestart=.NET Desktop Runtime was installed. Restart Windows before launching Circuit Editor.
ru.RuntimeRestart=.NET Desktop Runtime установлен. Перезагрузите Windows перед запуском Circuit Editor.
en.RuntimeLink=Open Microsoft's official .NET 8 download page?
ru.RuntimeLink=Открыть официальную страницу загрузки .NET 8?
en.DataDelete=Delete Circuit Editor data belonging to the current Windows account (logs, recent projects and chip library)?
ru.DataDelete=Удалить данные Circuit Editor текущей учётной записи Windows (журнал, список недавних проектов и библиотеку микросхем)?
en.DataRetained=User data was kept. The uninstall account could not be matched to the signed-in desktop user or the data folder contains a redirected path.
ru.DataRetained=Данные сохранены. Не удалось подтвердить совпадение учётных записей удаления и рабочего стола либо папка данных содержит перенаправление.
en.DataDeleteFailed=Some Circuit Editor user data could not be deleted.
ru.DataDeleteFailed=Не удалось удалить часть данных пользователя Circuit Editor.

[Code]
const
  RuntimeRegistry = 'SOFTWARE\dotnet\Setup\InstalledVersions\x64';
  DirectoryAttribute = $10;
  ReparseAttribute = $400;
  MissingFileAttributes = $FFFFFFFF;

var
  ReadyToLaunch: Boolean;
  RuntimeProgress: TOutputMarqueeProgressWizardPage;

function GetFileAttributes(const FileName: String): Cardinal;
  external 'GetFileAttributesW@kernel32.dll stdcall';
function GetCurrentProcessId: Cardinal;
  external 'GetCurrentProcessId@kernel32.dll stdcall';
function GetShellWindow: HWND;
  external 'GetShellWindow@user32.dll stdcall';
function GetWindowThreadProcessId(Wnd: HWND; var ProcessId: Cardinal): Cardinal;
  external 'GetWindowThreadProcessId@user32.dll stdcall';

function StableRuntime8(const Version: String): Boolean;
var
  PatchText: String;
  Patch: Integer;
begin
  PatchText := Copy(Version, 5, Length(Version));
  Patch := StrToIntDef(PatchText, -1);
  Result := (Copy(Version, 1, 4) = '8.0.') and
    (Patch >= 0) and (IntToStr(Patch) = PatchText);
end;

function HasRuntimeVersion(const Root, Version: String): Boolean;
var
  Desktop, Core: String;
begin
  Desktop := AddBackslash(Root) + 'shared\Microsoft.WindowsDesktop.App\' + Version;
  Core := AddBackslash(Root) + 'shared\Microsoft.NETCore.App\' + Version;
  Result := StableRuntime8(Version) and FileExists(Desktop + '\PresentationFramework.dll') and
    FileExists(Desktop + '\Microsoft.WindowsDesktop.App.deps.json') and
    FileExists(Core + '\System.Private.CoreLib.dll');
end;

function HasRuntimeAt(const Root: String): Boolean;
var
  Find: TFindRec;
begin
  Result := False;
  if (Root = '') or not FileExists(AddBackslash(Root) + 'dotnet.exe') then Exit;
  if FindFirst(AddBackslash(Root) + 'shared\Microsoft.WindowsDesktop.App\8.0.*', Find) then
    try
      repeat
        Result := ((Find.Attributes and DirectoryAttribute) <> 0) and HasRuntimeVersion(Root, Find.Name);
      until Result or not FindNext(Find);
    finally
      FindClose(Find);
    end;
end;

function DesktopRuntimeInstalled: Boolean;
var
  Root: String;
  Versions: TArrayOfString;
  I: Integer;
begin
  if RegQueryStringValue(HKLM64, RuntimeRegistry, 'InstallLocation', Root) then
    if RegGetValueNames(HKLM64, RuntimeRegistry + '\sharedfx\Microsoft.WindowsDesktop.App', Versions) then
      for I := 0 to GetArrayLength(Versions) - 1 do
        if FileExists(AddBackslash(Root) + 'dotnet.exe') and HasRuntimeVersion(Root, Versions[I]) then
        begin
          Result := True;
          Exit;
        end;
  Result := HasRuntimeAt(Root) or HasRuntimeAt(ExpandConstant('{pf64}\dotnet'));
end;

procedure InitializeWizard;
begin
  RuntimeProgress := CreateOutputMarqueeProgressPage(CustomMessage('RuntimeProgress'), CustomMessage('RuntimeWait'));
end;

function CanLaunchApp: Boolean;
begin
  Result := ReadyToLaunch;
end;

procedure ReportMissingRuntime(const Details: String);
var
  MessageText: String;
  ErrorCode: Integer;
begin
  MessageText := Details;
  if MessageText <> '' then MessageText := MessageText + #13#10;
  MessageText := MessageText + CustomMessage('RuntimeMissing') + #13#10 + '{#RuntimePage}';
  Log(MessageText);
  if not WizardSilent then
    if MsgBox(MessageText + #13#10#13#10 + CustomMessage('RuntimeLink'), mbError, MB_YESNO or MB_DEFBUTTON2) = IDYES then
      if not ShellExecAsOriginalUser('open', '{#RuntimePage}', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode) then
        Log('Could not open the .NET download page, code ' + IntToStr(ErrorCode));
end;

procedure InstallDesktopRuntime;
var
  ExitCode: Integer;
  Started: Boolean;
begin
  ReadyToLaunch := DesktopRuntimeInstalled;
  if ReadyToLaunch then
  begin
    Log('.NET Desktop Runtime 8 x64 is already available.');
    Exit;
  end;
  if not WizardSilent then
    if MsgBox(CustomMessage('RuntimePrompt'), mbConfirmation, MB_YESNO or MB_DEFBUTTON1) <> IDYES then
    begin
      ReportMissingRuntime('');
      Exit;
    end;

  Started := False;
  ExitCode := -1;
  try
    if not WizardSilent then
    begin
      RuntimeProgress.Show;
      RuntimeProgress.Animate;
    end;
    try
      ExtractTemporaryFile('{#RuntimeName}');
      Started := Exec(ExpandConstant('{tmp}\{#RuntimeName}'), '/install /quiet /norestart',
        '', SW_HIDE, ewWaitUntilTerminated, ExitCode);
    finally
      if not WizardSilent then RuntimeProgress.Hide;
    end;
  except
    Log(GetExceptionMessage);
    ReportMissingRuntime(CustomMessage('RuntimeException'));
    Exit;
  end;

  Log('Microsoft runtime installer result: ' + IntToStr(ExitCode));
  if not Started then
    ReportMissingRuntime(FmtMessage(CustomMessage('RuntimeFailed'), [IntToStr(ExitCode)]))
  else if ExitCode = 3010 then
  begin
    Log(CustomMessage('RuntimeRestart'));
    if not WizardSilent then MsgBox(CustomMessage('RuntimeRestart'), mbInformation, MB_OK);
  end
  else if (ExitCode = 0) or (ExitCode = 1638) then
  begin
    ReadyToLaunch := DesktopRuntimeInstalled;
    if not ReadyToLaunch then ReportMissingRuntime('');
  end
  else
    ReportMissingRuntime(FmtMessage(CustomMessage('RuntimeFailed'), [IntToStr(ExitCode)]));
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then InstallDesktopRuntime;
end;

function IsDesktopUser: Boolean;
var
  DesktopProcess: Cardinal;
  ShellWindow: HWND;
  Locator, Service, SelfProcess, Desktop, SelfOwner, DesktopOwner: Variant;
begin
  Result := False;
  ShellWindow := GetShellWindow;
  if ShellWindow = 0 then Exit;
  if GetWindowThreadProcessId(ShellWindow, DesktopProcess) = 0 then Exit;
  try
    Locator := CreateOleObject('WbemScripting.SWbemLocator');
    Service := Locator.ConnectServer('.', 'root\cimv2');
    SelfProcess := Service.Get('Win32_Process.Handle="' + IntToStr(GetCurrentProcessId) + '"');
    Desktop := Service.Get('Win32_Process.Handle="' + IntToStr(DesktopProcess) + '"');
    SelfOwner := SelfProcess.ExecMethod_('GetOwnerSid');
    DesktopOwner := Desktop.ExecMethod_('GetOwnerSid');
    Result := (SelfOwner.ReturnValue = 0) and (DesktopOwner.ReturnValue = 0) and
      (SelfOwner.Sid <> '') and (SelfOwner.Sid = DesktopOwner.Sid);
  except
    Result := False;
  end;
end;

function ContainsRedirectedDirectory(const Path: String): Boolean;
var
  Find: TFindRec;
  Attributes: Cardinal;
begin
  Attributes := GetFileAttributes(Path);
  Result := (Attributes = MissingFileAttributes) or ((Attributes and ReparseAttribute) <> 0);
  if Result then Exit;
  if FindFirst(AddBackslash(Path) + '*', Find) then
    try
      repeat
        if (Find.Name <> '.') and (Find.Name <> '..') then
        begin
          Result := (Find.Attributes and ReparseAttribute) <> 0;
          if not Result and ((Find.Attributes and DirectoryAttribute) <> 0) then
            Result := ContainsRedirectedDirectory(AddBackslash(Path) + Find.Name);
        end;
      until Result or not FindNext(Find);
    finally
      FindClose(Find);
    end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Root, Parent: String;
begin
  if (CurUninstallStep <> usPostUninstall) or UninstallSilent then Exit;
  Root := ExpandConstant('{localappdata}\CircuitEditor');
  if not DirExists(Root) then Exit;
  if MsgBox(CustomMessage('DataDelete'), mbConfirmation, MB_YESNO or MB_DEFBUTTON2) <> IDYES then Exit;

  Parent := Root;
  while Length(Parent) > 3 do
  begin
    if (GetFileAttributes(Parent) and ReparseAttribute) <> 0 then
    begin
      MsgBox(CustomMessage('DataRetained'), mbInformation, MB_OK);
      Exit;
    end;
    Parent := ExtractFileDir(Parent);
  end;
  if not IsDesktopUser or ContainsRedirectedDirectory(Root) then
    MsgBox(CustomMessage('DataRetained'), mbInformation, MB_OK)
  else if not DelTree(Root, True, True, True) then
    MsgBox(CustomMessage('DataDeleteFailed'), mbError, MB_OK);
end;
