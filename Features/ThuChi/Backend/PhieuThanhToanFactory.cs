using System;
using System.Data;
using System.Data.OleDb;

namespace CuahangNongduoc.DataLayer
{
    public class PhieuThanhToanFactory
    {
        private readonly DataService m_Ds = new DataService();

        public DataTable DanhsachPhieuThanhToan()
        {
            m_Ds.Load(new OleDbCommand("SELECT * FROM [PHIEU_THANH_TOAN]"));
            return m_Ds;
        }

        public DataTable TimPhieuThanhToan(String kh, DateTime ngay)
        {
            OleDbCommand cmd = new OleDbCommand(
                "SELECT * FROM [PHIEU_THANH_TOAN] WHERE [ID_KHACH_HANG]=? " +
                "AND [NGAY_THANH_TOAN]>=? AND [NGAY_THANH_TOAN]<?");
            cmd.Parameters.Add("kh", OleDbType.VarChar, 50).Value = kh;
            cmd.Parameters.Add("tuNgay", OleDbType.Date).Value = ngay.Date;
            cmd.Parameters.Add("denNgay", OleDbType.Date).Value = ngay.Date.AddDays(1);
            m_Ds.Load(cmd);
            return m_Ds;
        }

        public DataTable LayPhieuThanhToan(String id)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM [PHIEU_THANH_TOAN] WHERE [ID]=?");
            cmd.Parameters.Add("id", OleDbType.VarChar, 50).Value = id;
            m_Ds.Load(cmd);
            return m_Ds;
        }

        public static long LayTongTien(String kh, int thang, int nam)
        {
            DateTime dauThang = new DateTime(nam, thang, 1);
            DateTime dauThangSau = dauThang.AddMonths(1);
            OleDbCommand cmd = new OleDbCommand(
                "SELECT SUM([TONG_TIEN]) FROM [PHIEU_THANH_TOAN] " +
                "WHERE [ID_KHACH_HANG]=? AND [NGAY_THANH_TOAN]>=? AND [NGAY_THANH_TOAN]<?");
            cmd.Parameters.Add("kh", OleDbType.VarChar, 50).Value = kh;
            cmd.Parameters.Add("tuNgay", OleDbType.Date).Value = dauThang;
            cmd.Parameters.Add("denNgay", OleDbType.Date).Value = dauThangSau;
            return ThuChiScalarQuery.LaySoTien(cmd);
        }

        public DataRow NewRow() { return m_Ds.NewRow(); }
        public void Add(DataRow row) { m_Ds.Rows.Add(row); }
        public bool Save() { return m_Ds.ExecuteNoneQuery() > 0; }
    }
}
