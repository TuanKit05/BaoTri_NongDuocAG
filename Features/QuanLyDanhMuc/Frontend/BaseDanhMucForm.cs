using System;
using System.Data;
using System.Windows.Forms;
using CuahangNongduoc.Controller;

namespace CuahangNongduoc
{
    public class BaseDanhMucForm : Form
    {
        public BaseDanhMucForm()
        {
            StartPosition = FormStartPosition.CenterScreen;
        }
        protected DataGridView dataGridView;
        protected BindingNavigator bindingNavigator;
        protected ToolStripTextBox bindingNavigatorPositionItem;

        protected virtual BaseController Controller { get { return null; } }
        protected virtual string TenDanhMuc { get { return String.Empty; } }

        protected bool IsDesignMode
        {
            get
            {
                return System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime
                    || DesignMode || (Site != null && Site.DesignMode);
            }
        }

        protected virtual void Base_Load(object sender, EventArgs e)
        {
            if (IsDesignMode) return;
            LoadData();
            if ((this is frmKhachHang || this is frmDaiLy || this is frmNhaCungCap) && dataGridView != null)
                dataGridView.AllowUserToAddRows = false;
            SecuredUi.ApplyForm(this);
        }

        protected virtual void LoadData()
        {
            if (Controller != null && dataGridView != null && bindingNavigator != null)
            {
                Controller.HienthiDataGridview(dataGridView, bindingNavigator);
            }
        }

        protected virtual void DemandSavePermission()
        {
            AccessControl.DemandCurrent("SuaDanhMuc");
        }

        protected virtual void Luu()
        {
            try
            {
                DemandSavePermission();
                if (Controller != null)
                {
                    if (bindingNavigator != null && bindingNavigator.BindingSource != null)
                    {
                        bindingNavigator.BindingSource.EndEdit();
                    }

                    bool success = Controller.Save();
                    if (success)
                    {
                        MessageBox.Show($"Lưu dữ liệu {TenDanhMuc} thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Không có thay đổi hoặc lưu dữ liệu {TenDanhMuc} thất bại.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu: {ErrorLog.UserMessage(ex, "Không thể hoàn tất thao tác - BaseDanhMucForm")}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected virtual void Thoat()
        {
            this.Close();
        }

        protected virtual void XoaDongHienTai()
        {
            if (!AccessControl.Can("SuaDanhMuc"))
            { MessageBox.Show("Bạn chỉ được xem danh mục này.", "Phân quyền"); return; }
            if (dataGridView != null && dataGridView.CurrentRow != null && !dataGridView.CurrentRow.IsNewRow)
            {
                dataGridView.Rows.Remove(dataGridView.CurrentRow);
            }
        }

        protected virtual void HuyXoaDong(DataGridViewRowCancelEventArgs e)
        {
            if (!AccessControl.Can("SuaDanhMuc")) e.Cancel = true;
        }
    }
}
