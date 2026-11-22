using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Windows.Forms;
using CuahangNongduoc.DataLayer;
using CuahangNongduoc.BusinessObject;

namespace CuahangNongduoc.Controller
{
    public class PhieuNhapController
    {
        private PhieuNhapFactory factory = new PhieuNhapFactory();
        private BindingSource bs = new BindingSource();

        public PhieuNhapController()
        {
            bs.DataSource = factory.LayPhieuNhap("-1");
        }

        public void TaiNapDanhSach()
        {
            bs.DataSource = factory.DanhsachPhieuNhap();
        }

        public void ChonPhieuNhap(string maPhieu)
        {
            for (int i = 0; i < bs.Count; i++)
            {
                DataRowView row = bs[i] as DataRowView;
                if (row != null && String.Equals(Convert.ToString(row["ID"]),
                    maPhieu, StringComparison.OrdinalIgnoreCase))
                {
                    bs.Position = i;
                    return;
                }
            }
        }

        public DataRow NewRow()
        {
            return factory.NewRow();
        }

        public void Add(DataRow row)
        {
            factory.Add(row);
        }

        public bool Update()
        {
            bs.EndEdit();
            return factory.Save();
        }

        public bool Save()
        {
            bs.EndEdit();
            return factory.Save();
        }

        public PhieuNhap LayPhieuNhap(String id)
        {
            DataTable tbl = factory.LayPhieuNhap(id);
            if (tbl.Rows.Count == 0)
                return null;

            DataRow row = tbl.Rows[0];
            PhieuNhap ph = new PhieuNhap();
            ph.Id = Convert.ToString(row["ID"]);
            ph.NgayNhap = Convert.ToDateTime(row["NGAY_NHAP"]);
            ph.TongTien = Convert.ToInt64(row["TONG_TIEN"]);
            ph.DaTra = Convert.ToInt64(row["DA_TRA"]);
            ph.ConNo = Convert.ToInt64(row["CON_NO"]);
            ph.NhaCungCap = new NhaCungCapController().LayNCC(Convert.ToString(row["ID_NHA_CUNG_CAP"]));
            ph.ChiTiet = new MaSanPhamController().ChiTietPhieuNhap(ph.Id);
            return ph;
        }

        public void HienthiPhieuNhap(BindingNavigator bn, DataGridView dg)
        {
            TaiNapDanhSach();
            bn.BindingSource = bs;
            dg.DataSource = bs;
        }

        public void HienthiPhieuNhap(BindingNavigator bn, TextBox txt, ComboBox cmb,
            DateTimePicker dt, NumericUpDown numTongTien, NumericUpDown numDaTra,
            NumericUpDown numConNo)
        {
            bn.BindingSource = bs;

            txt.DataBindings.Clear();
            txt.DataBindings.Add("Text", bs, "ID", true, DataSourceUpdateMode.Never);

            cmb.DataBindings.Clear();
            cmb.DataBindings.Add("SelectedValue", bs, "ID_NHA_CUNG_CAP", true, DataSourceUpdateMode.Never);

            dt.DataBindings.Clear();
            dt.DataBindings.Add("Value", bs, "NGAY_NHAP", true, DataSourceUpdateMode.Never);

            numTongTien.DataBindings.Clear();
            numTongTien.DataBindings.Add("Value", bs, "TONG_TIEN", true, DataSourceUpdateMode.Never);

            numDaTra.DataBindings.Clear();
            numDaTra.DataBindings.Add("Value", bs, "DA_TRA", true, DataSourceUpdateMode.Never);

            numConNo.DataBindings.Clear();
            numConNo.DataBindings.Add("Value", bs, "CON_NO", true, DataSourceUpdateMode.Never);
        }

        public void TimPhieuNhap(String maNCC, DateTime dt)
        {
            bs.DataSource = factory.TimPhieuNhap(maNCC, dt.Date);
        }
    }
}
