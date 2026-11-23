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
            DataService receipt = new DataService();
            receipt.Load(cmd);
            return receipt;
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
        public bool CoPhieuMoi()
        {
            return m_Ds.GetChanges(DataRowState.Added) != null;
        }

        public bool SaveTrongGiaoDich(OleDbConnection conn, OleDbTransaction tr)
        {
            if (!AccessControl.Can("SuaPhieuBan") && m_Ds.GetChanges(DataRowState.Added) == null)
                throw new UnauthorizedAccessException("Chỉ quản lý được sửa phiếu đã lập.");
            AccessControl.ValidateRows(m_Ds, m_Ds.Command.CommandText);
            foreach (DataRow row in m_Ds.Rows)
            {
                if (row.RowState != DataRowState.Added && row.RowState != DataRowState.Modified) continue;
                string customerId = Convert.ToString(row["ID_KHACH_HANG"]).Trim();
                if (customerId.Length == 0 || Convert.ToInt32(AccountRepository.Scalar(conn, tr,
                    "SELECT COUNT(*) FROM KHACH_HANG WHERE ID=?", customerId)) != 1)
                    throw new InvalidOperationException("Vui long chon khach hang hoac dai ly hop le.");
            }

            OleDbCommand cmdSelect = m_Ds.Command;
            cmdSelect.Connection = conn;
            cmdSelect.Transaction = tr;

            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = cmdSelect;
            adapter.AcceptChangesDuringUpdate = false;
            OleDbCommandBuilder builder = new OleDbCommandBuilder(adapter);
            adapter.RowUpdating += delegate (object sender, OleDbRowUpdatingEventArgs e)
            {
                // CommandBuilder skips rows whose values match their original values.
                if (e.Command == null) return;
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

        // Ghi phí vận chuyển vào cột TIEN_VAN_CHUYEN của phiếu đang nằm trong bộ nhớ (trước khi lưu giao dịch).
        // Ném lỗi nếu không tìm thấy phiếu, để không lưu thiếu phí.
        // Ghi tiền giảm (tay + tự động) vào phiếu đang sửa, để báo cáo ưu đãi khớp với tổng đã lưu
        public void DatTienGiamGia(String id, long tienGiam)
        {
            foreach (DataRow row in m_Ds.Rows)
            {
                if (row.RowState == DataRowState.Deleted)
                    continue;
                if (String.Equals(Convert.ToString(row["ID"]), id, StringComparison.Ordinal))
                {
                    row["TIEN_GIAM_GIA"] = tienGiam;
                    return;
                }
            }
            throw new InvalidOperationException("Không tìm thấy phiếu " + id + " để ghi tiền giảm.");
        }

        public void DatTienVanChuyen(String id, long phiVanChuyen)
        {
            foreach (DataRow row in m_Ds.Rows)
            {
                if (row.RowState == DataRowState.Deleted)
                    continue;
                if (String.Equals(Convert.ToString(row["ID"]), id, StringComparison.Ordinal))
                {
                    row["TIEN_VAN_CHUYEN"] = phiVanChuyen;
                    return;
                }
            }
            throw new InvalidOperationException("Không tìm thấy phiếu " + id + " để ghi phí vận chuyển.");
        }

        public void HuyThayDoi() { m_Ds.RejectChanges(); }

        public void ChapNhanThayDoi()
        {
            m_Ds.AcceptChanges();
        }

        // Ưu đãi còn hiệu lực trong ngày: TU_NGAY <= ngày và (DEN_NGAY >= ngày hoặc chưa có DEN_NGAY)
        public static DataTable LayUuDaiHieuLuc(DateTime ngay)
        {
            DataService ds = new DataService();
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM UU_DAI WHERE TU_NGAY <= ? AND (DEN_NGAY >= ? OR DEN_NGAY IS NULL)");
            cmd.Parameters.Add("tuNgay", OleDbType.Date).Value = ngay.Date;
            cmd.Parameters.Add("denNgay", OleDbType.Date).Value = ngay.Date;
            ds.Load(cmd);
            return ds;
        }

        // Ghi dịch vụ của phiếu vào CHI_TIET_DICH_VU trong cùng giao dịch: xóa dòng cũ rồi ghi lại.
        // Dịch vụ phải có trong danh mục DICH_VU đang sử dụng; nếu không thì ném lỗi và giao dịch được rollback.
        public void LuuDichVuTrongGiaoDich(OleDbConnection conn, OleDbTransaction tr, String idPhieu,
            System.Collections.Generic.IList<CuahangNongduoc.BusinessObject.DichVuPhieuBan> dichVu)
        {
            XoaDichVuTrongGiaoDich(conn, tr, idPhieu);
            if (dichVu == null)
                return;
            foreach (CuahangNongduoc.BusinessObject.DichVuPhieuBan dv in dichVu)
            {
                String moTa = dv.Ten == null ? "" : dv.Ten.Trim();
                object maDichVu = TimMaDichVu(conn, tr, moTa);
                OleDbCommand cmd = new OleDbCommand(
                    "INSERT INTO CHI_TIET_DICH_VU (ID_PHIEU_BAN, ID_DICH_VU, MO_TA, SO_LUONG, DON_GIA, THANH_TIEN) VALUES (?, ?, ?, ?, ?, ?)",
                    conn, tr);
                cmd.Parameters.Add("idPhieu", OleDbType.VarChar, 50).Value = idPhieu;
                cmd.Parameters.AddWithValue("idDichVu", maDichVu);
                cmd.Parameters.Add("moTa", OleDbType.VarWChar, 255).Value = moTa;
                cmd.Parameters.Add("soLuong", OleDbType.Integer).Value = 1;
                cmd.Parameters.Add("donGia", OleDbType.Currency).Value = (decimal)dv.TienDV;
                cmd.Parameters.Add("thanhTien", OleDbType.Currency).Value = (decimal)dv.TienDV;
                cmd.ExecuteNonQuery();
            }
        }

        // Xóa toàn bộ dịch vụ của một phiếu trong giao dịch do người gọi quản lý
        public void XoaDichVuTrongGiaoDich(OleDbConnection conn, OleDbTransaction tr, String idPhieu)
        {
            OleDbCommand cmd = new OleDbCommand("DELETE FROM CHI_TIET_DICH_VU WHERE ID_PHIEU_BAN = ?", conn, tr);
            cmd.Parameters.Add("idPhieu", OleDbType.VarChar, 50).Value = idPhieu;
            cmd.ExecuteNonQuery();
        }

        // Tìm mã dịch vụ trong danh mục DICH_VU đang sử dụng theo tên
        static object TimMaDichVu(OleDbConnection conn, OleDbTransaction tr, String tenDichVu)
        {
            if (tenDichVu.Length == 0)
                throw new InvalidOperationException("Dịch vụ chưa có tên. Hãy nhập tên dịch vụ hoặc xóa dòng đó.");
            OleDbCommand cmd = new OleDbCommand("SELECT ID FROM DICH_VU WHERE TEN_DICH_VU = ? AND DANG_SU_DUNG = TRUE", conn, tr);
            cmd.Parameters.Add("ten", OleDbType.VarWChar, 255).Value = tenDichVu;
            object ma = cmd.ExecuteScalar();
            if (ma == null || ma == DBNull.Value)
                throw new InvalidOperationException("Dịch vụ \"" + tenDichVu + "\" chưa có trong danh mục dịch vụ đang sử dụng. Hãy thêm vào danh mục trước.");
            return ma;
        }
    }
}
