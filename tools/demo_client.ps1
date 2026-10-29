$root = Split-Path -Parent $PSScriptRoot
Set-Location (Join-Path $root "robot_server")
$env:PYTHONPATH = (Get-Location).Path
python (Join-Path $root "tools\demo_client.py")
