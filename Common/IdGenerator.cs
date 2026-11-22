using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.Threading;

namespace CuahangNongduoc
{
    // Reserve numbers in a database transaction; never reuse a cancelled/deleted number.
    public static class IdGenerator
    {
        private static readonly string[] Tables = { "SAN_PHAM", "KHACH_HANG", "NHA_CUNG_CAP",
            "PHIEU_BAN", "PHIEU_NHAP", "PHIEU_CHI", "PHIEU_THANH_TOAN", "TAI_KHOAN" };
        private static readonly object SchemaLock = new object();

        public static void EnsureSchema(OleDbConnection connection)
        {
            lock (SchemaLock)
            {
                using (DataTable schema = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables,
                    new object[] { null, null, "BO_DEM_MA", "TABLE" }))
                {
                    if (schema.Rows.Count == 0)
                        using (OleDbCommand create = new OleDbCommand(
                            "CREATE TABLE BO_DEM_MA (TEN_BANG TEXT(50) NOT NULL PRIMARY KEY, GIA_TRI LONG NOT NULL)", connection))
                            create.ExecuteNonQuery();
                }
                foreach (string table in Tables)
                {
                    int maximum = 0;
                    using (OleDbCommand read = new OleDbCommand("SELECT ID FROM [" + table + "]", connection))
                    using (OleDbDataReader reader = read.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string code = Convert.ToString(reader[0]).Trim();
                            if (table == "TAI_KHOAN" && code.StartsWith("TK", StringComparison.OrdinalIgnoreCase)) code = code.Substring(2);
                            int number;
                            if (Int32.TryParse(code, NumberStyles.None, CultureInfo.InvariantCulture, out number))
                                maximum = Math.Max(maximum, number);
                        }
                    }
                    using (OleDbCommand update = new OleDbCommand(
                        "UPDATE BO_DEM_MA SET GIA_TRI=? WHERE TEN_BANG=? AND GIA_TRI<?", connection))
                    {
                        update.Parameters.Add("maximum", OleDbType.Integer).Value = maximum;
                        update.Parameters.Add("table", OleDbType.VarWChar, 50).Value = table;
                        update.Parameters.Add("minimum", OleDbType.Integer).Value = maximum;
                        update.ExecuteNonQuery();
                    }
                    using (OleDbCommand exists = new OleDbCommand("SELECT COUNT(*) FROM BO_DEM_MA WHERE TEN_BANG=?", connection))
                    {
                        exists.Parameters.Add("table", OleDbType.VarWChar, 50).Value = table;
                        if (Convert.ToInt32(exists.ExecuteScalar()) != 0) continue;
                    }
                    using (OleDbCommand insert = new OleDbCommand("INSERT INTO BO_DEM_MA (TEN_BANG,GIA_TRI) VALUES (?,?)", connection))
                    {
                        insert.Parameters.Add("table", OleDbType.VarWChar, 50).Value = table;
                        insert.Parameters.Add("maximum", OleDbType.Integer).Value = maximum;
                        insert.ExecuteNonQuery();
                    }
                }
            }
        }

        private static void Demand(string table)
        {
            if (Array.IndexOf(Tables, table) < 0) throw new ArgumentException("Bảng cấp mã không hợp lệ.");
            if (table == "PHIEU_BAN") AccessControl.DemandCurrent("BanHang");
            else if (table == "PHIEU_NHAP") AccessControl.DemandCurrent("NhapHang");
            else if (table == "PHIEU_CHI" || table == "PHIEU_THANH_TOAN") AccessControl.DemandCurrent("ThuChi");
            else if (table == "TAI_KHOAN") AccessControl.DemandCurrent("QuanLyTaiKhoan");
            else if (table == "SAN_PHAM") AccessControl.DemandCreateProduct();
            else if (table == "KHACH_HANG" && AccessControl.CanAddCustomer())
            { AccessControl.DemandCurrent("BanHang"); AccessControl.DemandCurrent("KhachHang"); }
            else AccessControl.DemandCurrent("SuaDanhMuc");
        }

        public static string Next(string table)
        {
            Demand(table);
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    using (OleDbConnection connection = new OleDbConnection(DataService.m_ConnectString))
                    {
                        connection.Open();
                        using (OleDbTransaction transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                int value;
                                using (OleDbCommand read = new OleDbCommand("SELECT GIA_TRI FROM BO_DEM_MA WHERE TEN_BANG=?", connection, transaction))
                                {
                                    read.Parameters.Add("table", OleDbType.VarWChar, 50).Value = table;
                                    object current = read.ExecuteScalar();
                                    if (current == null || current == DBNull.Value)
                                        throw new InvalidOperationException("Chưa khởi tạo bộ đếm mã. Hãy khởi động lại ứng dụng.");
                                    value = Convert.ToInt32(current);
                                }
                                string code;
                                while (true)
                                {
                                    int next = checked(value + 1);
                                    using (OleDbCommand update = new OleDbCommand(
                                        "UPDATE BO_DEM_MA SET GIA_TRI=? WHERE TEN_BANG=? AND GIA_TRI=?", connection, transaction))
                                    {
                                        update.Parameters.Add("next", OleDbType.Integer).Value = next;
                                        update.Parameters.Add("table", OleDbType.VarWChar, 50).Value = table;
                                        update.Parameters.Add("previous", OleDbType.Integer).Value = value;
                                        if (update.ExecuteNonQuery() != 1) throw new DBConcurrencyException("Bộ đếm đang được sử dụng.");
                                    }
                                    value = next;
                                    code = table == "TAI_KHOAN" ? "TK" + value.ToString("D6", CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture);
                                    using (OleDbCommand exists = new OleDbCommand("SELECT COUNT(*) FROM [" + table + "] WHERE ID=?", connection, transaction))
                                    {
                                        exists.Parameters.Add("id", OleDbType.VarWChar, 50).Value = code;
                                        if (Convert.ToInt32(exists.ExecuteScalar()) == 0) break;
                                    }
                                }
                                transaction.Commit();
                                return code;
                            }
                            catch { ErrorLog.Rollback(transaction, "Cấp mã tự tăng"); throw; }
                        }
                    }
                }
                catch (DBConcurrencyException) { if (attempt >= 4) throw; Thread.Sleep(50 * (attempt + 1)); }
                catch (OleDbException error)
                {
                    bool busy = false;
                    foreach (OleDbError detail in error.Errors)
                        busy |= detail.NativeError == 3188 || detail.NativeError == 3218 || detail.NativeError == 3260 || detail.NativeError == 3197;
                    if (!busy || attempt >= 4) throw ErrorLog.Wrap(error, "Không thể cấp mã tự tăng");
                    Thread.Sleep(50 * (attempt + 1));
                }
            }
        }
    }
}
