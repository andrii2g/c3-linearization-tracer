$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
dotnet restore c3-linearization-tracer.slnx
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build c3-linearization-tracer.slnx -c Release --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet test c3-linearization-tracer.slnx -c Release --no-build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$cli = 'src/C3Tracer.Cli/bin/Release/net10.0/c3-tracer.dll'
dotnet $cli trace fixtures/diamond.json --json artifacts/smoke/diamond.json --svg artifacts/smoke/diamond.svg --force
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet $cli trace fixtures/inconsistent-order.json --json artifacts/smoke/conflict.json --svg artifacts/smoke/conflict.svg --force
if ($LASTEXITCODE -ne 3) { throw "Expected exit 3; got $LASTEXITCODE" }
exit 0
