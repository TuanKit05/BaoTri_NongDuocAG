using System;
using System.Collections.Generic;
using System.Data;
using CuahangNongduoc.BusinessObject;

namespace CuahangNongduoc
{
    // Input lots are ordered by expiry, receipt date, then lot code.
    public static class LotAllocation
    {
        public static void Add(DataTable lots, DataTable pending, string invoice, decimal quantity, decimal price, bool gift = false)
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
                    DataRowVersion version=row.RowState==DataRowState.Deleted?DataRowVersion.Original:DataRowVersion.Current;
                    if(Convert.ToString(row["ID_PHIEU_BAN",version])!=invoice) continue;
                    if (row.HasVersion(DataRowVersion.Original) && Convert.ToString(row["ID_MA_SAN_PHAM", DataRowVersion.Original]) == id)
                        available += Convert.ToInt64(row["SO_LUONG", DataRowVersion.Original]);
                    if (row.RowState != DataRowState.Deleted && Convert.ToString(row["ID_MA_SAN_PHAM"]) == id)
                    {
                        available -= Convert.ToInt64(row["SO_LUONG"]);
                        bool rowGift = pending.Columns.Contains("LA_HANG_TANG") && row["LA_HANG_TANG"] != DBNull.Value && Convert.ToBoolean(row["LA_HANG_TANG"]);
                        if (rowGift == gift) existing = row;
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
                    if (pending.Columns.Contains("LA_HANG_TANG")) target["LA_HANG_TANG"] = gift;
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
    public static class SalePromotionWorkflow
    {
        private static bool Gift(DataRow row) { return row["LA_HANG_TANG"]!=DBNull.Value && Convert.ToBoolean(row["LA_HANG_TANG"]); }
        private static string Key(DataRow row) { return Convert.ToString(row["ID_PHIEU_BAN"])+"\u0001"+Convert.ToString(row["ID_MA_SAN_PHAM"])+"\u0001"+Gift(row); }
        public static DataTable Plan(DataTable pending,DataTable lots,IList<UuDaiQuyDinh> rules,string invoice,
            string product,string selectedLot,decimal quantity,decimal price,bool automatic,bool gift)
        {
            DataTable plan=pending.Copy();
            if(quantity>0) {
                DataTable choices=lots.Clone();
                foreach(DataRow lot in lots.Rows) if(Convert.ToString(lot["ID_SAN_PHAM"])==product &&
                    (automatic || Convert.ToString(lot["ID"])==selectedLot)) choices.ImportRow(lot);
                LotAllocation.Add(choices,plan,invoice,quantity,gift?0m:price,gift);
            }
            SynchronizeGifts(plan,lots,rules,invoice);
            return plan;
        }
        private static void SynchronizeGifts(DataTable plan,DataTable lots,IList<UuDaiQuyDinh> rules,string invoice)
        {
            // Remove generated gifts on the plan, preserving original versions for stock credits.
            for(int i=plan.Rows.Count-1;i>=0;i--) {
                DataRow row=plan.Rows[i];
                if(row.RowState!=DataRowState.Deleted && Convert.ToString(row["ID_PHIEU_BAN"])==invoice && Gift(row)) row.Delete();
            }
            Dictionary<string,int> budgets=ChonKhuyenMai.QuyenTang(rules,plan,invoice);
            foreach(KeyValuePair<string,int> budget in budgets) {
                if(budget.Value<=0) continue;
                bool byProduct=budget.Key.StartsWith("SP:");
                DataTable choices=lots.Clone();
                foreach(DataRow lot in lots.Rows)
                    if(Convert.ToString(lot[byProduct?"ID_SAN_PHAM":"ID"])==budget.Key.Substring(3)) choices.ImportRow(lot);
                try { LotAllocation.Add(choices,plan,invoice,budget.Value,0m,true); }
                catch(InvalidOperationException error) { throw new InvalidOperationException("Không đủ hàng tặng còn hạn cho ưu đãi ("+budget.Key.Substring(3)+"). "+error.Message,error); }
            }
        }
        public static void Apply(DataTable pending,DataTable plan,string invoice)
        {
            Dictionary<string,DataRow> desired=new Dictionary<string,DataRow>(StringComparer.OrdinalIgnoreCase);
            foreach(DataRow row in plan.Rows)
                if(row.RowState!=DataRowState.Deleted && Convert.ToString(row["ID_PHIEU_BAN"])==invoice) desired[Key(row)]=row;
            for(int i=pending.Rows.Count-1;i>=0;i--) {
                DataRow row=pending.Rows[i];
                if(row.RowState==DataRowState.Deleted || Convert.ToString(row["ID_PHIEU_BAN"])!=invoice) continue;
                DataRow target;
                if(!desired.TryGetValue(Key(row),out target)) { row.Delete(); continue; }
                foreach(string name in new string[] { "SO_LUONG","DON_GIA","THANH_TIEN" })
                    if(!Object.Equals(row[name],target[name])) row[name]=target[name];
                desired.Remove(Key(row));
            }
            foreach(DataRow row in desired.Values) { DataRow added=pending.NewRow(); added.ItemArray=row.ItemArray; pending.Rows.Add(added); }
        }
        public static Dictionary<string,long> StockChanges(DataTable details)
        {
            Dictionary<string,long> result=new Dictionary<string,long>(StringComparer.OrdinalIgnoreCase);
            foreach(DataRow row in details.Rows) {
                if(row.RowState==DataRowState.Unchanged || row.RowState==DataRowState.Detached) continue;
                if(row.HasVersion(DataRowVersion.Original)) Add(result,Convert.ToString(row["ID_MA_SAN_PHAM",DataRowVersion.Original]),Convert.ToInt64(row["SO_LUONG",DataRowVersion.Original]));
                if(row.RowState!=DataRowState.Deleted) Add(result,Convert.ToString(row["ID_MA_SAN_PHAM"]),-Convert.ToInt64(row["SO_LUONG"]));
            }
            return result;
        }
        private static void Add(Dictionary<string,long> result,string lot,long amount)
        { long previous; result.TryGetValue(lot,out previous); result[lot]=checked(previous+amount); }
    }

}
