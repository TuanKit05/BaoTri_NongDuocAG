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
        private readonly TextBox txtTen = new TextBox();
        private readonly NumericUpDown numGia = new NumericUpDown();
        private readonly Button btnThem = new Button();
        private readonly Button btnLuu = new Button();
        private readonly Button btnDong = new Button();

        public frmDichVu()
        {
            Text = "Danh mục dịch vụ";
            ClientSize = new Size(640, 400);
            StartPosition = FormStartPosition.CenterParent;

            Label lblTen = new Label { Text = "Tên dịch vụ", Location = new Point(12, 15), AutoSize = true };
            txtTen.Location = new Point(100, 12);
            txtTen.Width = 270;

            Label lblGia = new Label { Text = "Đơn giá", Location = new Point(385, 15), AutoSize = true };
            numGia.Location = new Point(445, 12);
            numGia.Width = 120;
            numGia.Maximum = 1000000000;
            numGia.ThousandsSeparator = true;

            btnThem.Text = "Thêm";
            btnThem.Location = new Point(575, 10);
            btnThem.Width = 55;

            grid.Location = new Point(12, 45);
            grid.Size = new Size(616, 300);
            grid.AutoGenerateColumns = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ID", HeaderText = "Mã", ReadOnly = true, Width = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TEN_DICH_VU", HeaderText = "Tên dịch vụ", Width = 280 });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DON_GIA",
                HeaderText = "Đơn giá",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" }
            });
            grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "DANG_SU_DUNG", HeaderText = "Đang dùng", Width = 90 });

            btnLuu.Text = "Lưu thay đổi";
            btnLuu.Location = new Point(400, 355);
            btnLuu.Width = 110;
            btnDong.Text = "Đóng";
            btnDong.Location = new Point(518, 355);
            btnDong.Width = 110;

            Controls.AddRange(new Control[] { lblTen, txtTen, lblGia, numGia, btnThem, grid, btnLuu, btnDong });

            btnThem.Click += delegate { ThemDichVu(); };
            btnLuu.Click += delegate { LuuDanhMuc(); };
            btnDong.Click += delegate { Close(); };
            Load += delegate
            {
                factory.DanhSach();
                source.DataSource = factory;
                grid.DataSource = source;
            };
        }

        private void ThemDichVu()
        {
            try
            {
                AccessControl.DemandCurrent("SuaDanhMuc");
                string ten = txtTen.Text.Trim();
                if (ten.Length == 0)
                    throw new InvalidOperationException("Vui lòng nhập tên dịch vụ.");
                if (factory.Select("TEN_DICH_VU = '" + ten.Replace("'", "''") + "'").Length > 0)
                    throw new InvalidOperationException("Dịch vụ \"" + ten + "\" đã có trong danh mục.");

                string id = IdGenerator.Next("DICH_VU");
                factory.ThemDichVu(id, ten, numGia.Value);

                txtTen.Clear();
                numGia.Value = 0;
                source.Position = source.Count - 1;
                MessageBox.Show("Đã thêm dịch vụ. Nhấn \"Lưu thay đổi\" để ghi xuống cơ sở dữ liệu.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception error)
            {
                MessageBox.Show(ErrorLog.UserMessage(error, "Không thể thêm dịch vụ"), Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LuuDanhMuc()
        {
            try
            {
                AccessControl.DemandCurrent("SuaDanhMuc");
                grid.EndEdit();
                source.EndEdit();
                if (!factory.Save())
                    throw new InvalidOperationException("Không lưu được danh mục dịch vụ.");
                MessageBox.Show("Đã lưu danh mục dịch vụ.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception error)
            {
                MessageBox.Show(ErrorLog.UserMessage(error, "Không thể lưu danh mục dịch vụ"), Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}