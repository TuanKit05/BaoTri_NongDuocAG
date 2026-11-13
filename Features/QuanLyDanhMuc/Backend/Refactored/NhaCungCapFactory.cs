using System;
using System.Data;
using System.Data.OleDb;

namespace CuahangNongduoc.DataLayer
{
    /// <summary>
    /// Factory cho Nhà cung cấp.
    /// Kế thừa BaseFactory, chỉ giữ lại các method đặc thù (tìm kiếm).
    /// </summary>
    public class NhaCungCapFactory : BaseFactory
    {
        protected override string TableName => "NHA_CUNG_CAP";

        public DataTable DanhsachNCC() => DanhSach();

        public DataTable LayNCC(string id) => LayTheoId(id);

        public DataTable TimHoTen(string hoten) => TimKiem("HO_TEN", hoten);

        public DataTable TimDiaChi(string diachi) => TimKiem("DIA_CHI", diachi);
    }
}
