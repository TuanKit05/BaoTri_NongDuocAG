using System;

namespace CuahangNongduoc.Features.BaoCao.ThongKeUuDai
{
    /// <summary>
    /// Một dòng dữ liệu hóa đơn ưu đãi dùng để hiển thị trên form.
    /// </summary>
    public class ThongKeUuDai
    {
        private string maHoaDon;
        private DateTime ngayBan;
        private string maNhanVien;
        private string tenNhanVien;
        private decimal tongTien;
        private decimal tienChietKhauGiamGia;
        private decimal tienKhuyenMai;
        private string ghiChuKhuyenMai;
        private int soLuongHangTang;
        private decimal giaVonHangTang;

        public string MaHoaDon { get { return maHoaDon; } set { maHoaDon = value; } }
        public DateTime NgayBan { get { return ngayBan; } set { ngayBan = value; } }
        public string MaNhanVien { get { return maNhanVien; } set { maNhanVien = value; } }
        public string TenNhanVien { get { return tenNhanVien; } set { tenNhanVien = value; } }
        public decimal TongTien { get { return tongTien; } set { tongTien = value; } }
        public decimal TienChietKhauGiamGia { get { return tienChietKhauGiamGia; } set { tienChietKhauGiamGia = value; } }
        public decimal TienKhuyenMai { get { return tienKhuyenMai; } set { tienKhuyenMai = value; } }
        public string GhiChuKhuyenMai { get { return ghiChuKhuyenMai; } set { ghiChuKhuyenMai = value; } }
        // Số lượng và giá vốn các dòng hàng tặng (LA_HANG_TANG = True) của phiếu, lấy từ chi tiết
        public int SoLuongHangTang { get { return soLuongHangTang; } set { soLuongHangTang = value; } }
        public decimal GiaVonHangTang { get { return giaVonHangTang; } set { giaVonHangTang = value; } }
        public decimal TongUuDai { get { return TienChietKhauGiamGia + TienKhuyenMai + GiaVonHangTang; } }
    }

    /// <summary>
    /// Quyền xem báo cáo toàn cửa hàng lấy trực tiếp từ phiên đăng nhập (AccessControl).
    /// Mặc định không cấp quyền.
    /// </summary>
    public static class ThongKeUuDaiSession
    {
        public static bool CoQuyenXemToanBo
        {
            get { return AccessControl.Can("XemTatCaPhieuBan"); }
        }
    }
}