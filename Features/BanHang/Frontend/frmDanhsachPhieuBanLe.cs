using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using CuahangNongduoc.BusinessObject;
using CuahangNongduoc.Controller;

namespace CuahangNongduoc
{
    public partial class frmDanhsachPhieuBanLe : Form
    {
        public frmDanhsachPhieuBanLe()
        {
            InitializeComponent();
            CuahangNongduoc.ResponsiveLayout.Attach(this);
        }

        PhieuBanController ctrl = new PhieuBanController();
        KhachHangController ctrlKH = new KhachHangController();

        private void frmDanhsachPhieuNhap_Load(object sender, EventArgs e)
        {
            ctrlKH.HienthiKhachHangDataGridviewComboBox(colKhachhang);
            ctrl.HienthiPhieuBanLe(bindingNavigator, dataGridView);
        }

        frmBanLe BanLe = null;

        private void dataGridView_DoubleClick(object sender, EventArgs e)
        {
            if (bindingNavigator.BindingSource == null || bindingNavigator.BindingSource.Current == null) return;
            if (BanLe == null || BanLe.IsDisposed)
            {
                BanLe = new frmBanLe(ctrl);
                if (this.MdiParent != null) BanLe.MdiParent = this.MdiParent;
                BanLe.Show();
            }
            else
                BanLe.Activate();
        }

        private void bindingNavigatorAddNewItem_Click(object sender, EventArgs e)
        {
            if (BanLe == null || BanLe.IsDisposed)
            {
                BanLe = new frmBanLe();
                if (this.MdiParent != null) BanLe.MdiParent = this.MdiParent;
                BanLe.Show();
            }
            else
                BanLe.Activate();
        }

        // TEMPLATE METHOD: xóa qua XoaHoaDon rồi tải lại danh sách
        private void dataGridView_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc chắn xóa không?", "Phieu Ban Le", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }

            DataRowView view = e.Row.DataBoundItem as DataRowView;
            if (view == null)
            {
                e.Cancel = true;
                return;
            }

            // Tự xử lý việc xóa; không để lưới xóa dòng trực tiếp
            String maPhieu = view["ID"].ToString();
            e.Cancel = true;
            this.BeginInvoke(new MethodInvoker(delegate { XoaVaTaiLai(maPhieu); }));
        }

        private void bindingNavigatorDeleteItem_Click(object sender, EventArgs e)
        {
            DataRowView view = (DataRowView)bindingNavigator.BindingSource.Current;
            if (view == null)
                return;

            if (MessageBox.Show("Bạn có chắc chắn xóa không?", "Phieu Ban Le", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                XoaVaTaiLai(view["ID"].ToString());
            }
        }

        // Xóa hóa đơn (hoàn tồn, xóa chi tiết, phiếu và phí) rồi tải lại danh sách
        void XoaVaTaiLai(String maPhieu)
        {
            try
            {
                ThaoTacPhieuBan xoa = new XoaHoaDon(ctrl, maPhieu);
                xoa.ThucHien();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Xóa phiếu bán thất bại. Chi tiết: " + ex.Message, "Phieu Ban Le", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            ctrl.HienthiPhieuBanLe(bindingNavigator, dataGridView);
        }

        private void toolPrint_Click(object sender, EventArgs e)
        {
            DataRowView row = (DataRowView)bindingNavigator.BindingSource.Current;
            if (row != null)
            {
                PhieuBanController ctrlPB = new PhieuBanController();
                String ma_phieu = row["ID"].ToString();
                CuahangNongduoc.BusinessObject.PhieuBan ph = ctrlPB.LayPhieuBan(ma_phieu);
                frmInPhieuBan PhieuBan = new frmInPhieuBan(ph);
                PhieuBan.Show();
            }
        }

        private void toolTimKiem_Click(object sender, EventArgs e)
        {
            frmTimPhieuBanLe Tim = new frmTimPhieuBanLe(false);
            Point p = PointToScreen(toolTimKiem.Bounds.Location);
            p.X += toolTimKiem.Width;
            p.Y += toolTimKiem.Height;
            Tim.Location = p;
            Tim.ShowDialog();
            // Chưa chọn khách hàng thì không tìm
            if (Tim.DialogResult == DialogResult.OK && Tim.cmbNCC.SelectedValue != null)
            {
                ctrl.TimPhieuBan(Tim.cmbNCC.SelectedValue.ToString(), Tim.dtNgayNhap.Value.Date);
            }
        }

        private void dataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
        }
    }
}