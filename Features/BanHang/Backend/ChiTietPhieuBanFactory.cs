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

        // Cảnh báo trước khi lưu (YC-KHO-07): so số lượng cần bán với tồn từng lô.
        // Trả về nội dung cảnh báo, hoặc null nếu mọi lô đều đủ tồn và còn hạn.
        public String KiemTraTonTruocKhiLuu()
        {
            Dictionary<String, int> canBan = new Dictionary<String, int>(StringComparer.Ordinal);
            foreach (DataRow row in m_Ds.Rows)
            {
                String lot;
                int so;
                if (row.RowState == DataRowState.Added)
                {
                    lot = Convert.ToString(row["ID_MA_SAN_PHAM"]);
                    so = Convert.ToInt32(row["SO_LUONG"]);
                }
                else if (row.RowState == DataRowState.Modified)
                {
                    lot = Convert.ToString(row["ID_MA_SAN_PHAM"]);
                    so = Convert.ToInt32(row["SO_LUONG"]) - Convert.ToInt32(row["SO_LUONG", DataRowVersion.Original]);
                    if (so <= 0) continue;
                }
                else
                {
                    continue;
                }
                int tong;
                canBan.TryGetValue(lot, out tong);
                canBan[lot] = tong + so;
            }

            StringBuilder canhBao = new StringBuilder();
            foreach (KeyValuePair<String, int> lo in canBan)
            {
                OleDbCommand cmd = new OleDbCommand("SELECT SO_LUONG, NGAY_HET_HAN FROM MA_SAN_PHAM WHERE ID = @id");
                cmd.Parameters.Add("id", OleDbType.VarWChar, 50).Value = lo.Key;
                DataService ds = new DataService();
                ds.Load(cmd);
                if (ds.Rows.Count == 0)
                {
                    canhBao.AppendLine("- Lô " + lo.Key + ": không tồn tại.");
                    continue;
                }
                int ton = Convert.ToInt32(ds.Rows[0]["SO_LUONG"]);
                DateTime hetHan = Convert.ToDateTime(ds.Rows[0]["NGAY_HET_HAN"]);
                if (hetHan < DateTime.Today)
                    canhBao.AppendLine("- Lô " + lo.Key + ": đã hết hạn " + hetHan.ToString("dd/MM/yyyy") + ".");
                else if (ton < lo.Value)
                    canhBao.AppendLine("- Lô " + lo.Key + ": cần bán " + lo.Value + ", chỉ còn " + ton + ".");
            }
            return canhBao.Length == 0 ? null : canhBao.ToString();
        }

        // Có dòng thêm, sửa hoặc xóa cần ghi xuống CSDL và đồng bộ tồn
        public bool CoThayDoi()
        {
            foreach (DataRow row in m_Ds.Rows)
            {
                if (row.RowState == DataRowState.Added ||
                    row.RowState == DataRowState.Modified ||
                    row.RowState == DataRowState.Deleted)
                    return true;
            }
            return false;
        }

        // Giá vốn cách 1 (YC-KHO-06): giá vốn đơn vị = giá nhập của lô đang bán, ghi trên từng dòng phiếu.
        // Lưu vào GIA_VON_DON_VI và THANH_TIEN_GIA_VON của CHI_TIET_PHIEU_BAN.
        private static void GanGiaVon(OleDbConnection conn, OleDbTransaction tr, DataRow row, String maLo, int soLuong)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT DON_GIA_NHAP FROM MA_SAN_PHAM WHERE ID = ?", conn, tr);
            cmd.Parameters.Add("id", OleDbType.VarWChar, 50).Value = maLo;
            object gia = cmd.ExecuteScalar();
            if (gia == null || gia == DBNull.Value)
                throw new InvalidOperationException("Không lấy được giá nhập của lô: " + maLo);
            decimal giaVonDonVi = Convert.ToDecimal(gia);
            row["GIA_VON_DON_VI"] = giaVonDonVi;
            row["THANH_TIEN_GIA_VON"] = giaVonDonVi * soLuong;
        }

        // Trừ tồn một lô khi bán. Ném lỗi nếu lô không đủ tồn hoặc đã hết hạn.
        private static void TruTonTrongGiaoDich(OleDbConnection conn, OleDbTransaction tr, String maLo, int soLuong)
        {
            OleDbCommand cmdTon = new OleDbCommand("UPDATE MA_SAN_PHAM SET SO_LUONG = SO_LUONG + ? WHERE ID = ? AND SO_LUONG >= ? AND NGAY_HET_HAN >= ?", conn, tr);
            cmdTon.Parameters.Add("pQuantity", OleDbType.Integer).Value = -soLuong;
            cmdTon.Parameters.Add("pLot", OleDbType.VarWChar, 50).Value = maLo;
            cmdTon.Parameters.Add("pMinimum", OleDbType.Integer).Value = soLuong;
            cmdTon.Parameters.Add("pDate", OleDbType.Date).Value = DateTime.Today;
            if (cmdTon.ExecuteNonQuery() != 1)
                throw new InvalidOperationException("Lô không đủ tồn hoặc đã hết hạn: " + maLo);
        }

        // Cộng lại tồn một lô khi hoàn hàng (xóa dòng hoặc giảm số lượng). Ném lỗi nếu không cập nhật đúng một lô.
        private static void CongTonTrongGiaoDich(OleDbConnection conn, OleDbTransaction tr, String maLo, int soLuong)
        {
            OleDbCommand cmdTon = new OleDbCommand("UPDATE MA_SAN_PHAM SET SO_LUONG = SO_LUONG + ? WHERE ID = ?", conn, tr);
            cmdTon.Parameters.Add("so", OleDbType.Integer).Value = soLuong;
            cmdTon.Parameters.Add("id", OleDbType.VarChar, 50).Value = maLo;
            if (cmdTon.ExecuteNonQuery() != 1)
                throw new InvalidOperationException("Không hoàn được tồn cho lô: " + maLo);
        }

        // Kiểm tra quyền sở hữu phiếu của dòng chi tiết (nhân viên chỉ ghi phiếu của mình)
        private static void KiemTraQuyenDongPhieu(OleDbConnection conn, OleDbTransaction tr, DataRow row, DataRowVersion phienBan)
        {
            if (AccessControl.Can("XemTatCaPhieuBan"))
                return;
            object owner = AccountRepository.Scalar(conn, tr, "SELECT ID_NHAN_VIEN FROM PHIEU_BAN WHERE ID=?", Convert.ToString(row["ID_PHIEU_BAN", phienBan]));
            if (!String.Equals(Convert.ToString(owner), AppSession.CurrentUser.Id, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("Không được ghi chi tiết phiếu của người khác.");
        }

        /// <summary>
        /// Ghi chi tiết và trừ tồn trong transaction riêng của hàm này.
        /// Nếu lỗi thì rollback và ném lại exception (không nuốt).
        /// </summary>
        public bool Save()
        {
            AccessControl.DemandCurrent("BanHang");
            if (!CoThayDoi())
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
            if (!CoThayDoi())
                return true;

            // 1. Đồng bộ tồn kho theo từng dòng, trong cùng giao dịch với việc ghi chi tiết
            foreach (DataRow row in m_Ds.Rows)
            {
                if (row.RowState == DataRowState.Added)
                {
                    // Dòng mới: trừ tồn theo số lượng bán
                    int quantity = Convert.ToInt32(row["SO_LUONG"]);
                    if (quantity <= 0) throw new InvalidOperationException("Số lượng bán phải lớn hơn không.");
                    KiemTraQuyenDongPhieu(conn, tr, row, DataRowVersion.Current);
                    GanGiaVon(conn, tr, row, Convert.ToString(row["ID_MA_SAN_PHAM"]), quantity);
                    TruTonTrongGiaoDich(conn, tr, Convert.ToString(row["ID_MA_SAN_PHAM"]), quantity);
                }
                else if (row.RowState == DataRowState.Modified)
                {
                    // Dòng đã lưu bị sửa số lượng: trừ hoặc cộng phần chênh lệch
                    String lotCu = Convert.ToString(row["ID_MA_SAN_PHAM", DataRowVersion.Original]);
                    String lotMoi = Convert.ToString(row["ID_MA_SAN_PHAM"]);
                    if (!String.Equals(lotCu, lotMoi, StringComparison.Ordinal))
                        throw new InvalidOperationException("Không đổi lô của dòng đã lưu. Hãy xóa dòng rồi thêm lại.");
                    int soCu = Convert.ToInt32(row["SO_LUONG", DataRowVersion.Original]);
                    int soMoi = Convert.ToInt32(row["SO_LUONG"]);
                    if (soMoi <= 0) throw new InvalidOperationException("Số lượng bán phải lớn hơn không.");
                    KiemTraQuyenDongPhieu(conn, tr, row, DataRowVersion.Current);
                    // Giữ giá vốn đơn vị đã ghi lúc bán; chỉ tính lại thành tiền giá vốn theo số lượng mới
                    if (row["GIA_VON_DON_VI"] == DBNull.Value)
                        GanGiaVon(conn, tr, row, lotMoi, soMoi);
                    else
                        row["THANH_TIEN_GIA_VON"] = Convert.ToDecimal(row["GIA_VON_DON_VI"]) * soMoi;
                    int chenhLech = soMoi - soCu;
                    if (chenhLech > 0)
                        TruTonTrongGiaoDich(conn, tr, lotMoi, chenhLech);
                    else if (chenhLech < 0)
                        CongTonTrongGiaoDich(conn, tr, lotMoi, -chenhLech);
                }
                else if (row.RowState == DataRowState.Deleted)
                {
                    // Dòng bị gỡ khỏi phiếu: hoàn lại số lượng đã trừ lúc lưu
                    String lot = Convert.ToString(row["ID_MA_SAN_PHAM", DataRowVersion.Original]);
                    int soCu = Convert.ToInt32(row["SO_LUONG", DataRowVersion.Original]);
                    KiemTraQuyenDongPhieu(conn, tr, row, DataRowVersion.Original);
                    CongTonTrongGiaoDich(conn, tr, lot, soCu);
                }
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
                CongTonTrongGiaoDich(conn, tr, ct.Key, ct.Value);
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