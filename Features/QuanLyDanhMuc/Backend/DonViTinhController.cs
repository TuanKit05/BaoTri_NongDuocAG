using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using CuahangNongduoc.BusinessObject;
using CuahangNongduoc.DataLayer;


namespace CuahangNongduoc.Controller
{
    public class DonViTinhController : DanhMucController
    {
        DonViTinhFactory factory = new DonViTinhFactory();

        protected override DataTable LayDanhSach()
        {
            return factory.DanhsachDVT();
        }

        protected override bool LuuDuLieu()
        {
            return factory.Save();
        }

        protected override string DisplayMember
        {
            get { return "TEN_DON_VI"; }
        }

        protected override string DataPropertyName
        {
            get { return "ID_DON_VI_TINH"; }
        }

        protected override string HeaderText
        {
            get { return "Đơn vị tính"; }
        }

        public System.Windows.Forms.DataGridViewComboBoxColumn HienthiDataGridViewComboBoxColumn()
        {
            System.Windows.Forms.DataGridViewComboBoxColumn cmb = new System.Windows.Forms.DataGridViewComboBoxColumn();
            CauHinhComboBoxColumn(cmb);
            return cmb;
        }

        public DonViTinh LayDVT(int id)
        {
            DataTable tbl = factory.LayDVT(id);
            DonViTinh dvt = null;
            if (tbl.Rows.Count > 0)
            {
                dvt = new DonViTinh(Convert.ToInt32(tbl.Rows[0]["ID"]), Convert.ToString(tbl.Rows[0]["TEN_DON_VI"]));
            }
            return dvt;
        }
    }
}
