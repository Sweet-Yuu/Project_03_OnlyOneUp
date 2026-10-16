param([string]$UnityEditorPath = 'D:/Unity Editor/6000.3.15f1/Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$testProject = Join-Path $projectRoot 'Temp/NestCarryValidation/UnityProject'
New-Item -ItemType Directory -Force -Path "$testProject/Assets/Editor", "$testProject/Packages", "$testProject/ProjectSettings" | Out-Null
Copy-Item "$projectRoot/ProjectSettings/ProjectVersion.txt" "$testProject/ProjectSettings/ProjectVersion.txt"
Copy-Item "$projectRoot/ProjectSettings/TagManager.asset" "$testProject/ProjectSettings/TagManager.asset"
$ugui = Get-ChildItem "$projectRoot/Library/PackageCache" -Directory -Filter 'com.unity.ugui@*' | Select-Object -First 1
if ($null -eq $ugui) { throw 'Open the main Unity project once to restore its existing uGUI dependency.' }
@{ dependencies = @{ 'com.unity.modules.physics' = '1.0.0'; 'com.unity.modules.audio' = '1.0.0'; 'com.unity.modules.animation' = '1.0.0';
    'com.unity.ugui' = ('file:' + $ugui.FullName.Replace('\', '/')) } } |
    ConvertTo-Json -Depth 3 | Set-Content "$testProject/Packages/manifest.json"
foreach ($name in @('NestPositionSmoother', 'NestClientPresentation', 'NestEggFrame', 'NestEggFrameBuffer', 'NestEggPhase', 'NestEggBody',
    'INestCarryGeometry', 'NestCarryPose', 'NestCarryInput', 'NestCarryState', 'INestCarrySimulation',
    'NestCarrySimulation', 'NestCarryInputBuffer', 'NestCarryPrediction', 'NestReconciledPresentation', 'NestEggPresentation', 'NestEggContainment',
    'INestEggPhysicsBody', 'INestEggCarryPhysics', 'NestEggCarryPhysics', 'CooperativeJumpWindow', 'PlayerCharacterMotor', 'NestEggDamageVisual', 'EggDamageFeedback', 'OnlineCheckpoint',
    'NetworkRoundManager', 'IRoundSession', 'IRoundResetTarget', 'PlayerSpawnProbe', 'EggFallTrajectory',
    'PlayerAnimationPresenter', 'OnlinePlayerStateMachine', 'NestWinContact')) {
    $source = @(Get-ChildItem "$projectRoot/Assets/Scripts" -Recurse -Filter "$name.cs")
    if ($source.Count -ne 1) { throw "Expected one source for $name" }
    Copy-Item $source[0].FullName "$testProject/Assets/$name.cs"
}
Copy-Item "$projectRoot/Assets/Scripts/NestEggPhysics/Nest/Physics/NestImpactDetector.cs" "$testProject/Assets/NestImpactDetector.cs"
Copy-Item "$PSScriptRoot/NestImpactValidation.cs" "$testProject/Assets/Editor/NestImpactValidation.cs"
Copy-Item "$PSScriptRoot/WalkSpeedValidation.cs" "$testProject/Assets/Editor/WalkSpeedValidation.cs"
Copy-Item "$PSScriptRoot/NestWinValidation.cs" "$testProject/Assets/Editor/NestWinValidation.cs"
Copy-Item "$PSScriptRoot/NestCarryNetcodeDoubles.cs" "$testProject/Assets/NestCarryNetcodeDoubles.cs"
New-Item -ItemType Directory -Force -Path "$testProject/Assets/Resources" | Out-Null
Copy-Item "$projectRoot/Assets/Resources/EggTrajectoryLine.shader" "$testProject/Assets/Resources/EggTrajectoryLine.shader"
Copy-Item "$PSScriptRoot/RoundManagerNetcodeDoubles.cs" "$testProject/Assets/RoundManagerNetcodeDoubles.cs"
Copy-Item "$PSScriptRoot/NetworkRoundManagerValidation.cs" "$testProject/Assets/Editor/NetworkRoundManagerValidation.cs"
Copy-Item "$PSScriptRoot/NestCarryValidation.cs" "$testProject/Assets/Editor/NestCarryValidation.cs"
Copy-Item "$PSScriptRoot/NestPredictionValidation.cs" "$testProject/Assets/Editor/NestPredictionValidation.cs"
Copy-Item "$PSScriptRoot/NestEggPresentationValidation.cs" "$testProject/Assets/Editor/NestEggPresentationValidation.cs"
Copy-Item "$PSScriptRoot/NestEggPhysicsValidation.cs" "$testProject/Assets/Editor/NestEggPhysicsValidation.cs"
Copy-Item "$PSScriptRoot/PlayerJumpValidation.cs" "$testProject/Assets/Editor/PlayerJumpValidation.cs"
Copy-Item "$PSScriptRoot/EggDamageFeedbackValidation.cs" "$testProject/Assets/Editor/EggDamageFeedbackValidation.cs"
foreach ($model in @('Pink_egg_Unity', 'Broken_egg2')) {
    Copy-Item "$projectRoot/Assets/Quang/Model3D/$model.fbx" "$testProject/Assets/$model.fbx"
    Copy-Item "$projectRoot/Assets/Quang/Model3D/$model.fbx.meta" "$testProject/Assets/$model.fbx.meta"
}
# Keep the actual physics hierarchy; omit network scripts and the nested visual asset
# so the isolated physics project does not require NGO or the art asset's dependencies.
$prefabText = Get-Content "$projectRoot/Assets/Prefabs/Nest.prefab" -Raw
$blocks = [regex]::Split($prefabText, '(?m)(?=^--- !u!)')
$excludedIds = @()
$keptBlocks = foreach ($block in $blocks) {
    if ($block -match '^--- !u!(?:114|1001) &(\d+)' -or $block -match '^--- !u!\d+ &(\d+) stripped') {
        $excludedIds += $Matches[1]
    } else { $block }
}
$physicsPrefab = $keptBlocks -join ''
foreach ($id in $excludedIds) {
    $physicsPrefab = [regex]::Replace($physicsPrefab, '(?m)^  - (?:component: )?\{fileID: ' + $id + '\}\r?\n', '')
}
[IO.File]::WriteAllText("$testProject/Assets/Nest.prefab", $physicsPrefab)
$unityArguments = @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $testProject + '"'),
    '-executeMethod', 'NestCarryValidation.Run', '-logFile', ('"' + "$testProject/editor.log" + '"'))
$process = Start-Process -FilePath "$UnityEditorPath/Unity.exe" -ArgumentList $unityArguments -WindowStyle Hidden -PassThru -Wait
if ($process.ExitCode -ne 0) { throw "Nest tests failed. Inspect $testProject/editor.log" }
$result = Get-Content "$testProject/result.txt" -Raw
if ($result -notmatch '^PASS:') { throw $result }
$result
