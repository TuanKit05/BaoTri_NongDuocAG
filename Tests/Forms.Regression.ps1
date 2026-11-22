param([string]$ProjectRoot='D:\NongDuoc_BaoTri', [string]$AssemblyPath='')
$ErrorActionPreference='Stop'
function Assert($condition,$message) { if (!$condition) { throw $message } }
if (!$AssemblyPath) { $AssemblyPath=Join-Path $ProjectRoot 'bin\Debug\CuahangNongduoc.exe' }
[void][Reflection.Assembly]::LoadFrom($AssemblyPath)
Add-Type -AssemblyName System.Windows.Forms
[Runtime.CompilerServices.RuntimeHelpers]::RunClassConstructor([CuahangNongduoc.DataService].TypeHandle)
[System.Windows.Forms.Application]::SetUnhandledExceptionMode([System.Windows.Forms.UnhandledExceptionMode]::ThrowException)
$folder=Join-Path (Join-Path $ProjectRoot 'obj') ('FormsRegression-'+[Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $folder)
$db=Join-Path $folder 'cuahang.mdb'
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'Database\cuahang.dll') -Destination $db
[CuahangNongduoc.DataService]::m_ConnectString="Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$db;OLE DB Services=-2"
[CuahangNongduoc.ErrorLog]::DirectoryPath=Join-Path $folder 'Logs'
$repo=New-Object CuahangNongduoc.AccountRepository
$repo.EnsureSchema()
[CuahangNongduoc.AppSession]::CurrentUser=$repo.FindByUsername('admin')
$flags=[Reflection.BindingFlags]'Instance,NonPublic'
function Field($form,$name) { return $form.GetType().GetField($name,$flags).GetValue($form) }
function Invoke($form,$name) { [void]$form.GetType().GetMethod($name,$flags).Invoke($form,@()) }
function Pump { [System.Windows.Forms.Application]::DoEvents(); [System.Windows.Forms.Application]::DoEvents() }
function UnitName($id) {
 $connection=New-Object System.Data.OleDb.OleDbConnection([CuahangNongduoc.DataService]::m_ConnectString)
 try { $connection.Open();$command=$connection.CreateCommand();$command.CommandText="SELECT TEN_DON_VI FROM DON_VI_TINH WHERE ID=$id";return $command.ExecuteScalar() }
 finally { $connection.Dispose() }
}
$unit=New-Object CuahangNongduoc.frmDonViTinh
try {
 $unit.Show();Pump
 $grid=Field $unit 'dataGridView';$nav=Field $unit 'bindingNavigator'
 Assert ($null -ne (Field $unit 'toolChinhSua')) 'Unit Edit button is missing'
 Invoke $unit 'ThemDonViTinh';Pump
 Assert ($null -ne $grid.EditingControl) 'Add did not open the unit name editor'
 $grid.EditingControl.Text='Regression unit'
 Invoke $unit 'LuuDonViTinh';Pump
 $id=[int]$nav.BindingSource.Current['ID']
 Assert ($id -gt 0) 'Unit form did not receive the Access-generated key'
 Assert ((UnitName $id) -eq 'Regression unit') 'Unit typed in the grid was not saved'
 Invoke $unit 'SuaDonViTinh';Pump
 $grid.EditingControl.Text='Regression unit edited'
 Invoke $unit 'LuuDonViTinh';Pump
 Assert ((UnitName $id) -eq 'Regression unit edited') 'Unit edit was not saved'
 $count=$nav.BindingSource.Count
 Invoke $unit 'ThemDonViTinh';Pump
 $grid.EditingControl.Text='Unsaved unit'
 Invoke $unit 'HuyDonViTinh';Pump
 Assert ($nav.BindingSource.Count -eq $count) 'Cancel left an extra unit in the list'
 Invoke $unit 'ThemDonViTinh';Pump
 $grid.EditingControl.Text='   '
 $blocked=$false;try { Invoke $unit 'LuuDonViTinh' } catch { $blocked=$true }
 Assert $blocked 'Blank unit name was saved'
 Invoke $unit 'HuyDonViTinh'
} finally { $unit.Dispose() }
# Open actual sale windows and cancel their first, unsaved invoice.
foreach($name in @('frmBanLe','frmBanSi')) {
 $form=New-Object ('CuahangNongduoc.'+$name)
 try {
  $form.Show();Pump
  Invoke $form 'HuyThayDoi';Pump
  $nav=Field $form 'bindingNavigator'
  Assert ($nav.BindingSource.Count -eq 0) ($name+': cancel did not return to empty view')
  $controller=Field $form 'ctrlPhieuBan';$controller.HuyThayDoi();Pump
  Assert ((Field $form 'txtMaPhieu').Text -eq '') ($name+': empty view shows an old ID')
  Assert ((Field $form 'numTongTien').Value -eq 0) ($name+': empty view shows an old total')
 } finally { $form.Dispose() }
}
# Lists open maximized; receipt editors keep their normal centered size.
foreach($name in @('frmNhapHang','frmBanLe','frmBanSi','frmDanhsachPhieuNhap','frmDanhsachPhieuBanLe','frmDanhsachPhieuBanSi')) {
 $parent=New-Object System.Windows.Forms.Form
 $parent.IsMdiContainer=$true;$parent.Width=1500;$parent.Height=950
 $form=New-Object ('CuahangNongduoc.'+$name)
 try {
  $form.MdiParent=$parent;$parent.Show();$form.Show();Pump
  $client=@($parent.Controls | Where-Object { $_ -is [System.Windows.Forms.MdiClient] })[0]
  if ($name.StartsWith('frmDanhsach')) {
   Assert ($form.WindowState -eq [System.Windows.Forms.FormWindowState]::Maximized) ($name+': list is not maximized')
   Assert ($form.Width -ge $client.ClientSize.Width) ($name+': list does not fill the workspace')
  } else {
  Assert ($form.WindowState -eq [System.Windows.Forms.FormWindowState]::Normal) ($name+': form was maximized')
  Assert ($form.Width -lt $client.ClientSize.Width) ($name+': form fills the parent')
  Assert ([Math]::Abs($form.Left-($client.ClientSize.Width-$form.Width)/2) -le 2) ($name+': horizontal centering failed')
  Assert ([Math]::Abs($form.Top-($client.ClientSize.Height-$form.Height)/2) -le 2) ($name+': vertical centering failed')
  }
 } finally { $form.Dispose();$parent.Dispose() }
}
# Open the editor through each real list's Add handler, rather than as an unrelated window.
foreach($listName in @('frmDanhsachPhieuBanLe','frmDanhsachPhieuBanSi')) {
 $parent=New-Object System.Windows.Forms.Form
 $parent.IsMdiContainer=$true;$parent.Width=1500;$parent.Height=950
 $list=New-Object ('CuahangNongduoc.'+$listName)
 $editor=$null
 try {
  $list.MdiParent=$parent;$parent.Show();$list.Show();Pump
  $bounds=$list.Bounds
  [void]$list.GetType().GetMethod('MoPhieuBan',$flags).Invoke($list,@($true));Pump
  $editor=Field $list 'BanLe'
  Assert ($editor.Visible -and $editor.Owner -eq $parent -and $null -eq $editor.MdiParent) ($listName+': editor still shares the MDI workspace')
  Assert ($list.Visible -and !$list.IsDisposed -and $list.WindowState -eq [System.Windows.Forms.FormWindowState]::Maximized) ($listName+': opening Add hid or restored the list')
  Assert ($list.Bounds -eq $bounds) ($listName+': Add resized the background list')
  Assert ($editor.WindowState -eq [System.Windows.Forms.FormWindowState]::Normal) ($listName+': editor changed size')
  Invoke $editor 'HuyThayDoi';$editor.Close();Pump
  Assert ($list.Visible -and $list.WindowState -eq [System.Windows.Forms.FormWindowState]::Maximized) ($listName+': closing Add did not restore the list')
  $source=(Field $list 'bindingNavigator').BindingSource
  if($source.Count -gt 0) {
   $selected=[string]$source.Current['ID']
   [void]$list.GetType().GetMethod('MoPhieuBan',$flags).Invoke($list,@($false));Pump
   $editor=Field $list 'BanLe'
   Assert ((Field $editor 'txtMaPhieu').Text -eq $selected) ($listName+': detail opened the wrong invoice')
   if($source.Count -gt 1){$source.MoveLast();Pump}
   Assert ((Field $editor 'txtMaPhieu').Text -eq $selected) ($listName+': background selection changed the editor invoice')
   $editor.Close();Pump
  }
 } finally { if($null -ne $editor){$editor.Dispose()};$list.Dispose();$parent.Dispose() }
}
Write-Output 'PASS: unit add/edit through the live grid editor, native identity, persisted values, unit cancel/validation, first-sale cancel with empty binding reset, three maximized lists, centered receipt editors, Add/details owned windows that preserve background lists, isolated detail selection.'
Write-Output ('Test database: '+$db)
