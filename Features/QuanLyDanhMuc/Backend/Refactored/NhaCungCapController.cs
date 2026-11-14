using CuahangNongduoc.BusinessObject;
using CuahangNongduoc.DataLayer;
using System;
using System.Data;
using System.Windows.Forms;

namespace CuahangNongduoc.Controller
{
    public class NhaCungCapController : BaseController
    {
        private readonly NhaCungCapFactory factory = new NhaCungCapFactory();

        protected override BaseFactory Factory => factory;

        public void TimHoTen(string ten)
        {
            factory.TimHoTen(ten);
        }

        public void TimDiaChi(string diachi)
        {
            factory.TimDiaChi(diachi);
        }

        public override DataRow NewRow()
        {
            return factory.NewRow();
        }

        public NhaCungCap LayNCC(string id)
        {
            DataTable tbl = factory.LayNCC(id);
            NhaCungCap ncc = new NhaCungCap();
            if (tbl.Rows.Count > 0)
            {
                ncc.Id = Convert.ToString(tbl.Rows[0]["ID"]);
                ncc.HoTen = Convert.ToString(tbl.Rows[0]["HO_TEN"]);
                ncc.DienThoai = Convert.ToString(tbl.Rows[0]["DIEN_THOAI"]);
                ncc.DiaChi = Convert.ToString(tbl.Rows[0]["DIA_CHI"]);
            }
            return ncc;
        }

        public void HienthiAutoComboBox(ComboBox cmb)
        {
            DataTable tbl = factory.DanhsachNCC();
            cmb.DataSource = tbl;
            cmb.DisplayMember = "HO_TEN";
            cmb.ValueMember = "ID";
            cmb.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cmb.AutoCompleteSource = AutoCompleteSource.ListItems;
        }

        public void HienthiDataGridviewComboBox(System.Windows.Forms.DataGridViewComboBoxColumn cmb)
        {
            cmb.DataSource = factory.DanhsachNCC();
            cmb.DisplayMember = "HO_TEN";
            cmb.ValueMember = "ID";
            cmb.DataPropertyName = "ID_NHA_CUNG_CAP";
            cmb.HeaderText = "Nhà cung cấp";
        }
    }
}