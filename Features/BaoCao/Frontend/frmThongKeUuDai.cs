using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
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

            bool coQuyen = ThongKeUuDaiSession.CoQuyenXemToanBo;
            this.cbTenNV.Enabled = coQuyen;
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
            ThemCot("TongUuDai", "Tổng ưu đãi", 90F, "N0");
            ThemCot("GhiChuKhuyenMai", "Ghi chú", 130F);
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
            if (!ThongKeUuDaiSession.CoQuyenXemToanBo)
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
            List<ThongKeUuDai> rows = new List<ThongKeUuDai>();
            string employeeCondition = String.IsNullOrEmpty(employeeId) ? String.Empty : " AND PB.ID_NHAN_VIEN = ?";
            string sql = "SELECT PB.ID, PB.NGAY_BAN, PB.ID_NHAN_VIEN, T.HO_TEN, PB.TONG_TIEN, " +
                "PB.TIEN_CHIET_KHAU, PB.TIEN_GIAM_GIA, PB.TIEN_KHUYEN_MAI, PB.GHI_CHU_KHUYEN_MAI " +
                "FROM PHIEU_BAN AS PB LEFT JOIN TAI_KHOAN AS T ON PB.ID_NHAN_VIEN = T.ID " +
                "WHERE PB.NGAY_BAN >= ? AND PB.NGAY_BAN < ? " +
                "AND (IIF(IsNull(PB.TIEN_CHIET_KHAU),0,PB.TIEN_CHIET_KHAU) <> 0 " +
                "OR IIF(IsNull(PB.TIEN_GIAM_GIA),0,PB.TIEN_GIAM_GIA) <> 0 " +
                "OR IIF(IsNull(PB.TIEN_KHUYEN_MAI),0,PB.TIEN_KHUYEN_MAI) <> 0) " +
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
            return rows;
        }

        private void HienThiKetQuaTheoNhanVien()
        {
            this.dgvThongKeUuDai.Rows.Clear();
            string currentEmployee = null;
            string currentEmployeeName = null;
            int invoiceCount = 0;
            decimal sumTotal = 0;
            decimal sumDiscount = 0;
            decimal sumPromotion = 0;

            foreach (ThongKeUuDai row in ketQua)
            {
                if (currentEmployee != null && currentEmployee != row.MaNhanVien)
                {
                    ThemDongTongNhanVien(currentEmployee, currentEmployeeName, invoiceCount, sumTotal, sumDiscount, sumPromotion);
                    invoiceCount = 0; sumTotal = 0; sumDiscount = 0; sumPromotion = 0;
                }
                currentEmployee = row.MaNhanVien;
                currentEmployeeName = row.TenNhanVien;
                this.dgvThongKeUuDai.Rows.Add(row.MaHoaDon, row.NgayBan, row.MaNhanVien,
                    row.TenNhanVien, row.TongTien, row.TienChietKhauGiamGia, row.TienKhuyenMai,
                    row.TongUuDai, row.GhiChuKhuyenMai);
                invoiceCount++;
                sumTotal += row.TongTien;
                sumDiscount += row.TienChietKhauGiamGia;
                sumPromotion += row.TienKhuyenMai;
            }

            if (currentEmployee != null)
                ThemDongTongNhanVien(currentEmployee, currentEmployeeName, invoiceCount, sumTotal, sumDiscount, sumPromotion);
        }

        private void ThemDongTongNhanVien(string employeeId, string employeeName, int count,
            decimal total, decimal discount, decimal promotion)
        {
            int index = this.dgvThongKeUuDai.Rows.Add(
                "TỔNG " + count.ToString() + " HÓA ĐƠN", null, employeeId, employeeName,
                total, discount, promotion, discount + promotion, String.Empty);
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

        private void btnXuatExcel_Click(object sender, EventArgs e)
        {
            if (this.dgvThongKeUuDai.Rows.Count == 0) return;
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Xuất báo cáo ưu đãi";
                dialog.Filter = "Tệp CSV mở bằng Excel (*.csv)|*.csv";
                dialog.FileName = "ThongKeUuDai.csv";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    using (StreamWriter writer = new StreamWriter(dialog.FileName, false, new UTF8Encoding(true)))
                    {
                        writer.WriteLine("Mã hóa đơn,Ngày bán,Mã nhân viên,Nhân viên,Tổng tiền,Chiết khấu/giảm giá,Khuyến mãi,Tổng ưu đãi,Ghi chú");
                        foreach (DataGridViewRow row in this.dgvThongKeUuDai.Rows)
                        {
                            if (row.IsNewRow) continue;
                            for (int i = 0; i < row.Cells.Count; i++)
                            {
                                if (i > 0) writer.Write(",");
                                writer.Write(EscapeCsv(GiaTriCsv(row.Cells[i].Value)));
                            }
                            writer.WriteLine();
                        }
                    }
                    MessageBox.Show(this, "Đã xuất tệp CSV. Bạn có thể mở tệp này bằng Excel.",
                        "Xuất báo cáo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    HienThiLoi("Không xuất được tệp", ex);
                }
            }
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
            MessageBox.Show(this, noiDung + ": " + ex.Message, "Lỗi",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
