
using System;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public static class DatabaseConnection
    {
        // Đường dẫn tới file cơ sở dữ liệu
        private static readonly string dbPath =
            @"D:\Bảo trì PM\db\cuahang.dll";

        // Chuỗi kết nối Access MDB
        private static readonly string connectionString =
            @"Provider=Microsoft.Jet.OLEDB.4.0;" +
            @"Data Source=" + dbPath + ";";

        // Lấy kết nối đến cơ sở dữ liệu
        public static OleDbConnection GetConnection()
        {
            return new OleDbConnection(connectionString);
        }

        // Kiểm tra kết nối cơ sở dữ liệu
        public static bool TestConnection()
        {
            try
            {
                if (!File.Exists(dbPath))
                {
                    MessageBox.Show(
                        "Không tìm thấy file database:\n" + dbPath,
                        "Lỗi",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );

                    return false;
                }

                using (OleDbConnection conn = GetConnection())
                {
                    conn.Open();

                    return conn.State == ConnectionState.Open;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Kết nối database thất bại:\n" + ex.Message,
                    "Lỗi kết nối",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                return false;
            }
        }

        // Lấy dữ liệu từ câu lệnh SELECT
        public static DataTable ExecuteQuery(
            string sql,
            params OleDbParameter[] parameters)
        {
            using (OleDbConnection conn = GetConnection())
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }

                using (OleDbDataAdapter adapter =
                       new OleDbDataAdapter(cmd))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    return table;
                }
            }
        }

        // Thực hiện INSERT, UPDATE, DELETE
        public static int ExecuteNonQuery(
            string sql,
            params OleDbParameter[] parameters)
        {
            using (OleDbConnection conn = GetConnection())
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }

                conn.Open();

                return cmd.ExecuteNonQuery();
            }
        }
    }
}
