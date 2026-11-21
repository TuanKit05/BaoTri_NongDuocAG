using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

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
        }
        // MDI inserts a system icon whenever the active child changes.
        // Scale that image too, even when Windows supplies a large source icon.
        private void ConfigureToolbarImages()
        {
            menuStrip.ImageScalingSize = new Size(16, 16);
            menuStrip.ItemAdded += delegate(object sender, ToolStripItemEventArgs e) {
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
            if (SanPham == null || SanPham.IsDisposed)
            {
                SanPham = new frmSanPham();
                SanPham.MdiParent = this;
                SanPham.Show();
            }
            else
                SanPham.Activate();
        }
        frmKhachHang KhachHang = null;
        private void mnuKhachHang_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("KhachHang")) return;
            if (KhachHang == null || KhachHang.IsDisposed)
            {
                KhachHang = new frmKhachHang();
                KhachHang.MdiParent = this;
                KhachHang.Show();
            }
            else
                KhachHang.Activate();
        }
        frmDaiLy DaiLy = null;
        private void mnuDaiLy_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("DaiLy")) return;
            if (DaiLy == null || DaiLy.IsDisposed)
            {
                DaiLy = new frmDaiLy();
                DaiLy.MdiParent = this;
                DaiLy.Show();
            }
            else
                DaiLy.Activate();

        }
        frmDanhsachPhieuNhap NhapHang = null;
        private void mnuNhapHang_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("NhapHang")) return;
            if (NhapHang == null || NhapHang.IsDisposed)
            {
                NhapHang = new frmDanhsachPhieuNhap();
                NhapHang.MdiParent = this;
                NhapHang.Show();
            }
            else
                NhapHang.Activate();
        }
        frmDanhsachPhieuBanLe BanLe = null;
        private void mnuBanHangKH_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("BanHang")) return;
            if (BanLe == null || BanLe.IsDisposed)
            {
                BanLe = new frmDanhsachPhieuBanLe();
                BanLe.MdiParent = this;
                BanLe.Show();
            }
            else
                BanLe.Activate();
        }
        frmDanhsachPhieuBanSi BanSi = null;
        private void mnuBanHangDL_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("BanHang")) return;
            if (BanSi == null || BanSi.IsDisposed)
            {
                BanSi = new frmDanhsachPhieuBanSi();
                BanSi.MdiParent = this;
                BanSi.Show();
            }
            else
                BanSi.Activate();
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
            if (ThanhToan == null || ThanhToan.IsDisposed)
            {
                ThanhToan = new frmThanhToan();
                ThanhToan.MdiParent = this;
                ThanhToan.Show();
            }
            else
                ThanhToan.Activate();
        }
        frmDunoKhachhang DunoKhachhang = null;
        private void mnuTonghopDuno_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("ThuChi")) return;
            if (DunoKhachhang == null || DunoKhachhang.IsDisposed)
            {
                DunoKhachhang = new frmDunoKhachhang();
                DunoKhachhang.MdiParent = this;
                DunoKhachhang.Show();
            }
            else
                DunoKhachhang.Activate();
        }
        frmDoanhThu DoanhThu = null;
        private void mnuBaocaoDoanhThu_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("BaoCaoBanHang")) return;
            if (DoanhThu == null || DoanhThu.IsDisposed)
            {
                DoanhThu = new frmDoanhThu();
                DoanhThu.MdiParent = this;
                DoanhThu.Show();
            }
            else
                DoanhThu.Activate();

        }

        frmSoLuongTon SoLuongTon = null;
        private void mnuBaocaoSoluongton_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("BaoCaoTonKho")) return;

            if (SoLuongTon == null || SoLuongTon.IsDisposed)
            {
                SoLuongTon = new frmSoLuongTon();
                SoLuongTon.MdiParent = this;
                SoLuongTon.Show();
            }
            else
                SoLuongTon.Activate();

        }

        frmThongKeTonKho ThongKeTonKho = null;
        private void mnuThongKeTonKho_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("BaoCaoTonKho")) return;
            if (ThongKeTonKho == null || ThongKeTonKho.IsDisposed)
            {
                ThongKeTonKho = new frmThongKeTonKho();
                ThongKeTonKho.MdiParent = this;
                ThongKeTonKho.Show();
            }
            else
                ThongKeTonKho.Activate();
        }
        frmSoLuongBan SoLuongBan = null;
        private void mnuSoLuongBan_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("BaoCaoBanHang")) return;
            if (SoLuongBan == null || SoLuongBan.IsDisposed)
            {
                SoLuongBan = new frmSoLuongBan();
                SoLuongBan.MdiParent = this;
                SoLuongBan.Show();
            }
            else
                SoLuongBan.Activate();
        }
        frmSanphamHethan SanphamHethan = null;
        private void mnuSanphamHethan_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("BaoCaoTonKho")) return;
            if (SanphamHethan == null || SanphamHethan.IsDisposed)
            {
                SanphamHethan = new frmSanphamHethan();
                SanphamHethan.MdiParent = this;
                SanphamHethan.Show();
            }
            else
                SanphamHethan.Activate();
        }

        private void mnuThoat_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        frmThongtinCuahang ThongtinCuahang = null;
        private void mnuTuychinhThongtin_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("QuanTri")) return;

            if (ThongtinCuahang == null || ThongtinCuahang.IsDisposed)
            {
                ThongtinCuahang = new frmThongtinCuahang();
                ThongtinCuahang.MdiParent = this;
                ThongtinCuahang.Show();
            }
            else
                ThongtinCuahang.Activate();
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
            if (NhaCungCap == null || NhaCungCap.IsDisposed)
            {
                NhaCungCap = new frmNhaCungCap();
                NhaCungCap.MdiParent = this;
                NhaCungCap.Show();
            }
            else
                NhaCungCap.Activate();
        }
        frmLyDoChi LyDoChi = null;
        private void mnuLyDoChi_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("LyDoChi")) return;
            if (LyDoChi == null || LyDoChi.IsDisposed)
            {
                LyDoChi = new frmLyDoChi();
                LyDoChi.MdiParent = this;
                LyDoChi.Show();
            }
            else
                LyDoChi.Activate();
        }

        frmPhieuChi PhieuChi = null;
        private void mnuPhieuChi_Click(object sender, EventArgs e)
        {
            if (!KiemTraQuyen("ThuChi")) return;
            if (PhieuChi == null || PhieuChi.IsDisposed)
            {
                PhieuChi = new frmPhieuChi();
                PhieuChi.MdiParent = this;
                PhieuChi.Show();
            }
            else
                PhieuChi.Activate();
        }

        private void mnuTrogiupHuongdan_Click(object sender, EventArgs e)
        {
            // Help.ShowHelp(this, "CPP.CHM");
        }

        private void KhoiTaoMenuTaiKhoan()
        {
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
            ToolStripMenuItem audit = new ToolStripMenuItem("Nhật ký thao tác");
            audit.Visible = AccessControl.Can("XemNhatKy");
            audit.Click += delegate {
                if (!KiemTraQuyen("XemNhatKy")) return;
                DataService rows = new DataService();
                rows.Load(new System.Data.OleDb.OleDbCommand("SELECT * FROM NHAT_KY ORDER BY THOI_GIAN DESC, ID DESC"));
                using (Form viewer = new Form())
                {
                    viewer.Text = "Nhật ký thao tác"; viewer.Width = 1050; viewer.Height = 550;
                    DataGridView grid = new DataGridView(); grid.Dock = DockStyle.Fill;
                    grid.ReadOnly = true; grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false;
                    grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
                    grid.DataSource = rows; viewer.Controls.Add(grid); viewer.ShowDialog(this);
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