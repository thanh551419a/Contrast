param(
    [string]$UnityExe = "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe",
    [string]$ProjectPath = "y:\Contrast\My project",
    [string]$LogFile = "y:\Contrast\My project\unity_build_android.log"
)

Write-Host "================================================================="
Write-Host " CONTRAST - BUILD ANDROID GAME (Game.apk)"
Write-Host "================================================================="

$buildArgs = "-batchmode -quit -projectPath `"$ProjectPath`" -executeMethod Contrast.Build.ContrastBuildPipeline.BuildGameAndroid -logFile `"$LogFile`""
$proc = Start-Process -FilePath $UnityExe -ArgumentList $buildArgs -Wait -PassThru

Write-Host "Exit code: $($proc.ExitCode)"
exit $proc.ExitCode
