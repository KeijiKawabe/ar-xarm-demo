$root = Split-Path -Parent $PSScriptRoot
Set-Location (Join-Path $root "robot_server")
$env:PYTHONPATH = (Get-Location).Path
python .\run_server.py --config .\config\demo.json
