using System;
using System.Data;
using System.Data.OleDb;

namespace CuahangNongduoc
{
    public static class WarehouseSettings
    {
        public const string FirstInFirstOut = "FIFO";
        public const string SpecificLot = "CHI_DINH_LO";

        internal static void EnsureSchema(OleDbConnection connection)
        {
            bool exists = false;
            foreach (DataRow row in connection.GetSchema("Tables").Rows)
                if (String.Equals(Convert.ToString(row["TABLE_NAME"]), "CAU_HINH_KHO", StringComparison.OrdinalIgnoreCase)) exists = true;
            if (!exists)
                AccountRepository.Execute(connection, null, "CREATE TABLE CAU_HINH_KHO ([ID] COUNTER PRIMARY KEY, [PHAM_VI] TEXT(20) NOT NULL, [PHUONG_PHAP_XUAT] TEXT(20) NOT NULL, [NGAY_HIEU_LUC] DATETIME, [NGUOI_DOI] TEXT(50), CONSTRAINT UQ_CAU_HINH_KHO UNIQUE ([PHAM_VI]))");
            if (Convert.ToInt32(AccountRepository.Scalar(connection, null, "SELECT COUNT(*) FROM CAU_HINH_KHO WHERE PHAM_VI='CUA_HANG'")) == 0)
                AccountRepository.Execute(connection, null, "INSERT INTO CAU_HINH_KHO (PHAM_VI, PHUONG_PHAP_XUAT, NGAY_HIEU_LUC, NGUOI_DOI) VALUES ('CUA_HANG', ?, ?, ?)", SpecificLot, DateTime.Now, "SYSTEM");
        }

        public static string Load()
        {
            AccessControl.DemandCurrent(ReadPermission());
            using (OleDbConnection connection = new OleDbConnection(DataService.m_ConnectString))
            {
                connection.Open();
                string value = Convert.ToString(AccountRepository.Scalar(connection, null,
                    "SELECT PHUONG_PHAP_XUAT FROM CAU_HINH_KHO WHERE PHAM_VI='CUA_HANG'"));
                Validate(value);
                return value;
            }
        }

        private static string ReadPermission()
        {
            if (AccessControl.Can("QuanTri")) return "QuanTri";
            if (AccessControl.Can("BanHang")) return "BanHang";
            return "NhapHang";
        }

        private static void Validate(string method)
        {
            if (method != FirstInFirstOut && method != SpecificLot)
                throw new ArgumentException("Phương pháp xuất kho không hợp lệ.");
        }

        public static void Save(string method)
        {
            AccessControl.DemandCurrent("QuanTri");
            Validate(method);
            using (OleDbConnection connection = new OleDbConnection(DataService.m_ConnectString))
            {
                connection.Open();
                using (OleDbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string previous = Convert.ToString(AccountRepository.Scalar(connection, transaction,
                            "SELECT PHUONG_PHAP_XUAT FROM CAU_HINH_KHO WHERE PHAM_VI='CUA_HANG'"));
                        Validate(previous);
                        if (previous != method)
                        {
                            int changed = AccountRepository.Execute(connection, transaction,
                                "UPDATE CAU_HINH_KHO SET PHUONG_PHAP_XUAT=?, NGAY_HIEU_LUC=?, NGUOI_DOI=? WHERE PHAM_VI='CUA_HANG' AND PHUONG_PHAP_XUAT=?",
                                method, DateTime.Now, AppSession.CurrentUser.Id, previous);
                            if (changed != 1) throw new InvalidOperationException("Cấu hình đã thay đổi. Vui lòng mở lại để kiểm tra.");
                            AccountRepository.Audit(connection, transaction, "DOI_CAU_HINH_KHO", "CUA_HANG", previous + " -> " + method);
                        }
                        transaction.Commit();
                    }
                    catch { ErrorLog.Rollback(transaction, "Lưu cấu hình kho"); throw; }
                }
            }
        }
    }
}
