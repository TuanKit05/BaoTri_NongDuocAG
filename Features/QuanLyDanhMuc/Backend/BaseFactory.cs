using System.Data;
using System.Data.OleDb;

namespace CuahangNongduoc.DataLayer
{
    /// <summary>
    /// Lớp cơ sở (Base Class) cho tất cả các Factory trong tầng DataLayer.
    /// Kế thừa từ DataService để tái sử dụng các phương thức nạp và quản lý dữ liệu.
    /// </summary>
    public abstract class BaseFactory : DataService
    {
        /// <summary>
        /// Thuộc tính trừu tượng định nghĩa tên bảng trong cơ sở dữ liệu cho từng Factory con.
        /// </summary>
        protected abstract string TableName { get; }

        /// <summary>
        /// Lấy toàn bộ danh sách dữ liệu từ bảng tương ứng.
        /// </summary>
        public virtual DataTable DanhSach()
        {
            OleDbCommand cmd = new OleDbCommand($"SELECT * FROM {TableName}");
            Load(cmd);
            return this;
        }

        /// <summary>
        /// Lấy bản ghi theo mã định danh (ID).
        /// </summary>
        public virtual DataTable LayTheoId(object id)
        {
            OleDbCommand cmd = new OleDbCommand($"SELECT * FROM {TableName} WHERE ID = @id");
            cmd.Parameters.AddWithValue("@id", id);
            Load(cmd);
            return this;
        }

        /// <summary>
        /// Tìm kiếm bản ghi theo tên cột và từ khóa (LIKE '%từ khóa%').
        /// </summary>
        public virtual DataTable TimKiem(string columnName, string keyword)
        {
            OleDbCommand cmd = new OleDbCommand($"SELECT * FROM {TableName} WHERE {columnName} LIKE @keyword");
            cmd.Parameters.AddWithValue("@keyword", "%" + (keyword ?? string.Empty) + "%");
            Load(cmd);
            return this;
        }

        /// <summary>
        /// Tạo một dòng dữ liệu mới (New Row) cho DataTable.
        /// </summary>
        public virtual DataRow NewRow()
        {
            DataTable tbl = DanhSach();
            return tbl.NewRow();
        }

        /// <summary>
        /// Thêm dòng dữ liệu vào tiến trình xử lý.
        /// </summary>
        public virtual void Add(DataRow row)
        {
            // Thêm logic tùy thuộc vào DataService của dự án nếu cần
        }

        /// <summary>
        /// Lưu các thay đổi của bảng xuống cơ sở dữ liệu.
        /// Trả về true nếu không có thay đổi hoặc lưu thành công; false nếu lưu thất bại.
        /// Lưu ý: ExecuteNoneQuery() của DataService nuốt lỗi, nên false chỉ phản ánh
        /// trường hợp không có dòng nào được cập nhật.
        /// </summary>
        public virtual bool Save()
        {
            if (this.GetChanges() == null)
                return true;

            int soDong = ExecuteNoneQuery();
            if (soDong > 0)
            {
                this.AcceptChanges();
                return true;
            }
            return false;
        }
    }
}