# Публикация Circuit Editor для Windows x64

Из корня репозитория на Windows с .NET 8 SDK. Для новой поставки используйте пустую выходную папку.

```powershell
$work = Join-Path $env:TEMP 'CircuitEditor-work/final'
dotnet publish CircuitEditor.App -c Release -r win-x64 --self-contained true `
  --artifacts-path "$work/publish-build" -p:DebugType=none -p:DebugSymbols=false `
  -o artifacts/CircuitEditor-win-x64
if ($LASTEXITCODE -ne 0) { throw 'Публикация не собрана.' }
Compress-Archive -LiteralPath artifacts/CircuitEditor-win-x64 `
  -DestinationPath artifacts/CircuitEditor-win-x64.zip
```

Результат — `artifacts/CircuitEditor-win-x64/CircuitEditor.App.exe` и архив `artifacts/CircuitEditor-win-x64.zip`. До загрузки распакуйте архив отдельно, откройте EXE, выберите **Файл → Примеры → Uno · мигающий светодиод** и нажмите **Старт схемы**.

В GitHub откройте **Releases → Draft a new release**, выберите версию, скопируйте описание из [docs/release-notes.md](docs/release-notes.md) и приложите ZIP одним файлом. Папка публикации и архив игнорируются Git.
