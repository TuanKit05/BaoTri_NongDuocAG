param([string]$ProjectRoot='D:\NongDuoc_BaoTri')
$ErrorActionPreference='Stop'
. (Join-Path $ProjectRoot 'Tests\WarehouseCost.YCKHO05.ps1') -ProjectRoot $ProjectRoot
Assert ([CuahangNongduoc.WarehouseSettings]::LoadExpiryDays() -eq 60) 'Default expiry threshold is not 60'
Execute "INSERT INTO MA_SAN_PHAM (ID,ID_SAN_PHAM,ID_PHIEU_NHAP,DON_GIA_NHAP,SO_LUONG,NGAY_NHAP,NGAY_SAN_XUAT,NGAY_HET_HAN) VALUES ('K09_TODAY','K05_SP','PN_K05_L1',100,2,Date(),Date(),Date())"
Execute "INSERT INTO MA_SAN_PHAM (ID,ID_SAN_PHAM,ID_PHIEU_NHAP,DON_GIA_NHAP,SO_LUONG,NGAY_NHAP,NGAY_SAN_XUAT,NGAY_HET_HAN) VALUES ('K09_EDGE','K05_SP','PN_K05_L1',100,3,Date(),Date(),Date()+60)"
Execute "INSERT INTO MA_SAN_PHAM (ID,ID_SAN_PHAM,ID_PHIEU_NHAP,DON_GIA_NHAP,SO_LUONG,NGAY_NHAP,NGAY_SAN_XUAT,NGAY_HET_HAN) VALUES ('K09_OUTSIDE','K05_SP','PN_K05_L1',100,4,Date(),Date(),Date()+61)"
Execute "INSERT INTO MA_SAN_PHAM (ID,ID_SAN_PHAM,ID_PHIEU_NHAP,DON_GIA_NHAP,SO_LUONG,NGAY_NHAP,NGAY_SAN_XUAT,NGAY_HET_HAN) VALUES ('K09_EXPIRED','K05_SP','PN_K05_L1',100,5,Date(),Date(),Date()-1)"
Execute "INSERT INTO MA_SAN_PHAM (ID,ID_SAN_PHAM,ID_PHIEU_NHAP,DON_GIA_NHAP,SO_LUONG,NGAY_NHAP,NGAY_SAN_XUAT,NGAY_HET_HAN) VALUES ('K09_EMPTY','K05_SP','PN_K05_L1',100,0,Date(),Date(),Date()+1)"
$rows=[CuahangNongduoc.ExpiryWarning]::Load([DateTime]::Today)
$codes=@($rows.Rows | ForEach-Object { $_['MA_LO'] })
Assert ($codes -contains 'K09_TODAY' -and $codes -contains 'K09_EDGE') 'Boundary dates excluded'
Assert ($codes -notcontains 'K09_OUTSIDE' -and $codes -notcontains 'K09_EXPIRED' -and $codes -notcontains 'K09_EMPTY') 'Outside/expired/empty lots included'
Assert (@($rows.Rows | Where-Object { $_['MA_LO'] -eq 'K09_EDGE' })[0]['SO_NGAY_CON'] -eq 60) 'Days remaining incorrect'
[CuahangNongduoc.WarehouseSettings]::Save($manual,$fifo,0)
$repo.EnsureSchema()
Assert ([CuahangNongduoc.WarehouseSettings]::LoadExpiryDays() -eq 0) 'Saved threshold reset during startup'
$rows=[CuahangNongduoc.ExpiryWarning]::Load([DateTime]::Today)
Assert (@($rows.Rows | Where-Object { $_['MA_LO'] -eq 'K09_TODAY' }).Count -eq 1) 'Zero-day threshold excludes today'
Assert (@($rows.Rows | Where-Object { $_['MA_LO'] -eq 'K09_EDGE' }).Count -eq 0) 'Custom threshold ignored'
$failed=$false;try{[CuahangNongduoc.WarehouseSettings]::Save($manual,$fifo,-1)}catch{$failed=$true};Assert $failed 'Negative threshold accepted'
$owner=New-Object System.Windows.Forms.Form
$warning=$null
try {
 $owner.Show();$warning=[CuahangNongduoc.ExpiryWarning]::ShowIfNeeded($owner)
 Assert ($null -ne $warning -and $warning.Visible) 'Warning window did not open'
 $grid=@($warning.Controls | Where-Object { $_ -is [System.Windows.Forms.DataGridView] })[0]
 Assert ($grid.ReadOnly -and $grid.Columns.Count -eq 5) 'Warning grid missing columns or editable'
}finally{if($null -ne $warning){$warning.Dispose()};$owner.Dispose()}
[CuahangNongduoc.AppSession]::CurrentUser=$repo.FindByUsername('k05_seller')
Assert ([CuahangNongduoc.ExpiryWarning]::Load([DateTime]::Today).Rows.Count -gt 0) 'Seller cannot read expiry warning'
$failed=$false;try{[CuahangNongduoc.WarehouseSettings]::Save($manual,$fifo,30)}catch{$failed=$true};Assert $failed 'Seller changed threshold'
Write-Output 'PASS YC-KHO-09: default/custom thresholds, repeat startup, date boundaries, zero stock/expired exclusion, remaining days, warning UI and permissions.'
