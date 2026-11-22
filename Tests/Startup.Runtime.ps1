param([string]$ProjectRoot='D:\NongDuoc_BaoTri')
$ErrorActionPreference='Stop'
$output=Join-Path $ProjectRoot 'obj\StartupRuntime'
[void](New-Item -ItemType Directory -Force -Path $output)
$source=@'
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using CuahangNongduoc;
class StartupRuntimeProbe
{
    static string assemblyPath;
    [STAThread] static int Main(string[] args)
    {
        assemblyPath = args[0];
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs e) {
            string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(assemblyPath), new AssemblyName(e.Name).Name + ".dll");
            if (new AssemblyName(e.Name).Name == "CuahangNongduoc") path = assemblyPath;
            return System.IO.File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
        try { Run(); Console.WriteLine("PASS: CLR " + Environment.Version + ", database path, Access connection and main form resources."); return 0; }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
    [MethodImpl(MethodImplOptions.NoInlining)] static void Run()
    {
        if (Environment.Version.Major != 4) throw new Exception("Incorrect startup runtime");
        string expected = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(assemblyPath), @"..\..\Database\cuahang.dll"));
        if (!String.Equals(DataService.DatabasePath, expected, StringComparison.OrdinalIgnoreCase)) throw new Exception("Incorrect database path: " + DataService.DatabasePath);
        using (System.Data.OleDb.OleDbConnection c = new System.Data.OleDb.OleDbConnection(DataService.m_ConnectString)) { c.Open(); }
        AccountRepository repo = new AccountRepository();
        AppSession.CurrentUser = repo.FindByUsername("admin");
        using (Form form = new frmMain()) { if (form.Controls.Count == 0) throw new Exception("Main form did not initialize"); }
        // Force every embedded resource value to deserialize under the actual configured CLR.
        Assembly app = typeof(frmMain).Assembly;
        foreach (string name in app.GetManifestResourceNames())
            if (name.EndsWith(".resources"))
                using (System.IO.Stream stream = app.GetManifestResourceStream(name))
                using (System.Resources.ResourceReader reader = new System.Resources.ResourceReader(stream))
                    foreach (System.Collections.DictionaryEntry item in reader) { object value = item.Value; }
    }
}
'@
$sourcePath=Join-Path $output 'StartupRuntimeProbe.cs'
$exe=Join-Path $output 'StartupRuntimeProbe.exe'
[IO.File]::WriteAllText($sourcePath,$source)
$app=Join-Path $ProjectRoot 'bin\Debug\CuahangNongduoc.exe'
& 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe' /nologo /noconfig /nostdlib /target:exe /platform:x86 /reference:C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll /reference:C:\Windows\Microsoft.NET\Framework\v2.0.50727\System.dll "/out:$exe" "/reference:$app" /reference:C:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Windows.Forms.dll /reference:C:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Drawing.dll /reference:C:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Data.dll /reference:C:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Xml.dll $sourcePath
if ($LASTEXITCODE -ne 0) { throw 'Startup probe compilation failed' }
Copy-Item -LiteralPath ($app+'.config') -Destination ($exe+'.config')
& $exe $app
if ($LASTEXITCODE -ne 0) { throw 'Native startup probe failed' }
