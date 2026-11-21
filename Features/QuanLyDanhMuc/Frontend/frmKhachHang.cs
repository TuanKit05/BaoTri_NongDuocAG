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
            CuahangNongduoc.ResponsiveLayout.Attach(this);
            base.dataGridView = this.dataGridView;
            base.bindingNavigator = this.bindingNavigator;
            base.bindingNavigatorPositionItem = this.bindingNavigatorPositionItem;
        }

        protected override void DemandSavePermission()
        {
            if (AccessControl.Can("SuaDanhMuc")) base.DemandSavePermission();
            else
            {
                AccessControl.DemandCurrent("BanHang");
                AccessControl.DemandCurrent("KhachHang");
            }
        }

        internal void ConfigureCustomerCreation()
        {
            if (AccessControl.Can("SuaDanhMuc") || !AccessControl.CanAddCustomer()) return;
            dataGridView.ReadOnly = false;
            dataGridView.AllowUserToAddRows = false;
            dataGridView.AllowUserToDeleteRows = false;
            bindingNavigatorAddNewItem.Enabled = true;
            toolLuu.Enabled = true;
            colID.ReadOnly = true;
            colLoaiKH.ReadOnly = true;
            dataGridView.DataBindingComplete += delegate { UpdateCustomerRowPermissions(); };
            UpdateCustomerRowPermissions();
        }

        private void UpdateCustomerRowPermissions()
        {
            if (AccessControl.Can("SuaDanhMuc") || !AccessControl.CanAddCustomer()) return;
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                DataRowView item = row.DataBoundItem as DataRowView;
                row.ReadOnly = item == null || item.Row.RowState != DataRowState.Added;
            }
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
            dataGridView.EndEdit();
            Luu();
            UpdateCustomerRowPermissions();
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
            if (!AccessControl.Can("SuaDanhMuc"))
            {
                AccessControl.DemandCurrent("BanHang");
                AccessControl.DemandCurrent("KhachHang");
            }
            long maso = ThamSo.KhachHang;
            ThamSo.KhachHang = maso + 1;

            DataRowView row = (DataRowView)bindingNavigator.BindingSource.AddNew();
            row["ID"] = maso;
            row["LOAI_KH"] = false;
            UpdateCustomerRowPermissions();
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