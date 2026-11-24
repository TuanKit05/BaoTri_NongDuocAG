using System;
using System.Data;
using System.Data.OleDb;

namespace CuahangNongduoc.DataLayer
{
    public class DichVuFactory : BaseFactory
    {
        protected override string TableName => "DICH_VU";

        public static void DemandManage()
        {
            AccessControl.DemandCurrent("QuanTri");
            if (!AccountRoles.IsAdmin(AppSession.CurrentUser.Role))
                throw new UnauthorizedAccessException("Chỉ quản lý được thêm, sửa hoặc xóa dịch vụ.");
        }

        public string LuuDichVu(string id, string name, decimal price)
        {
            DemandManage();
            name = (name ?? "").Trim();
            if (name.Length == 0 || name.Length > 100 || price < 0 || price > 1000000000000m || price != Decimal.Truncate(price))
                throw new ArgumentException("Nhập tên dịch vụ (tối đa 100 ký tự) và đơn giá nguyên không âm, tối đa 1.000 tỷ đồng.");
            bool adding = String.IsNullOrEmpty(id);
            if (adding) id = IdGenerator.Next("DICH_VU");
            using (OleDbConnection c = new OleDbConnection(m_ConnectString))
            {
                c.Open();
                using (OleDbTransaction tr = c.BeginTransaction())
                {
                    try
                    {
                        if (Convert.ToInt32(AccountRepository.Scalar(c, tr,
                            "SELECT COUNT(*) FROM DICH_VU WHERE TEN_DICH_VU=? AND DANG_SU_DUNG=True AND ID<>?", name, id)) > 0)
                            throw new InvalidOperationException("Tên dịch vụ đang được sử dụng.");
                        if (adding) AccountRepository.Execute(c, tr,
                            "INSERT INTO DICH_VU (ID,TEN_DICH_VU,DON_GIA,DANG_SU_DUNG) VALUES (?,?,?,True)", id, name, price);
                        else if (AccountRepository.Execute(c, tr,
                            "UPDATE DICH_VU SET TEN_DICH_VU=?,DON_GIA=? WHERE ID=? AND DANG_SU_DUNG=True", name, price, id) != 1)
                            throw new InvalidOperationException("Dịch vụ đã bị xóa. Hãy tải lại danh sách.");
                        AccountRepository.Audit(c, tr, adding ? "THEM_DICH_VU" : "SUA_DICH_VU", id, name + "; " + price);
                        tr.Commit();
                    }
                    catch { tr.Rollback(); throw; }
                }
            }
            return id;
        }

        public void XoaDichVu(string id)
        {
            DemandManage();
            if (String.IsNullOrEmpty(id)) throw new ArgumentException("Chọn dịch vụ cần xóa.");
            using (OleDbConnection c = new OleDbConnection(m_ConnectString))
            {
                c.Open();
                using (OleDbTransaction tr = c.BeginTransaction())
                {
                    try
                    {
                        bool used = Convert.ToInt32(AccountRepository.Scalar(c, tr,
                            "SELECT COUNT(*) FROM CHI_TIET_DICH_VU WHERE ID_DICH_VU=?", id)) > 0;
                        string sql = used ? "UPDATE DICH_VU SET DANG_SU_DUNG=False WHERE ID=? AND DANG_SU_DUNG=True" :
                            "DELETE FROM DICH_VU WHERE ID=? AND DANG_SU_DUNG=True";
                        if (AccountRepository.Execute(c, tr, sql, id) != 1)
                            throw new InvalidOperationException("Dịch vụ không còn tồn tại trong danh sách.");
                        AccountRepository.Audit(c, tr, "XOA_DICH_VU", id,
                            used ? "Ngừng sử dụng; giữ dữ liệu các phiếu cũ." : "Xóa dịch vụ chưa sử dụng.");
                        tr.Commit();
                    }
                    catch { tr.Rollback(); throw; }
                }
            }
        }

        public override bool Save()
        {
            DemandManage();
            System.Collections.Generic.Dictionary<string, bool> names =
                new System.Collections.Generic.Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in Rows)
            {
                if (row.RowState == DataRowState.Deleted) continue;
                string name = Convert.ToString(row["TEN_DICH_VU"]).Trim();
                if (name.Length == 0 || name.Length > 100 || row["DON_GIA"] == DBNull.Value || Convert.ToDecimal(row["DON_GIA"]) < 0)
                    throw new InvalidOperationException("Dịch vụ cần tên (tối đa 100 ký tự) và đơn giá không âm.");
                bool active = row["DANG_SU_DUNG"] != DBNull.Value && Convert.ToBoolean(row["DANG_SU_DUNG"]);
                if (active && names.ContainsKey(name)) throw new InvalidOperationException("Tên dịch vụ đang dùng bị trùng: " + name);
                if (active) names.Add(name, true);
            }
            return base.Save();
        }

        // Danh mục đang sử dụng cho màn bán hàng.
        // Đọc trực tiếp, không dùng DataService.Load để không xóa dữ liệu đang giữ trong factory.
        public DataTable DanhSachDangSuDung()
        {
            DataTable bang = new DataTable();
            const string sql = "SELECT ID, TEN_DICH_VU, DON_GIA FROM DICH_VU " +
                               "WHERE DANG_SU_DUNG = TRUE ORDER BY TEN_DICH_VU";
            using (OleDbConnection conn = new OleDbConnection(m_ConnectString))
            using (OleDbDataAdapter adapter = new OleDbDataAdapter(sql, conn))
            {
                adapter.Fill(bang);
            }
            return bang;
        }

        // Thêm dịch vụ mới vào bảng đang giữ (chưa ghi xuống Access cho tới khi gọi Save).
        // Không dùng NewRow() của BaseFactory vì nó tải lại bảng và xóa các dòng chưa lưu.
        public DataRow ThemDichVu(string id, string tenDichVu, decimal donGia)
        {
            DemandManage();
            object[] values = new object[Columns.Count];
            values[Columns.IndexOf("ID")] = id;
            values[Columns.IndexOf("TEN_DICH_VU")] = tenDichVu;
            values[Columns.IndexOf("DON_GIA")] = donGia;
            values[Columns.IndexOf("DANG_SU_DUNG")] = true;
            return Rows.Add(values);
        }
    }
}
