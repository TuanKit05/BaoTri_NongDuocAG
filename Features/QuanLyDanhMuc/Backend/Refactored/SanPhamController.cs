using System.Data;
using System.Windows.Forms;
using CuahangNongduoc.DataLayer;

namespace CuahangNongduoc.Controller
{
    public class SanPhamController : BaseController
    {
        private readonly SanPhamFactory factory = new SanPhamFactory();

        protected override BaseFactory Factory => factory;

        public void HienthiDataGridview(DataGridView dg, BindingNavigator bn,
            TextBox txtMa, TextBox txtTen, ComboBox cmbDVT,
            NumericUpDown numSL, NumericUpDown numGiaNhap, NumericUpDown numGiaBanSi, NumericUpDown numGiaBanLe)
        {
            BindingSource bs = new BindingSource();
            DataTable tbl = factory.DanhSach();
            bs.DataSource = tbl;
            bn.BindingSource = bs;
            dg.DataSource = bs;

            // Bind dữ liệu chi tiết lên các control nếu có
            if (txtMa != null) txtMa.DataBindings.Clear();
            if (txtMa != null) txtMa.DataBindings.Add("Text", bs, "ID", true);

            if (txtTen != null) txtTen.DataBindings.Clear();
            if (txtTen != null) txtTen.DataBindings.Add("Text", bs, "TEN_SAN_PHAM", true);

            if (cmbDVT != null) cmbDVT.DataBindings.Clear();
            if (cmbDVT != null) cmbDVT.DataBindings.Add("SelectedValue", bs, "ID_DON_VI_TINH", true);

            if (numSL != null) numSL.DataBindings.Clear();
            if (numSL != null) numSL.DataBindings.Add("Value", bs, "SO_LUONG", true);

            if (numGiaNhap != null) numGiaNhap.DataBindings.Clear();
            if (numGiaNhap != null) numGiaNhap.DataBindings.Add("Value", bs, "DON_GIA_NHAP", true);

            if (numGiaBanSi != null) numGiaBanSi.DataBindings.Clear();
            if (numGiaBanSi != null) numGiaBanSi.DataBindings.Add("Value", bs, "GIA_BAN_SI", true);

            if (numGiaBanLe != null) numGiaBanLe.DataBindings.Clear();
            if (numGiaBanLe != null) numGiaBanLe.DataBindings.Add("Value", bs, "GIA_BAN_LE", true);
        }

        public void TimMaSanPham(string ma)
        {
            factory.TimMaSanPham(ma);
        }

        public void TimTenSanPham(string ten)
        {
            factory.TimTenSanPham(ten);
        }

        public override DataRow NewRow()
        {
            return factory.NewRow();
        }
    }
}