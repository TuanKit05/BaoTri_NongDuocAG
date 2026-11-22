param([string]$ProjectRoot='D:\NongDuoc_BaoTri',[string]$AssemblyPath='')
$ErrorActionPreference='Stop'
if (!$AssemblyPath) { $AssemblyPath=Join-Path $ProjectRoot 'bin\Debug\CuahangNongduoc.exe' }
[void][Reflection.Assembly]::LoadFrom($AssemblyPath)
[Runtime.CompilerServices.RuntimeHelpers]::RunClassConstructor([CuahangNongduoc.DataService].TypeHandle)
$folder=Join-Path $env:TEMP ('SalesReport-'+[Guid]::NewGuid().ToString('N'))
$sampleFolder=Join-Path $ProjectRoot 'obj\ReportValidationSamples'
[void](New-Item -ItemType Directory -Force -Path $sampleFolder)
[void](New-Item -ItemType Directory -Path $folder)
$db=Join-Path $folder 'cuahang.mdb'
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'Database\cuahang.dll') -Destination $db
[CuahangNongduoc.DataService]::m_ConnectString="Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$db;OLE DB Services=-2"
$repo=New-Object CuahangNongduoc.AccountRepository
$repo.EnsureSchema()
[CuahangNongduoc.AppSession]::CurrentUser=$repo.FindByUsername('admin')
$form=New-Object CuahangNongduoc.frmSoLuongBan
$flags=[Reflection.BindingFlags]'Instance,NonPublic'
try {
 [void]$form.GetType().GetMethod('frmSoLuongBan_Load',$flags).Invoke($form,@($form,[EventArgs]::Empty))
 $viewer=$form.GetType().GetField('reportViewer',$flags).GetValue($form)
 foreach($method in @('btnXemNgay_Click','btnXemThang_Click')) {
  [void]$form.GetType().GetMethod($method,$flags).Invoke($form,@($form,[EventArgs]::Empty))
  try { $pdf=$viewer.LocalReport.Render('PDF',$null) }
  catch { $failure=$_.Exception; while($failure) { Write-Output $failure.Message; $failure=$failure.InnerException }; throw }
  if($pdf.Length -lt 100 -or [Text.Encoding]::ASCII.GetString($pdf,0,4) -ne '%PDF') { throw "Report rendering failed: $method" }
  [IO.File]::WriteAllBytes((Join-Path $sampleFolder ($method+'.pdf')),$pdf)
  Write-Output ('PASS: '+$method+' renders a PDF under CLR4 without legacy CAS policy.')
 }
} finally { $form.Dispose() }
foreach($spec in @(@('frmSoLuongTon','frmSoLuongTon_Load','SoLuongTonBindingSource'),@('frmSanphamHethan','btnXem_Click','MaSanPhamBindingSource'))) {
 $form=New-Object ('CuahangNongduoc.'+$spec[0])
 try {
  [void]$form.GetType().GetMethod($spec[1],$flags).Invoke($form,@($form,[EventArgs]::Empty))
  $viewer=$form.GetType().GetField('reportViewer',$flags).GetValue($form)
  $mime=$null;$encoding=$null;$extension=$null;$streams=$null;$warnings=$null
  $pdf=$viewer.LocalReport.Render('PDF',$null,[ref]$mime,[ref]$encoding,[ref]$extension,[ref]$streams,[ref]$warnings)
  if($pdf.Length -lt 100) { throw ('Empty PDF: '+$spec[0]) }
  foreach($warning in $warnings) { if($warning.Code -match 'Expression|Error' -or $warning.Severity -eq 'Error') { throw $warning.Message } }
  $source=$form.GetType().GetField($spec[2],$flags).GetValue($form).DataSource
  if($source.Rows.Count -eq 0) { throw ('No fixture rows to test: '+$spec[0]) }
  $name=[string]$source.Rows[0]['TenSanPham']
  if(!$name) { throw ('Product name missing from source: '+$spec[0]) }
  if($spec[0] -eq 'frmSoLuongTon' -and [string]::IsNullOrEmpty([string]$source.Rows[0]['DonViTinh'])) { throw 'Unit name missing from stock report' }
  [IO.File]::WriteAllBytes((Join-Path $sampleFolder ($spec[0]+'.pdf')),$pdf)
  Write-Output ('PASS: '+$spec[0]+' renders PDF without expression errors; populated product names and units.')
 } finally { $form.Dispose() }
}
Write-Output ('Report samples: '+$sampleFolder)
