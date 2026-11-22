using CuahangNongduoc.Controller;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public partial class frmDonViTinh : BaseDanhMucForm
    {
        private readonly DonViTinhController _ctrl;
        private enum UnitMode { View, Add, Edit }
        private UnitMode mode;
        private DataRow editingRow;
        private readonly ToolStripButton toolChinhSua = new ToolStripButton("Sửa");
        private readonly ToolStripButton toolHuy = new ToolStripButton("Hủy");
        protected override BaseController Controller { get { return _ctrl; } }
        protected override string TenDanhMuc { get { return "Đơn vị tính"; } }
        private BindingSource Source { get { return bindingNavigator.BindingSource; } }
        private DataTable UnitTable { get { return Source.DataSource as DataTable; } }

        public frmDonViTinh()
        {
            InitializeComponent();
            if (IsDesignMode) return;
            _ctrl = new DonViTinhController();
            Text = "Đơn vị tính";
            ClientSize = new Size(560, 360);
            bindingNavigator.AddNewItem = null;
            bindingNavigator.DeleteItem = null;
            toolChinhSua.Name = "toolChinhSua";
            toolChinhSua.Image = Properties.Resources.edit;
            toolChinhSua.TextImageRelation = TextImageRelation.ImageAboveText;
            toolChinhSua.Overflow = ToolStripItemOverflow.Never;
            toolHuy.Name = "toolHuy";
            toolHuy.TextImageRelation = TextImageRelation.ImageAboveText;
            bindingNavigator.Items.Insert(bindingNavigator.Items.IndexOf(toolLuu), toolChinhSua);
            bindingNavigator.Items.Insert(bindingNavigator.Items.IndexOf(toolLuu) + 1, toolHuy);
            bindingNavigatorAddNewItem.Click += delegate { TryAction(ThemDonViTinh); };
            bindingNavigatorDeleteItem.Click += delegate { TryAction(XoaDonViTinh); };
            toolChinhSua.Click += delegate { TryAction(SuaDonViTinh); };
            toolHuy.Click += delegate { HuyDonViTinh(); };
            dataGridView.AutoGenerateColumns = false;
            dataGridView.AllowUserToAddRows = dataGridView.AllowUserToDeleteRows = false;
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView.MultiSelect = false;
            colId.ReadOnly = true;
            colTenDV.HeaderText = "Tên đơn vị tính";
            base.dataGridView = dataGridView;
            base.bindingNavigator = bindingNavigator;
            ResponsiveLayout.Attach(this);
            FormClosing += delegate(object sender, FormClosingEventArgs e) {
                if (mode == UnitMode.View) return;
                DialogResult answer = MessageBox.Show("Bạn có muốn lưu thay đổi đơn vị tính?", Text,
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (answer == DialogResult.Cancel) { e.Cancel = true; return; }
                if (answer == DialogResult.No) { HuyDonViTinh(); return; }
                try { LuuDonViTinh(); }
                catch (Exception error) { e.Cancel = true; ShowError(error); }
            };
        }

        private void TryAction(MethodInvoker action)
        {
            try { action(); } catch (Exception error) { ShowError(error); }
        }

        private void ShowError(Exception error)
        {
            MessageBox.Show(ErrorLog.UserMessage(error, "Không thể xử lý đơn vị tính"), Text,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void frmDonViTinh_Load(object sender, EventArgs e)
        {
            Base_Load(sender, e);
            Source.CurrentChanged += delegate { if (mode == UnitMode.View) SetMode(UnitMode.View); };
            SetMode(UnitMode.View);
        }

        private void SetMode(UnitMode value)
        {
            mode = value;
            bool editing = value != UnitMode.View;
            bool allowed = AccessControl.Can("SuaDanhMuc");
            bool selected = Source != null && Source.Current != null;
            bindingNavigatorAddNewItem.Enabled = !editing && allowed;
            bindingNavigatorDeleteItem.Enabled = !editing && selected && allowed;
            toolChinhSua.Enabled = !editing && selected && allowed;
            toolLuu.Enabled = toolHuy.Enabled = editing && allowed;
            dataGridView.ReadOnly = !editing;
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                DataRowView item = row.DataBoundItem as DataRowView;
                row.ReadOnly = !editing || item == null || item.Row != editingRow;
            }
            foreach (ToolStripItem item in new ToolStripItem[] { bindingNavigatorMoveFirstItem,
                bindingNavigatorMovePreviousItem, bindingNavigatorMoveNextItem, bindingNavigatorMoveLastItem,
                bindingNavigatorPositionItem }) item.Enabled = !editing;
        }

        internal void ThemDonViTinh()
        {
            if (mode != UnitMode.View) return;
            AccessControl.DemandCurrent("SuaDanhMuc");
            mode = UnitMode.Add;
            DataRowView item = (DataRowView)Source.AddNew();
            editingRow = item.Row;
            item["TEN_DON_VI"] = String.Empty;
            SetMode(UnitMode.Add);
            BatDauNhap();
        }

        internal void SuaDonViTinh()
        {
            if (mode != UnitMode.View || Source.Current == null) return;
            AccessControl.DemandCurrent("SuaDanhMuc");
            editingRow = ((DataRowView)Source.Current).Row;
            SetMode(UnitMode.Edit);
            BatDauNhap();
        }

        private void BatDauNhap()
        {
            if (dataGridView.CurrentRow == null) return;
            dataGridView.CurrentCell = dataGridView.CurrentRow.Cells[colTenDV.Index];
            dataGridView.Focus();
            dataGridView.BeginEdit(true);
        }

        internal void LuuDonViTinh()
        {
            if (mode == UnitMode.View) return;
            AccessControl.DemandCurrent("SuaDanhMuc");
            if (!Validate() || !dataGridView.EndEdit())
                throw new InvalidOperationException("Vui lòng hoàn tất tên đơn vị tính.");
            Source.EndEdit();
            string name = Convert.ToString(editingRow["TEN_DON_VI"]).Trim();
            if (name.Length == 0) throw new InvalidOperationException("Vui lòng nhập tên đơn vị tính.");
            int limit = UnitTable.Columns["TEN_DON_VI"].MaxLength;
            if (limit > 0 && name.Length > limit) throw new InvalidOperationException("Tên đơn vị tính quá dài.");
            editingRow["TEN_DON_VI"] = name;
            if (!_ctrl.Save()) throw new InvalidOperationException("Không lưu được đơn vị tính.");
            object id = editingRow["ID"];
            editingRow = null;
            SetMode(UnitMode.View);
            Source.Position = Source.Find("ID", id);
            Source.ResetCurrentItem();
        }

        internal void HuyDonViTinh()
        {
            dataGridView.CancelEdit();
            if (Source != null) { Source.CancelEdit(); UnitTable.RejectChanges(); }
            editingRow = null;
            SetMode(UnitMode.View);
        }

        private void XoaDonViTinh()
        {
            if (mode != UnitMode.View || Source.Current == null) return;
            AccessControl.DemandCurrent("SuaDanhMuc");
            AccessControl.DemandCurrent("XoaPhieu");
            if (MessageBox.Show("Bạn có chắc muốn xóa đơn vị tính đang chọn?", Text,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                ((DataRowView)Source.Current).Row.Delete();
                if (!_ctrl.Save()) throw new InvalidOperationException("Không xóa được đơn vị tính.");
            }
            catch { UnitTable.RejectChanges(); throw; }
            SetMode(UnitMode.View);
        }

        private void toolLuu_Click(object sender, EventArgs e)
        {
            try
            {
                LuuDonViTinh();
                MessageBox.Show("Đã lưu đơn vị tính thành công.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception error) { ShowError(error); }
        }
    }
}
