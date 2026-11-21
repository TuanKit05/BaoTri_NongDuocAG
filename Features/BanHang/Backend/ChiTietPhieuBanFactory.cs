using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.OleDb;

namespace CuahangNongduoc.DataLayer
{
    public class ChiTietPhieuBanFactory
    {
        DataService m_Ds = new DataService();



        public DataTable LayChiTietPhieuBan(String idPhieuBan)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM CHI_TIET_PHIEU_BAN WHERE ID_PHIEU_BAN = @id");
            cmd.Parameters.Add("id", OleDbType.VarChar, 50).Value = idPhieuBan;
            m_Ds.Load(cmd);
            return m_Ds;
        }

        public DataTable LayChiTietPhieuBan(DateTime dtNgayBan)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT CT.* FROM CHI_TIET_PHIEU_BAN CT INNER JOIN PHIEU_BAN PB ON CT.ID_PHIEU_BAN = PB.ID " +
                    " WHERE PB.NGAY_BAN = @ngayban");
            cmd.Parameters.Add("ngayban", OleDbType.Date).Value = dtNgayBan;
            m_Ds.Load(cmd);
            return m_Ds;
        }

        public DataTable LayChiTietPhieuBan(int thang, int nam)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT CT.* FROM CHI_TIET_PHIEU_BAN CT INNER JOIN PHIEU_BAN PB ON CT.ID_PHIEU_BAN = PB.ID " +
                    " WHERE MONTH(PB.NGAY_BAN) = @thang AND YEAR(PB.NGAY_BAN)= @nam");
            cmd.Parameters.Add("thang", OleDbType.Integer).Value = thang;
            cmd.Parameters.Add("nam", OleDbType.Integer).Value = nam;
            m_Ds.Load(cmd);
            return m_Ds;
        }



        public DataRow NewRow()
        {
            return m_Ds.NewRow();
        }
        public void Add(DataRow row)
        {
            m_Ds.Rows.Add(row);
        }

        public bool CoDongMoi()
        {
            foreach (DataRow row in m_Ds.Rows)
            {
                if (row.RowState == DataRowState.Added)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Ghi chi tiết và trừ tồn trong transaction riêng của hàm này.
        /// Nếu lỗi thì rollback và ném lại exception (không nuốt).
        /// </summary>
        public bool Save()
        {
            AccessControl.DemandCurrent("BanHang");
            if (!CoDongMoi())
                return true;

            using (OleDbConnection conn = new OleDbConnection(DataService.m_ConnectString))
            {
                conn.Open();
                using (OleDbTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        bool ok = SaveTrongGiaoDich(conn, tr);
                        tr.Commit();
                        ChapNhanThayDoi();
                        return ok;
                    }
                    catch
                    {
                        tr.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Ghi chi tiết và trừ tồn trong transaction do người gọi quản lý.
        /// Dùng khi phiếu bán và chi tiết phải lưu cùng một transaction.
        /// Gọi ChapNhanThayDoi() sau khi người gọi commit thành công.
        /// </summary>
        public bool SaveTrongGiaoDich(OleDbConnection conn, OleDbTransaction tr)
        {
            AccessControl.DemandCurrent("BanHang");
            if (!AccessControl.Can("SuaPhieuBan") && AccessControl.SaleTransaction != tr)
                throw new UnauthorizedAccessException("Chi tiết phiếu mới phải được lưu cùng đầu phiếu.");
            if (!CoDongMoi())
                return true;

            // 1. Trừ tồn cho từng dòng mới
            foreach (DataRow row in m_Ds.Rows)
            {
                if (row.RowState != DataRowState.Added)
                    continue;

                int quantity = Convert.ToInt32(row["SO_LUONG"]);
                if (quantity <= 0) throw new InvalidOperationException("Số lượng bán phải lớn hơn không.");
                if (!AccessControl.Can("XemTatCaPhieuBan"))
                {
                    object owner = AccountRepository.Scalar(conn, tr, "SELECT ID_NHAN_VIEN FROM PHIEU_BAN WHERE ID=?", Convert.ToString(row["ID_PHIEU_BAN"]));
                    if (!String.Equals(Convert.ToString(owner), AppSession.CurrentUser.Id, StringComparison.Ordinal))
                        throw new UnauthorizedAccessException("Không được ghi chi tiết phiếu của người khác.");
                }
                OleDbCommand cmdTon = new OleDbCommand("UPDATE MA_SAN_PHAM SET SO_LUONG = SO_LUONG + ? WHERE ID = ? AND SO_LUONG >= ? AND NGAY_HET_HAN >= ?", conn, tr);
                cmdTon.Parameters.Add("pQuantity", OleDbType.Integer).Value = -quantity;
                cmdTon.Parameters.Add("pLot", OleDbType.VarWChar, 50).Value = Convert.ToString(row["ID_MA_SAN_PHAM"]);
                cmdTon.Parameters.Add("pMinimum", OleDbType.Integer).Value = quantity;
                cmdTon.Parameters.Add("pDate", OleDbType.Date).Value = DateTime.Today;
                if (cmdTon.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException("Lô không đủ tồn hoặc đã hết hạn: " + Convert.ToString(row["ID_MA_SAN_PHAM"]));
            }

            // 2. Ghi các dòng chi tiết mới
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

            int soDong = adapter.Update(m_Ds);
            return soDong > 0;
        }

        /// <summary>
        /// Hoàn tồn cho từng dòng chi tiết của phiếu rồi xóa các dòng đó.
        /// Chạy trong transaction do người gọi quản lý (cùng transaction với việc xóa phiếu).
        /// </summary>
        public void XoaTheoPhieuTrongGiaoDich(OleDbConnection conn, OleDbTransaction tr, String idPhieuBan)
        {
            // Đọc trước, đóng reader, rồi mới chạy lệnh khác trên cùng connection
            AccessControl.DemandCurrent("XoaPhieu");
            List<KeyValuePair<String, int>> dong = new List<KeyValuePair<String, int>>();
            OleDbCommand cmdChon = new OleDbCommand("SELECT ID_MA_SAN_PHAM, SO_LUONG FROM CHI_TIET_PHIEU_BAN WHERE ID_PHIEU_BAN = @id", conn, tr);
            cmdChon.Parameters.Add("id", OleDbType.VarChar, 50).Value = idPhieuBan;
            using (OleDbDataReader rd = cmdChon.ExecuteReader())
            {
                while (rd.Read())
                    dong.Add(new KeyValuePair<String, int>(Convert.ToString(rd[0]), Convert.ToInt32(rd[1])));
            }

            // Hoàn tồn: ngược với lúc bán (bán thì trừ)
            foreach (KeyValuePair<String, int> ct in dong)
            {
                OleDbCommand cmdTon = new OleDbCommand("UPDATE MA_SAN_PHAM SET SO_LUONG = SO_LUONG + @so WHERE ID = @id", conn, tr);
                cmdTon.Parameters.Add("so", OleDbType.Integer).Value = ct.Value;
                cmdTon.Parameters.Add("id", OleDbType.VarChar, 50).Value = ct.Key;
                cmdTon.ExecuteNonQuery();
            }

            OleDbCommand cmdXoa = new OleDbCommand("DELETE FROM CHI_TIET_PHIEU_BAN WHERE ID_PHIEU_BAN = @id", conn, tr);
            cmdXoa.Parameters.Add("id", OleDbType.VarChar, 50).Value = idPhieuBan;
            cmdXoa.ExecuteNonQuery();
        }

        public void ChapNhanThayDoi()
        {
            m_Ds.AcceptChanges();
        }
    }
}