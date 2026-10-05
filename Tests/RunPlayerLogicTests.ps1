param([string]$UnityEditorPath = 'D:/Unity Editor/6000.3.15f1/Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$editorData = Join-Path $UnityEditorPath 'Data'
$testOutput = Join-Path $projectRoot 'Temp/PlayerLogicTests'
New-Item -ItemType Directory -Force -Path $testOutput | Out-Null
$sources = @('Assets/Scripts/CarryDistanceConstraint.cs', 'Assets/Scripts/PlayerPreparation.cs',
    'Assets/Scripts/OnlinePlayerStateMachine.cs', 'Tests/PlayerLogicTests.cs')
$arguments = @('/nologo', '/target:exe', '/nostdlib+', ('/out:"' + (Join-Path $testOutput 'PlayerLogicTests.exe') + '"'),
    ('/reference:"' + (Join-Path $editorData 'NetStandard/ref/2.1.0/netstandard.dll') + '"'),
    ('/reference:"' + (Join-Path $editorData 'Managed/UnityEngine/UnityEngine.CoreModule.dll') + '"'))
$arguments += $sources | ForEach-Object { '"' + (Join-Path $projectRoot $_) + '"' }
$responseFile = Join-Path $testOutput 'tests.rsp'
Set-Content -LiteralPath $responseFile -Value $arguments
& (Join-Path $editorData 'NetCoreRuntime/dotnet.exe') (Join-Path $editorData 'DotNetSdkRoslyn/csc.dll') ('@' + $responseFile)
if ($LASTEXITCODE -ne 0) { throw 'Player logic test compilation failed.' }
$oldMonoPath = $env:MONO_PATH
try {
    $env:MONO_PATH = Join-Path $editorData 'Managed/UnityEngine'
    & (Join-Path $editorData 'MonoBleedingEdge/bin/mono.exe') (Join-Path $testOutput 'PlayerLogicTests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Player logic tests failed.' }
} finally {
    $env:MONO_PATH = $oldMonoPath
}
