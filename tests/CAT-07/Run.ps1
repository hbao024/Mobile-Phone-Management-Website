$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$project = Join-Path $repo 'DoAnLTWeb/QuanLiDienThoaiDD/QuanLiDienThoaiDD'
$bin = Join-Path $project 'bin'
$output = Join-Path $project 'obj/Cat07Checks'
New-Item -ItemType Directory -Path $output -Force | Out-Null
Copy-Item (Join-Path $bin '*.dll') $output
$refs = @('QuanLiDienThoaiDD.dll', 'EntityFramework.dll', 'System.Web.Mvc.dll', 'System.Web.WebPages.dll', 'System.Web.Razor.dll', 'System.Web.WebPages.Razor.dll') | ForEach-Object { '/r:' + (Join-Path $bin $_) }
& (Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe') /nologo /target:exe /main:InventoryChecks "/out:$output/InventoryChecks.exe" /r:System.Web.dll /r:System.ComponentModel.DataAnnotations.dll /r:System.Data.dll @refs (Join-Path $PSScriptRoot 'InventoryChecks.cs') (Join-Path $repo 'tests/CAT-04/ProductChecks.cs')
if ($LASTEXITCODE -ne 0) { throw 'Inventory check compilation failed.' }
# Deliberately non-runtime connection: ToString generates SQL without opening this connection.
@'
<configuration>
  <configSections><section name="entityFramework" type="System.Data.Entity.Internal.ConfigFile.EntityFrameworkSection, EntityFramework" /></configSections>
  <connectionStrings><add name="QLHANGHOA_PHUKIEN_WEBEntities" providerName="System.Data.EntityClient" connectionString="metadata=res://*/Models.Model1.csdl|res://*/Models.Model1.ssdl|res://*/Models.Model1.msl;provider=System.Data.SqlClient;provider connection string=&quot;Data Source=invalid.invalid;Initial Catalog=CAT07_NO_EXECUTION;Integrated Security=True;Connect Timeout=1&quot;" /></connectionStrings>
  <entityFramework><providers><provider invariantName="System.Data.SqlClient" type="System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer" /></providers></entityFramework>
</configuration>
'@ | Set-Content -Encoding UTF8 (Join-Path $output 'InventoryChecks.exe.config')
& (Join-Path $output 'InventoryChecks.exe') $project
if ($LASTEXITCODE -ne 0) { throw 'Inventory checks failed.' }
