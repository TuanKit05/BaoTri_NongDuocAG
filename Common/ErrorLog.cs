using System;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CuahangNongduoc
{
    public static class ErrorLog
    {
        private static readonly object Sync = new object();
        public static string DirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        private static Exception Recorded(Exception error)
        {
            for (Exception current = error; current != null; current = current.InnerException)
                if (current.Data.Contains("ErrorLogId")) return current;
            return null;
        }

        public static string Record(Exception error, string operation)
        {
            if (error == null) throw new ArgumentNullException("error");
            lock (Sync)
            {
                Exception previous = Recorded(error);
                if (previous != null) return Convert.ToString(previous.Data["ErrorLogId"]);
                string id = Guid.NewGuid().ToString("N");
                error.Data["ErrorLogId"] = id;
                string detail = Regex.Replace(error.ToString(),
                    @"(?i)(password|pwd|mat_khau|matkhauhash|salt)\s*[=:]\s*([^;\r\n]*)", "$1=[REDACTED]");
                string entry = DateTimeOffset.Now.ToString("o") + " | " + id + " | " + operation +
                    Environment.NewLine + detail + Environment.NewLine + Environment.NewLine;
                string[] folders = { DirectoryPath,
                    Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NongDuoc"), "Logs"),
                    Path.Combine(Path.Combine(Path.GetTempPath(), "NongDuoc"), "Logs") };
                foreach (string folder in folders)
                {
                    try
                    {
                        Directory.CreateDirectory(folder);
                        string path = Path.Combine(folder, "errors-" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");
                        File.AppendAllText(path, entry, Encoding.UTF8);
                        error.Data["ErrorLogPath"] = path;
                        return id;
                    }
                    catch (Exception logError)
                    {
                        Trace.TraceError("Cannot write error log: " + logError.GetType().FullName);
                    }
                }
                Trace.TraceError(entry);
                return id;
            }
        }

        public static string Describe(Exception error, string operation)
        {
            string id = Record(error, operation);
            Exception recorded = Recorded(error);
            string path = Convert.ToString(recorded.Data["ErrorLogPath"]);
            return operation + ".\r\nMã lỗi: " + id +
                (path.Length > 0 ? "\r\nNhật ký: " + path : "\r\nKhông ghi được tệp nhật ký. Hãy kiểm tra quyền ghi thư mục Logs.");
        }

        public static string UserMessage(Exception error, string operation)
        {
            return IsDatabaseError(error) ? Describe(error, operation) : error.Message;
        }

        public static InvalidOperationException Wrap(Exception error, string operation)
        {
            return new InvalidOperationException(Describe(error, operation), error);
        }

        public static void Rollback(OleDbTransaction transaction, string operation)
        {
            if (transaction == null) return;
            try { transaction.Rollback(); }
            catch (Exception rollbackError) { Record(rollbackError, operation + " - rollback"); }
        }

        public static bool IsDatabaseError(Exception error)
        {
            for (Exception current = error; current != null; current = current.InnerException)
                if (current is OleDbException || current is System.Data.DataException ||
                    current is IOException || current.Data.Contains("ErrorLogId")) return true;
            return false;
        }
    }
}
