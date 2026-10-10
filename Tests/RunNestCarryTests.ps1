param([string]$UnityEditorPath = 'D:/Unity Editor/6000.3.15f1/Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$testProject = Join-Path $projectRoot 'Temp/NestCarryValidation/UnityProject'
New-Item -ItemType Directory -Force -Path "$testProject/Assets/Editor", "$testProject/Packages", "$testProject/ProjectSettings" | Out-Null
Copy-Item "$projectRoot/ProjectSettings/ProjectVersion.txt" "$testProject/ProjectSettings/ProjectVersion.txt"
Set-Content "$testProject/Packages/manifest.json" '{"dependencies":{"com.unity.modules.physics":"1.0.0","com.unity.modules.audio":"1.0.0"}}'
foreach ($name in @('NestPositionSmoother', 'NestClientPresentation', 'NestEggFrame', 'NestEggFrameBuffer', 'NestEggPhase', 'NestEggBody',
    'INestCarryGeometry', 'NestCarryPose', 'NestCarryInput', 'NestCarryState', 'INestCarrySimulation',
    'NestCarrySimulation', 'NestCarryInputBuffer', 'NestCarryPrediction', 'NestReconciledPresentation', 'NestEggPresentation', 'NestEggContainment')) {
    Copy-Item "$projectRoot/Assets/Scripts/$name.cs" "$testProject/Assets/$name.cs"
}
Copy-Item "$PSScriptRoot/NestCarryNetcodeDoubles.cs" "$testProject/Assets/NestCarryNetcodeDoubles.cs"
Copy-Item "$PSScriptRoot/NestCarryValidation.cs" "$testProject/Assets/Editor/NestCarryValidation.cs"
Copy-Item "$PSScriptRoot/NestPredictionValidation.cs" "$testProject/Assets/Editor/NestPredictionValidation.cs"
Copy-Item "$PSScriptRoot/NestEggPresentationValidation.cs" "$testProject/Assets/Editor/NestEggPresentationValidation.cs"
$unityArguments = @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $testProject + '"'),
    '-executeMethod', 'NestCarryValidation.Run', '-logFile', ('"' + "$testProject/editor.log" + '"'))
$process = Start-Process -FilePath "$UnityEditorPath/Unity.exe" -ArgumentList $unityArguments -WindowStyle Hidden -PassThru -Wait
if ($process.ExitCode -ne 0) { throw "Nest tests failed. Inspect $testProject/editor.log" }
$result = Get-Content "$testProject/result.txt" -Raw
if ($result -notmatch '^PASS:') { throw $result }
$result
