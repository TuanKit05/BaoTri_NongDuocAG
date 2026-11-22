using System;
using System.Data;
using System.Data.OleDb;

namespace CuahangNongduoc
{
    public static class WarehouseCost
    {
        internal static decimal UnitCost(OleDbConnection connection, OleDbTransaction transaction, string lot, string method)
        {
            DataTable rows = AccountRepository.ReadTable(connection, transaction,
                "SELECT MSP.DON_GIA_NHAP AS GIA_LO, SP.DON_GIA_NHAP AS GIA_BQ FROM MA_SAN_PHAM MSP INNER JOIN SAN_PHAM SP ON MSP.ID_SAN_PHAM=SP.ID WHERE MSP.ID=?", lot);
            if (rows.Rows.Count != 1) throw new InvalidOperationException("Không tìm thấy lô để tính giá xuất: " + lot);
            object value = rows.Rows[0][method == WarehouseSettings.WeightedAverage ? "GIA_BQ" : "GIA_LO"];
            if (value == DBNull.Value || Convert.ToDecimal(value) < 0) throw new InvalidOperationException("Giá vốn của lô không hợp lệ: " + lot);
            return Convert.ToDecimal(value);
        }

        public static decimal Preview(string lot)
        {
            AccessControl.DemandCurrent("XemGiaVon");
            using (OleDbConnection connection = new OleDbConnection(DataService.m_ConnectString))
            {
                connection.Open();
                return UnitCost(connection, null, lot, WarehouseSettings.ReadCostMethod(connection, null));
            }
        }
    }
}
