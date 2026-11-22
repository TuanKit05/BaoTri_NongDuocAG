using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace CuahangNongduoc.DataLayer
{
    public class SanPhamFactory : BaseFactory
    {
        protected override string TableName { get { return "SAN_PHAM"; } }

        // Every product read uses the same statement; the legacy SAN_PHAM.SO_LUONG is never read.
        internal const string ProductStockSelect =
            "SELECT SP.ID, SP.TEN_SAN_PHAM, SP.ID_DON_VI_TINH, SP.DON_GIA_NHAP, SP.GIA_BAN_SI, SP.GIA_BAN_LE, " +
            "IIF(TON.TONG_SO_LUONG IS NULL, 0, TON.TONG_SO_LUONG) AS SO_LUONG " +
            "FROM SAN_PHAM AS SP LEFT JOIN " +
            "(SELECT ID_SAN_PHAM, SUM(SO_LUONG) AS TONG_SO_LUONG FROM MA_SAN_PHAM GROUP BY ID_SAN_PHAM) AS TON " +
            "ON SP.ID=TON.ID_SAN_PHAM";

        private static OleDbConnection OpenFreshConnection()
        {
            OleDbConnectionStringBuilder settings = new OleDbConnectionStringBuilder(DataService.m_ConnectString);
            settings["OLE DB Services"] = -2;
            OleDbConnection connection = new OleDbConnection(settings.ConnectionString);
            connection.Open();
            return connection;
        }

        private DataTable ReadProducts(OleDbCommand command)
        {
            AccessControl.PrepareRead(command);
            using (OleDbConnection connection = OpenFreshConnection())
            {
                command.Connection = connection;
                if (Columns.Contains("SO_LUONG")) Columns["SO_LUONG"].ReadOnly = false;
                Clear();
                using (OleDbDataAdapter adapter = new OleDbDataAdapter(command)) adapter.Fill(this);
            }
            foreach (DataColumn column in Columns) column.ReadOnly = column.ColumnName == "SO_LUONG";
            Columns["SO_LUONG"].DefaultValue = 0;
            AcceptChanges();
            AccessControl.HideCosts(this);
            return this;
        }

        public override DataTable DanhSach()
        {
            return ReadProducts(new OleDbCommand(ProductStockSelect));
        }
        public override DataTable LayTheoId(object id)
        {
            OleDbCommand command = new OleDbCommand(ProductStockSelect + " WHERE SP.ID=?");
            command.Parameters.Add("id", OleDbType.VarWChar, 50).Value = Convert.ToString(id);
            return ReadProducts(command);
        }
        public override DataTable TimKiem(string columnName, string keyword)
        {
            if (columnName != "ID" && columnName != "TEN_SAN_PHAM") throw new ArgumentException("Cot tim kiem khong hop le.");
            OleDbCommand command = new OleDbCommand(ProductStockSelect + " WHERE SP.[" + columnName + "] LIKE ?");
            command.Parameters.Add("keyword", OleDbType.VarWChar, 255).Value = "%" + (keyword ?? String.Empty) + "%";
            return ReadProducts(command);
        }
        public DataTable DanhsachSanPham() { return DanhSach(); }
        public DataTable LaySanPham(string id) { return LayTheoId(id); }
        public DataTable TimMaSanPham(string id) { return TimKiem("ID", id); }
        public DataTable TimTenSanPham(string ten) { return TimKiem("TEN_SAN_PHAM", ten); }
        public DataTable LaySoLuongTon()
        {
            DanhSach();
            if (!Columns.Contains("SO_LUONG_TON")) Columns.Add("SO_LUONG_TON", typeof(int));
            foreach (DataRow row in Rows) row["SO_LUONG_TON"] = row["SO_LUONG"];
            AcceptChanges();
            return this;
        }

        // Persist catalog fields only. Stock belongs to MA_SAN_PHAM.
        public override bool Save()
        {
            if (GetChanges() == null) return true;
            AccessControl.ValidateRows(this, "SELECT * FROM SAN_PHAM");
            using (OleDbConnection connection = new OleDbConnection(DataService.m_ConnectString))
            {
                connection.Open();
                using (OleDbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (DataRow row in Rows)
                        {
                            if (row.RowState == DataRowState.Unchanged || row.RowState == DataRowState.Detached) continue;
                            if (row.RowState == DataRowState.Deleted)
                            {
                                string id = Convert.ToString(row["ID", DataRowVersion.Original]);
                                if (Convert.ToInt32(AccountRepository.Scalar(connection, transaction,
                                    "SELECT COUNT(*) FROM MA_SAN_PHAM WHERE ID_SAN_PHAM=?", id)) > 0)
                                    throw new InvalidOperationException("Khong duoc xoa san pham da co lo hang.");
                                if (AccountRepository.Execute(connection, transaction, "DELETE FROM SAN_PHAM WHERE ID=?", id) != 1)
                                    throw new DBConcurrencyException("San pham da bi thay doi.");
                                continue;
                            }
                            string code = Convert.ToString(row["ID"]);
                            int changed;
                            if (row.RowState == DataRowState.Added)
                                changed = AccountRepository.Execute(connection, transaction,
                                    "INSERT INTO SAN_PHAM (TEN_SAN_PHAM,ID_DON_VI_TINH,DON_GIA_NHAP,GIA_BAN_SI,GIA_BAN_LE,ID) VALUES (?,?,?,?,?,?)",
                                    row["TEN_SAN_PHAM"], row["ID_DON_VI_TINH"], row["DON_GIA_NHAP"], row["GIA_BAN_SI"], row["GIA_BAN_LE"], code);
                            else
                            {
                                if (!Object.Equals(row["ID"], row["ID", DataRowVersion.Original]))
                                    throw new InvalidOperationException("Khong duoc doi ma san pham.");
                                changed = AccountRepository.Execute(connection, transaction,
                                    "UPDATE SAN_PHAM SET TEN_SAN_PHAM=?,ID_DON_VI_TINH=?,DON_GIA_NHAP=?,GIA_BAN_SI=?,GIA_BAN_LE=? WHERE ID=?",
                                    row["TEN_SAN_PHAM"], row["ID_DON_VI_TINH"], row["DON_GIA_NHAP"], row["GIA_BAN_SI"], row["GIA_BAN_LE"], code);
                            }
                            if (changed != 1) throw new DBConcurrencyException("Khong the luu san pham.");
                        }
                        AccountRepository.Audit(connection, transaction, "LUU_SAN_PHAM", "SAN_PHAM", AccessControl.DescribeChanges(this));
                        transaction.Commit();
                    }
                    catch { ErrorLog.Rollback(transaction, "Rollback Access"); throw; }
                }
            }
            AcceptChanges();
            return true;
        }
    }
}
