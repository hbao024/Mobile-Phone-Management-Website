$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$project = Join-Path $repo 'DoAnLTWeb/QuanLiDienThoaiDD/QuanLiDienThoaiDD'
$bin = Join-Path $project 'bin'
$output = Join-Path $project 'obj/Cat04Checks'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (!(Test-Path (Join-Path $bin 'QuanLiDienThoaiDD.dll'))) { throw 'Build the solution with MSBuild before running these checks.' }
New-Item -ItemType Directory -Path $output -Force | Out-Null
Copy-Item -Path (Join-Path $bin '*.dll') -Destination $output
$refs = @('QuanLiDienThoaiDD.dll', 'EntityFramework.dll', 'System.Web.Mvc.dll', 'System.Web.WebPages.dll', 'System.Web.Razor.dll', 'System.Web.WebPages.Razor.dll') | ForEach-Object { '/r:' + (Join-Path $bin $_) }
& $compiler /nologo /target:exe "/out:$output/ProductChecks.exe" /r:System.Web.dll /r:System.ComponentModel.DataAnnotations.dll /r:System.Data.dll @refs (Join-Path $PSScriptRoot 'ProductChecks.cs')
if ($LASTEXITCODE -ne 0) { throw 'Product check compilation failed.' }
& (Join-Path $output 'ProductChecks.exe') $project
if ($LASTEXITCODE -ne 0) { throw 'Product checks failed.' }
