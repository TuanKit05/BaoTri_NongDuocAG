param([string]$ProjectRoot='D:\NongDuoc_BaoTri',[string]$AssemblyPath='')
$ErrorActionPreference='Stop'
function Assert($condition,$message) { if (!$condition) { throw $message } }
if (!$AssemblyPath) { $AssemblyPath=Join-Path $ProjectRoot 'bin\Debug\CuahangNongduoc.exe' }
[void][Reflection.Assembly]::LoadFrom($AssemblyPath)
[Runtime.CompilerServices.RuntimeHelpers]::RunClassConstructor([CuahangNongduoc.DataService].TypeHandle)
$folder=Join-Path $env:TEMP ('AuditLog-'+[Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $folder)
$db=Join-Path $folder 'cuahang.mdb'
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'Database\cuahang.dll') -Destination $db
[CuahangNongduoc.DataService]::m_ConnectString="Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$db;OLE DB Services=-2"
$repo=New-Object CuahangNongduoc.AccountRepository
$repo.EnsureSchema()
$admin=$repo.FindByUsername('admin')
[CuahangNongduoc.AppSession]::CurrentUser=$admin
$before=$repo.GetAuditLog().Rows.Count
$repo.CreateAccount('audit_sales','Audit integration','TestPassword123','BANHANG')
# A fresh repository/connection must see the committed event, including the actor username.
$reader=New-Object CuahangNongduoc.AccountRepository
$rows=$reader.GetAuditLog()
Assert ($rows.Rows.Count -eq $before+1) 'Audit was not persisted to Access'
Assert ($rows.Columns.Count -eq 4) 'Wrong audit projection'
Assert ($rows.Rows[0]['ID_TAI_KHOAN'] -eq $admin.Id) 'Wrong actor ID'
Assert ($rows.Rows[0]['TEN_TAI_KHOAN'] -eq $admin.Username) 'Wrong actor username'
Assert ($rows.Rows[0]['THAO_TAC'] -eq 'TAO_TAI_KHOAN') 'Newest event missing'
Assert ($rows.Rows[0]['THOI_GIAN'] -is [DateTime]) 'Timestamp lost its date type'
$form=New-Object CuahangNongduoc.frmNhatKyThaoTac
try {
 $form.Show();[Windows.Forms.Application]::DoEvents()
 $grid=$form.Controls['gridNhatKy']
 Assert ($form.StartPosition -eq 'CenterScreen') 'Audit form is not centered'
 Assert ($grid.ReadOnly -and !$grid.AutoGenerateColumns) 'Audit form permits edits or extra columns'
 Assert (($grid.Columns | ForEach-Object DataPropertyName) -join ',' -eq 'ID_TAI_KHOAN,TEN_TAI_KHOAN,THOI_GIAN,THAO_TAC') 'Wrong column order'
 Assert ($grid.Rows.Count -eq $rows.Rows.Count) 'Persisted events not displayed'
} finally { $form.Dispose() }
[CuahangNongduoc.AppSession]::CurrentUser=$repo.FindByUsername('audit_sales')
$denied=$false;try { $reader.GetAuditLog() } catch { $denied=$true }
Assert $denied 'Unauthorized account can read audit events'
Write-Output 'PASS: persisted Access audit event, actor ID/username, timestamp, newest-first order, centered read-only form with four columns and permission guard.'
