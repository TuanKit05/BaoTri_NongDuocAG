using CuahangNongduoc.Controller;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public partial class frmSanPham : BaseDanhMucForm
    {
        private readonly SanPhamController _ctrl = new SanPhamController();
        private readonly DonViTinhController _ctrlDVT = new DonViTinhController();

        protected override BaseController Controller => _ctrl;
        protected override string TenDanhMuc => "Sản phẩm";

        public frmSanPham()
        {
            InitializeComponent();
            this.dataGridView = dataGridView;
            this.bindingNavigator = bindingNavigator;
            this.bindingNavigatorPositionItem = bindingNavigatorPositionItem;
        }

        protected override void LoadData()
        {
            _ctrlDVT.HienthiAutoComboBox(cmbDVT);
            dataGridView.Columns.Add(_ctrlDVT.HienthiDataGridViewComboBoxColumn());
            _ctrl.HienthiDataGridview(dataGridView, bindingNavigator,
                txtMaSanPham, txtTenSanPham, cmbDVT,
                numSoLuong, numDonGiaNhap, numGiaBanSi, numGiaBanLe);
        }

        private void frmSanPham_Load(object sender, EventArgs e)
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

        private void bindingNavigatorAddNewItem_Click(object sender, EventArgs e)
        {
            DataRow row = _ctrl.NewRow();
            long maso = ThamSo.SanPham;
            ThamSo.SanPham = maso + 1;
            row["ID"] = maso;
            row["TEN_SAN_PHAM"] = "";
            row["SO_LUONG"] = 0;
            row["DON_GIA_NHAP"] = 0;
            row["GIA_BAN_SI"] = 0;
            row["GIA_BAN_LE"] = 0;
            _ctrl.Add(row);
            bindingNavigator.BindingSource.MoveLast();
        }

        private void dataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = true;
        }

        private void btnThemDVT_Click(object sender, EventArgs e)
        {
            frmDonViTinh dvt = new frmDonViTinh();
            dvt.ShowDialog();
            _ctrlDVT.HienthiAutoComboBox(cmbDVT);
        }

        private void toolTimMaSanPham_Click(object sender, EventArgs e)
        {
            toolTimMaSanPham.Checked = true;
            toolTimTenSanPham.Checked = false;
            toolTimSanPham.Text = "";
        }

        private void mnuTimTenSanPham_Click(object sender, EventArgs e)
        {
            toolTimMaSanPham.Checked = false;
            toolTimTenSanPham.Checked = true;
            toolTimSanPham.Text = "";
        }

        private void toolTimSanPham_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                TimSanPham();
        }

        private void toolTimSanPham_Leave(object sender, EventArgs e)
        {
            TimSanPham();
        }

        private void toolTimSanPham_Enter(object sender, EventArgs e)
        {
            toolTimSanPham.Text = "";
            toolTimSanPham.ForeColor = Color.Black;
        }

        private void TimSanPham()
        {
            if (toolTimMaSanPham.Checked)
                _ctrl.TimMaSanPham(toolTimSanPham.Text);
            else
                _ctrl.TimTenSanPham(toolTimSanPham.Text);
        }
    }
}