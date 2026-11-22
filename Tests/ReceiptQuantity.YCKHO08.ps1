param([string]$ProjectRoot='D:\NongDuoc_BaoTri')
$ErrorActionPreference='Stop'
. (Join-Path $ProjectRoot 'Tests\WarehouseCost.YCKHO05.ps1') -ProjectRoot $ProjectRoot
Assert ((Execute "SELECT SO_LUONG_NHAP FROM MA_SAN_PHAM WHERE ID='K05_L1'" $true) -eq 10) 'Import did not save original receipt quantity'
Assert ((Execute "SELECT SO_LUONG FROM MA_SAN_PHAM WHERE ID='K05_L1'" $true) -eq 5) 'Sale fixture did not reduce current stock'
$controller=New-Object CuahangNongduoc.Controller.MaSanPhamController
$lot=$controller.LayMaSanPham('K05_L1')
Assert ($lot.SoLuongNhap -eq 10 -and $lot.SoLuong -eq 5) 'Lot model confused original and remaining quantities'
$repo.EnsureSchema()
Assert ((Execute "SELECT SO_LUONG_NHAP FROM MA_SAN_PHAM WHERE ID='K05_L1'" $true) -eq 10) 'Repeat startup overwrote original quantity'
$sale=New-Object CuahangNongduoc.Controller.PhieuBanController
$sale.XoaPhieuBan('K05_B1')
Assert ((Execute "SELECT SO_LUONG FROM MA_SAN_PHAM WHERE ID='K05_L1'" $true) -eq 10) 'Sale deletion did not restore stock'
Assert ((Execute "SELECT SO_LUONG_NHAP FROM MA_SAN_PHAM WHERE ID='K05_L1'" $true) -eq 10) 'Restoring stock changed original quantity'
Execute "INSERT INTO MA_SAN_PHAM (ID,ID_SAN_PHAM,ID_PHIEU_NHAP,DON_GIA_NHAP,SO_LUONG,NGAY_NHAP,NGAY_SAN_XUAT,NGAY_HET_HAN) VALUES ('K08_LEGACY','K05_SP','PN_K05_L1',100,3,Date(),Date(),#12/31/2099#)"
$repo.EnsureSchema()
Assert ((Execute "SELECT SO_LUONG_NHAP FROM MA_SAN_PHAM WHERE ID='K08_LEGACY'" $true) -is [DBNull]) 'Migration fabricated legacy original quantity'
Assert ($null -eq $controller.LayMaSanPham('K08_LEGACY').SoLuongNhap) 'Unknown original quantity was mapped to zero/current stock'
$form=New-Object CuahangNongduoc.frmNhapHang
try {
 $flags=[Reflection.BindingFlags]'Instance,NonPublic'
 $grid=$form.GetType().GetField('dataGridView',$flags).GetValue($form)
 $column=$grid.Columns['colSoLuongNhap']
 Assert ($column.ReadOnly -and $column.DataPropertyName -eq 'SO_LUONG_NHAP') 'Receipt original quantity column is missing/editable'
}finally{$form.Dispose()}
Write-Output 'PASS YC-KHO-08: receipt quantity stored, model and readonly column, sale/restore independence, repeat migration and legacy unknown values.'
