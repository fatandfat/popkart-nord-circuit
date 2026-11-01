param([string]$ClientPath,[switch]$NonInteractive)
. (Join-Path $PSScriptRoot 'Common.ps1')
try {
    Assert-Package
    if(Test-Path -LiteralPath (Join-Path $appRoot 'installed.json')){Assert-Installed;Write-Host '已经安装完成，请运行 2-Play.cmd。' -ForegroundColor Green;exit 0}
    if(-not $ClientPath){$ClientPath=Read-Host '请输入原版韩服P5136客户端目录（里面应有KartRider.exe），然后按回车'}
    $ClientPath=$ClientPath.Trim().Trim('"')
    $source=[IO.Path]::GetFullPath((Resolve-Path -LiteralPath $ClientPath).Path).TrimEnd('\')
    $destination=[IO.Path]::GetFullPath($gameDir).TrimEnd('\')
    if($destination.StartsWith($source+'\',[StringComparison]::OrdinalIgnoreCase) -or $source.StartsWith($destination+'\',[StringComparison]::OrdinalIgnoreCase) -or $source -eq $destination){throw '请使用补丁包外的原版客户端目录。'}
    Assert-Hash (Join-Path $source 'KartRider.exe') $manifest.clientExeSha256
    Assert-Hash (Join-Path $source 'Data/track_village_R01.rho') $manifest.baseRhoSha256
    Assert-Hash (Join-Path $source 'Data/aaa.pk') $manifest.baseAaaSha256
    $files=Get-Content -LiteralPath (Join-Path $appRoot 'client-files.json') -Raw | ConvertFrom-Json
    $defaults=@('KartRider.pin','KartRider.xml','mav.dat')
    foreach($f in $files){
        if($f.path -match '(^|[\\/])\.\.?([\\/]|$)|(^|[\\/])\.zcode([\\/]|$)' -or [IO.Path]::IsPathRooted($f.path)){throw '客户端文件清单包含非法路径。'}
        $checkPath=Join-Path $source $f.path
        if($f.path -in $defaults){$checkPath=Join-Path $appRoot ('defaults/'+$f.path)}
        $item=Get-Item -LiteralPath $checkPath -ErrorAction Stop
        if($item.Length -ne $f.bytes){throw ('客户端文件不完整或版本不同：'+$f.path)}
    }
    if(Test-Path -LiteralPath $destination){throw '安装目录已经存在但未完成安装。请重新解压补丁到新的空目录后重试；原目录已保留。'}
    Write-Host '正在创建独立客户端副本，约需4GB空间；不会修改原客户端或复制个人存档。'
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    $count=0
    foreach($f in $files){
        $src=Join-Path $source $f.path
        if($f.path -in $defaults){$src=Join-Path $appRoot ('defaults/'+$f.path)}
        $dst=Join-Path $destination $f.path
        New-Item -ItemType Directory -Path (Split-Path -Parent $dst) -Force | Out-Null
        Copy-Item -LiteralPath $src -Destination $dst
        $count++;if($count%100 -eq 0){Write-Progress -Activity '安装独立客户端' -Status "$count / $($files.Count)" -PercentComplete ($count*100/$files.Count)}
    }
    Write-Progress -Activity '安装独立客户端' -Completed
    foreach($name in @('track_village_R01.rho','aaa.pk')){Copy-Item -LiteralPath (Join-Path $appRoot ('payload/'+$name)) -Destination (Join-Path $destination ('Data/'+$name)) -Force}
    $serverDir=Join-Path $appRoot 'server'
    if(Test-Path -LiteralPath $serverDir){throw '服务端目录已存在，请重新解压到空目录后重试。'}
    Expand-Archive -LiteralPath (Join-Path $appRoot 'vendor/p5136-windows-x64.zip') -DestinationPath $serverDir
    Assert-Hash $gameExe $manifest.clientExeSha256
    Assert-Hash (Join-Path $destination 'Data/track_village_R01.rho') $manifest.finalRhoSha256
    Assert-Hash (Join-Path $destination 'Data/aaa.pk') $manifest.finalAaaSha256
    Assert-Hash (Join-Path $serverDir 'p5136.exe') $manifest.serverExeSha256
    [ordered]@{version=$manifest.version;installedUtc=(Get-Date).ToUniversalTime().ToString('o');clientFiles=$files.Count;personalProfilesCopied=$false;relativeGameDirectory='runtime/nord-full-v10/KartRider_5136'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $appRoot 'installed.json') -Encoding UTF8
    Write-Host '安装完成！以后双击 2-Play.cmd 即可启动。' -ForegroundColor Green
} catch {Write-Host ('安装未完成：'+$_.Exception.Message) -ForegroundColor Red;if(-not $NonInteractive){Read-Host '按回车关闭'|Out-Null};exit 1}
if(-not $NonInteractive){Read-Host '按回车关闭'|Out-Null}
