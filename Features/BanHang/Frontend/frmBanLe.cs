using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using CuahangNongduoc.Controller;
using CuahangNongduoc.BusinessObject;

namespace CuahangNongduoc
{
    public partial class frmBanLe : Form
    {
        SanPhamController ctrlSanPham = new SanPhamController();
        KhachHangController ctrlKhachHang = new KhachHangController();
        MaSanPhamController ctrlMaSanPham = new MaSanPhamController();
        PhieuBanController ctrlPhieuBan = new PhieuBanController();
        ChiTietPhieuBanController ctrlChiTiet = new ChiTietPhieuBanController();
        IList<MaSanPham> deleted = new List<MaSanPham>();
        Controll status = Controll.Normal;

        // OBSERVER (refactoring.guru): mô hình hóa đơn đang nhập.
        // Form là người quan sát: mỗi lần mô hình đổi, HoaDon_DaThayDoi cập nhật ô tổng và tiền giảm.
        HoaDonDangLap hoaDon = new HoaDonDangLap();
        bool dangNapPhi = false;

        public frmBanLe()
        {
            InitializeComponent();
            this.Shown += delegate {
                if (!AccessControl.Can("ThietLapUuDai"))
                {
                    numPhanTramGiam.Enabled = false;
                }
            };

            cmbLoaiChietKhau.SelectedIndex = 0;
            cmbLoaiGiamGia.SelectedIndex = 0;

            status = Controll.AddNew;
            hoaDon.DaThayDoi += new EventHandler(HoaDon_DaThayDoi);
        }


        private decimal tienHangGoc = 0m;
        private decimal tienChietKhau = 0m;
        private decimal tienGiamGia = 0m;

        private void TinhLaiHoaDon()
        {
            if (cmbLoaiChietKhau.SelectedIndex < 0 ||
                cmbLoaiGiamGia.SelectedIndex < 0)
                return;

            IChienLuocUuDai chienLuocChietKhau =
                cmbLoaiChietKhau.SelectedIndex == 0
                ? (IChienLuocUuDai)new UuDaiPhanTram()
                : new UuDaiCoDinh();

            IChienLuocUuDai chienLuocGiamGia =
                cmbLoaiGiamGia.SelectedIndex == 0
                ? (IChienLuocUuDai)new UuDaiPhanTram()
                : new UuDaiCoDinh();

            TinhTienHoaDon boTinh = new TinhTienHoaDon();

            tienHangGoc = TinhTienHangTuDanhSach();
            KetQuaUuDai ketQua = boTinh.Tinh(
                tienHangGoc,
                chienLuocChietKhau,
                numGiaTriChietKhau.Value,
                chienLuocGiamGia,
                numGiaTriGiamGia.Value
            );
            tienChietKhau = ketQua.TienChietKhau;
            tienGiamGia = ketQua.TienGiamGia;


            lblTienChietKhau.Text =
                ketQua.TienChietKhau.ToString("#,##0") + " đ";

            lblTienGiamGia.Text =
                ketQua.TienGiamGia.ToString("#,##0") + " đ";

            if (ketQua.TongThanhToan > numTongTien.Maximum)
            {
                MessageBox.Show(
                    "Tổng thanh toán vượt giới hạn cho phép.");
                return;
            }

            numTongTien.Value = ketQua.TongThanhToan;
            CapNhatCongNo();

        }


        private decimal TinhTienHangTuDanhSach()
        {
            decimal tong = 0m;

            foreach (DataGridViewRow row in dgvDanhsachSP.Rows)
            {
                if (row.IsNewRow)
                    continue;

                object giaTri = row.Cells["colThanhTien"].Value;

                if (giaTri != null && giaTri != DBNull.Value)
                {
                    tong += Convert.ToDecimal(giaTri);
                }
            }

            return tong;
        }


        private void UuDai_ValueChanged(object sender, EventArgs e)
        {
            TinhLaiHoaDon();
        }


        public frmBanLe(PhieuBanController ctrlPB)
            : this()
        {
            this.ctrlPhieuBan = ctrlPB;
            status = Controll.Normal;
        }

        private void frmNhapHang_Load(object sender, EventArgs e)
        {
            ctrlSanPham.HienthiAutoComboBox(cmbSanPham);
            ctrlMaSanPham.HienThiDataGridViewComboBox(colMaSanPham);

            cmbSanPham.SelectedIndexChanged += new EventHandler(cmbSanPham_SelectedIndexChanged);

            ctrlKhachHang.HienthiAutoComboBox(cmbKhachHang, false);

            ctrlPhieuBan.HienthiPhieuBan(bindingNavigator, cmbKhachHang, txtMaPhieu, dtNgayLapPhieu, numTongTien, numDaTra, numConNo);

            bindingNavigator.BindingSource.CurrentChanged -= new EventHandler(BindingSource_CurrentChanged);
            bindingNavigator.BindingSource.CurrentChanged += new EventHandler(BindingSource_CurrentChanged);

            ctrlChiTiet.HienThiChiTiet(dgvDanhsachSP, txtMaPhieu.Text);

            if (status == Controll.AddNew)
            {
                txtMaPhieu.Text = ThamSo.LayMaPhieuBan().ToString();
                DongBoTienHang();
            }
            else
            {
                this.Allow(false);
                HienThiPhi(txtMaPhieu.Text);
            }
        }

        void BindingSource_CurrentChanged(object sender, EventArgs e)
        {
            if (status == Controll.Normal)
            {
                ctrlChiTiet.HienThiChiTiet(dgvDanhsachSP, txtMaPhieu.Text);
                HienThiPhi(txtMaPhieu.Text);
            }
        }



        void cmbSanPham_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbSanPham.SelectedValue == null ||
                cmbSanPham.SelectedValue is DataRowView)
                return;

            MaSanPhamController ctrlMSP = new MaSanPhamController();

            cmbMaSanPham.SelectedIndexChanged -=
                new EventHandler(cmbMaSanPham_SelectedIndexChanged);

            try
            {
                ctrlMSP.HienThiAutoComboBox(
                    cmbSanPham.SelectedValue.ToString(),
                    cmbMaSanPham);
            }
            finally
            {
                cmbMaSanPham.SelectedIndexChanged +=
                    new EventHandler(cmbMaSanPham_SelectedIndexChanged);
            }

            // Cập nhật thông tin nếu danh sách đã chọn được mã hợp lệ.
            cmbMaSanPham_SelectedIndexChanged(cmbMaSanPham, EventArgs.Empty);
        }

        void cmbMaSanPham_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbMaSanPham.SelectedValue == null ||
                cmbMaSanPham.SelectedValue is DataRowView)
                return;

            MaSanPhamController ctrl = new MaSanPhamController();

            MaSanPham masp = ctrl.LayMaSanPham(
                cmbMaSanPham.SelectedValue.ToString());

            if (masp == null || masp.SanPham == null)
                return;

            numDonGia.Value = masp.SanPham.GiaBanLe;
            txtGiaNhap.Text = masp.GiaNhap.ToString("#,###0");
            txtGiaBanSi.Text = masp.SanPham.GiaBanSi.ToString("#,###0");
            txtGiaBanLe.Text = masp.SanPham.GiaBanLe.ToString("#,###0");
            txtGiaBQGQ.Text = masp.SanPham.DonGiaNhap.ToString("#,###0");
        }


        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (cmbMaSanPham.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn Mã sản phẩm !", "Phieu Nhap", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (numSoLuong.Value <= 0)
            {
                MessageBox.Show("Vui lòng nhập Số lượng !", "Phieu Nhap", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (numDonGia.Value * numSoLuong.Value != numThanhTien.Value)
            {
                MessageBox.Show("Thành tiền sai!", "Phieu Nhap", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                DataRow row = ctrlChiTiet.NewRow();

                row["ID_MA_SAN_PHAM"] = cmbMaSanPham.SelectedValue;
                row["ID_PHIEU_BAN"] = txtMaPhieu.Text;
                row["DON_GIA"] = numDonGia.Value;
                row["SO_LUONG"] = numSoLuong.Value;
                row["THANH_TIEN"] = numThanhTien.Value;

                ctrlChiTiet.Add(row);
                TinhLaiHoaDon();
            }
        }

        private void numDonGia_ValueChanged(object sender, EventArgs e)
        {
            numThanhTien.Value = numDonGia.Value * numSoLuong.Value;
        }

        private void numTongTien_ValueChanged(object sender, EventArgs e)
        {
            CapNhatCongNo();
        }

        private void CapNhatCongNo()
        {
            decimal conNo = numTongTien.Value - numDaTra.Value;

            if (conNo < 0m)
                conNo = 0m;

            numConNo.Value = conNo;
        }

        // ---------- Phí phát sinh và giảm giá (dùng HoaDonDangLap) ----------

        // Đọc tiền hàng từ lưới chi tiết vào mô hình
        void DongBoTienHang()
        {
            long tienHang = 0;
            BindingSource bs = dgvDanhsachSP.DataSource as BindingSource;
            if (bs != null)
            {
                foreach (object o in bs)
                {
                    DataRowView rv = (DataRowView)o;
                    tienHang += Convert.ToInt64(rv["THANH_TIEN"]);
                }
            }
            hoaDon.TienHang = tienHang;
        }

        // Đẩy phí vận chuyển, % giảm và dịch vụ từ ô nhập vào mô hình
        void DongBoPhi()
        {
            if (dangNapPhi)
                return;
            hoaDon.DatPhi((long)numPhiVanChuyen.Value, (double)numPhanTramGiam.Value, DocDichVu());
        }

        // Người quan sát: cập nhật ô tổng tiền và tiền giảm mỗi khi mô hình đổi
        void HoaDon_DaThayDoi(object sender, EventArgs e)
        {
            numTongTien.Value = hoaDon.TongTien;
            lblTienGiam.Text = hoaDon.TienGiam.ToString("#,###0");
        }

        IList<DichVuPhieuBan> DocDichVu()
        {
            IList<DichVuPhieuBan> ds = new List<DichVuPhieuBan>();
            foreach (DataGridViewRow r in dgvDichVu.Rows)
            {
                if (r.IsNewRow)
                    continue;
                long so;
                object v = r.Cells["colDichVuTien"].Value;
                if (v == null || !long.TryParse(v.ToString(), out so) || so <= 0)
                    continue;
                DichVuPhieuBan dv = new DichVuPhieuBan();
                object ten = r.Cells["colDichVuTen"].Value;
                dv.Ten = ten == null ? "" : ten.ToString();
                dv.TienDV = so;
                ds.Add(dv);
            }
            return ds;
        }

        void HienThiPhi(String idPhieu)
        {
            PhieuBan phi = PhieuBanController.DocPhi(idPhieu);
            dangNapPhi = true;
            numPhiVanChuyen.Value = phi.PhiVanChuyen;
            numPhanTramGiam.Value = (decimal)phi.PhanTramGiam;
            dgvDichVu.Rows.Clear();
            foreach (DichVuPhieuBan dv in phi.DichVu)
            {
                dgvDichVu.Rows.Add(dv.Ten, dv.TienDV);
            }
            dangNapPhi = false;
            DongBoPhi();
            DongBoTienHang();
        }

        private void numPhiVanChuyen_ValueChanged(object sender, EventArgs e)
        {
            DongBoPhi();
        }

        private void numPhanTramGiam_ValueChanged(object sender, EventArgs e)
        {
            DongBoPhi();
        }

        private void dgvDichVu_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            DongBoPhi();
        }

        private void dgvDichVu_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            DongBoPhi();
        }

        // ---------- Lưu / sửa / xóa ----------

        private void toolLuu_Click(object sender, EventArgs e)
        {
            bindingNavigatorPositionItem.Focus();
            if (this.Luu())
            {
                status = Controll.Normal;
                this.Allow(false);
            }
        }

        // Trả về true nếu lưu thành công
        bool Luu()
        {
            if (status == Controll.AddNew)
            {
                return ThemMoi();
            }
            CapNhat();
            return true;
        }

        void CapNhat()
        {
            AccessControl.DemandCurrent("SuaPhieuBan");
            // Sửa phiếu đã lưu: chỉ hoàn tồn cho dòng bị gỡ khỏi phiếu (không phải xóa cả phiếu)
            foreach (MaSanPham masp in deleted)
            {
                CuahangNongduoc.DataLayer.MaSanPhanFactory.CapNhatSoLuong(masp.Id, masp.SoLuong);
            }
            deleted.Clear();

            ctrlChiTiet.Save();
            ctrlPhieuBan.Update();
        }

        // TEMPLATE METHOD: lập hóa đơn mới qua LapHoaDonMoi (kiểm tra -> lưu -> thông báo)
        bool ThemMoi()
        {
            if (!AccessControl.Can("ThietLapUuDai") && (numGiaTriChietKhau.Value != 0 || numGiaTriGiamGia.Value != 0))
                throw new UnauthorizedAccessException("Chỉ quản lý được thiết lập mức ưu đãi.");
            DongBoPhi();
            DongBoTienHang();

            DataRow row = ctrlPhieuBan.NewRow();
            row["ID"] = txtMaPhieu.Text;
            row["ID_KHACH_HANG"] = cmbKhachHang.SelectedValue;
            row["NGAY_BAN"] = dtNgayLapPhieu.Value.Date;
            row["TONG_TIEN"] = numTongTien.Value;
            row["DA_TRA"] = numDaTra.Value;
            row["CON_NO"] = numConNo.Value;
            row["TIEN_CHIET_KHAU"] = tienChietKhau;
            row["TIEN_GIAM_GIA"] = tienGiamGia;
            ctrlPhieuBan.Add(row);

            LapHoaDonMoi lap = new LapHoaDonMoi(ctrlPhieuBan, ctrlChiTiet, hoaDon.ToPhieuBan(txtMaPhieu.Text));
            bool daLuu;
            try
            {
                daLuu = lap.ThucHien();
            }
            catch (Exception ex)
            {
                // Transaction đã rollback; giữ nguyên dữ liệu đang nhập để người dùng lưu lại
                MessageBox.Show("Lưu phiếu bán thất bại. Chi tiết: " + ex.Message, "Phieu Ban Le", MessageBoxButtons.OK, MessageBoxIcon.Error);
                daLuu = false;
            }

            if (!daLuu)
            {
                // Dòng chưa được lưu: bỏ khỏi DataTable, không đổi mã phiếu
                row.Delete();
                return false;
            }

            if (ThamSo.LaSoNguyen(txtMaPhieu.Text))
            {
                long so = Convert.ToInt64(txtMaPhieu.Text);
                if (so >= ThamSo.LayMaPhieuBan())
                {
                    ThamSo.GanMaPhieuBan(so + 1);
                }
            }
            return true;
        }

        private void toolLuu_Them_Click(object sender, EventArgs e)
        {
            ctrlPhieuBan = new PhieuBanController();
            status = Controll.AddNew;
            txtMaPhieu.Text = ThamSo.LayMaPhieuBan().ToString();

            tienHangGoc = 0m;

            numGiaTriChietKhau.Value = 0;
            numGiaTriGiamGia.Value = 0;

            cmbLoaiChietKhau.SelectedIndex = 0;
            cmbLoaiGiamGia.SelectedIndex = 0;

            numDaTra.Value = 0;
            
            ctrlChiTiet.HienThiChiTiet(dgvDanhsachSP, txtMaPhieu.Text);
            TinhLaiHoaDon();

            this.Allow(true);
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            BindingSource bs = dgvDanhsachSP.DataSource as BindingSource;

            if (bs == null || bs.Current == null)
                return;

            if (MessageBox.Show(
                "Bạn có chắc chắn xóa không?",
                "Phieu Ban Le",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            DataRowView row = bs.Current as DataRowView;

            if (row == null)
                return;

            deleted.Add(new MaSanPham(
                Convert.ToString(row["ID_MA_SAN_PHAM"]),
                Convert.ToInt32(row["SO_LUONG"])
            ));

            bs.RemoveCurrent();

            TinhLaiHoaDon();
        }


        private void dgvDanhsachSP_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (MessageBox.Show(
                "Bạn có chắc chắn xóa không?",
                "Phieu Ban Le",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }

            DataRowView row = e.Row.DataBoundItem as DataRowView;

            if (row != null)
            {
                deleted.Add(new MaSanPham(
                    Convert.ToString(row["ID_MA_SAN_PHAM"]),
                    Convert.ToInt32(row["SO_LUONG"])
                ));
            }
        }

        private void dgvDanhsachSP_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            TinhLaiHoaDon();
        }

        private void toolLuuIn_Click(object sender, EventArgs e)
        {
            if (status != Controll.Normal)
            {
                MessageBox.Show("Vui lòng lưu lại Phiếu bán hiện tại!", "Phieu Ban Le", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                String ma_phieu = txtMaPhieu.Text;
                PhieuBanController ctrlPB = new PhieuBanController();
                CuahangNongduoc.BusinessObject.PhieuBan ph = ctrlPB.LayPhieuBan(ma_phieu);
                frmInPhieuBan InPhieuBan = new frmInPhieuBan(ph);
                InPhieuBan.Show();
            }
        }

        private void dgvDanhsachSP_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = true;
        }

        private void bindingNavigatorDeleteItem_Click(object sender, EventArgs e)
        {
        }

        private void toolChinhSua_Click(object sender, EventArgs e)
        {
            if (!AccessControl.Can("SuaPhieuBan"))
            { MessageBox.Show("Chỉ quản lý được sửa phiếu đã lập.", "Phân quyền"); return; }
            status = Controll.Edit;
            this.Allow(true);
        }

        void Allow(bool val)
        {
            txtMaPhieu.Enabled = val;
            dtNgayLapPhieu.Enabled = val;
            // Ô tổng tiền tính tự động, không sửa tay
            numTongTien.Enabled = false;
            btnAdd.Enabled = val;
            btnRemove.Enabled = val;
            dgvDanhsachSP.Enabled = val;
            cmbLoaiChietKhau.Enabled = val && AccessControl.Can("ThietLapUuDai");
            numGiaTriChietKhau.Enabled = val && AccessControl.Can("ThietLapUuDai");

            cmbLoaiGiamGia.Enabled = val && AccessControl.Can("ThietLapUuDai");
            numGiaTriGiamGia.Enabled = val && AccessControl.Can("ThietLapUuDai");
        }

        private void toolThoat_Click(object sender, EventArgs e)
        {
            if (status != Controll.Normal)
            {
                if (MessageBox.Show("Bạn có muốn lưu lại Phiếu bán này không?", "Phieu Ban Le", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    // Lưu thất bại hoặc đạt giới hạn: không đóng form để không mất dữ liệu đang nhập
                    if (!this.Luu())
                        return;
                }
            }
            this.Close();
        }

        // TEMPLATE METHOD: xóa hóa đơn qua XoaHoaDon (không áp giới hạn bản dùng thử)
        private void toolXoa_Click(object sender, EventArgs e)
        {
            DataRowView view = (DataRowView)bindingNavigator.BindingSource.Current;
            if (view == null)
                return;
            if (MessageBox.Show("Bạn có chắc chắn xóa không?", "Phieu Ban Le", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            String maPhieu = view["ID"].ToString();
            try
            {
                ThaoTacPhieuBan xoa = new XoaHoaDon(ctrlPhieuBan, maPhieu);
                if (!xoa.ThucHien())
                    return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Xóa phiếu bán thất bại. Chi tiết: " + ex.Message, "Phieu Ban Le", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            bindingNavigator.BindingSource.RemoveCurrent();
        }

        private void btnThemDaiLy_Click(object sender, EventArgs e)
        {
            frmKhachHang KhachHang = new frmKhachHang();
            KhachHang.ShowDialog();
            ctrlKhachHang.HienthiAutoComboBox(cmbKhachHang, false);
        }

        private void btnThemSanPham_Click(object sender, EventArgs e)
        {
            frmSanPham SanPham = new frmSanPham();
            SanPham.ShowDialog();
            ctrlSanPham.HienthiAutoComboBox(cmbSanPham);
        }
    }
}