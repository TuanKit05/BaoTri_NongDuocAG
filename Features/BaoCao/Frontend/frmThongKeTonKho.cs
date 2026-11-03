using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using CuahangNongduoc.Controller;

namespace CuahangNongduoc
{
    public partial class frmThongKeTonKho : Form
    {
        private DataTable m_Data;
        private int m_PrintRowIndex = 0;
        private int m_TotalSoLuong = 0;
        private long m_TotalThanhTien = 0;

        public frmThongKeTonKho()
        {
            InitializeComponent();
        }

        private void frmThongKeTonKho_Load(object sender, EventArgs e)
        {
            dtTuNgay.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dtDenNgay.Value = DateTime.Now.Date;
            SetupGridColumns();
        }

        private void SetupGridColumns()
        {
            dgvTonKho.AutoGenerateColumns = false;
            dgvTonKho.Columns.Clear();

            dgvTonKho.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colMaLo",
                DataPropertyName = "MA_LO",
                HeaderText = "Mã lô",
                Width = 90
            });
            dgvTonKho.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colMaSP",
                DataPropertyName = "MA_SP",
                HeaderText = "Mã SP",
                Width = 80
            });
            dgvTonKho.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colTenSP",
                DataPropertyName = "TEN_SAN_PHAM",
                HeaderText = "Tên sản phẩm",
                Width = 180
            });
            dgvTonKho.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDVT",
                DataPropertyName = "TEN_DON_VI",
                HeaderText = "ĐVT",
                Width = 60
            });
            dgvTonKho.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colSoLuong",
                DataPropertyName = "SO_LUONG",
                HeaderText = "Số lượng tồn",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Format = "N0"
                }
            });
            dgvTonKho.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDonGia",
                DataPropertyName = "DON_GIA_NHAP",
                HeaderText = "Đơn giá nhập",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Format = "N0"
                }
            });
            dgvTonKho.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colThanhTien",
                DataPropertyName = "THANH_TIEN",
                HeaderText = "Thành tiền",
                Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Format = "N0"
                }
            });
            dgvTonKho.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colNgayNhap",
                DataPropertyName = "NGAY_NHAP",
                HeaderText = "Ngày nhập",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" }
            });
            dgvTonKho.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colNSX",
                DataPropertyName = "NGAY_SAN_XUAT",
                HeaderText = "Ngày SX",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" }
            });
            dgvTonKho.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colHSD",
                DataPropertyName = "NGAY_HET_HAN",
                HeaderText = "Hạn dùng",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" }
            });
        }

        private void btnXem_Click(object sender, EventArgs e)
        {
            if (dtTuNgay.Value.Date > dtDenNgay.Value.Date)
            {
                MessageBox.Show("Từ ngày không được lớn hơn đến ngày.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                m_Data = MaSanPhamController.LayTonKhoTheoKhoangNgay(dtTuNgay.Value.Date, dtDenNgay.Value.Date);
                dgvTonKho.DataSource = m_Data;

                m_TotalSoLuong = 0;
                m_TotalThanhTien = 0;
                if (m_Data != null)
                {
                    foreach (DataRow row in m_Data.Rows)
                    {
                        m_TotalSoLuong += Convert.ToInt32(row["SO_LUONG"]);
                        m_TotalThanhTien += Convert.ToInt64(row["THANH_TIEN"]);
                    }
                }

                lblTongSo.Text = string.Format("Tổng số dòng: {0}  |  Tổng SL tồn: {1:N0}  |  Tổng giá trị: {2:N0} đ",
                    m_Data != null ? m_Data.Rows.Count : 0, m_TotalSoLuong, m_TotalThanhTien);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnIn_Click(object sender, EventArgs e)
        {
            if (m_Data == null || m_Data.Rows.Count == 0)
            {
                MessageBox.Show("Chưa có dữ liệu để in. Hãy bấm Xem trước.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            m_PrintRowIndex = 0;
            printDocument.DefaultPageSettings.Landscape = true;
            printDocument.DefaultPageSettings.Margins = new Margins(40, 40, 40, 40);

            PrintPreviewDialog preview = new PrintPreviewDialog();
            preview.Document = printDocument;
            preview.WindowState = FormWindowState.Maximized;
            preview.ShowDialog(this);
        }

        private void printDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            Font fontTitle = new Font("Arial", 14, FontStyle.Bold);
            Font fontHeader = new Font("Arial", 8, FontStyle.Bold);
            Font fontCell = new Font("Arial", 8);
            Font fontFooter = new Font("Arial", 9, FontStyle.Bold);

            float left = e.MarginBounds.Left;
            float top = e.MarginBounds.Top;
            float y = top;
            float pageWidth = e.MarginBounds.Width;

            string title = "THỐNG KÊ TỒN KHO";
            string period = string.Format("Từ ngày {0:dd/MM/yyyy} đến ngày {1:dd/MM/yyyy}",
                dtTuNgay.Value, dtDenNgay.Value);

            SizeF titleSize = g.MeasureString(title, fontTitle);
            g.DrawString(title, fontTitle, Brushes.Black, left + (pageWidth - titleSize.Width) / 2, y);
            y += titleSize.Height + 4;

            SizeF periodSize = g.MeasureString(period, fontCell);
            g.DrawString(period, fontCell, Brushes.Black, left + (pageWidth - periodSize.Width) / 2, y);
            y += periodSize.Height + 12;

            // Column widths (landscape A4 roughly)
            float[] widths = { 70, 60, 150, 50, 70, 80, 85, 70, 70, 70 };
            string[] headers = { "Mã lô", "Mã SP", "Tên sản phẩm", "ĐVT", "SL tồn", "Đơn giá", "Thành tiền", "Ngày nhập", "Ngày SX", "Hạn dùng" };

            float x = left;
            for (int i = 0; i < headers.Length; i++)
            {
                g.DrawString(headers[i], fontHeader, Brushes.Black, x, y);
                x += widths[i];
            }
            y += 16;
            g.DrawLine(Pens.Black, left, y, left + pageWidth, y);
            y += 4;

            float rowHeight = 16;
            float bottomLimit = e.MarginBounds.Bottom - 40;

            while (m_PrintRowIndex < m_Data.Rows.Count)
            {
                if (y + rowHeight > bottomLimit)
                {
                    e.HasMorePages = true;
                    return;
                }

                DataRow row = m_Data.Rows[m_PrintRowIndex];
                string[] values = new string[]
                {
                    Convert.ToString(row["MA_LO"]),
                    Convert.ToString(row["MA_SP"]),
                    Convert.ToString(row["TEN_SAN_PHAM"]),
                    Convert.ToString(row["TEN_DON_VI"]),
                    string.Format("{0:N0}", row["SO_LUONG"]),
                    string.Format("{0:N0}", row["DON_GIA_NHAP"]),
                    string.Format("{0:N0}", row["THANH_TIEN"]),
                    Convert.ToDateTime(row["NGAY_NHAP"]).ToString("dd/MM/yyyy"),
                    Convert.ToDateTime(row["NGAY_SAN_XUAT"]).ToString("dd/MM/yyyy"),
                    Convert.ToDateTime(row["NGAY_HET_HAN"]).ToString("dd/MM/yyyy")
                };

                x = left;
                for (int i = 0; i < values.Length; i++)
                {
                    string text = values[i] ?? "";
                    // Truncate long product names
                    if (i == 2 && text.Length > 28)
                        text = text.Substring(0, 27) + "…";
                    g.DrawString(text, fontCell, Brushes.Black, x, y);
                    x += widths[i];
                }

                y += rowHeight;
                m_PrintRowIndex++;
            }

            // Footer totals on last page
            y += 8;
            g.DrawLine(Pens.Black, left, y, left + pageWidth, y);
            y += 6;
            string footer = string.Format("Tổng số dòng: {0}   |   Tổng SL tồn: {1:N0}   |   Tổng giá trị: {2:N0} đ",
                m_Data.Rows.Count, m_TotalSoLuong, m_TotalThanhTien);
            g.DrawString(footer, fontFooter, Brushes.Black, left, y);

            e.HasMorePages = false;
        }

        private void btnThoat_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
