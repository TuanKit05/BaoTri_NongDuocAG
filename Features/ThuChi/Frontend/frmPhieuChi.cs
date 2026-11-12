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
    public partial class frmPhieuChi : Form
    {
        LyDoChiController ctrlLyDo = new LyDoChiController();
        PhieuChiController ctrl = new PhieuChiController();
        public frmPhieuChi()
        {
            InitializeComponent();
        }

        private void frmThanhToan_Load(object sender, EventArgs e)
        {
            ctrlLyDo.HienthiAutoComboBox(cmbLyDoChi);
            ctrlLyDo.HienthiDataGridviewComboBox(colLyDoChi);
            ctrl.HienthiPhieuChi(bindingNavigator, dataGridView, cmbLyDoChi, txtMaPhieu, dtNgayChi, numTongTien, txtGhiChu);
        }

        private void toolAdd_Click(object sender, EventArgs e)
        {
            if (cmbLyDoChi.SelectedValue == null || cmbLyDoChi.SelectedValue == DBNull.Value)
            {
                MessageBox.Show("Vui lòng chọn lý do chi trước khi tạo phiếu.");
                return;
            }
            long maphieu = ThamSo.PhieuChi;
            ThamSo.PhieuChi = maphieu + 1;

            DataRow row = ctrl.NewRow();
            row["ID"] = maphieu;
            row["NGAY_CHI"] = DateTime.Today;
            row["TONG_TIEN"] = 0m;
            row["GHI_CHU"] = String.Empty;
            if (cmbLyDoChi.SelectedValue != null && cmbLyDoChi.SelectedValue != DBNull.Value)
                row["ID_LY_DO_CHI"] = cmbLyDoChi.SelectedValue;
            ctrl.Add(row);
            bindingNavigator.BindingSource.MoveLast();
        }

        private void dataGridView_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (MessageBox.Show("Bạn chắc chắn xóa phiếu chi này không?", "Phieu Chi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                e.Cancel = true;
            }
        }

        private void toolDelete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Bạn chắc chắn xóa phiếu chi này không?", "Phieu Chi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                bindingNavigator.BindingSource.RemoveCurrent();
                LuuThayDoi();
            }
        }

        private void toolSave_Click(object sender, EventArgs e)
        {
            LuuThayDoi();
        }

        // Extract Method: all save paths commit the current bound row before writing to Access.
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
                MessageBox.Show("Không thể lưu phiếu chi: " + ex.Message,
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
                MessageBox.Show("Vui lòng lưu phiếu chi trước khi in.");
                return;
            }
            CuahangNongduoc.BusinessObject.PhieuChi ph = ctrl.LayPhieuChi(
                Convert.ToString(row["ID"]));
            if (ph == null)
            {
                MessageBox.Show("Không tìm thấy phiếu chi đã lưu.");
                return;
            }
            new frmInPhieuChi(ph).Show();
        }

        private void btnThemLyDoChi_Click(object sender, EventArgs e)
        {
            frmLyDoChi Chi = new frmLyDoChi();
            Chi.ShowDialog();
            ctrlLyDo.HienthiAutoComboBox(cmbLyDoChi);
        }

        private void toolTimKiem_Click(object sender, EventArgs e)
        {
            frmTimPhieuChi Tim = new frmTimPhieuChi();
            Point p = PointToScreen(toolTimKiem.Bounds.Location);
            p.X += toolTimKiem.Width;
            p.Y += toolTimKiem.Height;
            Tim.Location = p;
            Tim.ShowDialog();
            if (Tim.DialogResult == DialogResult.OK)
            {
                int maLyDo = Convert.ToInt32(Tim.cmbLyDo.SelectedValue);
                DateTime ngayTim = Tim.dtNgayChi.Value.Date;
                ctrl.TimPhieuChi(bindingNavigator, dataGridView, cmbLyDoChi, txtMaPhieu,
                    dtNgayChi, numTongTien, txtGhiChu, maLyDo, ngayTim);

            }
        }
    }
}