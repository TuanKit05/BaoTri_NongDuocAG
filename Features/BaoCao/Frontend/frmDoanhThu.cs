using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public partial class frmDoanhThu : Form
    {
        private DataGridView dgvDoanhThu;
        private GroupBox groupBox3;
        private DateTimePicker dtpTuNgay;
        private DateTimePicker dtpDenNgay;
        private Button btnXemKhoang;

        public frmDoanhThu()
        {
            InitializeComponent();
            KhoiTaoDieuKhien();
            KetNoiSuKien();
            CuahangNongduoc.ResponsiveLayout.Attach(this);
        }

        // Tạo thêm bộ lọc "Từ ngày - đến ngày" và bảng kết quả ngay trong code,
        // không cần sửa frmDoanhThu.Designer.cs.
        private void KhoiTaoDieuKhien()
        {
            numericUpDown1.Value = DateTime.Now.Year;
            comboBox1.SelectedIndex = DateTime.Now.Month - 1;

            groupBox3 = new GroupBox();
            groupBox3.Text = "Từ ngày - đến ngày";
            groupBox3.Location = new Point(622, 3);
            groupBox3.Size = new Size(170, 95);

            Label lblTu = new Label();
            lblTu.Text = "Từ ngày";
            lblTu.AutoSize = true;
            lblTu.Location = new Point(8, 25);

            dtpTuNgay = new DateTimePicker();
            dtpTuNgay.Format = DateTimePickerFormat.Custom;
            dtpTuNgay.CustomFormat = "dd/MM/yyyy";
            dtpTuNgay.Location = new Point(62, 21);
            dtpTuNgay.Size = new Size(100, 20);
            dtpTuNgay.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            Label lblDen = new Label();
            lblDen.Text = "Đến ngày";
            lblDen.AutoSize = true;
            lblDen.Location = new Point(8, 52);

            dtpDenNgay = new DateTimePicker();
            dtpDenNgay.Format = DateTimePickerFormat.Custom;
            dtpDenNgay.CustomFormat = "dd/MM/yyyy";
            dtpDenNgay.Location = new Point(62, 48);
            dtpDenNgay.Size = new Size(100, 20);
            dtpDenNgay.Value = DateTime.Now.Date;

            btnXemKhoang = new Button();
            btnXemKhoang.Text = "Xem";
            btnXemKhoang.Location = new Point(62, 68);
            btnXemKhoang.Size = new Size(100, 24);
            btnXemKhoang.UseVisualStyleBackColor = true;

            groupBox3.Controls.Add(lblTu);
            groupBox3.Controls.Add(dtpTuNgay);
            groupBox3.Controls.Add(lblDen);
            groupBox3.Controls.Add(dtpDenNgay);
            groupBox3.Controls.Add(btnXemKhoang);
            panel1.Controls.Add(groupBox3);

            // Bảng kết quả thay cho ReportViewer (chưa có file .rdlc doanh thu).
            dgvDoanhThu = new DataGridView();
            dgvDoanhThu.Dock = DockStyle.Fill;
            dgvDoanhThu.ReadOnly = true;
            dgvDoanhThu.AllowUserToAddRows = false;
            dgvDoanhThu.AllowUserToDeleteRows = false;
            dgvDoanhThu.RowHeadersVisible = false;
            dgvDoanhThu.AutoGenerateColumns = false;
            dgvDoanhThu.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDoanhThu.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDoanhThu.BackgroundColor = SystemColors.Window;

            dgvDoanhThu.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colChiTieu",
                DataPropertyName = "TEN",
                HeaderText = "Chỉ tiêu",
                FillWeight = 70
            });
            dgvDoanhThu.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colSoTien",
                DataPropertyName = "SO_TIEN",
                HeaderText = "Số tiền (đ)",
                FillWeight = 30,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Format = "N0"
                }
            });

            reportViewer.Visible = false;
            panel2.Controls.Add(dgvDoanhThu);
            dgvDoanhThu.BringToFront();
        }

        private void KetNoiSuKien()
        {
            // Xem theo ngày
            button1.Click += delegate
            {
                DateTime ngay = dateTimePicker1.Value.Date;
                XemTheoKhoang(ngay, ngay);
            };

            // Xem theo tháng
            button2.Click += delegate { XemTheoThang(); };

            // Xem theo năm
            button3.Click += delegate { XemTheoNam(); };

            // Từ ngày - đến ngày
            btnXemKhoang.Click += delegate
            {
                XemTheoKhoang(dtpTuNgay.Value.Date, dtpDenNgay.Value.Date);
            };
        }

        private void XemTheoThang()
        {
            if (comboBox1.SelectedIndex < 0)
            {
                MessageBox.Show("Vui lòng chọn tháng.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int nam = (int)numericUpDown1.Value;
            int thang = comboBox1.SelectedIndex + 1;
            DateTime tu = new DateTime(nam, thang, 1);
            DateTime den = tu.AddMonths(1).AddDays(-1);
            XemTheoKhoang(tu, den);
        }

        private void XemTheoNam()
        {
            int nam = (int)numericUpDown1.Value;
            XemTheoKhoang(new DateTime(nam, 1, 1), new DateTime(nam, 12, 31));
        }

        private void XemTheoKhoang(DateTime tuNgay, DateTime denNgay)
        {
            if (tuNgay > denNgay)
            {
                MessageBox.Show("Từ ngày không được lớn hơn đến ngày.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                IList<KetQuaChiTieu> ketQua =
                    new BaoCaoTongHopService().LayThongKe(tuNgay, denNgay);

                DataTable dt = new DataTable();
                dt.Columns.Add("TEN", typeof(string));
                dt.Columns.Add("SO_TIEN", typeof(decimal));

                foreach (KetQuaChiTieu kq in ketQua)
                {
                    object soTien = kq.SoTien.HasValue
                        ? (object)kq.SoTien.Value
                        : DBNull.Value;
                    dt.Rows.Add(kq.Ten, soTien);
                }

                dgvDoanhThu.DataSource = dt;
                this.Text = string.Format("DOANH THU - từ {0:dd/MM/yyyy} đến {1:dd/MM/yyyy}",
                    tuNgay, denNgay);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " +
                    ErrorLog.UserMessage(ex, "Không thể hoàn tất thao tác"),
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void frmDoanhThu_Load(object sender, EventArgs e)
        {
            DateTime hom = DateTime.Now;
            DateTime dauThang = new DateTime(hom.Year, hom.Month, 1);
            XemTheoKhoang(dauThang, dauThang.AddMonths(1).AddDays(-1));
        }
    }
}