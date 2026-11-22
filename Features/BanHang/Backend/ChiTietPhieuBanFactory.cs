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
            if (m_Ds.Columns.Contains("LA_HANG_TANG")) m_Ds.Columns["LA_HANG_TANG"].DefaultValue = false;
            return m_Ds;
        }

        public DataTable LayChiTietPhieuBan(DateTime dtNgayBan)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT CT.* FROM CHI_TIET_PHIEU_BAN CT INNER JOIN PHIEU_BAN PB ON CT.ID_PHIEU_BAN = PB.ID " +
                    " WHERE PB.NGAY_BAN = @ngayban");
            cmd.Parameters.Add("ngayban", OleDbType.Date).Value = dtNgayBan;
            m_Ds.Load(cmd);
            if (m_Ds.Columns.Contains("LA_HANG_TANG")) m_Ds.Columns["LA_HANG_TANG"].DefaultValue = false;
            return m_Ds;
        }

        public DataTable LayChiTietPhieuBan(int thang, int nam)
        {
            OleDbCommand cmd = new OleDbCommand("SELECT CT.* FROM CHI_TIET_PHIEU_BAN CT INNER JOIN PHIEU_BAN PB ON CT.ID_PHIEU_BAN = PB.ID " +
                    " WHERE MONTH(PB.NGAY_BAN) = @thang AND YEAR(PB.NGAY_BAN)= @nam");
            cmd.Parameters.Add("thang", OleDbType.Integer).Value = thang;
            cmd.Parameters.Add("nam", OleDbType.Integer).Value = nam;
            m_Ds.Load(cmd);
            if (m_Ds.Columns.Contains("LA_HANG_TANG")) m_Ds.Columns["LA_HANG_TANG"].DefaultValue = false;
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

        // Ghi sổ biến động kho (YC-BC-02) trong cùng transaction với việc đổi tồn.
        // soLuong dương = tăng tồn, âm = giảm tồn. Ngày ghi là ngày hôm nay.
        private static void GhiBienDong(OleDbConnection conn, OleDbTransaction tr, String maLo, String loai, int soLuong, String maPhieu)
        {
            OleDbCommand cmd = new OleDbCommand("INSERT INTO BIEN_DONG_KHO (NGAY, MA_LO, LOAI, SO_LUONG, MA_PHIEU, ID_TAI_KHOAN) VALUES (?, ?, ?, ?, ?, ?)", conn, tr);
            cmd.Parameters.Add("ngay", OleDbType.Date).Value = DateTime.Today;
            cmd.Parameters.Add("lo", OleDbType.VarWChar, 50).Value = maLo;
            cmd.Parameters.Add("loai", OleDbType.VarWChar, 20).Value = loai;
            cmd.Parameters.Add("so", OleDbType.Integer).Value = soLuong;
            cmd.Parameters.Add("phieu", OleDbType.VarWChar, 50).Value = maPhieu ?? "";
            cmd.Parameters.Add("tk", OleDbType.VarWChar, 50).Value = AppSession.CurrentUser.Id;
            cmd.ExecuteNonQuery();
        }

        // Trừ tồn một lô khi bán. Ném lỗi nếu lô không đủ tồn hoặc đã hết hạn.
        private static void TruTonTrongGiaoDich(OleDbConnection conn, OleDbTransaction tr, String maLo, int soLuong, String maPhieu)
        {
            OleDbCommand cmdTon = new OleDbCommand("UPDATE MA_SAN_PHAM SET SO_LUONG = SO_LUONG + ? WHERE ID = ? AND SO_LUONG >= ? AND NGAY_HET_HAN >= ?", conn, tr);
            cmdTon.Parameters.Add("pQuantity", OleDbType.Integer).Value = -soLuong;
            cmdTon.Parameters.Add("pLot", OleDbType.VarWChar, 50).Value = maLo;
            cmdTon.Parameters.Add("pMinimum", OleDbType.Integer).Value = soLuong;
            cmdTon.Parameters.Add("pDate", OleDbType.Date).Value = DateTime.Today;
            if (cmdTon.ExecuteNonQuery() != 1)
                throw new InvalidOperationException("Lô không đủ tồn hoặc đã hết hạn: " + maLo);
            GhiBienDong(conn, tr, maLo, "BAN", -soLuong, maPhieu);
        }

        // Cộng lại tồn một lô khi hoàn hàng (xóa dòng hoặc giảm số lượng). Ném lỗi nếu không cập nhật đúng một lô.
        private static void CongTonTrongGiaoDich(OleDbConnection conn, OleDbTransaction tr, String maLo, int soLuong, String maPhieu)
        {
            OleDbCommand cmdTon = new OleDbCommand("UPDATE MA_SAN_PHAM SET SO_LUONG = SO_LUONG + ? WHERE ID = ?", conn, tr);
            cmdTon.Parameters.Add("so", OleDbType.Integer).Value = soLuong;
            cmdTon.Parameters.Add("id", OleDbType.VarChar, 50).Value = maLo;
            if (cmdTon.ExecuteNonQuery() != 1)
                throw new InvalidOperationException("Không hoàn được tồn cho lô: " + maLo);
            GhiBienDong(conn, tr, maLo, "HOAN_HANG", soLuong, maPhieu);
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
            if (m_Ds.GetChanges() == null)
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
                        ErrorLog.Rollback(tr, "Rollback Access");
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
            if (m_Ds.GetChanges() == null)
                return true;

            Dictionary<string, long> changes = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            string costMethod = WarehouseSettings.ReadCostMethod(conn, tr);
            foreach (DataRow row in m_Ds.Rows)
            {
                if (row.RowState == DataRowState.Unchanged || row.RowState == DataRowState.Detached) continue;
                KiemTraQuyenDongPhieu(conn, tr, row, row.RowState == DataRowState.Deleted ? DataRowVersion.Original : DataRowVersion.Current);
                object savedCost = DBNull.Value;
                object savedMethod = DBNull.Value;
                if (row.RowState != DataRowState.Added)
                {
                    AccessControl.DemandCurrent("SuaPhieuBan");
                    string oldInvoice = Convert.ToString(row["ID_PHIEU_BAN", DataRowVersion.Original]);
                    string oldLot = Convert.ToString(row["ID_MA_SAN_PHAM", DataRowVersion.Original]);
                    int oldQuantity = Convert.ToInt32(row["SO_LUONG", DataRowVersion.Original]);
                    using (OleDbCommand original = new OleDbCommand(
                        "SELECT SO_LUONG,DON_GIA,THANH_TIEN,GIA_VON,PHUONG_PHAP_GIA FROM CHI_TIET_PHIEU_BAN WHERE ID_PHIEU_BAN=? AND ID_MA_SAN_PHAM=? AND LA_HANG_TANG=?", conn, tr))
                    {
                        original.Parameters.Add("invoice", OleDbType.VarWChar, 50).Value = oldInvoice;
                        original.Parameters.Add("lot", OleDbType.VarWChar, 50).Value = oldLot;
                        original.Parameters.Add("gift", OleDbType.Boolean).Value = row["LA_HANG_TANG", DataRowVersion.Original] != DBNull.Value && Convert.ToBoolean(row["LA_HANG_TANG", DataRowVersion.Original]);
                        using (OleDbDataReader reader = original.ExecuteReader())
                        {
                            if (!reader.Read() || Convert.ToInt32(reader[0]) != oldQuantity ||
                                Convert.ToDecimal(reader[1]) != Convert.ToDecimal(row["DON_GIA", DataRowVersion.Original]) ||
                                Convert.ToDecimal(reader[2]) != Convert.ToDecimal(row["THANH_TIEN", DataRowVersion.Original]))
                                throw new DBConcurrencyException("Dong phieu da duoc thay doi. Hay tai lai phieu.");
                            savedCost = reader[3];
                            savedMethod = reader[4];
                        }
                    }
                    AddStockChange(changes, oldLot, oldQuantity);
                }
                if (row.RowState == DataRowState.Deleted) continue;
                if (row.RowState == DataRowState.Modified && !Object.Equals(row["ID_PHIEU_BAN"], row["ID_PHIEU_BAN", DataRowVersion.Original]))
                    throw new InvalidOperationException("Khong duoc chuyen dong hang sang phieu khac.");
                if (row["SO_LUONG"] == DBNull.Value || Convert.ToDecimal(row["SO_LUONG"]) <= 0 ||
                    Convert.ToDecimal(row["SO_LUONG"]) != Decimal.Truncate(Convert.ToDecimal(row["SO_LUONG"])) ||
                    String.IsNullOrEmpty(Convert.ToString(row["ID_MA_SAN_PHAM"]).Trim()))
                    throw new InvalidOperationException("Ma lo va so luong nguyen duong la bat buoc.");
                string invoice = Convert.ToString(row["ID_PHIEU_BAN"]);
                if (Convert.ToInt32(AccountRepository.Scalar(conn, tr,
                    "SELECT COUNT(*) FROM PHIEU_BAN PB INNER JOIN KHACH_HANG KH ON PB.ID_KHACH_HANG=KH.ID WHERE PB.ID=?", invoice)) != 1)
                    throw new InvalidOperationException("Phieu ban phai co khach hang hop le.");
                if (!AccessControl.Can("XemTatCaPhieuBan"))
                {
                    object owner = AccountRepository.Scalar(conn, tr, "SELECT ID_NHAN_VIEN FROM PHIEU_BAN WHERE ID=?", invoice);
                    if (!String.Equals(Convert.ToString(owner), AppSession.CurrentUser.Id, StringComparison.Ordinal))
                        throw new UnauthorizedAccessException("Khong duoc sua phieu cua nguoi khac.");
                }
                if (row["LA_HANG_TANG"] != DBNull.Value && Convert.ToBoolean(row["LA_HANG_TANG"])) row["DON_GIA"] = 0m;
                row["THANH_TIEN"] = CuahangNongduoc.BusinessObject.TienTe.ThanhTien(
                    Convert.ToDecimal(row["DON_GIA"]), Convert.ToDecimal(row["SO_LUONG"]));
                bool newLot = row.RowState == DataRowState.Added ||
                    !Object.Equals(row["ID_MA_SAN_PHAM"], row["ID_MA_SAN_PHAM", DataRowVersion.Original]);
                row["GIA_VON"] = newLot ? (object)WarehouseCost.UnitCost(conn, tr, Convert.ToString(row["ID_MA_SAN_PHAM"]), costMethod) : savedCost;
                row["PHUONG_PHAP_GIA"] = newLot ? (object)costMethod : savedMethod;
                row["GIA_VON_DON_VI"] = row["GIA_VON"];
                row["THANH_TIEN_GIA_VON"] = row["GIA_VON"] == DBNull.Value ? (object)DBNull.Value : Convert.ToDecimal(row["GIA_VON"]) * Convert.ToInt32(row["SO_LUONG"]);
                AddStockChange(changes, Convert.ToString(row["ID_MA_SAN_PHAM"]), -Convert.ToInt32(row["SO_LUONG"]));
            }
            List<string> stockErrors = new List<string>();
            foreach (KeyValuePair<string, long> change in changes)
                if (change.Value < 0)
                {
                    string problem = StockProblem(conn, tr, change.Key, -change.Value);
                    if (problem != null) stockErrors.Add(problem);
                }
            if (stockErrors.Count > 0)
                throw new InvalidOperationException("Không thể lưu/thanh toán phiếu bán:\r\n" +
                    String.Join("\r\n", stockErrors.ToArray()) + "\r\nVui lòng giảm số lượng hoặc chọn lô khác.");

            foreach (DataRow ledgerRow in m_Ds.Rows)
            {
                if (ledgerRow.RowState == DataRowState.Unchanged || ledgerRow.RowState == DataRowState.Detached) continue;
                if (ledgerRow.HasVersion(DataRowVersion.Original))
                    GhiBienDong(conn, tr, Convert.ToString(ledgerRow["ID_MA_SAN_PHAM", DataRowVersion.Original]), "HOAN", Convert.ToInt32(ledgerRow["SO_LUONG", DataRowVersion.Original]), Convert.ToString(ledgerRow["ID_PHIEU_BAN", DataRowVersion.Original]));
                if (ledgerRow.RowState != DataRowState.Deleted)
                    GhiBienDong(conn, tr, Convert.ToString(ledgerRow["ID_MA_SAN_PHAM"]), "BAN", -Convert.ToInt32(ledgerRow["SO_LUONG"]), Convert.ToString(ledgerRow["ID_PHIEU_BAN"]));
            }
            foreach (KeyValuePair<string, long> change in changes)
            {
                if (change.Value == 0) continue;
                string sql = "UPDATE MA_SAN_PHAM SET SO_LUONG=SO_LUONG+? WHERE ID=?";
                if (change.Value < 0) sql += " AND SO_LUONG>=? AND NGAY_HET_HAN>=?";
                using (OleDbCommand stock = new OleDbCommand(sql, conn, tr))
                {
                    stock.Parameters.Add("delta", OleDbType.Integer).Value = checked((int)change.Value);
                    stock.Parameters.Add("lot", OleDbType.VarWChar, 50).Value = change.Key;
                    if (change.Value < 0)
                    {
                        stock.Parameters.Add("minimum", OleDbType.Integer).Value = checked((int)-change.Value);
                        stock.Parameters.Add("expiry", OleDbType.Date).Value = DateTime.Today;
                    }
                    if (stock.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException(change.Value < 0
                            ? (StockProblem(conn, tr, change.Key, -change.Value) ?? "Tồn kho của lô " + change.Key + " vừa thay đổi. Vui lòng kiểm tra lại phiếu.")
                            : "Không thể hoàn tồn cho lô: " + change.Key);
                }
            }
            OleDbCommand select = m_Ds.Command;
            select.Connection = conn;
            select.Transaction = tr;
            using (OleDbDataAdapter adapter = new OleDbDataAdapter(select))
            using (OleDbCommandBuilder builder = new OleDbCommandBuilder(adapter))
            {
                // The explicit original-row check above handles concurrency. Masked cost fields
                // in a seller's table must not become predicates against real persisted costs.
                builder.ConflictOption = ConflictOption.OverwriteChanges;
                adapter.AcceptChangesDuringUpdate = false;
                adapter.RowUpdating += delegate(object sender, OleDbRowUpdatingEventArgs e) {
                    if (e.Command == null) return;
                    e.Command.Connection = conn;
                    e.Command.Transaction = tr;
                };
                foreach (DataRowState state in new DataRowState[] { DataRowState.Deleted, DataRowState.Modified, DataRowState.Added })
                {
                    List<DataRow> rows = new List<DataRow>();
                    foreach (DataRow row in m_Ds.Rows) if (row.RowState == state) rows.Add(row);
                    if (rows.Count > 0) adapter.Update(rows.ToArray());
                }
            }
            return true;
        }

        private static string StockProblem(OleDbConnection connection, OleDbTransaction transaction, string lot, long required)
        {
            using (OleDbCommand command = new OleDbCommand(
                "SELECT SO_LUONG, NGAY_HET_HAN FROM MA_SAN_PHAM WHERE ID=?", connection, transaction))
            {
                command.Parameters.Add("lot", OleDbType.VarWChar, 50).Value = lot;
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return "Lô " + lot + ": không còn tồn tại.";
                    if (reader[1] == DBNull.Value || Convert.ToDateTime(reader[1]).Date < DateTime.Today)
                        return "Lô " + lot + ": đã hết hạn hoặc chưa có hạn dùng, không thể xuất.";
                    long available = reader[0] == DBNull.Value ? 0 : Convert.ToInt64(reader[0]);
                    if (required > available)
                        return "Lô " + lot + ": tồn hiện tại " + available + ", cần xuất thêm " + required +
                            ", thiếu " + (required - available) + ".";
                    return null;
                }
            }
        }

        private static void AddStockChange(Dictionary<string, long> changes, string lot, long quantity)
        {
            long previous;
            changes.TryGetValue(lot, out previous);
            changes[lot] = checked(previous + quantity);
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
                if (cmdTon.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException("Không thể hoàn tồn cho lô: " + ct.Key);
                GhiBienDong(conn, tr, ct.Key, "HOAN", ct.Value, idPhieuBan);
            }

            OleDbCommand cmdXoa = new OleDbCommand("DELETE FROM CHI_TIET_PHIEU_BAN WHERE ID_PHIEU_BAN = @id", conn, tr);
            cmdXoa.Parameters.Add("id", OleDbType.VarChar, 50).Value = idPhieuBan;
            cmdXoa.ExecuteNonQuery();
        }

        public void ChapNhanThayDoi()
        {
            m_Ds.AcceptChanges();
            AccessControl.HideCosts(m_Ds);
        }
    }
}
