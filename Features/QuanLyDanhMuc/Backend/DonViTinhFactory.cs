using System;
using System.Data;
using System.Data.OleDb;

namespace CuahangNongduoc.DataLayer
{
    /// <summary>
    /// Factory cho Đơn vị tính.
    /// Kế thừa BaseFactory → loại bỏ code trùng lặp (Extract Superclass).
    /// </summary>
    public class DonViTinhFactory : BaseFactory
    {
        protected override string TableName => "DON_VI_TINH";

        // Giữ tên method cũ để tương thích với code Frontend hiện tại
        public DataTable DanhsachDVT() => DanhSach();

        public DataTable LayDVT(int id) => LayTheoId(id);
    }
}
