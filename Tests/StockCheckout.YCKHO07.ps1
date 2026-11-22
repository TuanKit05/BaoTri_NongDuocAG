param([string]$ProjectRoot='D:\NongDuoc_BaoTri')
$ErrorActionPreference='Stop'
. (Join-Path $ProjectRoot 'Tests\WarehouseCost.YCKHO05.ps1') -ProjectRoot $ProjectRoot
$refresh=New-Object CuahangNongduoc.DataService;$refresh.CloseConnection()
$sale=New-Object CuahangNongduoc.Controller.PhieuBanController
$details=New-Object CuahangNongduoc.Controller.ChiTietPhieuBanController
$grid=New-Object System.Windows.Forms.DataGridView
try {
 $details.HienThiChiTiet($grid,'K07_PB')
 $first=$details.NewRow();$first['ID_PHIEU_BAN']='K07_PB';$first['ID_MA_SAN_PHAM']='K05_L1';$first['SO_LUONG']=2;$first['DON_GIA']=700;$first['THANH_TIEN']=1400;$details.Add($first)
 $second=$details.NewRow();$second['ID_PHIEU_BAN']='K07_PB';$second['ID_MA_SAN_PHAM']='K05_L2';$second['SO_LUONG']=4;$second['DON_GIA']=700;$second['THANH_TIEN']=2800;$details.Add($second)
 $header=$sale.NewRow();$header['ID']='K07_PB';$header['ID_KHACH_HANG']=$customer;$header['NGAY_BAN']=[DateTime]::Today;$header['TONG_TIEN']=4200;$header['DA_TRA']=0;$header['CON_NO']=4200;$sale.Add($header)
 # Another checkout consumes stock after the lines were prepared.
 Execute "UPDATE MA_SAN_PHAM SET SO_LUONG=1 WHERE ID='K05_L2'"
 $message='';try{$sale.LuuPhieuVaChiTiet($details)}catch{$message=$_.Exception.ToString()}
 Assert ($message.Contains('K05_L2') -and $message.Contains('1') -and $message.Contains('4') -and $message.Contains('3')) 'Checkout did not report lot, current stock, requested delta and shortage'
 Assert ((Execute "SELECT COUNT(*) FROM PHIEU_BAN WHERE ID='K07_PB'" $true) -eq 0) 'Shortage committed header/payment'
 Assert ((Execute "SELECT COUNT(*) FROM CHI_TIET_PHIEU_BAN WHERE ID_PHIEU_BAN='K07_PB'" $true) -eq 0) 'Shortage partially committed details'
 Assert ((Execute "SELECT SO_LUONG FROM MA_SAN_PHAM WHERE ID='K05_L1'" $true) -eq 5) 'Shortage partially deducted another lot'
 Assert ($second.RowState -eq [System.Data.DataRowState]::Added) 'Failed save discarded pending rows'
 $second['SO_LUONG']=1;$second['THANH_TIEN']=700;$header['TONG_TIEN']=2100;$header['CON_NO']=2100
 $sale.LuuPhieuVaChiTiet($details)
 Assert ((Execute "SELECT SO_LUONG FROM MA_SAN_PHAM WHERE ID='K05_L1'" $true) -eq 3) 'Corrected save failed'
 Assert ((Execute "SELECT SO_LUONG FROM MA_SAN_PHAM WHERE ID='K05_L2'" $true) -eq 0) 'Exact stock checkout failed'
 # An edit must compare only the extra quantity with current stock.
 $first['SO_LUONG']=7;$first['THANH_TIEN']=4900;$header['TONG_TIEN']=5600;$header['CON_NO']=5600
 $message='';try{$sale.LuuPhieuVaChiTiet($details)}catch{$message=$_.Exception.ToString()}
 Assert ($message.Contains('K05_L1') -and $message.Contains('3') -and $message.Contains('5') -and $message.Contains('2')) 'Edit stock warning counted saved quantity twice'
 Assert ((Execute "SELECT SO_LUONG FROM CHI_TIET_PHIEU_BAN WHERE ID_PHIEU_BAN='K07_PB' AND ID_MA_SAN_PHAM='K05_L1'" $true) -eq 2) 'Failed edit changed stored quantity'
 Assert ((Execute "SELECT TONG_TIEN FROM PHIEU_BAN WHERE ID='K07_PB'" $true) -eq 2100) 'Failed edit changed stored payment total'
 $first['SO_LUONG']=5;$first['THANH_TIEN']=3500;$header['TONG_TIEN']=4200;$header['CON_NO']=4200
 $sale.LuuPhieuVaChiTiet($details)
 Assert ((Execute "SELECT SO_LUONG FROM MA_SAN_PHAM WHERE ID='K05_L1'" $true) -eq 0) 'Exact delta edit rejected'
}finally{$grid.Dispose()}
Write-Output 'PASS YC-KHO-07: fresh checkout stock, detailed shortage warning, no partial header/payment/detail/stock writes, retained input, corrected retry and exact-stock edits.'
