using System;
using System.Data;
using System.Windows.Forms;

namespace CuahangNongduoc.Controller
{
    // Introduce Parameter Object: gom 7 controls thuong duoc chuyen cung nhau.
    internal sealed class ThuChiControls
    {
        internal readonly BindingNavigator Navigator;
        internal readonly DataGridView Grid;
        internal readonly ComboBox DoiTuong;
        internal readonly TextBox MaPhieu;
        internal readonly DateTimePicker Ngay;
        internal readonly NumericUpDown SoTien;
        internal readonly TextBox GhiChu;

        internal ThuChiControls(BindingNavigator navigator, DataGridView grid,
            ComboBox doiTuong, TextBox maPhieu, DateTimePicker ngay,
            NumericUpDown soTien, TextBox ghiChu)
        {
            Navigator = navigator;
            Grid = grid;
            DoiTuong = doiTuong;
            MaPhieu = maPhieu;
            Ngay = ngay;
            SoTien = soTien;
            GhiChu = ghiChu;
        }
    }

    // Move Method / Extract Class: chuyen phan lien ket giao dien ra khoi 2 controller.
    internal static class ThuChiBinding
    {
        internal static void HienThi(ThuChiControls controls, DataTable rows,
            String cotDoiTuong, String cotNgay)
        {
            BindingSource source = new BindingSource();
            source.DataSource = rows;
            controls.Navigator.BindingSource = source;
            controls.Grid.DataSource = source;

            GanLienKet(controls.MaPhieu, "Text", source, "ID");
            GanLienKet(controls.DoiTuong, "SelectedValue", source, cotDoiTuong);
            GanLienKet(controls.Ngay, "Value", source, cotNgay);
            GanLienKet(controls.SoTien, "Value", source, "TONG_TIEN");
            GanLienKet(controls.GhiChu, "Text", source, "GHI_CHU");
        }

        private static void GanLienKet(Control control, String tenThuocTinh,
            BindingSource source, String tenCot)
        {
            control.DataBindings.Clear();
            control.DataBindings.Add(tenThuocTinh, source, tenCot);
        }
    }
}
