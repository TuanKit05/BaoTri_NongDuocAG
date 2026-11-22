using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    // Quản lý ưu đãi (bảng UU_DAI). Chỉ người có quyền ThietLapUuDai mới mở và sửa được.
    // Phiếu đã lập giữ mức giảm tự động lúc lập, nên sửa hoặc xóa ưu đãi không làm đổi tiền phiếu cũ.
    public partial class frmQLUuDai : Form
    {
        private const String LoaiGiamPhanTram = "GIAM_PHAN_TRAM";
        private const String LoaiKhuyenMai = "KHUYEN_MAI";
        private const String PhamViHoaDon = "HOA_DON";
        private static readonly String[] KhoaDieuKien = { "TOI_THIEU_TIEN", "TOI_THIEU_SL", "MUA_SL", "TANG_SL" };

        private BindingSource nguon = new BindingSource();
        private object idDangChon; // null khi đang nhập ưu đãi mới

        public frmQLUuDai()
        {
            InitializeComponent();
            // Gắn sự kiện bằng code, không double-click trong Designer, để không bị trùng hàm
            this.Load += frmQLUuDai_Load;
            this.Load += delegate { SecuredUi.ApplyForm(this); };
            btnThem.Click += btnThem_Click;
            btnLuu.Click += btnLuu_Click;
            btnXoa.Click += btnXoa_Click;
            btnDong.Click += delegate { this.Close(); };
            dgvUuDai.SelectionChanged += dgvUuDai_SelectionChanged;
        }

        private void frmQLUuDai_Load(object sender, EventArgs e)
        {
            if (!AccessControl.Can("ThietLapUuDai"))
            {
                MessageBox.Show("Bạn không có quyền thiết lập ưu đãi.", "Quản lý ưu đãi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Close();
                return;
            }
            cmbLoai.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLoai.Items.Clear();
            cmbLoai.Items.Add(LoaiGiamPhanTram);
            cmbLoai.Items.Add(LoaiKhuyenMai);
            numGiaTri.Minimum = 0;
            numGiaTri.Maximum = 100;
            dgvUuDai.ReadOnly = true;
            dgvUuDai.AllowUserToAddRows = false;
            dgvUuDai.MultiSelect = false;
            dgvUuDai.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvUuDai.DataSource = nguon;
            TaiLai();
            XoaNhap();
        }

        private void TaiLai()
        {
            try
            {
                DataTable tbl = new DataTable();
                using (OleDbConnection c = new OleDbConnection(DataService.m_ConnectString))
                using (OleDbDataAdapter da = new OleDbDataAdapter("SELECT * FROM UU_DAI ORDER BY TU_NGAY DESC", c))
                {
                    da.Fill(tbl);
                }
                nguon.DataSource = tbl;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không tải được danh sách ưu đãi: " + ex.Message, "Quản lý ưu đãi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvUuDai_SelectionChanged(object sender, EventArgs e)
        {
            DataRowView rv = nguon.Current as DataRowView;
            if (rv == null)
                return;
            idDangChon = rv["ID"];
            String loai = Convert.ToString(rv["LOAI"]);
            cmbLoai.SelectedItem = cmbLoai.Items.Contains(loai) ? loai : null;
            numGiaTri.Value = KepTrong(DocSo(rv["GIA_TRI"]), 0m, 100m);
            txtMaSanPham.Text = Convert.ToString(rv["MA_SAN_PHAM"]);
            txtDieuKien.Text = Convert.ToString(rv["DIEU_KIEN"]);
            dtTuNgay.Value = rv["TU_NGAY"] == DBNull.Value ? DateTime.Today : Convert.ToDateTime(rv["TU_NGAY"]);
            bool khongHetHan = rv["DEN_NGAY"] == DBNull.Value;
            chkKhongHetHan.Checked = khongHetHan;
            dtDenNgay.Value = khongHetHan ? DateTime.Today.AddMonths(1) : Convert.ToDateTime(rv["DEN_NGAY"]);
        }

        private void XoaNhap()
        {
            idDangChon = null;
            cmbLoai.SelectedIndex = 0;
            numGiaTri.Value = 0;
            txtMaSanPham.Clear();
            txtDieuKien.Clear();
            dtTuNgay.Value = DateTime.Today;
            chkKhongHetHan.Checked = false;
            dtDenNgay.Value = DateTime.Today.AddMonths(1);
        }

        private void btnThem_Click(object sender, EventArgs e)
        {
            // Bỏ chọn trước: sự kiện chọn dòng chạy đồng bộ và sẽ điền lại ô nhập, nên XoaNhap phải đứng sau
            dgvUuDai.ClearSelection();
            XoaNhap();
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            if (!AccessControl.Can("ThietLapUuDai"))
                return;

            String loi = KiemTraNhap();
            if (loi != null)
            {
                MessageBox.Show(loi, "Quản lý ưu đãi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            String loai = Convert.ToString(cmbLoai.SelectedItem);
            decimal giaTri = loai == LoaiGiamPhanTram ? numGiaTri.Value : 0m;
            String maSp = txtMaSanPham.Text.Trim();
            String dieuKien = txtDieuKien.Text.Trim();
            DateTime tuNgay = dtTuNgay.Value.Date;
            bool coDenNgay = !chkKhongHetHan.Checked;
            DateTime denNgay = dtDenNgay.Value.Date;

            try
            {
                using (OleDbConnection c = new OleDbConnection(DataService.m_ConnectString))
                {
                    c.Open();
                    OleDbCommand cmd;
                    if (idDangChon == null)
                    {
                        cmd = new OleDbCommand("INSERT INTO UU_DAI (LOAI, PHAM_VI, GIA_TRI, MA_SAN_PHAM, DIEU_KIEN, TU_NGAY, DEN_NGAY, NGUOI_THIET_LAP) VALUES (?, ?, ?, ?, ?, ?, ?, ?)", c);
                    }
                    else
                    {
                        cmd = new OleDbCommand("UPDATE UU_DAI SET LOAI = ?, PHAM_VI = ?, GIA_TRI = ?, MA_SAN_PHAM = ?, DIEU_KIEN = ?, TU_NGAY = ?, DEN_NGAY = ? WHERE ID = ?", c);
                    }
                    cmd.Parameters.Add("loai", OleDbType.VarWChar, 30).Value = loai;
                    cmd.Parameters.Add("phamVi", OleDbType.VarWChar, 30).Value = PhamViHoaDon;
                    cmd.Parameters.Add("giaTri", OleDbType.Double).Value = (double)giaTri;
                    cmd.Parameters.Add("maSp", OleDbType.VarWChar, 50).Value = maSp.Length == 0 ? (object)DBNull.Value : maSp;
                    cmd.Parameters.Add("dieuKien", OleDbType.LongVarWChar).Value = dieuKien.Length == 0 ? (object)DBNull.Value : dieuKien;
                    cmd.Parameters.Add("tuNgay", OleDbType.Date).Value = tuNgay;
                    cmd.Parameters.Add("denNgay", OleDbType.Date).Value = coDenNgay ? (object)denNgay : DBNull.Value;
                    if (idDangChon == null)
                        cmd.Parameters.Add("nguoi", OleDbType.VarWChar, 50).Value = AppSession.CurrentUser.Id;
                    else
                        cmd.Parameters.AddWithValue("id", idDangChon);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không lưu được ưu đãi: " + ex.Message, "Quản lý ưu đãi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            TaiLai();
            XoaNhap();
            MessageBox.Show("Đã lưu ưu đãi. Phiếu đã lập không đổi; chỉ phiếu lập sau thời điểm này mới áp dụng.", "Quản lý ưu đãi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnXoa_Click(object sender, EventArgs e)
        {
            if (idDangChon == null || !AccessControl.Can("ThietLapUuDai"))
                return;
            if (MessageBox.Show("Xóa ưu đãi này? Phiếu đã lập vẫn giữ mức giảm cũ.", "Quản lý ưu đãi",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                using (OleDbConnection c = new OleDbConnection(DataService.m_ConnectString))
                {
                    c.Open();
                    OleDbCommand cmd = new OleDbCommand("DELETE FROM UU_DAI WHERE ID = ?", c);
                    cmd.Parameters.AddWithValue("id", idDangChon);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không xóa được ưu đãi: " + ex.Message, "Quản lý ưu đãi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            TaiLai();
            XoaNhap();
        }

        // Trả về thông báo lỗi đầu tiên, hoặc null nếu dữ liệu hợp lệ
        private String KiemTraNhap()
        {
            if (cmbLoai.SelectedItem == null)
                return "Chọn loại ưu đãi.";

            String loai = Convert.ToString(cmbLoai.SelectedItem);
            if (loai == LoaiGiamPhanTram && numGiaTri.Value <= 0)
                return "Ưu đãi giảm % phải có mức lớn hơn 0.";

            if (!chkKhongHetHan.Checked && dtDenNgay.Value.Date < dtTuNgay.Value.Date)
                return "Ngày kết thúc không được trước ngày bắt đầu.";

            Dictionary<String, decimal> dieuKien = new Dictionary<String, decimal>();
            String loiDieuKien;
            if (!TachDieuKien(txtDieuKien.Text, dieuKien, out loiDieuKien))
                return loiDieuKien;

            if (loai == LoaiKhuyenMai)
            {
                if (!dieuKien.ContainsKey("MUA_SL") || !dieuKien.ContainsKey("TANG_SL") ||
                    dieuKien["MUA_SL"] <= 0 || dieuKien["TANG_SL"] <= 0)
                    return "Khuyến mãi mua tặng cần MUA_SL và TANG_SL lớn hơn 0 trong điều kiện.";
            }
            return null;
        }

        // Đọc điều kiện dạng "KHOA=GIA_TRI;KHOA=GIA_TRI". Trả false và nêu lỗi nếu sai.
        private static bool TachDieuKien(String text, Dictionary<String, decimal> ketQua, out String loi)
        {
            loi = null;
            if (String.IsNullOrWhiteSpace(text))
                return true;
            foreach (String cap in text.Split(';'))
            {
                String c = cap.Trim();
                if (c.Length == 0)
                    continue;
                int dau = c.IndexOf('=');
                if (dau <= 0)
                {
                    loi = "Điều kiện sai dạng KHOA=GIA_TRI: " + c;
                    return false;
                }
                String khoa = c.Substring(0, dau).Trim().ToUpperInvariant();
                if (Array.IndexOf(KhoaDieuKien, khoa) < 0)
                {
                    loi = "Khóa điều kiện không được hỗ trợ: " + khoa;
                    return false;
                }
                decimal v;
                if (!decimal.TryParse(c.Substring(dau + 1).Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out v) || v < 0)
                {
                    loi = "Giá trị không hợp lệ cho " + khoa + ".";
                    return false;
                }
                ketQua[khoa] = v;
            }
            return true;
        }

        private static decimal DocSo(object o)
        {
            return o == null || o == DBNull.Value ? 0m : Convert.ToDecimal(o);
        }

        private static decimal KepTrong(decimal v, decimal min, decimal max)
        {
            return v < min ? min : (v > max ? max : v);
        }
    }
}
