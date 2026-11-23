using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace CuahangNongduoc.BusinessObject
{
    public class BoTinhUuDai
    {
        private IChienLuocUuDai chienLuoc;

        public BoTinhUuDai(IChienLuocUuDai chienLuoc)
        {
            if (chienLuoc == null)
                throw new ArgumentNullException("chienLuoc");

            this.chienLuoc = chienLuoc;
        }

        public void ThayDoiChienLuoc(IChienLuocUuDai chienLuocMoi)
        {
            if (chienLuocMoi == null)
                throw new ArgumentNullException("chienLuocMoi");

            this.chienLuoc = chienLuocMoi;
        }

        public decimal TinhUuDai(decimal soTienGoc, decimal giaTri)
        {
            return chienLuoc.TinhUuDai(soTienGoc, giaTri);
        }
    }

    // Một dòng ưu đãi đọc từ bảng UU_DAI (chỉ đọc, không ghi).
    // LOAI = GIAM_PHAN_TRAM: giảm % trên cả hóa đơn (PHAM_VI = HOA_DON), GIA_TRI là % (10 nghĩa là 10%).
    // LOAI = KHUYEN_MAI: mua X tặng Y (chưa áp dụng tự động trong đợt này).
    // DIEU_KIEN dạng "KHOA=GIA_TRI;KHOA=GIA_TRI", ví dụ "TOI_THIEU_TIEN=500000;TOI_THIEU_SL=10".
    public class UuDaiQuyDinh
    {
        public const String LoaiGiamPhanTram = "GIAM_PHAN_TRAM";
        public const String LoaiChietKhau = "CHIET_KHAU";
        public const String LoaiKhuyenMai = "KHUYEN_MAI";
        public const String PhamViHoaDon = "HOA_DON";

        public String Loai = "";
        public String PhamVi = "";
        public decimal GiaTri;
        public decimal TienToiThieu;
        public int SoLuongToiThieu;
        public int MuaSl;
        public int TangSl;
        public String MaSanPham = ""; // Trống = áp cho mọi mặt hàng (KHUYEN_MAI)

        public static UuDaiQuyDinh TuDataRow(DataRow row)
        {
            UuDaiQuyDinh ud = new UuDaiQuyDinh();
            ud.Loai = DocChuoiCot(row, "LOAI").Trim().ToUpperInvariant();
            ud.PhamVi = DocChuoiCot(row, "PHAM_VI").Trim().ToUpperInvariant();
            ud.GiaTri = DocSoCot(row, "GIA_TRI");
            ud.MaSanPham = DocChuoiCot(row, "MA_SAN_PHAM").Trim();

            Dictionary<String, String> dieuKien = PhanTichDieuKien(DocChuoiCot(row, "DIEU_KIEN"));
            ud.TienToiThieu = LaySo(dieuKien, "TOI_THIEU_TIEN");
            ud.SoLuongToiThieu = (int)LaySo(dieuKien, "TOI_THIEU_SL");
            ud.MuaSl = (int)LaySo(dieuKien, "MUA_SL");
            ud.TangSl = (int)LaySo(dieuKien, "TANG_SL");
            return ud;
        }

        private static String DocChuoiCot(DataRow row, String ten)
        {
            if (!row.Table.Columns.Contains(ten))
                return "";
            object o = row[ten];
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o);
        }

        private static decimal DocSoCot(DataRow row, String ten)
        {
            if (!row.Table.Columns.Contains(ten))
                return 0m;
            object o = row[ten];
            return o == null || o == DBNull.Value ? 0m : Convert.ToDecimal(o);
        }

        private static Dictionary<String, String> PhanTichDieuKien(String dieuKien)
        {
            Dictionary<String, String> d = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            if (dieuKien == null || dieuKien.Trim().Length == 0)
                return d;
            foreach (String cap in dieuKien.Split(';'))
            {
                int dau = cap.IndexOf('=');
                if (dau <= 0)
                    continue;
                d[cap.Substring(0, dau).Trim()] = cap.Substring(dau + 1).Trim();
            }
            return d;
        }

        private static decimal LaySo(Dictionary<String, String> d, String khoa)
        {
            String s;
            decimal v;
            if (d.TryGetValue(khoa, out s) &&
                decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out v))
                return v;
            return 0m;
        }
    }

    // Mua tặng (KHUYEN_MAI): mua MUA_SL tặng TANG_SL cùng mặt hàng.
    public static class ChonKhuyenMai
    {
        // Quy tắc đầu tiên áp được cho mặt hàng ma. Null nếu không có.
        public static UuDaiQuyDinh TimQuyTac(IEnumerable<UuDaiQuyDinh> dsUuDai, String ma)
        {
            if (dsUuDai == null)
                return null;
            foreach (UuDaiQuyDinh ud in dsUuDai)
            {
                if (ud.Loai != UuDaiQuyDinh.LoaiKhuyenMai || ud.MuaSl <= 0 || ud.TangSl <= 0)
                    continue;
                if (ud.MaSanPham.Length > 0 && !String.Equals(ud.MaSanPham, ma, StringComparison.OrdinalIgnoreCase))
                    continue;
                return ud;
            }
            return null;
        }

        // Số hàng tặng còn thiếu so với số đã mua (đã tặng rồi thì không tặng thêm)
        public static int SoHangTangCanThem(UuDaiQuyDinh quyTac, int daMua, int daTang)
        {
            int can = (daMua / quyTac.MuaSl) * quyTac.TangSl;
            return can > daTang ? can - daTang : 0;
        }
    }

    // Chọn ưu đãi tự động cho hóa đơn. Đây là quy tắc nghiệp vụ, không phụ thuộc giao diện.
    public static class ChonUuDai
    {
        // Mức giảm % cao nhất trong các ưu đãi HOA_DON đủ điều kiện. Không có thì trả 0.
        public static double PhanTramGiamHoaDon(long tienHang, int tongSoLuong, IEnumerable<UuDaiQuyDinh> dsUuDai)
        {
            if (dsUuDai == null)
                return 0;
            double cao = 0;
            double chietKhau = 0;
            foreach (UuDaiQuyDinh ud in dsUuDai)
            {
                if ((ud.Loai != UuDaiQuyDinh.LoaiGiamPhanTram && ud.Loai != UuDaiQuyDinh.LoaiChietKhau) || ud.PhamVi != UuDaiQuyDinh.PhamViHoaDon)
                    continue;
                if (tienHang < ud.TienToiThieu || tongSoLuong < ud.SoLuongToiThieu)
                    continue;
                double phanTram = (double)ud.GiaTri;
                if (ud.Loai == UuDaiQuyDinh.LoaiChietKhau)
                    chietKhau = Math.Max(chietKhau, phanTram);
                else if (phanTram > cao)
                    cao = phanTram;
            }
            return Math.Min(100, cao + chietKhau);
        }
    }
}
