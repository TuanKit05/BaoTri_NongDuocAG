using System;
using System.Data;
using System.Data.OleDb;

namespace CuahangNongduoc.DataLayer
{
    /// <summary>
    /// Factory cho Lý do chi.
    /// Kế thừa BaseFactory → loại bỏ code trùng lặp.
    /// </summary>
    public class LyDoChiFactory : BaseFactory
    {
        protected override string TableName => "LY_DO_CHI";

        public DataTable DanhsachLyDo() => DanhSach();

        public DataTable LayLyDoChi(long id) => LayTheoId(id);
    }
}
