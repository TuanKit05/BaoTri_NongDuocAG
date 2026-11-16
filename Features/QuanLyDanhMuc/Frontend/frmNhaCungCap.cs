using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using CuahangNongduoc.Controller;

namespace CuahangNongduoc
{
    public partial class frmNhaCungCap : BaseDanhMucForm
    {
        private readonly NhaCungCapController _ctrl = new NhaCungCapController();

        protected override BaseController Controller => _ctrl;
        protected override string TenDanhMuc => "Nhà cung cấp";

        public frmNhaCungCap()
        {
            InitializeComponent();
            this.dataGridView = dataGridView;
            this.bindingNavigator = bindingNavigator;
            this.bindingNavigatorPositionItem = bindingNavigatorPositionItem;
        }

        protected override void LoadData()
        {
            _ctrl.HienthiDataGridview(dataGridView, bindingNavigator);
        }

        private void frmNhaCungCap_Load(object sender, EventArgs e)
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
            long maso = ThamSo.NhaCungCap;
            ThamSo.NhaCungCap = maso + 1;

            DataRowView row = (DataRowView)bindingNavigator.BindingSource.AddNew();
            row["ID"] = maso;
        }

        private void toolTimHoTen_Click(object sender, EventArgs e)
        {
            toolTimDiaChi.Checked = !toolTimDiaChi.Checked;
            toolTimHoTen.Checked = !toolTimDiaChi.Checked;
            toolTimNhaCungCap.Text = "Tìm theo Nhà cung cấp";
            bindingNavigator.Focus();
        }

        private void toolTimDiaChi_Click(object sender, EventArgs e)
        {
            toolTimHoTen.Checked = !toolTimHoTen.Checked;
            toolTimDiaChi.Checked = !toolTimHoTen.Checked;
            toolTimNhaCungCap.Text = "Tìm theo Địa chỉ";
            bindingNavigator.Focus();
        }

        private void toolTimNhaCungCap_Enter(object sender, EventArgs e)
        {
            toolTimNhaCungCap.Text = "";
            toolTimNhaCungCap.ForeColor = Color.Black;
        }

        private void toolTimNhaCungCap_Leave(object sender, EventArgs e)
        {
            if (toolTimHoTen.Checked)
                toolTimNhaCungCap.Text = "Tìm theo Nhà cung cấp";
            else
                toolTimNhaCungCap.Text = "Tìm theo Địa chỉ";

            toolTimNhaCungCap.ForeColor = Color.FromArgb(224, 224, 224);
        }

        private void toolTimNhaCungCap_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (toolTimHoTen.Checked)
                    _ctrl.TimHoTen(toolTimNhaCungCap.Text);
                else
                    _ctrl.TimDiaChi(toolTimNhaCungCap.Text);
            }
        }
    }
}