using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace CuahangNongduoc.Features.BaoCao.Backend
{
    // YC-BC-02: tồn kho từng lô tại một ngày bất kỳ.
    // Công thức: tồn tại ngày D = tồn hiện tại - tổng biến động ghi sau ngày D (BIEN_DONG_KHO).
    // Lô nhập sau ngày D được coi là tồn 0 tại D.
    // Số liệu chính xác khi D không trước ngày mở sổ (dòng LOAI = 'MO_SO' do script cài đặt tạo).
    public sealed class TonKhoLo
    {
        public string MaLo { get; set; }
        public string MaSp { get; set; }
        public string TenSanPham { get; set; }
        public string TenDonVi { get; set; }
        public DateTime NgayNhap { get; set; }
        public DateTime? NgayHetHan { get; set; }
        public long SoLuongHienTai { get; set; }
        public long SoLuongTaiNgay { get; set; }
        public decimal DonGiaNhap { get; set; }
        public decimal GiaTriTaiNgay { get; set; }
    }

    public sealed class KetQuaTonKho
    {
        public DateTime Ngay { get; set; }
        public DateTime? NgayMoSo { get; set; }
        public bool DuDuLieu { get; set; }
        public List<TonKhoLo> DanhSach { get; set; }
        public long TongSoLuong { get; set; }
        public decimal TongGiaTri { get; set; }
    }

    public sealed class TonKhoTaiNgayService
    {
        public KetQuaTonKho LayTonKhoTaiNgay(DateTime ngay)
        {
            DateTime d = ngay.Date;
            KetQuaTonKho kq = new KetQuaTonKho();
            kq.Ngay = d;
            kq.DanhSach = new List<TonKhoLo>();

            kq.NgayMoSo = LayNgayMoSo();
            kq.DuDuLieu = kq.NgayMoSo.HasValue && d >= kq.NgayMoSo.Value.Date;

            // Biến động sau ngày D, cộng gộp theo lô
            Dictionary<string, long> sauD = new Dictionary<string, long>(StringComparer.Ordinal);
            OleDbCommand cmdBd = new OleDbCommand(
                "SELECT MA_LO, SUM(SO_LUONG) AS BIEN_DONG FROM BIEN_DONG_KHO WHERE NGAY >= ? GROUP BY MA_LO");
            cmdBd.Parameters.Add("ngay", OleDbType.Date).Value = d.AddDays(1);
            DataService dsBd = new DataService();
            dsBd.Load(cmdBd);
            foreach (DataRow r in dsBd.Rows)
            {
                long bd = r["BIEN_DONG"] == DBNull.Value ? 0 : Convert.ToInt64(r["BIEN_DONG"]);
                sauD[Convert.ToString(r["MA_LO"])] = bd;
            }

            DataService dsLo = new DataService();
            dsLo.Load(new OleDbCommand(
                "SELECT MSP.ID AS MA_LO, SP.ID AS MA_SP, SP.TEN_SAN_PHAM, DVT.TEN_DON_VI, " +
                "MSP.SO_LUONG, MSP.DON_GIA_NHAP, MSP.NGAY_NHAP, MSP.NGAY_HET_HAN " +
                "FROM ((MA_SAN_PHAM MSP INNER JOIN SAN_PHAM SP ON MSP.ID_SAN_PHAM = SP.ID) " +
                "INNER JOIN DON_VI_TINH DVT ON SP.ID_DON_VI_TINH = DVT.ID) " +
                "ORDER BY SP.TEN_SAN_PHAM, MSP.NGAY_NHAP"));

            foreach (DataRow r in dsLo.Rows)
            {
                string maLo = Convert.ToString(r["MA_LO"]);
                DateTime ngayNhap = r["NGAY_NHAP"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["NGAY_NHAP"]);
                long tonHienTai = r["SO_LUONG"] == DBNull.Value ? 0 : Convert.ToInt64(r["SO_LUONG"]);

                long tonTaiNgay;
                if (ngayNhap.Date > d)
                {
                    tonTaiNgay = 0;
                }
                else
                {
                    long bd;
                    sauD.TryGetValue(maLo, out bd);
                    tonTaiNgay = tonHienTai - bd;
                }
                if (tonTaiNgay <= 0)
                    continue;

                decimal donGia = r["DON_GIA_NHAP"] == DBNull.Value ? 0m : Convert.ToDecimal(r["DON_GIA_NHAP"]);
                TonKhoLo lo = new TonKhoLo();
                lo.MaLo = maLo;
                lo.MaSp = Convert.ToString(r["MA_SP"]);
                lo.TenSanPham = Convert.ToString(r["TEN_SAN_PHAM"]);
                lo.TenDonVi = Convert.ToString(r["TEN_DON_VI"]);
                lo.NgayNhap = ngayNhap;
                lo.NgayHetHan = r["NGAY_HET_HAN"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["NGAY_HET_HAN"]);
                lo.SoLuongHienTai = tonHienTai;
                lo.SoLuongTaiNgay = tonTaiNgay;
                lo.DonGiaNhap = donGia;
                lo.GiaTriTaiNgay = tonTaiNgay * donGia;
                kq.DanhSach.Add(lo);

                kq.TongSoLuong += tonTaiNgay;
                kq.TongGiaTri += lo.GiaTriTaiNgay;
            }
            return kq;
        }

        // Ngày mở sổ biến động: dòng LOAI = 'MO_SO' do script cài đặt tạo.
        private static DateTime? LayNgayMoSo()
        {
            DataService ds = new DataService();
            ds.Load(new OleDbCommand("SELECT MIN(NGAY) AS NGAY_MO FROM BIEN_DONG_KHO WHERE LOAI = 'MO_SO'"));
            if (ds.Rows.Count == 0 || ds.Rows[0][0] == DBNull.Value)
                return null;
            return Convert.ToDateTime(ds.Rows[0][0]);
        }
    }
}
