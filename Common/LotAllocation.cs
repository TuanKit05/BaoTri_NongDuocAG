using System;
using System.Collections.Generic;
using System.Data;
using CuahangNongduoc.BusinessObject;

namespace CuahangNongduoc
{
    // Input lots are ordered by expiry, receipt date, then lot code.
    public static class LotAllocation
    {
        public static void Add(DataTable lots, DataTable pending, string invoice, decimal quantity, decimal price)
        {
            if (quantity <= 0 || quantity != Decimal.Truncate(quantity) || quantity > Int32.MaxValue)
                throw new InvalidOperationException("Số lượng phải là số nguyên lớn hơn 0.");
            TienTe.ThanhTien(price, quantity);
            long remaining = (long)quantity;
            List<KeyValuePair<DataRow, int>> plan = new List<KeyValuePair<DataRow, int>>();
            foreach (DataRow lot in lots.Rows)
            {
                if (lot["NGAY_HET_HAN"] == DBNull.Value || Convert.ToDateTime(lot["NGAY_HET_HAN"]).Date < DateTime.Today) continue;
                string id = Convert.ToString(lot["ID"]);
                long available = lot["SO_LUONG"] == DBNull.Value ? 0 : Convert.ToInt64(lot["SO_LUONG"]);
                DataRow existing = null;
                foreach (DataRow row in pending.Rows)
                {
                    if (row.HasVersion(DataRowVersion.Original) && Convert.ToString(row["ID_MA_SAN_PHAM", DataRowVersion.Original]) == id)
                        available += Convert.ToInt64(row["SO_LUONG", DataRowVersion.Original]);
                    if (row.RowState != DataRowState.Deleted && Convert.ToString(row["ID_MA_SAN_PHAM"]) == id)
                    {
                        available -= Convert.ToInt64(row["SO_LUONG"]);
                        existing = row;
                    }
                }
                if (available <= 0) continue;
                int take = (int)Math.Min(available, remaining);
                DataRow target = existing ?? pending.NewRow();
                int total = checked((existing == null ? 0 : Convert.ToInt32(existing["SO_LUONG"])) + take);
                if (existing != null && Convert.ToDecimal(existing["DON_GIA"]) != price)
                    throw new InvalidOperationException("Lô " + id + " đã có đơn giá khác trên phiếu. Vui lòng sửa dòng hiện có trước.");
                TienTe.ThanhTien(price, total);
                if (existing == null)
                {
                    target["ID_MA_SAN_PHAM"] = id;
                    target["ID_PHIEU_BAN"] = invoice;
                    target["DON_GIA"] = price;
                }
                plan.Add(new KeyValuePair<DataRow, int>(target, total));
                remaining -= take;
                if (remaining == 0) break;
            }
            if (remaining > 0) throw new InvalidOperationException("Không đủ tồn kho còn hạn dùng. Thiếu " + remaining + " sản phẩm.");
            // Do not change any invoice rows until the entire request can be fulfilled.
            foreach (KeyValuePair<DataRow, int> line in plan)
            {
                line.Key["SO_LUONG"] = line.Value;
                line.Key["THANH_TIEN"] = TienTe.ThanhTien(price, line.Value);
                if (line.Key.RowState == DataRowState.Detached) pending.Rows.Add(line.Key);
            }
        }
    }
}
