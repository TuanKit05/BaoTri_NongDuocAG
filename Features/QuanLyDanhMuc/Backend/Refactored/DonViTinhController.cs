using System;
using System.Data;
using CuahangNongduoc.BusinessObject;
using System.Windows.Forms;
using CuahangNongduoc.DataLayer;

namespace CuahangNongduoc.Controller
{
    public class DonViTinhController : BaseController
    {
        private readonly DonViTinhFactory factory = new DonViTinhFactory();

        protected override BaseFactory Factory => factory;

        // Khôi phục API được MaSanPhamController sử dụng.
        public DonViTinh LayDVT(int id)
        {
            DataTable bang = factory.LayDVT(id);
            if (bang.Rows.Count == 0)
                return null;

            DataRow row = bang.Rows[0];
            return new DonViTinh
            {
                Id = Convert.ToString(row["ID"]),
                TenDonViTinh = Convert.ToString(row["TEN_DON_VI_TINH"])
            };
        }

        public void HienthiAutoComboBox(ComboBox cmb)
        {
            cmb.DataSource = factory.DanhSach();
            cmb.DisplayMember = "TEN_DON_VI_TINH";
            cmb.ValueMember = "ID";
        }

        public DataGridViewComboBoxColumn HienthiDataGridViewComboBoxColumn()
        {
            DataGridViewComboBoxColumn cmb = new DataGridViewComboBoxColumn();
            cmb.DataSource = factory.DanhSach();
            cmb.DisplayMember = "TEN_DON_VI_TINH";
            cmb.ValueMember = "ID";
            cmb.DataPropertyName = "ID_DON_VI_TINH";
            cmb.HeaderText = "Đơn vị tính";
            return cmb;
        }
    }
}