param(
    [Parameter(Mandatory=$true)][string]$ClientDirectory,
    [Parameter(Mandatory=$true)][string]$Destination,
    [string]$PatchArchive
)
$ErrorActionPreference='Stop'
$zipName='Nordschleife-P5136-Final-1.5x-v1.zip'
$folderName='Nordschleife-P5136-Final-1.5x-v1'
$url='https://github.com/fatandfat/popkart-nord-circuit/releases/download/v1.0.0/'+$zipName
$expected='a0f182a058ce663e1b34ede6ad28b1f7c60c56ce4c100b7d41d7f17ae9a4fb07'
try {
    $client=[IO.Path]::GetFullPath((Resolve-Path -LiteralPath $ClientDirectory).Path).TrimEnd('\')
    $destinationFull=[IO.Path]::GetFullPath($Destination).TrimEnd('\')
    if($destinationFull -eq $client -or $destinationFull.StartsWith($client+'\',[StringComparison]::OrdinalIgnoreCase) -or $client.StartsWith($destinationFull+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Choose a destination separate from the base client.'}
    $exe=Join-Path $client 'KartRider.exe'
    if((Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant() -ne '629f084e2a12c6fa1ff0ea603b90f8768454d13a1bc2df6a8504f8aa06fd6194'){throw 'Unsupported base client executable.'}
    if(Test-Path -LiteralPath $destinationFull){throw 'Destination must be a new directory. Existing files are preserved.'}
    if($PatchArchive){$zip=(Resolve-Path -LiteralPath $PatchArchive).Path}else{$zip=Join-Path $destinationFull $zipName}
    New-Item -ItemType Directory -Path $destinationFull | Out-Null
    if(-not $PatchArchive){
        Write-Host 'Downloading the pinned release patch...'
        $curl=Get-Command curl.exe -ErrorAction SilentlyContinue
        if($curl){& $curl.Source --fail --location --retry 3 --output $zip $url;if($LASTEXITCODE -ne 0){throw 'Release download failed; partial files preserved.'}}
        else{[Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12;Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing}
    }
    if((Get-Item -LiteralPath $zip).Length -ne 155875922){throw 'Patch archive size mismatch.'}
    if((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expected){throw 'Patch archive SHA256 mismatch.'}
    Expand-Archive -LiteralPath $zip -DestinationPath $destinationFull
    $package=Join-Path $destinationFull $folderName
    & powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File (Join-Path $package 'launcher/Install.ps1') -ClientPath $client -NonInteractive
    if($LASTEXITCODE -ne 0){throw 'Installation failed. Preserve the output and use a fresh directory for a retry.'}
    $ps32=Join-Path $env:WINDIR 'SysWOW64/WindowsPowerShell/v1.0/powershell.exe'
    & $ps32 -NoLogo -NoProfile -ExecutionPolicy Bypass -File (Join-Path $package 'launcher/Play.ps1') -VerifyOnly
    if($LASTEXITCODE -ne 0){throw 'Installed client verification failed.'}
    Write-Host ('READY: '+(Join-Path $package '2-Play.cmd')) -ForegroundColor Green
    Write-Host 'No game was launched. Follow the guide: approve UAC, wait in lobby, press Enter in the helper before starting a race.'
} catch {Write-Error $_.Exception.Message;exit 1}
