using System;
using System.Data;
using System.Data.OleDb;

namespace CuahangNongduoc
{
    public static class WarehouseSettings
    {
        public const string FirstInFirstOut = "FIFO";
        public const string SpecificLot = "CHI_DINH_LO";
        public const string WeightedAverage = "BQGQ";

        public static void DemandManage()
        {
            AccessControl.DemandCurrent("QuanTri");
            if (!AccountRoles.IsAdmin(AppSession.CurrentUser.Role))
                throw new UnauthorizedAccessException("Chỉ quản lý được thay đổi cấu hình kho.");
        }

        public static string Describe(string method, string costMethod, int days)
        {
            return "Xuất kho: " + (method == FirstInFirstOut ? "Nhập trước xuất trước" : "Chỉ định lô") +
                "; giá vốn: " + (costMethod == WeightedAverage ? "Bình quân gia quyền" : "FIFO theo lớp giá nhập trước") +
                "; cảnh báo trước " + days + " ngày";
        }

        public static DataTable History()
        {
            DemandManage();
            using (OleDbConnection connection = new OleDbConnection(DataService.m_ConnectString))
            {
                connection.Open();
                return AccountRepository.ReadTable(connection, null,
                    "SELECT NK.THOI_GIAN, NK.ID_TAI_KHOAN, TK.TEN_DANG_NHAP, TK.HO_TEN, NK.NOI_DUNG " +
                    "FROM NHAT_KY NK LEFT JOIN TAI_KHOAN TK ON NK.ID_TAI_KHOAN=TK.ID " +
                    "WHERE NK.THAO_TAC='DOI_CAU_HINH_KHO' AND NK.DOI_TUONG='CUA_HANG' ORDER BY NK.THOI_GIAN DESC, NK.ID DESC");
            }
        }

        internal static void EnsureSchema(OleDbConnection connection)
        {
            bool exists = false;
            foreach (DataRow row in connection.GetSchema("Tables").Rows)
                if (String.Equals(Convert.ToString(row["TABLE_NAME"]), "CAU_HINH_KHO", StringComparison.OrdinalIgnoreCase)) exists = true;
            if (!exists)
                AccountRepository.Execute(connection, null, "CREATE TABLE CAU_HINH_KHO ([ID] COUNTER PRIMARY KEY, [PHAM_VI] TEXT(20) NOT NULL, [PHUONG_PHAP_XUAT] TEXT(20) NOT NULL, [NGAY_HIEU_LUC] DATETIME, [NGUOI_DOI] TEXT(50), CONSTRAINT UQ_CAU_HINH_KHO UNIQUE ([PHAM_VI]))");
            if (Convert.ToInt32(AccountRepository.Scalar(connection, null, "SELECT COUNT(*) FROM CAU_HINH_KHO WHERE PHAM_VI='CUA_HANG'")) == 0)
                AccountRepository.Execute(connection, null, "INSERT INTO CAU_HINH_KHO (PHAM_VI, PHUONG_PHAP_XUAT, NGAY_HIEU_LUC, NGUOI_DOI) VALUES ('CUA_HANG', ?, ?, ?)", SpecificLot, DateTime.Now, "SYSTEM");
            AddColumn(connection, "CAU_HINH_KHO", "PHUONG_PHAP_GIA", "TEXT(20)");
            AccountRepository.Execute(connection, null, "UPDATE CAU_HINH_KHO SET PHUONG_PHAP_GIA='BQGQ' WHERE PHUONG_PHAP_GIA IS NULL");
            AddColumn(connection, "CHI_TIET_PHIEU_BAN", "GIA_VON", "CURRENCY");
            AddColumn(connection, "CHI_TIET_PHIEU_BAN", "PHUONG_PHAP_GIA", "TEXT(20)");
            AddColumn(connection, "CHI_TIET_PHIEU_BAN", "LA_HANG_TANG", "YESNO");
            AccountRepository.Execute(connection, null, "UPDATE CHI_TIET_PHIEU_BAN SET LA_HANG_TANG=False WHERE LA_HANG_TANG IS NULL");
            // Historical lots remain NULL when their initial receipt quantity was not recorded.
            AddColumn(connection, "MA_SAN_PHAM", "SO_LUONG_NHAP", "LONG");
            AddColumn(connection, "CAU_HINH_KHO", "NGUONG_HET_HAN", "LONG");
            AccountRepository.Execute(connection, null, "UPDATE CAU_HINH_KHO SET NGUONG_HET_HAN=60 WHERE NGUONG_HET_HAN IS NULL");
        }

        public static int LoadExpiryDays()
        {
            AccessControl.DemandCurrent("SanPham");
            using (OleDbConnection connection = new OleDbConnection(DataService.m_ConnectString))
            {
                connection.Open();
                return ReadExpiryDays(connection, null);
            }
        }

        private static int ReadExpiryDays(OleDbConnection connection, OleDbTransaction transaction)
        {
            int days = Convert.ToInt32(AccountRepository.Scalar(connection, transaction,
                "SELECT NGUONG_HET_HAN FROM CAU_HINH_KHO WHERE PHAM_VI='CUA_HANG'"));
            if (days < 0 || days > 3650) throw new ArgumentException("Ngưỡng cảnh báo phải từ 0 đến 3650 ngày.");
            return days;
        }

        private static void AddColumn(OleDbConnection connection, string table, string column, string type)
        {
            DataTable columns = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Columns, new object[] { null, null, table, column });
            if (columns.Rows.Count == 0) AccountRepository.Execute(connection, null, "ALTER TABLE [" + table + "] ADD COLUMN [" + column + "] " + type);
        }

        public static string LoadCostMethod()
        {
            AccessControl.DemandCurrent(ReadPermission());
            using (OleDbConnection connection = new OleDbConnection(DataService.m_ConnectString))
            {
                connection.Open();
                return ReadCostMethod(connection, null);
            }
        }

        internal static string ReadCostMethod(OleDbConnection connection, OleDbTransaction transaction)
        {
            string method = Convert.ToString(AccountRepository.Scalar(connection, transaction,
                "SELECT PHUONG_PHAP_GIA FROM CAU_HINH_KHO WHERE PHAM_VI='CUA_HANG'"));
            ValidateCost(method);
            return method;
        }

        private static void ValidateCost(string method)
        {
            if (method != WeightedAverage && method != FirstInFirstOut) throw new ArgumentException("Phương pháp giá xuất không hợp lệ.");
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
            Save(method, LoadCostMethod());
        }

        public static void Save(string method, string costMethod)
        {
            Save(method, costMethod, LoadExpiryDays());
        }

        public static void Save(string method, string costMethod, int expiryDays)
        {
            DemandManage();
            if (expiryDays < 0 || expiryDays > 3650) throw new ArgumentException("Ngưỡng cảnh báo phải từ 0 đến 3650 ngày.");
            Validate(method);
            ValidateCost(costMethod);
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
                        string previousCost = ReadCostMethod(connection, transaction);
                        int previousDays = ReadExpiryDays(connection, transaction);
                        if (previous != method || previousCost != costMethod || previousDays != expiryDays)
                        {
                            int changed = AccountRepository.Execute(connection, transaction,
                                "UPDATE CAU_HINH_KHO SET PHUONG_PHAP_XUAT=?, PHUONG_PHAP_GIA=?, NGUONG_HET_HAN=?, NGAY_HIEU_LUC=?, NGUOI_DOI=? WHERE PHAM_VI='CUA_HANG' AND PHUONG_PHAP_XUAT=? AND PHUONG_PHAP_GIA=? AND NGUONG_HET_HAN=?",
                                method, costMethod, expiryDays, DateTime.Now, AppSession.CurrentUser.Id, previous, previousCost, previousDays);
                            if (changed != 1) throw new InvalidOperationException("Cấu hình đã thay đổi. Vui lòng mở lại để kiểm tra.");
                            AccountRepository.Audit(connection, transaction, "DOI_CAU_HINH_KHO", "CUA_HANG",
                                "Cũ: " + Describe(previous, previousCost, previousDays) + "\r\nMới: " + Describe(method, costMethod, expiryDays));
                        }
                        transaction.Commit();
                    }
                    catch { ErrorLog.Rollback(transaction, "Lưu cấu hình kho"); throw; }
                }
            }
        }
    }
}
