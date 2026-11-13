using System;
using System.Data;
using System.Data.OleDb;

namespace CuahangNongduoc.DataLayer
{
    /// <summary>
    /// Factory cho Sản phẩm.
    /// Có các truy vấn đặc thù (tồn kho, cập nhật giá) → giữ lại.
    /// </summary>
    public class SanPhamFactory : BaseFactory
    {
        protected override string TableName => "SAN_PHAM";

        public DataTable DanhsachSanPham() => DanhSach();

        public DataTable LaySanPham(string id) => LayTheoId(id);

        public DataTable TimMaSanPham(string id) => TimKiem("ID", id);

        public DataTable TimTenSanPham(string ten) => TimKiem("TEN_SAN_PHAM", ten);

        /// <summary>
        /// Truy vấn đặc thù: Lấy số lượng tồn (join với bảng MA_SAN_PHAM).
        /// Không thể đưa vào BaseFactory vì logic riêng.
        /// </summary>
        public DataTable LaySoLuongTon()
        {
            OleDbCommand cmd = new OleDbCommand(
                "SELECT SP.ID, SP.TEN_SAN_PHAM, SP.DON_GIA_NHAP, SP.GIA_BAN_SI, SP.GIA_BAN_LE, " +
                "SP.ID_DON_VI_TINH, SP.SO_LUONG, SUM(MA.SO_LUONG) AS SO_LUONG_TON " +
                "FROM SAN_PHAM SP INNER JOIN MA_SAN_PHAM MA ON SP.ID = MA.ID_SAN_PHAM " +
                "GROUP BY SP.ID, SP.TEN_SAN_PHAM, SP.DON_GIA_NHAP, SP.GIA_BAN_SI, " +
                "SP.GIA_BAN_LE, SP.ID_DON_VI_TINH, SP.SO_LUONG");
            
            Load(cmd); // Gọi trực tiếp phương thức Load của DataService
            return this; // Trả về instance bảng hiện tại
        }
    }
}