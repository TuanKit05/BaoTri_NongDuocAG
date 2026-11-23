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
    internal sealed class ThuChiEditor
    {
        internal delegate DataRow NewReceiptRow();
        internal delegate void SaveReceipt();
        private readonly ThuChiControls controls;
        private readonly string objectColumn, dateColumn;
        private readonly NewReceiptRow newRow;
        private readonly Action<DataRow> addRow;
        private readonly SaveReceipt save;
        private readonly ToolStripButton add, saveButton, delete, print, search;
        private readonly ToolStripButton edit = new ToolStripButton("Sửa");
        private readonly ToolStripButton cancel = new ToolStripButton("Hủy");
        private BindingSource source;
        private DataRow draft;
        private bool adding;
        internal bool IsEditing { get { return draft != null; } }

        internal ThuChiEditor(ThuChiControls controls, string objectColumn, string dateColumn,
            ToolStripButton add, ToolStripButton saveButton, ToolStripButton delete,
            ToolStripButton print, ToolStripButton search,
            NewReceiptRow newRow, Action<DataRow> addRow, SaveReceipt save)
        {
            this.controls = controls; this.objectColumn = objectColumn; this.dateColumn = dateColumn;
            this.add = add; this.saveButton = saveButton; this.delete = delete;
            this.print = print; this.search = search;
            this.newRow = newRow; this.addRow = addRow; this.save = save;
            edit.Name = "toolEdit"; cancel.Name = "toolCancel";
            controls.Navigator.Items.Insert(controls.Navigator.Items.IndexOf(saveButton), edit);
            controls.Navigator.Items.Insert(controls.Navigator.Items.IndexOf(saveButton) + 1, cancel);
            edit.Click += delegate { BeginEdit(); };
            cancel.Click += delegate { Cancel(); };
            controls.MaPhieu.ReadOnly = true;
            controls.MaPhieu.Enabled = false;
            controls.MaPhieu.BackColor = System.Drawing.SystemColors.Control;
            controls.Grid.ReadOnly = true;
            controls.Grid.AllowUserToAddRows = false;
            controls.Grid.AllowUserToDeleteRows = false;
            controls.Grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            controls.Grid.MultiSelect = false;
            controls.DoiTuong.AutoCompleteSource = AutoCompleteSource.ListItems;
            controls.DoiTuong.DropDownStyle = ComboBoxStyle.DropDownList;
            controls.Ngay.Format = DateTimePickerFormat.Custom;
            controls.Ngay.ValueChanged += delegate { if (IsEditing) controls.Ngay.CustomFormat = "dd/MM/yyyy"; };
            controls.Ngay.CloseUp += delegate { if (IsEditing) controls.Ngay.CustomFormat = "dd/MM/yyyy"; };
            controls.Ngay.KeyDown += delegate(object sender, KeyEventArgs e) {
                if (IsEditing && e.KeyCode != Keys.Tab) controls.Ngay.CustomFormat = "dd/MM/yyyy";
            };
            RefreshSource();
        }

        internal void RefreshSource()
        {
            if (source != null) { source.PositionChanged -= PositionChanged; source.ListChanged -= ListChanged; }
            foreach (Control control in new Control[] { controls.MaPhieu, controls.DoiTuong, controls.Ngay, controls.SoTien, controls.GhiChu })
                control.DataBindings.Clear();
            source = controls.Navigator.BindingSource;
            source.PositionChanged += PositionChanged; source.ListChanged += ListChanged;
            ShowCurrent();
        }
        private void PositionChanged(object sender, EventArgs e) { ShowCurrent(); }
        private void ListChanged(object sender, System.ComponentModel.ListChangedEventArgs e) { ShowCurrent(); }
        private void ShowCurrent()
        {
            if (IsEditing) return;
            DataRowView current = source.Current as DataRowView;
            if (current == null) ClearInputs(""); else LoadRow(current.Row);
            SetState();
        }
        private void ClearInputs(string id)
        {
            controls.MaPhieu.Text = id;
            controls.DoiTuong.SelectedIndex = -1;
            controls.Ngay.Value = DateTime.Today;
            controls.Ngay.CustomFormat = " ";
            controls.SoTien.Value = 0;
            controls.SoTien.Text = "";
            controls.GhiChu.Clear();
        }
        private void LoadRow(DataRow row)
        {
            controls.MaPhieu.Text = Convert.ToString(row["ID"]);
            controls.DoiTuong.SelectedValue = row[objectColumn];
            if (row[dateColumn] == DBNull.Value) controls.Ngay.CustomFormat = " ";
            else { controls.Ngay.Value = Convert.ToDateTime(row[dateColumn]); controls.Ngay.CustomFormat = "dd/MM/yyyy"; }
            decimal amount = row["TONG_TIEN"] == DBNull.Value ? 0 : Convert.ToDecimal(row["TONG_TIEN"]);
            controls.SoTien.Value = Math.Max(controls.SoTien.Minimum, Math.Min(controls.SoTien.Maximum, amount));
            controls.SoTien.Text = amount.ToString("N0");
            controls.GhiChu.Text = Convert.ToString(row["GHI_CHU"]);
        }
        private void SetState()
        {
            bool editing = IsEditing, canWrite = AccessControl.Can("ThuChi");
            controls.DoiTuong.Enabled = controls.Ngay.Enabled = controls.SoTien.Enabled = controls.GhiChu.Enabled = editing && canWrite;
            controls.Grid.Enabled = !editing;
            add.Enabled = !editing && canWrite;
            edit.Enabled = !editing && source.Current != null && canWrite;
            saveButton.Enabled = cancel.Enabled = editing && canWrite;
            delete.Enabled = !editing && source.Current != null && AccessControl.Can("XoaPhieu");
            print.Enabled = !editing && source.Current != null;
            search.Enabled = !editing;
            foreach (ToolStripItem item in controls.Navigator.Items)
                if (item.Name.StartsWith("bindingNavigatorMove") || item is ToolStripTextBox) item.Enabled = !editing;
        }
        internal void BeginAdd()
        {
            if (IsEditing) return;
            AccessControl.DemandCurrent("ThuChi");
            DataRow row = newRow();
            draft = row; adding = true;
            ClearInputs(Convert.ToString(row["ID"]));
            SetState(); controls.DoiTuong.Focus();
        }
        internal void BeginEdit()
        {
            if (IsEditing) return;
            AccessControl.DemandCurrent("ThuChi");
            DataRowView current = source.Current as DataRowView;
            if (current == null) return;
            draft = current.Row; adding = false;
            LoadRow(draft); SetState(); controls.DoiTuong.Focus();
        }
        internal void Save()
        {
            if (!IsEditing) { save(); ShowCurrent(); return; }
            AccessControl.DemandCurrent("ThuChi");
            if (controls.DoiTuong.SelectedValue == null || controls.DoiTuong.SelectedValue == DBNull.Value)
                throw new InvalidOperationException("Vui lòng chọn " + (objectColumn == "ID_KHACH_HANG" ? "khách hàng." : "lý do chi."));
            if (controls.Ngay.CustomFormat.Trim().Length == 0)
                throw new InvalidOperationException("Vui lòng chọn ngày lập phiếu.");
            if (controls.SoTien.Text.Trim().Length == 0 || controls.SoTien.Value <= 0)
                throw new InvalidOperationException("Vui lòng nhập tổng tiền lớn hơn 0.");
            draft[objectColumn] = controls.DoiTuong.SelectedValue;
            draft[dateColumn] = controls.Ngay.Value.Date;
            draft["TONG_TIEN"] = controls.SoTien.Value;
            draft["GHI_CHU"] = controls.GhiChu.Text.Trim();
            if (adding && draft.RowState == DataRowState.Detached) addRow(draft);
            DataRow saved = draft;
            save();
            draft = null;
            source.Position = source.Find("ID", saved["ID"]);
            ShowCurrent();
        }
        internal void Cancel()
        {
            if (!IsEditing) return;
            if (draft.RowState != DataRowState.Detached) draft.RejectChanges();
            draft = null; ShowCurrent();
        }
    }

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
