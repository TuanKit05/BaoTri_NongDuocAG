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
        private ThuChiEditor editor;
        public frmPhieuChi()
        {
            InitializeComponent();
            txtMaPhieu.ReadOnly = true;
            colMaPhieu.ReadOnly = true;
            cmbLyDoChi.AutoCompleteSource = AutoCompleteSource.ListItems;
            cmbLyDoChi.DropDownStyle = ComboBoxStyle.DropDownList;
            dataGridView.AllowUserToAddRows = false;
            CuahangNongduoc.ResponsiveLayout.Attach(this);
            this.Load += delegate { SecuredUi.ApplyForm(this); };
        }

        private void frmThanhToan_Load(object sender, EventArgs e)
        {
            ctrlLyDo.HienthiAutoComboBox(cmbLyDoChi);
            ctrlLyDo.HienthiDataGridviewComboBox(colLyDoChi);
            ctrl.HienthiPhieuChi(bindingNavigator, dataGridView, cmbLyDoChi, txtMaPhieu, dtNgayChi, numTongTien, txtGhiChu);
            editor = new ThuChiEditor(new ThuChiControls(bindingNavigator, dataGridView,
                cmbLyDoChi, txtMaPhieu, dtNgayChi, numTongTien, txtGhiChu),
                "ID_LY_DO_CHI", "NGAY_CHI", toolAdd, toolSave, toolDelete, toolIn, toolTimKiem,
                delegate { return ctrl.NewRow(); }, delegate(DataRow row) { ctrl.Add(row); },
                delegate { ctrl.Save(); });
        }

        private void toolAdd_Click(object sender, EventArgs e)
        {
            if (editor != null) editor.BeginAdd();
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
                if (editor != null) editor.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ErrorLog.UserMessage(ex, "Không thể lưu phiếu chi"),
                    "Lưu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            if (editor != null) editor.RefreshSource();
        }
    }
}
