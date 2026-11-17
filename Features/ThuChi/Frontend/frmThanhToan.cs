using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using CuahangNongduoc.Controller;

namespace CuahangNongduoc
{
    public partial class frmThanhToan : Form
    {
        KhachHangController ctrlKH = new KhachHangController();
        PhieuThanhToanController ctrl = new PhieuThanhToanController();
        public frmThanhToan()
        {
            InitializeComponent();
        }

        private void frmThanhToan_Load(object sender, EventArgs e)
        {
            ctrlKH.HienthiChungAutoComboBox(cmbKhachHang);
            ctrlKH.HienthiKhachHangChungDataGridviewComboBox(colKhachHang);
            ctrl.HienthiPhieuThanhToan(bindingNavigator, dataGridView, cmbKhachHang, txtMaPhieu, dtNgayThanhToan, numTongTien, txtGhiChu);
            bindingNavigator.BindingSource.AddingNew += new AddingNewEventHandler(BindingSource_AddingNew);
        }

        void BindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {

        }

        private void toolAdd_Click(object sender, EventArgs e)
        {
            if (cmbKhachHang.SelectedValue == null || cmbKhachHang.SelectedValue == DBNull.Value)
            {
                MessageBox.Show("Vui lòng chọn khách hàng trước khi tạo phiếu.");
                return;
            }
            long maphieu = ThamSo.LayMaPhieuThanhToan(); ;
            ThamSo.GanMaPhieuThanhToan(maphieu + 1);

            DataRow row = ctrl.NewRow();
            row["ID"] = maphieu;
            row["NGAY_THANH_TOAN"] = DateTime.Today;
            row["TONG_TIEN"] = 0m;
            row["GHI_CHU"] = String.Empty;
            if (cmbKhachHang.SelectedValue != null && cmbKhachHang.SelectedValue != DBNull.Value)
                row["ID_KHACH_HANG"] = cmbKhachHang.SelectedValue;
            ctrl.Add(row);
            bindingNavigator.BindingSource.MoveLast();
        }

        private void dataGridView_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (MessageBox.Show("Bạn chắc chắn xóa phiếu thanh toán này không?", "Phieu Thanh Toan", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                e.Cancel = true;
            }
        }

        private void toolDelete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Bạn chắc chắn xóa phiếu thanh toán này không?", "Phieu Thanh Toan", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                bindingNavigator.BindingSource.RemoveCurrent();
                LuuThayDoi();
            }
        }

        private void toolSave_Click(object sender, EventArgs e)
        {
            LuuThayDoi();
        }

        // Extract Method: all save paths commit edits before writing to the database.
        private void LuuThayDoi()
        {
            try
            {
                if (!this.ValidateChildren()) return;
                dataGridView.EndEdit();
                if (bindingNavigator.BindingSource != null)
                    bindingNavigator.BindingSource.EndEdit();
                ctrl.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể lưu phiếu thanh toán: " + ex.Message,
                    "Lỗi lưu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = true;
        }

        private void toolIn_Click(object sender, EventArgs e)
        {
            if (!this.ValidateChildren()) return;
            dataGridView.EndEdit();
            if (bindingNavigator.BindingSource == null) return;
            bindingNavigator.BindingSource.EndEdit();
            DataRowView row = bindingNavigator.BindingSource.Current as DataRowView;
            if (row == null) return;
            if (row.Row.RowState != DataRowState.Unchanged)
            {
                MessageBox.Show("Vui lòng lưu phiếu thanh toán trước khi in.");
                return;
            }
            CuahangNongduoc.BusinessObject.PhieuThanhToan ph = ctrl.LayPhieuThanhToan(
                Convert.ToString(row["ID"]));
            if (ph == null)
            {
                MessageBox.Show("Không tìm thấy phiếu thanh toán đã lưu.");
                return;
            }
            new frmInPhieuThanhToan(ph).Show();
        }

        private void toolTimKiem_Click(object sender, EventArgs e)
        {
            frmTimPhieuThu Tim = new frmTimPhieuThu();
            Point p = PointToScreen(toolTimKiem.Bounds.Location);
            p.X += toolTimKiem.Width;
            p.Y += toolTimKiem.Height;
            Tim.Location = p;
            Tim.ShowDialog();
            if (Tim.DialogResult == DialogResult.OK)
            {
                String maKhachHang = Tim.cmbKhachHang.SelectedValue.ToString();
                DateTime ngayThu = Tim.dtNgayThu.Value.Date;
                ctrl.TimPhieuThanhToan(bindingNavigator, dataGridView, cmbKhachHang,
                    txtMaPhieu, dtNgayThanhToan, numTongTien, txtGhiChu,
                    maKhachHang, ngayThu);
            }
        }

    }
}