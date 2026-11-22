using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public static class ExpiryWarning
    {
        public static DataTable Load(DateTime today)
        {
            AccessControl.DemandCurrent("SanPham");
            int days = WarehouseSettings.LoadExpiryDays();
            using (OleDbConnection connection = new OleDbConnection(DataService.m_ConnectString))
            {
                connection.Open();
                DataTable table = AccountRepository.ReadTable(connection, null,
                    "SELECT MSP.ID AS MA_LO, SP.TEN_SAN_PHAM, MSP.SO_LUONG, MSP.NGAY_HET_HAN " +
                    "FROM MA_SAN_PHAM MSP INNER JOIN SAN_PHAM SP ON MSP.ID_SAN_PHAM=SP.ID " +
                    "WHERE MSP.SO_LUONG>0 AND MSP.NGAY_HET_HAN>=? AND MSP.NGAY_HET_HAN<? " +
                    "ORDER BY MSP.NGAY_HET_HAN, MSP.ID", today.Date, today.Date.AddDays(days + 1));
                table.Columns.Add("SO_NGAY_CON", typeof(int));
                foreach (DataRow row in table.Rows)
                    row["SO_NGAY_CON"] = (Convert.ToDateTime(row["NGAY_HET_HAN"]).Date - today.Date).Days;
                return table;
            }
        }

        public static Form ShowIfNeeded(Form owner)
        {
            if (!AccessControl.Can("SanPham")) return null;
            DataTable rows = Load(DateTime.Today);
            if (rows.Rows.Count == 0) return null;
            Form warning = new Form();
            warning.Text = "Cảnh báo hàng sắp hết hạn";
            warning.StartPosition = FormStartPosition.CenterScreen;
            warning.Size = new Size(850, 420);
            warning.MinimizeBox = false;
            Label summary = new Label();
            summary.Text = "Có " + rows.Rows.Count + " lô còn tồn sắp hết hạn. Hãy ưu tiên kiểm tra, xử lý các lô gần hạn.";
            summary.Dock = DockStyle.Top;
            summary.Height = 45;
            summary.Padding = new Padding(10);
            DataGridView grid = new DataGridView();
            grid.Name = "dgvCanhBaoHanDung";
            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = grid.AllowUserToDeleteRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.BindingContext = new BindingContext();
            grid.DataSource = rows;
            grid.Columns["MA_LO"].HeaderText = "Số lô";
            grid.Columns["TEN_SAN_PHAM"].HeaderText = "Sản phẩm";
            grid.Columns["SO_LUONG"].HeaderText = "Tồn kho";
            grid.Columns["NGAY_HET_HAN"].HeaderText = "Hạn dùng";
            grid.Columns["NGAY_HET_HAN"].DefaultCellStyle.Format = "dd/MM/yyyy";
            grid.Columns["SO_NGAY_CON"].HeaderText = "Số ngày còn";
            Button close = new Button();
            close.Text = "Đóng";
            close.Dock = DockStyle.Bottom;
            close.Height = 32;
            close.Click += delegate { warning.Close(); };
            warning.Controls.Add(grid);
            warning.Controls.Add(summary);
            warning.Controls.Add(close);
            warning.Show(owner);
            return warning;
        }
    }
}
