using System;
using System.Data;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public static class SaleLotDisplay
    {
        public static void Attach(DataGridView grid, DataGridViewComboBoxColumn lotColumn)
        {
            DataGridViewTextBoxColumn expiry = new DataGridViewTextBoxColumn();
            expiry.Name = "colHanDung";
            expiry.HeaderText = "Hạn dùng";
            expiry.ReadOnly = true;
            expiry.SortMode = DataGridViewColumnSortMode.NotSortable;
            expiry.FillWeight = 70;
            grid.Columns.Add(expiry);
            expiry.DisplayIndex = lotColumn.DisplayIndex + 1;
            DataGridViewTextBoxColumn cost = new DataGridViewTextBoxColumn();
            cost.Name = "colGiaVonDaLuu";
            cost.HeaderText = "Giá vốn đã lưu";
            cost.DataPropertyName = "GIA_VON";
            cost.ReadOnly = true;
            cost.FillWeight = 85;
            cost.DefaultCellStyle.Format = "N0";
            cost.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            cost.Visible = AccessControl.Can("XemGiaVon") && AccessControl.Can("XemGiaNhap");
            grid.Columns.Add(cost);
            grid.CellFormatting += delegate(object sender, DataGridViewCellFormattingEventArgs e) {
                if (e.RowIndex < 0 || grid.Columns[e.ColumnIndex] != expiry) return;
                string id = Convert.ToString(grid.Rows[e.RowIndex].Cells[lotColumn.Index].Value);
                DataTable lots = lotColumn.DataSource as DataTable;
                e.Value = "";
                if (lots != null)
                    foreach (DataRow row in lots.Rows)
                        if (String.Equals(Convert.ToString(row["ID"]), id, StringComparison.OrdinalIgnoreCase))
                        {
                            e.Value = row["NGAY_HET_HAN"] == DBNull.Value ? "" : Convert.ToDateTime(row["NGAY_HET_HAN"]).ToString("dd/MM/yyyy");
                            break;
                        }
                e.FormattingApplied = true;
            };
        }
    }
}
