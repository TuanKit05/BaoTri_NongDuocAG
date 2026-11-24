using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using CuahangNongduoc.BusinessObject;

namespace CuahangNongduoc
{
    // The name remains a text cell so historical and ad hoc services always display correctly.
    internal static class InvoiceServiceEditor
    {
        public static void Attach(DataGridView grid, MethodInvoker changed)
        {
            DataTable catalog = new DataLayer.DichVuFactory().DanhSachDangSuDung();
            Control parent = grid.Parent;
            Panel host = new Panel { Name = "serviceEditor", Bounds = grid.Bounds, Anchor = grid.Anchor };
            FlowLayoutPanel bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 32, WrapContents = false };
            ComboBox picker = new ComboBox { Name = "cmbDichVu", Width = 230, DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };
            bool manage = AppSession.CurrentUser != null && AccountRoles.IsAdmin(AppSession.CurrentUser.Role);
            if (!manage) picker.DropDownStyle = ComboBoxStyle.DropDownList;
            grid.Columns["colDichVuTen"].ReadOnly = !manage;
            picker.DisplayMember = "TEN_DICH_VU";
            picker.ValueMember = "ID";
            picker.DataSource = catalog;
            picker.SelectedIndex = -1;
            Button add = new Button { Name = "btnThemDichVu", Text = "Thêm dịch vụ", AutoSize = true };
            Button remove = new Button { Name = "btnBoDichVu", Text = "Bỏ dịch vụ", AutoSize = true, Enabled = false };
            bar.Controls.Add(picker); bar.Controls.Add(add); bar.Controls.Add(remove);
            parent.Controls.Remove(grid);
            grid.Dock = DockStyle.Fill;
            host.Controls.Add(grid); host.Controls.Add(bar);
            parent.Controls.Add(host);
            add.Click += delegate {
                string name = picker.Text.Trim();
                if (name.Length == 0) { picker.Focus(); return; }
                long amount = 0;
                foreach (DataRow item in catalog.Rows)
                    if (String.Equals(Convert.ToString(item["TEN_DICH_VU"]), name, StringComparison.OrdinalIgnoreCase))
                        amount = Convert.ToInt64(item["DON_GIA"]);
                int index = grid.Rows.Add(name, amount);
                grid.CurrentCell = grid.Rows[index].Cells["colDichVuTien"];
                grid.BeginEdit(true);
                picker.SelectedIndex = -1; picker.Text = "";
                changed();
            };
            grid.RowsAdded += delegate { changed(); };
            grid.RowsRemoved += delegate { changed(); };
            MethodInvoker updateRemove = delegate {
                remove.Enabled = grid.Enabled && !grid.ReadOnly && grid.CurrentRow != null && !grid.CurrentRow.IsNewRow;
            };
            grid.SelectionChanged += delegate { updateRemove(); };
            grid.EnabledChanged += delegate { updateRemove(); };
            grid.ReadOnlyChanged += delegate { updateRemove(); };
            remove.Click += delegate {
                if (!grid.Enabled || grid.ReadOnly || grid.CurrentRow == null || grid.CurrentRow.IsNewRow) return;
                int index = grid.CurrentRow.Index;
                grid.CancelEdit();
                grid.Rows.RemoveAt(index);
                changed();
                updateRemove();
            };
            grid.EnabledChanged += delegate { bar.Enabled = grid.Enabled; };
            grid.CellEndEdit += delegate(object sender, DataGridViewCellEventArgs e) {
                if (e.RowIndex < 0 || grid.Columns[e.ColumnIndex].Name != "colDichVuTen") return;
                DataGridViewRow row = grid.Rows[e.RowIndex];
                string name = Convert.ToString(row.Cells[e.ColumnIndex].Value).Trim();
                foreach (DataRow item in catalog.Rows)
                    if (String.Equals(Convert.ToString(item["TEN_DICH_VU"]), name, StringComparison.OrdinalIgnoreCase))
                        row.Cells["colDichVuTien"].Value = Convert.ToInt64(item["DON_GIA"]);
                changed();
            };
            grid.Disposed += delegate { catalog.Dispose(); };
        }

        public static IList<DichVuPhieuBan> Read(DataGridView grid, bool validate)
        {
            if (validate && !grid.EndEdit()) throw new InvalidOperationException("Chưa hoàn tất dòng dịch vụ.");
            List<DichVuPhieuBan> result = new List<DichVuPhieuBan>();
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                string name = Convert.ToString(row.Cells["colDichVuTen"].Value).Trim();
                string text = Convert.ToString(row.Cells["colDichVuTien"].Value).Trim();
                if (name.Length == 0 && text.Length == 0) continue;
                decimal amount = 0;
                bool valid = name.Length > 0 && name.Length <= 100 &&
                    Decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out amount) &&
                    amount > 0 && amount == Decimal.Truncate(amount) && amount <= Int64.MaxValue;
                if (!valid)
                {
                    if (validate) throw new InvalidOperationException("Dịch vụ cần tên (tối đa 100 ký tự) và số tiền nguyên lớn hơn 0.");
                    continue;
                }
                result.Add(new DichVuPhieuBan { Ten = name, TienDV = (long)amount });
            }
            return result;
        }
    }
}
