$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$project = Join-Path $repo 'DoAnLTWeb/QuanLiDienThoaiDD/QuanLiDienThoaiDD'
$bin = Join-Path $project 'bin'
$output = Join-Path $project 'obj/Cat05Checks'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (!(Test-Path (Join-Path $bin 'QuanLiDienThoaiDD.dll'))) { throw 'Build the solution first.' }
New-Item -ItemType Directory -Path $output -Force | Out-Null
Copy-Item -Path (Join-Path $bin '*.dll') -Destination $output
$refs = @('QuanLiDienThoaiDD.dll', 'EntityFramework.dll', 'System.Web.Mvc.dll') | ForEach-Object { '/r:' + (Join-Path $bin $_) }
& $compiler /nologo /target:exe "/out:$output/ImageChecks.exe" /r:System.Web.dll /r:System.Drawing.dll /r:System.ComponentModel.DataAnnotations.dll /r:System.Data.dll @refs (Join-Path $PSScriptRoot 'ImageChecks.cs')
if ($LASTEXITCODE -ne 0) { throw 'Image check compilation failed.' }
& (Join-Path $output 'ImageChecks.exe') $output
if ($LASTEXITCODE -ne 0) { throw 'Image checks failed.' }
& node (Join-Path $PSScriptRoot 'PreviewChecks.js') $project
if ($LASTEXITCODE -ne 0) { throw 'Preview script checks failed.' }
