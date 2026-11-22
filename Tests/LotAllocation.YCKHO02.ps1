param([string]$ProjectRoot='D:\NongDuoc_BaoTri', [string]$AssemblyPath='')
$ErrorActionPreference='Stop'
function Assert($condition,$message) { if (!$condition) { throw $message } }
if (!$AssemblyPath) { $AssemblyPath=Join-Path $ProjectRoot 'bin\Debug\CuahangNongduoc.exe' }
[void][Reflection.Assembly]::LoadFrom($AssemblyPath)
[Runtime.CompilerServices.RuntimeHelpers]::RunClassConstructor([CuahangNongduoc.DataService].TypeHandle)
$folder=Join-Path $ProjectRoot ('obj\LotAllocation-'+[Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $folder)
$db=Join-Path $folder 'cuahang.mdb'
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'Database\cuahang.dll') -Destination $db
[CuahangNongduoc.DataService]::m_ConnectString="Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$db;OLE DB Services=-2"
function Execute([string]$sql,[bool]$scalar=$false) {
 $c=New-Object System.Data.OleDb.OleDbConnection([CuahangNongduoc.DataService]::m_ConnectString)
 try {$c.Open();$cmd=$c.CreateCommand();$cmd.CommandText=$sql;if($scalar){return $cmd.ExecuteScalar()};[void]$cmd.ExecuteNonQuery()}finally{$c.Dispose()}
}
$repo=New-Object CuahangNongduoc.AccountRepository
$repo.EnsureSchema()
[CuahangNongduoc.AppSession]::CurrentUser=$repo.FindByUsername('admin')
[CuahangNongduoc.WarehouseSettings]::Save([CuahangNongduoc.WarehouseSettings]::FirstInFirstOut)
$unit=Execute 'SELECT TOP 1 ID FROM DON_VI_TINH' $true
$vendor=Execute 'SELECT TOP 1 ID FROM NHA_CUNG_CAP' $true
$customer=Execute 'SELECT TOP 1 ID FROM KHACH_HANG' $true
Execute "INSERT INTO SAN_PHAM (ID,TEN_SAN_PHAM,ID_DON_VI_TINH,DON_GIA_NHAP,GIA_BAN_SI,GIA_BAN_LE,SO_LUONG) VALUES ('K02_SP','K02 product',$unit,100,120,150,0)"
Execute "INSERT INTO PHIEU_NHAP (ID,ID_NHA_CUNG_CAP,NGAY_NHAP,TONG_TIEN,DA_TRA,CON_NO) VALUES ('K02_PN','$vendor',Date(),0,0,0)"
foreach($lot in @(@('K02_LATE',8,'12/31/2099'),@('K02_EARLY',3,'01/01/2099'),@('K02_EXPIRED',100,'01/01/2000'))) {
 Execute "INSERT INTO MA_SAN_PHAM (ID,ID_SAN_PHAM,ID_PHIEU_NHAP,DON_GIA_NHAP,SO_LUONG,NGAY_NHAP,NGAY_SAN_XUAT,NGAY_HET_HAN) VALUES ('$($lot[0])','K02_SP','K02_PN',100,$($lot[1]),Date(),Date(),#$($lot[2])#)"
}
$details=New-Object CuahangNongduoc.Controller.ChiTietPhieuBanController
$grid=New-Object System.Windows.Forms.DataGridView
try {
 $details.HienThiChiTiet($grid,'K02_PB')
 $table=$details.NewRow().Table
 Assert ($details.ThemTuDong($customer,'K02_SP','K02_PB',5,150)) 'FIFO mode not applied'
 Assert ($table.Rows.Count -eq 2) 'Request was not split across lots'
 Assert ($table.Rows[0]['ID_MA_SAN_PHAM'] -eq 'K02_EARLY' -and $table.Rows[0]['SO_LUONG'] -eq 3) 'Nearest expiry not allocated first'
 Assert ($table.Rows[1]['ID_MA_SAN_PHAM'] -eq 'K02_LATE' -and $table.Rows[1]['SO_LUONG'] -eq 2) 'Remainder not allocated correctly'
 Assert ($details.ThemTuDong($customer,'K02_SP','K02_PB',2,150)) 'Repeated allocation failed'
 Assert ($table.Rows.Count -eq 2 -and $table.Rows[1]['SO_LUONG'] -eq 4) 'Pending quantities ignored or duplicate lot created'
 $failed=$false;try{$details.ThemTuDong($customer,'K02_SP','K02_PB',5,150)}catch{$failed=$true}
 Assert $failed 'Insufficient stock or expired stock accepted'
 Assert ($table.Rows.Count -eq 2 -and $table.Rows[1]['SO_LUONG'] -eq 4) 'Failed request partially changed invoice'
 $sale=New-Object CuahangNongduoc.Controller.PhieuBanController
 $header=$sale.NewRow();$header['ID']='K02_PB';$header['ID_KHACH_HANG']=$customer;$header['NGAY_BAN']=[DateTime]::Today;$header['TONG_TIEN']=1050;$header['DA_TRA']=0;$header['CON_NO']=1050;$sale.Add($header)
 $sale.LuuPhieuVaChiTiet($details)
 Assert ((Execute "SELECT SO_LUONG FROM MA_SAN_PHAM WHERE ID='K02_EARLY'" $true) -eq 0) 'Early lot stock not deducted'
 Assert ((Execute "SELECT SO_LUONG FROM MA_SAN_PHAM WHERE ID='K02_LATE'" $true) -eq 4) 'Late lot stock not deducted'
 Assert ((Execute "SELECT SO_LUONG FROM MA_SAN_PHAM WHERE ID='K02_EXPIRED'" $true) -eq 100) 'Expired lot was consumed'
 # Existing invoice rows are already deducted in database and must not be counted twice.
 Assert ($details.ThemTuDong($customer,'K02_SP','K02_PB',1,150)) 'Edit allocation failed'
 Assert ($table.Rows[1]['SO_LUONG'] -eq 5) 'Saved invoice stock counted twice'
 $header['TONG_TIEN']=1200;$header['CON_NO']=1200;$sale.LuuPhieuVaChiTiet($details)
 Assert ((Execute "SELECT SO_LUONG FROM MA_SAN_PHAM WHERE ID='K02_LATE'" $true) -eq 3) 'Edit deducted original quantities again'
 [CuahangNongduoc.WarehouseSettings]::Save([CuahangNongduoc.WarehouseSettings]::SpecificLot)
 Assert (!$details.ThemTuDong($customer,'K02_SP','K02_PB',1,150)) 'Manual mode still auto allocates'
 Assert ($table.Rows[1]['SO_LUONG'] -eq 5) 'Manual mode mutated rows'
}finally{$grid.Dispose()}
 # YC-KHO-04: sold-out and subsequently expired lots remain visible on saved invoices.
 Execute "UPDATE MA_SAN_PHAM SET NGAY_HET_HAN=#01/01/2000# WHERE ID='K02_EARLY'"
 $refresh=New-Object CuahangNongduoc.DataService;$refresh.CloseConnection()
 $historicalGrid=New-Object System.Windows.Forms.DataGridView
 try {
  $historicalGrid.AutoGenerateColumns=$false
  $lotColumn=New-Object System.Windows.Forms.DataGridViewComboBoxColumn
  $lotColumn.Name='colMaSanPham'
  [void]$historicalGrid.Columns.Add($lotColumn)
  $lotController=New-Object CuahangNongduoc.Controller.MaSanPhamController
  $lotController.HienThiDataGridViewComboBox($lotColumn)
  [CuahangNongduoc.SaleLotDisplay]::Attach($historicalGrid,$lotColumn)
  $historicalDetails=New-Object CuahangNongduoc.Controller.ChiTietPhieuBanController
  $historicalDetails.HienThiChiTiet($historicalGrid,'K02_PB')
  $hostForm=New-Object System.Windows.Forms.Form
  try {
   $hostForm.Controls.Add($historicalGrid);$hostForm.Show();[System.Windows.Forms.Application]::DoEvents()
   $earlyRow=@($historicalGrid.Rows | Where-Object { !$_.IsNewRow -and $_.Cells['colMaSanPham'].Value -eq 'K02_EARLY' })[0]
   Assert ($null -ne $earlyRow) 'Sold-out historical lot disappeared'
   Assert ($earlyRow.Cells['colMaSanPham'].FormattedValue -eq 'K02_EARLY') 'Historical lot code is not displayed'
   Assert ($earlyRow.Cells['colHanDung'].FormattedValue -eq '01/01/2000') 'Historical expiry is not displayed'
  }finally{$hostForm.Dispose()}
  $printedInvoice=New-Object CuahangNongduoc.BusinessObject.PhieuBan
  $printedInvoice.ChiTiet=$historicalDetails.ChiTietPhieuBan('K02_PB')
  $printTable=[CuahangNongduoc.SalePrintData]::Details($printedInvoice)
  $printedEarly=@($printTable.Rows | Where-Object { $_['MaLo'] -eq 'K02_EARLY' })[0]
  Assert ($null -ne $printedEarly -and $printedEarly['HanDung'] -eq '01/01/2000') 'Historical lot/expiry missing in print data'
 }finally{$historicalGrid.Dispose()}
 Execute "UPDATE MA_SAN_PHAM SET NGAY_HET_HAN=#01/01/2099# WHERE ID='K02_EARLY'"
 Write-Output 'PASS YC-KHO-04: saved invoice grid and print data retain lot codes and expiry for sold-out/expired lots.'
 $sale.XoaPhieuBan('K02_PB')
 $flags=[Reflection.BindingFlags]'Instance,NonPublic'
 function Field($form,$name) { return $form.GetType().GetField($name,$flags).GetValue($form) }
 foreach($name in @('frmBanLe','frmBanSi')) {
  $form=New-Object ('CuahangNongduoc.'+$name)
  try {
   $form.Show();[System.Windows.Forms.Application]::DoEvents()
   (Field $form 'cmbKhachHang').SelectedIndex=0
   (Field $form 'cmbSanPham').SelectedValue='K02_SP'
   $combo=Field $form 'cmbMaSanPham'
   Assert ($combo.Enabled) ($name+': manual lot selector disabled')
   Assert ($combo.SelectedValue -eq 'K02_EARLY') ($name+': default is not nearest expiry')
   Assert ($combo.Items.Count -eq 2) ($name+': expired lot in selector')
   Assert ($combo.Text.Contains('01/01/2099')) ($name+': expiry missing in lot selector')
   # Reload to ensure the display column is not added twice.
   (Field $form 'cmbSanPham').SelectedIndex=-1
   (Field $form 'cmbSanPham').SelectedValue='K02_SP'
   $combo.SelectedValue='K02_LATE'
   (Field $form 'numSoLuong').Value=2
   [void]$form.GetType().GetMethod('btnAdd_Click',$flags).Invoke($form,@($null,[EventArgs]::Empty))
   $rows=(Field $form 'ctrlChiTiet').NewRow().Table
   Assert ($rows.Rows.Count -eq 1 -and $rows.Rows[0]['ID_MA_SAN_PHAM'] -eq 'K02_LATE') ($name+': explicit lot ignored')
   $detailGrid=Field $form 'dgvDanhsachSP'
   Assert ($detailGrid.Columns['colMaSanPham'].HeaderText -eq ([string][char]83+[char]7889+' l'+[char]244)) ($name+': lot header incorrect')
   Assert ($detailGrid.Rows[0].Cells['colHanDung'].FormattedValue -eq '31/12/2099') ($name+': detail expiry missing')
   [void]$form.GetType().GetMethod('HuyThayDoi',$flags).Invoke($form,@())
  }finally{$form.Dispose()}
 }
 Write-Output 'PASS: manual-lot defaults, explicit selection, expiry filtering, repeated loading and detail expiry in both sale forms.'
 [CuahangNongduoc.WarehouseSettings]::Save([CuahangNongduoc.WarehouseSettings]::FirstInFirstOut)
 $flags=[Reflection.BindingFlags]'Instance,NonPublic'
 function Field($form,$name) { return $form.GetType().GetField($name,$flags).GetValue($form) }
 foreach($name in @('frmBanLe','frmBanSi')) {
  $form=New-Object ('CuahangNongduoc.'+$name)
  try {
   $form.Show();[System.Windows.Forms.Application]::DoEvents()
   (Field $form 'cmbKhachHang').SelectedIndex=0
   (Field $form 'cmbSanPham').SelectedValue='K02_SP'
   (Field $form 'numSoLuong').Value=5
   Assert (!(Field $form 'cmbMaSanPham').Enabled) ($name+': manual lot selector remains enabled')
   [void]$form.GetType().GetMethod('btnAdd_Click',$flags).Invoke($form,@($null,[EventArgs]::Empty))
   $rows=(Field $form 'ctrlChiTiet').NewRow().Table
   Assert ($rows.Rows.Count -eq 2) ($name+': Add did not split rows')
   Assert ($rows.Rows[0]['ID_MA_SAN_PHAM'] -eq 'K02_EARLY' -and $rows.Rows[0]['SO_LUONG'] -eq 3) ($name+': incorrect first lot')
   Assert ($rows.Rows[1]['SO_LUONG'] -eq 2) ($name+': incorrect remainder')
   [void]$form.GetType().GetMethod('HuyThayDoi',$flags).Invoke($form,@())
  }finally{$form.Dispose()}
 }
Write-Output 'PASS: expiry order, multi-lot splitting, repeated additions, shortage atomicity, expired exclusion, save/edit stock, manual mode.'
Write-Output 'PASS: actual retail and wholesale Add handlers split rows with manual lot selection disabled.'
