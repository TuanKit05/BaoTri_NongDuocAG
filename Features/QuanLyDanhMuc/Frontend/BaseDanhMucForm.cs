using System;
using System.Data;
using System.Windows.Forms;
using CuahangNongduoc.Controller;

namespace CuahangNongduoc
{
    public abstract class BaseDanhMucForm : Form
    {
        protected DataGridView dataGridView;
        protected BindingNavigator bindingNavigator;
        protected ToolStripTextBox bindingNavigatorPositionItem;

        protected abstract BaseController Controller { get; }
        protected abstract string TenDanhMuc { get; }

        protected virtual void Base_Load(object sender, EventArgs e)
        {
            LoadData();
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
                MessageBox.Show($"Lỗi khi lưu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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