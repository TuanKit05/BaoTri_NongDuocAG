using System.Data;
using System.Windows.Forms;
using CuahangNongduoc.DataLayer;

namespace CuahangNongduoc.Controller
{
    public class DonViTinhController : BaseController
    {
        private readonly DonViTinhFactory factory = new DonViTinhFactory();

        protected override BaseFactory Factory => factory;

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