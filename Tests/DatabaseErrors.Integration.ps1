param([string]$ProjectRoot='D:\NongDuoc_BaoTri', [string]$AssemblyPath='')
$ErrorActionPreference='Stop'
function Assert($condition,$message) { if (!$condition) { throw $message } }
if (!$AssemblyPath) { $AssemblyPath=Join-Path $ProjectRoot 'bin\Debug\CuahangNongduoc.exe' }
[void][Reflection.Assembly]::LoadFrom($AssemblyPath)
[Runtime.CompilerServices.RuntimeHelpers]::RunClassConstructor([CuahangNongduoc.DataService].TypeHandle)
$folder=Join-Path (Join-Path $ProjectRoot 'obj') ('DatabaseErrors-'+[Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $folder)
$db=Join-Path $folder 'cuahang.mdb'
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'Database\cuahang.dll') -Destination $db
[CuahangNongduoc.DataService]::m_ConnectString="Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$db;OLE DB Services=-2"
[CuahangNongduoc.ErrorLog]::DirectoryPath=Join-Path $folder 'Logs'
$repo=New-Object CuahangNongduoc.AccountRepository
$repo.EnsureSchema()
[CuahangNongduoc.AppSession]::CurrentUser=$repo.FindByUsername('admin')
$ds=New-Object CuahangNongduoc.DataService
function ExpectLogged([scriptblock]$action) {
 $failure=$null
 try { & $action } catch { $failure=$_.Exception; while($failure.InnerException) { $failure=$failure.InnerException } }
 Assert ($null -ne $failure) 'Failure was swallowed'
 Assert ($failure.Data.Contains('ErrorLogId')) 'Original exception has no log reference'
 $path=[string]$failure.Data['ErrorLogPath']
 Assert (Test-Path -LiteralPath $path) 'Log file was not written'
 $log=Get-Content -LiteralPath $path -Raw -Encoding UTF8
 Assert ($log.Contains([string]$failure.Data['ErrorLogId'])) 'Log has no matching error reference'
 Assert ($log.Contains($failure.GetType().FullName)) 'Log has no exception type'
 $before=(Get-Item -LiteralPath $path).Length
 $message=[CuahangNongduoc.ErrorLog]::Describe($failure,'Database test')
 Assert ($message.Contains([string]$failure.Data['ErrorLogId'])) 'User message has no error reference'
 Assert (!$message.Contains('Provider=')) 'User message exposes connection settings'
 Assert ((Get-Item -LiteralPath $path).Length -eq $before) 'Same error was logged twice'
}
ExpectLogged { $ds.Load((New-Object System.Data.OleDb.OleDbCommand('SELECT * FROM YC01_MISSING_TABLE'))) }
ExpectLogged { [void]$ds.ExecuteScalar((New-Object System.Data.OleDb.OleDbCommand('SELECT COUNT(*) FROM YC01_MISSING_TABLE'))) }
ExpectLogged { [void]$ds.ExecuteNoneQuery((New-Object System.Data.OleDb.OleDbCommand('UPDATE YC01_MISSING_TABLE SET ID=1'))) }
# A failed batch must rollback prior rows and leave the pending rows available for retry.
$connection=New-Object System.Data.OleDb.OleDbConnection([CuahangNongduoc.DataService]::m_ConnectString)
try {
 $connection.Open();$command=$connection.CreateCommand()
 $command.CommandText='CREATE TABLE YC01_TEST (ID TEXT(50) PRIMARY KEY, TITLE TEXT(50))'
 [void]$command.ExecuteNonQuery()
 $ds.CloseConnection()
 $ds.Load((New-Object System.Data.OleDb.OleDbCommand('SELECT * FROM YC01_TEST')))
 $first=$ds.NewRow();$first['ID']='A';$first['TITLE']='first';$ds.Rows.Add($first)
 $second=$ds.NewRow();$second['ID']='A';$second['TITLE']='second';$ds.Rows.Add($second)
 ExpectLogged { [void]$ds.ExecuteNoneQuery() }
 Assert ($first.RowState -eq [System.Data.DataRowState]::Added -and $second.RowState -eq [System.Data.DataRowState]::Added) 'Failed save accepted changes'
 Assert ($ds.ExecuteScalar((New-Object System.Data.OleDb.OleDbCommand('SELECT COUNT(*) FROM YC01_TEST'))) -eq 0) 'Failed batch left partial database writes'
 $second['ID']='B'
 Assert ($ds.ExecuteNoneQuery() -eq 2) 'Retry could not persist the corrected rows'
 Assert ($first.RowState -eq [System.Data.DataRowState]::Unchanged) 'Successful commit did not accept changes'
} finally { $connection.Dispose() }
# Force a fresh connection to an invalid path to verify OpenConnection no longer silently returns false.
$ds.CloseConnection()
$field=[CuahangNongduoc.DataService].GetField('m_Connection',[Reflection.BindingFlags]'Static,NonPublic')
$oldConnection=$field.GetValue($null);if($oldConnection){$oldConnection.Dispose()};$field.SetValue($null,$null)
[CuahangNongduoc.DataService]::m_ConnectString="Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$folder\missing\not-found.mdb"
ExpectLogged { [void][CuahangNongduoc.DataService]::OpenConnection() }
$secret=New-Object System.Exception('Password=secret-one;Pwd=secret-two;MatKhauHash=secret-three;Salt=secret-four;')
[void][CuahangNongduoc.ErrorLog]::Record($secret,'Redaction test')
$text=Get-Content -LiteralPath ([string]$secret.Data['ErrorLogPath']) -Raw -Encoding UTF8
foreach($value in @('secret-one','secret-two','secret-three','secret-four')) { Assert (!$text.Contains($value)) 'Credential appeared in error log' }
Write-Output 'PASS: connection/read/scalar/write failures logged, original exceptions retained, error references, deduplication, transaction rollback and retry, credential redaction.'
Write-Output ('Test folder: '+$folder)
