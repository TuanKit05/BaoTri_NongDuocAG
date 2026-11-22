using System;
using System.Collections.Generic;
using System.Data.OleDb;
using CuahangNongduoc.BusinessObject;
using CuahangNongduoc.Validation;

namespace CuahangNongduoc.DataLayer
{
    /// <summary>
    /// Xu ly ghi/xoa phieu nhap bang mot OleDbTransaction.
    /// Dau phieu, cac lo moi va gia nhap binh quan duoc luu cung mot giao dich.
    /// </summary>
    public class NhapHangService
    {
        public bool LuuPhieuNhap(
            string maPhieu,
            DateTime ngayNhap,
            long tongTien,
            string maNhaCungCap,
            long daTra,
            long conNo,
            bool laPhieuMoi,
            IList<LoNhapInput> cacLoMoi,
            out string thongBao)
        {
            thongBao = String.Empty;
            OleDbConnection connection = null;
            OleDbTransaction transaction = null;

            try
            {
                AccessControl.DemandCurrent("NhapHang");
                // Kiem tra lai tai tang nghiep vu/du lieu, khong chi phu thuoc giao dien.
                string loiPhieu = NhapHangValidator.KiemTraThongTinPhieuNhap(
                    maPhieu, maNhaCungCap, tongTien, daTra);
                if (!String.IsNullOrEmpty(loiPhieu))
                    throw new InvalidOperationException(loiPhieu);

                if (conNo != tongTien - daTra)
                    throw new InvalidOperationException("Công nợ không khớp với tổng tiền trừ số tiền đã trả.");

                if (cacLoMoi == null)
                    cacLoMoi = new List<LoNhapInput>();

                if (laPhieuMoi && cacLoMoi.Count == 0)
                    throw new InvalidOperationException("Phiếu nhập mới phải có ít nhất một lô hàng.");

                decimal tongTienCacLoMoi = 0;
                Dictionary<string, bool> maLoDaGap = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < cacLoMoi.Count; i++)
                {
                    LoNhapInput lo = cacLoMoi[i];
                    if (lo == null)
                        throw new InvalidOperationException("Danh sách lô nhập có dữ liệu trống.");

                    string loiLo = NhapHangValidator.KiemTraThongTinLoNhap(
                        lo.MaLo, lo.MaSanPham, lo.DonGiaNhap, lo.SoLuong,
                        lo.NgayNhap, lo.NgaySanXuat, lo.NgayHetHan);
                    if (!String.IsNullOrEmpty(loiLo))
                        throw new InvalidOperationException("Lô " + lo.MaLo + ": " + loiLo);

                    if (lo.NgayNhap.Date != ngayNhap.Date)
                        throw new InvalidOperationException("Ngày nhập của lô " + lo.MaLo + " phải trùng với ngày nhập trên phiếu.");

                    if (maLoDaGap.ContainsKey(lo.MaLo.Trim()))
                        throw new InvalidOperationException("Mã lô " + lo.MaLo + " bị lặp trong phiếu hiện tại.");
                    maLoDaGap.Add(lo.MaLo.Trim(), true);
                    tongTienCacLoMoi = checked(tongTienCacLoMoi + ((decimal)lo.DonGiaNhap * lo.SoLuong));
                }

                if (laPhieuMoi && tongTienCacLoMoi != tongTien)
                    throw new InvalidOperationException("Tổng tiền phiếu không khớp với tổng thành tiền các lô.");

                connection = new OleDbConnection(CuahangNongduoc.DataService.m_ConnectString);
                connection.Open();
                transaction = connection.BeginTransaction();

                KiemTraMaPhieu(connection, transaction, maPhieu, laPhieuMoi);
                if (!laPhieuMoi)
                {
                    using (OleDbCommand current = new OleDbCommand("SELECT TONG_TIEN FROM PHIEU_NHAP WHERE ID=?", connection, transaction))
                    {
                        current.Parameters.Add("id", OleDbType.VarWChar, 50).Value = maPhieu;
                        decimal previousTotal = Convert.ToDecimal(current.ExecuteScalar());
                        if (previousTotal + tongTienCacLoMoi != tongTien)
                            throw new InvalidOperationException("Tong tien phai bang tong phieu da luu cong cac lo moi.");
                    }
                }

                GhiThongTinPhieuNhap(connection, transaction, maPhieu, ngayNhap,
                    tongTien, maNhaCungCap, daTra, conNo, laPhieuMoi);

                Dictionary<string, TongGiaNhapTheoSanPham> tongHop =
                    TaoTongHopGiaNhap(cacLoMoi);

                for (int i = 0; i < cacLoMoi.Count; i++)
                {
                    KiemTraMaLo(connection, transaction, cacLoMoi[i].MaLo);
                    ThemLoNhap(connection, transaction, maPhieu, cacLoMoi[i]);
                }

                CapNhatGiaNhapBinhQuan(connection, transaction, tongHop);

                AccountRepository.Audit(connection, transaction, "NHAP_HANG", maPhieu, "Nhập hoặc cập nhật phiếu / lô hàng");
                transaction.Commit();
                thongBao = "Lưu phiếu nhập thành công.";
                return true;
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    ErrorLog.Rollback(transaction, "Hoàn tác phiếu nhập");
                }

                thongBao = "Không thể lưu phiếu nhập. Dữ liệu đã được hoàn tác. Chi tiết: " + ErrorLog.UserMessage(ex, "Không thể xử lý phiếu nhập");
                return false;
            }
            finally
            {
                if (transaction != null)
                    transaction.Dispose();
                if (connection != null)
                    connection.Dispose();
            }
        }

        /// <summary>
        /// Chi xoa phieu nhap khi chua thanh toan va khong co lo nao da duoc ban.
        /// Ton kho va gia nhap binh quan duoc tinh lai trong cung giao dich.
        /// </summary>
        public bool XoaPhieuNhap(string maPhieu, out string thongBao)
        {
            thongBao = String.Empty;
            OleDbConnection connection = null;
            OleDbTransaction transaction = null;

            try
            {
                AccessControl.DemandCurrent("XoaPhieu");
                connection = new OleDbConnection(CuahangNongduoc.DataService.m_ConnectString);
                connection.Open();
                transaction = connection.BeginTransaction();

                KiemTraCoTheXoaPhieu(connection, transaction, maPhieu);

                List<LoNhapCanXoa> cacLo = LayCacLoCuaPhieu(connection, transaction, maPhieu);
                List<string> maSanPham = LayDanhSachMaSanPham(cacLo);

                for (int i = 0; i < cacLo.Count; i++)
                {
                    if (KiemTraLoDaBan(connection, transaction, cacLo[i].MaLo))
                        throw new InvalidOperationException(
                            "Không thể xóa phiếu vì lô " + cacLo[i].MaLo + " đã phát sinh bán hàng.");
                }

                XoaChiTietLienKet(connection, transaction, maPhieu);
                XoaCacLo(connection, transaction, maPhieu);

                for (int i = 0; i < maSanPham.Count; i++)
                    TinhLaiTonVaGiaNhap(connection, transaction, maSanPham[i]);

                XoaDauPhieu(connection, transaction, maPhieu);

                AccountRepository.Audit(connection, transaction, "XOA_PHIEU_NHAP", maPhieu, "Hoàn tác phiếu nhập");
                transaction.Commit();
                thongBao = "Xóa phiếu nhập thành công.";
                return true;
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    ErrorLog.Rollback(transaction, "Hoàn tác phiếu nhập");
                }

                thongBao = "Không thể xóa phiếu nhập. " + ErrorLog.UserMessage(ex, "Không thể xử lý phiếu nhập");
                return false;
            }
            finally
            {
                if (transaction != null)
                    transaction.Dispose();
                if (connection != null)
                    connection.Dispose();
            }
        }

        private static void KiemTraMaPhieu(OleDbConnection connection,
            OleDbTransaction transaction, string maPhieu, bool laPhieuMoi)
        {
            OleDbCommand command = TaoLenh(connection, transaction,
                "SELECT COUNT(*) FROM PHIEU_NHAP WHERE ID = ?");
            command.Parameters.Add("p1", OleDbType.VarChar, 50).Value = maPhieu;
            int soDong = Convert.ToInt32(command.ExecuteScalar());

            if (laPhieuMoi && soDong > 0)
                throw new InvalidOperationException("Mã phiếu nhập đã tồn tại. Vui lòng tải lại mã phiếu.");

            if (!laPhieuMoi && soDong == 0)
                throw new InvalidOperationException("Không tìm thấy phiếu nhập cần cập nhật.");
        }

        private static void GhiThongTinPhieuNhap(OleDbConnection connection,
            OleDbTransaction transaction, string maPhieu, DateTime ngayNhap,
            long tongTien, string maNhaCungCap, long daTra, long conNo, bool laPhieuMoi)
        {
            string sql;
            if (laPhieuMoi)
            {
                sql = "INSERT INTO PHIEU_NHAP (ID, NGAY_NHAP, TONG_TIEN, ID_NHA_CUNG_CAP, DA_TRA, CON_NO) " +
                      "VALUES (?, ?, ?, ?, ?, ?)";
            }
            else
            {
                sql = "UPDATE PHIEU_NHAP SET NGAY_NHAP = ?, TONG_TIEN = ?, ID_NHA_CUNG_CAP = ?, " +
                      "DA_TRA = ?, CON_NO = ? WHERE ID = ?";
            }

            OleDbCommand command = TaoLenh(connection, transaction, sql);
            if (laPhieuMoi)
            {
                command.Parameters.Add("p1", OleDbType.VarChar, 50).Value = maPhieu;
                command.Parameters.Add("p2", OleDbType.Date).Value = ngayNhap.Date;
                command.Parameters.Add("p3", OleDbType.Currency).Value = tongTien;
                command.Parameters.Add("p4", OleDbType.VarChar, 50).Value = maNhaCungCap;
                command.Parameters.Add("p5", OleDbType.Currency).Value = daTra;
                command.Parameters.Add("p6", OleDbType.Currency).Value = conNo;
            }
            else
            {
                command.Parameters.Add("p1", OleDbType.Date).Value = ngayNhap.Date;
                command.Parameters.Add("p2", OleDbType.Currency).Value = tongTien;
                command.Parameters.Add("p3", OleDbType.VarChar, 50).Value = maNhaCungCap;
                command.Parameters.Add("p4", OleDbType.Currency).Value = daTra;
                command.Parameters.Add("p5", OleDbType.Currency).Value = conNo;
                command.Parameters.Add("p6", OleDbType.VarChar, 50).Value = maPhieu;
            }

            int affected = command.ExecuteNonQuery();
            if (affected <= 0)
                throw new InvalidOperationException("Không ghi được thông tin đầu phiếu nhập.");
        }

        private static void KiemTraMaLo(OleDbConnection connection,
            OleDbTransaction transaction, string maLo)
        {
            OleDbCommand command = TaoLenh(connection, transaction,
                "SELECT COUNT(*) FROM MA_SAN_PHAM WHERE ID = ?");
            command.Parameters.Add("p1", OleDbType.VarChar, 50).Value = maLo;
            if (Convert.ToInt32(command.ExecuteScalar()) > 0)
                throw new InvalidOperationException("Mã lô " + maLo + " đã tồn tại trong kho.");
        }

        private static void ThemLoNhap(OleDbConnection connection,
            OleDbTransaction transaction, string maPhieu, LoNhapInput lo)
        {
            OleDbCommand command = TaoLenh(connection, transaction,
                "INSERT INTO MA_SAN_PHAM (ID, ID_PHIEU_NHAP, ID_SAN_PHAM, DON_GIA_NHAP, SO_LUONG, " +
                "NGAY_NHAP, NGAY_SAN_XUAT, NGAY_HET_HAN, SO_LUONG_NHAP) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)");

            command.Parameters.Add("p1", OleDbType.VarChar, 50).Value = lo.MaLo;
            command.Parameters.Add("p2", OleDbType.VarChar, 50).Value = maPhieu;
            command.Parameters.Add("p3", OleDbType.VarChar, 50).Value = lo.MaSanPham;
            command.Parameters.Add("p4", OleDbType.Currency).Value = lo.DonGiaNhap;
            command.Parameters.Add("p5", OleDbType.Integer).Value = lo.SoLuong;
            command.Parameters.Add("p6", OleDbType.Date).Value = lo.NgayNhap.Date;
            command.Parameters.Add("p7", OleDbType.Date).Value = lo.NgaySanXuat.Date;
            command.Parameters.Add("p8", OleDbType.Date).Value = lo.NgayHetHan.Date;
            command.Parameters.Add("p9", OleDbType.Integer).Value = lo.SoLuong;

            if (command.ExecuteNonQuery() <= 0)
                throw new InvalidOperationException("Không thêm được lô " + lo.MaLo + ".");
        }

        private static Dictionary<string, TongGiaNhapTheoSanPham> TaoTongHopGiaNhap(
            IList<LoNhapInput> cacLo)
        {
            Dictionary<string, TongGiaNhapTheoSanPham> tongHop =
                new Dictionary<string, TongGiaNhapTheoSanPham>(StringComparer.OrdinalIgnoreCase);

            if (cacLo == null)
                return tongHop;

            for (int i = 0; i < cacLo.Count; i++)
            {
                LoNhapInput lo = cacLo[i];
                TongGiaNhapTheoSanPham thongTin;
                if (!tongHop.TryGetValue(lo.MaSanPham, out thongTin))
                {
                    thongTin = new TongGiaNhapTheoSanPham();
                    thongTin.SoLuongNhap = 0;
                    thongTin.TongGiaTriNhap = 0;
                    tongHop.Add(lo.MaSanPham, thongTin);
                }

                thongTin.SoLuongNhap += lo.SoLuong;
                thongTin.TongGiaTriNhap += (decimal)lo.DonGiaNhap * lo.SoLuong;
            }

            return tongHop;
        }

        private static void CapNhatGiaNhapBinhQuan(OleDbConnection connection,
            OleDbTransaction transaction, Dictionary<string, TongGiaNhapTheoSanPham> tongHop)
        {
            foreach (KeyValuePair<string, TongGiaNhapTheoSanPham> item in tongHop)
            {
                OleDbCommand select = TaoLenh(connection, transaction,
                    "SELECT DON_GIA_NHAP FROM SAN_PHAM WHERE ID = ?");
                select.Parameters.Add("p1", OleDbType.VarChar, 50).Value = item.Key;

                long soLuongCu = 0;
                long giaNhapCu;
                using (OleDbDataReader reader = select.ExecuteReader())
                {
                    if (!reader.Read())
                        throw new InvalidOperationException("Không tìm thấy sản phẩm " + item.Key + ".");
                    giaNhapCu = reader["DON_GIA_NHAP"] == DBNull.Value ? 0 : Convert.ToInt64(reader["DON_GIA_NHAP"]);
                }

                if (soLuongCu < 0 || giaNhapCu < 0)
                    throw new InvalidOperationException("Dữ liệu tồn/giá nhập hiện tại của sản phẩm " + item.Key + " không hợp lệ.");

                long soLuongNhap = item.Value.SoLuongNhap;
                using (OleDbCommand stock = TaoLenh(connection, transaction,
                    "SELECT SUM(SO_LUONG) FROM MA_SAN_PHAM WHERE ID_SAN_PHAM=?"))
                {
                    stock.Parameters.Add("product", OleDbType.VarWChar, 50).Value = item.Key;
                    object total = stock.ExecuteScalar();
                    soLuongCu = (total == DBNull.Value || total == null ? 0 : Convert.ToInt64(total)) - soLuongNhap;
                }

                long tongSoLuong = soLuongCu + soLuongNhap;
                if (tongSoLuong <= 0 || tongSoLuong > Int32.MaxValue)
                    throw new InvalidOperationException("Tổng số lượng tồn của sản phẩm " + item.Key + " vượt giới hạn lưu trữ.");

                decimal giaTriTonCu = (decimal)soLuongCu * giaNhapCu;
                decimal giaTriTonMoi = giaTriTonCu + item.Value.TongGiaTriNhap;
                long giaNhapBinhQuan = Convert.ToInt64(decimal.Round(
                    giaTriTonMoi / tongSoLuong, 0, MidpointRounding.AwayFromZero));

                OleDbCommand update = TaoLenh(connection, transaction,
                    "UPDATE SAN_PHAM SET DON_GIA_NHAP = ? WHERE ID = ?");
                update.Parameters.Add("p2", OleDbType.Currency).Value = giaNhapBinhQuan;
                update.Parameters.Add("p3", OleDbType.VarChar, 50).Value = item.Key;
                if (update.ExecuteNonQuery() <= 0)
                    throw new InvalidOperationException("Không cập nhật được tồn và giá nhập của sản phẩm " + item.Key + ".");
            }
        }

        private static void KiemTraCoTheXoaPhieu(OleDbConnection connection,
            OleDbTransaction transaction, string maPhieu)
        {
            OleDbCommand command = TaoLenh(connection, transaction,
                "SELECT DA_TRA FROM PHIEU_NHAP WHERE ID = ?");
            command.Parameters.Add("p1", OleDbType.VarChar, 50).Value = maPhieu;
            object daTra = command.ExecuteScalar();
            if (daTra == null || daTra == DBNull.Value)
                throw new InvalidOperationException("Không tìm thấy phiếu nhập cần xóa.");
            if (Convert.ToDecimal(daTra) > 0)
                throw new InvalidOperationException("Phiếu đã có thanh toán. Cần xử lý công nợ trước khi xóa.");
        }

        private static List<LoNhapCanXoa> LayCacLoCuaPhieu(OleDbConnection connection,
            OleDbTransaction transaction, string maPhieu)
        {
            List<LoNhapCanXoa> ds = new List<LoNhapCanXoa>();
            OleDbCommand command = TaoLenh(connection, transaction,
                "SELECT ID, ID_SAN_PHAM FROM MA_SAN_PHAM WHERE ID_PHIEU_NHAP = ?");
            command.Parameters.Add("p1", OleDbType.VarChar, 50).Value = maPhieu;

            using (OleDbDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    LoNhapCanXoa lo = new LoNhapCanXoa();
                    lo.MaLo = Convert.ToString(reader["ID"]);
                    lo.MaSanPham = Convert.ToString(reader["ID_SAN_PHAM"]);
                    ds.Add(lo);
                }
            }
            return ds;
        }

        private static List<string> LayDanhSachMaSanPham(List<LoNhapCanXoa> cacLo)
        {
            List<string> ds = new List<string>();
            for (int i = 0; i < cacLo.Count; i++)
            {
                if (!ds.Contains(cacLo[i].MaSanPham))
                    ds.Add(cacLo[i].MaSanPham);
            }
            return ds;
        }

        private static bool KiemTraLoDaBan(OleDbConnection connection,
            OleDbTransaction transaction, string maLo)
        {
            OleDbCommand command = TaoLenh(connection, transaction,
                "SELECT COUNT(*) FROM CHI_TIET_PHIEU_BAN WHERE ID_MA_SAN_PHAM = ?");
            command.Parameters.Add("p1", OleDbType.VarChar, 50).Value = maLo;
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        private static void XoaChiTietLienKet(OleDbConnection connection,
            OleDbTransaction transaction, string maPhieu)
        {
            // Older databases store import lines directly in MA_SAN_PHAM.
            // Delete the separate detail rows only when that optional table exists.
            using (System.Data.DataTable tables = connection.GetOleDbSchemaTable(
                OleDbSchemaGuid.Tables, new object[] { null, null, "CHI_TIET_PHIEU_NHAP", "TABLE" }))
            {
                if (tables == null || tables.Rows.Count == 0) return;
            }
            OleDbCommand command = TaoLenh(connection, transaction,
                "DELETE FROM CHI_TIET_PHIEU_NHAP WHERE ID_PHIEU_NHAP = ?");
            command.Parameters.Add("p1", OleDbType.VarChar, 50).Value = maPhieu;
            command.ExecuteNonQuery();
        }

        private static void XoaCacLo(OleDbConnection connection,
            OleDbTransaction transaction, string maPhieu)
        {
            OleDbCommand command = TaoLenh(connection, transaction,
                "DELETE FROM MA_SAN_PHAM WHERE ID_PHIEU_NHAP = ?");
            command.Parameters.Add("p1", OleDbType.VarChar, 50).Value = maPhieu;
            command.ExecuteNonQuery();
        }

        private static void TinhLaiTonVaGiaNhap(OleDbConnection connection,
            OleDbTransaction transaction, string maSanPham)
        {
            OleDbCommand select = TaoLenh(connection, transaction,
                "SELECT SUM(SO_LUONG) AS TONG_SO_LUONG, " +
                "SUM(SO_LUONG * DON_GIA_NHAP) AS TONG_GIA_TRI " +
                "FROM MA_SAN_PHAM WHERE ID_SAN_PHAM = ? AND SO_LUONG > 0");
            select.Parameters.Add("p1", OleDbType.VarChar, 50).Value = maSanPham;

            long soLuong = 0;
            decimal tongGiaTri = 0;
            using (OleDbDataReader reader = select.ExecuteReader())
            {
                if (reader.Read())
                {
                    if (reader["TONG_SO_LUONG"] != DBNull.Value)
                        soLuong = Convert.ToInt64(reader["TONG_SO_LUONG"]);
                    if (reader["TONG_GIA_TRI"] != DBNull.Value)
                        tongGiaTri = Convert.ToDecimal(reader["TONG_GIA_TRI"]);
                }
            }

            if (soLuong > Int32.MaxValue)
                throw new InvalidOperationException("Số lượng tồn còn lại của sản phẩm " + maSanPham + " vượt giới hạn lưu trữ.");

            long giaNhapBinhQuan = soLuong == 0 ? 0 : Convert.ToInt64(decimal.Round(
                tongGiaTri / soLuong, 0, MidpointRounding.AwayFromZero));

            OleDbCommand update = TaoLenh(connection, transaction,
                "UPDATE SAN_PHAM SET DON_GIA_NHAP = ? WHERE ID = ?");
            update.Parameters.Add("p2", OleDbType.Currency).Value = giaNhapBinhQuan;
            update.Parameters.Add("p3", OleDbType.VarChar, 50).Value = maSanPham;
            if (update.ExecuteNonQuery() <= 0)
                throw new InvalidOperationException("Không cập nhật lại được sản phẩm " + maSanPham + ".");
        }

        private static void XoaDauPhieu(OleDbConnection connection,
            OleDbTransaction transaction, string maPhieu)
        {
            OleDbCommand command = TaoLenh(connection, transaction,
                "DELETE FROM PHIEU_NHAP WHERE ID = ?");
            command.Parameters.Add("p1", OleDbType.VarChar, 50).Value = maPhieu;
            if (command.ExecuteNonQuery() <= 0)
                throw new InvalidOperationException("Không xóa được đầu phiếu nhập.");
        }

        private static OleDbCommand TaoLenh(OleDbConnection connection,
            OleDbTransaction transaction, string sql)
        {
            return new OleDbCommand(sql, connection, transaction);
        }

        private class TongGiaNhapTheoSanPham
        {
            public long SoLuongNhap;
            public decimal TongGiaTriNhap;
        }

        private class LoNhapCanXoa
        {
            public string MaLo;
            public string MaSanPham;
        }
    }
}
