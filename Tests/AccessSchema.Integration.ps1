param([string]$ProjectRoot='D:\NongDuoc_BaoTri')
$ErrorActionPreference='Stop'
$folder=Join-Path $ProjectRoot ('obj\AccessSchema-'+[Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $folder)
$db=Join-Path $folder 'cuahang.mdb'
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'Database\cuahang.dll') -Destination $db
$c=New-Object Data.OleDb.OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$db")
$c.Open()
try {
 foreach($name in @('TONG_TIEN','DA_TRA','CON_NO')){
  $column=$c.GetOleDbSchemaTable([Data.OleDb.OleDbSchemaGuid]::Columns,@($null,$null,'PHIEU_BAN',$name))
  if([int]$column.Rows[0].DATA_TYPE -ne 6){throw "Not Currency: PHIEU_BAN.$name"}
 }
 $cmd=$c.CreateCommand();$cmd.CommandText='SELECT TOP 1 ID FROM KHACH_HANG';$customer=$cmd.ExecuteScalar()
 $tx=$c.BeginTransaction()
 try {
  $cmd=$c.CreateCommand();$cmd.Transaction=$tx;$cmd.CommandText='INSERT INTO PHIEU_BAN (ID,ID_KHACH_HANG,NGAY_BAN,TONG_TIEN,DA_TRA,CON_NO) VALUES (?,?,Now(),?,?,?)'
  [void]$cmd.Parameters.AddWithValue('id','CSDL_TEST_3B');[void]$cmd.Parameters.AddWithValue('kh',$customer)
  [void]$cmd.Parameters.Add('total',[Data.OleDb.OleDbType]::Currency);$cmd.Parameters[2].Value=3000000000d
  [void]$cmd.Parameters.Add('paid',[Data.OleDb.OleDbType]::Currency);$cmd.Parameters[3].Value=0d
  [void]$cmd.Parameters.Add('debt',[Data.OleDb.OleDbType]::Currency);$cmd.Parameters[4].Value=3000000000d
  [void]$cmd.ExecuteNonQuery()
  $cmd=$c.CreateCommand();$cmd.Transaction=$tx;$cmd.CommandText="SELECT TONG_TIEN FROM PHIEU_BAN WHERE ID='CSDL_TEST_3B'"
  if($cmd.ExecuteScalar() -ne 3000000000d){throw 'Currency amount above Int32 did not round-trip'}
 }finally{$tx.Rollback();$tx.Dispose()}
 $keys=$c.GetOleDbSchemaTable([Data.OleDb.OleDbSchemaGuid]::Primary_Keys,@($null,$null,'CHI_TIET_PHIEU_BAN'))
 if($keys.Rows.Count -ne 3){throw 'Paid/gift composite key missing'}
 $tx=$c.BeginTransaction()
 try {
  $cmd=$c.CreateCommand();$cmd.Transaction=$tx;$cmd.CommandText="INSERT INTO PHIEU_BAN (ID,ID_KHACH_HANG,NGAY_BAN,TONG_TIEN,DA_TRA,CON_NO) VALUES ('CSDL_BAD_FK','INVALID_CUSTOMER',Now(),1,0,1)"
  $blocked=$false;try{[void]$cmd.ExecuteNonQuery()}catch{$blocked=$true}
  if(!$blocked){throw 'Customer foreign key did not reject orphan'}
 }finally{$tx.Rollback();$tx.Dispose()}
}finally{$c.Dispose()}
& (Join-Path $ProjectRoot 'Database\Bo_sung_Access_theo_yeu_cau.ps1') -DatabasePath $db -ProjectRoot $ProjectRoot
Write-Output 'PASS: Currency 3 billion round-trip, customer FK, paid/gift primary key, repeated migration and preservation checks.'
