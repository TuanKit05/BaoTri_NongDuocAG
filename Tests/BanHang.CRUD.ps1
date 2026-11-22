param([string]$ProjectRoot = 'D:\NongDuoc_BaoTri', [string]$AssemblyPath = '')
$ErrorActionPreference = 'Stop'
function Assert($condition, $message) { if (!$condition) { throw $message } }
if ([string]::IsNullOrEmpty($AssemblyPath)) { $AssemblyPath=Join-Path $ProjectRoot 'bin\Debug\CuahangNongduoc.exe' }
[void][Reflection.Assembly]::LoadFrom($AssemblyPath)
[Runtime.CompilerServices.RuntimeHelpers]::RunClassConstructor([CuahangNongduoc.DataService].TypeHandle)
$folder = Join-Path (Join-Path $ProjectRoot 'obj') ('NongDuocCRUD-' + [Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $folder)
$db = Join-Path $folder 'cuahang.mdb'
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'Database\cuahang.dll') -Destination $db
[CuahangNongduoc.DataService]::m_ConnectString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$db;OLE DB Services=-2"
function Query([string]$sql) {
 $connection = New-Object System.Data.OleDb.OleDbConnection([CuahangNongduoc.DataService]::m_ConnectString)
 try {
  $connection.Open(); $cmd=$connection.CreateCommand(); $cmd.CommandText=$sql
  $table=New-Object System.Data.DataTable
  $adapter=New-Object System.Data.OleDb.OleDbDataAdapter($cmd)
  [void]$adapter.Fill($table); return ,$table
 } finally { $connection.Dispose() }
}
$repo=New-Object CuahangNongduoc.AccountRepository
$repo.EnsureSchema()
[CuahangNongduoc.AppSession]::CurrentUser=$repo.FindByUsername('admin')
$customer=[string](Query 'SELECT TOP 1 ID FROM KHACH_HANG').Rows[0][0]
$lotRow=(Query 'SELECT TOP 1 ID,ID_SAN_PHAM,SO_LUONG FROM MA_SAN_PHAM WHERE SO_LUONG>=5 AND NGAY_HET_HAN>=Date()').Rows[0]
Assert ($null -ne $lotRow) 'A nonexpired lot with at least five units is needed'
$lot=[string]$lotRow['ID']; $escapedLot=$lot.Replace("'","''")
$stock=[int]$lotRow['SO_LUONG']; $product=[string]$lotRow['ID_SAN_PHAM']
$id='CRUD'+[Guid]::NewGuid().ToString('N').Substring(0,8)
$ctrl=New-Object CuahangNongduoc.Controller.PhieuBanController
$details=New-Object CuahangNongduoc.Controller.ChiTietPhieuBanController
$grid=New-Object System.Windows.Forms.DataGridView
try {
 $details.HienThiChiTiet($grid,$id)
 $line=$details.NewRow(); $line['ID_PHIEU_BAN']=$id; $line['ID_MA_SAN_PHAM']=$lot
 $line['SO_LUONG']=2; $line['DON_GIA']=100; $line['THANH_TIEN']=200; $details.Add($line)
 $header=$ctrl.NewRow(); $header['ID']=$id; $header['ID_KHACH_HANG']=$customer
 $header['NGAY_BAN']=[DateTime]::Today; $header['TONG_TIEN']=200; $header['DA_TRA']=0; $header['CON_NO']=200
 $ctrl.Add($header); $ctrl.LuuPhieuVaChiTiet($details)
 Assert ((Query "SELECT SO_LUONG FROM MA_SAN_PHAM WHERE ID='$escapedLot'").Rows[0][0] -eq ($stock-2)) 'Create did not deduct stock'
 # A second save must not deduct the same units again.
 $ctrl.LuuPhieuVaChiTiet($details)
 Assert ((Query "SELECT SO_LUONG FROM MA_SAN_PHAM WHERE ID='$escapedLot'").Rows[0][0] -eq ($stock-2)) 'Repeated save deducted stock twice'
 $line['SO_LUONG']=3; $line['THANH_TIEN']=300
 $header['TONG_TIEN']=300; $header['CON_NO']=300
 $ctrl.LuuPhieuVaChiTiet($details)
 Assert ((Query "SELECT SO_LUONG FROM MA_SAN_PHAM WHERE ID='$escapedLot'").Rows[0][0] -eq ($stock-3)) 'Edit did not apply only the stock difference'
 # Invalid payment must leave both the stored header and stock unchanged.
 $header['DA_TRA']=301; $header['CON_NO']=-1
 $blocked=$false; try { $ctrl.LuuPhieuVaChiTiet($details) } catch { $blocked=$true }
 Assert $blocked 'Overpayment was accepted'
 $ctrl.HuyThayDoi()
 Assert ([int]$header['DA_TRA'] -eq 0) 'Cancel did not restore the header'
 Assert ((Query "SELECT SO_LUONG FROM MA_SAN_PHAM WHERE ID='$escapedLot'").Rows[0][0] -eq ($stock-3)) 'Cancel changed stock'
 # Deleting a line restores its lot within the same transaction.
 $line.Delete(); $replacement=$details.NewRow()
 $replacement['ID_PHIEU_BAN']=$id; $replacement['ID_MA_SAN_PHAM']=$lot
 $replacement['SO_LUONG']=1; $replacement['DON_GIA']=100; $replacement['THANH_TIEN']=100
 $details.Add($replacement); $header['TONG_TIEN']=100; $header['CON_NO']=100
 $ctrl.LuuPhieuVaChiTiet($details)
 Assert ((Query "SELECT SO_LUONG FROM MA_SAN_PHAM WHERE ID='$escapedLot'").Rows[0][0] -eq ($stock-1)) 'Delete and replacement did not restore the old quantity'
 $products=New-Object CuahangNongduoc.DataLayer.SanPhamFactory
 $table=$products.DanhSach()
 $expected=(Query ("SELECT SUM(SO_LUONG) FROM MA_SAN_PHAM WHERE ID_SAN_PHAM='"+$product.Replace("'","''")+"'")).Rows[0][0]
 Assert ($table.Select("ID='"+$product.Replace("'","''")+"'")[0]['SO_LUONG'] -eq $expected) 'Product stock does not equal the sum of lots'
 $ctrl.XoaPhieuBan($id)
 Assert ((Query "SELECT SO_LUONG FROM MA_SAN_PHAM WHERE ID='$escapedLot'").Rows[0][0] -eq $stock) 'Invoice deletion did not restore stock'
 Assert ((Query "SELECT COUNT(*) FROM PHIEU_BAN WHERE ID='$id'").Rows[0][0] -eq 0) 'Invoice was not deleted'
 Assert ([CuahangNongduoc.BusinessObject.TienTe]::ThanhTien([decimal]100.5,3) -eq 303) 'VND rounding changed'
 # Exercise both forms without opening windows or writing production data.
 $flags=[Reflection.BindingFlags]'Instance,NonPublic'
 foreach($name in @('frmBanLe','frmBanSi')) {
  $form=New-Object ('CuahangNongduoc.'+$name)
  try {
   $form.BindingContext=New-Object System.Windows.Forms.BindingContext
   [void]$form.GetType().GetMethod('frmNhapHang_Load',$flags).Invoke($form,@($form,[EventArgs]::Empty))
   [void]$form.GetType().GetMethod('Allow',$flags).Invoke($form,@($true))
   $save=$form.GetType().GetField('toolLuu',$flags).GetValue($form)
   $cancel=$form.GetType().GetField('toolHuy',$flags).GetValue($form)
   $edit=$form.GetType().GetField('toolChinhSua',$flags).GetValue($form)
   $delete=$form.GetType().GetField('toolXoa',$flags).GetValue($form)
   Assert ($save.Enabled -and $cancel.Enabled -and !$edit.Enabled -and !$delete.Enabled) ($name+': invalid add mode')
   [void]$form.GetType().GetMethod('HuyThayDoi',$flags).Invoke($form,@())
   Assert (!$save.Enabled -and !$cancel.Enabled -and !$edit.Enabled -and !$delete.Enabled) ($name+': empty view allows writes')
   [void]$form.GetType().GetMethod('toolLuu_Them_Click',$flags).Invoke($form,@($form,[EventArgs]::Empty))
   Assert ($save.Enabled -and $cancel.Enabled) ($name+': cannot start another invoice after cancel')
   $customerBox=$form.GetType().GetField('cmbKhachHang',$flags).GetValue($form)
   Assert ($customerBox.SelectedIndex -eq -1) ($name+': new invoice reused a previous customer')
   $customerBox.SelectedIndex=0
   $invoiceId='UI'+[Guid]::NewGuid().ToString('N').Substring(0,8)
   $form.GetType().GetField('txtMaPhieu',$flags).GetValue($form).Text=$invoiceId
   $formDetails=$form.GetType().GetField('ctrlChiTiet',$flags).GetValue($form)
   $uiLine=$formDetails.NewRow(); $uiLine['ID_PHIEU_BAN']=$invoiceId; $uiLine['ID_MA_SAN_PHAM']=$lot
   $uiLine['SO_LUONG']=1; $uiLine['DON_GIA']=100; $uiLine['THANH_TIEN']=100; $formDetails.Add($uiLine)
   [void]$form.GetType().GetMethod('DongBoTienHang',$flags).Invoke($form,@())
   $formGrid=$form.GetType().GetField('dgvDanhsachSP',$flags).GetValue($form)
   $validation=[CuahangNongduoc.Controller.SaleInputValidation]::CheckHeader($customerBox,$formGrid)
   Assert ($validation.Length -eq 0) ($name+': '+$validation)
   Write-Output ($name+': saving test invoice; rows='+$formGrid.Rows.Count+'; total='+$form.GetType().GetField('numTongTien',$flags).GetValue($form).Value)
   [void]$form.GetType().GetMethod('toolLuu_Click',$flags).Invoke($form,@($form,[EventArgs]::Empty))
   Assert (!$save.Enabled -and $edit.Enabled -and $delete.Enabled) ($name+': successful save did not return to view')
   Assert ((Query "SELECT COUNT(*) FROM PHIEU_BAN WHERE ID='$invoiceId'").Rows[0][0] -eq 1) ($name+': invoice not saved')
   [void]$form.GetType().GetMethod('toolChinhSua_Click',$flags).Invoke($form,@($form,[EventArgs]::Empty))
   Assert ($save.Enabled -and $cancel.Enabled -and !$delete.Enabled) ($name+': invalid edit mode')
   [void]$form.GetType().GetMethod('HuyThayDoi',$flags).Invoke($form,@())
   Assert ($form.GetType().GetField('txtMaPhieu',$flags).GetValue($form).Text -eq $invoiceId) ($name+': edit cancellation restored the wrong invoice')
   $formCtrl=$form.GetType().GetField('ctrlPhieuBan',$flags).GetValue($form)
   $formCtrl.XoaPhieuBan($invoiceId)
  } finally { $form.Dispose() }
 }
 # Product catalog CRUD must never overwrite stock derived from lots.
 $catalog=New-Object CuahangNongduoc.DataLayer.SanPhamFactory
 [void]$catalog.DanhSach()
 $productId='CP'+[Guid]::NewGuid().ToString('N').Substring(0,8)
 $unit=(Query 'SELECT TOP 1 ID FROM DON_VI_TINH').Rows[0][0]
 $newProduct=$catalog.NewRow(); $newProduct['ID']=$productId; $newProduct['TEN_SAN_PHAM']='CRUD test'
 $newProduct['ID_DON_VI_TINH']=$unit; $newProduct['DON_GIA_NHAP']=100
 $newProduct['GIA_BAN_SI']=120; $newProduct['GIA_BAN_LE']=150
 $catalog.Rows.Add($newProduct); Assert ($catalog.Save()) 'Product create failed'
 $newProduct['TEN_SAN_PHAM']='CRUD edited'; Assert ($catalog.Save()) 'Product edit failed'
 Assert ((Query "SELECT TEN_SAN_PHAM FROM SAN_PHAM WHERE ID='$productId'").Rows[0][0] -eq 'CRUD edited') 'Product edit not persisted'
 $newProduct.Delete(); Assert ($catalog.Save()) 'Product delete failed'
 Assert ((Query "SELECT COUNT(*) FROM SAN_PHAM WHERE ID='$productId'").Rows[0][0] -eq 0) 'Product delete not persisted'
 # Import create, header edit, invalid payment rollback, and delete use the same copied database.
 $import=New-Object CuahangNongduoc.DataLayer.NhapHangService
 $importId='IN'+[Guid]::NewGuid().ToString('N').Substring(0,8)
 $vendor=[string](Query 'SELECT TOP 1 ID FROM NHA_CUNG_CAP').Rows[0][0]
 $newLot=New-Object CuahangNongduoc.BusinessObject.LoNhapInput
 $newLot.MaLo='LT'+[Guid]::NewGuid().ToString('N').Substring(0,8); $newLot.MaSanPham=$product
 $newLot.SoLuong=2; $newLot.DonGiaNhap=100; $newLot.NgayNhap=[DateTime]::Today
 $newLot.NgaySanXuat=[DateTime]::Today; $newLot.NgayHetHan=[DateTime]::Today.AddYears(1)
 $lotList=New-Object 'System.Collections.Generic.List[CuahangNongduoc.BusinessObject.LoNhapInput]'
 $lotList.Add($newLot); $message=''
 Assert ($import.LuuPhieuNhap($importId,[DateTime]::Today,200,$vendor,0,200,$true,$lotList,[ref]$message)) ('Import create: '+$message)
 $emptyLots=New-Object 'System.Collections.Generic.List[CuahangNongduoc.BusinessObject.LoNhapInput]'
 Assert ($import.LuuPhieuNhap($importId,[DateTime]::Today,200,$vendor,100,100,$false,$emptyLots,[ref]$message)) ('Import edit: '+$message)
 Assert (!$import.LuuPhieuNhap($importId,[DateTime]::Today,200,$vendor,201,-1,$false,$emptyLots,[ref]$message)) 'Import overpayment accepted'
 Assert (!$import.XoaPhieuNhap($importId,[ref]$message)) 'Paid import could be deleted'
 Assert ($import.LuuPhieuNhap($importId,[DateTime]::Today,200,$vendor,0,200,$false,$emptyLots,[ref]$message)) ('Import payment reset: '+$message)
 Assert ($import.XoaPhieuNhap($importId,[ref]$message)) ('Import delete: '+$message)
 Assert ((Query "SELECT COUNT(*) FROM MA_SAN_PHAM WHERE ID_PHIEU_NHAP='$importId'").Rows[0][0] -eq 0) 'Import lots not deleted'
 # Binding the complete invoice schema must not expose new database columns.
 foreach($listName in @('frmDanhsachPhieuBanLe','frmDanhsachPhieuBanSi')) {
  $parent=New-Object System.Windows.Forms.Form
  $parent.IsMdiContainer=$true; $parent.Width=1200; $parent.Height=800
  $list=New-Object ('CuahangNongduoc.'+$listName)
  try {
   $list.MdiParent=$parent; $parent.Show(); $list.Show()
   [System.Windows.Forms.Application]::DoEvents()
   [System.Windows.Forms.Application]::DoEvents()
   $listGrid=$list.GetType().GetField('dataGridView',$flags).GetValue($list)
   Assert (!$listGrid.AutoGenerateColumns -and $listGrid.Columns.Count -eq 6) ($listName+': raw schema columns exposed')
   foreach($column in $listGrid.Columns) {
    Assert (!$column.HeaderText.Contains('_')) ($listName+': database field used as heading')
   }
   Assert ($list.WindowState -eq [System.Windows.Forms.FormWindowState]::Maximized) ($listName+': first open must maximize the list')
   $mdiClient=@($parent.Controls | Where-Object { $_ -is [System.Windows.Forms.MdiClient] })[0]
   Assert ($list.Width -ge $mdiClient.ClientSize.Width) ($listName+': list does not fill the workspace')
  } finally { $list.Dispose(); $parent.Dispose() }
 }
 Write-Output 'PASS: sales CRUD, form states and first-open layout, explicit invoice columns, stock differences, payment rollback, cancel, stock totals, rounding, product CRUD, import CRUD and deletion guards.'
 Write-Output ('Test database: '+$db)
} finally { $grid.Dispose(); [System.Data.OleDb.OleDbConnection]::ReleaseObjectPool() }
