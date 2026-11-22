using CuahangNongduoc.Controller;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public partial class frmSanPham : BaseDanhMucForm
    {
        private enum ProductMode { View, Add, Edit }
        private ProductMode _mode;
        private DataRow _editingRow;
        private readonly ToolStripButton toolChinhSua = new ToolStripButton("S\u1eeda");
        private readonly ToolStripButton toolHuy = new ToolStripButton("H\u1ee7y");

        private readonly SanPhamController _ctrl;
        private readonly DonViTinhController _ctrlDVT;

        protected override BaseController Controller => _ctrl;
        protected override string TenDanhMuc => "Sản phẩm";

        public frmSanPham()
        {
            InitializeComponent();
            if (IsDesignMode) return;
            _ctrl = new SanPhamController();
            _ctrlDVT = new DonViTinhController();
            toolChinhSua.Name = "toolChinhSua";
            toolHuy.Name = "toolHuy";
            bindingNavigator.Items.Insert(bindingNavigator.Items.IndexOf(toolLuu), toolChinhSua);
            bindingNavigator.Items.Insert(bindingNavigator.Items.IndexOf(toolLuu) + 1, toolHuy);
            toolChinhSua.Click += delegate {
                if (!AccessControl.Can("SuaDanhMuc") || CurrentProduct == null) return;
                _editingRow = CurrentProduct.Row;
                SetProductMode(ProductMode.Edit);
                txtTenSanPham.Focus();
            };
            Activated += delegate { RefreshProductView(); };
            FormClosing += delegate(object sender, FormClosingEventArgs e) {
                if (_mode == ProductMode.View) return;
                DialogResult answer = MessageBox.Show("Bạn có muốn lưu thay đổi sản phẩm trước khi đóng?",
                    "Sản phẩm", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (answer == DialogResult.Cancel) { e.Cancel = true; return; }
                if (answer == DialogResult.Yes) {
                    try { SaveProduct(); }
                    catch (Exception ex) {
                        e.Cancel = true;
                        MessageBox.Show(ErrorLog.UserMessage(ex, "Không thể hoàn tất thao tác"), "Không thể lưu sản phẩm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else if (ProductSource != null) ProductTable.RejectChanges();
            };
            toolHuy.Click += delegate {
                ProductTable.RejectChanges();
                _editingRow = null;
                SetProductMode(ProductMode.View);
                ShowCurrentProduct();
            };

            CuahangNongduoc.ResponsiveLayout.Attach(this);
            base.dataGridView = this.dataGridView;
            base.bindingNavigator = this.bindingNavigator;
            base.bindingNavigatorPositionItem = this.bindingNavigatorPositionItem;
        }

        internal void RefreshProductView()
        {
            if (_mode != ProductMode.View || ProductSource == null) return;
            string selectedId = txtMaSanPham.Text;
            _ctrl.TaiNapSanPham();
            int position = ProductSource.Find("ID", selectedId);
            if (position >= 0) ProductSource.Position = position;
            ShowCurrentProduct();
        }

        protected override void LoadData()
        {
            _ctrlDVT.HienthiAutoComboBox(cmbDVT);
            dataGridView.Columns.Add(_ctrlDVT.HienthiDataGridViewComboBoxColumn());
            _ctrl.HienthiDataGridview(dataGridView, bindingNavigator,
                txtMaSanPham, txtTenSanPham, cmbDVT,
                numSoLuong, numDonGiaNhap, numGiaBanSi, numGiaBanLe);
            foreach (Control input in new Control[] { txtMaSanPham, txtTenSanPham, cmbDVT,
                numSoLuong, numDonGiaNhap, numGiaBanSi, numGiaBanLe }) input.DataBindings.Clear();
            bindingNavigator.BindingSource.CurrentChanged += delegate { ShowCurrentProduct(); };
            SetProductMode(ProductMode.View);
            ShowCurrentProduct();

        }

        private void frmSanPham_Load(object sender, EventArgs e)
        {
            Base_Load(sender, e);
        }

        private void toolLuu_Click(object sender, EventArgs e)
        {
            try { SaveProduct(); }
            catch (Exception ex) { MessageBox.Show(ErrorLog.UserMessage(ex, "Không thể hoàn tất thao tác"), "S\u1ea3n ph\u1ea9m", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void toolThoat_Click(object sender, EventArgs e)
        {
            Thoat();
        }

        private void bindingNavigatorDeleteItem_Click(object sender, EventArgs e)
        {
            if (_mode != ProductMode.View || CurrentProduct == null) return;
            try
            {
                AccessControl.DemandCurrent("SuaDanhMuc");
                AccessControl.DemandCurrent("XoaPhieu");
                if (MessageBox.Show("Bạn có chắc muốn xóa sản phẩm đang chọn?", "Xóa sản phẩm",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                CurrentProduct.Row.Delete();
                if (!_ctrl.Save()) throw new InvalidOperationException("Khong the xoa san pham.");
                ShowCurrentProduct();
            }
            catch (Exception ex)
            {
                ProductTable.RejectChanges();
                ShowCurrentProduct();
                MessageBox.Show(ErrorLog.UserMessage(ex, "Không thể hoàn tất thao tác"), "San pham", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void bindingNavigatorAddNewItem_Click(object sender, EventArgs e)
        {
            if (_mode != ProductMode.View || !AccessControl.CanCreateProduct()) return;
            _editingRow = null;
            SetProductMode(ProductMode.Add);
            txtMaSanPham.Text = IdGenerator.Next("SAN_PHAM");
            txtTenSanPham.Clear();
            cmbDVT.SelectedIndex = -1;
            numSoLuong.Value = numDonGiaNhap.Value = numGiaBanSi.Value = numGiaBanLe.Value = 0;
            txtMaSanPham.Focus();
        }

        private BindingSource ProductSource { get { return bindingNavigator.BindingSource; } }
        private DataTable ProductTable { get { return (DataTable)ProductSource.DataSource; } }
        private DataRowView CurrentProduct { get { return ProductSource.Current as DataRowView; } }

        internal void ApplyProductPermissions()
        {
            SetProductMode(_mode);
        }

        private void SetProductMode(ProductMode mode)
        {
            _mode = mode;
            bool editing = mode == ProductMode.Add ? AccessControl.CanCreateProduct() :
                mode == ProductMode.Edit && AccessControl.Can("SuaDanhMuc");
            dataGridView.ReadOnly = true;
            dataGridView.AllowUserToDeleteRows = false;
            dataGridView.Enabled = !editing;
            txtMaSanPham.Enabled = false;
            txtMaSanPham.ReadOnly = true;
            txtTenSanPham.Enabled = cmbDVT.Enabled = editing;
            numGiaBanSi.Enabled = numGiaBanLe.Enabled = editing && (mode == ProductMode.Add || AccessControl.Can("SuaGiaBan"));
            numSoLuong.Enabled = false;
            numDonGiaNhap.Enabled = editing && AccessControl.Can("XemGiaNhap");
            btnThemDVT.Enabled = editing;
            bindingNavigatorAddNewItem.Enabled = !editing && AccessControl.CanCreateProduct();
            bindingNavigatorDeleteItem.Enabled = !editing && CurrentProduct != null && AccessControl.Can("SuaDanhMuc");
            toolChinhSua.Enabled = !editing && CurrentProduct != null && AccessControl.Can("SuaDanhMuc");
            toolLuu.Enabled = toolHuy.Enabled = editing;
            foreach (ToolStripItem item in new ToolStripItem[] { bindingNavigatorMoveFirstItem,
                bindingNavigatorMovePreviousItem, bindingNavigatorMoveNextItem,
                bindingNavigatorMoveLastItem, bindingNavigatorPositionItem, toolTimSanPham, toolStripTextBox1 })
                item.Enabled = !editing;
        }

        private void ShowCurrentProduct()
        {
            if (_mode != ProductMode.View) return;
            DataRowView row = CurrentProduct;
            txtMaSanPham.Text = row == null ? "" : Convert.ToString(row["ID"]);
            txtTenSanPham.Text = row == null ? "" : Convert.ToString(row["TEN_SAN_PHAM"]);
            cmbDVT.SelectedIndex = -1;
            if (row != null) cmbDVT.SelectedValue = row["ID_DON_VI_TINH"];
            SetNumericValue(numSoLuong, row, "SO_LUONG");
            SetNumericValue(numDonGiaNhap, row, "DON_GIA_NHAP");
            SetNumericValue(numGiaBanSi, row, "GIA_BAN_SI");
            SetNumericValue(numGiaBanLe, row, "GIA_BAN_LE");
            SetProductMode(ProductMode.View);
        }

        private static void SetNumericValue(NumericUpDown input, DataRowView row, string column)
        {
            decimal value = row == null || row[column] == DBNull.Value ? 0 : Convert.ToDecimal(row[column]);
            input.Value = Math.Max(input.Minimum, Math.Min(input.Maximum, value));
        }

        private void SaveProduct()
        {
            if (_mode == ProductMode.View) return;
            if (_mode == ProductMode.Add) AccessControl.DemandCreateProduct();
            else AccessControl.DemandCurrent("SuaDanhMuc");
            string id = txtMaSanPham.Text.Trim();
            string name = txtTenSanPham.Text.Trim();
            if (id.Length == 0 || id.Length > 50 || name.Length == 0)
                throw new InvalidOperationException("Vui long nhap ma va ten san pham hop le.");
            if (cmbDVT.SelectedValue == null || cmbDVT.SelectedValue == DBNull.Value)
                throw new InvalidOperationException("Vui long chon don vi tinh.");
            bool adding = _mode == ProductMode.Add;
            foreach (DataRow other in ProductTable.Rows)
                if (other.RowState != DataRowState.Deleted && other != _editingRow &&
                    String.Equals(Convert.ToString(other["ID"]), id, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Ma san pham da ton tai.");
            if (_editingRow == null)
            {
                _editingRow = ProductTable.NewRow();
                _editingRow["ID"] = id;
                _editingRow["DON_GIA_NHAP"] = 0;
            }
            _editingRow["TEN_SAN_PHAM"] = name;
            _editingRow["ID_DON_VI_TINH"] = cmbDVT.SelectedValue;
            _editingRow["DON_GIA_NHAP"] = numDonGiaNhap.Value;
            _editingRow["GIA_BAN_SI"] = numGiaBanSi.Value;
            _editingRow["GIA_BAN_LE"] = numGiaBanLe.Value;
            if (adding && _editingRow.RowState == DataRowState.Detached) ProductTable.Rows.Add(_editingRow);
            if (!_ctrl.Save()) throw new InvalidOperationException("Khong the luu san pham.");
            _editingRow = null;
            SetProductMode(ProductMode.View);
            ProductSource.Position = ProductSource.Find("ID", id);
            ShowCurrentProduct();
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
            if (_mode == ProductMode.View) TimSanPham();
        }

        private void toolTimSanPham_Enter(object sender, EventArgs e)
        {
            toolTimSanPham.Text = "";
            toolTimSanPham.ForeColor = Color.Black;
        }

        private void TimSanPham()
        {
            if (_mode != ProductMode.View) return;
            if (toolTimMaSanPham.Checked)
                _ctrl.TimMaSanPham(toolTimSanPham.Text);
            else
                _ctrl.TimTenSanPham(toolTimSanPham.Text);
            ShowCurrentProduct();
        }
    }
}