param([string]$UnityEditorPath = 'C:/Program Files/Unity/Hub/Editor/6000.3.15f1/Editor')
$ErrorActionPreference = 'Stop'
$lightsRoot = Split-Path $PSScriptRoot -Parent
$lightsOutput = Join-Path $PSScriptRoot '.artifacts~'
New-Item -ItemType Directory -Force -Path $lightsOutput | Out-Null
$lightsEditorData = Join-Path $UnityEditorPath 'Data'
$lightsExe = Join-Path $lightsOutput 'LinkedLightsBoardTests.exe'
$lightsSources = @((Join-Path $lightsRoot 'Scripts/Puzzles/LinkedLightsBoard.cs'), (Join-Path $PSScriptRoot 'Editor/LinkedLightsBoardTests.cs'))
$lightsArguments = @('/nologo', '/target:exe', '/nostdlib+', ('/out:"' + $lightsExe + '"'),
    ('/reference:"' + (Join-Path $lightsEditorData 'NetStandard/ref/2.1.0/netstandard.dll') + '"'))
$lightsArguments += $lightsSources | ForEach-Object { '"' + $_ + '"' }
$lightsResponse = Join-Path $lightsOutput 'linked-lights.rsp'
Set-Content -LiteralPath $lightsResponse -Value $lightsArguments
& (Join-Path $lightsEditorData 'NetCoreRuntime/dotnet.exe') (Join-Path $lightsEditorData 'DotNetSdkRoslyn/csc.dll') ('@' + $lightsResponse)
if ($LASTEXITCODE -ne 0) { throw 'Linked lights test compilation failed.' }
& (Join-Path $lightsEditorData 'MonoBleedingEdge/bin/mono.exe') $lightsExe
if ($LASTEXITCODE -ne 0) { throw 'Linked lights tests failed.' }
