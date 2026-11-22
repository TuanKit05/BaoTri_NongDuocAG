using System;
using System.Collections.Generic;
using System.Text;
using CuahangNongduoc.DataLayer;
using CuahangNongduoc.BusinessObject;
using System.Windows.Forms;
using System.Data;

namespace CuahangNongduoc.Controller
{
    public static class SaleInputValidation
    {
        public static string CheckPayment(decimal total, decimal paid)
        {
            if (total < 0) return "Tổng tiền không được âm.";
            if (paid < 0) return "Số tiền đã trả không được âm.";
            if (paid > total) return "Số tiền đã trả không được vượt tổng tiền.";
            return String.Empty;
        }

        public static void ValidatePaymentRow(DataRow row)
        {
            if (row["TONG_TIEN"] == DBNull.Value || row["DA_TRA"] == DBNull.Value || row["CON_NO"] == DBNull.Value)
                throw new InvalidOperationException("Tổng tiền, đã trả và còn nợ không được để trống.");
            decimal total = Convert.ToDecimal(row["TONG_TIEN"]);
            decimal paid = Convert.ToDecimal(row["DA_TRA"]);
            string error = CheckPayment(total, paid);
            if (error.Length > 0) throw new InvalidOperationException(error);
            if (Convert.ToDecimal(row["CON_NO"]) != total - paid)
                throw new InvalidOperationException("Còn nợ phải bằng tổng tiền trừ số tiền đã trả và không được âm.");
        }

        public static string Selection(ComboBox combo)
        {
            return combo.SelectedIndex < 0 || combo.SelectedValue == null || combo.SelectedValue == DBNull.Value ||
                combo.SelectedValue is DataRowView ? String.Empty : Convert.ToString(combo.SelectedValue).Trim();
        }

        public static string CheckHeader(ComboBox customer, DataGridView details)
        {
            if (Selection(customer).Length == 0) return "Vui lòng chọn khách hàng / đại lý.";
            int count = 0;
            foreach (DataGridViewRow view in details.Rows)
            {
                if (view.IsNewRow) continue;
                DataRowView item = view.DataBoundItem as DataRowView;
                if (item == null) continue;
                if (String.IsNullOrEmpty(Convert.ToString(item["ID_MA_SAN_PHAM"]).Trim())) return "Vui lòng chọn mã lô.";
                if (item["SO_LUONG"] == DBNull.Value || Convert.ToDecimal(item["SO_LUONG"]) <= 0 ||
                    Convert.ToDecimal(item["SO_LUONG"]) != Decimal.Truncate(Convert.ToDecimal(item["SO_LUONG"])))
                    return "Số lượng phải là số nguyên lớn hơn 0.";
                count++;
            }
            return count == 0 ? "Phiếu bán phải có ít nhất một dòng hàng." : String.Empty;
        }

        public static string CheckLine(ComboBox customer, ComboBox product, ComboBox lot, decimal quantity, DataGridView details)
        {
            if (Selection(customer).Length == 0) return "Vui lòng chọn khách hàng / đại lý.";
            if (Selection(product).Length == 0) return "Vui lòng chọn sản phẩm.";
            string id = Selection(lot);
            if (id.Length == 0) return "Vui lòng chọn mã lô.";
            if (quantity <= 0 || quantity != Decimal.Truncate(quantity) || quantity > Int32.MaxValue)
                return "Số lượng phải là số nguyên lớn hơn 0.";
            DataService data = new DataService();
            System.Data.OleDb.OleDbCommand command = new System.Data.OleDb.OleDbCommand(
                "SELECT ID_SAN_PHAM, SO_LUONG, NGAY_HET_HAN FROM MA_SAN_PHAM WHERE ID=?");
            command.Parameters.Add("id", System.Data.OleDb.OleDbType.VarWChar, 50).Value = id;
            data.Load(command);
            if (data.Rows.Count != 1 || Convert.ToString(data.Rows[0]["ID_SAN_PHAM"]) != Selection(product))
                return "Lô không tồn tại hoặc không thuộc sản phẩm đã chọn.";
            if (data.Rows[0]["NGAY_HET_HAN"] == DBNull.Value || Convert.ToDateTime(data.Rows[0]["NGAY_HET_HAN"]).Date < DateTime.Today)
                return "Lô đã hết hạn hoặc chưa có hạn dùng.";
            decimal requested = quantity;
            BindingSource source = details.DataSource as BindingSource;
            DataTable pending = source == null ? details.DataSource as DataTable : source.DataSource as DataTable;
            if (pending != null)
            {
                foreach (DataRow row in pending.Rows)
                {
                    if (row.RowState == DataRowState.Unchanged || row.RowState == DataRowState.Detached) continue;
                    if (row.HasVersion(DataRowVersion.Original) && Convert.ToString(row["ID_MA_SAN_PHAM", DataRowVersion.Original]) == id)
                        requested -= Convert.ToDecimal(row["SO_LUONG", DataRowVersion.Original]);
                    if (row.RowState != DataRowState.Deleted && Convert.ToString(row["ID_MA_SAN_PHAM"]) == id)
                        requested += Convert.ToDecimal(row["SO_LUONG"]);
                }
            }
            return data.Rows[0]["SO_LUONG"] == DBNull.Value || requested > Convert.ToDecimal(data.Rows[0]["SO_LUONG"])
                ? "Tổng số lượng của lô vượt tồn kho." : String.Empty;
        }
    }

    public class ChiTietPhieuBanController
    {
        ChiTietPhieuBanFactory factory = new ChiTietPhieuBanFactory();

        public bool ThemTuDong(string customer, string product, string invoice, decimal quantity, decimal price, bool gift = false)
        {
            if (WarehouseSettings.Load() != WarehouseSettings.FirstInFirstOut) return false;
            if (String.IsNullOrEmpty(customer)) throw new InvalidOperationException("Vui lòng chọn khách hàng / đại lý.");
            if (String.IsNullOrEmpty(product)) throw new InvalidOperationException("Vui lòng chọn sản phẩm.");
            AccessControl.DemandCurrent("BanHang");
            DataTable lots;
            using (System.Data.OleDb.OleDbConnection connection = new System.Data.OleDb.OleDbConnection(DataService.m_ConnectString))
            {
                connection.Open();
                lots = AccountRepository.ReadTable(connection, null,
                    "SELECT ID, SO_LUONG, NGAY_HET_HAN, NGAY_NHAP FROM MA_SAN_PHAM WHERE ID_SAN_PHAM=? AND NGAY_HET_HAN>=? ORDER BY NGAY_HET_HAN, NGAY_NHAP, ID",
                    product, DateTime.Today);
            }
            LotAllocation.Add(lots, factory.NewRow().Table, invoice, quantity, gift ? 0m : price, gift);
            return true;
        }



        public void HienThiChiTiet(DataGridView dgv, String idPhieuBan)
        {
            BindingSource bs = new BindingSource();
            bs.DataSource = factory.LayChiTietPhieuBan(idPhieuBan);
            dgv.DataSource = bs;
        }
        public DataRow NewRow()
        {
            return factory.NewRow();
        }
        public void Add(DataRow row)
        {
            factory.Add(row);
        }
        public void Save()
        {
            factory.Save();
        }

        public bool CoDongMoi()
        {
            return factory.CoDongMoi();
        }

        public bool SaveTrongGiaoDich(System.Data.OleDb.OleDbConnection conn, System.Data.OleDb.OleDbTransaction tr)
        {
            return factory.SaveTrongGiaoDich(conn, tr);
        }

        public void XoaTheoPhieuTrongGiaoDich(System.Data.OleDb.OleDbConnection conn, System.Data.OleDb.OleDbTransaction tr, String idPhieuBan)
        {
            factory.XoaTheoPhieuTrongGiaoDich(conn, tr, idPhieuBan);
        }

        public void ChapNhanThayDoi()
        {
            factory.ChapNhanThayDoi();
        }

        // Cảnh báo tồn kho trước khi lưu (YC-KHO-07). Null nếu đủ tồn.
        public String KiemTraTonTruocKhiLuu()
        {
            return factory.KiemTraTonTruocKhiLuu();
        }


        private static void GanGiaVonDaLuu(ChiTietPhieuBan detail, DataRow row)
        {
            detail.GiaVon = AccessControl.Can("XemGiaVon") && AccessControl.Can("XemGiaNhap") &&
                row.Table.Columns.Contains("GIA_VON") && row["GIA_VON"] != DBNull.Value
                ? (decimal?)Convert.ToDecimal(row["GIA_VON"]) : null;
            detail.PhuongPhapGia = row.Table.Columns.Contains("PHUONG_PHAP_GIA") ? Convert.ToString(row["PHUONG_PHAP_GIA"]) : "";
        }

        public IList<ChiTietPhieuBan> ChiTietPhieuBan(String idPhieuBan)
        {
            IList<ChiTietPhieuBan> ds = new List<ChiTietPhieuBan>();

            DataTable tbl = factory.LayChiTietPhieuBan(idPhieuBan);
            foreach (DataRow row in tbl.Rows)
            {
                MaSanPhamController ctrl = new MaSanPhamController();
                ChiTietPhieuBan ct = new ChiTietPhieuBan();
                ct.DonGia = Convert.ToInt64(row["DON_GIA"]);
                ct.SoLuong = Convert.ToInt32(row["SO_LUONG"]);
                ct.ThanhTien = Convert.ToInt64(row["THANH_TIEN"]);
                GanGiaVonDaLuu(ct, row);
                ct.MaSanPham = ctrl.LayMaSanPham(Convert.ToString(row["ID_MA_SAN_PHAM"]));

                ds.Add(ct);
            }
            return ds;
        }

        public IList<ChiTietPhieuBan> ChiTietPhieuBan(DateTime dtNgayBan)
        {
            IList<ChiTietPhieuBan> ds = new List<ChiTietPhieuBan>();

            DataTable tbl = factory.LayChiTietPhieuBan(dtNgayBan);
            foreach (DataRow row in tbl.Rows)
            {
                MaSanPhamController ctrl = new MaSanPhamController();
                ChiTietPhieuBan ct = new ChiTietPhieuBan();
                ct.DonGia = Convert.ToInt64(row["DON_GIA"]);
                ct.SoLuong = Convert.ToInt32(row["SO_LUONG"]);
                ct.ThanhTien = Convert.ToInt64(row["THANH_TIEN"]);
                GanGiaVonDaLuu(ct, row);
                ct.MaSanPham = ctrl.LayMaSanPham(Convert.ToString(row["ID_MA_SAN_PHAM"]));

                ds.Add(ct);
            }
            return ds;
        }
        public IList<ChiTietPhieuBan> ChiTietPhieuBan(int thang, int nam)
        {
            IList<ChiTietPhieuBan> ds = new List<ChiTietPhieuBan>();

            DataTable tbl = factory.LayChiTietPhieuBan(thang, nam);
            foreach (DataRow row in tbl.Rows)
            {
                MaSanPhamController ctrl = new MaSanPhamController();
                ChiTietPhieuBan ct = new ChiTietPhieuBan();
                ct.DonGia = Convert.ToInt64(row["DON_GIA"]);
                ct.SoLuong = Convert.ToInt32(row["SO_LUONG"]);
                ct.ThanhTien = Convert.ToInt64(row["THANH_TIEN"]);
                GanGiaVonDaLuu(ct, row);
                ct.MaSanPham = ctrl.LayMaSanPham(Convert.ToString(row["ID_MA_SAN_PHAM"]));

                ds.Add(ct);
            }
            return ds;
        }

    }
}
