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
            DataService receipt = new DataService();
            receipt.Load(cmd);
            return receipt;
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

        public DataRow NewRow()
        {
            DataRow row = m_Ds.NewRow();
            row["ID"] = IdGenerator.Next("PHIEU_THANH_TOAN");
            row["NGAY_THANH_TOAN"] = DateTime.Today;
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
                if (Convert.ToString(row["ID"]).Trim().Length == 0 || row["NGAY_THANH_TOAN"] == DBNull.Value ||
                    row["ID_KHACH_HANG"] == DBNull.Value || Convert.ToString(row["ID_KHACH_HANG"]).Trim().Length == 0)
                    throw new InvalidOperationException("Vui lòng nhập đầy đủ mã phiếu, ngày thu và khách hàng.");
                if (row["TONG_TIEN"] == DBNull.Value || Convert.ToDecimal(row["TONG_TIEN"]) <= 0)
                    throw new InvalidOperationException("Tổng tiền thu phải lớn hơn 0.");
                if (row.RowState == DataRowState.Modified && !Object.Equals(row["ID", DataRowVersion.Original], row["ID"]))
                    throw new InvalidOperationException("Không được thay đổi mã phiếu thu.");
            }
            return m_Ds.ExecuteNoneQuery() > 0;
        }
    }
}
