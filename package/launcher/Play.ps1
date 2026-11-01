param([switch]$VerifyOnly,[switch]$ServerCheckOnly)
. (Join-Path $PSScriptRoot 'Common.ps1')
. (Join-Path $PSScriptRoot 'NativeLog.ps1')
$ownedServer=$null;$launchedGame=$null;$failed=$false
try {
    if([IntPtr]::Size -ne 4){throw '请通过2-Play.cmd启动。'}
    Assert-Package
    Assert-Installed
    Add-Type -Path (Join-Path $PSScriptRoot 'RangePatch.cs')
    if($VerifyOnly){Write-Host 'PASS package hashes, installed client and32-bit patch compilation';exit 0}
    if(-not $ServerCheckOnly){
        $principal=New-Object Security.Principal.WindowsPrincipal ([Security.Principal.WindowsIdentity]::GetCurrent())
        if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw '请通过2-Play.cmd启动并同意UAC。'}
        if(Get-Process KartRider -ErrorAction SilentlyContinue){throw '请先正常退出已经打开的跑跑卡丁车。'}
    }
    $port=[int]$manifest.localBasePort
    $occupied=@(Get-NetTCPConnection -LocalPort ($port+1),($port+2),($port+3) -State Listen -ErrorAction SilentlyContinue)
    $udp=@(Get-NetUDPEndpoint -LocalPort $port,($port+1) -ErrorAction SilentlyContinue)
    if($occupied.Count -or $udp.Count){throw '本补丁的本地端口已占用，请先关闭另一个补丁启动窗口和它的游戏。'}
    $run=Join-Path $appRoot ('user/logs/'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $run -Force | Out-Null
    $profile=Join-Path $appRoot 'user/profiles';New-Item -ItemType Directory -Path $profile -Force|Out-Null
    $env:P5136_LOG_DIR=$run
    $serverExe=Join-Path $appRoot 'server/p5136.exe'
    Write-Host '正在启动本地单机服务……'
    $ownedServer=Start-Process -FilePath $serverExe -WindowStyle Hidden -ArgumentList @('server','--bind','127.0.0.1','--advertise','127.0.0.1','--configured-port',([string]$port),'--client-dir',('"'+$gameDir+'"'),'--profile-root',('"'+$profile+'"')) -WorkingDirectory $appRoot -PassThru -RedirectStandardOutput (Join-Path $run 'server.out.txt') -RedirectStandardError (Join-Path $run 'server.err.txt')
    $serverHandle=$ownedServer.Handle
    $ready=$false
    for($i=0;$i -lt 20;$i++){
        if($ownedServer.HasExited){throw '本地服务退出。请查看app/user/logs中的server.err.txt。'}
        $probe=Invoke-NativeFileLogged -Executable $serverExe -Arguments @('probe','--server','127.0.0.1','--configured-port',([string]$port),'--timeout-ms','600') -WorkingDirectory $appRoot -StdoutPath (Join-Path $run 'probe.out.txt') -StderrPath (Join-Path $run 'probe.err.txt')
        if($probe.ExitCode -eq 0){$ready=$true;break};Start-Sleep -Milliseconds 500
    }
    if(-not $ready){throw '本地服务未能就绪。'}
    if($ServerCheckOnly){
        $plan=Invoke-NativeFileLogged -Executable $serverExe -Arguments @('connect','--game-dir',('"'+$gameDir+'"'),'--username','NordPlayer','--server','127.0.0.1','--configured-port',([string]$port),'--runner','native','--dry-run') -WorkingDirectory $appRoot -StdoutPath (Join-Path $run 'dry-run.out.txt') -StderrPath (Join-Path $run 'dry-run.err.txt')
        if($plan.ExitCode -ne 0){throw ('连接准备检查失败：'+$plan.Stderr)}
        Write-Host 'PASS local server, probe and connector dry-run; no game launched';exit 0
    }
    Write-Host '正在启动纽北最终版（1.5倍），昵称NordPlayer。'
    $launch=Invoke-NativeFileLogged -Executable $serverExe -Arguments @('connect','--game-dir',('"'+$gameDir+'"'),'--username','NordPlayer','--server','127.0.0.1','--configured-port',([string]$port),'--runner','native') -WorkingDirectory $appRoot -StdoutPath (Join-Path $run 'launch.out.txt') -StderrPath (Join-Path $run 'launch.err.txt') -CompletionPattern 'Game launch accepted via native: PID [0-9]+,'
    if($launch.ExitCode -ne 0){throw ('游戏启动失败：'+$launch.Stderr)}
    $match=[regex]::Match($launch.Stdout,'Game launch accepted via native: PID ([0-9]+),')
    if(-not $match.Success){throw '没有找到本次游戏进程。'}
    $gameProcessId=[int]$match.Groups[1].Value;$launchedGame=Get-Process -Id $gameProcessId -ErrorAction Stop;$gameHandle=$launchedGame.Handle
    Write-Host '到大厅/我的房间后，先不要开始比赛。' -ForegroundColor Yellow
    Read-Host 'Alt+Tab回到这里，按一次回车启用地图范围补丁'|Out-Null
    $report=[NativeRange.Patcher]::ApplyGame($gameProcessId,$gameExe)
    $report|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $run 'range-patch.json') -Encoding UTF8
    if(-not $report.Installed -or -not $report.AllThreadsResumed -or -not $report.ProtectionRestored){throw ('地图范围补丁未启用：'+$report.Error)}
    Write-Host '准备完成！切回游戏，选择 빌리지 고가의 질주，正向，即可开始。' -ForegroundColor Green
    Write-Host '本窗口可最小化。正常退出游戏后，本次本地服务会自动关闭。'
    $launchedGame.WaitForExit()
} catch {
    $failed=$true;Write-Host $_.Exception.Message -ForegroundColor Red
    if($launchedGame -and -not $launchedGame.HasExited){Write-Host '请正常退出游戏。关闭后本地服务自动停止。';$launchedGame.WaitForExit()}
} finally {
    if($ownedServer -and -not $ownedServer.HasExited){$ownedServer.Kill();$ownedServer.WaitForExit(5000)|Out-Null}
}
if($failed){if(-not $VerifyOnly -and -not $ServerCheckOnly){Read-Host '按回车关闭'|Out-Null};exit 1}
