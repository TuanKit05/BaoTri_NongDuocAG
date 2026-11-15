using System;
using System.Collections.Generic;
using System.Text;
using CuahangNongduoc.DataLayer;
using CuahangNongduoc.BusinessObject;
using System.Windows.Forms;
using System.Data;

namespace CuahangNongduoc.Controller
{

    public class PhieuBanController
    {
        private const int GIOI_HAN_PHIEU_BAN = 50;

        PhieuBanFactory factory = new PhieuBanFactory();

        BindingSource bs = new BindingSource();


        public PhieuBanController()
        {
            bs.DataSource = factory.LayPhieuBan("-1");
        }
        public DataRow NewRow()
        {
            return factory.NewRow();
        }
        public void Add(DataRow row)
        {
            factory.Add(row);
        }
        public void Update()
        {
            bs.MoveNext();
            factory.Save();
        }

        public void Save()
        {
            int n = PhieuBanFactory.LaySoPhieu();

            if (n >= GIOI_HAN_PHIEU_BAN)
            {
                MessageBox.Show(
                    "Đây là bản dùng thử! Chỉ lưu được 50 phiếu bán!",
                    "Phieu Ban",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    "Đây là bản dùng thử! Chỉ lưu được thêm "
                    + Convert.ToString(GIOI_HAN_PHIEU_BAN - n)
                    + " phiếu bán!",
                    "Phieu Ban",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                factory.Save();
            }
        }

        private void GanNguonDuLieuPhieuBan(
            BindingNavigator bn,
            DataGridView dg)
        {
            bn.BindingSource = bs;
            dg.DataSource = bs;
        }

        public void HienthiPhieuBanLe(
            BindingNavigator bn, DataGridView dg)
        {
            bs.DataSource = factory.DanhsachPhieuBanLe();
            GanNguonDuLieuPhieuBan(bn, dg);
        }

        public void HienthiPhieuBanSi(
            BindingNavigator bn, DataGridView dg)
        {
            bs.DataSource = factory.DanhsachPhieuBanSi();
            GanNguonDuLieuPhieuBan(bn, dg);
        }

        public void HienthiPhieuBan(BindingNavigator bn, ComboBox cmb, TextBox txt, DateTimePicker dt, NumericUpDown numTongTien, NumericUpDown numDatra, NumericUpDown numConNo)
        {

            bn.BindingSource = bs;

            txt.DataBindings.Clear();
            txt.DataBindings.Add("Text", bs, "ID");

            cmb.DataBindings.Clear();
            cmb.DataBindings.Add("SelectedValue", bs, "ID_KHACH_HANG");

            dt.DataBindings.Clear();
            dt.DataBindings.Add("Value", bs, "NGAY_BAN");

            numTongTien.DataBindings.Clear();
            numTongTien.DataBindings.Add("Value", bs, "TONG_TIEN");

            numDatra.DataBindings.Clear();
            numDatra.DataBindings.Add("Value", bs, "DA_TRA");

            numConNo.DataBindings.Clear();
            numConNo.DataBindings.Add("Value", bs, "CON_NO");


        }

        public PhieuBan LayPhieuBan(String id)
        {
            DataTable tbl = factory.LayPhieuBan(id);

            if (tbl.Rows.Count == 0)
                return null;

            return TaoPhieuBan(tbl.Rows[0]);
        }

        private PhieuBan TaoPhieuBan(DataRow row)
        {
            PhieuBan ph = new PhieuBan();

            ph.Id = Convert.ToString(row["ID"]);
            ph.NgayBan = Convert.ToDateTime(row["NGAY_BAN"]);
            ph.TongTien = Convert.ToInt64(row["TONG_TIEN"]);
            ph.DaTra = Convert.ToInt64(row["DA_TRA"]);
            ph.ConNo = Convert.ToInt64(row["CON_NO"]);

            KhachHangController ctrlKH =
                new KhachHangController();

            ph.KhachHang = ctrlKH.LayKhachHang(
                Convert.ToString(row["ID_KHACH_HANG"]));

            ChiTietPhieuBanController ctrl =
                new ChiTietPhieuBanController();

            ph.ChiTiet = ctrl.ChiTietPhieuBan(ph.Id);

            return ph;
        }

        public void TimPhieuBan(String maKH, DateTime dt)
        {
            factory.TimPhieuBan(maKH, dt);

        }

    }
}
