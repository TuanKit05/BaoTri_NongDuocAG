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
        public frmMain()
        {
            InitializeComponent();
        }
        frmDonViTinh DonViTinh = null;

        private void ShowChildForm<T>(ref T form)
        where T : Form, new()
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

        }
        frmSanPham SanPham = null;
        private void mnuSanPham_Click(object sender, EventArgs e)
        {
            ShowChildForm(ref SanPham);
        }
        frmKhachHang KhachHang = null;
        private void mnuKhachHang_Click(object sender, EventArgs e)
        {
            ShowChildForm(ref KhachHang);
        }
        frmDaiLy DaiLy = null;
        private void mnuDaiLy_Click(object sender, EventArgs e)
        {
            ShowChildForm(ref DaiLy);

        }
        frmDanhsachPhieuNhap NhapHang = null;
        private void mnuNhapHang_Click(object sender, EventArgs e)
        {
            ShowChildForm(ref NhapHang);
        }
        frmDanhsachPhieuBanLe BanLe = null;
        private void mnuBanHangKH_Click(object sender, EventArgs e)
        {
            ShowChildForm(ref BanLe);
        }
        frmDanhsachPhieuBanSi BanSi = null;
        private void mnuBanHangDL_Click(object sender, EventArgs e)
        {
            ShowChildForm(ref BanSi);
        }

        private void ToggleVisibility(ToolStripMenuItem menuItem, Control target)
        {
            menuItem.Checked = !menuItem.Checked;
            target.Visible = menuItem.Checked;
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
            ShowChildForm(ref ThanhToan);
        }
        frmDunoKhachhang DunoKhachhang = null;
        private void mnuTonghopDuno_Click(object sender, EventArgs e)
        {
            ShowChildForm(ref DunoKhachhang);
        }
        frmDoanhThu DoanhThu = null;
        private void mnuBaocaoDoanhThu_Click(object sender, EventArgs e)
        {
            ShowChildForm(ref DoanhThu);

        }

        frmSoLuongTon SoLuongTon = null;
        private void mnuBaocaoSoluongton_Click(object sender, EventArgs e)
        {
            ShowChildForm(ref SoLuongTon);

        }

        frmThongKeTonKho ThongKeTonKho = null;
        private void mnuThongKeTonKho_Click(object sender, EventArgs e)
        {
            ShowChildForm(ref ThongKeTonKho);
        }
        frmSoLuongBan SoLuongBan = null;
        private void mnuSoLuongBan_Click(object sender, EventArgs e)
        {
            ShowChildForm(ref SoLuongBan);
        }
        frmSanphamHethan SanphamHethan = null;
        private void mnuSanphamHethan_Click(object sender, EventArgs e)
        {
            ShowChildForm(ref SanphamHethan);
        }

        private void mnuThoat_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        frmThongtinCuahang ThongtinCuahang = null;
        private void mnuTuychinhThongtin_Click(object sender, EventArgs e)
        {
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
            ShowChildForm(ref NhaCungCap);
        }
        frmLyDoChi LyDoChi = null;
        private void mnuLyDoChi_Click(object sender, EventArgs e)
        {
            ShowChildForm(ref LyDoChi);
        }

        frmPhieuChi PhieuChi = null;
        private void mnuPhieuChi_Click(object sender, EventArgs e)
        {
            ShowChildForm(ref PhieuChi);
        }

        private void mnuTrogiupHuongdan_Click(object sender, EventArgs e)
        {
            // Help.ShowHelp(this, "CPP.CHM");
        }
    }
}