using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public class frmNhatKyThaoTac : Form
    {
        private readonly DataGridView grid = new DataGridView();
        private DataTable rows;

        public frmNhatKyThaoTac()
        {
            Text = "Nhật ký thao tác";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(960, 520);
            MinimumSize = new Size(640, 320);
            grid.Name = "gridNhatKy";
            grid.Dock = DockStyle.Fill;
            grid.AutoGenerateColumns = false;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToOrderColumns = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.RowHeadersVisible = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            string[] fields = { "ID_TAI_KHOAN", "TEN_TAI_KHOAN", "THOI_GIAN", "THAO_TAC" };
            string[] titles = { "ID tài khoản", "Tên tài khoản", "Thời gian", "Thao tác" };
            int[] widths = { 130, 180, 180, 260 };
            for (int i = 0; i < fields.Length; i++)
            {
                DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
                column.Name = fields[i];
                column.DataPropertyName = fields[i];
                column.HeaderText = titles[i];
                column.MinimumWidth = widths[i];
                column.FillWeight = widths[i];
                if (fields[i] == "THOI_GIAN") column.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm:ss";
                grid.Columns.Add(column);
            }
            Controls.Add(grid);
            Load += delegate {
                try
                {
                    rows = new AccountRepository().GetAuditLog();
                    grid.DataSource = rows;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ErrorLog.UserMessage(ex, "Không thể tải nhật ký thao tác"), Text,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Close();
                }
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                grid.DataSource = null;
                if (rows != null) { rows.Dispose(); rows = null; }
            }
            base.Dispose(disposing);
        }
    }
}
