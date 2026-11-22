using CuahangNongduoc.BusinessObject;
using CuahangNongduoc.DataLayer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace CuahangNongduoc.Controller
{
    public class SanPhamController : BaseController
    {
        private readonly SanPhamFactory factory = new SanPhamFactory();

        protected override BaseFactory Factory => factory;

        public void HienthiDataGridview(DataGridView dg, BindingNavigator bn,
            TextBox txtMa, TextBox txtTen, ComboBox cmbDVT,
            NumericUpDown numSL, NumericUpDown numGiaNhap, NumericUpDown numGiaBanSi, NumericUpDown numGiaBanLe)
        {

            BindingSource bs = new BindingSource();

            try
            {
                if (dg == null || bn == null)
                    throw new Exception("DataGridView hoặc BindingNavigator chưa được khởi tạo.");

                DataTable tbl = factory.DanhSach();

                if (tbl == null || tbl.Columns.Count == 0)
                    throw new Exception(
                        "Không đọc được cấu trúc bảng SAN_PHAM. " +
                        "Hãy kiểm tra kết nối Access và DataService.cs.");

                if (!tbl.Columns.Contains("ID") ||
                    !tbl.Columns.Contains("TEN_SAN_PHAM") ||
                    !tbl.Columns.Contains("ID_DON_VI_TINH"))
                    throw new Exception(
                        "Bảng SAN_PHAM thiếu các cột bắt buộc.");

                bs.DataSource = tbl;

                dg.AutoGenerateColumns = false;
                dg.DataSource = null;
                dg.DataSource = bs;

                bn.BindingSource = bs;
            }
            catch (Exception ex)
            {
                throw ErrorLog.Wrap(ex, "Không thể tải danh sách sản phẩm");
            }

            // Bind dữ liệu chi tiết lên các control nếu có
            if (txtMa != null) txtMa.DataBindings.Clear();
            if (txtMa != null) txtMa.DataBindings.Add("Text", bs, "ID", true);

            if (txtTen != null) txtTen.DataBindings.Clear();
            if (txtTen != null) txtTen.DataBindings.Add("Text", bs, "TEN_SAN_PHAM", true);

            if (cmbDVT != null) cmbDVT.DataBindings.Clear();
            if (cmbDVT != null) cmbDVT.DataBindings.Add("SelectedValue", bs, "ID_DON_VI_TINH", true);

            if (numSL != null) numSL.DataBindings.Clear();
            if (numSL != null) numSL.DataBindings.Add("Value", bs, "SO_LUONG", true);

            if (numGiaNhap != null) numGiaNhap.DataBindings.Clear();
            if (numGiaNhap != null) numGiaNhap.DataBindings.Add("Value", bs, "DON_GIA_NHAP", true);

            if (numGiaBanSi != null) numGiaBanSi.DataBindings.Clear();
            if (numGiaBanSi != null) numGiaBanSi.DataBindings.Add("Value", bs, "GIA_BAN_SI", true);

            if (numGiaBanLe != null) numGiaBanLe.DataBindings.Clear();
            if (numGiaBanLe != null) numGiaBanLe.DataBindings.Add("Value", bs, "GIA_BAN_LE", true);
        }

        public void HienthiAutoComboBox(ComboBox cmb)
        {
            DataTable tbl = factory.DanhsachSanPham();
            cmb.DataSource = tbl;
            cmb.DisplayMember = "TEN_SAN_PHAM";
            cmb.ValueMember = "ID";
            cmb.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cmb.AutoCompleteSource = AutoCompleteSource.ListItems;
        }

        public void HienthiDataGridViewComboBoxColumn(DataGridViewComboBoxColumn cmb)
        {
            cmb.DataSource = factory.DanhsachSanPham();
            cmb.DisplayMember = "TEN_SAN_PHAM";
            cmb.ValueMember = "ID";
            cmb.AutoComplete = true;
        }

        public void CapNhatGiaNhap(String id, long gia_moi, long so_luong)
        {
            DataTable tbl = factory.LaySanPham(id);
            if (tbl.Rows.Count > 0)
            {
                long tong_so = Convert.ToInt32(tbl.Rows[0]["SO_LUONG"]);
                long tong_gia = Convert.ToInt64(tbl.Rows[0]["DON_GIA_NHAP"]);
                if (tong_gia != gia_moi)
                {
                    long thanh_tien = gia_moi * so_luong + tong_gia * tong_so;
                    tong_so += so_luong;
                    tbl.Rows[0]["DON_GIA_NHAP"] = thanh_tien / tong_so;
                }
                factory.Save();
            }
        }

        public void TaiNapSanPham()
        {
            factory.DanhSach();
        }

        public void TimMaSanPham(string ma)
        {
            factory.TimMaSanPham(ma);
        }

        public void TimTenSanPham(string ten)
        {
            factory.TimTenSanPham(ten);
        }

        public override DataRow NewRow()
        {
            return factory.NewRow();
        }

        public static IList<SoLuongTon> LaySoLuongTon()
        {
            SanPhamFactory f = new SanPhamFactory();
            DataTable tbl = f.LaySoLuongTon();

            IList<SoLuongTon> ds = new List<SoLuongTon>();
            foreach (DataRow row in tbl.Rows)
            {
                SanPham sp = new SanPham();
                sp.Id = Convert.ToString(row["ID"]);
                sp.TenSanPham = Convert.ToString(row["TEN_SAN_PHAM"]);
                sp.IdDonViTinh = Convert.ToString(row["ID_DON_VI_TINH"]);
                sp.SoLuong = Convert.ToInt32(row["SO_LUONG"]);
                sp.DonGiaNhap = Convert.ToDecimal(row["DON_GIA_NHAP"]);
                sp.GiaBanLe = Convert.ToDecimal(row["GIA_BAN_LE"]);
                sp.GiaBanSi = Convert.ToDecimal(row["GIA_BAN_SI"]);

                SoLuongTon slt = new SoLuongTon();
                slt.SanPham = sp;
                slt.SoLuong = Convert.ToInt32(row["SO_LUONG_TON"]);
                ds.Add(slt);
            }
            return ds;
        }

        public SanPham LaySanPham(string id)
        {
            DataTable tbl = factory.LaySanPham(id);
            if (tbl.Rows.Count == 0)
                return null;

            DataRow row = tbl.Rows[0];
            SanPham sp = new SanPham();
            sp.Id = Convert.ToString(row["ID"]);
            sp.TenSanPham = Convert.ToString(row["TEN_SAN_PHAM"]);
            sp.IdDonViTinh = Convert.ToString(row["ID_DON_VI_TINH"]);
            sp.SoLuong = Convert.ToInt32(row["SO_LUONG"]);
            sp.DonGiaNhap = Convert.ToDecimal(row["DON_GIA_NHAP"]);
            sp.GiaBanLe = Convert.ToDecimal(row["GIA_BAN_LE"]);
            sp.GiaBanSi = Convert.ToDecimal(row["GIA_BAN_SI"]);
            return sp;
        }
    }
}