param(
    [string]$UnityExe = "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe",
    [string]$ProjectPath = "y:\Contrast\My project",
    [string]$LogFile = "y:\Contrast\My project\unity_build_all.log"
)

Write-Host "================================================================="
Write-Host " CONTRAST - FULL RELEASE BUILD PIPELINE"
Write-Host "================================================================="
Write-Host "Unity Executable : $UnityExe"
Write-Host "Project Path     : $ProjectPath"
Write-Host "Log File         : $LogFile"

$buildArgs = "-batchmode -quit -projectPath `"$ProjectPath`" -executeMethod Contrast.Build.ContrastBuildPipeline.BuildAll -logFile `"$LogFile`""
$proc = Start-Process -FilePath $UnityExe -ArgumentList $buildArgs -Wait -PassThru

Write-Host "Build process exited with code $($proc.ExitCode)"
if ($proc.ExitCode -eq 0) {
    Write-Host "All build targets completed successfully!" -ForegroundColor Green
} else {
    Write-Host "Build encountered errors. Check $LogFile for diagnostics." -ForegroundColor Red
}
exit $proc.ExitCode
