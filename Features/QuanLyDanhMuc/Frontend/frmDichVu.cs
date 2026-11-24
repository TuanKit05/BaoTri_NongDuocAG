using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using CuahangNongduoc.DataLayer;

namespace CuahangNongduoc
{
    public class frmDichVu : Form
    {
        private readonly DichVuFactory factory = new DichVuFactory();
        private readonly BindingSource source = new BindingSource();
        private readonly DataGridView grid = new DataGridView();
        private readonly TextBox txtMa = new TextBox();
        private readonly TextBox txtTen = new TextBox();
        private readonly NumericUpDown numGia = new NumericUpDown();
        private readonly Button btnThem = new Button();
        private readonly Button btnSua = new Button();
        private readonly Button btnXoa = new Button();
        private readonly Button btnLuu = new Button();
        private readonly Button btnHuy = new Button();
        private readonly Button btnDong = new Button();
        private string selectedId;
        private bool editing;
        private bool loading;

        public frmDichVu()
        {
            Text = "Dịch vụ"; StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(720, 440); MinimumSize = new Size(660, 400);
            Font = new Font("Segoe UI", 9F);
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 3 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            TableLayoutPanel inputs = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3 };
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Control[] fields = { txtMa, txtTen, numGia }; string[] labels = { "Mã dịch vụ", "Tên dịch vụ", "Đơn giá" };
            for (int i = 0; i < fields.Length; i++) {
                inputs.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / 3));
                inputs.Controls.Add(new Label { Text = labels[i], Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, i);
                fields[i].Dock = DockStyle.Fill; fields[i].Margin = new Padding(3, 6, 3, 3); inputs.Controls.Add(fields[i], 1, i);
            }
            txtMa.ReadOnly = true; txtMa.Enabled = false; txtMa.BackColor = SystemColors.Control;
            txtTen.MaxLength = 100; numGia.Maximum = 1000000000000m; numGia.ThousandsSeparator = true;
            grid.Dock = DockStyle.Fill; grid.AutoGenerateColumns = false; grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.ReadOnly = true; grid.AllowUserToAddRows = grid.AllowUserToDeleteRows = false;
            grid.MultiSelect = false; grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ID", DataPropertyName = "ID", HeaderText = "Mã dịch vụ", FillWeight = 25 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TEN_DICH_VU", DataPropertyName = "TEN_DICH_VU", HeaderText = "Tên dịch vụ", FillWeight = 50 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DON_GIA", DataPropertyName = "DON_GIA", HeaderText = "Đơn giá", FillWeight = 25, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } });
            grid.DataSource = source;
            FlowLayoutPanel buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0, 6, 0, 0) };
            Button[] all = { btnThem, btnSua, btnXoa, btnLuu, btnHuy, btnDong }; string[] captions = { "Thêm", "Sửa", "Xóa", "Lưu", "Hủy", "Đóng" };
            for (int i = 0; i < all.Length; i++) { all[i].Text = captions[i]; all[i].Size = new Size(84, 30); buttons.Controls.Add(all[i]); }
            layout.Controls.Add(inputs, 0, 0); layout.Controls.Add(grid, 0, 1); layout.Controls.Add(buttons, 0, 2); Controls.Add(layout);
            btnThem.Click += delegate { Run(ThemDichVu); }; btnSua.Click += delegate { Run(SuaDichVu); };
            btnLuu.Click += delegate { Run(LuuDichVu); }; btnHuy.Click += delegate { HuyThayDoi(); };
            btnDong.Click += delegate { Close(); };
            btnXoa.Click += delegate {
                if (selectedId == null || MessageBox.Show(this, "Xóa dịch vụ đã chọn? Dữ liệu phiếu cũ vẫn được giữ.", Text,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                Run(XoaDichVuDaChon);
            };
            source.CurrentChanged += delegate { if (!loading && !editing) ShowCurrent(); };
            Load += delegate { try { TaiDichVu(); } catch (Exception error) { MessageBox.Show(this, error.Message, Text); Close(); } };
            FormClosing += delegate(object sender, FormClosingEventArgs e) {
                if (editing && MessageBox.Show(this, "Bỏ các thay đổi chưa lưu?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) e.Cancel = true;
            };
            SetEditing(false);
        }
        private void Run(MethodInvoker action)
        {
            try { action(); } catch (Exception error) { MessageBox.Show(this, ErrorLog.UserMessage(error, "Không thể hoàn tất thao tác dịch vụ"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        internal void TaiDichVu()
        {
            DichVuFactory.DemandManage(); loading = true;
            try { source.DataSource = factory.DanhSachDangSuDung(); } finally { loading = false; }
            editing = false; ShowCurrent();
        }
        private void SetEditing(bool value)
        {
            editing = value;
            bool allowed = AppSession.CurrentUser != null && AccountRoles.IsAdmin(AppSession.CurrentUser.Role);
            txtTen.Enabled = numGia.Enabled = value && allowed; grid.Enabled = !value;
            btnThem.Enabled = !value && allowed; btnSua.Enabled = btnXoa.Enabled = !value && allowed && selectedId != null;
            btnLuu.Enabled = btnHuy.Enabled = value && allowed;
        }
        private void ShowCurrent()
        {
            DataRowView row = source.Current as DataRowView;
            selectedId = row == null ? null : Convert.ToString(row["ID"]);
            txtMa.Text = selectedId ?? ""; txtTen.Text = row == null ? "" : Convert.ToString(row["TEN_DICH_VU"]);
            numGia.Value = row == null || row["DON_GIA"] == DBNull.Value ? 0 : Math.Max(0, Math.Min(numGia.Maximum, Convert.ToDecimal(row["DON_GIA"])));
            SetEditing(false);
        }
        internal void ThemDichVu()
        {
            DichVuFactory.DemandManage(); selectedId = null; txtMa.Text = "Tự động khi lưu"; txtTen.Clear(); numGia.Value = 0; SetEditing(true); txtTen.Focus();
        }
        internal void SuaDichVu()
        {
            DichVuFactory.DemandManage(); if (selectedId == null) throw new InvalidOperationException("Chọn dịch vụ cần sửa."); SetEditing(true); txtTen.Focus();
        }
        internal void LuuDichVu()
        {
            if (!editing) return;
            string id = factory.LuuDichVu(selectedId, txtTen.Text, numGia.Value);
            TaiDichVu(); source.Position = source.Find("ID", id); ShowCurrent();
        }
        internal void HuyThayDoi() { ShowCurrent(); }
        internal void XoaDichVuDaChon() { factory.XoaDichVu(selectedId); TaiDichVu(); }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { source.Dispose(); factory.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
