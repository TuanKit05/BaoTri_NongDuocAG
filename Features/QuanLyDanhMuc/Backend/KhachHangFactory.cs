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

        public static bool IsValidPhoneNumber(string phone)
        {
            if (phone == null) return true;
            foreach (char digit in phone)
                if (digit < '0' || digit > '9') return false;
            return true;
        }

        public override bool Save()
        {
            // Validate the whole batch before writing any row, including pasted values.
            foreach (DataRow row in Rows)
            {
                if (row.RowState != DataRowState.Added && row.RowState != DataRowState.Modified) continue;
                if (!IsValidPhoneNumber(Convert.ToString(row["DIEN_THOAI"])))
                    throw new ArgumentException("Số điện thoại của khách hàng/đại lý mã " +
                        Convert.ToString(row["ID"]) + " chỉ được chứa chữ số từ 0 đến 9.");
            }
            return base.Save();
        }
        public DataTable DanhsachKhachHang() => DanhSach();

        public DataTable DanhsachKhachHang(bool loai)
        {
            OleDbCommand cmd = new OleDbCommand(
                $"SELECT * FROM {TableName} WHERE LOAI_KH = @loai");
            cmd.Parameters.Add("@loai", OleDbType.Boolean).Value = loai;
            Load(cmd); // Gọi trực tiếp phương thức của DataService
            return this; // Trả về chính instance dữ liệu bảng
        }

        public DataTable LayKhachHang(string id) => LayTheoId(id);

        public DataTable TimHoTen(string hoten, bool loai)
        {
            OleDbCommand cmd = new OleDbCommand(
                $"SELECT * FROM {TableName} WHERE HO_TEN LIKE '%' + @hoten + '%' AND LOAI_KH = @loai");
            cmd.Parameters.Add("@hoten", OleDbType.VarWChar, 255).Value = hoten ?? String.Empty;
            cmd.Parameters.Add("@loai", OleDbType.Boolean).Value = loai;
            Load(cmd);
            return this;
        }

        public DataTable TimDiaChi(string diachi, bool loai)
        {
            OleDbCommand cmd = new OleDbCommand(
                $"SELECT * FROM {TableName} WHERE DIA_CHI LIKE '%' + @diachi + '%' AND LOAI_KH = @loai");
            cmd.Parameters.Add("@diachi", OleDbType.VarWChar, 255).Value = diachi ?? String.Empty;
            cmd.Parameters.Add("@loai", OleDbType.Boolean).Value = loai;
            Load(cmd);
            return this;
        }
    }
}