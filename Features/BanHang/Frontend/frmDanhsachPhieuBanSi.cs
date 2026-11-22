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
    public partial class frmDanhsachPhieuBanSi : Form
    {
        private const string TieuDeThongBao = "Danh sách phiếu bán sỉ";
        public frmDanhsachPhieuBanSi()
        {
            InitializeComponent();
            CuahangNongduoc.ResponsiveLayout.Attach(this);
            ResponsiveLayout.ConfigureSaleList(dataGridView, true);
        }

        PhieuBanController ctrl = new PhieuBanController();
        KhachHangController ctrlKH = new KhachHangController();

        private void frmDanhsachPhieuNhap_Load(object sender, EventArgs e)
        {
            ctrlKH.HienthiDaiLyDataGridviewComboBox(colKhachhang);
            ctrl.HienthiPhieuBanSi(bindingNavigator, dataGridView);
        }

        frmBanSi BanLe = null;

        private void dataGridView_DoubleClick(object sender, EventArgs e)
        {
            MoPhieuBan(false);
        }

        private void bindingNavigatorAddNewItem_Click(object sender, EventArgs e)
        {
            MoPhieuBan(true);
        }

        private void MoPhieuBan(bool themMoi)
        {
            if (BanLe != null && !BanLe.IsDisposed)
            {
                BanLe.BringToFront();
                BanLe.Activate();
                return;
            }
            if (themMoi) BanLe = new frmBanSi();
            else
            {
                DataRowView selected = bindingNavigator.BindingSource == null ? null :
                    bindingNavigator.BindingSource.Current as DataRowView;
                if (selected == null) return;
                // The editor must not follow selection changes in the list behind it.
                PhieuBanController editorController = new PhieuBanController();
                editorController.TaiPhieuBan(Convert.ToString(selected["ID"]));
                BanLe = new frmBanSi(editorController);
            }
            frmBanSi editor = BanLe;
            editor.ShowInTaskbar = false;
            editor.FormClosed += delegate {
                if (IsDisposed || Disposing) return;
                string selectedId = editor.MaPhieuHienTai;
                ctrl.HienthiPhieuBanSi(bindingNavigator, dataGridView);
                int position = bindingNavigator.BindingSource.Find("ID", selectedId);
                if (position >= 0) bindingNavigator.BindingSource.Position = position;
                if (Visible) { Activate(); WindowState = FormWindowState.Maximized; }
            };
            // Use an owned top-level window so a normal editor cannot restore the maximized MDI list.
            editor.Show(MdiParent == null ? (Form)this : MdiParent);
        }

        // TEMPLATE METHOD: xóa qua XoaHoaDon rồi tải lại danh sách
        private void dataGridView_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc chắn xóa không?", TieuDeThongBao, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
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

            if (MessageBox.Show("Bạn có chắc chắn xóa không?", TieuDeThongBao, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
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
                MessageBox.Show("Xóa phiếu bán thất bại. Chi tiết: " + ErrorLog.UserMessage(ex, "Kh\u00f4ng th\u1ec3 ho\u00e0n t\u1ea5t thao t\u00e1c"), TieuDeThongBao, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            ctrl.HienthiPhieuBanSi(bindingNavigator, dataGridView);
        }

        private void toolIn_Click(object sender, EventArgs e)
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
            frmTimPhieuBanLe Tim = new frmTimPhieuBanLe(true);
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