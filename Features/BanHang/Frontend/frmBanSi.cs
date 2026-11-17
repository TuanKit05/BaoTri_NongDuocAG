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
    public partial class frmBanSi : Form
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

        public frmBanSi()
        {
            InitializeComponent();
            status = Controll.AddNew;
            hoaDon.DaThayDoi += new EventHandler(HoaDon_DaThayDoi);
        }

        public frmBanSi(PhieuBanController ctrlPB)
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

            ctrlKhachHang.HienthiAutoComboBox(cmbKhachHang, true);

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
            if (cmbSanPham.SelectedValue != null)
            {
                MaSanPhamController ctrlMSP = new MaSanPhamController();

                cmbMaSanPham.SelectedIndexChanged -= new EventHandler(cmbMaSanPham_SelectedIndexChanged);
                ctrlMSP.HienThiAutoComboBox(cmbSanPham.SelectedValue.ToString(), cmbMaSanPham);
                cmbMaSanPham.SelectedIndexChanged += new EventHandler(cmbMaSanPham_SelectedIndexChanged);
            }
        }

        void cmbMaSanPham_SelectedIndexChanged(object sender, EventArgs e)
        {
            MaSanPhamController ctrl = new MaSanPhamController();
            MaSanPham masp = ctrl.LayMaSanPham(cmbMaSanPham.SelectedValue.ToString());
            numDonGia.Value = masp.SanPham.GiaBanSi;
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
                DongBoTienHang();
            }
        }

        private void numDonGia_ValueChanged(object sender, EventArgs e)
        {
            numThanhTien.Value = numDonGia.Value * numSoLuong.Value;
        }

        private void numTongTien_ValueChanged(object sender, EventArgs e)
        {
            numConNo.Value = numTongTien.Value - numDaTra.Value;
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
            DongBoPhi();
            DongBoTienHang();

            DataRow row = ctrlPhieuBan.NewRow();
            row["ID"] = txtMaPhieu.Text;
            row["ID_KHACH_HANG"] = cmbKhachHang.SelectedValue;
            row["NGAY_BAN"] = dtNgayLapPhieu.Value.Date;
            row["TONG_TIEN"] = numTongTien.Value;
            row["DA_TRA"] = numDaTra.Value;
            row["CON_NO"] = numConNo.Value;
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
                MessageBox.Show("Lưu phiếu bán thất bại. Chi tiết: " + ex.Message, "Phieu Ban Si", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            ctrlChiTiet.HienThiChiTiet(dgvDanhsachSP, txtMaPhieu.Text);

            dangNapPhi = true;
            numPhiVanChuyen.Value = 0;
            numPhanTramGiam.Value = 0;
            dgvDichVu.Rows.Clear();
            dangNapPhi = false;

            DongBoPhi();
            DongBoTienHang();
            this.Allow(true);
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc chắn xóa không?", "Phieu Ban Si", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                BindingSource bs = ((BindingSource)dgvDanhsachSP.DataSource);
                DataRowView row = (DataRowView)bs.Current;
                deleted.Add(new MaSanPham(Convert.ToString(row["ID_MA_SAN_PHAM"]), Convert.ToInt32(row["SO_LUONG"])));
                bs.RemoveCurrent();
                DongBoTienHang();
            }
        }

        private void dgvDanhsachSP_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc chắn xóa không?", "Phieu Ban Si", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                e.Cancel = true;
            }
            else
            {
                BindingSource bs = ((BindingSource)dgvDanhsachSP.DataSource);
                DataRowView row = (DataRowView)bs.Current;
                deleted.Add(new MaSanPham(Convert.ToString(row["ID_MA_SAN_PHAM"]), Convert.ToInt32(row["SO_LUONG"])));
                // Tính lại sau khi dòng bị xóa khỏi lưới
                this.BeginInvoke(new MethodInvoker(DongBoTienHang));
            }
        }

        private void toolLuuIn_Click(object sender, EventArgs e)
        {
            if (status != Controll.Normal)
            {
                MessageBox.Show("Vui lòng lưu lại Phiếu bán hiện tại!", "Phieu Ban Si", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            // Phí chỉ nhập khi lập phiếu mới; phiếu đã lưu không sửa phí
            bool choPhi = val && status == Controll.AddNew;
            numPhiVanChuyen.Enabled = choPhi;
            numPhanTramGiam.Enabled = choPhi;
            dgvDichVu.Enabled = choPhi;
        }

        private void toolThoat_Click(object sender, EventArgs e)
        {
            if (status != Controll.Normal)
            {
                if (MessageBox.Show("Bạn có muốn lưu lại Phiếu bán này không?", "Phieu Ban Si", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
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
            if (MessageBox.Show("Bạn có chắc chắn xóa không?", "Phieu Ban Si", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
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
                MessageBox.Show("Xóa phiếu bán thất bại. Chi tiết: " + ex.Message, "Phieu Ban Si", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            bindingNavigator.BindingSource.RemoveCurrent();
        }

        // Nút "Xem lại" của bản Sỉ: nạp lại sản phẩm và đại lý
        private void toolXemLai_Click(object sender, EventArgs e)
        {
            ctrlSanPham.HienthiAutoComboBox(cmbSanPham);
            ctrlMaSanPham.HienThiDataGridViewComboBox(colMaSanPham);
            ctrlKhachHang.HienthiAutoComboBox(cmbKhachHang, true);
        }

        private void btnThemDaiLy_Click(object sender, EventArgs e)
        {
            frmDaiLy DaiLy = new frmDaiLy();
            DaiLy.ShowDialog();
            ctrlKhachHang.HienthiAutoComboBox(cmbKhachHang, true);
        }

        private void btnThemSanPham_Click(object sender, EventArgs e)
        {
            frmSanPham SanPham = new frmSanPham();
            SanPham.ShowDialog();
            ctrlSanPham.HienthiAutoComboBox(cmbSanPham);
        }
    }
}