param([string]$ProjectRoot='D:\NongDuoc_BaoTri', [string]$AssemblyPath='')
$ErrorActionPreference='Stop'
function Assert($condition,$message) { if (!$condition) { throw $message } }
if (!$AssemblyPath) { $AssemblyPath=Join-Path $ProjectRoot 'bin\Debug\CuahangNongduoc.exe' }
[void][Reflection.Assembly]::LoadFrom($AssemblyPath)
[Runtime.CompilerServices.RuntimeHelpers]::RunClassConstructor([CuahangNongduoc.DataService].TypeHandle)
$folder=Join-Path (Join-Path $ProjectRoot 'obj') ('StockYC10-'+[Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $folder)
$db=Join-Path $folder 'cuahang.mdb'
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'Database\cuahang.dll') -Destination $db
[CuahangNongduoc.DataService]::m_ConnectString="Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$db;OLE DB Services=-2"
[CuahangNongduoc.ErrorLog]::DirectoryPath=Join-Path $folder 'Logs'
function Execute([string]$sql,[bool]$scalar=$false) {
 $c=New-Object System.Data.OleDb.OleDbConnection([CuahangNongduoc.DataService]::m_ConnectString)
 try { $c.Open();$cmd=$c.CreateCommand();$cmd.CommandText=$sql;if($scalar){return $cmd.ExecuteScalar()};[void]$cmd.ExecuteNonQuery() }
 finally { $c.Dispose() }
}
$repo=New-Object CuahangNongduoc.AccountRepository
$repo.EnsureSchema()
[CuahangNongduoc.AppSession]::CurrentUser=$repo.FindByUsername('admin')
$unit=Execute 'SELECT TOP 1 ID FROM DON_VI_TINH' $true
$vendor=Execute 'SELECT TOP 1 ID FROM NHA_CUNG_CAP' $true
$customer=Execute 'SELECT TOP 1 ID FROM KHACH_HANG' $true
Execute "INSERT INTO SAN_PHAM (ID,TEN_SAN_PHAM,ID_DON_VI_TINH,DON_GIA_NHAP,GIA_BAN_SI,GIA_BAN_LE,SO_LUONG) VALUES ('YC10_SP','YC10 product',$unit,100,120,150,999)"
$factory=New-Object CuahangNongduoc.DataLayer.SanPhamFactory
Assert ($factory.LayTheoId('YC10_SP').Rows[0]['SO_LUONG'] -eq 0) 'Product with no lots used its legacy stock'
Execute "INSERT INTO PHIEU_NHAP (ID,ID_NHA_CUNG_CAP,NGAY_NHAP,TONG_TIEN,DA_TRA,CON_NO) VALUES ('YC10_PN','$vendor',Date(),1100,0,1100)"
Execute "INSERT INTO MA_SAN_PHAM (ID,ID_SAN_PHAM,ID_PHIEU_NHAP,DON_GIA_NHAP,SO_LUONG,NGAY_NHAP,NGAY_SAN_XUAT,NGAY_HET_HAN) VALUES ('YC10_L1','YC10_SP','YC10_PN',100,4,Date(),Date(),#12/31/2099#)"
Execute "INSERT INTO MA_SAN_PHAM (ID,ID_SAN_PHAM,ID_PHIEU_NHAP,DON_GIA_NHAP,SO_LUONG,NGAY_NHAP,NGAY_SAN_XUAT,NGAY_HET_HAN) VALUES ('YC10_L2','YC10_SP','YC10_PN',100,7,Date()-365,Date()-400,Date()-1)"
Assert ($factory.LayTheoId('YC10_SP').Rows[0]['SO_LUONG'] -eq 11) 'Product read did not sum all physical lots'
Assert ($factory.TimMaSanPham('YC10_SP').Rows[0]['SO_LUONG'] -eq 11) 'Code search has a different stock source'
Assert ($factory.TimTenSanPham('YC10 product').Rows[0]['SO_LUONG'] -eq 11) 'Name search has a different stock source'
$lots=New-Object CuahangNongduoc.DataLayer.MaSanPhanFactory
Assert ($lots.LaySanPham('YC10_L1').Rows[0]['SO_LUONG'] -eq 11) 'Product by lot used legacy stock'
Assert ($lots.LayMaSanPham('YC10_L1').Rows[0]['SO_LUONG'] -eq 4) 'Switching to a lot confused lot and product stock'
$report=@([CuahangNongduoc.Controller.SanPhamController]::LaySoLuongTon() | Where-Object { $_.SanPham.Id -eq 'YC10_SP' })
Assert ($report.Count -eq 1 -and $report[0].SoLuong -eq 11) 'Stock report does not match lot totals'
$table=$factory.LayTheoId('YC10_SP')
Assert ($table.Columns['SO_LUONG'].ReadOnly) 'Derived stock can be edited'
$table.Rows[0]['TEN_SAN_PHAM']='YC10 product edited';Assert ($factory.Save()) 'Catalog edit failed'
Assert ((Execute "SELECT SO_LUONG FROM SAN_PHAM WHERE ID='YC10_SP'" $true) -eq 999) 'Catalog save rewrote legacy product stock'
# A sale and its edit/delete change only lot stock; stale SAN_PHAM.SO_LUONG is ignored.
$sale=New-Object CuahangNongduoc.Controller.PhieuBanController
$details=New-Object CuahangNongduoc.Controller.ChiTietPhieuBanController
$grid=New-Object System.Windows.Forms.DataGridView
try {
 $details.HienThiChiTiet($grid,'YC10_PB')
 $line=$details.NewRow();$line['ID_PHIEU_BAN']='YC10_PB';$line['ID_MA_SAN_PHAM']='YC10_L1';$line['SO_LUONG']=2;$line['DON_GIA']=100;$line['THANH_TIEN']=200;$details.Add($line)
 $header=$sale.NewRow();$header['ID']='YC10_PB';$header['ID_KHACH_HANG']=$customer;$header['NGAY_BAN']=[DateTime]::Today;$header['TONG_TIEN']=200;$header['DA_TRA']=0;$header['CON_NO']=200;$sale.Add($header)
 $sale.LuuPhieuVaChiTiet($details)
 Assert ($factory.LayTheoId('YC10_SP').Rows[0]['SO_LUONG'] -eq 9) 'Sale did not change derived stock'
 $line['SO_LUONG']=1;$line['THANH_TIEN']=100;$header['TONG_TIEN']=100;$header['CON_NO']=100
 $sale.LuuPhieuVaChiTiet($details)
 Assert ($factory.LayTheoId('YC10_SP').Rows[0]['SO_LUONG'] -eq 10) 'Sale edit did not change derived stock'
 $sale.XoaPhieuBan('YC10_PB')
 Assert ($factory.LayTheoId('YC10_SP').Rows[0]['SO_LUONG'] -eq 11) 'Sale deletion did not restore derived stock'
} finally { $grid.Dispose() }
$import=New-Object CuahangNongduoc.DataLayer.NhapHangService
$input=New-Object CuahangNongduoc.BusinessObject.LoNhapInput
$input.MaLo='YC10_L3';$input.MaSanPham='YC10_SP';$input.SoLuong=3;$input.DonGiaNhap=100
$input.NgayNhap=[DateTime]::Today;$input.NgaySanXuat=[DateTime]::Today;$input.NgayHetHan=[DateTime]::Today.AddYears(1)
$inputs=New-Object 'System.Collections.Generic.List[CuahangNongduoc.BusinessObject.LoNhapInput]';$inputs.Add($input);$message=''
Assert ($import.LuuPhieuNhap('YC10_PN2',[DateTime]::Today,300,$vendor,0,300,$true,$inputs,[ref]$message)) ('Import failed: '+$message)
Assert ($factory.LayTheoId('YC10_SP').Rows[0]['SO_LUONG'] -eq 14) 'Import did not increase derived stock'
Assert ($import.XoaPhieuNhap('YC10_PN2',[ref]$message)) ('Import deletion failed: '+$message)
Assert ($factory.LayTheoId('YC10_SP').Rows[0]['SO_LUONG'] -eq 11) 'Import deletion did not restore derived stock'
# Removing the obsolete field on the disposable copy proves reads and inserts no longer depend on it.
(New-Object CuahangNongduoc.DataService).CloseConnection()
Execute 'ALTER TABLE SAN_PHAM DROP COLUMN SO_LUONG'
Assert ($factory.LayTheoId('YC10_SP').Rows[0]['SO_LUONG'] -eq 11) 'Product query still requires the legacy stock column'
Assert ($lots.LaySanPham('YC10_L1').Rows[0]['SO_LUONG'] -eq 11) 'Lot lookup still requires the legacy stock column'
$factory.DanhSach() | Out-Null
$row=$factory.NewRow();$row['ID']='YC10_EMPTY';$row['TEN_SAN_PHAM']='YC10 empty';$row['ID_DON_VI_TINH']=$unit;$row['DON_GIA_NHAP']=0;$row['GIA_BAN_SI']=0;$row['GIA_BAN_LE']=0;$factory.Rows.Add($row)
Assert ($factory.Save()) 'Product insert still requires the legacy stock column'
Assert ($factory.LayTheoId('YC10_EMPTY').Rows[0]['SO_LUONG'] -eq 0) 'New product without lots has nonzero stock'
Write-Output 'PASS: stale product stock ignored, zero-lot product, code/name search, product-by-lot, lot quantity, stock report, read-only derived stock, sale/edit/delete, import/delete, no dependency on legacy stock column.'
Write-Output ('Test database: '+$db)
