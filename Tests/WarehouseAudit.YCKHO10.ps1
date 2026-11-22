param([string]$ProjectRoot='D:\NongDuoc_BaoTri')
$ErrorActionPreference='Stop'
. (Join-Path $ProjectRoot 'Tests\WarehouseCost.YCKHO05.ps1') -ProjectRoot $ProjectRoot
$admin=[CuahangNongduoc.AppSession]::CurrentUser
$before=[CuahangNongduoc.WarehouseSettings]::History().Rows.Count
$oldDays=[CuahangNongduoc.WarehouseSettings]::LoadExpiryDays()
[CuahangNongduoc.WarehouseSettings]::Save($manual,$average,30)
$history=[CuahangNongduoc.WarehouseSettings]::History()
Assert ($history.Rows.Count -eq $before+1) 'Changed configuration did not create one audit row'
Assert ($history.Rows[0]['ID_TAI_KHOAN'] -eq $admin.Id -and $history.Rows[0]['TEN_DANG_NHAP'] -eq $admin.Username) 'Actor missing in history'
Assert ($history.Rows[0]['THOI_GIAN'] -is [DateTime]) 'Audit timestamp missing'
Assert ($history.Rows[0]['NOI_DUNG'].Contains([CuahangNongduoc.WarehouseSettings]::Describe($manual,$fifo,$oldDays))) 'Old values missing'
Assert ($history.Rows[0]['NOI_DUNG'].Contains([CuahangNongduoc.WarehouseSettings]::Describe($manual,$average,30))) 'New values missing'
[CuahangNongduoc.WarehouseSettings]::Save($manual,$average,30)
Assert ([CuahangNongduoc.WarehouseSettings]::History().Rows.Count -eq $history.Rows.Count) 'No-op save logged a change'
$form=New-Object CuahangNongduoc.frmCauHinhKho
try{Assert ($form.Controls['btnLichSuCauHinh'].Text.Length -gt 0) 'Configuration history button missing'}finally{$form.Dispose()}
# Even a mistakenly delegated configuration permission must not bypass the manager role.
Execute "INSERT INTO VAI_TRO_QUYEN (MaVaiTro,MaQuyen) VALUES ('BANHANG','QuanTri')"
[CuahangNongduoc.AppSession]::CurrentUser=$repo.FindByUsername('k05_seller')
$failed=$false;try{[CuahangNongduoc.WarehouseSettings]::Save($manual,$fifo,15)}catch{$failed=$true}
Assert $failed 'Non-manager changed configuration using delegated permission'
$failed=$false;try{[CuahangNongduoc.WarehouseSettings]::History()}catch{$failed=$true}
Assert $failed 'Non-manager accessed configuration history'
[CuahangNongduoc.AppSession]::CurrentUser=$admin
$repo.CreateAccount('k10_manager','Temporary manager','AuditTest!12345','ADMIN')
$manager=$repo.FindByUsername('k10_manager')
[CuahangNongduoc.AppSession]::CurrentUser=$manager
Execute "UPDATE TAI_KHOAN_VAI_TRO SET MaVaiTro='BANHANG' WHERE ID_TAI_KHOAN='$($manager.Id)'"
$failed=$false;try{[CuahangNongduoc.WarehouseSettings]::Save($manual,$fifo,15)}catch{$failed=$true}
Assert $failed 'Revoked manager retained save access'
[CuahangNongduoc.AppSession]::CurrentUser=$admin
Assert ([CuahangNongduoc.WarehouseSettings]::LoadExpiryDays() -eq 30) 'Denied saves changed settings'
Assert ([CuahangNongduoc.WarehouseSettings]::History().Rows.Count -eq $history.Rows.Count) 'Denied saves created change history'
# A history failure must roll back the configuration in the same transaction.
Execute 'ALTER TABLE NHAT_KY DROP COLUMN NOI_DUNG'
$failed=$false;try{[CuahangNongduoc.WarehouseSettings]::Save($manual,$fifo,15)}catch{$failed=$true}
Assert $failed 'Audit failure was ignored'
Assert ([CuahangNongduoc.WarehouseSettings]::LoadExpiryDays() -eq 30) 'Audit failure partially committed settings'
Assert ([CuahangNongduoc.WarehouseSettings]::LoadCostMethod() -eq $average) 'Audit failure changed cost method'
Write-Output 'PASS YC-KHO-10: manager-only configuration/history, live revocation, actor/time/old-new audit, no-op/denial behavior, history button and transaction rollback on audit failure.'
