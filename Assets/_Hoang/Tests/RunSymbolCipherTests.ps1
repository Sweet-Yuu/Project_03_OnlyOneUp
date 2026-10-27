param([string]$UnityEditorPath = 'C:/Program Files/Unity/Hub/Editor/6000.3.15f1/Editor')
$ErrorActionPreference = 'Stop'
$hoangRoot = Split-Path $PSScriptRoot -Parent
$testOutput = Join-Path $PSScriptRoot '.artifacts~'
New-Item -ItemType Directory -Force -Path $testOutput | Out-Null
$editorData = Join-Path $UnityEditorPath 'Data'
$testExe = Join-Path $testOutput 'SymbolCipherLockTests.exe'
$sources = @((Join-Path $hoangRoot 'Scripts/Puzzles/SymbolCipherLock.cs'), (Join-Path $PSScriptRoot 'Editor/SymbolCipherLockTests.cs'))
$arguments = @('/nologo', '/target:exe', '/nostdlib+', ('/out:"' + $testExe + '"'),
    ('/reference:"' + (Join-Path $editorData 'NetStandard/ref/2.1.0/netstandard.dll') + '"'))
$arguments += $sources | ForEach-Object { '"' + $_ + '"' }
$responseFile = Join-Path $testOutput 'tests.rsp'
Set-Content -LiteralPath $responseFile -Value $arguments
& (Join-Path $editorData 'NetCoreRuntime/dotnet.exe') (Join-Path $editorData 'DotNetSdkRoslyn/csc.dll') ('@' + $responseFile)
if ($LASTEXITCODE -ne 0) { throw 'Symbol cipher test compilation failed.' }
& (Join-Path $editorData 'MonoBleedingEdge/bin/mono.exe') $testExe
if ($LASTEXITCODE -ne 0) { throw 'Symbol cipher tests failed.' }
