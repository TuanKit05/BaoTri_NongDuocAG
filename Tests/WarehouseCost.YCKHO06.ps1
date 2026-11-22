param([string]$ProjectRoot='D:\NongDuoc_BaoTri')
$ErrorActionPreference='Stop'
# Reuse the verified receipt/sale fixture in its isolated database copy.
. (Join-Path $ProjectRoot 'Tests\WarehouseCost.YCKHO05.ps1') -ProjectRoot $ProjectRoot
function RefreshConnection { $refresh=New-Object CuahangNongduoc.DataService;$refresh.CloseConnection() }
RefreshConnection
$details=New-Object CuahangNongduoc.Controller.ChiTietPhieuBanController
$stored=@($details.ChiTietPhieuBan('K05_B1'))[0]
Assert ($stored.GiaVon -eq 200 -and $stored.PhuongPhapGia -eq 'BQGQ') 'Business model recomputed old average cost'
Assert ($stored.ThanhTienGiaVon -eq 1000) 'Extended cost does not use saved unit cost'
$byDate=@($details.ChiTietPhieuBan([DateTime]::Today) | Where-Object {$_.MaSanPham.Id -eq 'K05_L1'})[0]
Assert ($byDate.GiaVon -eq 200) 'Daily query discarded snapshot'
$byMonth=@($details.ChiTietPhieuBan([DateTime]::Today.Month,[DateTime]::Today.Year) | Where-Object {$_.MaSanPham.Id -eq 'K05_L1'})[0]
Assert ($byMonth.GiaVon -eq 200) 'Monthly query discarded snapshot'
[CuahangNongduoc.WarehouseSettings]::Save($manual,$average)
ImportLot 'K06_L4' 900
RefreshConnection
Assert (@($details.ChiTietPhieuBan('K05_B2'))[0].GiaVon -eq 300) 'Receipt or method change rewrote FIFO cost'
$grid=New-Object System.Windows.Forms.DataGridView
try {
 $details.HienThiChiTiet($grid,'K05_B1')
 $row=$details.NewRow().Table.Rows[0]
 $row['GIA_VON']=999;$row['PHUONG_PHAP_GIA']='FIFO'
 $details.Save()
 Assert ((Execute "SELECT GIA_VON FROM CHI_TIET_PHIEU_BAN WHERE ID_PHIEU_BAN='K05_B1'" $true) -eq 200) 'Editing old row changed cost snapshot'
 Assert ((Execute "SELECT PHUONG_PHAP_GIA FROM CHI_TIET_PHIEU_BAN WHERE ID_PHIEU_BAN='K05_B1'" $true) -eq 'BQGQ') 'Editing old row changed saved method'
 # Historical unknown costs remain unknown through queries and edits.
 Execute "UPDATE CHI_TIET_PHIEU_BAN SET GIA_VON=NULL,PHUONG_PHAP_GIA=NULL WHERE ID_PHIEU_BAN='K05_B1'"
 RefreshConnection
 Assert ($null -eq @($details.ChiTietPhieuBan('K05_B1'))[0].GiaVon) 'Legacy unknown cost fabricated'
 $details.HienThiChiTiet($grid,'K05_B1');$row=$details.NewRow().Table.Rows[0];$row['GIA_VON']=888
 $details.Save()
 Assert ((Execute "SELECT GIA_VON FROM CHI_TIET_PHIEU_BAN WHERE ID_PHIEU_BAN='K05_B1'" $true) -is [DBNull]) 'Legacy row received invented current cost'
 # Editing staff without cost visibility still pass concurrency checks and cannot alter costs.
 Execute "INSERT INTO VAI_TRO_QUYEN (MaVaiTro,MaQuyen) VALUES ('BANHANG','SuaPhieuBan')"
 [CuahangNongduoc.AppSession]::CurrentUser=$repo.FindByUsername('k05_seller')
 RefreshConnection
 Assert ($null -eq @($details.ChiTietPhieuBan('K05_B3'))[0].GiaVon) 'Cost leaked to business model'
 $details.HienThiChiTiet($grid,'K05_B3');$row=$details.NewRow().Table.Rows[0]
 Assert ($row['GIA_VON'] -eq 0) 'Cost leaked to unauthorized table'

 $row['GIA_VON']=999
 $details.Save()
 Assert ((Execute "SELECT GIA_VON FROM CHI_TIET_PHIEU_BAN WHERE ID_PHIEU_BAN='K05_B3'" $true) -eq 300) 'Seller changed saved cost'
 Assert ($row['GIA_VON'] -eq 0) 'Saving exposed real cost to seller'
}finally{$grid.Dispose()}
Write-Output 'PASS YC-KHO-06: saved unit costs and methods, daily/monthly mapping, receipt/config independence, tamper-resistant edits, legacy nulls and masked-cost permissions.'
