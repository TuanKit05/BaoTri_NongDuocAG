param([string]$DatabasePath='', [string]$ProjectRoot='D:\NongDuoc_BaoTri')
$ErrorActionPreference='Stop'
if([IntPtr]::Size -ne 4){throw 'Run this script with C:\Windows\SysWOW64\WindowsPowerShell\v1.0\powershell.exe (32-bit Jet).'}
if(!$DatabasePath){$DatabasePath=Join-Path $ProjectRoot 'Database\cuahang.dll'}
$DatabasePath=[IO.Path]::GetFullPath($DatabasePath)
$stamp=[DateTime]::Now.ToString('yyyyMMdd_HHmmss_fff')
$backupDir=Join-Path (Split-Path -Parent $DatabasePath) 'Backups'
[void](New-Item -ItemType Directory -Force -Path $backupDir)
$backup=Join-Path $backupDir ('cuahang_truoc_bo_sung_'+$stamp+'.mdb')
$stage=Join-Path $backupDir ('cuahang_dang_kiem_tra_'+$stamp+'.mdb')
# Snapshot under an exclusive file handle; never copy an actively edited Access file.
$stream=[IO.File]::Open($DatabasePath,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::None)
try{$buffer=New-Object IO.MemoryStream;$stream.CopyTo($buffer);$bytes=$buffer.ToArray();[IO.File]::WriteAllBytes($backup,$bytes);[IO.File]::WriteAllBytes($stage,$bytes)}finally{$stream.Dispose()}
$c=New-Object Data.OleDb.OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$stage;OLE DB Services=-2")
$changes=New-Object 'System.Collections.Generic.List[string]'
function Query($sql){$cmd=$c.CreateCommand();$cmd.CommandText=$sql;$adapter=New-Object Data.OleDb.OleDbDataAdapter($cmd);$t=New-Object Data.DataTable;[void]$adapter.Fill($t);return ,$t}
function Scalar($sql){$cmd=$c.CreateCommand();$cmd.CommandText=$sql;return $cmd.ExecuteScalar()}
function Exec($sql){$cmd=$c.CreateCommand();$cmd.CommandText=$sql;[void]$cmd.ExecuteNonQuery();$changes.Add($sql)}
function HasTable($name){return @($c.GetSchema('Tables').Rows | Where-Object {$_.TABLE_NAME -eq $name -and $_.TABLE_TYPE -eq 'TABLE'}).Count -gt 0}
function Columns([string]$name){try{return ,$c.GetOleDbSchemaTable([Data.OleDb.OleDbSchemaGuid]::Columns,[object[]]@($null,$null,$name,$null))}catch{throw ('Cannot inspect table ['+$name+']: '+$_.Exception.Message)}}
function AddColumn($table,$column,$type){if(@((Columns $table).Rows | Where-Object COLUMN_NAME -eq $column).Count -eq 0){Exec "ALTER TABLE [$table] ADD COLUMN [$column] $type"}}
function AddTable($name,$fields){if(!(HasTable $name)){Exec "CREATE TABLE [$name] ($fields)"}}
function Snapshot {
 $result=@{}
 foreach($table in @('SAN_PHAM','MA_SAN_PHAM','KHACH_HANG','NHA_CUNG_CAP','PHIEU_NHAP','PHIEU_BAN','CHI_TIET_PHIEU_BAN','PHIEU_THANH_TOAN','PHIEU_CHI','DU_NO_KH')){
  $rows=Query "SELECT * FROM [$table]";$sums=@{}
  foreach($col in $rows.Columns){if($col.ColumnName -match '^(TONG_TIEN|DA_TRA|CON_NO|DAU_KY|CUOI_KY|PHAT_SINH|SO_LUONG|DON_GIA|DON_GIA_NHAP|TIEN_.*)$'){$sum=0d;foreach($row in $rows.Rows){if($row[$col.ColumnName] -ne [DBNull]::Value){$sum+=[decimal]$row[$col.ColumnName]}};$sums[$col.ColumnName]=$sum}}
  $ids=if($rows.Columns.Contains('ID')){(@($rows.Rows | ForEach-Object { [string]$_['ID'] } | Sort-Object) -join '|')}else{''}
  $result[$table]=@{Rows=$rows.Rows.Count;Sums=$sums;IDs=$ids}
 }
 return $result
}
$c.Open()
try {
 $before=Snapshot
 AddTable 'CAU_HINH_KHO' '[ID] COUNTER PRIMARY KEY, [PHAM_VI] TEXT(20) NOT NULL, [PHUONG_PHAP_XUAT] TEXT(20) NOT NULL, [NGAY_HIEU_LUC] DATETIME, [NGUOI_DOI] TEXT(50), CONSTRAINT UQ_CAU_HINH_KHO UNIQUE ([PHAM_VI])'
 AddColumn 'CAU_HINH_KHO' 'PHUONG_PHAP_GIA' 'TEXT(20)'
 AddColumn 'CAU_HINH_KHO' 'NGUONG_HET_HAN' 'LONG'
 if((Scalar "SELECT COUNT(*) FROM CAU_HINH_KHO WHERE PHAM_VI='CUA_HANG'") -eq 0){Exec "INSERT INTO CAU_HINH_KHO (PHAM_VI,PHUONG_PHAP_XUAT,PHUONG_PHAP_GIA,NGUONG_HET_HAN,NGAY_HIEU_LUC,NGUOI_DOI) VALUES ('CUA_HANG','CHI_DINH_LO','BQGQ',60,Now(),'SYSTEM')"}
 Exec "UPDATE CAU_HINH_KHO SET PHUONG_PHAP_GIA='BQGQ' WHERE PHUONG_PHAP_GIA IS NULL"
 Exec 'UPDATE CAU_HINH_KHO SET NGUONG_HET_HAN=60 WHERE NGUONG_HET_HAN IS NULL'
 AddColumn 'MA_SAN_PHAM' 'SO_LUONG_NHAP' 'LONG'
 AddColumn 'CHI_TIET_PHIEU_BAN' 'GIA_VON' 'CURRENCY'
 AddColumn 'CHI_TIET_PHIEU_BAN' 'PHUONG_PHAP_GIA' 'TEXT(20)'
 AddColumn 'CHI_TIET_PHIEU_BAN' 'GIA_VON_DON_VI' 'CURRENCY'
 AddColumn 'CHI_TIET_PHIEU_BAN' 'THANH_TIEN_GIA_VON' 'CURRENCY'
 AddColumn 'CHI_TIET_PHIEU_BAN' 'GIAM_GIA' 'CURRENCY'
 AddColumn 'CHI_TIET_PHIEU_BAN' 'LA_HANG_TANG' 'YESNO'
 Exec 'UPDATE CHI_TIET_PHIEU_BAN SET LA_HANG_TANG=False WHERE LA_HANG_TANG IS NULL'
 AddColumn 'DU_NO_KH' 'DA_CHOT' 'YESNO'
 Exec 'UPDATE DU_NO_KH SET DA_CHOT=False WHERE DA_CHOT IS NULL'
 foreach($name in @('TIEN_VAN_CHUYEN','TIEN_CHIET_KHAU','TIEN_GIAM_GIA','TIEN_KHUYEN_MAI')){AddColumn 'PHIEU_BAN' $name 'CURRENCY'}
 AddColumn 'PHIEU_BAN' 'ID_NHAN_VIEN' 'TEXT(50)'
 AddColumn 'PHIEU_BAN' 'ID_UU_DAI' 'TEXT(50)'
 AddColumn 'PHIEU_BAN' 'GIAM_GIA_LOAI' 'TEXT(20)'
 AddColumn 'PHIEU_BAN' 'GIAM_GIA_GIA_TRI' 'CURRENCY'
 AddTable 'DICH_VU' '[ID] TEXT(50) PRIMARY KEY,[TEN_DICH_VU] TEXT(255),[DON_GIA] CURRENCY,[DANG_SU_DUNG] YESNO'
 AddTable 'CHI_TIET_DICH_VU' '[ID] COUNTER PRIMARY KEY,[ID_PHIEU_BAN] TEXT(50),[ID_DICH_VU] TEXT(50),[MO_TA] TEXT(255),[SO_LUONG] LONG,[DON_GIA] CURRENCY,[THANH_TIEN] CURRENCY'
 AddTable 'UU_DAI' '[ID] TEXT(50) PRIMARY KEY,[LOAI] TEXT(20),[PHAM_VI] TEXT(20),[MA_SAN_PHAM] TEXT(50),[DIEU_KIEN] MEMO,[GIA_TRI] DOUBLE,[TU_NGAY] DATETIME,[DEN_NGAY] DATETIME,[NGUOI_THIET_LAP] TEXT(50)'
 AddTable 'BIEN_DONG_KHO' '[ID] COUNTER PRIMARY KEY,[NGAY] DATETIME,[MA_LO] TEXT(50),[LOAI] TEXT(20),[SO_LUONG] LONG,[MA_PHIEU] TEXT(50),[ID_TAI_KHOAN] TEXT(50)'
 # Convert money columns only. IDs, stock, percentages, counters and manual lot codes retain their types.
 foreach($table in @($c.GetSchema('Tables').Rows | Where-Object {$_.TABLE_TYPE -eq 'TABLE' -and $_.TABLE_NAME -notlike 'MSys*'} | ForEach-Object TABLE_NAME)){
  foreach($column in (Columns $table).Rows){$name=$column.COLUMN_NAME
   if($name -match '^(TONG_TIEN|DA_TRA|CON_NO|DAU_KY|CUOI_KY|PHAT_SINH|DON_GIA|DON_GIA_NHAP|GIA_BAN_SI|GIA_BAN_LE|THANH_TIEN|THANH_TIEN_GIA_VON|GIA_VON|GIA_VON_DON_VI|GIAM_GIA|GIAM_GIA_GIA_TRI|TIEN_.*|SO_TIEN)$' -and [int]$column.DATA_TYPE -in @(2,3,4,5)){
    Exec "ALTER TABLE [$table] ALTER COLUMN [$name] CURRENCY"
   }
  }
 }
 # A gift and a paid row may share one lot. The companion compatibility changes set False on new paid rows.
 $pk=$c.GetOleDbSchemaTable([Data.OleDb.OleDbSchemaGuid]::Primary_Keys,@($null,$null,'CHI_TIET_PHIEU_BAN'))
 if($pk.Rows.Count -eq 2){Exec ('ALTER TABLE CHI_TIET_PHIEU_BAN DROP CONSTRAINT ['+$pk.Rows[0].PK_NAME+']');Exec 'ALTER TABLE CHI_TIET_PHIEU_BAN ADD CONSTRAINT PK_CHI_TIET_BAN PRIMARY KEY (ID_PHIEU_BAN,ID_MA_SAN_PHAM,LA_HANG_TANG)'}
 foreach($relation in @(
  @('PHIEU_BAN','ID_KHACH_HANG','KHACH_HANG','ID'),@('PHIEU_NHAP','ID_NHA_CUNG_CAP','NHA_CUNG_CAP','ID'),
  @('SAN_PHAM','ID_DON_VI_TINH','DON_VI_TINH','ID'),@('PHIEU_CHI','ID_LY_DO_CHI','LY_DO_CHI','ID'),
  @('UU_DAI','NGUOI_THIET_LAP','TAI_KHOAN','ID'),@('BIEN_DONG_KHO','MA_LO','MA_SAN_PHAM','ID')
 )){
  $child,$key,$parent,$parentKey=$relation
  $foreign=$c.GetOleDbSchemaTable([Data.OleDb.OleDbSchemaGuid]::Foreign_Keys,$null)
  if(@($foreign.Rows | Where-Object {$_.FK_TABLE_NAME -eq $child -and $_.FK_COLUMN_NAME -eq $key -and $_.PK_TABLE_NAME -eq $parent}).Count){continue}
  $orphans=Scalar "SELECT COUNT(*) FROM [$child] C LEFT JOIN [$parent] P ON C.[$key]=P.[$parentKey] WHERE C.[$key] IS NOT NULL AND P.[$parentKey] IS NULL"
  if($orphans -gt 0){$changes.Add("SKIP FK $child.$key -> $parent.$parentKey : $orphans orphan rows; no data deleted");continue}
  Exec "ALTER TABLE [$child] ADD CONSTRAINT [FK_${child}_${key}] FOREIGN KEY ([$key]) REFERENCES [$parent] ([$parentKey])"
 }
 foreach($index in @(
  @('MA_SAN_PHAM','IX_LO_HAN_DUNG','NGAY_HET_HAN,SO_LUONG'),
  @('BIEN_DONG_KHO','IX_BIEN_DONG_LO_NGAY','MA_LO,NGAY'),
  @('PHIEU_BAN','IX_BAN_NV_NGAY','ID_NHAN_VIEN,NGAY_BAN'),
  @('CHI_TIET_DICH_VU','IX_DICH_VU_PHIEU','ID_PHIEU_BAN')
 )){
  $table,$name,$fields=$index
  $indexes=$c.GetOleDbSchemaTable([Data.OleDb.OleDbSchemaGuid]::Indexes,@($null,$null,$null,$null,$table))
  if(@($indexes.Rows | Where-Object INDEX_NAME -eq $name).Count -eq 0){Exec "CREATE INDEX [$name] ON [$table] ($fields)"}
 }
 $multipleRoles=Scalar 'SELECT COUNT(*) FROM (SELECT ID_TAI_KHOAN FROM TAI_KHOAN_VAI_TRO GROUP BY ID_TAI_KHOAN HAVING COUNT(*)>1)'
 if($multipleRoles -gt 0){throw 'Accounts with multiple roles exist. Select one role per account before migration; no roles were removed.'}
 $roleIndexes=$c.GetOleDbSchemaTable([Data.OleDb.OleDbSchemaGuid]::Indexes,@($null,$null,$null,$null,'TAI_KHOAN_VAI_TRO'))
 if(@($roleIndexes.Rows | Where-Object INDEX_NAME -eq 'UX_TAI_KHOAN_MOT_VAI_TRO').Count -eq 0){Exec 'CREATE UNIQUE INDEX UX_TAI_KHOAN_MOT_VAI_TRO ON TAI_KHOAN_VAI_TRO (ID_TAI_KHOAN)'}
 $after=Snapshot
 foreach($table in $before.Keys){
  if($before[$table].Rows -ne $after[$table].Rows -or $before[$table].IDs -ne $after[$table].IDs){throw "Row count or identifiers changed: $table"}
  foreach($column in $before[$table].Sums.Keys){if($before[$table].Sums[$column] -ne $after[$table].Sums[$column]){throw "Financial/stock total changed: $table.$column"}}
 }
 $columnsAfter=@();foreach($table in $after.Keys){foreach($column in (Columns $table).Rows){$columnsAfter+=[PSCustomObject]@{Table=$table;Column=$column.COLUMN_NAME;Type=[int]$column.DATA_TYPE}}}
 $report=@{Backup=$backup;Database=$DatabasePath;Changes=@($changes);Before=$before;After=$after;Columns=$columnsAfter;Validation='Business row counts, IDs, money totals and stock totals preserved'}
}finally{$c.Dispose()}
# Publish only after successful staged validation. If locked, the original remains untouched.
$destination=[IO.File]::Open($DatabasePath,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$sha=[Security.Cryptography.SHA256]::Create()
try{$currentHash=[BitConverter]::ToString($sha.ComputeHash($destination));$originalHash=[BitConverter]::ToString($sha.ComputeHash($bytes))}catch{$destination.Dispose();throw}finally{$sha.Dispose()}
if($currentHash -ne $originalHash){$destination.Dispose();throw 'Original database changed during staging; no changes were published. Close the application and rerun.'}
$destination.Position=0
try{$verified=[IO.File]::ReadAllBytes($stage);$destination.SetLength(0);$destination.Write($verified,0,$verified.Length);$destination.Flush()}catch{ $destination.SetLength(0);$destination.Write($bytes,0,$bytes.Length);$destination.Flush();throw }finally{$destination.Dispose()}
$reportPath=Join-Path $backupDir ('Doi_chieu_CSDL_'+$stamp+'.json')
$report | ConvertTo-Json -Depth 8 | Set-Content -Encoding UTF8 $reportPath
Write-Output ('UPDATED: '+$DatabasePath)
Write-Output ('BACKUP: '+$backup)
Write-Output ('VALIDATION: '+$reportPath)
