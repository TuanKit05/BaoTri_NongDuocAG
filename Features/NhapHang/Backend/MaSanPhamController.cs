using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Data;
using CuahangNongduoc.DataLayer;
using CuahangNongduoc.BusinessObject;

namespace CuahangNongduoc.Controller
{
    public class MaSanPhamController
    {
        private MaSanPhanFactory factory = new MaSanPhanFactory();

        public DataRow NewRow()
        {
            return factory.NewRow();
        }

        public void Add(DataRow row)
        {
            factory.Add(row);
        }

        public bool Save()
        {
            return factory.Save();
        }

        public SanPham LaySanPham(String idMaSanPham)
        {
            MaSanPhanFactory f = new MaSanPhanFactory();
            DataTable tbl = f.LaySanPham(idMaSanPham);
            if (tbl.Rows.Count == 0)
                return null;

            DataRow row = tbl.Rows[0];
            SanPham sp = new SanPham();
            sp.Id = Convert.ToString(row["ID"]);
            sp.TenSanPham = Convert.ToString(row["TEN_SAN_PHAM"]);
            sp.SoLuong = Convert.ToInt32(row["SO_LUONG"]);
            sp.DonGiaNhap = Convert.ToInt64(row["DON_GIA_NHAP"]);
            sp.GiaBanLe = Convert.ToInt64(row["GIA_BAN_LE"]);
            sp.GiaBanSi = Convert.ToInt64(row["GIA_BAN_SI"]);
            sp.IdDonViTinh = Convert.ToString(row["ID_DON_VI_TINH"]);
            sp.DonViTinh = new DonViTinhController().LayDVT(Convert.ToInt32(row["ID_DON_VI_TINH"]));
            return sp;
        }

        public MaSanPham LayMaSanPham(String idMaSanPham)
        {
            MaSanPhanFactory f = new MaSanPhanFactory();
            DataTable tbl = f.LayMaSanPham(idMaSanPham);
            if (tbl.Rows.Count == 0)
                return null;

            return TaoDoiTuongLoNhap(tbl.Rows[0], new SanPhamController());
        }

        /// <summary>Extract Method: dung chung mot cach anh xa DataRow thanh doi tuong lo nhap.</summary>
        private static MaSanPham TaoDoiTuongLoNhap(DataRow row, SanPhamController ctrlSanPham)
        {
            MaSanPham lo = new MaSanPham();
            lo.Id = Convert.ToString(row["ID"]);
            lo.SoLuong = Convert.ToInt32(row["SO_LUONG"]);
            lo.GiaNhap = Convert.ToInt64(row["DON_GIA_NHAP"]);
            lo.NgayNhap = Convert.ToDateTime(row["NGAY_NHAP"]);
            lo.NgaySanXuat = Convert.ToDateTime(row["NGAY_SAN_XUAT"]);
            lo.NgayHetHan = Convert.ToDateTime(row["NGAY_HET_HAN"]);
            lo.SanPham = ctrlSanPham.LaySanPham(Convert.ToString(row["ID_SAN_PHAM"]));
            lo.ThanhTien = (long)lo.SoLuong * lo.GiaNhap;
            return lo;
        }

        public static IList<MaSanPham> LayMaSanPhamHetHan(DateTime dt)
        {
            IList<MaSanPham> ds = new List<MaSanPham>();
            DataTable tbl = new MaSanPhanFactory().DanhsachMaSanPhamHetHan(dt);
            SanPhamController ctrlSanPham = new SanPhamController();

            foreach (DataRow row in tbl.Rows)
                ds.Add(TaoDoiTuongLoNhap(row, ctrlSanPham));

            return ds;
        }

        public static DataTable LayTonKhoTheoKhoangNgay(DateTime tuNgay, DateTime denNgay)
        {
            return new MaSanPhanFactory().LayTonKhoTheoKhoangNgay(tuNgay, denNgay);
        }

        public void HienThiAutoComboBox(String sp, ComboBox cmb)
        {
            cmb.DataSource = factory.DanhsachMaSanPham(sp);
            cmb.DisplayMember = "ID";
            cmb.ValueMember = "ID";
        }

        public void HienThiDataGridViewComboBox(DataGridViewComboBoxColumn cmb)
        {
            cmb.DataSource = factory.DanhsachMaSanPham();
            cmb.DisplayMember = "ID";
            cmb.ValueMember = "ID";
            cmb.DataPropertyName = "ID_MA_SAN_PHAM";
            cmb.HeaderText = "Mã sản phẩm";
        }

        public void HienThiChiTietPhieuNhap(String id, DataGridView dg)
        {
            dg.DataSource = factory.DanhsachChiTiet(id);
        }

        public IList<MaSanPham> ChiTietPhieuNhap(String id)
        {
            IList<MaSanPham> ds = new List<MaSanPham>();
            DataTable tbl = factory.DanhsachChiTiet(id);
            SanPhamController ctrlSanPham = new SanPhamController();

            foreach (DataRow row in tbl.Rows)
                ds.Add(TaoDoiTuongLoNhap(row, ctrlSanPham));

            return ds;
        }
    }
}
