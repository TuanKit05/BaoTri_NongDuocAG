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

        public string MaHoaDon { get { return maHoaDon; } set { maHoaDon = value; } }
        public DateTime NgayBan { get { return ngayBan; } set { ngayBan = value; } }
        public string MaNhanVien { get { return maNhanVien; } set { maNhanVien = value; } }
        public string TenNhanVien { get { return tenNhanVien; } set { tenNhanVien = value; } }
        public decimal TongTien { get { return tongTien; } set { tongTien = value; } }
        public decimal TienChietKhauGiamGia { get { return tienChietKhauGiamGia; } set { tienChietKhauGiamGia = value; } }
        public decimal TienKhuyenMai { get { return tienKhuyenMai; } set { tienKhuyenMai = value; } }
        public string GhiChuKhuyenMai { get { return ghiChuKhuyenMai; } set { ghiChuKhuyenMai = value; } }
        public decimal TongUuDai { get { return TienChietKhauGiamGia + TienKhuyenMai; } }
    }

    /// <summary>
    /// Quyền báo cáo toàn cửa hàng cần được gán sau khi xác thực tài khoản.
    /// Mặc định không cấp quyền.
    /// </summary>
    public static class ThongKeUuDaiSession
    {
        private static bool coQuyenXemToanBo;

        public static bool CoQuyenXemToanBo
        {
            get { return coQuyenXemToanBo; }
        }

        public static void GanQuyen(bool quanLyHoacChuCuaHang)
        {
            coQuyenXemToanBo = quanLyHoacChuCuaHang;
        }

        public static void DangXuat()
        {
            coQuyenXemToanBo = false;
        }
    }
}
