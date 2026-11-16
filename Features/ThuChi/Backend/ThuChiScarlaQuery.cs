using System;
using System.Data.OleDb;

namespace CuahangNongduoc.DataLayer
{
    // Extract Method / Move Method: truy van scalar va hai tham so thang-nam duoc su dung chung.
    internal static class ThuChiScalarQuery
    {
        internal static long LaySoTien(OleDbCommand command)
        {
            object result = new DataService().ExecuteScalar(command);
            return result == null ? 0L : Convert.ToInt64(result);
        }

        internal static void ThemThamSoThangNam(OleDbCommand command, int thang, int nam)
        {
            command.Parameters.Add("thang", OleDbType.Integer).Value = thang;
            command.Parameters.Add("nam", OleDbType.Integer).Value = nam;
        }
    }
}
