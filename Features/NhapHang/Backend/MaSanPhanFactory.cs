using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.OleDb;
using CuahangNongduoc.Features.BaoCao.Backend;

namespace CuahangNongduoc.DataLayer
{
    public class MaSanPhanFactory
    {
        DataService m_Ds = new DataService();

        private void LoadFresh(OleDbCommand command)
        {
            AccessControl.PrepareRead(command);
            OleDbConnectionStringBuilder settings = new OleDbConnectionStringBuilder(DataService.m_ConnectString);
            settings["OLE DB Services"] = -2;
            using (OleDbConnection connection = new OleDbConnection(settings.ConnectionString))
            {
                connection.Open();
                command.Connection = connection;
                if (m_Ds.Columns.Contains("SO_LUONG")) m_Ds.Columns["SO_LUONG"].ReadOnly = false;
                m_Ds.Clear();
                using (OleDbDataAdapter adapter = new OleDbDataAdapter(command)) adapter.Fill(m_Ds);
            }
            m_Ds.Command = command;
            AccessControl.HideCosts(m_Ds);
        }


        public void LoadSchema()
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM MA_SAN_PHAM WHERE ID = '-1'");
            LoadFresh(cmd);
        }

        public DataTable DanhsachMaSanPham(String sp)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM MA_SAN_PHAM WHERE ID_SAN_PHAM=@id AND SO_LUONG > 0 AND NGAY_HET_HAN>=@today ORDER BY NGAY_HET_HAN, NGAY_NHAP, ID");
            cmd.Parameters.Add("id", OleDbType.VarChar, 50).Value = sp;
            cmd.Parameters.Add("today", OleDbType.Date).Value = DateTime.Today;
            LoadFresh(cmd);

            return m_Ds;
        }
        public DataTable DanhsachChiTiet(String sp)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM MA_SAN_PHAM WHERE ID_PHIEU_NHAP=@id");
            cmd.Parameters.Add("id", OleDbType.VarChar, 50).Value = sp;
            LoadFresh(cmd);

            return m_Ds;
        }

        public DataTable LaySanPham(String idMaSanPham)
        {
            OleDbCommand cmd = new OleDbCommand(SanPhamFactory.ProductStockSelect + " WHERE SP.ID IN (SELECT ID_SAN_PHAM FROM MA_SAN_PHAM WHERE ID=@id)");
            cmd.Parameters.Add("id", OleDbType.VarChar, 50).Value = idMaSanPham;
            LoadFresh(cmd);
            m_Ds.Columns["SO_LUONG"].ReadOnly = true;
            return m_Ds;
        }

        public DataTable LayMaSanPham(String idMaSanPham)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM MA_SAN_PHAM MSP WHERE MSP.ID = @id");
            cmd.Parameters.Add("id", OleDbType.VarChar, 50).Value = idMaSanPham;
            LoadFresh(cmd);
            return m_Ds;
        }

        public DataTable DanhsachMaSanPhamHetHan(DateTime dt)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM MA_SAN_PHAM WHERE SO_LUONG > 0 AND NGAY_HET_HAN <= @ngay");
            cmd.Parameters.Add("ngay", OleDbType.Date).Value = dt;
            LoadFresh(cmd);

            return m_Ds;
        }

        public DataTable DanhsachMaSanPham()
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM MA_SAN_PHAM WHERE SO_LUONG > 0");
            LoadFresh(cmd);

            return m_Ds;
        }

        public DataTable TatCaLoChoPhieuBan()
        {
            LoadFresh(new OleDbCommand("SELECT * FROM MA_SAN_PHAM ORDER BY NGAY_HET_HAN, NGAY_NHAP, ID"));
            return m_Ds;
        }

        /// <summary>
        /// Lấy chi tiết tồn kho theo khoảng ngày nhập (chỉ các lô còn số lượng > 0).
        /// </summary>


        public DataTable LayTonKhoTheoKhoangNgay(
            DateTime tuNgay, DateTime denNgay)
        {
            KiemTraKhoangNgay(tuNgay, denNgay);

            // YC-BC-01: tồn cuối kỳ (tại denNgay) của mọi lô còn hàng, kể cả lô nhập trước tuNgay.
            // Tính theo sổ biến động kho (BIEN_DONG_KHO) thông qua TonKhoTaiNgayService.
            KetQuaTonKho kq = new TonKhoTaiNgayService().LayTonKhoTaiNgay(denNgay.Date);
            DataTable ketQua = TaoBangTonKho(kq);
            ketQua.ExtendedProperties["CanhBao"]=kq.CanhBao;
            ketQua.ExtendedProperties["DuDuLieu"]=kq.DuDuLieu;
            AccessControl.HideCosts(ketQua);
            return ketQua;
        }

        private static void KiemTraKhoangNgay(
            DateTime tuNgay, DateTime denNgay)
        {
            if (tuNgay.Date > denNgay.Date)
            {
                throw new ArgumentException(
                    "Từ ngày không được lớn hơn đến ngày.");
            }
        }

        // Chuyển kết quả tồn tại ngày sang dạng bảng mà frmThongKeTonKho đang hiển thị.
        private static DataTable TaoBangTonKho(KetQuaTonKho kq)
        {
            DataTable tbl = new DataTable();
            tbl.Columns.Add("MA_LO", typeof(string));
            tbl.Columns.Add("MA_SP", typeof(string));
            tbl.Columns.Add("TEN_SAN_PHAM", typeof(string));
            tbl.Columns.Add("TEN_DON_VI", typeof(string));
            tbl.Columns.Add("SO_LUONG", typeof(long));
            tbl.Columns.Add("DON_GIA_NHAP", typeof(decimal));
            tbl.Columns.Add("THANH_TIEN", typeof(decimal));
            tbl.Columns.Add("NGAY_NHAP", typeof(DateTime));
            tbl.Columns.Add("NGAY_SAN_XUAT", typeof(DateTime));
            tbl.Columns.Add("NGAY_HET_HAN", typeof(DateTime));

            foreach (TonKhoLo lo in kq.DanhSach)
            {
                DataRow r = tbl.NewRow();
                r["MA_LO"] = lo.MaLo;
                r["MA_SP"] = lo.MaSp;
                r["TEN_SAN_PHAM"] = lo.TenSanPham;
                r["TEN_DON_VI"] = lo.TenDonVi;
                r["SO_LUONG"] = lo.SoLuongTaiNgay;
                r["DON_GIA_NHAP"] = lo.DonGiaNhap;
                r["THANH_TIEN"] = lo.GiaTriTaiNgay;
                r["NGAY_NHAP"] = lo.NgayNhap;
                if (lo.NgaySanXuat.HasValue) r["NGAY_SAN_XUAT"] = lo.NgaySanXuat.Value;
                if (lo.NgayHetHan.HasValue) r["NGAY_HET_HAN"] = lo.NgayHetHan.Value;
                tbl.Rows.Add(r);
            }
            return tbl;
        }

        public static void CapNhatSoLuong(String masp, int so_luong)
        {
            DataService ds = new DataService();
            OleDbCommand cmd = new OleDbCommand("UPDATE MA_SAN_PHAM SET SO_LUONG = SO_LUONG + @so WHERE ID = @id");
            cmd.Parameters.Add("so", OleDbType.Integer).Value = so_luong;
            cmd.Parameters.Add("id", OleDbType.VarChar).Value = masp;
            ds.ExecuteNoneQuery(cmd);
        }

        public DataRow NewRow()
        {
            return m_Ds.NewRow();
        }
        public void Add(DataRow row)
        {
            m_Ds.Rows.Add(row);
        }
        public bool Save()
        {
            return m_Ds.ExecuteNoneQuery() > 0;
        }
    }
}