using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    // One layout policy for functional forms. Business logic and bindings stay in their forms.
    public static class ResponsiveLayout
    {
        private static readonly Dictionary<Form, bool> attached = new Dictionary<Form, bool>();

        public static void Attach(Form form)
        {
            if (form != null && !form.IsDisposed) form.StartPosition = FormStartPosition.CenterScreen;
            if (form == null || form.IsDisposed || form.IsMdiContainer || attached.ContainsKey(form) ||
                LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            attached.Add(form, true);
            form.Disposed += delegate { attached.Remove(form); };
            bool saleDocument = form is frmBanLe || form is frmBanSi;
            if (saleDocument) ArrangeSaleDocument(form);
            if (IsCenteredDocument(form))
            {
                Size normalSize = form.Size;
                form.StartPosition = FormStartPosition.CenterScreen;
                form.WindowState = FormWindowState.Normal;
                form.Shown += delegate {
                    form.BeginInvoke(new MethodInvoker(delegate {
                        if (!form.IsDisposed && !form.Disposing) CenterDocument(form, normalSize);
                    }));
                };
            }
            if (form is frmDanhsachPhieuBanLe || form is frmDanhsachPhieuBanSi || form is frmDanhsachPhieuNhap)
            {
                form.WindowState = FormWindowState.Maximized;
                form.Shown += delegate {
                    form.BeginInvoke(new MethodInvoker(delegate {
                        if (form.IsDisposed || form.Disposing) return;
                        form.WindowState = FormWindowState.Normal;
                        form.WindowState = FormWindowState.Maximized;
                    }));
                };
            }
            if (saleDocument) return;
            form.SuspendLayout();
            try
            {
                ArrangeDirectGrid(form);
                // Fee sections in the older sale forms were positioned with fixed coordinates.
                foreach (Control child in Children(form))
                    if (child.Name == "grpPhi") child.Dock = DockStyle.Bottom;
                ArrangeContainer(form, false);
            }
            finally { form.ResumeLayout(true); }
        }

        public static bool IsCenteredDocument(Form form)
        {
            return form is frmBanLe || form is frmBanSi || form is frmNhapHang;
        }

        private static void ArrangeSaleDocument(Form form)
        {
            Control header = form.Controls["panel1"];
            Control lines = form.Controls["groupBox1"];
            Control fees = form.Controls["grpPhi"];
            Control navigator = form.Controls["bindingNavigator"];
            Control product = header.Controls["groupBox2"];
            Control invoice = header.Controls["grpPhieuBanLe"];
            int productWidth = 0;
            foreach (Control child in product.Controls) productWidth = Math.Max(productWidth, child.Right + 12);
            int width = Math.Max(header.Width, invoice.Width + productWidth);
            foreach (Control child in fees.Controls) width = Math.Max(width, child.Right + 12);
            int height = header.Height + Math.Max(lines.Height, 180) + fees.Height;
            Panel viewport = new Panel();
            viewport.Name = "saleViewport";
            viewport.Dock = DockStyle.Fill;
            viewport.AutoScroll = true;
            Panel content = new Panel();
            content.Name = "saleContent";
            content.Size = new Size(width, height);
            viewport.Controls.Add(content);
            form.SuspendLayout();
            content.SuspendLayout();
            header.Dock = DockStyle.Top;
            fees.Dock = DockStyle.Bottom;
            lines.Dock = DockStyle.Fill;
            content.Controls.Add(lines);
            content.Controls.Add(fees);
            content.Controls.Add(header);
            content.Controls.SetChildIndex(lines, 0);
            form.Controls.Add(viewport);
            form.Controls.SetChildIndex(viewport, 0);
            navigator.Dock = DockStyle.Top;
            foreach (Control child in product.Controls)
                child.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            Control serviceGrid = fees.Controls["dgvDichVu"];
            serviceGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            foreach (Control child in Children(content)) ConfigureSaleGrids(child);
            viewport.AutoScrollMinSize = new Size(width, height);
            EventHandler resize = delegate {
                content.Size = new Size(Math.Max(width, viewport.ClientSize.Width), Math.Max(height, viewport.ClientSize.Height));
            };
            viewport.ClientSizeChanged += resize;
            form.ClientSize = new Size(width, height + navigator.Height);
            content.ResumeLayout(true);
            form.ResumeLayout(true);
            resize(viewport, EventArgs.Empty);
        }

        private static void ConfigureSaleGrids(Control parent)
        {
            DataGridView grid = parent as DataGridView;
            if (grid != null) ConfigureGrid(grid);
            foreach (Control child in parent.Controls) ConfigureSaleGrids(child);
        }

        private static void CenterDocument(Form form, Size normalSize)
        {
            form.WindowState = FormWindowState.Normal;
            Rectangle area = Screen.FromControl(form).WorkingArea;
            if (form.MdiParent != null)
                foreach (Control control in form.MdiParent.Controls)
                    if (control is MdiClient) { area = control.ClientRectangle; break; }
            int width = Math.Min(normalSize.Width, Math.Max(1, area.Width - 24));
            int height = Math.Min(normalSize.Height, Math.Max(1, area.Height - 24));
            form.SetBounds(area.Left + (area.Width - width) / 2, area.Top + (area.Height - height) / 2, width, height);
        }

        public static void ApplyOpenForms(object sender, EventArgs e)
        {
            foreach (Form form in Application.OpenForms) Attach(form);
        }

        public static void ConfigureSaleList(DataGridView grid, bool wholesale)
        {
            grid.AutoGenerateColumns = false;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            string[] fields = { "ID", "ID_KHACH_HANG", "NGAY_BAN", "TONG_TIEN", "DA_TRA", "CON_NO" };
            string[] titles = { "Mã phiếu", wholesale ? "Đại lý" : "Khách hàng", "Ngày bán", "Tổng tiền", "Đã trả", "Còn nợ" };
            int[] widths = { 90, 220, 120, 130, 130, 130 };
            foreach (DataGridViewColumn column in grid.Columns)
            {
                int index = Array.IndexOf(fields, column.DataPropertyName);
                column.Visible = index >= 0;
                if (index < 0) continue;
                column.HeaderText = titles[index];
                column.DisplayIndex = index;
                column.MinimumWidth = widths[index];
                column.FillWeight = widths[index];
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                if (index == 2) column.DefaultCellStyle.Format = "dd/MM/yyyy";
                if (index >= 3)
                {
                    column.DefaultCellStyle.Format = "N0";
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                DataGridViewComboBoxColumn customer = column as DataGridViewComboBoxColumn;
                if (customer != null) customer.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
            }
        }

        private static List<Control> Children(Control control)
        {
            List<Control> result = new List<Control>();
            foreach (Control child in control.Controls) result.Add(child);
            return result;
        }

        // Account management has a grid above a group of controls on the same form.
        private static void ArrangeDirectGrid(Form form)
        {
            DataGridView grid = null;
            foreach (Control child in form.Controls)
                if (child is DataGridView && child.Dock == DockStyle.None) { grid = (DataGridView)child; break; }
            if (grid == null) return;
            List<Control> footerControls = new List<Control>();
            foreach (Control child in Children(form))
                if (child != grid && child.Dock == DockStyle.None && child.Top >= grid.Bottom)
                    footerControls.Add(child);
            if (footerControls.Count > 0)
            {
                int top = grid.Bottom + 8;
                Panel footer = new Panel(); footer.Name = "responsiveFooter";
                footer.Height = Math.Max(100, form.ClientSize.Height - top);
                footer.Dock = DockStyle.Bottom;
                foreach (Control child in footerControls)
                {
                    Point position = child.Location;
                    footer.Controls.Add(child);
                    child.Location = new Point(position.X, position.Y - top);
                }
                form.Controls.Add(footer);
            }
            grid.Dock = DockStyle.Fill;
            form.Controls.SetChildIndex(grid, 0);
        }

        private static bool HasMainGrid(Control parent)
        {
            if (parent is DataGridView) return parent.Name != "dgvDichVu";
            foreach (Control child in parent.Controls)
                if (HasMainGrid(child)) return true;
            return false;
        }

        private static void ArrangeContainer(Control parent, bool insideCenteredContent)
        {
            List<Control> children = Children(parent);
            foreach (Control child in children)
            {
                DataGridView grid = child as DataGridView;
                if (grid != null) { ConfigureGrid(grid); continue; }
                ToolStrip strip = child as ToolStrip;
                if (strip != null) { CenterToolbar(strip); continue; }
                if (child is Panel || child is GroupBox || child is TabPage)
                    ArrangeContainer(child, insideCenteredContent);
            }
            if (insideCenteredContent || parent is TableLayoutPanel || parent is FlowLayoutPanel || HasMainGrid(parent)) return;
            List<Control> content = new List<Control>();
            foreach (Control child in Children(parent))
                if (!(child is ToolStrip) && !(child is MdiClient)) content.Add(child);
            if (content.Count == 0) return;
            // Do not override report viewers or other specialized controls.
            foreach (Control child in content)
                if (!(child is Label || child is TextBoxBase || child is ButtonBase || child is ComboBox ||
                    child is NumericUpDown || child is DateTimePicker || child is ListControl ||
                    child is Panel || child is GroupBox || child is DataGridView || child is LinkLabel)) return;
            bool docked = false;
            int left = Int32.MaxValue, right = 0;
            foreach (Control child in content)
            {
                docked |= child.Dock != DockStyle.None;
                left = Math.Min(left, child.Left); right = Math.Max(right, child.Right);
            }
            int width = docked ? parent.ClientSize.Width : right - left;
            if (width <= 0) return;
            int topInset = parent is GroupBox ? parent.Font.Height + 4 : 0;
            Panel center = new Panel(); center.Name = "responsiveContent";
            center.Width = width; center.Height = parent.ClientSize.Height;
            center.Anchor = AnchorStyles.Top;
            parent.SuspendLayout(); center.SuspendLayout();
            foreach (Control child in content)
            {
                Point location = child.Location;
                center.Controls.Add(child);
                if (child.Dock == DockStyle.None)
                {
                    child.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    child.Location = new Point(location.X - (docked ? 0 : left), location.Y - topInset);
                }
            }
            parent.Controls.Add(center);
            center.ResumeLayout(true);
            EventHandler align = delegate {
                if (parent.IsDisposed || center.IsDisposed) return;
                center.SetBounds(Math.Max(0, (parent.ClientSize.Width - width) / 2), topInset, width, Math.Max(0, parent.ClientSize.Height - topInset));
            };
            parent.ClientSizeChanged += align;
            parent.SuspendLayout(); align(parent, EventArgs.Empty); parent.ResumeLayout(true);
            parent.ResumeLayout(true);
        }

        private static void ConfigureGrid(DataGridView grid)
        {
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            Action<DataGridViewColumn> configure = delegate(DataGridViewColumn column) {
                if (String.Equals(column.DataPropertyName, "ID", StringComparison.OrdinalIgnoreCase)) column.ReadOnly = true;
                if (column.Frozen) return;
                column.FillWeight = Math.Max(50, Math.Min(250, column.Width));
                column.MinimumWidth = Math.Max(column.MinimumWidth, 60);
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            };
            foreach (DataGridViewColumn column in grid.Columns) configure(column);
            grid.ColumnAdded += delegate(object sender, DataGridViewColumnEventArgs e) { configure(e.Column); };
        }

        private static void CenterToolbar(ToolStrip strip)
        {
            bool aligning = false;
            Padding original = strip.Padding;
            strip.Layout += delegate {
                if (aligning || strip.IsDisposed) return;
                aligning = true;
                try
                {
                    int width = strip.GripStyle == ToolStripGripStyle.Visible ? strip.GripRectangle.Width : 0;
                    foreach (ToolStripItem item in strip.Items)
                        if (item.Available) width += item.GetPreferredSize(Size.Empty).Width + item.Margin.Horizontal;
                    Padding padding = new Padding(Math.Max(original.Left, (strip.ClientSize.Width - width) / 2), original.Top, original.Right, original.Bottom);
                    if (strip.Padding != padding) strip.Padding = padding;
                }
                finally { aligning = false; }
            };
        }
    }
}
