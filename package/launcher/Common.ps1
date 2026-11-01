$ErrorActionPreference='Stop'
$packageRoot=Split-Path -Parent $PSScriptRoot
$appRoot=Join-Path $packageRoot 'app'
$gameDir=Join-Path $appRoot 'runtime/nord-full-v10/KartRider_5136'
$gameExe=Join-Path $gameDir 'KartRider.exe'
$manifest=Get-Content -LiteralPath (Join-Path $appRoot 'manifest.json') -Raw | ConvertFrom-Json
function Assert-Hash([string]$Path,[string]$Expected) {
    if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){throw ('缺少文件：'+$Path)}
    if((Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $Expected){throw ('文件版本或内容不匹配：'+$Path)}
}
function Assert-Package {
    foreach($f in $manifest.packageFiles){Assert-Hash (Join-Path $appRoot $f.path) $f.sha256}
}
function Assert-Installed {
    if(-not(Test-Path -LiteralPath (Join-Path $appRoot 'installed.json'))){throw '请先运行 1-Install.cmd 完成安装。'}
    Assert-Hash $gameExe $manifest.clientExeSha256
    Assert-Hash (Join-Path $gameDir 'Data/track_village_R01.rho') $manifest.finalRhoSha256
    Assert-Hash (Join-Path $gameDir 'Data/aaa.pk') $manifest.finalAaaSha256
    Assert-Hash (Join-Path $appRoot 'server/p5136.exe') $manifest.serverExeSha256
}
