using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;

namespace CuahangNongduoc
{
    public sealed class FifoCostPart
    {
        public string Lot;
        public int Quantity;
        public decimal UnitPrice;
    }
    public static class WarehouseCost
    {
        internal static void EnsureSchema(OleDbConnection c)
        {
            bool exists=false;
            foreach(DataRow row in c.GetSchema("Tables").Rows) if(Convert.ToString(row["TABLE_NAME"])=="LOP_GIA_VON") exists=true;
            if(!exists) {
                string source=new OleDbConnectionStringBuilder(DataService.m_ConnectString).DataSource;
                string folder=Path.Combine(Path.GetDirectoryName(source),"Backups");Directory.CreateDirectory(folder);
                File.Copy(source,Path.Combine(folder,"cuahang_truoc_fifo_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".mdb"));
                AccountRepository.Execute(c,null,"CREATE TABLE LOP_GIA_VON (MA_LO TEXT(50) PRIMARY KEY, ID_SAN_PHAM TEXT(50), NGAY_NHAP DATETIME, DON_GIA CURRENCY, SO_LUONG LONG)");
            }
            bool allocations=false;
            foreach(DataRow row in c.GetSchema("Tables").Rows) if(Convert.ToString(row["TABLE_NAME"])=="PHAN_BO_GIA_VON") allocations=true;
            if(!allocations) AccountRepository.Execute(c,null,"CREATE TABLE PHAN_BO_GIA_VON (ID COUNTER PRIMARY KEY, ID_PHIEU_BAN TEXT(50), MA_LO_XUAT TEXT(50), LA_HANG_TANG YESNO, MA_LO_GIA TEXT(50), SO_LUONG LONG, DON_GIA CURRENCY)");
            // Opening cost layers use current verified stored quantities; old invoice costs are never rewritten.
            AccountRepository.Execute(c,null,"INSERT INTO LOP_GIA_VON (MA_LO,ID_SAN_PHAM,NGAY_NHAP,DON_GIA,SO_LUONG) SELECT M.ID,M.ID_SAN_PHAM,M.NGAY_NHAP,M.DON_GIA_NHAP,M.SO_LUONG FROM MA_SAN_PHAM M LEFT JOIN LOP_GIA_VON L ON M.ID=L.MA_LO WHERE L.MA_LO IS NULL");
        }
        internal static void AddReceipt(OleDbConnection c,OleDbTransaction tr,string lot)
        {
            AccountRepository.Execute(c,tr,"INSERT INTO LOP_GIA_VON (MA_LO,ID_SAN_PHAM,NGAY_NHAP,DON_GIA,SO_LUONG) SELECT ID,ID_SAN_PHAM,NGAY_NHAP,DON_GIA_NHAP,SO_LUONG FROM MA_SAN_PHAM WHERE ID=?",lot);
        }
        internal static void DeleteReceiptLayer(OleDbConnection c,OleDbTransaction tr,string lot)
        {
            if(Convert.ToInt32(AccountRepository.Scalar(c,tr,"SELECT COUNT(*) FROM PHAN_BO_GIA_VON WHERE MA_LO_GIA=?",lot))>0)
                throw new InvalidOperationException("Lô "+lot+" đã được dùng trong tính giá vốn; không thể xóa phiếu nhập.");
            AccountRepository.Execute(c,tr,"DELETE FROM LOP_GIA_VON WHERE MA_LO=?",lot);
        }
        public static IList<FifoCostPart> Plan(DataTable layers,int quantity)
        {
            if(quantity<=0) throw new ArgumentException("Số lượng tính giá vốn phải lớn hơn 0.");
            List<FifoCostPart> result=new List<FifoCostPart>();int remaining=quantity;
            foreach(DataRow row in layers.Rows) {
                if(row["SO_LUONG"]==DBNull.Value || Convert.ToInt32(row["SO_LUONG"])<0) throw new InvalidOperationException("Lớp giá vốn thiếu số lượng hoặc có tồn âm; cần đối chiếu dữ liệu.");
                if(Convert.ToInt32(row["SO_LUONG"])>0 && (row["NGAY_NHAP"]==DBNull.Value || row["DON_GIA"]==DBNull.Value || Convert.ToDecimal(row["DON_GIA"])<0))
                    throw new InvalidOperationException("Lớp giá vốn còn hàng nhưng thiếu ngày nhập hoặc giá hợp lệ.");
            }
            DataView ordered=new DataView(layers);ordered.Sort="NGAY_NHAP ASC, MA_LO ASC";
            foreach(DataRowView row in ordered) {
                if(Convert.ToInt32(row["SO_LUONG"])==0) continue;
                if(row["SO_LUONG"]==DBNull.Value || row["NGAY_NHAP"]==DBNull.Value || row["DON_GIA"]==DBNull.Value)
                    throw new InvalidOperationException("Lớp giá vốn thiếu số lượng, ngày nhập hoặc đơn giá; cần đối chiếu dữ liệu.");
                int available=Convert.ToInt32(row["SO_LUONG"]);decimal price=Convert.ToDecimal(row["DON_GIA"]);
                if(available<0 || price<0) throw new InvalidOperationException("Lớp giá vốn có số lượng hoặc đơn giá âm; cần đối chiếu dữ liệu.");
                int take=Math.Min(available,remaining);if(take==0) continue;
                result.Add(new FifoCostPart { Lot=Convert.ToString(row["MA_LO"]),Quantity=take,UnitPrice=price });
                remaining-=take;if(remaining==0) break;
            }
            if(remaining>0) throw new InvalidOperationException("Thiếu "+remaining+" đơn vị trong lớp giá vốn. Vui lòng đối chiếu tồn kho.");
            return result;
        }
        private static DataTable Layers(OleDbConnection c,OleDbTransaction tr,string physicalLot)
        {
            return AccountRepository.ReadTable(c,tr,"SELECT L.* FROM LOP_GIA_VON L WHERE L.ID_SAN_PHAM IN (SELECT ID_SAN_PHAM FROM MA_SAN_PHAM WHERE ID=?) ORDER BY L.NGAY_NHAP,L.MA_LO",physicalLot);
        }
        internal static decimal UnitCost(OleDbConnection c,OleDbTransaction tr,string lot,string method)
        {
            if(method!=WarehouseSettings.WeightedAverage) return Plan(Layers(c,tr,lot),1)[0].UnitPrice;
            object value=AccountRepository.Scalar(c,tr,"SELECT SP.DON_GIA_NHAP FROM MA_SAN_PHAM M INNER JOIN SAN_PHAM SP ON M.ID_SAN_PHAM=SP.ID WHERE M.ID=?",lot);
            if(value==null || value==DBNull.Value || Convert.ToDecimal(value)<0) throw new InvalidOperationException("Giá vốn bình quân không hợp lệ.");
            return Convert.ToDecimal(value);
        }
        internal static decimal Allocate(OleDbConnection c,OleDbTransaction tr,string invoice,string lot,bool gift,int quantity,string method)
        {
            IList<FifoCostPart> plan=Plan(Layers(c,tr,lot),quantity);decimal fifoTotal=0;
            foreach(FifoCostPart part in plan) {
                if(Execute(c,tr,"UPDATE LOP_GIA_VON SET SO_LUONG=SO_LUONG-? WHERE MA_LO=? AND SO_LUONG>=?",part.Quantity,part.Lot,part.Quantity)!=1)
                    throw new DBConcurrencyException("Lớp giá vốn vừa thay đổi; vui lòng tải lại phiếu.");
                Execute(c,tr,"INSERT INTO PHAN_BO_GIA_VON (ID_PHIEU_BAN,MA_LO_XUAT,LA_HANG_TANG,MA_LO_GIA,SO_LUONG,DON_GIA) VALUES (?,?,?,?,?,?)",invoice,lot,gift,part.Lot,part.Quantity,part.UnitPrice);
                fifoTotal=checked(fifoTotal+part.Quantity*part.UnitPrice);
            }
            return method==WarehouseSettings.WeightedAverage?UnitCost(c,tr,lot,method)*quantity:fifoTotal;
        }
        internal static void Restore(OleDbConnection c,OleDbTransaction tr,string invoice,string lot,bool gift,int legacyQuantity)
        {
            DataTable rows=AccountRepository.ReadTable(c,tr,"SELECT MA_LO_GIA,SO_LUONG FROM PHAN_BO_GIA_VON WHERE ID_PHIEU_BAN=? AND MA_LO_XUAT=? AND LA_HANG_TANG=?",invoice,lot,gift);
            if(rows.Rows.Count==0) {
                if(Execute(c,tr,"UPDATE LOP_GIA_VON SET SO_LUONG=SO_LUONG+? WHERE MA_LO=?",legacyQuantity,lot)!=1)
                    throw new InvalidOperationException("Không tìm thấy lớp giá vốn để hoàn lô "+lot);
            } else foreach(DataRow row in rows.Rows)
                if(Execute(c,tr,"UPDATE LOP_GIA_VON SET SO_LUONG=SO_LUONG+? WHERE MA_LO=?",Convert.ToInt32(row["SO_LUONG"]),Convert.ToString(row["MA_LO_GIA"]))!=1)
                    throw new InvalidOperationException("Không thể hoàn lớp giá vốn.");
            Execute(c,tr,"DELETE FROM PHAN_BO_GIA_VON WHERE ID_PHIEU_BAN=? AND MA_LO_XUAT=? AND LA_HANG_TANG=?",invoice,lot,gift);
        }
        internal static bool ChangesQuantity(DataRow row)
        {
            return row.RowState==DataRowState.Added || row.RowState==DataRowState.Deleted ||
                !Object.Equals(row["SO_LUONG"],row["SO_LUONG",DataRowVersion.Original]) ||
                !Object.Equals(row["ID_MA_SAN_PHAM"],row["ID_MA_SAN_PHAM",DataRowVersion.Original]) ||
                !Object.Equals(row["LA_HANG_TANG"],row["LA_HANG_TANG",DataRowVersion.Original]);
        }
        private static int Execute(OleDbConnection c,OleDbTransaction tr,string sql,params object[] values)
        {
            using(OleDbCommand cmd=new OleDbCommand(sql,c,tr)) {
                foreach(object v in values) cmd.Parameters.Add("?",v is bool?OleDbType.Boolean:v is int?OleDbType.Integer:v is decimal?OleDbType.Currency:OleDbType.VarWChar).Value=v;
                return cmd.ExecuteNonQuery();
            }
        }
        public static decimal Preview(string lot)
        {
            AccessControl.DemandCurrent("XemGiaVon");
            using(OleDbConnection c=new OleDbConnection(DataService.m_ConnectString)) { c.Open();return UnitCost(c,null,lot,WarehouseSettings.ReadCostMethod(c,null)); }
        }
    }
}
