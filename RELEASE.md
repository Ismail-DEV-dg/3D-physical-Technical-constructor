# Публикация Circuit Editor для Windows x64

Из корня репозитория на Windows с .NET 8 SDK и Inno Setup 6:

```powershell
.\installer\build-installer.ps1
```

Основной файл релиза — `start/setup.exe`. Он устанавливает Circuit Editor и при необходимости разворачивает встроенный .NET Desktop Runtime 8 x64. Подробности и проверка описаны в [installer/README.md](installer/README.md).

В GitHub откройте **Releases → Draft a new release**, укажите версию, вставьте [описание релиза](docs/release-notes.md) и приложите `setup.exe` и, при необходимости, переносимый `CircuitEditor-win-x64.zip`. Обе поставки игнорируются Git.

**SHA-256 setup.exe** `A7EE6B453EC69EA38EFB6038349445A5A541EC1EFDF0BB37FD4C44473B1CEB4C`.

EXE-файлы Circuit Editor не подписаны. Для этого выпуска подпись сертификатом не предусматривалась, поэтому SmartScreen может показать предупреждение. Встроенный установщик .NET подписан Microsoft.

## Переносимый ZIP

Для новой сборки используйте пустую папку публикации. ZIP включает собственную копию .NET и распаковывается целиком.

```powershell
$work = Join-Path $env:TEMP 'CircuitEditor-work/final'
dotnet publish CircuitEditor.App -c Release -r win-x64 --self-contained true `
  --artifacts-path "$work/publish-build" -p:DebugType=none -p:DebugSymbols=false `
  -o artifacts/CircuitEditor-win-x64
if ($LASTEXITCODE -ne 0) { throw 'Публикация не собрана.' }
Compress-Archive -LiteralPath artifacts/CircuitEditor-win-x64 `
  -DestinationPath artifacts/CircuitEditor-win-x64.zip
```
