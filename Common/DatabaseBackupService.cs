using System;
using System.IO;

namespace CuahangNongduoc
{
    public static class DatabaseBackupService
    {
        public static void DemandManage()
        {
            AccessControl.DemandCurrent("QuanTri");
            if(!AccountRoles.IsAdmin(AppSession.CurrentUser.Role)) throw new UnauthorizedAccessException("Chỉ quản lý được sao lưu toàn bộ dữ liệu.");
        }
        public static void Backup(string destinationPath)
        {
            DemandManage();
            CopyDatabase(destinationPath);
            AccessControl.LogChange("SAO_LUU","DATABASE","Sao lưu thủ công thành công.");
        }
        internal static bool BackupDailyIfDue()
        {
            string folder=Path.Combine(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(DataService.DatabasePath)),"Backups"),"Database");
            string destination=Path.Combine(folder,"cuahang_tu_dong_"+DateTime.Today.ToString("yyyyMMdd")+".mdb");
            if(File.Exists(destination)) return false;
            CopyDatabase(destination);
            return true;
        }
        internal static void TryDailyBackup()
        {
            try { BackupDailyIfDue(); }
            catch(Exception error) { ErrorLog.Record(error,"Sao lưu tự động chưa hoàn tất; sẽ thử lại khi ứng dụng đang chạy."); }
        }
        private static void CopyDatabase(string destinationPath)
        {
            if(String.IsNullOrEmpty(destinationPath)) throw new ArgumentException("Chưa chọn nơi lưu bản sao.");
            string source=Path.GetFullPath(DataService.DatabasePath),destination=Path.GetFullPath(destinationPath);
            if(String.Equals(source,destination,StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Không được ghi bản sao đè lên cơ sở dữ liệu đang dùng.");
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            string temporary=destination+"."+Guid.NewGuid().ToString("N")+".tmp";
            DataService shared=new DataService(); shared.CloseConnection();
            System.Data.OleDb.OleDbConnection.ReleaseObjectPool();
            try {
                // Deny writers while copying; a busy database is retried, never copied halfway through a write.
                using(FileStream input=new FileStream(source,FileMode.Open,FileAccess.Read,FileShare.Read))
                using(FileStream output=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {
                    byte[] buffer=new byte[65536];int count;
                    while((count=input.Read(buffer,0,buffer.Length))>0) output.Write(buffer,0,count);
                    output.Flush();
                }
                string legacy=Path.Combine(Path.GetDirectoryName(source),"phihoadon.xml");
                string relocated=Path.Combine(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(source)),"Config"),"phihoadon.xml");
                if(File.Exists(relocated)) legacy=relocated;
                if(File.Exists(legacy)) File.Copy(legacy,destination+".phihoadon.xml",true);
                if(File.Exists(destination)) File.Replace(temporary,destination,null); else File.Move(temporary,destination);
            } finally {
                if(File.Exists(temporary)) File.Delete(temporary);
            }
        }
    }
}
