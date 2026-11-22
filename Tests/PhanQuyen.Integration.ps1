param([string]$ProjectRoot = 'D:\NongDuoc_BaoTri')
$ErrorActionPreference = 'Stop'
function Assert($condition, $message) { if (!$condition) { throw $message } }
function Denied([scriptblock]$action, $message) { $blocked=$false; try { & $action } catch { $blocked=$true }; Assert $blocked $message }
[void][Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\Debug\CuahangNongduoc.exe'))
[Runtime.CompilerServices.RuntimeHelpers]::RunClassConstructor([CuahangNongduoc.DataService].TypeHandle)
$folder = Join-Path $env:TEMP ('NongDuocAuth-' + [Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $folder)
$db=Join-Path $folder 'cuahang.mdb'
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'Database\cuahang.dll') -Destination $db
[CuahangNongduoc.DataService]::m_ConnectString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$db"
$c = New-Object System.Data.OleDb.OleDbConnection([CuahangNongduoc.DataService]::m_ConnectString)
$c.Open()
function Query([string]$sql) { $cmd=$c.CreateCommand();$cmd.CommandText=$sql;$dt=New-Object System.Data.DataTable;$da=New-Object System.Data.OleDb.OleDbDataAdapter($cmd);[void]$da.Fill($dt); return ,$dt }
function Exec([string]$sql) { $cmd=$c.CreateCommand();$cmd.CommandText=$sql;[void]$cmd.ExecuteNonQuery() }
$before=Query 'SELECT ID,TEN_DANG_NHAP,MAT_KHAU FROM TAI_KHOAN'
$repo=New-Object CuahangNongduoc.AccountRepository
$repo.EnsureSchema()
$repo.EnsureSchema()
Assert ($repo.GetAccountCount() -eq $before.Rows.Count) 'Startup created accounts'
Assert ($repo.GetRoles().Rows.Count -eq 4) 'Role catalog does not have four roles'
$admin=$repo.FindByUsername('admin')
Assert ($admin.Permissions.Contains('QuanLyTaiKhoan')) 'Existing owner not migrated'
[CuahangNongduoc.AppSession]::CurrentUser=$admin
$auth=New-Object CuahangNongduoc.AuthService($repo)
$legacy=$before.Select("TEN_DANG_NHAP='admin'")
if($legacy.Count -gt 0 -and ![string]::IsNullOrEmpty([string]$legacy[0]['MAT_KHAU'])) { Assert ($auth.Login('admin',[string]$legacy[0]['MAT_KHAU']).Success) 'Existing password migration failed' }
Assert (!$auth.Login('admin','definitely-wrong-password').Success) 'Wrong password accepted'
foreach($spec in @(@('pq_sales','BANHANG'),@('pq_stock','KHO'),@('pq_accounts','KETOAN'))) {
 $repo.CreateAccount($spec[0],'Authorization test','TestPassword123',$spec[1])
}
$sales=$repo.FindByUsername('pq_sales');$stock=$repo.FindByUsername('pq_stock');$accounts=$repo.FindByUsername('pq_accounts')
[CuahangNongduoc.AppSession]::CurrentUser=$sales
Assert ([CuahangNongduoc.AccessControl]::Can('BanHang')) 'Sales cannot sell'
Assert (![CuahangNongduoc.AccessControl]::Can('ThuChi')) 'Sales can access finance'
Assert (![CuahangNongduoc.AccessControl]::Can('XemGiaNhap')) 'Sales can see costs'
Denied { $repo.GetAllAccounts() } 'Sales can enumerate accounts'
Denied { $repo.CreateAccount('bad','bad','TestPassword123','ADMIN') } 'Sales can create accounts'
Denied { $repo.UpdateRole($sales.Id,'ADMIN') } 'Sales can elevate role'
Denied { [CuahangNongduoc.AccessControl]::DemandWrite('UPDATE SAN_PHAM SET GIA_BAN_LE=1') } 'Sales can change catalog prices'
Denied { [CuahangNongduoc.AccessControl]::DemandWrite('DELETE FROM PHIEU_BAN WHERE ID=1') } 'Sales can delete invoices'
$customer=(Query 'SELECT ID FROM KHACH_HANG').Rows[0][0]
$own='PQA'+[Guid]::NewGuid().ToString('N').Substring(0,5)
$other='PQB'+[Guid]::NewGuid().ToString('N').Substring(0,5)
Exec ("INSERT INTO PHIEU_BAN (ID,ID_KHACH_HANG,NGAY_BAN,TONG_TIEN,DA_TRA,CON_NO,ID_NHAN_VIEN) VALUES ('$own','$customer',Date(),100,0,100,'"+$sales.Id+"')")
Exec ("INSERT INTO PHIEU_BAN (ID,ID_KHACH_HANG,NGAY_BAN,TONG_TIEN,DA_TRA,CON_NO,ID_NHAN_VIEN) VALUES ('$other','$customer',Date(),200,0,200,'"+$accounts.Id+"')")
$ds=New-Object CuahangNongduoc.DataService
$ds.Load((New-Object System.Data.OleDb.OleDbCommand('SELECT * FROM PHIEU_BAN')))
Assert ($ds.Rows.Count -eq 1 -and $ds.Rows[0]['ID'] -eq $own) 'Sales invoice ownership filter failed'
$ds.Load((New-Object System.Data.OleDb.OleDbCommand('SELECT SUM(TONG_TIEN) AS Total FROM PHIEU_BAN WHERE TONG_TIEN>0')))
Assert ($ds.Rows[0]['Total'] -eq 100) 'Scoped aggregate leaked other invoices'
$ds.Load((New-Object System.Data.OleDb.OleDbCommand('SELECT PB.* FROM PHIEU_BAN PB INNER JOIN KHACH_HANG KH ON PB.ID_KHACH_HANG=KH.ID WHERE KH.LOAI_KH=False ORDER BY PB.ID')))
foreach($r in $ds.Rows){Assert ($r['ID_NHAN_VIEN'] -eq $sales.Id) 'Join ownership filter failed'}
$ds.Load((New-Object System.Data.OleDb.OleDbCommand('SELECT * FROM SAN_PHAM')))
foreach($r in $ds.Rows){Assert ([decimal]$r['DON_GIA_NHAP'] -eq 0) 'Cost leaked in product data'}
# Exercise the complete sale transaction using a copied database only.
$lot=[string](Query 'SELECT ID FROM MA_SAN_PHAM').Rows[0][0]
Exec ("UPDATE MA_SAN_PHAM SET SO_LUONG=100, NGAY_HET_HAN=#12/31/2099# WHERE ID='"+$lot.Replace("'","''")+"'")
# Let Jet refresh cached pages written by the independent fixture connection.
Start-Sleep -Milliseconds 5100
$ctrl=New-Object CuahangNongduoc.Controller.PhieuBanController
$detail=New-Object CuahangNongduoc.Controller.ChiTietPhieuBanController
$grid=New-Object System.Windows.Forms.DataGridView
$detail.HienThiChiTiet($grid,'PQ_NEW')
$line=$detail.NewRow(); $line['ID_PHIEU_BAN']='PQ_NEW';$line['ID_MA_SAN_PHAM']=$lot;$line['DON_GIA']=100;$line['SO_LUONG']=2;$line['THANH_TIEN']=200;$detail.Add($line)
$header=$ctrl.NewRow();$header['ID']='PQ_NEW';$header['ID_KHACH_HANG']=$customer;$header['NGAY_BAN']=[DateTime]::Today;$header['TONG_TIEN']=200;$header['DA_TRA']=0;$header['CON_NO']=200;$ctrl.Add($header)
try { $ctrl.LuuPhieuVaChiTiet($detail) } catch {
 Write-Output ('Lot fixture: '+$lot)
 Query ("SELECT ID,SO_LUONG,NGAY_HET_HAN FROM MA_SAN_PHAM WHERE ID='"+$lot.Replace("'","''")+"'") | Format-Table -AutoSize | Out-String | Write-Output
 throw
}
Assert ((Query "SELECT ID_NHAN_VIEN FROM PHIEU_BAN WHERE ID='PQ_NEW'").Rows[0][0] -eq $sales.Id) 'Seller identity not saved'
# The assertion connection is independent of the transaction writer; Jet caches its pages.
$deadline=[DateTime]::UtcNow.AddSeconds(8)
do {
 $remaining=(Query ("SELECT SO_LUONG FROM MA_SAN_PHAM WHERE ID='"+$lot.Replace("'","''")+"'")).Rows[0][0]
 if($remaining -eq 98){break}
 Start-Sleep -Milliseconds 250
} while([DateTime]::UtcNow -lt $deadline)
Assert ($remaining -eq 98) 'Stock not deducted exactly once' 
Denied { $ctrl.XoaPhieuBan('PQ_NEW') } 'Sales can delete through controller'
$counter=[CuahangNongduoc.ThamSo]::LayMaPhieuBan()
[CuahangNongduoc.ThamSo]::GanMaPhieuBan($counter+1)
Assert ([CuahangNongduoc.ThamSo]::LayMaPhieuBan() -eq $counter+1) 'Sale counter blocked by table permission detection'
$grid.Dispose()
[CuahangNongduoc.AppSession]::CurrentUser=$stock
Assert ([CuahangNongduoc.AccessControl]::Can('NhapHang')) 'Stock cannot import'
Assert ([CuahangNongduoc.AccessControl]::Can('XemGiaNhap')) 'Stock cannot see purchase costs'
Denied { [CuahangNongduoc.AccessControl]::DemandWrite('UPDATE SAN_PHAM SET GIA_BAN_LE=1') } 'Stock can edit sales prices'
Denied { [CuahangNongduoc.AccessControl]::DemandCurrent('XoaPhieu') } 'Stock can delete receipts'
[CuahangNongduoc.AppSession]::CurrentUser=$accounts
Assert ([CuahangNongduoc.AccessControl]::Can('ThuChi')) 'Accountant cannot access finance'
Assert ([CuahangNongduoc.AccessControl]::Can('XemGiaVon')) 'Accountant cannot see costs'
Assert (![CuahangNongduoc.AccessControl]::Can('BanHang')) 'Accountant can create sales'
$ds.Load((New-Object System.Data.OleDb.OleDbCommand('SELECT * FROM PHIEU_BAN')))
Assert ($ds.Rows.Count -ge 2) 'Accountant cannot see invoice reports'
Denied { [CuahangNongduoc.AccessControl]::DemandCurrent('XoaPhieu') } 'Accountant can delete invoices'
$payment=New-Object CuahangNongduoc.DataLayer.PhieuThanhToanFactory
[void]$payment.DanhsachPhieuThanhToan()
$payrow=$payment.NewRow();$payrow['ID']='PQ_PAY';$payrow['ID_KHACH_HANG']=$customer;$payrow['NGAY_THANH_TOAN']=[DateTime]::Today;$payrow['TONG_TIEN']=50;$payrow['GHI_CHU']='Permission test';$payment.Add($payrow)
Assert ($payment.Save()) 'Accountant cannot save receipt'
$payrow.Delete()
Denied { $payment.Save() } 'Accountant can delete receipt'
[CuahangNongduoc.AppSession]::CurrentUser=$admin
Denied { $repo.UpdateRole($sales.Id,'KHO,KETOAN') } 'Multiple roles accepted on update'
Denied { $repo.CreateAccount('pq_multi','Test','TestPassword123','KHO,KETOAN') } 'Multiple roles accepted on create'
Denied { Exec ("INSERT INTO TAI_KHOAN_VAI_TRO (ID_TAI_KHOAN,MaVaiTro) VALUES ('"+$sales.Id+"','KHO')") } 'Database accepted a second role'
foreach($invalidRole in @('ADMIN,KHO','ADMIN;BANHANG','ADMIN,ADMIN','')) {
 Assert (![CuahangNongduoc.AccountRoles]::IsValid($invalidRole)) ('Invalid role accepted: '+$invalidRole)
 Assert (![CuahangNongduoc.AccountRoles]::IsAdmin($invalidRole)) ('Invalid role granted admin: '+$invalidRole)
}
$repo.UpdateRole($sales.Id,'KETOAN')
Assert ($repo.GetById($sales.Id).Permissions.Contains('ThuChi')) 'Role update not reflected in grants'
$repo.SetActive($sales.Id,$false)
[CuahangNongduoc.AppSession]::CurrentUser=$sales
Denied { [CuahangNongduoc.AccessControl]::DemandCurrent('BanHang') } 'Locked stale session still writes'
Assert (!$auth.Login('pq_sales','TestPassword123').Success) 'Locked account can login'
[CuahangNongduoc.AppSession]::CurrentUser=$admin
$form=New-Object CuahangNongduoc.frmQuanLyTaiKhoan($repo)
$flags=[Reflection.BindingFlags]'Instance,NonPublic'
$combo=$form.GetType().GetField('cboRole',$flags).GetValue($form)
Assert ($combo -is [System.Windows.Forms.ComboBox]) 'Role picker is not a ComboBox'
Assert ($combo.Items.Count -eq 4) 'Role picker does not contain four roles'
foreach($item in $combo.Items){ Assert (!$combo.GetItemText($item).Contains('System.Data.')) 'Raw DataRowView displayed in role picker' }
foreach($code in @('ADMIN','KHO','BANHANG','KETOAN')) {
 $combo.SelectedValue=$code
 $selected=$form.GetType().GetMethod('SelectedRole',$flags).Invoke($form,@())
 Assert ($selected -eq $code) ('Cannot select role '+$code)
}
Assert ($null -eq $form.GetType().GetField('btnMultiRole',$flags)) 'Multiple-role button still exists'
$form.Dispose()
$audit=Query 'SELECT * FROM NHAT_KY'
Assert ($audit.Rows.Count -gt 0) 'Missing audit trail'
$clear=Query 'SELECT MAT_KHAU FROM TAI_KHOAN WHERE MAT_KHAU IS NOT NULL'
Assert ($clear.Rows.Count -eq 0) 'Plaintext credentials remain'
$c.Close()
[System.Data.OleDb.OleDbConnection]::ReleaseObjectPool()
Write-Output 'PASS: existing-account migration, no automatic accounts, four database roles, single-role enforcement, wrong passwords, account management guards, sales ownership and aggregates, hidden costs, stock restrictions, accountant finance, lock/revocation, form binding, audit, password hashing.'
Write-Output ('Test database: '+$db)
