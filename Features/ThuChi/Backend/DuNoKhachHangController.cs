using System;
using System.Data;
using System.Windows.Forms;
using CuahangNongduoc.BusinessObject;
using CuahangNongduoc.DataLayer;

namespace CuahangNongduoc.Controller
{
    public class DuNoKhachHangController
    {
        private readonly DuNoKhachHangFactory factory = new DuNoKhachHangFactory();

        // Rename Method: Tonghop -> TongHop; cac noi goi da duoc cap nhat.
        public void TongHop(int thang, int nam, ToolStripProgressBar bar,
            DataGridView dg, BindingNavigator bn)
        {
            int thangTruoc = 0;
            int namTruoc = 0;
            ThamSo.PreMonth(ref thangTruoc, ref namTruoc, thang, nam);
            factory.Clear(thang, nam);

            BindingSource source = new BindingSource();
            source.DataSource = factory.LayDuNoKhachHang("-1", 0, 0);
            bn.BindingSource = source;
            dg.DataSource = source;

            KhachHangFactory khachHang = new KhachHangFactory();
            DataTable danhSach = khachHang.DanhsachKhachHang();
            bar.Minimum = 0;
            bar.Value = 0;
            bar.Maximum = danhSach.Rows.Count;

            foreach (DataRow khRow in danhSach.Rows)
            {
                String maKhachHang = Convert.ToString(khRow["ID"]);
                DuNoKhachHang congNo = TinhCongNo(maKhachHang,
                    thangTruoc, namTruoc, thang, nam);
                DataRow dongDuNo = factory.NewRow();
                GhiDuNo(dongDuNo, maKhachHang, thang, nam, congNo);
                factory.Add(dongDuNo);
                bar.Value++;
            }
        }

        // Separate Query from Modifier: lay so lieu, tinh cong no, khong ghi DataRow.
        private static DuNoKhachHang TinhCongNo(String maKhachHang,
            int thangTruoc, int namTruoc, int thang, int nam)
        {
            DuNoKhachHang result = new DuNoKhachHang();
            result.DauKy = DuNoKhachHangFactory.LayDuNo(maKhachHang, thangTruoc, namTruoc);
            result.PhatSinh = PhieuBanFactory.LayConNo(maKhachHang, thang, nam);
            result.DaTra = PhieuThanhToanFactory.LayTongTien(maKhachHang, thang, nam);
            result.CuoiKy = result.TinhCuoiKy();
            return result;
        }

        // Modifier tach rieng: chi thay doi DataRow dinh gui vao DataService.
        private static void GhiDuNo(DataRow row, String maKhachHang,
            int thang, int nam, DuNoKhachHang congNo)
        {
            row["ID_KHACH_HANG"] = maKhachHang;
            row["THANG"] = thang;
            row["NAM"] = nam;
            row["DAU_KY"] = congNo.DauKy;
            row["PHAT_SINH"] = congNo.PhatSinh;
            row["DA_TRA"] = congNo.DaTra;
            row["CUOI_KY"] = congNo.CuoiKy;
        }

        // Giu ten cu cho cac noi goi ngoai du an co the dang su dung.
        [Obsolete("Su dung TongHop thay cho Tonghop.")]
        public void Tonghop(int thang, int nam, ToolStripProgressBar bar,
            DataGridView dg, BindingNavigator bn)
        {
            TongHop(thang, nam, bar, dg, bn);
        }

        public bool Save()
        {
            return factory.Save();
        }
    }
}
