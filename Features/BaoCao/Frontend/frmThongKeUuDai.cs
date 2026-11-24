using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using CuahangNongduoc;

namespace CuahangNongduoc.Features.BaoCao.ThongKeUuDai
{
    public partial class frmThongKeUuDai : Form
    {
        private const string ALL_EMPLOYEES = "*";
        private readonly List<ThongKeUuDai> ketQua = new List<ThongKeUuDai>();
        private DateTime ngayBatDauMacDinh;
        private DateTime ngayKetThucMacDinh;

        public frmThongKeUuDai()
        {
            InitializeComponent();
            CuahangNongduoc.ResponsiveLayout.Attach(this);
            this.Load += new EventHandler(frmThongKeUuDai_Load);
            this.btnThongKe.Click += new EventHandler(btnThongKe_Click);
            this.btnKhoiPhuc.Click += new EventHandler(btnKhoiPhuc_Click);
            this.btnXuatExcel.Click += new EventHandler(btnXuatExcel_Click);
        }

        private void frmThongKeUuDai_Load(object sender, EventArgs e)
        {
            ngayBatDauMacDinh = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ngayKetThucMacDinh = DateTime.Today;
            this.dtpTuNgay.Value = ngayBatDauMacDinh;
            this.dateTimePicker1.Value = ngayKetThucMacDinh;
            CauHinhLuoi();

            bool coQuyen = AccessControl.Can("BaoCaoBanHang");
            this.cbTenNV.Enabled = AccessControl.Can("XemTatCaPhieuBan");
            this.btnThongKe.Enabled = coQuyen;
            this.btnXuatExcel.Enabled = false;

            if (!coQuyen)
            {
                this.Text = "Thống kê ưu đãi - không đủ quyền";
                MessageBox.Show(this,
                    "Chức năng này chỉ dành cho quản lý hoặc chủ cửa hàng. Hãy gán quyền sau khi đăng nhập thành công.",
                    "Không đủ quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                NapDanhSachNhanVien();
            }
            catch (Exception ex)
            {
                HienThiLoi("Không tải được danh sách nhân viên", ex);
            }
        }

        private void CauHinhLuoi()
        {
            this.dgvThongKeUuDai.AutoGenerateColumns = false;
            this.dgvThongKeUuDai.ReadOnly = true;
            this.dgvThongKeUuDai.AllowUserToAddRows = false;
            this.dgvThongKeUuDai.AllowUserToDeleteRows = false;
            this.dgvThongKeUuDai.MultiSelect = false;
            this.dgvThongKeUuDai.Columns.Clear();

            ThemCot("MaHoaDon", "Mã hóa đơn", 85F);
            ThemCot("NgayBan", "Ngày bán", 75F, "dd/MM/yyyy");
            ThemCot("MaNhanVien", "Mã nhân viên", 80F);
            ThemCot("TenNhanVien", "Nhân viên", 110F);
            ThemCot("TongTien", "Tổng tiền", 90F, "N0");
            ThemCot("TienChietKhauGiamGia", "Chiết khấu / giảm giá", 105F, "N0");
            ThemCot("TienKhuyenMai", "Khuyến mãi", 90F, "N0");
            ThemCot("SoLuongHangTang", "SL hàng tặng", 70F, "N0");
            ThemCot("GiaVonHangTang", "Giá vốn hàng tặng", 95F, "N0");
            ThemCot("TongUuDai", "Tổng ưu đãi", 90F, "N0");
            ThemCot("GhiChuKhuyenMai", "Ghi chú", 130F);
            ApDungQuyenGiaVon();
        }

        private void ThemCot(string propertyName, string header, float fillWeight)
        {
            ThemCot(propertyName, header, fillWeight, String.Empty);
        }

        private void ThemCot(string propertyName, string header, float fillWeight, string format)
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.Name = propertyName;
            column.DataPropertyName = propertyName;
            column.HeaderText = header;
            column.FillWeight = fillWeight;
            column.ReadOnly = true;
            if (!String.IsNullOrEmpty(format)) column.DefaultCellStyle.Format = format;
            this.dgvThongKeUuDai.Columns.Add(column);
        }

        private void NapDanhSachNhanVien()
        {
            DataTable table = new DataTable();
            table.Columns.Add("ID", typeof(string));
            table.Columns.Add("HO_TEN", typeof(string));
            if (!AccessControl.Can("XemTatCaPhieuBan"))
            {
                table.Rows.Add(AppSession.CurrentUser.Id, AppSession.CurrentUser.FullName);
                this.cbTenNV.DataSource = table; this.cbTenNV.DisplayMember = "HO_TEN"; this.cbTenNV.ValueMember = "ID";
                this.cbTenNV.SelectedIndex = 0; return;
            }
            table.Rows.Add(ALL_EMPLOYEES, "Tất cả nhân viên");

            using (OleDbConnection connection = new OleDbConnection(DataService.m_ConnectString))
            using (OleDbCommand command = new OleDbCommand(
                "SELECT ID, HO_TEN FROM TAI_KHOAN ORDER BY HO_TEN", connection))
            {
                connection.Open();
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string id = DocChuoi(reader["ID"]);
                        string name = DocChuoi(reader["HO_TEN"]);
                        if (id.Length > 0) table.Rows.Add(id, name);
                    }
                }
            }

            this.cbTenNV.DataSource = table;
            this.cbTenNV.DisplayMember = "HO_TEN";
            this.cbTenNV.ValueMember = "ID";
            this.cbTenNV.SelectedIndex = 0;
        }

        private void btnThongKe_Click(object sender, EventArgs e)
        {
            if (!AccessControl.Can("BaoCaoBanHang"))
            {
                MessageBox.Show(this, "Bạn không có quyền xem báo cáo toàn bộ nhân viên.",
                    "Không đủ quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (this.dtpTuNgay.Value.Date > this.dateTimePicker1.Value.Date)
            {
                MessageBox.Show(this, "Từ ngày phải nhỏ hơn hoặc bằng đến ngày.",
                    "Khoảng ngày không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string employeeId = LayMaNhanVienDuocChon();
                ketQua.Clear();
                ketQua.AddRange(TruyVanHoaDon(this.dtpTuNgay.Value.Date, this.dateTimePicker1.Value.Date, employeeId));
                HienThiKetQuaTheoNhanVien();
                this.btnXuatExcel.Enabled = ketQua.Count > 0;
                this.Text = "Thống kê ưu đãi - " + ketQua.Count.ToString() + " hóa đơn";
            }
            catch (Exception ex)
            {
                ketQua.Clear();
                this.dgvThongKeUuDai.Rows.Clear();
                this.btnXuatExcel.Enabled = false;
                HienThiLoi("Không lấy được dữ liệu thống kê", ex);
            }
        }

        private string LayMaNhanVienDuocChon()
        {
            if (this.cbTenNV.SelectedValue == null) return null;
            string value = Convert.ToString(this.cbTenNV.SelectedValue);
            return value == ALL_EMPLOYEES ? null : value;
        }

        private List<ThongKeUuDai> TruyVanHoaDon(DateTime fromDate, DateTime toDate, string employeeId)
        {
            AccessControl.DemandCurrent("BaoCaoBanHang");
            if (!AccessControl.Can("XemTatCaPhieuBan")) employeeId = AppSession.CurrentUser.Id;
            List<ThongKeUuDai> rows = new List<ThongKeUuDai>();
            string employeeCondition = String.IsNullOrEmpty(employeeId) ? String.Empty : " AND PB.ID_NHAN_VIEN = ?";
            string sql = "SELECT PB.ID, PB.NGAY_BAN, PB.ID_NHAN_VIEN, T.HO_TEN, PB.TONG_TIEN, " +
                "PB.TIEN_CHIET_KHAU, PB.TIEN_GIAM_GIA, PB.TIEN_KHUYEN_MAI, PB.GHI_CHU_KHUYEN_MAI " +
                "FROM PHIEU_BAN AS PB LEFT JOIN TAI_KHOAN AS T ON PB.ID_NHAN_VIEN = T.ID " +
                "WHERE PB.NGAY_BAN >= ? AND PB.NGAY_BAN < ? " +
                "AND (IIF(IsNull(PB.TIEN_CHIET_KHAU),0,PB.TIEN_CHIET_KHAU) <> 0 " +
                "OR IIF(IsNull(PB.TIEN_GIAM_GIA),0,PB.TIEN_GIAM_GIA) <> 0 " +
                "OR IIF(IsNull(PB.TIEN_KHUYEN_MAI),0,PB.TIEN_KHUYEN_MAI) <> 0 " +
                "OR PB.ID IN (SELECT CT.ID_PHIEU_BAN FROM CHI_TIET_PHIEU_BAN AS CT WHERE CT.LA_HANG_TANG = True)) " +
                employeeCondition + " ORDER BY PB.ID_NHAN_VIEN, PB.NGAY_BAN, PB.ID";

            using (OleDbConnection connection = new OleDbConnection(DataService.m_ConnectString))
            using (OleDbCommand command = new OleDbCommand(sql, connection))
            {
                command.Parameters.Add("?", OleDbType.Date).Value = fromDate;
                command.Parameters.Add("?", OleDbType.Date).Value = toDate.AddDays(1);
                if (!String.IsNullOrEmpty(employeeId))
                    command.Parameters.Add("?", OleDbType.VarWChar).Value = employeeId;

                connection.Open();
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ThongKeUuDai row = new ThongKeUuDai();
                        row.MaHoaDon = DocChuoi(reader["ID"]);
                        row.NgayBan = reader["NGAY_BAN"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["NGAY_BAN"]);
                        row.MaNhanVien = DocChuoi(reader["ID_NHAN_VIEN"]);
                        row.TenNhanVien = DocChuoi(reader["HO_TEN"]);
                        row.TongTien = DocTien(reader["TONG_TIEN"]);
                        row.TienChietKhauGiamGia = DocTien(reader["TIEN_CHIET_KHAU"]) + DocTien(reader["TIEN_GIAM_GIA"]);
                        row.TienKhuyenMai = DocTien(reader["TIEN_KHUYEN_MAI"]);
                        row.GhiChuKhuyenMai = DocChuoi(reader["GHI_CHU_KHUYEN_MAI"]);
                        rows.Add(row);
                    }
                }
            }
            GanHangTangVaoHoaDon(rows, fromDate, toDate, employeeId);
            return rows;
        }

        // YC-BC-03: hàng tặng nằm ở dòng chi tiết (LA_HANG_TANG = True); gắn số lượng và giá vốn đã lưu vào từng hóa đơn
        private static void GanHangTangVaoHoaDon(List<ThongKeUuDai> rows, DateTime fromDate, DateTime toDate, string employeeId)
        {
            AccessControl.DemandCurrent("BaoCaoBanHang");
            bool xemGiaVon = AccessControl.Can("XemGiaVon");
            if (!AccessControl.Can("XemTatCaPhieuBan")) employeeId = AppSession.CurrentUser.Id;
            Dictionary<string, ThongKeUuDai> theoPhieu = new Dictionary<string, ThongKeUuDai>(StringComparer.Ordinal);
            foreach (ThongKeUuDai row in rows) theoPhieu[row.MaHoaDon] = row;

            string employeeCondition = String.IsNullOrEmpty(employeeId) ? String.Empty : " AND PB.ID_NHAN_VIEN = ?";
            string sql = "SELECT CT.ID_PHIEU_BAN, SUM(CT.SO_LUONG) AS SL_TANG, " +
                (xemGiaVon ? "SUM(CT.THANH_TIEN_GIA_VON)" : "0") + " AS GIA_VON_TANG " +
                "FROM CHI_TIET_PHIEU_BAN AS CT INNER JOIN PHIEU_BAN AS PB ON CT.ID_PHIEU_BAN = PB.ID " +
                "WHERE CT.LA_HANG_TANG = True AND PB.NGAY_BAN >= ? AND PB.NGAY_BAN < ?" +
                employeeCondition + " GROUP BY CT.ID_PHIEU_BAN";

            using (OleDbConnection connection = new OleDbConnection(DataService.m_ConnectString))
            using (OleDbCommand command = new OleDbCommand(sql, connection))
            {
                command.Parameters.Add("?", OleDbType.Date).Value = fromDate;
                command.Parameters.Add("?", OleDbType.Date).Value = toDate.AddDays(1);
                if (!String.IsNullOrEmpty(employeeId))
                    command.Parameters.Add("?", OleDbType.VarWChar).Value = employeeId;

                connection.Open();
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ThongKeUuDai row;
                        if (!theoPhieu.TryGetValue(DocChuoi(reader["ID_PHIEU_BAN"]), out row)) continue;
                        row.SoLuongHangTang = (int)DocTien(reader["SL_TANG"]);
                        row.GiaVonHangTang = DocTien(reader["GIA_VON_TANG"]);
                    }
                }
            }
        }

        private void HienThiKetQuaTheoNhanVien()
        {
            AccessControl.DemandCurrent("BaoCaoBanHang");
            if (!AccessControl.Can("XemGiaVon"))
                foreach (ThongKeUuDai item in ketQua) item.GiaVonHangTang = 0;
            this.dgvThongKeUuDai.Rows.Clear();
            string currentEmployee = null;
            string currentEmployeeName = null;
            int invoiceCount = 0;
            decimal sumTotal = 0;
            decimal sumDiscount = 0;
            decimal sumPromotion = 0;
            int sumGiftQty = 0;
            decimal sumGiftCost = 0;

            foreach (ThongKeUuDai row in ketQua)
            {
                if (currentEmployee != null && currentEmployee != row.MaNhanVien)
                {
                    ThemDongTongNhanVien(currentEmployee, currentEmployeeName, invoiceCount, sumTotal,
                        sumDiscount, sumPromotion, sumGiftQty, sumGiftCost);
                    invoiceCount = 0; sumTotal = 0; sumDiscount = 0; sumPromotion = 0;
                    sumGiftQty = 0; sumGiftCost = 0;
                }
                currentEmployee = row.MaNhanVien;
                currentEmployeeName = row.TenNhanVien;
                this.dgvThongKeUuDai.Rows.Add(row.MaHoaDon, row.NgayBan, row.MaNhanVien,
                    row.TenNhanVien, row.TongTien, row.TienChietKhauGiamGia, row.TienKhuyenMai,
                    row.SoLuongHangTang, row.GiaVonHangTang, row.TongUuDai, row.GhiChuKhuyenMai);
                invoiceCount++;
                sumTotal += row.TongTien;
                sumDiscount += row.TienChietKhauGiamGia;
                sumPromotion += row.TienKhuyenMai;
                sumGiftQty += row.SoLuongHangTang;
                sumGiftCost += row.GiaVonHangTang;
            }

            if (currentEmployee != null)
                ThemDongTongNhanVien(currentEmployee, currentEmployeeName, invoiceCount, sumTotal,
                    sumDiscount, sumPromotion, sumGiftQty, sumGiftCost);
            ApDungQuyenGiaVon();
        }

        // Clear cached costs too, so a permission revoked after viewing cannot leak through export or totals.
        private void ApDungQuyenGiaVon()
        {
            bool allowed = AccessControl.Can("XemGiaVon");
            DataGridViewColumn cost = dgvThongKeUuDai.Columns["GiaVonHangTang"];
            if (cost == null) return;
            cost.Visible = allowed;
            if (allowed) return;
            foreach (ThongKeUuDai item in ketQua) item.GiaVonHangTang = 0;
            foreach (DataGridViewRow row in dgvThongKeUuDai.Rows)
            {
                if (row.IsNewRow) continue;
                row.Cells["GiaVonHangTang"].Value = 0m;
                row.Cells["TongUuDai"].Value = DocTien(row.Cells["TienChietKhauGiamGia"].Value) +
                    DocTien(row.Cells["TienKhuyenMai"].Value);
            }
        }

        private void ThemDongTongNhanVien(string employeeId, string employeeName, int count,
            decimal total, decimal discount, decimal promotion, int giftQty, decimal giftCost)
        {
            int index = this.dgvThongKeUuDai.Rows.Add(
                "TỔNG " + count.ToString() + " HÓA ĐƠN", null, employeeId, employeeName,
                total, discount, promotion, giftQty, giftCost, discount + promotion + giftCost, String.Empty);
            DataGridViewRow row = this.dgvThongKeUuDai.Rows[index];
            row.DefaultCellStyle.Font = new Font(this.dgvThongKeUuDai.Font, FontStyle.Bold);
            row.DefaultCellStyle.BackColor = Color.AliceBlue;
        }

        private void btnKhoiPhuc_Click(object sender, EventArgs e)
        {
            this.dtpTuNgay.Value = ngayBatDauMacDinh;
            this.dateTimePicker1.Value = ngayKetThucMacDinh;
            if (this.cbTenNV.Items.Count > 0) this.cbTenNV.SelectedIndex = 0;
            ketQua.Clear();
            this.dgvThongKeUuDai.Rows.Clear();
            this.btnXuatExcel.Enabled = false;
            this.Text = "Thống kê hóa đơn giảm giá và khuyến mãi";
        }

        // YC-BC-04: xuất Excel (.xls, mở được bằng Excel), PDF (in qua "Microsoft Print to PDF"), CSV
        private void btnXuatExcel_Click(object sender, EventArgs e)
        {
            if (this.dgvThongKeUuDai.Rows.Count == 0) return;
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Xuất báo cáo ưu đãi";
                dialog.Filter = "Excel (*.xls)|*.xls|PDF (*.pdf)|*.pdf|CSV (*.csv)|*.csv";
                dialog.FileName = "ThongKeUuDai";
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
                    MessageBox.Show(this, "Đã xuất báo cáo.", "Xuất báo cáo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    HienThiLoi("Không xuất được tệp", ex);
                }
            }
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

        // Dòng 0 là tiêu đề cột; các dòng sau là dữ liệu và dòng tổng từng nhân viên (đúng như đang hiển thị)
        private List<object[]> LayDuLieuXuat()
        {
            AccessControl.DemandCurrent("BaoCaoBanHang");
            ApDungQuyenGiaVon();
            List<DataGridViewColumn> columns = new List<DataGridViewColumn>();
            foreach (DataGridViewColumn column in dgvThongKeUuDai.Columns)
                if (column.Visible) columns.Add(column);
            List<object[]> ds = new List<object[]>();
            object[] tieuDe = new object[columns.Count];
            for (int i = 0; i < tieuDe.Length; i++)
                tieuDe[i] = columns[i].HeaderText;
            ds.Add(tieuDe);
            foreach (DataGridViewRow row in this.dgvThongKeUuDai.Rows)
            {
                if (row.IsNewRow) continue;
                object[] dong = new object[columns.Count];
                for (int i = 0; i < dong.Length; i++)
                    dong[i] = row.Cells[columns[i].Index].Value;
                ds.Add(dong);
            }
            return ds;
        }

        // Excel 2003 XML: mở trực tiếp bằng Excel, giữ định dạng số và ngày
        private void XuatExcelXml(string path)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.Append("<?mso-application progid=\"Excel.Sheet\"?>");
            sb.Append("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
            sb.Append("<Styles>");
            sb.Append("<Style ss:ID=\"hdr\"><Font ss:Bold=\"1\"/><Interior ss:Color=\"#DDEBF7\" ss:Pattern=\"Solid\"/></Style>");
            sb.Append("<Style ss:ID=\"num\"><NumberFormat ss:Format=\"#,##0\"/></Style>");
            sb.Append("<Style ss:ID=\"date\"><NumberFormat ss:Format=\"dd/mm/yyyy\"/></Style>");
            sb.Append("</Styles>");
            sb.Append("<Worksheet ss:Name=\"ThongKeUuDai\"><Table>");
            bool tieuDe = true;
            foreach (object[] dong in LayDuLieuXuat())
            {
                sb.Append("<Row>");
                foreach (object v in dong)
                    sb.Append(OXml(v, tieuDe));
                sb.Append("</Row>");
                tieuDe = false;
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

        // PDF: in qua hộp thoại in của Windows; chọn máy in "Microsoft Print to PDF" để lưu tệp PDF
        private void XuatPdf()
        {
            List<object[]> du = LayDuLieuXuat();
            if (du.Count <= 1) return;

            MessageBox.Show(this, "Trong hộp thoại in, chọn máy in \"Microsoft Print to PDF\" rồi chọn nơi lưu tệp.",
                "Xuất PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);

            List<float> trongSo = new List<float>();
            foreach (DataGridViewColumn column in dgvThongKeUuDai.Columns)
                if (column.Visible) trongSo.Add(column.FillWeight);
            Font fChu = new Font("Arial", 8f);
            Font fDam = new Font("Arial", 8f, FontStyle.Bold);
            Font fTieuDe = new Font("Arial", 12f, FontStyle.Bold);
            string khoangNgay = "Từ " + this.dtpTuNgay.Value.ToString("dd/MM/yyyy") + " đến " + this.dateTimePicker1.Value.ToString("dd/MM/yyyy");
            int dong = 1;
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
                    float[] rong = new float[trongSo.Count];
                    for (int i = 0; i < rong.Length; i++) rong[i] = vung.Width * trongSo[i] / tong;
                    float h = fChu.GetHeight(g) + 6;
                    float y = vung.Top;

                    if (trangDau)
                    {
                        g.DrawString("Thống kê hóa đơn giảm giá và khuyến mãi", fTieuDe, Brushes.Black, vung.Left, y);
                        y += 24;
                        g.DrawString(khoangNgay, fChu, Brushes.Black, vung.Left, y);
                        y += 20;
                        trangDau = false;
                    }

                    VeMotDong(g, du[0], rong, vung.Left, y, h, fDam);
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
            return Convert.ToString(value);
        }

        private static string EscapeCsv(string value)
        {
            if (value == null) value = String.Empty;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static decimal DocTien(object value)
        {
            if (value == null || value == DBNull.Value) return Decimal.Zero;
            return Convert.ToDecimal(value);
        }

        private static string DocChuoi(object value)
        {
            if (value == null || value == DBNull.Value) return String.Empty;
            return Convert.ToString(value);
        }

        private void HienThiLoi(string noiDung, Exception ex)
        {
            MessageBox.Show(this, noiDung + ": " + ErrorLog.UserMessage(ex, "Kh\u00f4ng th\u1ec3 ho\u00e0n t\u1ea5t thao t\u00e1c"), "Lỗi",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
