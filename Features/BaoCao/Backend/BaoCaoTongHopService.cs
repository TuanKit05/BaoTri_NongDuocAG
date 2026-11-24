using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using CuahangNongduoc.Features.BaoCao.Backend;

namespace CuahangNongduoc
{
    // Đối tượng lưu kết quả một chỉ tiêu báo cáo
    public sealed class KetQuaChiTieu
    {
        private string m_Ten;
        private decimal? m_SoTien;

        public string Ten
        {
            get { return m_Ten; }
            set { m_Ten = value; }
        }

        public string CanhBao { get; set; }

        public decimal? SoTien
        {
            get { return m_SoTien; }
            set { m_SoTien = value; }
        }
    }

    // Service xử lý dữ liệu báo cáo tổng hợp (YC-BC-01)
    public sealed class BaoCaoTongHopService
    {
        public IList<KetQuaChiTieu> LayThongKe(
            DateTime tuNgay,
            DateTime denNgay)
        {
            AccessControl.DemandCurrent(AccessControl.Can("BaoCaoBanHang")?"BaoCaoBanHang":"BaoCaoTonKho");
            if (tuNgay.Date > denNgay.Date)
            {
                throw new ArgumentException(
                    "Từ ngày không được lớn hơn đến ngày.");
            }

            DateTime tu = tuNgay.Date;
            DateTime den = denNgay.Date.AddDays(1);
            List<KetQuaChiTieu> ketQua = new List<KetQuaChiTieu>();

            if(AccessControl.Can("BaoCaoBanHang")) {
                ketQua.Add(ChiTieu("Doanh số hóa đơn (sau ưu đãi, gồm phí và dịch vụ)",
                    "SELECT SUM(PB.TONG_TIEN) FROM PHIEU_BAN PB WHERE PB.NGAY_BAN >= ? AND PB.NGAY_BAN < ?",tu,den));
                ketQua.Add(ChiTieu("Đã trả lũy kế của các hóa đơn trong kỳ (không phải thu tiền riêng trong kỳ)",
                    "SELECT SUM(PB.DA_TRA) FROM PHIEU_BAN PB WHERE PB.NGAY_BAN >= ? AND PB.NGAY_BAN < ?",tu,den));
                ketQua.Add(ChiTieu("Còn nợ hiện tại của các hóa đơn trong kỳ",
                    "SELECT SUM(PB.CON_NO) FROM PHIEU_BAN PB WHERE PB.NGAY_BAN >= ? AND PB.NGAY_BAN < ?",tu,den));
                if(AccessControl.Can("ThuChi")) ketQua.Add(ChiTieu("Tiền thu theo ngày phiếu thu trong kỳ (toàn cửa hàng)",
                    "SELECT SUM(TONG_TIEN) FROM PHIEU_THANH_TOAN WHERE NGAY_THANH_TOAN >= ? AND NGAY_THANH_TOAN < ?",tu,den));

                ketQua.Add(ChiTieu("Phí vận chuyển",
                    "SELECT SUM(PB.TIEN_VAN_CHUYEN) FROM PHIEU_BAN PB " +
                    "WHERE PB.NGAY_BAN >= ? AND PB.NGAY_BAN < ?", tu, den));

                ketQua.Add(ChiTieu("Dịch vụ phụ",
                    "SELECT SUM(DV.THANH_TIEN) FROM CHI_TIET_DICH_VU DV " +
                    "INNER JOIN PHIEU_BAN PB ON DV.ID_PHIEU_BAN = PB.ID " +
                    "WHERE PB.NGAY_BAN >= ? AND PB.NGAY_BAN < ?", tu, den));

                ketQua.Add(ChiTieu("Giảm giá",
                    "SELECT SUM(PB.TIEN_GIAM_GIA) FROM PHIEU_BAN PB " +
                    "WHERE PB.NGAY_BAN >= ? AND PB.NGAY_BAN < ?", tu, den));

                if(AccessControl.Can("XemGiaVon")) ketQua.Add(ChiTieu("Khuyến mãi (giá vốn hàng tặng)",
                    "SELECT SUM(CT.THANH_TIEN_GIA_VON) AS GIA_VON_TANG FROM CHI_TIET_PHIEU_BAN CT " +
                    "INNER JOIN PHIEU_BAN PB ON CT.ID_PHIEU_BAN = PB.ID " +
                    "WHERE CT.LA_HANG_TANG = True AND PB.NGAY_BAN >= ? AND PB.NGAY_BAN < ?", tu, den));

            }
            if(AccessControl.Can("BaoCaoTonKho") && AccessControl.Can("XemGiaNhap")) ketQua.Add(TonKhoCuoiKy(denNgay.Date));
            return ketQua;
        }

        private static KetQuaChiTieu ChiTieu(string ten, string sql, DateTime tu, DateTime den)
        {
            OleDbCommand cmd = new OleDbCommand(sql);
            cmd.Parameters.Add("tu", OleDbType.Date).Value = tu;
            cmd.Parameters.Add("den", OleDbType.Date).Value = den;
            DataService ds = new DataService();
            ds.Load(cmd);

            decimal tong = 0;
            if (ds.Rows.Count > 0 && ds.Rows[0][0] != DBNull.Value)
                tong = Convert.ToDecimal(ds.Rows[0][0]);

            KetQuaChiTieu kq = new KetQuaChiTieu();
            kq.Ten = ten;
            kq.SoTien = tong;
            return kq;
        }

        // Tồn kho cuối kỳ theo giá nhập. Nếu chưa có sổ biến động thì để trống, không tạo số liệu giả.
        private static KetQuaChiTieu TonKhoCuoiKy(DateTime ngay)
        {
            KetQuaChiTieu kq = new KetQuaChiTieu();
            kq.Ten = "Tồn kho cuối kỳ (giá nhập)";
            KetQuaTonKho inventory=new TonKhoTaiNgayService().LayTonKhoTaiNgay(ngay);
            kq.CanhBao=inventory.CanhBao;
            kq.SoTien=inventory.DuDuLieu?(decimal?)inventory.TongGiaTri:null;
            return kq;
        }
    }
}
