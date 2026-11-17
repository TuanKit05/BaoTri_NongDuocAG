using System;
using System.Data;
using System.Data.OleDb;

namespace CuahangNongduoc.DataLayer
{
    /// <summary>
    /// Factory cho Khách hàng / Đại lý.
    /// Có logic lọc theo LoaiKH → override thêm method riêng.
    /// </summary>
    public class KhachHangFactory : BaseFactory
    {
        protected override string TableName => "KHACH_HANG";

        public DataTable DanhsachKhachHang() => DanhSach();

        public DataTable DanhsachKhachHang(bool loai)
        {
            OleDbCommand cmd = new OleDbCommand(
                $"SELECT * FROM {TableName} WHERE LOAI_KH = @loai");
            cmd.Parameters.AddWithValue("@loai", loai);
            Load(cmd); // Gọi trực tiếp phương thức của DataService
            return this; // Trả về chính instance dữ liệu bảng
        }

        public DataTable LayKhachHang(string id) => LayTheoId(id);

        public DataTable TimHoTen(string hoten, bool loai)
        {
            OleDbCommand cmd = new OleDbCommand(
                $"SELECT * FROM {TableName} WHERE HO_TEN LIKE '%' + @hoten + '%' AND LOAI_KH = @loai");
            cmd.Parameters.AddWithValue("@hoten", hoten);
            cmd.Parameters.AddWithValue("@loai", loai);
            Load(cmd);
            return this;
        }

        public DataTable TimDiaChi(string diachi, bool loai)
        {
            OleDbCommand cmd = new OleDbCommand(
                $"SELECT * FROM {TableName} WHERE DIA_CHI LIKE '%' + @diachi + '%' AND LOAI_KH = @loai");
            cmd.Parameters.AddWithValue("@diachi", diachi);
            cmd.Parameters.AddWithValue("@loai", loai);
            Load(cmd);
            return this;
        }
    }
}