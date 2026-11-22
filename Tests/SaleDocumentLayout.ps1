param([string]$ProjectRoot='D:\NongDuoc_BaoTri', [string]$AssemblyPath='')
$ErrorActionPreference='Stop'
if (!$AssemblyPath) { $AssemblyPath=Join-Path $ProjectRoot 'bin\Debug\CuahangNongduoc.exe' }
[void][Reflection.Assembly]::LoadFrom($AssemblyPath)
[Runtime.CompilerServices.RuntimeHelpers]::RunClassConstructor([CuahangNongduoc.DataService].TypeHandle)
$testFolder=Join-Path $env:TEMP ('SaleLayout-'+[Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $testFolder)
$testDb=Join-Path $testFolder 'cuahang.mdb'
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'Database\cuahang.dll') -Destination $testDb
[CuahangNongduoc.DataService]::m_ConnectString="Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$testDb;OLE DB Services=-2"
$repo=New-Object CuahangNongduoc.AccountRepository
$repo.EnsureSchema()
[CuahangNongduoc.AppSession]::CurrentUser=$repo.FindByUsername('admin')
function Assert($condition,$message) { if (!$condition) { throw $message } }
foreach ($name in @('frmBanSi','frmBanLe')) {
 $form=New-Object ('CuahangNongduoc.'+$name)
 try {
  $form.Show(); [Windows.Forms.Application]::DoEvents(); [Windows.Forms.Application]::DoEvents()
  $viewport=$form.Controls['saleViewport']; $content=$viewport.Controls['saleContent']
  $header=$content.Controls['panel1']; $lines=$content.Controls['groupBox1']; $fees=$content.Controls['grpPhi']
  Assert ($null -ne $content) ($name+': missing scrollable layout')
  Assert ($form.WindowState -eq 'Normal') ($name+': unexpectedly maximized')
  Assert ($form.StartPosition -eq 'CenterScreen') ($name+': not centered')
  foreach($size in @((New-Object Drawing.Size(700,535)),(New-Object Drawing.Size(1100,760)),(New-Object Drawing.Size(1900,980)))) {
   $form.ClientSize=$size; $form.PerformLayout(); $viewport.PerformLayout(); $content.PerformLayout()
   [Windows.Forms.Application]::DoEvents()
   Assert ($lines.Top -ge $header.Bottom) ($name+': grid overlaps header')
   Assert ($lines.Bottom -le $fees.Top) ($name+': grid overlaps fees')
   Assert ($lines.Height -ge 170) ($name+': product list collapsed')
   Assert ($lines.Width -eq $content.ClientSize.Width) ($name+': grid does not fill width')
   Assert ($viewport.AutoScroll) ($name+': small-screen scrolling disabled')
   $product=$header.Controls['groupBox2']
   foreach($child in $product.Controls) {
    Assert ($child.Right -le $product.ClientSize.Width) ($name+': clipped product control '+$child.Name)
    Assert ($child.Top -ge 0 -and $child.Bottom -le $product.ClientSize.Height) ($name+': clipped product height '+$child.Name)
   }
   foreach($child in $fees.Controls) { Assert ($child.Right -le $fees.ClientSize.Width -and $child.Bottom -le $fees.ClientSize.Height) ($name+': clipped fee control '+$child.Name) }
  }
  Write-Output ('PASS: '+$name+' normal/centered, small-screen scrolling, full-width grid and visible product/fee controls at three window sizes.')
 } finally { $form.Dispose() }
}
