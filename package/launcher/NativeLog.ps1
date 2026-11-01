function Read-SharedNativeLog {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return '' }
    $stream = [IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
    $reader = New-Object IO.StreamReader $stream
    try { return $reader.ReadToEnd() } finally { $reader.Dispose() }
}

function Invoke-NativeFileLogged {
    param(
        [Parameter(Mandatory=$true)][string]$Executable,
        [Parameter(Mandatory=$true)][string[]]$Arguments,
        [Parameter(Mandatory=$true)][string]$WorkingDirectory,
        [Parameter(Mandatory=$true)][string]$StdoutPath,
        [Parameter(Mandatory=$true)][string]$StderrPath,
        [int]$TimeoutMilliseconds = 30000,
        [string]$CompletionPattern
    )
    # Avoid PowerShell 5's NativeCommandError promotion of stderr INFO logs.
    # WaitForExit(timeout) waits only for this connector PID, not its game child
    # or a child-inherited output handle. Keep both output streams as files.
    $process = Start-Process -FilePath $Executable -ArgumentList $Arguments -WorkingDirectory $WorkingDirectory -WindowStyle Hidden -PassThru -RedirectStandardOutput $StdoutPath -RedirectStandardError $StderrPath
    # Keep the native process handle open before waiting; Windows PowerShell
    # can otherwise lose ExitCode for a short-lived Start-Process child.
    $nativeHandle = $process.Handle
    $exited = $process.WaitForExit($TimeoutMilliseconds)
    if (-not $exited) { throw ('Connector PID ' + $process.Id + ' did not finish within the launch timeout. No process was terminated.') }
    $exitCode = $process.ExitCode
    $deadline = (Get-Date).AddSeconds(2)
    $stdout = ''
    do {
        $stdout = Read-SharedNativeLog -Path $StdoutPath
        if (-not $CompletionPattern -or $stdout -match $CompletionPattern -or $exitCode -ne 0) { break }
        Start-Sleep -Milliseconds 100
    } while ((Get-Date) -lt $deadline)
    $stderr = Read-SharedNativeLog -Path $StderrPath
    return [pscustomobject]@{ ProcessId=$process.Id; ExitCode=$exitCode; Stdout=$stdout; Stderr=$stderr; StdoutPath=$StdoutPath; StderrPath=$StderrPath }
}
