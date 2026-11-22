using System;
using System.Data;
using System.Windows.Forms;

namespace CuahangNongduoc.Controller
{
    /// <summary>
    /// Template Method cho các controller quản lý danh mục đơn giản (bảng có khóa ID).
    /// Lớp cha định nghĩa thuật toán hiển thị/lưu dữ liệu; lớp con chỉ khai báo
    /// nguồn dữ liệu và tên cột (các bước có thể thay đổi).
    /// </summary>
    public abstract class DanhMucController
    {
        // ----- Các bước do lớp con cung cấp -----

        /// <summary>Lấy toàn bộ dữ liệu của danh mục.</summary>
        protected abstract DataTable LayDanhSach();

        /// <summary>Lưu thay đổi xuống cơ sở dữ liệu.</summary>
        protected abstract bool LuuDuLieu();

        /// <summary>Cột hiển thị trong ComboBox / lưới.</summary>
        protected abstract string DisplayMember { get; }

        /// <summary>Cột giá trị (khóa) của danh mục.</summary>
        protected virtual string ValueMember
        {
            get { return "ID"; }
        }

        /// <summary>Tên thuộc tính dữ liệu của cột khóa ngoại trong bảng cha.</summary>
        protected abstract string DataPropertyName { get; }

        /// <summary>Tiêu đề cột khi hiển thị trong lưới.</summary>
        protected abstract string HeaderText { get; }

        // ----- Thuật toán chung (template) -----

        /// <summary>Nạp danh mục vào ComboBox.</summary>
        public void HienthiAutoComboBox(ComboBox cmb)
        {
            cmb.DataSource = LayDanhSach();
            cmb.DisplayMember = DisplayMember;
            cmb.ValueMember = ValueMember;
        }

        /// <summary>Nạp danh mục vào lưới và thanh điều hướng.</summary>
        public void HienthiDataGridview(DataGridView dg, BindingNavigator bn)
        {
            BindingSource bs = new BindingSource();
            bs.DataSource = LayDanhSach();
            bn.BindingSource = bs;
            dg.DataSource = bs;
        }

        /// <summary>Cấu hình một cột ComboBox dùng trong lưới của form khác.</summary>
        protected void CauHinhComboBoxColumn(DataGridViewComboBoxColumn cmb)
        {
            cmb.DataSource = LayDanhSach();
            cmb.DisplayMember = DisplayMember;
            cmb.ValueMember = ValueMember;
            cmb.DataPropertyName = DataPropertyName;
            cmb.HeaderText = HeaderText;
        }

        /// <summary>Lưu danh mục.</summary>
        public bool Save()
        {
            return LuuDuLieu();
        }
    }
}
