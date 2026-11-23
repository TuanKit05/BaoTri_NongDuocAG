using System;
using System.IO;

namespace CuahangNongduoc
{
    public static class DatabaseBackupService
    {
        public static void Backup(string destinationPath)
        {
            if (String.IsNullOrEmpty(destinationPath))
                throw new ArgumentException("Ch?a ch?n n?i l?u b?n sao l?u.", "destinationPath");

            string sourcePath = DataService.DatabasePath;
            string destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!Directory.Exists(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            DataService connection = new DataService();
            try
            {
                connection.CloseConnection();
                File.Copy(sourcePath, destinationPath, true);
            }
            finally
            {
                DataService.OpenConnection();
            }
        }
    }
}
