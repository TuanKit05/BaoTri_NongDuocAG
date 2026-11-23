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
            DataService receipt = new DataService();
            receipt.Load(cmd);
            return receipt;
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

        public DataRow NewRow()
        {
            DataRow row = m_Ds.NewRow();
            row["ID"] = IdGenerator.Next("PHIEU_CHI");
            row["NGAY_CHI"] = DateTime.Today;
            row["TONG_TIEN"] = 0m;
            row["GHI_CHU"] = String.Empty;
            return row;
        }
        public void Add(DataRow row) { m_Ds.Rows.Add(row); }
        public bool Save()
        {
            foreach (DataRow row in m_Ds.Rows)
            {
                if (row.RowState != DataRowState.Added && row.RowState != DataRowState.Modified) continue;
                string id = Convert.ToString(row["ID"]).Trim();
                if (row.RowState == DataRowState.Modified && !Object.Equals(row["ID", DataRowVersion.Original], row["ID"]))
                    throw new InvalidOperationException("Không được thay đổi mã phiếu chi.");
                if (id.Length == 0) throw new InvalidOperationException("Mã phiếu chi không được để trống.");
                if (row["NGAY_CHI"] == DBNull.Value)
                    throw new InvalidOperationException("Phiếu " + id + ": vui lòng nhập ngày chi.");
                if (row["ID_LY_DO_CHI"] == DBNull.Value || Convert.ToString(row["ID_LY_DO_CHI"]).Trim().Length == 0)
                    throw new InvalidOperationException("Phiếu " + id + ": vui lòng chọn lý do chi.");
                if (row["TONG_TIEN"] == DBNull.Value || Convert.ToDecimal(row["TONG_TIEN"]) <= 0m)
                    throw new InvalidOperationException("Phiếu " + id + ": tổng tiền phải lớn hơn 0.");
            }
            return m_Ds.ExecuteNoneQuery() > 0;
        }
    }
}
