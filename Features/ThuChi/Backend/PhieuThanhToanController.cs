using System;
using System.Data;
using System.Windows.Forms;
using CuahangNongduoc.DataLayer;
using CuahangNongduoc.BusinessObject;

namespace CuahangNongduoc.Controller
{
    public class PhieuThanhToanController
    {
        private readonly PhieuThanhToanFactory factory = new PhieuThanhToanFactory();

        public DataRow NewRow() { return factory.NewRow(); }
        public void Add(DataRow row) { factory.Add(row); }
        public void Save() { factory.Save(); }

        public PhieuThanhToan LayPhieuThanhToan(String id)
        {
            DataTable bang = factory.LayPhieuThanhToan(id);
            if (bang.Rows.Count == 0)
                return null;

            return TaoPhieuThanhToan(bang.Rows[0]);
        }

        private static PhieuThanhToan TaoPhieuThanhToan(DataRow row)
        {
            PhieuThanhToan phieu = new PhieuThanhToan(Convert.ToString(row["ID"]));
            KhachHangController khachHang = new KhachHangController();
            phieu.KhachHang = khachHang.LayKhachHang(Convert.ToString(row["ID_KHACH_HANG"]));
            phieu.NgayThanhToan = Convert.ToDateTime(row["NGAY_THANH_TOAN"]);
            phieu.TongTien = Convert.ToInt64(row["TONG_TIEN"]);
            phieu.GhiChu = Convert.ToString(row["GHI_CHU"]);
            return phieu;
        }

        public void HienthiPhieuThanhToan(BindingNavigator bn, DataGridView dg,
            ComboBox cmb, TextBox txt, DateTimePicker dt,
            NumericUpDown numTongTien, TextBox txtGhichu)
        {
            ThuChiBinding.HienThi(
                new ThuChiControls(bn, dg, cmb, txt, dt, numTongTien, txtGhichu),
                factory.DanhsachPhieuThanhToan(), "ID_KHACH_HANG", "NGAY_THANH_TOAN");
        }

        public void TimPhieuThanhToan(BindingNavigator bn, DataGridView dg,
            ComboBox cmb, TextBox txt, DateTimePicker dt,
            NumericUpDown numTongTien, TextBox txtGhichu,
            String idKH, DateTime dtNgayThu)
        {
            ThuChiBinding.HienThi(
                new ThuChiControls(bn, dg, cmb, txt, dt, numTongTien, txtGhichu),
                factory.TimPhieuThanhToan(idKH, dtNgayThu),
                "ID_KHACH_HANG", "NGAY_THANH_TOAN");
        }
    }
}
