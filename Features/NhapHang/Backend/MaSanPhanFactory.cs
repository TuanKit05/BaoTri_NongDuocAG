using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.OleDb;

namespace CuahangNongduoc.DataLayer
{
    public class MaSanPhanFactory
    {
        DataService m_Ds = new DataService();

        public void LoadSchema()
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM MA_SAN_PHAM WHERE ID = '-1'");
            m_Ds.Load(cmd);
        }

        public DataTable DanhsachMaSanPham(String sp)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM MA_SAN_PHAM WHERE ID_SAN_PHAM=@id AND SO_LUONG > 0");
            cmd.Parameters.Add("id", OleDbType.VarChar, 50).Value = sp;
            m_Ds.Load(cmd);

            return m_Ds;
        }
        public DataTable DanhsachChiTiet(String sp)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM MA_SAN_PHAM WHERE ID_PHIEU_NHAP=@id");
            cmd.Parameters.Add("id", OleDbType.VarChar, 50).Value = sp;
            m_Ds.Load(cmd);

            return m_Ds;
        }

        public DataTable LaySanPham(String idMaSanPham)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT SP.* FROM SAN_PHAM SP INNER JOIN MA_SAN_PHAM MSP ON SP.ID = MSP.ID_SAN_PHAM WHERE MSP.ID = @id");
            cmd.Parameters.Add("id", OleDbType.VarChar, 50).Value = idMaSanPham;
            m_Ds.Load(cmd);
            return m_Ds;
        }

        public DataTable LayMaSanPham(String idMaSanPham)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM MA_SAN_PHAM MSP WHERE MSP.ID = @id");
            cmd.Parameters.Add("id", OleDbType.VarChar, 50).Value = idMaSanPham;
            m_Ds.Load(cmd);
            return m_Ds;
        }

        public DataTable DanhsachMaSanPhamHetHan(DateTime dt)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM MA_SAN_PHAM WHERE SO_LUONG > 0 AND NGAY_HET_HAN <= @ngay");
            cmd.Parameters.Add("ngay", OleDbType.Date).Value = dt;
            m_Ds.Load(cmd);

            return m_Ds;
        }

        public DataTable DanhsachMaSanPham()
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM MA_SAN_PHAM WHERE SO_LUONG > 0");
            m_Ds.Load(cmd);

            return m_Ds;
        }

        /// <summary>
        /// Lấy chi tiết tồn kho theo khoảng ngày nhập (chỉ các lô còn số lượng > 0).
        /// </summary>
        public DataTable LayTonKhoTheoKhoangNgay(DateTime tuNgay, DateTime denNgay)
        {
            OleDbCommand cmd = new OleDbCommand(
                "SELECT MSP.ID AS MA_LO, SP.ID AS MA_SP, SP.TEN_SAN_PHAM, DVT.TEN_DON_VI, " +
                "MSP.SO_LUONG, MSP.DON_GIA_NHAP, (MSP.SO_LUONG * MSP.DON_GIA_NHAP) AS THANH_TIEN, " +
                "MSP.NGAY_NHAP, MSP.NGAY_SAN_XUAT, MSP.NGAY_HET_HAN " +
                "FROM ((MA_SAN_PHAM MSP INNER JOIN SAN_PHAM SP ON MSP.ID_SAN_PHAM = SP.ID) " +
                "INNER JOIN DON_VI_TINH DVT ON SP.ID_DON_VI_TINH = DVT.ID) " +
                "WHERE MSP.SO_LUONG > 0 AND MSP.NGAY_NHAP >= @tu AND MSP.NGAY_NHAP <= @den " +
                "ORDER BY SP.TEN_SAN_PHAM, MSP.NGAY_NHAP");
            cmd.Parameters.Add("tu", OleDbType.Date).Value = tuNgay.Date;
            cmd.Parameters.Add("den", OleDbType.Date).Value = denNgay.Date;
            m_Ds.Load(cmd);
            return m_Ds;
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
