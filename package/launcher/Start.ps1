$ErrorActionPreference='Stop'
try {
    $host32=Join-Path $env:WINDIR 'SysWOW64/WindowsPowerShell/v1.0/powershell.exe'
    if(-not(Test-Path -LiteralPath $host32)){throw '需要64位Windows和系统自带Windows PowerShell。'}
    $play=Join-Path $PSScriptRoot 'Play.ps1'
    $child=Start-Process -FilePath $host32 -Verb RunAs -WindowStyle Normal -ArgumentList @('-NoLogo','-NoProfile','-ExecutionPolicy','Bypass','-File',('"'+$play+'"')) -PassThru -Wait
    exit $child.ExitCode
} catch {Write-Host $_.Exception.Message -ForegroundColor Red;Read-Host '按回车关闭'|Out-Null;exit 1}
