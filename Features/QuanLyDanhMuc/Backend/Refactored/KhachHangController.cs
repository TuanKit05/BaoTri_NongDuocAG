using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using CuahangNongduoc.BusinessObject;
using CuahangNongduoc.DataLayer;

namespace CuahangNongduoc.Controller
{
    /// <summary>
    /// Controller Khách hàng / Đại lý – kế thừa BaseController.
    /// Có nhiều logic đặc thù (phân biệt LoaiKH) nên giữ lại các method riêng.
    /// </summary>
    public class KhachHangController : BaseController
    {
        private readonly KhachHangFactory _factory = new KhachHangFactory();

        protected override BaseFactory Factory => _factory;

        public void HienthiAutoComboBox(ComboBox cmb, bool loai)
        {
            cmb.DataSource = _factory.DanhsachKhachHang(loai);
            cmb.DisplayMember = "HO_TEN";
            cmb.ValueMember = "ID";
        }

        public void HienthiChungAutoComboBox(ComboBox cmb)
        {
            cmb.DataSource = _factory.DanhsachKhachHang();
            cmb.DisplayMember = "HO_TEN";
            cmb.ValueMember = "ID";
        }

        public void HienthiKhachHangDataGridview(DataGridView dg, BindingNavigator bn)
        {
            BindingSource bs = new BindingSource();
            DataTable tbl = _factory.DanhsachKhachHang(false);
            tbl.Columns[4].DefaultValue = false;
            bs.DataSource = tbl;
            bn.BindingSource = bs;
            dg.DataSource = bs;
        }

        public void HienthiDaiLyDataGridview(DataGridView dg, BindingNavigator bn)
        {
            BindingSource bs = new BindingSource();
            DataTable tbl = _factory.DanhsachKhachHang(true);
            tbl.Columns[4].DefaultValue = true;
            bs.DataSource = tbl;
            bn.BindingSource = bs;
            dg.DataSource = bs;
        }

        public void HienthiKhachHangChungDataGridviewComboBox(DataGridViewComboBoxColumn cmb)
        {
            cmb.DataSource = _factory.DanhsachKhachHang();
            cmb.DisplayMember = "HO_TEN";
            cmb.ValueMember = "ID";
            cmb.DataPropertyName = "ID_KHACH_HANG";
            cmb.HeaderText = "Khách hàng";
        }

        public void HienthiKhachHangDataGridviewComboBox(DataGridViewComboBoxColumn cmb)
        {
            cmb.DataSource = _factory.DanhsachKhachHang(false);
            cmb.DisplayMember = "HO_TEN";
            cmb.ValueMember = "ID";
            cmb.DataPropertyName = "ID_KHACH_HANG";
            cmb.HeaderText = "Khách hàng";
        }

        public void HienthiDaiLyDataGridviewComboBox(DataGridViewComboBoxColumn cmb)
        {
            cmb.DataSource = _factory.DanhsachKhachHang(true);
            cmb.DisplayMember = "HO_TEN";
            cmb.ValueMember = "ID";
            cmb.DataPropertyName = "ID_KHACH_HANG";
            cmb.HeaderText = "Đại lý";
        }

        public void TimHoTen(string hoten, bool loai) => _factory.TimHoTen(hoten, loai);
        public void TimDiaChi(string diachi, bool loai) => _factory.TimDiaChi(diachi, loai);

        public KhachHang LayKhachHang(string id)
        {
            DataTable tbl = _factory.LayKhachHang(id);
            KhachHang kh = new KhachHang();
            if (tbl.Rows.Count > 0)
            {
                kh.Id = Convert.ToString(tbl.Rows[0]["ID"]);
                kh.HoTen = Convert.ToString(tbl.Rows[0]["HO_TEN"]);
                kh.DienThoai = Convert.ToString(tbl.Rows[0]["DIEN_THOAI"]);
                kh.DiaChi = Convert.ToString(tbl.Rows[0]["DIA_CHI"]);
                kh.LoaiKH = Convert.ToBoolean(tbl.Rows[0]["LOAI_KH"]);
            }
            return kh;
        }

        public IList<KhachHang> LayDanhSachKhachHang()
        {
            DataTable tbl = _factory.DanhsachKhachHang();
            IList<KhachHang> ds = new List<KhachHang>();

            foreach (DataRow row in tbl.Rows)
            {
                KhachHang kh = new KhachHang();
                kh.Id = Convert.ToString(row["ID"]);
                kh.HoTen = Convert.ToString(row["HO_TEN"]);
                kh.DienThoai = Convert.ToString(row["DIEN_THOAI"]);
                kh.DiaChi = Convert.ToString(row["DIA_CHI"]);
                kh.LoaiKH = Convert.ToBoolean(row["LOAI_KH"]);
                ds.Add(kh);
            }
            return ds;
        }
    }
}
