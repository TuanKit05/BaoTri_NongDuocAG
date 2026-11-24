using CuahangNongduoc.Controller;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public partial class frmDaiLy : BaseDanhMucForm
    {
        private readonly KhachHangController _ctrl;

        protected override BaseController Controller => _ctrl;
        protected override string TenDanhMuc => "Đại lý";

        public frmDaiLy()
        {
            InitializeComponent();
            if (IsDesignMode) return;
            _ctrl = new KhachHangController();
            CuahangNongduoc.ResponsiveLayout.Attach(this);
            this.dataGridView.CellValidating += ValidatePhoneNumber;
            base.dataGridView = this.dataGridView;
            base.bindingNavigator = this.bindingNavigator;
            base.bindingNavigatorPositionItem = this.bindingNavigatorPositionItem;
        }

        private void ValidatePhoneNumber(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.RowIndex < 0 || dataGridView.Columns[e.ColumnIndex].DataPropertyName != "DIEN_THOAI") return;
            bool valid = CuahangNongduoc.DataLayer.KhachHangFactory.IsValidPhoneNumber(Convert.ToString(e.FormattedValue));
            e.Cancel = !valid;
            dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].ErrorText = valid ? String.Empty :
                "Số điện thoại chỉ được chứa chữ số từ 0 đến 9.";
            if (!valid)
                MessageBox.Show("Số điện thoại chỉ được chứa chữ số từ 0 đến 9.", "Số điện thoại không hợp lệ",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        protected override void LoadData()
        {
            _ctrl.HienthiDaiLyDataGridview(dataGridView, bindingNavigator);
        }

        private void frmDaiLy_Load(object sender, EventArgs e)
        {
            Base_Load(sender, e);
        }

        private void toolLuu_Click(object sender, EventArgs e)
        {
            if (!dataGridView.EndEdit()) return;
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
            string maso = IdGenerator.Next("KHACH_HANG");

            DataRowView row = (DataRowView)bindingNavigator.BindingSource.AddNew();
            row["ID"] = maso;
            row["LOAI_KH"] = true;
        }

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
            toolTimKhachHang.Text = "";
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
                    _ctrl.TimHoTen(toolTimKhachHang.Text, true);
                else
                    _ctrl.TimDiaChi(toolTimKhachHang.Text, true);
            }
        }
    }
}