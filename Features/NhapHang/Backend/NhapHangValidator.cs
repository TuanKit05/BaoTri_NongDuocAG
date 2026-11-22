using System;

namespace CuahangNongduoc.Validation
{
    /// <summary>
    /// Tap trung cac quy tac kiem tra phieu nhap va lo hang.
    /// Refactoring: Extract Class, Extract Method, Guard Clauses.
    /// </summary>
    public static class NhapHangValidator
    {
        private const int DoDaiMaToiDa = 50;

        public static string KiemTraThongTinLoNhap(
            string maLo,
            string maSanPham,
            decimal donGiaNhap,
            decimal soLuong,
            DateTime ngayNhap,
            DateTime ngaySanXuat,
            DateTime ngayHetHan)
        {
            maLo = maLo == null ? String.Empty : maLo.Trim();
            maSanPham = maSanPham == null ? String.Empty : maSanPham.Trim();

            if (String.IsNullOrEmpty(maLo))
                return "Vui lòng nhập mã lô sản phẩm.";

            if (maLo.Length > DoDaiMaToiDa)
                return "Mã lô không được dài quá 50 ký tự.";

            if (String.IsNullOrEmpty(maSanPham))
                return "Vui lòng chọn sản phẩm cần nhập.";

            if (maSanPham.Length > DoDaiMaToiDa)
                return "Mã sản phẩm không được dài quá 50 ký tự.";

            if (donGiaNhap <= 0)
                return "Đơn giá nhập phải lớn hơn 0.";

            if (soLuong <= 0)
                return "Số lượng nhập phải lớn hơn 0.";

            if (soLuong != Decimal.Truncate(soLuong))
                return "Số lượng nhập phải là số nguyên.";

            if (soLuong > Int32.MaxValue)
                return "Số lượng nhập vượt quá giới hạn cho phép.";

            if (donGiaNhap * soLuong > Int64.MaxValue)
                return "Thành tiền của lô vượt quá giới hạn lưu trữ.";

            if (ngaySanXuat.Date > ngayNhap.Date)
                return "Ngày sản xuất không được sau ngày nhập hàng.";

            if (ngayHetHan.Date <= ngaySanXuat.Date)
                return "Ngày hết hạn phải sau ngày sản xuất.";

            if (ngayHetHan.Date < ngayNhap.Date)
                return "Lô hàng đã hết hạn trước ngày nhập, không thể nhập kho.";

            return String.Empty;
        }

        public static string KiemTraThongTinPhieuNhap(
            string maPhieu,
            string maNhaCungCap,
            decimal tongTien,
            decimal daTra)
        {
            maPhieu = maPhieu == null ? String.Empty : maPhieu.Trim();
            maNhaCungCap = maNhaCungCap == null ? String.Empty : maNhaCungCap.Trim();

            if (String.IsNullOrEmpty(maPhieu))
                return "Mã phiếu nhập không được để trống.";

            if (maPhieu.Length > DoDaiMaToiDa)
                return "Mã phiếu nhập không được dài quá 50 ký tự.";

            if (String.IsNullOrEmpty(maNhaCungCap))
                return "Vui lòng chọn nhà cung cấp.";

            if (tongTien <= 0)
                return "Phiếu nhập phải có ít nhất một lô hàng có thành tiền lớn hơn 0.";

            if (tongTien > Int64.MaxValue)
                return "Tổng tiền phiếu nhập vượt quá giới hạn lưu trữ.";

            if (daTra < 0)
                return "Số tiền đã trả không hợp lệ.";

            if (daTra > tongTien)
                return "Số tiền đã trả không được lớn hơn tổng tiền phiếu nhập.";

            return String.Empty;
        }
    }
}
