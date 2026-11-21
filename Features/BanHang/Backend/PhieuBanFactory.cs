using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.OleDb;

namespace CuahangNongduoc.DataLayer
{
    public class PhieuBanFactory
    {
        DataService m_Ds = new DataService();

        public DataTable TimPhieuBan(String idKh, DateTime dt)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM PHIEU_BAN WHERE NGAY_BAN = @ngay AND ID_KHACH_HANG=@kh");
            cmd.Parameters.Add("ngay", OleDbType.Date).Value = dt;
            cmd.Parameters.Add("kh", OleDbType.VarChar).Value = idKh;

            m_Ds.Load(cmd);

            return m_Ds;
        }

        public DataTable DanhsachPhieuBanLe()
        {
            OleDbCommand cmd = new OleDbCommand("SELECT PB.* FROM PHIEU_BAN PB INNER JOIN KHACH_HANG KH ON PB.ID_KHACH_HANG=KH.ID WHERE KH.LOAI_KH=FALSE");
            m_Ds.Load(cmd);

            return m_Ds;
        }
        public DataTable DanhsachPhieuBanSi()
        {
            OleDbCommand cmd = new OleDbCommand("SELECT PB.* FROM PHIEU_BAN PB INNER JOIN KHACH_HANG KH ON PB.ID_KHACH_HANG=KH.ID WHERE KH.LOAI_KH=TRUE");
            m_Ds.Load(cmd);

            return m_Ds;
        }


        public DataTable LayPhieuBan(String id)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM PHIEU_BAN WHERE ID = @id");
            cmd.Parameters.Add("id", OleDbType.VarChar, 50).Value = id;
            m_Ds.Load(cmd);
            return m_Ds;
        }


        public DataTable LayChiTietPhieuBan(String idPhieuBan)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM CHI_TIET_PHIEU_BAN WHERE ID_PHIEU_BAN = @id");
            cmd.Parameters.Add("id", OleDbType.VarChar, 50).Value = idPhieuBan;
            m_Ds.Load(cmd);
            return m_Ds;
        }

        public static long LayConNo(String kh, int thang, int nam)
        {
            DataService ds = new DataService();
            OleDbCommand cmd = new OleDbCommand("SELECT SUM(CON_NO) FROM PHIEU_BAN WHERE ID_KHACH_HANG = @kh AND MONTH(NGAY_BAN)=@thang AND YEAR(NGAY_BAN)= @nam");
            cmd.Parameters.Add("kh", OleDbType.VarChar, 50).Value = kh;
            cmd.Parameters.Add("thang", OleDbType.Integer).Value = thang;
            cmd.Parameters.Add("nam", OleDbType.Integer).Value = nam;

            object obj = ds.ExecuteScalar(cmd);
            if (obj == null)
                return 0;
            else
                return Convert.ToInt64(obj);
        }

        public static int LaySoPhieu()
        {
            DataService ds = new DataService();
            OleDbCommand cmd = new OleDbCommand("SELECT COUNT(*) FROM PHIEU_BAN");

            object obj = ds.ExecuteScalar(cmd);
            if (obj == null)
                return 0;
            else
                return Convert.ToInt32(obj);
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

        /// <summary>
        /// Ghi phiếu bán trong transaction do người gọi quản lý.
        /// Gọi ChapNhanThayDoi() sau khi người gọi commit thành công.
        /// </summary>
        public bool SaveTrongGiaoDich(OleDbConnection conn, OleDbTransaction tr)
        {
            if (!AccessControl.Can("SuaPhieuBan") && m_Ds.GetChanges(DataRowState.Added) == null)
                throw new UnauthorizedAccessException("Chỉ quản lý được sửa phiếu đã lập.");
            AccessControl.ValidateRows(m_Ds, m_Ds.Command.CommandText);
            OleDbCommand cmdSelect = m_Ds.Command;
            cmdSelect.Connection = conn;
            cmdSelect.Transaction = tr;

            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = cmdSelect;
            OleDbCommandBuilder builder = new OleDbCommandBuilder(adapter);
            adapter.RowUpdating += delegate (object sender, OleDbRowUpdatingEventArgs e)
            {
                e.Command.Connection = conn;
                e.Command.Transaction = tr;
            };

            string auditChanges = AccessControl.DescribeChanges(m_Ds);
            int changed = adapter.Update(m_Ds);
            if (changed > 0) AccountRepository.Audit(conn, tr, "LAP_SUA_PHIEU_BAN", "PHIEU_BAN", auditChanges);
            return changed > 0;
        }

        /// <summary>
        /// Xóa phiếu trong transaction do người gọi quản lý. Ném lỗi nếu không xóa đúng một dòng.
        /// </summary>
        public void XoaPhieuTrongGiaoDich(OleDbConnection conn, OleDbTransaction tr, String idPhieu)
        {
            AccessControl.DemandCurrent("XoaPhieu");
            AccountRepository.Audit(conn, tr, "XOA_PHIEU_BAN", idPhieu, "Hoàn tồn và xóa chi tiết cùng giao dịch");
            OleDbCommand cmd = new OleDbCommand("DELETE FROM PHIEU_BAN WHERE ID = @id", conn, tr);
            cmd.Parameters.Add("id", OleDbType.VarChar, 50).Value = idPhieu;
            if (cmd.ExecuteNonQuery() != 1)
                throw new InvalidOperationException("Không xóa được phiếu bán " + idPhieu);
        }

        public void ChapNhanThayDoi()
        {
            m_Ds.AcceptChanges();
        }
    }
}