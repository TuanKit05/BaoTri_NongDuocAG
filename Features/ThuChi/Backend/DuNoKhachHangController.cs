using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using CuahangNongduoc.BusinessObject;
using CuahangNongduoc.DataLayer;

namespace CuahangNongduoc.Controller
{
    public class DuNoKhachHangController
    {
        private readonly DuNoKhachHangFactory factory = new DuNoKhachHangFactory();

        // Build a draft in memory; Save commits all new/changed rows together.
        // Unlike Clear(), merely clicking TongHop no longer erases previously saved balances.
        public void TongHop(int thang, int nam, ToolStripProgressBar bar,
            DataGridView dg, BindingNavigator bn)
        {
            if (thang < 1 || thang > 12 || nam < 2000 || nam > 9999)
                throw new ArgumentOutOfRangeException("thang", "Kỳ tổng hợp không hợp lệ.");

            if (factory.HasPendingChanges())
                throw new InvalidOperationException(
                    "Kết quả tổng hợp trước chưa được lưu. Hãy lưu trước khi tổng hợp lại.");

            if (CuahangNongduoc.Controller.PhieuBanController.KyDaChot(thang, nam))
                throw new InvalidOperationException(
                    "Kỳ " + thang + "/" + nam + " đã chốt công nợ, không tổng hợp lại.");

            int thangTruoc = 0;
            int namTruoc = 0;
            ThamSo.PreMonth(ref thangTruoc, ref namTruoc, thang, nam);

            DataTable duNoKyNay = factory.DanhsachDuNo(thang, nam);
            Dictionary<String, DataRow> theoKhachHang = new Dictionary<String, DataRow>(
                StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in duNoKyNay.Rows)
            {
                String id = Convert.ToString(row["ID_KHACH_HANG"]);
                if (id.Length > 0 && !theoKhachHang.ContainsKey(id))
                    theoKhachHang.Add(id, row);
            }

            KhachHangFactory khachHang = new KhachHangFactory();
            DataTable danhSach = khachHang.DanhsachKhachHang();
            bar.Minimum = 0;
            bar.Maximum = danhSach.Rows.Count;
            bar.Value = 0;
            foreach (DataRow khRow in danhSach.Rows)
            {
                String maKhachHang = Convert.ToString(khRow["ID"]);
                DuNoKhachHang congNo = TinhCongNo(maKhachHang,
                    thangTruoc, namTruoc, thang, nam);

                DataRow row;
                if (!theoKhachHang.TryGetValue(maKhachHang, out row))
                {
                    row = factory.NewRow();
                    factory.Add(row);
                    theoKhachHang.Add(maKhachHang, row);
                }
                GhiDuNo(row, maKhachHang, thang, nam, congNo);
                bar.Value++;
            }

            BindingSource source = new BindingSource();
            source.DataSource = duNoKyNay;
            bn.BindingSource = source;
            dg.DataSource = source;
        }

        // Separate Query from Modifier: computing debt never edits the DataTable.
        private static DuNoKhachHang TinhCongNo(String maKhachHang,
            int thangTruoc, int namTruoc, int thang, int nam)
        {
            DuNoKhachHang result = new DuNoKhachHang();
            result.DauKy = DuNoKhachHangFactory.LayDuNo(maKhachHang, thangTruoc, namTruoc);
            result.PhatSinh = PhieuBanFactory.LayConNo(maKhachHang, thang, nam);
            result.DaTra = PhieuThanhToanFactory.LayTongTien(maKhachHang, thang, nam);
            result.CuoiKy = result.TinhCuoiKy();
            return result;
        }

        private static void GhiDuNo(DataRow row, String maKhachHang,
            int thang, int nam, DuNoKhachHang congNo)
        {
            row["ID_KHACH_HANG"] = maKhachHang;
            row["THANG"] = thang;
            row["NAM"] = nam;
            row["DAU_KY"] = congNo.DauKy;
            row["PHAT_SINH"] = congNo.PhatSinh;
            row["DA_TRA"] = congNo.DaTra;
            row["CUOI_KY"] = congNo.CuoiKy;
        }

        [Obsolete("Sử dụng TongHop thay cho Tonghop.")]
        public void Tonghop(int thang, int nam, ToolStripProgressBar bar,
            DataGridView dg, BindingNavigator bn)
        {
            TongHop(thang, nam, bar, dg, bn);
        }

        public bool Save() { return factory.Save(); }

        // Chốt kỳ công nợ: đánh dấu DA_CHOT cho mọi khách hàng của kỳ. Phải tổng hợp kỳ trước.
        // Sau khi chốt, không sửa hoặc xóa phiếu của kỳ và không tổng hợp lại.
        public int ChotKy(int thang, int nam)
        {
            if (thang < 1 || thang > 12 || nam < 2000 || nam > 9999)
                throw new ArgumentOutOfRangeException("thang", "Kỳ chốt không hợp lệ.");

            DataService ds = new DataService();
            System.Data.OleDb.OleDbCommand kiem = new System.Data.OleDb.OleDbCommand(
                "SELECT COUNT(*) FROM DU_NO_KH WHERE THANG = @t AND NAM = @n");
            kiem.Parameters.Add("t", System.Data.OleDb.OleDbType.Integer).Value = thang;
            kiem.Parameters.Add("n", System.Data.OleDb.OleDbType.Integer).Value = nam;
            object dem = ds.ExecuteScalar(kiem);
            if (dem == null || Convert.ToInt32(dem) == 0)
                throw new InvalidOperationException("Chưa tổng hợp công nợ kỳ " + thang + "/" + nam + ". Hãy tổng hợp trước khi chốt.");

            System.Data.OleDb.OleDbCommand cmd = new System.Data.OleDb.OleDbCommand(
                "UPDATE DU_NO_KH SET DA_CHOT = TRUE WHERE THANG = @t AND NAM = @n");
            cmd.Parameters.Add("t", System.Data.OleDb.OleDbType.Integer).Value = thang;
            cmd.Parameters.Add("n", System.Data.OleDb.OleDbType.Integer).Value = nam;
            return ds.ExecuteNoneQuery(cmd);
        }
    }
}
