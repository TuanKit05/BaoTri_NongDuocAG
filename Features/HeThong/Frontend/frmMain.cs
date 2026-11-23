using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;

namespace CuahangNongduoc
{
    public partial class frmMain : Form
    {
        private System.Windows.Forms.ToolStripMenuItem mnuQuanLyTaiKhoan;
        private System.Windows.Forms.ToolStripMenuItem mnuDoiMatKhau;
        private System.Windows.Forms.ToolStripMenuItem mnuDangXuat;
        private frmQuanLyTaiKhoan quanLyTaiKhoan;
        private frmDoiMatKhau doiMatKhau;

        public frmMain()
        {
            InitializeComponent();
            ConfigureToolbarImages();
            this.MdiChildActivate += new EventHandler(frmMain_MdiChildActivate);
            KhoiTaoMenuTaiKhoan();
            Shown += delegate {
                BeginInvoke(new MethodInvoker(delegate {
                    if (IsDisposed || Disposing) return;
                    try { ExpiryWarning.ShowIfNeeded(this); }
                    catch (Exception error)
                    {
                        MessageBox.Show(this, ErrorLog.Describe(error, "Không thể kiểm tra hàng sắp hết hạn"),
                            "Cảnh báo kho", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }));
            };
        }
        // MDI inserts a system icon whenever the active child changes.
        // Scale that image too, even when Windows supplies a large source icon.
        private void ConfigureToolbarImages()
        {
            menuStrip.ImageScalingSize = new Size(16, 16);
            menuStrip.ItemAdded += delegate (object sender, ToolStripItemEventArgs e) {
                e.Item.ImageScaling = ToolStripItemImageScaling.SizeToFit;
            };
            foreach (ToolStripItem item in menuStrip.Items)
                item.ImageScaling = ToolStripItemImageScaling.SizeToFit;

            toolStrip.SuspendLayout();
            toolStrip.ImageScalingSize = new Size(24, 24);
            foreach (ToolStripItem item in toolStrip.Items)
            {
                item.ImageScaling = ToolStripItemImageScaling.SizeToFit;
                if (item is ToolStripButton)
                {
                    item.ImageAlign = ContentAlignment.MiddleCenter;
                    item.TextAlign = ContentAlignment.MiddleCenter;
                    item.TextImageRelation = TextImageRelation.ImageAboveText;
                }
            }
            toolStrip.ResumeLayout(true);
        }

        frmDonViTinh DonViTinh = null;

        // Apply to every MDI child, including forms opened from menus and toolbars.
        private void frmMain_MdiChildActivate(object sender, EventArgs e)
        {
            Form child = this.ActiveMdiChild;
            frmSanPham products = child as frmSanPham;
            if (products != null) products.RefreshProductView();
            if (ResponsiveLayout.IsCenteredDocument(child))
            {
                if (child.WindowState != FormWindowState.Normal) child.WindowState = FormWindowState.Normal;
                return;
            }
            frmDoiMatKhau passwordForm = child as frmDoiMatKhau;
            if (passwordForm != null)
            {
                passwordForm.RestoreCompactLayout();
                return;
            }
            if (child != null && !child.IsDisposed && child.WindowState != FormWindowState.Maximized)
                child.WindowState = FormWindowState.Maximized;
        }

        private void ShowChildForm<T>(ref T form) where T : Form, new()
        {
            if (form == null || form.IsDisposed)
            {
                form = new T();
                form.MdiParent = this;
                form.Show();
            }
            else
            {
                form.Activate();
            }
        }

        private void mnuDonViTinh_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("DonViTinh")) return;
            ShowChildForm(ref DonViTinh);
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            //RegistryKey regKey = Registry.CurrentUser.OpenSubKey("Software\\CoolSoft\\CuahangNongduoc");

            //if (regKey == null)
            //{
            //    DataService.m_ConnectString = "";
            //}
            //else
            //{
            //    try
            //    {
            //        DataService.m_ConnectString = (String)regKey.GetValue("ConnectString");
            //    }
            //    catch
            //    {
            //    }
            //    finally
            //    {
            //        regKey.Close();
            //    }
            //}

            //if (DataService.OpenConnection() == false)
            //{
            //    MessageBox.Show("Không thể kết nối dữ liệu!", "Cua hang Nong duoc", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    this.Close();
            //}

            DataService.OpenConnection();
            CapNhatGiaoDienTheoQuyen();
            if (AppSession.CurrentUser != null)
                this.Text = "QUAN LY CUA HANG NONG DUOC - " + AppSession.CurrentUser.FullName +
                    " (" + AppSession.CurrentUser.Role + ")";
        }
        frmSanPham SanPham = null;
        private void mnuSanPham_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("SanPham")) return;
            ShowChildForm(ref SanPham);
        }
        frmKhachHang KhachHang = null;
        private void mnuKhachHang_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("KhachHang")) return;
            ShowChildForm(ref KhachHang);
        }
        frmDaiLy DaiLy = null;
        private void mnuDaiLy_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("DaiLy")) return;
            ShowChildForm(ref DaiLy);

        }
        frmDanhsachPhieuNhap NhapHang = null;
        private void mnuNhapHang_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("NhapHang")) return;
            ShowChildForm(ref NhapHang);
        }
        frmDanhsachPhieuBanLe BanLe = null;
        private void mnuBanHangKH_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("BanHang")) return;
            ShowChildForm(ref BanLe);
        }
        frmDanhsachPhieuBanSi BanSi = null;
        private void mnuBanHangDL_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("BanHang")) return;
            ShowChildForm(ref BanSi);
        }

        private void ToggleVisibility(ToolStripMenuItem menuItem, Control control)
        {
            control.Visible = !control.Visible;
            menuItem.Checked = control.Visible;
        }

        private void mnuThanhCongCu_Click(object sender, EventArgs e)
        {
            ToggleVisibility(mnuThanhCongCu, toolStrip);
        }

        private void mnuThanhChucNang_Click(object sender, EventArgs e)
        {
            ToggleVisibility(mnuThanhChucNang, taskPane);
        }

        frmThanhToan ThanhToan = null;
        private void mnuThanhtoan_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("ThuChi")) return;
            ShowChildForm(ref ThanhToan);
        }
        frmDunoKhachhang DunoKhachhang = null;
        private void mnuTonghopDuno_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("ThuChi")) return;
            ShowChildForm(ref DunoKhachhang);
        }
        frmDoanhThu DoanhThu = null;
        private void mnuBaocaoDoanhThu_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("BaoCaoBanHang")) return;
            ShowChildForm(ref DoanhThu);

        }

        frmSoLuongTon SoLuongTon = null;
        private void mnuBaocaoSoluongton_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("BaoCaoTonKho")) return;
            ShowChildForm(ref SoLuongTon);
        }

        frmThongKeTonKho ThongKeTonKho = null;
        private void mnuThongKeTonKho_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("BaoCaoTonKho")) return;
            ShowChildForm(ref ThongKeTonKho);
        }
        CuahangNongduoc.Features.BaoCao.Frontend.frmTonKhoTaiNgay TonKhoTaiNgay = null;
        private ToolStripMenuItem mnuTonKhoTaiNgayItem;
        private void mnuTonKhoTaiNgay_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("BaoCaoTonKho")) return;
            ShowChildForm(ref TonKhoTaiNgay);
        }
        frmSoLuongBan SoLuongBan = null;
        private void mnuSoLuongBan_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("BaoCaoBanHang")) return;
            ShowChildForm(ref SoLuongBan);
        }
        frmSanphamHethan SanphamHethan = null;
        private void mnuSanphamHethan_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("BaoCaoTonKho")) return;
            ShowChildForm(ref SanphamHethan);
        }

        private void mnuThoat_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void toolSaoLuu_Click(object sender, EventArgs e)
        {
            string defaultName = "cuahang_sao_luu_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".dll";
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Sao lưu dữ liệu cửa hàng";
                dialog.Filter = "Cơ sở dữ liệu Access (*.dll)|*.dll|Tất cả các tệp (*.*)|*.*";
                dialog.FileName = defaultName;
                dialog.InitialDirectory = Path.Combine(Path.GetDirectoryName(DataService.DatabasePath), "Backups");
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    DatabaseBackupService.Backup(dialog.FileName);
                    MessageBox.Show(this, "Đã sao lưu toàn bộ dữ liệu thành công đến:\n" + dialog.FileName,
                        "Sao lưu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception error)
                {
                    MessageBox.Show(this, ErrorLog.Describe(error, "Không thể sao lưu dữ liệu"),
                        "Sao lưu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        frmThongtinCuahang ThongtinCuahang = null;
        private void mnuTuychinhThongtin_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("QuanTri")) return;
            ShowChildForm(ref ThongtinCuahang);
        }
        frmThongtinLienhe ThongtinLienhe = null;
        private void mnuTrogiupLienhe_Click(object sender, EventArgs e)
        {
            ShowChildForm(ref ThongtinLienhe);
        }

        frmNhaCungCap NhaCungCap = null;
        private void mnuNhaCungCap_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("NhaCungCap")) return;
            ShowChildForm(ref NhaCungCap);
        }
        frmLyDoChi LyDoChi = null;
        private void mnuLyDoChi_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("LyDoChi")) return;
            ShowChildForm(ref LyDoChi);
        }

        frmPhieuChi PhieuChi = null;
        private void mnuPhieuChi_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("ThuChi")) return;
            ShowChildForm(ref PhieuChi);
        }

        private void mnuTrogiupHuongdan_Click(object sender, EventArgs e)
        {
            // Help.ShowHelp(this, "CPP.CHM");
        }

        private void KhoiTaoMenuTaiKhoan()
        {
            ToolStripMenuItem discounts = new ToolStripMenuItem("Cấu hình giảm giá và chiết khấu");
            discounts.Name = "mnuCauHinhUuDai";
            discounts.Visible = AppSession.CurrentUser != null && AccountRoles.IsAdmin(AppSession.CurrentUser.Role);
            discounts.Click += delegate {
                try
                {
                    UuDaiSettings.DemandAdmin();
                    using (frmQLUuDai form = new frmQLUuDai()) form.ShowDialog(this);
                }
                catch (Exception error)
                {
                    MessageBox.Show(this, error.Message, "Cấu hình ưu đãi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            mnuTuychinh.DropDownItems.Add(discounts);
            ToolStripMenuItem warehouse = new ToolStripMenuItem("Cấu hình xuất kho");
            warehouse.Visible = AccessControl.Can("QuanTri");
            warehouse.Click += delegate {
                if (!KiemTraQuyen("QuanTri")) return;
                try { using (frmCauHinhKho form = new frmCauHinhKho()) form.ShowDialog(this); }
                catch (Exception error) { MessageBox.Show(this, error.Message, "Cấu hình xuất kho", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };
            mnuTuychinh.DropDownItems.Add(warehouse);
            mnuQuanLyTaiKhoan = new System.Windows.Forms.ToolStripMenuItem("Quản lý tài khoản");
            mnuQuanLyTaiKhoan.Click += new EventHandler(mnuQuanLyTaiKhoan_Click);
            mnuDoiMatKhau = new System.Windows.Forms.ToolStripMenuItem("Đổi mật khẩu");
            mnuDoiMatKhau.Click += new EventHandler(mnuDoiMatKhau_Click);
            mnuDangXuat = new System.Windows.Forms.ToolStripMenuItem("Đăng xuất");
            mnuDangXuat.Click += new EventHandler(mnuDangXuat_Click);

            ToolStripMenuItem report = new ToolStripMenuItem("Hóa đơn ưu đãi theo nhân viên");
            report.Visible = AccessControl.Can("BaoCaoBanHang");
            report.Click += delegate {
                if (!KiemTraQuyen("BaoCaoBanHang")) return;
                new CuahangNongduoc.Features.BaoCao.ThongKeUuDai.frmThongKeUuDai().ShowDialog(this);
            };
            mnuBaocao.DropDownItems.Add(report);
            mnuTonKhoTaiNgayItem = new ToolStripMenuItem("Tồn kho tại ngày");
            mnuTonKhoTaiNgayItem.Name = "mnuTonKhoTaiNgay";
            mnuTonKhoTaiNgayItem.Visible = AccessControl.Can("BaoCaoTonKho");
            mnuTonKhoTaiNgayItem.Click += mnuTonKhoTaiNgay_Click;
            mnuBaocao.DropDownItems.Add(mnuTonKhoTaiNgayItem);
            ToolStripMenuItem audit = new ToolStripMenuItem("Nhật ký thao tác");
            audit.Visible = AccessControl.Can("XemNhatKy");
            audit.Click += delegate {
                if (!KiemTraQuyen("XemNhatKy")) return;
                using (frmNhatKyThaoTac viewer = new frmNhatKyThaoTac())
                {
                    viewer.ShowDialog(this);
                }
            };
            mnuHeThong.DropDownItems.Add(audit);
            mnuHeThong.DropDownItems.Insert(0, mnuQuanLyTaiKhoan);
            mnuHeThong.DropDownItems.Insert(1, mnuDoiMatKhau);
            mnuHeThong.DropDownItems.Insert(2, new System.Windows.Forms.ToolStripSeparator());
            mnuHeThong.DropDownItems.Insert(3, mnuDangXuat);
        }

        private bool CoQuyen(string feature)
        {
            if (AppSession.CurrentUser == null)
                return false;
            IRolePolicy rolePolicy = RolePolicyFactory.Create(AppSession.CurrentUser.Role);
            AuthorizationProxy proxy = new AuthorizationProxy(AppSession.CurrentUser, rolePolicy);
            return proxy.CanAccess(feature);
        }

        private bool KiemTraQuyen(string feature)
        {
            if (!CoQuyen(feature))
            {
                MessageBox.Show("Tài khoản hiện tại không có quyền sử dụng chức năng này.",
                    "Từ chối truy cập", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void CapNhatGiaoDienTheoQuyen()
        {
            mnuLyDoChi.Visible = CoQuyen("LyDoChi");
            mnuDonViTinh.Visible = CoQuyen("DonViTinh");
            mnuSanPham.Visible = CoQuyen("SanPham");
            mnuKhachHang.Visible = CoQuyen("KhachHang");
            mnuDaiLy.Visible = CoQuyen("DaiLy");
            mnuNhaCungCap.Visible = CoQuyen("NhaCungCap");
            mnuNhapHang.Visible = CoQuyen("NhapHang");
            mnuBanHangKH.Visible = CoQuyen("BanHang");
            mnuBanHangDL.Visible = CoQuyen("BanHang");
            mnuBanHang.Visible = CoQuyen("BanHang");
            mnuPhieuChi.Visible = CoQuyen("ThuChi");
            mnuThanhtoan.Visible = CoQuyen("ThuChi");
            mnuTonghopDuno.Visible = CoQuyen("ThuChi");
            mnuQuanLy.Visible = CoQuyen("LyDoChi") || CoQuyen("DonViTinh") || CoQuyen("SanPham") ||
                CoQuyen("KhachHang") || CoQuyen("DaiLy") || CoQuyen("NhaCungCap");
            mnuNghiepVu.Visible = CoQuyen("NhapHang") || CoQuyen("BanHang") || CoQuyen("ThuChi");

            mnuBaocaoSoluongton.Visible = CoQuyen("BaoCaoTonKho");
            mnuThongKeTonKho.Visible = CoQuyen("BaoCaoTonKho");
            if (mnuTonKhoTaiNgayItem != null) mnuTonKhoTaiNgayItem.Visible = CoQuyen("BaoCaoTonKho");
            mnuSanphamHethan.Visible = CoQuyen("BaoCaoTonKho");
            mnuSoLuongBan.Visible = CoQuyen("BaoCaoBanHang");
            mnuBaocao.Visible = CoQuyen("BaoCaoTonKho") || CoQuyen("BaoCaoBanHang");

            mnuTuychinh.Visible = CoQuyen("QuanTri");
            mnuQuanLyTaiKhoan.Visible = CoQuyen("QuanLyTaiKhoan");

            toolSanPham.Visible = CoQuyen("SanPham");
            toolKhachHang.Visible = CoQuyen("KhachHang");
            toolDaiLy.Visible = CoQuyen("DaiLy");
            toolNhaCungCap.Visible = CoQuyen("NhaCungCap");
            toolNhapHang.Visible = CoQuyen("NhapHang");
            toolBanSi.Visible = CoQuyen("BanHang");
            toolBanLe.Visible = CoQuyen("BanHang");
            toolPhieuChi.Visible = CoQuyen("ThuChi");
            toolThanhtoan.Visible = CoQuyen("ThuChi");
            toolTonKho.Visible = CoQuyen("BaoCaoTonKho");
            // Keep the backup button discoverable; permission is enforced when it is clicked.
            toolSaoLuu.Visible = true;

            itemDaiLy.Visible = CoQuyen("DaiLy");
            itemKhachHang.Visible = CoQuyen("KhachHang");
            itemSanPham.Visible = CoQuyen("SanPham");
            itemNhaCungCap.Visible = CoQuyen("NhaCungCap");
            itemNhapHang.Visible = CoQuyen("NhapHang");
            itemBanSi.Visible = CoQuyen("BanHang");
            itemBanLe.Visible = CoQuyen("BanHang");
            itemThanhToan.Visible = CoQuyen("ThuChi");
            itemPhieuChi.Visible = CoQuyen("ThuChi");
            itemTonghopDoanhthu.Visible = CoQuyen("BaoCaoBanHang");
            itemTonKho.Visible = CoQuyen("BaoCaoTonKho");

            // Visible on a child also returns false when its parent is hidden.
            // Calculate group visibility from permissions instead of child controls.
            bool showCatalog = CoQuyen("DaiLy") || CoQuyen("KhachHang") ||
                CoQuyen("SanPham") || CoQuyen("NhaCungCap");
            bool showOperations = CoQuyen("NhapHang") || CoQuyen("BanHang") || CoQuyen("ThuChi");
            bool showReports = CoQuyen("BaoCaoBanHang") || CoQuyen("BaoCaoTonKho");
            expando1.Visible = showCatalog;
            expando2.Visible = showOperations;
            expando3.Visible = showReports;
            taskPane.Visible = showCatalog || showOperations || showReports;
            mnuThanhChucNang.Checked = showCatalog || showOperations || showReports;
            toolStrip.Visible = true;
            mnuThanhCongCu.Checked = true;
        }

        private void mnuQuanLyTaiKhoan_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("QuanLyTaiKhoan")) return;
            if (quanLyTaiKhoan == null || quanLyTaiKhoan.IsDisposed)
            {
                quanLyTaiKhoan = new frmQuanLyTaiKhoan(new AccountRepository());
                quanLyTaiKhoan.MdiParent = this;
                quanLyTaiKhoan.Show();
            }
            else quanLyTaiKhoan.Activate();
        }

        private void mnuDoiMatKhau_Click(object sender, EventArgs e)
        {
            if (AppSession.CurrentUser == null) return;
            if (doiMatKhau == null || doiMatKhau.IsDisposed)
            {
                doiMatKhau = new frmDoiMatKhau(new AccountRepository(), false);
                doiMatKhau.MdiParent = this;
                doiMatKhau.Show();
            }
            else doiMatKhau.Activate();
        }

        private void mnuDangXuat_Click(object sender, EventArgs e)
        {
            AppSession.LogoutRequested = true;
            this.Close();
        }

    }
}