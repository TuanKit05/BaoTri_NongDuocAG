using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using CuahangNongduoc.BusinessObject;
using CuahangNongduoc.DataLayer;


namespace CuahangNongduoc.Controller
{
    public class LyDoChiController : DanhMucController
    {
        LyDoChiFactory factory = new LyDoChiFactory();

        protected override DataTable LayDanhSach()
        {
            return factory.DanhsachLyDo();
        }

        protected override bool LuuDuLieu()
        {
            return factory.Save();
        }

        protected override string DisplayMember
        {
            get { return "LY_DO"; }
        }

        protected override string DataPropertyName
        {
            get { return "ID_LY_DO_CHI"; }
        }

        protected override string HeaderText
        {
            get { return "Lý do chi"; }
        }

        public void HienthiDataGridviewComboBox(System.Windows.Forms.DataGridViewComboBoxColumn cmb)
        {
            CauHinhComboBoxColumn(cmb);
        }

        public LyDoChi LayLyDoChi(long id)
        {
            DataTable tbl = factory.LayLyDoChi(id);
            LyDoChi lydo = new LyDoChi();
            if (tbl.Rows.Count > 0)
            {
                lydo.Id = Convert.ToInt64(tbl.Rows[0]["ID"]);
                lydo.LyDo = Convert.ToString(tbl.Rows[0]["LY_DO"]);
            }
            return lydo;
        }
    }
}
