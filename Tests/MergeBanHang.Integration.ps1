param([string]$ProjectRoot='D:\NongDuoc_BaoTri\obj\MergeBanHang\merged')
$ErrorActionPreference='Stop'
. (Join-Path $ProjectRoot 'Tests\WarehouseCost.YCKHO05.ps1') -ProjectRoot $ProjectRoot
Execute "INSERT INTO DICH_VU (ID,TEN_DICH_VU,DANG_SU_DUNG) VALUES ('MERGE_DV','Merge test service',TRUE)"
$refresh=New-Object CuahangNongduoc.DataService;$refresh.CloseConnection()
$ctrl=New-Object CuahangNongduoc.Controller.PhieuBanController
$detail=New-Object CuahangNongduoc.Controller.ChiTietPhieuBanController
$grid=New-Object System.Windows.Forms.DataGridView
try {
 $detail.HienThiChiTiet($grid,'MERGE_SALE')
 foreach($gift in @($false,$true)){
  $line=$detail.NewRow();$line['ID_PHIEU_BAN']='MERGE_SALE';$line['ID_MA_SAN_PHAM']='K05_L2';$line['SO_LUONG']=1;$line['DON_GIA']=700;$line['THANH_TIEN']=700;$line['LA_HANG_TANG']=$gift;$detail.Add($line)
 }
 $header=$ctrl.NewRow();$header['ID']='MERGE_SALE';$header['ID_KHACH_HANG']=$customer;$header['NGAY_BAN']=[DateTime]::Today;$header['TONG_TIEN']=750;$header['DA_TRA']=0;$header['CON_NO']=750;$header['TIEN_VAN_CHUYEN']=30;$ctrl.Add($header)
 $phi=New-Object CuahangNongduoc.BusinessObject.PhieuBan;$phi.Id='MERGE_SALE';$phi.PhiVanChuyen=30
 $service=New-Object CuahangNongduoc.BusinessObject.DichVuPhieuBan;$service.Ten='Merge test service';$service.TienDV=20;$phi.DichVu.Add($service)
 $before=Execute "SELECT SO_LUONG FROM MA_SAN_PHAM WHERE ID='K05_L2'" $true
 $ctrl.LuuPhieuVaChiTiet($detail,$phi)
 Assert ((Execute "SELECT COUNT(*) FROM CHI_TIET_PHIEU_BAN WHERE ID_PHIEU_BAN='MERGE_SALE'" $true) -eq 2) 'Paid/gifted lines for same lot collided'
 Assert ((Execute "SELECT THANH_TIEN FROM CHI_TIET_PHIEU_BAN WHERE ID_PHIEU_BAN='MERGE_SALE' AND LA_HANG_TANG=TRUE" $true) -eq 0) 'Gift line charged money'
 Assert ((Execute "SELECT SO_LUONG FROM MA_SAN_PHAM WHERE ID='K05_L2'" $true) -eq $before-2) 'Gift did not consume stock'
 Assert ((Execute "SELECT SUM(SO_LUONG) FROM BIEN_DONG_KHO WHERE MA_PHIEU='MERGE_SALE'" $true) -eq -2) 'Stock ledger missing'
 Assert ((Execute "SELECT SUM(THANH_TIEN) FROM CHI_TIET_DICH_VU WHERE ID_PHIEU_BAN='MERGE_SALE'" $true) -eq 20) 'Services not saved'
 Assert ((Execute "SELECT SUM(THANH_TIEN_GIA_VON) FROM CHI_TIET_PHIEU_BAN WHERE ID_PHIEU_BAN='MERGE_SALE'" $true) -eq 600) 'Cost aliases disagree with saved FIFO costs'
 $ctrl.XoaPhieuBan('MERGE_SALE')
 Assert ((Execute "SELECT SO_LUONG FROM MA_SAN_PHAM WHERE ID='K05_L2'" $true) -eq $before) 'Deleting gift sale failed to restore stock'
 Assert ((Execute "SELECT COUNT(*) FROM CHI_TIET_DICH_VU WHERE ID_PHIEU_BAN='MERGE_SALE'" $true) -eq 0) 'Deleted sale retained service rows'
 Assert ((Execute "SELECT SUM(SO_LUONG) FROM BIEN_DONG_KHO WHERE MA_PHIEU='MERGE_SALE'" $true) -eq 0) 'Delete ledger does not balance'
}finally{$grid.Dispose()}
Write-Output 'PASS: merged paid/gift same-lot rows, fee/services transaction, cost snapshots and aliases, stock ledger and deletion rollback.'
