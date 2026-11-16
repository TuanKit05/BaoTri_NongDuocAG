using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.OleDb;

namespace CuahangNongduoc.DataLayer
{
    public class DuNoKhachHangFactory
    {
        DataService m_Ds = new DataService();

        public void LoadSchema()
        {
            m_Ds.Load(new OleDbCommand("SELECT * FROM [DU_NO_KH] WHERE 1=0"));

        }

        public DataTable DanhsachDuNo(int thang, int nam)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM [DU_NO_KH] WHERE [THANG]=? AND [NAM]=?");
            cmd.Parameters.Add("thang", OleDbType.Integer).Value = thang;
            cmd.Parameters.Add("nam", OleDbType.Integer).Value = nam;

            m_Ds.Load(cmd);

            return m_Ds;
        }

        public DataTable LayDuNoKhachHang(String kh, int thang, int nam)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM [DU_NO_KH] WHERE [ID_KHACH_HANG]=? AND [THANG]=? AND [NAM]=?");
            cmd.Parameters.Add("kh", OleDbType.VarChar, 50).Value = kh;
            cmd.Parameters.Add("thang", OleDbType.Integer).Value = thang;
            cmd.Parameters.Add("nam", OleDbType.Integer).Value = nam;

            m_Ds.Load(cmd);

            return m_Ds;
        }

        public static long LayDuNo(String kh, int thang, int nam)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT [CUOI_KY] FROM [DU_NO_KH] WHERE [ID_KHACH_HANG]=? AND [THANG]=? AND [NAM]=?");
            cmd.Parameters.Add("kh", OleDbType.VarChar, 50).Value = kh;
            ThuChiScalarQuery.ThemThamSoThangNam(cmd, thang, nam);
            return ThuChiScalarQuery.LaySoTien(cmd);

        }

        public void Clear(int thang, int nam)
        {
            OleDbCommand cmd = new OleDbCommand("DELETE FROM [DU_NO_KH] WHERE [THANG]=? AND [NAM]=?");
            cmd.Parameters.Add("thang", OleDbType.Integer).Value = thang;
            cmd.Parameters.Add("nam", OleDbType.Integer).Value = nam;

            m_Ds.ExecuteNoneQuery(cmd);
        }

        public bool HasPendingChanges()
        {
            return m_Ds.GetChanges() != null;
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
