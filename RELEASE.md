# Публикация Circuit Editor для Windows x64

Из корня репозитория на Windows с .NET 8 SDK и Inno Setup 6:

```powershell
.\installer\build-installer.ps1
```

Основной файл релиза — `start/setup.exe`. Он устанавливает Circuit Editor и при необходимости разворачивает встроенный .NET Desktop Runtime 8 x64. Подробности и проверка описаны в [installer/README.md](installer/README.md).

В GitHub откройте **Releases → Draft a new release**. Выберите целевую ветку `main`, задайте тег `v1.0.0` и заголовок `Circuit Editor 1.0.0`. Вставьте описание релиза и приложите `setup.exe` и `CircuitEditor-win-x64.zip`. Обе поставки игнорируются Git. [Описание релиза](docs/release-notes.md)

Ожидаемые размеры и контрольные суммы публикуемых файлов указаны в [описании релиза](docs/release-notes.md).

EXE-файлы Circuit Editor не подписаны. Для этого выпуска подпись сертификатом не предусматривалась, поэтому SmartScreen может показать предупреждение. Встроенный установщик .NET подписан Microsoft.

## Переносимый ZIP

Для новой сборки используйте пустую папку публикации. ZIP включает собственную копию .NET и распаковывается целиком.

```powershell
$work = Join-Path $env:TEMP 'CircuitEditor-work/merge'
dotnet publish CircuitEditor.App -c Release -r win-x64 --self-contained true `
  --artifacts-path "$work/publish-build" -p:DebugType=none -p:DebugSymbols=false `
  -o "$work/CircuitEditor-win-x64"
if ($LASTEXITCODE -ne 0) { throw 'Публикация не собрана.' }
Compress-Archive -LiteralPath "$work/CircuitEditor-win-x64" `
  -DestinationPath artifacts/CircuitEditor-win-x64.zip
```

Загрузите оба файла в черновик GitHub Releases и дождитесь окончания загрузки. После публикации скачайте `setup.exe` и сравните его SHA-256 с опубликованной суммой командой `Get-FileHash -Algorithm SHA256 -LiteralPath <путь>`.
