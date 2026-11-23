using System;
using System.Data;
using System.Data.OleDb;

namespace CuahangNongduoc.DataLayer
{
    public class DichVuFactory : BaseFactory
    {
        protected override string TableName => "DICH_VU";

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
            object[] values = new object[Columns.Count];
            values[Columns.IndexOf("ID")] = id;
            values[Columns.IndexOf("TEN_DICH_VU")] = tenDichVu;
            values[Columns.IndexOf("DON_GIA")] = donGia;
            values[Columns.IndexOf("DANG_SU_DUNG")] = true;
            return Rows.Add(values);
        }
    }
}