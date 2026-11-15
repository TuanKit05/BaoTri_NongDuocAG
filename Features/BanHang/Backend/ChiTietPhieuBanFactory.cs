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

        private DataTable TaiDuLieu(OleDbCommand cmd)
        {
            m_Ds.Load(cmd);
            return m_Ds;
        }

        public DataTable LayChiTietPhieuBan(String idPhieuBan)
        {
            OleDbCommand cmd = new OleDbCommand(
                "SELECT * FROM CHI_TIET_PHIEU_BAN WHERE ID_PHIEU_BAN = @id");

            cmd.Parameters.Add("id", OleDbType.VarChar, 50).Value =
                idPhieuBan;

            return TaiDuLieu(cmd);
        }

        public DataTable LayChiTietPhieuBan(DateTime dtNgayBan)
        {
            OleDbCommand cmd = new OleDbCommand(
                "SELECT CT.* FROM CHI_TIET_PHIEU_BAN CT " +
                "INNER JOIN PHIEU_BAN PB ON CT.ID_PHIEU_BAN = PB.ID " +
                " WHERE PB.NGAY_BAN = @ngayban");

            cmd.Parameters.Add("ngayban", OleDbType.Date).Value =
                dtNgayBan;

            return TaiDuLieu(cmd);
        }

        public DataTable LayChiTietPhieuBan(int thang, int nam)
        {
            OleDbCommand cmd = new OleDbCommand(
                "SELECT CT.* FROM CHI_TIET_PHIEU_BAN CT " +
                "INNER JOIN PHIEU_BAN PB ON CT.ID_PHIEU_BAN = PB.ID " +
                " WHERE MONTH(PB.NGAY_BAN) = @thang " +
                "AND YEAR(PB.NGAY_BAN)= @nam");

            cmd.Parameters.Add("thang", OleDbType.Integer).Value =
                thang;

            cmd.Parameters.Add("nam", OleDbType.Integer).Value =
                nam;

            return TaiDuLieu(cmd);
        }

        public DataRow NewRow()
        {
            return m_Ds.NewRow();
        }
        public void Add(DataRow row)
        {
            m_Ds.Rows.Add(row);
        }

        private void CapNhatSoLuongSanPhamMoi()
        {
            foreach (DataRow row in m_Ds.Rows)
            {
                if (row.RowState == DataRowState.Added)
                {
                    CuahangNongduoc.DataLayer.MaSanPhanFactory
                        .CapNhatSoLuong(
                            Convert.ToString(row["ID_MA_SAN_PHAM"]),
                            -Convert.ToInt32(row["SO_LUONG"]));
                }
            }
        }

        public bool Save()
        {
            CapNhatSoLuongSanPhamMoi();

            return m_Ds.ExecuteNoneQuery() > 0;
        }

    }
}
