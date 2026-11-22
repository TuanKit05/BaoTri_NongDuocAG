param([string]$ProjectRoot='D:\NongDuoc_BaoTri', [string]$AssemblyPath='', [string]$WorkerDatabase='', [string]$WorkerOutput='')
$ErrorActionPreference='Stop'
function Assert($condition,$message) { if (!$condition) { throw $message } }
if (!$AssemblyPath) { $AssemblyPath=Join-Path $ProjectRoot 'bin\Debug\CuahangNongduoc.exe' }
[void][Reflection.Assembly]::LoadFrom($AssemblyPath)
[Runtime.CompilerServices.RuntimeHelpers]::RunClassConstructor([CuahangNongduoc.DataService].TypeHandle)
if ($WorkerDatabase) {
 [CuahangNongduoc.DataService]::m_ConnectString="Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$WorkerDatabase;OLE DB Services=-2"
 $workerRepo=New-Object CuahangNongduoc.AccountRepository
 [CuahangNongduoc.AppSession]::CurrentUser=$workerRepo.FindByUsername('admin')
 $codes=@();for($i=0;$i -lt 10;$i++){ $codes += [CuahangNongduoc.IdGenerator]::Next('PHIEU_BAN') }
 [IO.File]::WriteAllLines($WorkerOutput,[string[]]$codes)
 exit 0
}
$folder=Join-Path (Join-Path $ProjectRoot 'obj') ('AutoIncrement-'+[Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $folder)
$db=Join-Path $folder 'cuahang.mdb'
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'Database\cuahang.dll') -Destination $db
[CuahangNongduoc.DataService]::m_ConnectString="Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$db;OLE DB Services=-2"
[CuahangNongduoc.ErrorLog]::DirectoryPath=Join-Path $folder 'Logs'
$repo=New-Object CuahangNongduoc.AccountRepository
$repo.EnsureSchema()
[CuahangNongduoc.AppSession]::CurrentUser=$repo.FindByUsername('admin')
foreach($table in @('SAN_PHAM','KHACH_HANG','NHA_CUNG_CAP','PHIEU_BAN','PHIEU_NHAP','PHIEU_CHI','PHIEU_THANH_TOAN','TAI_KHOAN')) {
 $first=[CuahangNongduoc.IdGenerator]::Next($table)
 $second=[CuahangNongduoc.IdGenerator]::Next($table)
 $a=[int]($first -replace '^TK','');$b=[int]($second -replace '^TK','')
 Assert ($b -eq $a+1) ($table+': codes do not increase')
 $repo.EnsureSchema()
 $third=[CuahangNongduoc.IdGenerator]::Next($table)
 Assert ([int]($third -replace '^TK','') -eq $b+1) ($table+': schema initialization reused a reserved code')
}
# AutoNumber tables must receive the real database-generated key after save.
foreach($spec in @(@('DonViTinhFactory','TEN_DON_VI'),@('LyDoChiFactory','LY_DO'))) {
 $factory=New-Object ('CuahangNongduoc.DataLayer.'+$spec[0])
 $table=$factory.DanhSach()
 Assert ($table.Columns['ID'].AutoIncrement) ($spec[0]+': identity metadata missing')
 $row=$table.NewRow();$row[$spec[1]]='Identity integration test';$table.Rows.Add($row)
 Assert ([int]$row['ID'] -lt 0) ($spec[0]+': temporary identity is not negative')
 Assert ($factory.Save()) ($spec[0]+': native identity save failed')
 Assert ([int]$row['ID'] -gt 0) ($spec[0]+': actual Access identity was not returned')
 $saved=[int]$row['ID'];$factory.LayTheoId($saved) | Out-Null
 Assert ($factory.Rows.Count -eq 1 -and $factory.Rows[0][$spec[1]] -eq 'Identity integration test') ($spec[0]+': generated key references the wrong row')
}
# A manually imported collision is skipped without overwriting an existing customer.
$reserved=[int][CuahangNongduoc.IdGenerator]::Next('KHACH_HANG');$collision=$reserved+1
$c=New-Object System.Data.OleDb.OleDbConnection([CuahangNongduoc.DataService]::m_ConnectString)
try {
 $c.Open();$cmd=$c.CreateCommand();$cmd.CommandText="INSERT INTO KHACH_HANG (ID,HO_TEN,LOAI_KH) VALUES ('$collision','Collision test',False)"
 [void]$cmd.ExecuteNonQuery()
} finally { $c.Dispose() }
$next=[int][CuahangNongduoc.IdGenerator]::Next('KHACH_HANG')
Assert ($next -gt $collision) 'Existing imported code was reissued'
$repo.CreateAccount('auto_id_test','Auto ID test','TestPassword123','KHO')
$created=$repo.FindByUsername('auto_id_test')
Assert ($created.Id -match '^TK\d{6,}$') 'New account still uses a random identifier'
# Independent processes reserve numbers against one Access file.
$workers=@();$outputs=@()
for($i=0;$i -lt 3;$i++) {
 $output=Join-Path $folder ("worker-$i.txt");$outputs+=$output
 $arguments=@('-NoProfile','-ExecutionPolicy','Bypass','-File',('"'+$PSCommandPath+'"'),'-AssemblyPath',('"'+$AssemblyPath+'"'),'-WorkerDatabase',('"'+$db+'"'),'-WorkerOutput',('"'+$output+'"'))
 $workers+=Start-Process -FilePath "$env:WINDIR\SysWOW64\WindowsPowerShell\v1.0\powershell.exe" -ArgumentList $arguments -WindowStyle Hidden -PassThru
}
foreach($worker in $workers){ Assert ($worker.WaitForExit(45000)) 'Concurrent allocator did not finish'; Assert ($worker.ExitCode -eq 0) 'Concurrent allocator failed' }
$all=@();foreach($output in $outputs){ $all+=Get-Content -LiteralPath $output }
Assert ($all.Count -eq 30 -and @($all | Select-Object -Unique).Count -eq 30) 'Concurrent allocation returned duplicate IDs'
Write-Output 'PASS: eight shared database counters, increasing reservations, no reuse on restart, imported collision skip, native AutoNumber keys, sequential account codes, concurrent reservations across three processes.'
Write-Output ('Test database: '+$db)
