using System;
using System.Collections.Generic;
using Microsoft.Reporting.WinForms;

namespace CuahangNongduoc
{
    // Extract Class / Parameterize Method: doan tao tham so chung cho 3 bao cao.
    internal static class ThuChiReportParameters
    {
        // Preserve Whole Object: nhan chung tu thay vi tach TongTien o moi form.
        internal static IList<ReportParameter> Tao(BusinessObject.PhieuThuChiCoBan phieu)
        {
            return Tao(phieu.TongTien);
        }

        internal static IList<ReportParameter> Tao(long soTien)
        {
            BusinessObject.CuaHang cuaHang = ThamSo.LayCuaHang();
            Num2Str chuyenSo = new Num2Str();
            IList<ReportParameter> result = new List<ReportParameter>();
            result.Add(new ReportParameter("ten_cua_hang", cuaHang.TenCuaHang));
            result.Add(new ReportParameter("dia_chi", cuaHang.DiaChi));
            result.Add(new ReportParameter("dien_thoai", cuaHang.DienThoai));
            result.Add(new ReportParameter("bang_chu", chuyenSo.NumberToString(soTien.ToString())));
            return result;
        }
    }
}
