using System;
using System.Data;
using System.Windows.Forms;
using CuahangNongduoc.DataLayer;
using CuahangNongduoc.BusinessObject;

namespace CuahangNongduoc.Controller
{
    public class PhieuChiController
    {
        private readonly PhieuChiFactory factory = new PhieuChiFactory();

        public DataRow NewRow() { return factory.NewRow(); }
        public void Add(DataRow row) { factory.Add(row); }
        public void Save() { factory.Save(); }

        public PhieuChi LayPhieuChi(String id)
        {
            DataTable bang = factory.LayPhieuChi(id);
            if (bang.Rows.Count == 0)
                return null; // Replace Nested Conditional with Guard Clauses

            return TaoPhieuChi(bang.Rows[0]);
        }

        // Extract Method: chuyen mapping DataRow thanh nghiep vu rieng.
        private static PhieuChi TaoPhieuChi(DataRow row)
        {
            PhieuChi phieu = new PhieuChi(Convert.ToString(row["ID"]));
            LyDoChiController lyDo = new LyDoChiController();
            phieu.LyDoChi = lyDo.LayLyDoChi(Convert.ToInt64(row["ID_LY_DO_CHI"]));
            phieu.NgayChi = Convert.ToDateTime(row["NGAY_CHI"]);
            phieu.TongTien = Convert.ToInt64(row["TONG_TIEN"]);
            phieu.GhiChu = Convert.ToString(row["GHI_CHU"]);
            return phieu;
        }

        public void HienthiPhieuChi(BindingNavigator bn, DataGridView dg,
            ComboBox cmb, TextBox txt, DateTimePicker dt,
            NumericUpDown numTongTien, TextBox txtGhichu)
        {
            ThuChiBinding.HienThi(
                new ThuChiControls(bn, dg, cmb, txt, dt, numTongTien, txtGhichu),
                factory.DanhsachPhieuChi(), "ID_LY_DO_CHI", "NGAY_CHI");
        }

        public void TimPhieuChi(BindingNavigator bn, DataGridView dg,
            ComboBox cmb, TextBox txt, DateTimePicker dt,
            NumericUpDown numTongTien, TextBox txtGhichu,
            int lydo, DateTime ngay)
        {
            ThuChiBinding.HienThi(
                new ThuChiControls(bn, dg, cmb, txt, dt, numTongTien, txtGhichu),
                factory.TimPhieuChi(lydo, ngay), "ID_LY_DO_CHI", "NGAY_CHI");
        }
    }
}
