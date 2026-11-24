using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using CuahangNongduoc.Features.BaoCao.Backend;

namespace CuahangNongduoc.Features.BaoCao.Frontend
{
    // YC-BC-02: xem tồn kho từng lô tại một ngày được chọn.
    // YC-BC-04: xuất báo cáo ra Excel (.xls), PDF (in qua "Microsoft Print to PDF") và CSV.
    // Designer cần có: dtNgay, btnXem, btnDong, dgvTonKho, lblTongKet, lblCanhBao.
    // Nút "Xuất báo cáo" được tạo trong code nên không cần sửa frmTonKhoTaiNgay.Designer.cs.
    public partial class frmTonKhoTaiNgay : Form
    {
        private readonly TonKhoTaiNgayService service = new TonKhoTaiNgayService();
        private readonly Button btnXuat = new Button();

        private static readonly string[] TieuDeCot =
        {
            "Mã lô", "Mã SP", "Tên sản phẩm", "Đơn vị", "SL tại ngày",
            "Đơn giá nhập", "Giá trị", "Ngày nhập", "Ngày hết hạn"
        };

        public frmTonKhoTaiNgay()
        {
            InitializeComponent();
            CuahangNongduoc.ResponsiveLayout.Attach(this);
            this.Load += new EventHandler(frmTonKhoTaiNgay_Load);
            this.btnXem.Click += new EventHandler(btnXem_Click);
            this.btnDong.Click += new EventHandler(btnDong_Click);

            this.btnXuat.Text = "Xuất báo cáo";
            this.btnXuat.Size = new Size(120, this.btnDong.Height);
            this.btnXuat.Location = new Point(this.btnDong.Left - this.btnXuat.Width - 8, this.btnDong.Top);
            this.btnXuat.Anchor = this.btnDong.Anchor;
            this.btnXuat.Enabled = false;
            this.btnXuat.Click += new EventHandler(btnXuat_Click);
            this.btnDong.Parent.Controls.Add(this.btnXuat);
        }

        private void frmTonKhoTaiNgay_Load(object sender, EventArgs e)
        {
            dtNgay.Value = DateTime.Today;
            lblCanhBao.Text = "";
            lblTongKet.Text = "";
            btnXuat.Enabled = false;
        }

        private void btnXem_Click(object sender, EventArgs e)
        {
            try
            {
                AccessControl.DemandCurrent("BaoCaoTonKho");
                KetQuaTonKho kq = service.LayTonKhoTaiNgay(dtNgay.Value.Date);
                dgvTonKho.DataSource = ChuyenSangBang(kq);
                lblTongKet.Text = string.Format(
                    "Ngày {0:dd/MM/yyyy} | Tổng SL tồn: {1:N0} | Tổng giá trị (giá nhập): {2:N0} đ",
                    kq.Ngay, kq.TongSoLuong, kq.TongGiaTri);
                lblCanhBao.Text=kq.CanhBao;
                btnXuat.Enabled = dgvTonKho.Rows.Count > 0;
            }
            catch (Exception ex)
            {
                btnXuat.Enabled = false;
                MessageBox.Show("Lỗi tải tồn kho: " + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static DataTable ChuyenSangBang(KetQuaTonKho kq)
        {
            DataTable tbl = new DataTable();
            tbl.Columns.Add("MA_LO", typeof(string));
            tbl.Columns.Add("MA_SP", typeof(string));
            tbl.Columns.Add("TEN_SAN_PHAM", typeof(string));
            tbl.Columns.Add("TEN_DON_VI", typeof(string));
            tbl.Columns.Add("SO_LUONG_TAI_NGAY", typeof(long));
            tbl.Columns.Add("DON_GIA_NHAP", typeof(decimal));
            tbl.Columns.Add("GIA_TRI", typeof(decimal));
            tbl.Columns.Add("NGAY_NHAP", typeof(DateTime));
            tbl.Columns.Add("NGAY_HET_HAN", typeof(DateTime));

            foreach (TonKhoLo lo in kq.DanhSach)
            {
                DataRow r = tbl.NewRow();
                r["MA_LO"] = lo.MaLo;
                r["MA_SP"] = lo.MaSp;
                r["TEN_SAN_PHAM"] = lo.TenSanPham;
                r["TEN_DON_VI"] = lo.TenDonVi;
                r["SO_LUONG_TAI_NGAY"] = lo.SoLuongTaiNgay;
                r["DON_GIA_NHAP"] = lo.DonGiaNhap;
                r["GIA_TRI"] = lo.GiaTriTaiNgay;
                r["NGAY_NHAP"] = lo.NgayNhap;
                if (lo.NgayHetHan.HasValue)
                    r["NGAY_HET_HAN"] = lo.NgayHetHan.Value;
                tbl.Rows.Add(r);
            }
            return tbl;
        }

        private void btnDong_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // ===== Xuất báo cáo (YC-BC-04) =====

        private void btnXuat_Click(object sender, EventArgs e)
        {
            if (dgvTonKho.Rows.Count == 0) return;
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Xuất báo cáo tồn kho tại ngày";
                dialog.Filter = "Excel (*.xls)|*.xls|PDF (*.pdf)|*.pdf|CSV (*.csv)|*.csv";
                dialog.FileName = "TonKhoTaiNgay_" + dtNgay.Value.ToString("yyyyMMdd");
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                string duoi = Path.GetExtension(dialog.FileName).ToLowerInvariant();
                try
                {
                    if (duoi == ".pdf")
                    {
                        XuatPdf();
                        return;
                    }
                    if (duoi == ".xls") XuatExcelXml(dialog.FileName);
                    else XuatCsv(dialog.FileName);
                    MessageBox.Show(this, "Đã xuất báo cáo.", "Xuất báo cáo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Không xuất được tệp: " + ex.Message, "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Dòng tiêu đề báo cáo (1 ô) + dòng tóm tắt + tiêu đề cột + dữ liệu đúng như đang hiển thị
        private List<object[]> LayDuLieuXuat()
        {
            List<object[]> ds = new List<object[]>();
            ds.Add(new object[] { "BÁO CÁO TỒN KHO TẠI NGÀY" });
            ds.Add(new object[] { lblTongKet.Text });
            if (!String.IsNullOrEmpty(lblCanhBao.Text))
                ds.Add(new object[] { lblCanhBao.Text });
            ds.Add(new object[] { });
            ds.Add((object[])TieuDeCot.Clone());

            foreach (DataGridViewRow row in dgvTonKho.Rows)
            {
                if (row.IsNewRow) continue;
                object[] dong = new object[TieuDeCot.Length];
                for (int i = 0; i < dong.Length && i < row.Cells.Count; i++)
                    dong[i] = row.Cells[i].Value;
                ds.Add(dong);
            }
            return ds;
        }

        private void XuatCsv(string path)
        {
            using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(true)))
            {
                foreach (object[] dong in LayDuLieuXuat())
                {
                    for (int i = 0; i < dong.Length; i++)
                    {
                        if (i > 0) writer.Write(",");
                        writer.Write(EscapeCsv(GiaTriCsv(dong[i])));
                    }
                    writer.WriteLine();
                }
            }
        }

        // Excel 2003 XML: mở trực tiếp bằng Excel, giữ định dạng số và ngày
        private void XuatExcelXml(string path)
        {
            List<object[]> du = LayDuLieuXuat();
            StringBuilder sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.Append("<?mso-application progid=\"Excel.Sheet\"?>");
            sb.Append("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
            sb.Append("<Styles>");
            sb.Append("<Style ss:ID=\"hdr\"><Font ss:Bold=\"1\"/><Interior ss:Color=\"#DDEBF7\" ss:Pattern=\"Solid\"/></Style>");
            sb.Append("<Style ss:ID=\"num\"><NumberFormat ss:Format=\"#,##0\"/></Style>");
            sb.Append("<Style ss:ID=\"date\"><NumberFormat ss:Format=\"dd/mm/yyyy\"/></Style>");
            sb.Append("</Styles>");
            sb.Append("<Worksheet ss:Name=\"TonKhoTaiNgay\"><Table>");
            int soDongTieuDe = 3 + (String.IsNullOrEmpty(lblCanhBao.Text) ? 0 : 1); // chỉ số dòng tiêu đề cột
            for (int r = 0; r < du.Count; r++)
            {
                sb.Append("<Row>");
                bool laTieuDeCot = r == soDongTieuDe;
                foreach (object v in du[r])
                    sb.Append(OXml(v, laTieuDeCot));
                sb.Append("</Row>");
            }
            sb.Append("</Table></Worksheet></Workbook>");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static string OXml(object v, bool laTieuDe)
        {
            if (laTieuDe)
                return "<Cell ss:StyleID=\"hdr\"><Data ss:Type=\"String\">" + EscXml(Convert.ToString(v)) + "</Data></Cell>";
            if (v == null || v == DBNull.Value)
                return "<Cell/>";
            if (v is DateTime)
            {
                DateTime d = (DateTime)v;
                if (d == DateTime.MinValue) return "<Cell/>";
                return "<Cell ss:StyleID=\"date\"><Data ss:Type=\"DateTime\">" + d.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture) + "</Data></Cell>";
            }
            if (v is decimal || v is double || v is int || v is long)
                return "<Cell ss:StyleID=\"num\"><Data ss:Type=\"Number\">" + Convert.ToDecimal(v).ToString(CultureInfo.InvariantCulture) + "</Data></Cell>";
            return "<Cell><Data ss:Type=\"String\">" + EscXml(Convert.ToString(v)) + "</Data></Cell>";
        }

        private static string EscXml(string s)
        {
            if (s == null) return String.Empty;
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        // PDF: in qua hộp thoại in của Windows; chọn "Microsoft Print to PDF" để lưu tệp PDF
        private void XuatPdf()
        {
            List<object[]> du = LayDuLieuXuat();
            int dauDuLieu = du.FindIndex(d => d.Length == TieuDeCot.Length && Convert.ToString(d[0]) == TieuDeCot[0]);
            if (dauDuLieu < 0 || du.Count <= dauDuLieu + 1) return;

            MessageBox.Show(this, "Trong hộp thoại in, chọn máy in \"Microsoft Print to PDF\" rồi chọn nơi lưu tệp.",
                "Xuất PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);

            float[] trongSo = { 1.0f, 1.2f, 1.8f, 1.0f, 1.0f, 1.2f, 1.3f, 1.1f, 1.2f };
            Font fChu = new Font("Arial", 8f);
            Font fDam = new Font("Arial", 8f, FontStyle.Bold);
            Font fTieuDe = new Font("Arial", 12f, FontStyle.Bold);
            int dong = dauDuLieu + 1; // dòng dữ liệu đầu tiên
            bool trangDau = true;

            using (PrintDocument doc = new PrintDocument())
            using (PrintDialog hop = new PrintDialog())
            {
                doc.DefaultPageSettings.Landscape = true;
                doc.PrintPage += (s, ev) =>
                {
                    Graphics g = ev.Graphics;
                    RectangleF vung = ev.MarginBounds;
                    float tong = 0;
                    foreach (float t in trongSo) tong += t;
                    float[] rong = new float[trongSo.Length];
                    for (int i = 0; i < rong.Length; i++) rong[i] = vung.Width * trongSo[i] / tong;
                    float h = fChu.GetHeight(g) + 6;
                    float y = vung.Top;

                    if (trangDau)
                    {
                        g.DrawString("Báo cáo tồn kho tại ngày", fTieuDe, Brushes.Black, vung.Left, y);
                        y += 24;
                        g.DrawString(lblTongKet.Text, fChu, Brushes.Black, vung.Left, y);
                        y += 20;
                        if (!String.IsNullOrEmpty(lblCanhBao.Text))
                        {
                            SizeF warningSize=g.MeasureString(lblCanhBao.Text,fChu,(int)vung.Width);
                            g.DrawString(lblCanhBao.Text,fChu,Brushes.DarkRed,new RectangleF(vung.Left,y,vung.Width,warningSize.Height));
                            y += warningSize.Height+6;
                        }
                        trangDau = false;
                    }

                    VeMotDong(g, du[dauDuLieu], rong, vung.Left, y, h, fDam);
                    y += h;
                    while (dong < du.Count)
                    {
                        if (y + h > vung.Bottom)
                        {
                            ev.HasMorePages = true;
                            return;
                        }
                        VeMotDong(g, du[dong], rong, vung.Left, y, h, fChu);
                        y += h;
                        dong++;
                    }
                    ev.HasMorePages = false;
                };

                hop.Document = doc;
                hop.UseEXDialog = true;
                if (hop.ShowDialog(this) == DialogResult.OK)
                    doc.Print();
            }
        }

        private static void VeMotDong(Graphics g, object[] d, float[] rong, float x, float y, float h, Font font)
        {
            StringFormat fmt = new StringFormat(StringFormatFlags.NoWrap);
            fmt.Trimming = StringTrimming.EllipsisCharacter;
            float cx = x;
            for (int i = 0; i < d.Length && i < rong.Length; i++)
            {
                g.DrawRectangle(Pens.Gray, cx, y, rong[i], h);
                g.DrawString(GiaTriIn(d[i]), font, Brushes.Black, new RectangleF(cx + 2, y + 3, rong[i] - 4, h - 4), fmt);
                cx += rong[i];
            }
        }

        private static string GiaTriIn(object value)
        {
            if (value is decimal) return ((decimal)value).ToString("#,##0", CultureInfo.InvariantCulture);
            return GiaTriCsv(value);
        }

        private static string GiaTriCsv(object value)
        {
            if (value == null || value == DBNull.Value) return String.Empty;
            if (value is DateTime) return ((DateTime)value).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            if (value is decimal) return ((decimal)value).ToString(CultureInfo.InvariantCulture);
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static string EscapeCsv(string value)
        {
            if (value == null) value = String.Empty;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}