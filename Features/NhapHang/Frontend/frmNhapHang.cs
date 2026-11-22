using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using CuahangNongduoc.Controller;
using CuahangNongduoc.BusinessObject;
using CuahangNongduoc.DataLayer;
using CuahangNongduoc.Validation;

namespace CuahangNongduoc
{
    public partial class frmNhapHang : Form
    {
        private SanPhamController ctrlSanPham = new SanPhamController();
        private PhieuNhapController ctrl = new PhieuNhapController();
        private MaSanPhamController ctrlMaSP = new MaSanPhamController();
        private NhaCungCapController ctrlNCC = new NhaCungCapController();
        private PhieuNhapController ctrlDanhSach;
        private Controll status = Controll.AddNew;
        private readonly NhapHangService dichVuNhapHang = new NhapHangService();

        private BindingSource observedSource;
        private bool loading;
        private decimal savedTotal;
        private string savedInvoiceId;
        private readonly ToolStripButton toolHuy = new ToolStripButton("H\u1ee7y");

        public frmNhapHang()
        {
            InitializeComponent();
            dataGridView.AutoGenerateColumns = false;
            DataGridViewTextBoxColumn initialQuantity = new DataGridViewTextBoxColumn();
            initialQuantity.Name = "colSoLuongNhap";
            initialQuantity.DataPropertyName = "SO_LUONG_NHAP";
            initialQuantity.HeaderText = "SL nhập ban đầu";
            initialQuantity.ReadOnly = true;
            initialQuantity.DefaultCellStyle.Format = "N0";
            initialQuantity.DefaultCellStyle.NullValue = "Chưa ghi nhận";
            dataGridView.Columns.Add(initialQuantity);
            toolHuy.Name = "toolHuy";
            bindingNavigator.Items.Insert(bindingNavigator.Items.IndexOf(toolLuuThoat) + 1, toolHuy);
            toolHuy.Click += delegate { CancelChanges(); };
            toolLuuThoat.Text = "L\u01b0u";
            toolLuuThem.Text = "Th\u00eam";
            FormClosed += delegate {
                if (observedSource != null) observedSource.CurrentChanged -= BindingSource_CurrentChanged;
            };

            CuahangNongduoc.ResponsiveLayout.Attach(this);
            this.Load += delegate { SecuredUi.ApplyForm(this); };
            status = Controll.AddNew;
            FormClosing += delegate(object sender, FormClosingEventArgs e) {
                if (status == Controll.Normal) return;
                DialogResult answer = MessageBox.Show("Bạn có muốn lưu thay đổi phiếu nhập trước khi đóng?",
                    "Phiếu nhập", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (answer == DialogResult.Cancel) e.Cancel = true;
                else if (answer == DialogResult.Yes && !Luu()) e.Cancel = true;
                else if (answer == DialogResult.No) CancelChanges();
            };
            CauHinhONhapTien();
            dtNgayNhap.ValueChanged += dtNgayNhap_ValueChanged;
        }

        public frmNhapHang(PhieuNhapController ctrlPN)
            : this()
        {
            if (ctrlPN != null)
            {
                ctrl = ctrlPN;
                ctrlDanhSach = ctrlPN;
                status = Controll.Normal;
            }
        }

        private void CauHinhONhapTien()
        {
            // Tong tien va con no do chuong trinh tu tinh, khong cho sua bang tay.
            numThanhTien.Maximum = Decimal.MaxValue;
            numTongTien.Maximum = Decimal.MaxValue;
            numDaTra.Maximum = Decimal.MaxValue;
            numConNo.Maximum = Decimal.MaxValue;
            numTongTien.ReadOnly = true;
            numConNo.ReadOnly = true;
        }

        private void BindingSource_CurrentChanged(object sender, EventArgs e)
        {
            if (loading || status != Controll.Normal)
                return;

            if (bindingNavigator.BindingSource == null || bindingNavigator.BindingSource.Current == null)
            {
                dataGridView.DataSource = null;
                return;
            }

            DataRowView current = bindingNavigator.BindingSource.Current as DataRowView;
            if (current == null) return;
            loading = true;
            try
            {
                savedInvoiceId = Convert.ToString(current["ID"]);
                savedTotal = Convert.ToDecimal(current["TONG_TIEN"]);
                txtMaPhieu.Text = savedInvoiceId;
                cmbNhaCungCap.SelectedValue = current["ID_NHA_CUNG_CAP"];
                dtNgayNhap.Value = Convert.ToDateTime(current["NGAY_NHAP"]);
                numTongTien.Value = savedTotal;
                numDaTra.Value = Convert.ToDecimal(current["DA_TRA"]);
                numConNo.Value = Convert.ToDecimal(current["CON_NO"]);
                ctrlMaSP.HienThiChiTietPhieuNhap(savedInvoiceId, dataGridView);
            }
            finally { loading = false; }

        }

        /// <summary>
        /// Gan lai su kien khi BindingSource duoc thay bang nguon du lieu cua phieu moi.
        /// Neu khong gan lai, khi chuyen phieu bang BindingNavigator, bang lo hang co the khong cap nhat.
        /// </summary>
        private void GanSuKienBindingSource()
        {
            if (observedSource != null) observedSource.CurrentChanged -= BindingSource_CurrentChanged;
            observedSource = bindingNavigator.BindingSource;
            if (observedSource != null) observedSource.CurrentChanged += BindingSource_CurrentChanged;
        }

        private void frmNhapHang_Load(object sender, EventArgs e)
        {
            ctrlSanPham.HienthiAutoComboBox(cmbSanPham);
            ctrlSanPham.HienthiDataGridViewComboBoxColumn(colSanPham);
            ctrlNCC.HienthiAutoComboBox(cmbNhaCungCap);

            ctrl.HienthiPhieuNhap(bindingNavigator, txtMaPhieu, cmbNhaCungCap,
                dtNgayNhap, numTongTien, numDaTra, numConNo);

            GanSuKienBindingSource();

            if (status == Controll.AddNew)
            {
                BatDauPhieuNhapMoi(false);
            }
            else
            {
                BindingSource_CurrentChanged(this, EventArgs.Empty);
                Allow(false);
            }
        }

        /// <summary>
        /// Neu doi ngay nhap truoc khi luu phieu moi, cap nhat ngay nhap cho cac lo chua luu.
        /// Phieu da luu khong cho sua ngay nay de tranh lech ngay giua dau phieu va cac lo cu.
        /// </summary>
        private void dtNgayNhap_ValueChanged(object sender, EventArgs e)
        {
            if (status != Controll.AddNew || dataGridView.DataSource == null)
                return;

            foreach (DataGridViewRow view in dataGridView.Rows)
            {
                if (view.IsNewRow)
                    continue;

                DataRowView dongDuLieu = view.DataBoundItem as DataRowView;
                if (dongDuLieu != null && dongDuLieu.Row.RowState == DataRowState.Added)
                    dongDuLieu.Row["NGAY_NHAP"] = dtNgayNhap.Value.Date;
            }
        }

        // Su kien khi thay doi gia nhap hoac so luong.
        private void numGiaNhap_ValueChanged(object sender, EventArgs e)
        {
            CapNhatThanhTienLoNhap();
        }

        /// <summary>Extract Method: chi tinh tien lo, khong xu ly cac viec khac.</summary>
        private void CapNhatThanhTienLoNhap()
        {
            decimal thanhTien = numGiaNhap.Value * numSoLuong.Value;
            if (thanhTien > Int64.MaxValue)
            {
                numThanhTien.Value = numThanhTien.Maximum;
                return;
            }
            numThanhTien.Value = thanhTien;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (status == Controll.Normal)
            {
                MessageBox.Show("Hãy chọn Chỉnh sửa trước khi thêm lô vào phiếu đã lưu.",
                    "Phiếu nhập", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!KiemTraMaLoChuaTonTai())
                return;

            string maSanPham = (cmbSanPham.SelectedIndex < 0 || cmbSanPham.SelectedValue == null || cmbSanPham.SelectedValue == DBNull.Value)
                ? String.Empty
                : Convert.ToString(cmbSanPham.SelectedValue);

            string loi = NhapHangValidator.KiemTraThongTinLoNhap(
                txtMaSo.Text.Trim(), maSanPham, numGiaNhap.Value, numSoLuong.Value,
                dtNgayNhap.Value, dtNgaySanXuat.Value, dtNgayHetHan.Value);

            if (!String.IsNullOrEmpty(loi))
            {
                MessageBox.Show(loi, "Phiếu nhập", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (TinhTongTienHienTai() + numThanhTien.Value > Int64.MaxValue)
            {
                MessageBox.Show("Tổng tiền phiếu nhập vượt quá giới hạn lưu trữ.",
                    "Phiếu nhập", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ThemLoNhapVaoDanhSach();
            LamMoiOThongTinLo();
        }

        /// <summary>
        /// Kiem tra ma lo khong rong, chua co trong CSDL va chua bi dung trong phieu hien tai.
        /// Day la Guard Clauses: gap truong hop sai thi dung som.
        /// </summary>
        private bool KiemTraMaLoChuaTonTai()
        {
            string maLo = txtMaSo.Text.Trim();
            if (String.IsNullOrEmpty(maLo))
            {
                MessageBox.Show("Vui lòng nhập mã lô sản phẩm!", "Phiếu nhập",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (ctrlMaSP.LayMaSanPham(maLo) != null)
            {
                MessageBox.Show("Mã lô sản phẩm này đã tồn tại trong kho!", "Phiếu nhập",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            foreach (DataGridViewRow view in dataGridView.Rows)
            {
                if (view.IsNewRow)
                    continue;

                object giaTriMaLo = view.Cells["colMaSanPham"].Value;
                if (giaTriMaLo != null && maLo.Equals(
                    Convert.ToString(giaTriMaLo).Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Mã lô này đã có trong phiếu nhập hiện tại!", "Phiếu nhập",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            return true;
        }

        /// <summary>Extract Method: tao DataRow va gan thong tin cua mot lo hang.</summary>
        private void ThemLoNhapVaoDanhSach()
        {
            DataRow row = ctrlMaSP.NewRow();
            GanThongTinLoNhap(row);
            ctrlMaSP.Add(row);
            CapNhatTongTienTheoChiTiet();
        }

        private void GanThongTinLoNhap(DataRow row)
        {
            row["ID_SAN_PHAM"] = cmbSanPham.SelectedValue;
            row["ID_PHIEU_NHAP"] = txtMaPhieu.Text.Trim();
            row["ID"] = txtMaSo.Text.Trim();
            row["DON_GIA_NHAP"] = Convert.ToInt64(numGiaNhap.Value);
            row["SO_LUONG"] = Convert.ToInt32(numSoLuong.Value);
            row["SO_LUONG_NHAP"] = Convert.ToInt32(numSoLuong.Value);
            row["NGAY_NHAP"] = dtNgayNhap.Value.Date;
            row["NGAY_SAN_XUAT"] = dtNgaySanXuat.Value.Date;
            row["NGAY_HET_HAN"] = dtNgayHetHan.Value.Date;
        }

        private void LamMoiOThongTinLo()
        {
            txtMaSo.Clear();
            numGiaNhap.Value = 0;
            numSoLuong.Value = 0;
            numThanhTien.Value = 0;
            dtNgaySanXuat.Value = dtNgayNhap.Value.Date;
            dtNgayHetHan.Value = dtNgayNhap.Value.Date.AddMonths(1);
            if (txtMaSo.Enabled) txtMaSo.Focus();
        }

        private decimal TinhTongTienHienTai()
        {
            decimal tong = status == Controll.Edit ? savedTotal : 0;
            foreach (DataGridViewRow view in dataGridView.Rows)
            {
                if (view.IsNewRow)
                    continue;

                DataRowView item = view.DataBoundItem as DataRowView;
                if (status == Controll.Edit && (item == null || item.Row.RowState != DataRowState.Added)) continue;
                object gia = view.Cells["colDonGiaNhap"].Value;
                object soLuong = view.Cells["colSoLuong"].Value;
                if (gia == null || gia == DBNull.Value || soLuong == null || soLuong == DBNull.Value)
                    continue;

                decimal thanhTienDong = Convert.ToDecimal(gia) * Convert.ToDecimal(soLuong);
                tong = checked(tong + thanhTienDong);
            }
            return tong;
        }

        private int DemSoLoHienThi()
        {
            int soLo = 0;
            foreach (DataGridViewRow view in dataGridView.Rows)
            {
                if (!view.IsNewRow)
                    soLo++;
            }
            return soLo;
        }

        private bool CapNhatTongTienTheoChiTiet()
        {
            decimal tong = TinhTongTienHienTai();
            if (tong > Int64.MaxValue)
                return false;

            numTongTien.Value = tong;
            CapNhatCongNo();
            return true;
        }

        private void CapNhatCongNo()
        {
            if (loading || status == Controll.Normal) return;
            decimal conNo = numTongTien.Value - numDaTra.Value;
            numConNo.Value = conNo > 0 ? conNo : 0;
        }

        private List<LoNhapInput> LayDanhSachLoMoi()
        {
            List<LoNhapInput> ds = new List<LoNhapInput>();

            foreach (DataGridViewRow view in dataGridView.Rows)
            {
                if (view.IsNewRow)
                    continue;

                DataRowView dongDuLieu = view.DataBoundItem as DataRowView;
                if (dongDuLieu == null || dongDuLieu.Row.RowState != DataRowState.Added)
                    continue;

                DataRow row = dongDuLieu.Row;
                LoNhapInput lo = new LoNhapInput();
                lo.MaLo = Convert.ToString(row["ID"]).Trim();
                lo.MaSanPham = Convert.ToString(row["ID_SAN_PHAM"]).Trim();
                lo.DonGiaNhap = Convert.ToInt64(row["DON_GIA_NHAP"]);
                lo.SoLuong = Convert.ToInt32(row["SO_LUONG"]);
                lo.NgayNhap = Convert.ToDateTime(row["NGAY_NHAP"]).Date;
                lo.NgaySanXuat = Convert.ToDateTime(row["NGAY_SAN_XUAT"]).Date;
                lo.NgayHetHan = Convert.ToDateTime(row["NGAY_HET_HAN"]).Date;
                ds.Add(lo);
            }

            return ds;
        }

        private bool KiemTraTruocKhiLuu(out string thongBao)
        {
            thongBao = String.Empty;
            dataGridView.EndEdit();
            bindingNavigator.BindingSource.EndEdit();

            if (!CapNhatTongTienTheoChiTiet())
            {
                thongBao = "Tổng tiền phiếu nhập vượt quá giới hạn lưu trữ.";
                return false;
            }

            string maNCC = (cmbNhaCungCap.SelectedIndex < 0 || cmbNhaCungCap.SelectedValue == null || cmbNhaCungCap.SelectedValue == DBNull.Value)
                ? String.Empty
                : Convert.ToString(cmbNhaCungCap.SelectedValue);

            thongBao = NhapHangValidator.KiemTraThongTinPhieuNhap(
                txtMaPhieu.Text, maNCC, numTongTien.Value, numDaTra.Value);
            if (!String.IsNullOrEmpty(thongBao))
                return false;

            if (DemSoLoHienThi() == 0)
            {
                thongBao = "Phiếu nhập phải có ít nhất một lô hàng.";
                return false;
            }

            return true;
        }

        private bool Luu()
        {
            string thongBao;
            if (!KiemTraTruocKhiLuu(out thongBao))
            {
                MessageBox.Show(thongBao, "Phiếu nhập", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            string maPhieu = txtMaPhieu.Text.Trim();
            string maNCC = Convert.ToString(cmbNhaCungCap.SelectedValue);
            bool laPhieuMoi = status == Controll.AddNew;
            List<LoNhapInput> cacLoMoi = LayDanhSachLoMoi();

            bool thanhCong = dichVuNhapHang.LuuPhieuNhap(
                maPhieu,
                dtNgayNhap.Value.Date,
                Convert.ToInt64(numTongTien.Value),
                maNCC,
                Convert.ToInt64(numDaTra.Value),
                Convert.ToInt64(numConNo.Value),
                laPhieuMoi,
                cacLoMoi,
                out thongBao);

            if (!thanhCong)
            {
                MessageBox.Show(thongBao, "Phiếu nhập", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }



            status = Controll.Normal;
            NapLaiDuLieuSauKhiLuu(maPhieu);
            BindingSource_CurrentChanged(this, EventArgs.Empty);
            Allow(false);
            MessageBox.Show(thongBao, "Phiếu nhập", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return true;
        }

        private void NapLaiDuLieuSauKhiLuu(string maPhieu)
        {
            ctrl.TaiNapDanhSach();
            ctrl.HienthiPhieuNhap(bindingNavigator, txtMaPhieu, cmbNhaCungCap,
                dtNgayNhap, numTongTien, numDaTra, numConNo);
            GanSuKienBindingSource();
            ctrl.ChonPhieuNhap(maPhieu);
            ctrlMaSP.HienThiChiTietPhieuNhap(maPhieu, dataGridView);

            if (ctrlDanhSach != null && !Object.ReferenceEquals(ctrlDanhSach, ctrl))
                ctrlDanhSach.TaiNapDanhSach();
        }

        private void BatDauPhieuNhapMoi(bool taiLaiDanhSach)
        {
            if (taiLaiDanhSach && ctrlDanhSach != null)
                ctrlDanhSach.TaiNapDanhSach();

            ctrl = new PhieuNhapController();
            ctrl.HienthiPhieuNhap(bindingNavigator, txtMaPhieu, cmbNhaCungCap,
                dtNgayNhap, numTongTien, numDaTra, numConNo);
            GanSuKienBindingSource();

            status = Controll.AddNew;
            txtMaPhieu.Text = IdGenerator.Next("PHIEU_NHAP");
            cmbNhaCungCap.SelectedIndex = -1;
            dtNgayNhap.Value = DateTime.Today;
            numTongTien.Value = 0;
            numDaTra.Value = 0;
            numConNo.Value = 0;

            ctrlMaSP = new MaSanPhamController();
            ctrlMaSP.HienThiChiTietPhieuNhap("-1", dataGridView);
            Allow(true);
            LamMoiOThongTinLo();
        }

        private void toolLuuThoat_Click(object sender, EventArgs e)
        {
            if (status == Controll.Normal)
                return;

            if (Luu())
                Allow(false);
        }

        private void toolLuuThem_Click(object sender, EventArgs e)
        {
            if (status != Controll.Normal) return;

            BatDauPhieuNhapMoi(true);
        }

        private void toolSavePrint_Click(object sender, EventArgs e)
        {
            if (status != Controll.Normal)
            {
                MessageBox.Show("Vui lòng lưu phiếu nhập trước khi in.", "Phiếu nhập",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string maPhieu = txtMaPhieu.Text.Trim();
            PhieuNhap phieu = new PhieuNhapController().LayPhieuNhap(maPhieu);
            if (phieu == null)
            {
                MessageBox.Show("Không tìm thấy phiếu nhập cần in.", "Phiếu nhập",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            frmInPhieuNhap formIn = new frmInPhieuNhap(phieu);
            formIn.Show();
        }

        private void toolThoat_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnThemSanPham_Click(object sender, EventArgs e)
        {
            frmSanPham formSanPham = new frmSanPham();
            formSanPham.ShowDialog();
            ctrlSanPham.HienthiAutoComboBox(cmbSanPham);
        }

        private void dataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = true;
        }

        private void Allow(bool val)
        {
            txtMaPhieu.Enabled = false;
            cmbNhaCungCap.Enabled = val;
            // Ngay cua phieu da luu khong duoc sua de tranh khac ngay tren cac lo da ghi kho.
            dtNgayNhap.Enabled = val && status == Controll.AddNew;
            numTongTien.Enabled = false;
            numDaTra.Enabled = val;
            numConNo.Enabled = false;

            txtMaSo.Enabled = val;
            txtMaSo.ReadOnly = !val;
            cmbSanPham.Enabled = val;
            numGiaNhap.Enabled = val;
            numSoLuong.Enabled = val;
            dtNgaySanXuat.Enabled = val;
            dtNgayHetHan.Enabled = val;
            numThanhTien.Enabled = false;

            btnAdd.Enabled = val;
            btnRemove.Enabled = val;
            btnThemSanPham.Enabled = val;
            btnThemNCC.Enabled = val;
            dataGridView.Enabled = true;
            dataGridView.ReadOnly = true;
            dataGridView.AllowUserToDeleteRows = false;
            bool hasInvoice = bindingNavigator.BindingSource != null && bindingNavigator.BindingSource.Current != null;
            toolLuuThoat.Enabled = val;
            toolHuy.Enabled = val;
            toolLuuThem.Enabled = !val && AccessControl.Can("NhapHang");
            toolChinhsua.Enabled = !val && hasInvoice && AccessControl.Can("NhapHang");
            toolXoa.Enabled = !val && hasInvoice && AccessControl.Can("XoaPhieu");
            toolSavePrint.Enabled = !val && hasInvoice;
            foreach (ToolStripItem item in new ToolStripItem[] { bindingNavigatorMoveFirstItem,
                bindingNavigatorMovePreviousItem, bindingNavigatorMoveNextItem, bindingNavigatorMoveLastItem,
                bindingNavigatorPositionItem }) item.Enabled = !val;

        }

        private void CancelChanges()
        {
            string restoreId = savedInvoiceId;
            status = Controll.Normal;
            ctrl.TaiNapDanhSach();
            ctrl.HienthiPhieuNhap(bindingNavigator, txtMaPhieu, cmbNhaCungCap,
                dtNgayNhap, numTongTien, numDaTra, numConNo);
            GanSuKienBindingSource();
            if (!String.IsNullOrEmpty(restoreId)) ctrl.ChonPhieuNhap(restoreId);
            BindingSource_CurrentChanged(this, EventArgs.Empty);
            LamMoiOThongTinLo();
            Allow(false);
        }

        private void toolXemLai_Click(object sender, EventArgs e)
        {
            CancelChanges();
        }

        internal void BatDauChinhSua()
        {
            toolChinhsua_Click(this, EventArgs.Empty);
        }

        private void toolChinhsua_Click(object sender, EventArgs e)
        {
            if (status != Controll.Normal || !AccessControl.Can("NhapHang")) return;
            if (bindingNavigator.BindingSource == null || bindingNavigator.BindingSource.Current == null)
            {
                MessageBox.Show("Không có phiếu nhập để chỉnh sửa.", "Phiếu nhập",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            BindingSource_CurrentChanged(this, EventArgs.Empty);
            status = Controll.Edit;
            Allow(true);
            LamMoiOThongTinLo();
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (status == Controll.Normal) return;
            if (dataGridView.CurrentRow == null || dataGridView.CurrentRow.IsNewRow)
            {
                MessageBox.Show("Vui lòng chọn lô cần bỏ khỏi phiếu.", "Phiếu nhập",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataRowView dongDuLieu = dataGridView.CurrentRow.DataBoundItem as DataRowView;
            if (dongDuLieu == null || dongDuLieu.Row.RowState != DataRowState.Added)
            {
                MessageBox.Show(
                    "Không thể bỏ lô đã lưu. Lô có thể đã được liên kết với phiếu bán; cần thực hiện quy trình điều chỉnh kho riêng.",
                    "Phiếu nhập", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Bạn có chắc muốn bỏ lô mới thêm này khỏi phiếu nhập?",
                "Phiếu nhập", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            dongDuLieu.Row.Delete();
            CapNhatTongTienTheoChiTiet();
        }

        private void dataGridView_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            // Chi cho phep bo lo chua luu bang nut btnRemove de tranh xoa nham lich su kho.
            e.Cancel = true;
        }

        private void toolXoa_Click(object sender, EventArgs e)
        {
            if (status != Controll.Normal)
            {
                MessageBox.Show("Hãy lưu hoặc hủy thay đổi trước khi xóa phiếu.", "Phiếu nhập",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string maPhieu = txtMaPhieu.Text.Trim();
            if (String.IsNullOrEmpty(maPhieu))
                return;

            if (MessageBox.Show(
                "Chỉ xóa được phiếu chưa thanh toán và chưa có lô nào phát sinh bán hàng. Bạn có chắc muốn xóa?",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            string thongBao;
            if (!dichVuNhapHang.XoaPhieuNhap(maPhieu, out thongBao))
            {
                MessageBox.Show(thongBao, "Không thể xóa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            NapLaiDanhSachSauKhiXoa();
            MessageBox.Show(thongBao, "Phiếu nhập", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void NapLaiDanhSachSauKhiXoa()
        {
            ctrl.TaiNapDanhSach();
            ctrl.HienthiPhieuNhap(bindingNavigator, txtMaPhieu, cmbNhaCungCap,
                dtNgayNhap, numTongTien, numDaTra, numConNo);
            GanSuKienBindingSource();

            if (ctrlDanhSach != null && !Object.ReferenceEquals(ctrlDanhSach, ctrl))
                ctrlDanhSach.TaiNapDanhSach();

            DataRowView dongHienTai = bindingNavigator.BindingSource == null
                ? null : bindingNavigator.BindingSource.Current as DataRowView;
            if (dongHienTai != null)
            {
                ctrlMaSP.HienThiChiTietPhieuNhap(Convert.ToString(dongHienTai["ID"]), dataGridView);
            }
            else
            {
                dataGridView.DataSource = null;
                txtMaPhieu.Clear();
                cmbNhaCungCap.SelectedIndex = -1;
                dtNgayNhap.Value = DateTime.Today;
                numTongTien.Value = 0;
                numDaTra.Value = 0;
                numConNo.Value = 0;
            }
            Allow(false);
        }

        private void numDaTra_ValueChanged(object sender, EventArgs e)
        {
            CapNhatCongNo();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            frmNhaCungCap formNhaCungCap = new frmNhaCungCap();
            formNhaCungCap.ShowDialog();
            ctrlNCC.HienthiAutoComboBox(cmbNhaCungCap);
        }
    }
}
