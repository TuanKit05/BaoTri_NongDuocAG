using System;
using System.Data;
using CuahangNongduoc.BusinessObject;
using System.Windows.Forms;
using CuahangNongduoc.DataLayer;

namespace CuahangNongduoc.Controller
{
    public class LyDoChiController : BaseController
    {
        private readonly LyDoChiFactory factory = new LyDoChiFactory();

        protected override BaseFactory Factory => factory;

        // Trả về đối tượng nghiệp vụ để PhieuChiController hiển thị và in phiếu chi.
        public LyDoChi LayLyDoChi(long id)
        {
            DataTable bang = factory.LayLyDoChi(id);
            if (bang.Rows.Count == 0)
                return null;

            DataRow row = bang.Rows[0];
            return new LyDoChi
            {
                Id = Convert.ToString(row["ID"]),
                TenLyDo = Convert.ToString(row["LY_DO"])
            };
        }

        // Nạp danh sách lý do chi vào ComboBox của các form phiếu chi/tìm kiếm.
        public void HienthiAutoComboBox(ComboBox cmb)
        {
            cmb.DataSource = factory.DanhsachLyDo();
            cmb.DisplayMember = "LY_DO";
            cmb.ValueMember = "ID";
            cmb.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cmb.AutoCompleteSource = AutoCompleteSource.ListItems;
        }

        // Hiển thị lý do chi dưới dạng cột danh sách trong bảng phiếu chi.
        public void HienthiDataGridviewComboBox(DataGridViewComboBoxColumn cmb)
        {
            cmb.DataSource = factory.DanhsachLyDo();
            cmb.DisplayMember = "LY_DO";
            cmb.ValueMember = "ID";
            cmb.DataPropertyName = "ID_LY_DO_CHI";
            cmb.HeaderText = "Lý do chi";
        }

        public override DataRow NewRow()
        {
            return factory.NewRow();
        }
    }
}