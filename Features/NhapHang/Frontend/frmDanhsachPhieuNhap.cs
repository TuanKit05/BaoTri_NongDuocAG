using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using CuahangNongduoc.Controller;
using CuahangNongduoc.DataLayer;

namespace CuahangNongduoc
{
    public partial class frmDanhsachPhieuNhap : Form
    {
        public frmDanhsachPhieuNhap()
        {
            InitializeComponent();
        }

        PhieuNhapController ctrl = new PhieuNhapController();
        NhaCungCapController ctrlNCC = new NhaCungCapController();

        private void frmDanhsachPhieuNhap_Load(object sender, EventArgs e)
        {
            ctrlNCC.HienthiDataGridviewComboBox(colNhaCungCap);
            ctrl.HienthiPhieuNhap(bindingNavigator, dataGridView);
        }
        frmNhapHang NhapHang = null;
        private void dataGridView_DoubleClick(object sender, EventArgs e)
        {
            if (NhapHang == null || NhapHang.IsDisposed)
            {
                NhapHang = new frmNhapHang(ctrl);
                NhapHang.Show();
            }
            else
                NhapHang.Activate();
        }

        private void bindingNavigatorAddNewItem_Click(object sender, EventArgs e)
        {
            if (NhapHang == null || NhapHang.IsDisposed)
            {
                NhapHang = new frmNhapHang();
                NhapHang.Show();
            }
            else
                NhapHang.Activate();
        }

        private void toolIn_Click(object sender, EventArgs e)
        {
            DataRowView row = bindingNavigator.BindingSource == null
                ? null : bindingNavigator.BindingSource.Current as DataRowView;
            if (row == null)
            {
                MessageBox.Show("Vui lòng chọn phiếu nhập cần in.", "Phiếu nhập",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            PhieuNhapController ctrlPN = new PhieuNhapController();
            string maPhieu = Convert.ToString(row["ID"]);
            CuahangNongduoc.BusinessObject.PhieuNhap ph = ctrlPN.LayPhieuNhap(maPhieu);
            if (ph == null)
            {
                MessageBox.Show("Không tìm thấy phiếu nhập.", "Phiếu nhập",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            frmInPhieuNhap formIn = new frmInPhieuNhap(ph);
            formIn.Show();
        }

        private void bindingNavigatorDeleteItem_Click(object sender, EventArgs e)
        {
            DataRowView row = bindingNavigator.BindingSource == null
                ? null : bindingNavigator.BindingSource.Current as DataRowView;
            if (row == null)
            {
                MessageBox.Show("Vui lòng chọn phiếu nhập cần xóa.", "Phiếu nhập",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string maPhieu = Convert.ToString(row["ID"]);
            if (MessageBox.Show(
                "Chỉ xóa được phiếu chưa thanh toán và chưa có lô nào phát sinh bán hàng. Bạn có chắc muốn xóa?",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            string thongBao;
            NhapHangService dichVu = new NhapHangService();
            if (!dichVu.XoaPhieuNhap(maPhieu, out thongBao))
            {
                MessageBox.Show(thongBao, "Không thể xóa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ctrl.TaiNapDanhSach();
            MessageBox.Show(thongBao, "Phiếu nhập", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void toolTimKiem_Click(object sender, EventArgs e)
        {
            
            frmTimPhieuNhap TimPhieu = new frmTimPhieuNhap();
            Point p = PointToScreen(toolTimKiem.Bounds.Location);
            p.X += toolTimKiem.Width;
            p.Y += toolTimKiem.Height;
            TimPhieu.Location = p;
            TimPhieu.ShowDialog();
            if (TimPhieu.DialogResult == DialogResult.OK)
            {
                ctrl.TimPhieuNhap(TimPhieu.cmbNCC.SelectedValue.ToString(), TimPhieu.dtNgayNhap.Value.Date);
            }
        }
    }
}