using System;
using System.Data;
using System.Data.OleDb;

namespace CuahangNongduoc.DataLayer
{
    public class PhieuChiFactory
    {
        private readonly DataService m_Ds = new DataService();

        public DataTable TimPhieuChi(int lyDo, DateTime ngay)
        {
            // OleDb binds ? by position, even when a parameter has a name.
            OleDbCommand cmd = new OleDbCommand(
                "SELECT * FROM [PHIEU_CHI] WHERE [ID_LY_DO_CHI]=? AND [NGAY_CHI]>=? AND [NGAY_CHI]<?");
            cmd.Parameters.Add("lydo", OleDbType.Integer).Value = lyDo;
            cmd.Parameters.Add("tuNgay", OleDbType.Date).Value = ngay.Date;
            cmd.Parameters.Add("denNgay", OleDbType.Date).Value = ngay.Date.AddDays(1);
            m_Ds.Load(cmd);
            return m_Ds;
        }

        public DataTable DanhsachPhieuChi()
        {
            m_Ds.Load(new OleDbCommand("SELECT * FROM [PHIEU_CHI]"));
            return m_Ds;
        }

        public DataTable LayPhieuChi(String id)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM [PHIEU_CHI] WHERE [ID]=?");
            cmd.Parameters.Add("id", OleDbType.VarChar, 50).Value = id;
            m_Ds.Load(cmd);
            return m_Ds;
        }

        public static long LayTongTien(String lyDo, int thang, int nam)
        {
            DateTime dauThang = new DateTime(nam, thang, 1);
            DateTime dauThangSau = dauThang.AddMonths(1);
            OleDbCommand cmd = new OleDbCommand(
                "SELECT SUM([TONG_TIEN]) FROM [PHIEU_CHI] " +
                "WHERE [ID_LY_DO_CHI]=? AND [NGAY_CHI]>=? AND [NGAY_CHI]<?");
            // ID_LY_DO_CHI is the numeric foreign key to LY_DO_CHI.ID.
            cmd.Parameters.Add("lydo", OleDbType.Integer).Value = Convert.ToInt32(lyDo);
            cmd.Parameters.Add("tuNgay", OleDbType.Date).Value = dauThang;
            cmd.Parameters.Add("denNgay", OleDbType.Date).Value = dauThangSau;
            return ThuChiScalarQuery.LaySoTien(cmd);
        }

        public DataRow NewRow() { return m_Ds.NewRow(); }
        public void Add(DataRow row) { m_Ds.Rows.Add(row); }
        public bool Save() { return m_Ds.ExecuteNoneQuery() > 0; }
    }
}
