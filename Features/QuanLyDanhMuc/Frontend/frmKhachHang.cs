using CuahangNongduoc.Controller;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    /// <summary>
    /// Form Khách hàng – kế thừa BaseDanhMucForm.
    /// </summary>
    public partial class frmKhachHang : BaseDanhMucForm
    {
        private readonly KhachHangController _ctrl = new KhachHangController();

        protected override BaseController Controller => _ctrl;
        protected override string TenDanhMuc => "Khách hàng";

        public frmKhachHang()
        {
            InitializeComponent();
            this.dataGridView = dataGridView;
            this.bindingNavigator = bindingNavigator;
            this.bindingNavigatorPositionItem = bindingNavigatorPositionItem;
        }

        protected override void LoadData()
        {
            // Logic load dữ liệu khách hàng (LoaiKH = false)
            _ctrl.HienthiKhachHangDataGridview(dataGridView, bindingNavigator);
        }

        private void frmKhachHang_Load(object sender, EventArgs e)
        {
            Base_Load(sender, e);
        }

        private void toolLuu_Click(object sender, EventArgs e)
        {
            Luu();
        }

        private void toolThoat_Click(object sender, EventArgs e)
        {
            Thoat();
        }

        private void bindingNavigatorDeleteItem_Click(object sender, EventArgs e)
        {
            XoaDongHienTai();
        }

        private void dataGridView_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            HuyXoaDong(e);
        }

        private void bindingNavigatorAddNewItem_Click(object sender, EventArgs e)
        {
            long maso = ThamSo.KhachHang;
            ThamSo.KhachHang = maso + 1;

            DataRowView row = (DataRowView)bindingNavigator.BindingSource.AddNew();
            row["ID"] = maso;
        }

        // ========== Tìm kiếm ==========

        private void toolTimHoTen_Click(object sender, EventArgs e)
        {
            toolTimDiaChi.Checked = !toolTimDiaChi.Checked;
            toolTimHoTen.Checked = !toolTimDiaChi.Checked;
            toolTimKhachHang.Text = "Tìm theo Họ tên";
            bindingNavigator.Focus();
        }

        private void toolTimDiaChi_Click(object sender, EventArgs e)
        {
            toolTimHoTen.Checked = !toolTimHoTen.Checked;
            toolTimDiaChi.Checked = !toolTimHoTen.Checked;
            toolTimKhachHang.Text = "Tìm theo Địa chỉ";
            bindingNavigator.Focus();
        }

        private void toolTimKhachHang_Enter(object sender, EventArgs e)
        {
            toolTimKhachHang.Text = string.Empty;
            toolTimKhachHang.ForeColor = Color.Black;
        }

        private void toolTimKhachHang_Leave(object sender, EventArgs e)
        {
            if (toolTimHoTen.Checked)
                toolTimKhachHang.Text = "Tìm theo Họ tên";
            else
                toolTimKhachHang.Text = "Tìm theo Địa chỉ";

            toolTimKhachHang.ForeColor = Color.FromArgb(224, 224, 224);
        }

        private void toolTimKhachHang_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (toolTimHoTen.Checked)
                    _ctrl.TimHoTen(toolTimKhachHang.Text, false);
                else
                    _ctrl.TimDiaChi(toolTimKhachHang.Text, false);
            }
        }
    }
}