[CmdletBinding()]
param([string]$IsccPath)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Find-InnoCompiler {
    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    $candidates = @(
        $IsccPath
        if ($command) { $command.Source }
        if (${env:ProgramFiles(x86)}) { Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6/ISCC.exe' }
        Join-Path $env:ProgramFiles 'Inno Setup 6/ISCC.exe'
        Join-Path $env:LOCALAPPDATA 'Programs/Inno Setup 6/ISCC.exe'
    )
    if ($IsccPath) { $candidates = @($IsccPath) }
    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            return (Get-Item -LiteralPath $candidate).FullName
        }
    }
    throw 'Требуется ISCC.exe из Inno Setup 6.3 или более свежей версии ветки 6. Укажите путь через -IsccPath.'
}

function Get-LatestDesktopRuntime {
    $metadataUri = 'https://builds.dotnet.microsoft.com/dotnet/release-metadata/8.0/releases.json'
    $metadata = Invoke-RestMethod -Uri $metadataUri -MaximumRedirection 0
    $release = @($metadata.releases | Where-Object { $_.'release-version' -eq $metadata.'latest-release' })
    if ($release.Count -ne 1 -or $release[0].windowsdesktop.version -notmatch '^8\.0\.\d+$') {
        throw 'Официальные метаданные не содержат однозначного актуального выпуска Desktop Runtime 8.'
    }
    $desktop = $release[0].windowsdesktop
    $filename = "windowsdesktop-runtime-$($desktop.version)-win-x64.exe"
    $files = @($desktop.files | Where-Object { $_.rid -eq 'win-x64' -and ([uri]$_.url).AbsolutePath.EndsWith("/$filename") })
    if ($files.Count -ne 1 -or $files[0].hash -notmatch '^[a-fA-F0-9]{128}$') {
        throw 'В метаданных отсутствует официальный EXE для win-x64 или его SHA-512.'
    }
    $uri = [uri]$files[0].url
    if ($uri.Scheme -ne 'https' -or $uri.Host -notin @('dotnet.microsoft.com', 'builds.dotnet.microsoft.com') -or $uri.UserInfo) {
        throw 'Адрес runtime не принадлежит разрешённому источнику Microsoft.'
    }
    [pscustomobject]@{ Version = $desktop.version; Date = $release[0].'release-date'; Name = $filename; Uri = $uri; Hash = $files[0].hash }
}

function Confirm-DesktopRuntime([string]$Path, [string]$ExpectedHash) {
    if ((Get-FileHash -LiteralPath $Path -Algorithm SHA512).Hash -ne $ExpectedHash) {
        throw "SHA-512 не совпал с официальными метаданными Microsoft: $Path"
    }
    $signature = Get-AuthenticodeSignature -LiteralPath $Path
    if ($signature.Status -ne 'Valid' -or
        $signature.SignerCertificate.Subject -notmatch '(^|,\s*)O=Microsoft Corporation(,|$)') {
        throw "Подпись Microsoft не подтверждена: $($signature.Status). Пакет не будет использован."
    }
}

function Get-VerifiedRuntimeFile($Runtime, [string]$Cache, [string]$Work) {
    $cached = Join-Path $Cache $Runtime.Name
    $staged = Join-Path $Work $Runtime.Name
    $cacheHit = Test-Path -LiteralPath $cached -PathType Leaf
    if ($cacheHit) {
        Copy-Item -LiteralPath $cached -Destination $staged
    }
    else {
        Invoke-WebRequest -Uri $Runtime.Uri -OutFile $staged -MaximumRedirection 0 -UseBasicParsing
    }
    Confirm-DesktopRuntime $staged $Runtime.Hash
    if (-not $cacheHit) {
        New-Item -ItemType Directory -Path $Cache -Force | Out-Null
        Copy-Item -LiteralPath $staged -Destination $cached
    }
    return $staged
}

function Confirm-Payload([string]$Path) {
    $required = @(
        'CircuitEditor.App.exe', 'CircuitEditor.App.runtimeconfig.json', 'Code/Arduino.xshd',
        'Fonts/JetBrainsMono-Regular.ttf', 'Fonts/JetBrainsMono-Bold.ttf', 'Data/breadboard.json',
        'Library/controllers.json', 'LICENSE', 'THIRD-PARTY-NOTICES.md', 'licenses/JetBrainsMono-OFL-1.1.txt',
        'Samples/index.json'
    )
    foreach ($item in $required) {
        if (-not (Test-Path -LiteralPath (Join-Path $Path $item) -PathType Leaf)) { throw "В публикации нет $item." }
    }
    if (@(Get-ChildItem -LiteralPath (Join-Path $Path 'Samples') -Filter '*.circ' -File).Count -ne 9) {
        throw 'Ожидалось девять учебных проектов.'
    }
    $config = Get-Content -LiteralPath (Join-Path $Path 'CircuitEditor.App.runtimeconfig.json') -Raw | ConvertFrom-Json
    if (@($config.runtimeOptions.frameworks | Where-Object { $_.name -eq 'Microsoft.WindowsDesktop.App' -and $_.version -match '^8\.' }).Count -ne 1) {
        throw 'Публикация должна ссылаться на внешний Microsoft.WindowsDesktop.App 8.'
    }
    if (Get-ChildItem -LiteralPath $Path -File -Recurse | Where-Object { $_.Extension -eq '.pdb' -or $_.Name -eq 'coreclr.dll' -or $_.Name -like 'CircuitEditor.Tests*' }) {
        throw 'В публикацию попали символы, тесты или автономная среда выполнения.'
    }
}

$compiler = Find-InnoCompiler
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$workRoot = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) 'CircuitEditor-work/installer'))
$work = Join-Path $workRoot ([guid]::NewGuid().ToString('N'))
$savedTemp = $env:TEMP
$savedTmp = $env:TMP

try {
    New-Item -ItemType Directory -Path $work -Force | Out-Null
    $env:TEMP = $work
    $env:TMP = $work
    $runtime = Get-LatestDesktopRuntime
    $runtimeFile = Get-VerifiedRuntimeFile $runtime (Join-Path $PSScriptRoot '.cache') $work
    Write-Host ".NET Desktop Runtime $($runtime.Version), выпуск $($runtime.Date), SHA-512 и подпись проверены."

    $payload = Join-Path $work 'payload'
    dotnet publish (Join-Path $repo 'CircuitEditor.App') -c Release -r win-x64 --self-contained false `
        --artifacts-path (Join-Path $work 'build') -o $payload -p:DebugType=none -p:DebugSymbols=false `
        -p:UseSharedCompilation=false -nodeReuse:false
    if ($LASTEXITCODE -ne 0) { throw 'Не удалось собрать публикацию Circuit Editor.' }
    Confirm-Payload $payload

    $metadata = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $payload 'CircuitEditor.App.exe'))
    $version = $metadata.ProductVersion -replace '\+.*$'
    if (-not $metadata.CompanyName -or -not $metadata.LegalCopyright -or -not $metadata.ProductName -or -not $metadata.FileDescription) {
        throw 'В опубликованном EXE не заполнены метаданные продукта, автора или описания.'
    }
    if ($version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') { throw "Недопустимая версия приложения: $version" }
    $compiled = Join-Path $work 'output'
    & $compiler /Qp "/DAppVersion=$version" "/DPayloadDir=$payload" "/DRuntimeInstaller=$runtimeFile" `
        "/DSetupOutput=$compiled" "/DAppAuthor=$($metadata.CompanyName)" "/DAppCopyrightText=$($metadata.LegalCopyright)" `
        (Join-Path $PSScriptRoot 'CircuitEditor.iss')
    if ($LASTEXITCODE -ne 0) { throw 'Компилятор Inno Setup завершился с ошибкой.' }

    $target = Join-Path $repo 'start/setup.exe'
    Copy-Item -LiteralPath (Join-Path $compiled 'setup.exe') -Destination $target
    $file = Get-Item -LiteralPath $target
    $hash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
    Write-Host "Setup: $($file.FullName)"
    Write-Host "Size: $($file.Length) bytes"
    Write-Host "SHA-256: $hash"
}
finally {
    $env:TEMP = $savedTemp
    $env:TMP = $savedTmp
    if (Test-Path -LiteralPath $work) {
        $resolved = (Resolve-Path -LiteralPath $work).Path
        if (-not $resolved.Equals($work, [StringComparison]::OrdinalIgnoreCase) -or
            (Get-Item -LiteralPath $resolved).Attributes.HasFlag([IO.FileAttributes]::ReparsePoint)) {
            throw 'Не удалось подтвердить путь очистки рабочей папки.'
        }
        Remove-Item -LiteralPath $resolved -Recurse
    }
    if ((Test-Path -LiteralPath $workRoot) -and -not (Get-ChildItem -LiteralPath $workRoot -Force)) {
        Remove-Item -LiteralPath $workRoot
    }
}
