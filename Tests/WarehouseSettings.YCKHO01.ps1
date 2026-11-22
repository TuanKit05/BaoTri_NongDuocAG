param([string]$ProjectRoot='D:\NongDuoc_BaoTri', [string]$AssemblyPath='')
$ErrorActionPreference='Stop'
function Assert($condition,$message) { if (!$condition) { throw $message } }
if (!$AssemblyPath) { $AssemblyPath=Join-Path $ProjectRoot 'bin\Debug\CuahangNongduoc.exe' }
[void][Reflection.Assembly]::LoadFrom($AssemblyPath)
[Runtime.CompilerServices.RuntimeHelpers]::RunClassConstructor([CuahangNongduoc.DataService].TypeHandle)
$folder=Join-Path $ProjectRoot ('obj\WarehouseSettings-'+[Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $folder)
$db=Join-Path $folder 'cuahang.mdb'
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'Database\cuahang.dll') -Destination $db
[CuahangNongduoc.DataService]::m_ConnectString="Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$db;OLE DB Services=-2"
$repo=New-Object CuahangNongduoc.AccountRepository
$repo.EnsureSchema()
[CuahangNongduoc.AppSession]::CurrentUser=$repo.FindByUsername('admin')
$manual=[CuahangNongduoc.WarehouseSettings]::SpecificLot
$fifo=[CuahangNongduoc.WarehouseSettings]::FirstInFirstOut
Assert ([CuahangNongduoc.WarehouseSettings]::Load() -eq $manual) 'Migration must preserve existing manual-lot behavior'
[CuahangNongduoc.WarehouseSettings]::Save($fifo)
$repo.EnsureSchema()
Assert ([CuahangNongduoc.WarehouseSettings]::Load() -eq $fifo) 'Configuration lost after schema initialization'
$connection=New-Object System.Data.OleDb.OleDbConnection([CuahangNongduoc.DataService]::m_ConnectString)
try {
 $connection.Open();$cmd=$connection.CreateCommand()
 $cmd.CommandText="SELECT COUNT(*) FROM CAU_HINH_KHO WHERE PHAM_VI='CUA_HANG' AND PHUONG_PHAP_XUAT='FIFO' AND NGUOI_DOI IS NOT NULL AND NGAY_HIEU_LUC IS NOT NULL"
 Assert ($cmd.ExecuteScalar() -eq 1) 'Shared database does not contain saved mode'
 $cmd.CommandText="SELECT COUNT(*) FROM NHAT_KY WHERE THAO_TAC='DOI_CAU_HINH_KHO'"
 $before=$cmd.ExecuteScalar()
 [CuahangNongduoc.WarehouseSettings]::Save($fifo)
 Assert ($cmd.ExecuteScalar() -eq $before) 'Unchanged mode created an audit entry'
 $failed=$false;try { [CuahangNongduoc.WarehouseSettings]::Save('INVALID') } catch { $failed=$true }
 Assert $failed 'Invalid method accepted'
 Assert ([CuahangNongduoc.WarehouseSettings]::Load() -eq $fifo) 'Invalid input changed mode'
 $repo.CreateAccount('yc_kho_01_seller','Warehouse settings test','TestKho01!234','BANHANG')
 [CuahangNongduoc.AppSession]::CurrentUser=$repo.FindByUsername('yc_kho_01_seller')
 Assert ([CuahangNongduoc.WarehouseSettings]::Load() -eq $fifo) 'Seller cannot read shared mode'
 $failed=$false;try { [CuahangNongduoc.WarehouseSettings]::Save($manual) } catch { $failed=$true }
 Assert $failed 'Seller changed store configuration'
 [CuahangNongduoc.AppSession]::CurrentUser=$null
 $failed=$false;try { [CuahangNongduoc.WarehouseSettings]::Save($manual) } catch { $failed=$true }
 Assert $failed 'Unauthenticated settings change allowed'
 [CuahangNongduoc.AppSession]::CurrentUser=$repo.FindByUsername('admin')
 [CuahangNongduoc.WarehouseSettings]::Save($manual)
 Assert ([CuahangNongduoc.WarehouseSettings]::Load() -eq $manual) 'Switching back failed'
 $cmd.CommandText="SELECT COUNT(*) FROM CAU_HINH_KHO WHERE PHAM_VI='CUA_HANG'"
 Assert ($cmd.ExecuteScalar() -eq 1) 'Duplicate store configuration'
 $form=New-Object CuahangNongduoc.frmCauHinhKho
 try { Assert ($form.Controls.Count -eq 10) 'Settings form failed to initialize' } finally { $form.Dispose() }
} finally { $connection.Dispose() }
Write-Output 'PASS: store-wide mode persistence, repeat migration, validation, access denial, audit, and settings form.'
