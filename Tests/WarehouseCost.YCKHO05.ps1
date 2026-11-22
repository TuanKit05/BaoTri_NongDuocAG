param([string]$ProjectRoot='D:\NongDuoc_BaoTri')
$ErrorActionPreference='Stop'
function Assert($condition,$message) { if (!$condition) { throw $message } }
[void][Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\Debug\CuahangNongduoc.exe'))
[Runtime.CompilerServices.RuntimeHelpers]::RunClassConstructor([CuahangNongduoc.DataService].TypeHandle)
$folder=Join-Path $ProjectRoot ('obj\WarehouseCost-'+[Guid]::NewGuid().ToString('N'))
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
$manual=[CuahangNongduoc.WarehouseSettings]::SpecificLot
$average=[CuahangNongduoc.WarehouseSettings]::WeightedAverage
$fifo=[CuahangNongduoc.WarehouseSettings]::FirstInFirstOut
[CuahangNongduoc.WarehouseSettings]::Save($manual,$average)
$unit=Execute 'SELECT TOP 1 ID FROM DON_VI_TINH' $true
$vendor=Execute 'SELECT TOP 1 ID FROM NHA_CUNG_CAP' $true
$customer=Execute 'SELECT TOP 1 ID FROM KHACH_HANG' $true
Execute "INSERT INTO SAN_PHAM (ID,TEN_SAN_PHAM,ID_DON_VI_TINH,DON_GIA_NHAP,GIA_BAN_SI,GIA_BAN_LE,SO_LUONG) VALUES ('K05_SP','K05 product',$unit,0,600,700,0)"
$import=New-Object CuahangNongduoc.DataLayer.NhapHangService
function ImportLot($id,[long]$price) {
 $lo=New-Object CuahangNongduoc.BusinessObject.LoNhapInput
 $lo.MaLo=$id;$lo.MaSanPham='K05_SP';$lo.DonGiaNhap=$price;$lo.SoLuong=10
 $lo.NgayNhap=[DateTime]::Today;$lo.NgaySanXuat=[DateTime]::Today.AddDays(-1);$lo.NgayHetHan=[DateTime]'2099-12-31'
 $list=New-Object 'System.Collections.Generic.List[CuahangNongduoc.BusinessObject.LoNhapInput]';$list.Add($lo)
 $message='';Assert ($import.LuuPhieuNhap('PN_'+$id,[DateTime]::Today,($price*10),$vendor,0,($price*10),$true,$list,[ref]$message)) $message
}
ImportLot 'K05_L1' 100
ImportLot 'K05_L2' 300
Assert ([CuahangNongduoc.WarehouseCost]::Preview('K05_L1') -eq 200) 'Moving average after two receipts is incorrect'
function SaveSale($id,$lot,[int]$quantity) {
 $refresh=New-Object CuahangNongduoc.DataService;$refresh.CloseConnection()
 $sale=New-Object CuahangNongduoc.Controller.PhieuBanController
 $details=New-Object CuahangNongduoc.Controller.ChiTietPhieuBanController
 $grid=New-Object System.Windows.Forms.DataGridView
 try {
  $details.HienThiChiTiet($grid,$id)
  $line=$details.NewRow();$line['ID_PHIEU_BAN']=$id;$line['ID_MA_SAN_PHAM']=$lot;$line['SO_LUONG']=$quantity;$line['DON_GIA']=700;$line['THANH_TIEN']=$quantity*700;$details.Add($line)
  $header=$sale.NewRow();$header['ID']=$id;$header['ID_KHACH_HANG']=$customer;$header['NGAY_BAN']=[DateTime]::Today;$header['TONG_TIEN']=$quantity*700;$header['DA_TRA']=0;$header['CON_NO']=$quantity*700;$sale.Add($header)
  $sale.LuuPhieuVaChiTiet($details)
 }finally{$grid.Dispose()}
}
SaveSale 'K05_B1' 'K05_L1' 5
Assert ((Execute "SELECT GIA_VON FROM CHI_TIET_PHIEU_BAN WHERE ID_PHIEU_BAN='K05_B1'" $true) -eq 200) 'Average cost not applied at save'
ImportLot 'K05_L3' 500
Assert ([CuahangNongduoc.WarehouseCost]::Preview('K05_L2') -eq 320) 'Average was not recalculated from remaining stock after sale and receipt'
[CuahangNongduoc.WarehouseSettings]::Save($manual,$fifo)
$repo.EnsureSchema()
Assert ([CuahangNongduoc.WarehouseSettings]::LoadCostMethod() -eq $fifo) 'Cost setting lost after startup migration'
Assert ([CuahangNongduoc.WarehouseCost]::Preview('K05_L2') -eq 300) 'Lot-cost method ignored receipt cost'
SaveSale 'K05_B2' 'K05_L2' 2
Assert ((Execute "SELECT GIA_VON FROM CHI_TIET_PHIEU_BAN WHERE ID_PHIEU_BAN='K05_B2'" $true) -eq 300) 'Lot cost not applied at save'
Assert ((Execute "SELECT GIA_VON FROM CHI_TIET_PHIEU_BAN WHERE ID_PHIEU_BAN='K05_B1'" $true) -eq 200) 'Changing method rewrote old sale cost'
Assert ((Execute "SELECT DON_GIA FROM CHI_TIET_PHIEU_BAN WHERE ID_PHIEU_BAN='K05_B2'" $true) -eq 700) 'Cost method changed sale price'
$failed=$false;try{[CuahangNongduoc.WarehouseSettings]::Save($manual,'INVALID')}catch{$failed=$true}
Assert $failed 'Invalid cost method accepted'
 $repo.CreateAccount('k05_seller','Cost test seller','CostTest!12345','BANHANG')
 [CuahangNongduoc.AppSession]::CurrentUser=$repo.FindByUsername('k05_seller')
 $failed=$false;try{[CuahangNongduoc.WarehouseCost]::Preview('K05_L2')}catch{$failed=$true}
 Assert $failed 'Seller can read cost without permission'
 SaveSale 'K05_B3' 'K05_L2' 1
 Assert ((Execute "SELECT GIA_VON FROM CHI_TIET_PHIEU_BAN WHERE ID_PHIEU_BAN='K05_B3'" $true) -eq 300) 'Server cost calculation failed for seller without cost visibility'
 [CuahangNongduoc.AppSession]::CurrentUser=$repo.FindByUsername('admin')
$form=New-Object CuahangNongduoc.frmCauHinhKho
try{Assert ($form.Controls.Count -eq 10) 'Cost settings form failed'}finally{$form.Dispose()}
Write-Output 'PASS: cost configuration persistence, moving average after receipts and sale, actual exported-lot cost, saved costs, unchanged sale prices, invalid-mode rejection and settings form.'
