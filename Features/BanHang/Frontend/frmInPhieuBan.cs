using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public partial class frmInPhieuBan : Form
    {
        CuahangNongduoc.BusinessObject.PhieuBan m_PhieuBan;
        public frmInPhieuBan(CuahangNongduoc.BusinessObject.PhieuBan ph)
        {
            if (ph == null) throw new ArgumentNullException("ph", "Không tìm thấy phiếu bán cần in.");
            InitializeComponent();
            Text = "Xem trước phiếu bán hàng";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1000, 720);
            CuahangNongduoc.ResponsiveLayout.Attach(this);
            System.Reflection.Assembly assembly = typeof(frmInPhieuBan).Assembly;
            using (System.IO.Stream main = assembly.GetManifestResourceStream("CuahangNongduoc.Report.rptPhieuBan.rdlc"))
                reportViewer.LocalReport.LoadReportDefinition(main);
            using (System.IO.Stream detail = assembly.GetManifestResourceStream("CuahangNongduoc.Report.rptChiTietPhieuBan.rdlc"))
                reportViewer.LocalReport.LoadSubreportDefinition("rptChiTietPhieuBan", detail);
            this.reportViewer.LocalReport.SubreportProcessing += new Microsoft.Reporting.WinForms.SubreportProcessingEventHandler(LocalReport_SubreportProcessing);
            m_PhieuBan = ph;
        }

        void LocalReport_SubreportProcessing(object sender, Microsoft.Reporting.WinForms.SubreportProcessingEventArgs e)
        {
            e.DataSources.Clear();
            e.DataSources.Add(new Microsoft.Reporting.WinForms.ReportDataSource("CuahangNongduoc_BusinessObject_ChiTietPhieuBan", SalePrintData.Details(m_PhieuBan)));
        }

        private void frmInPhieuBan_Load(object sender, EventArgs e)
        {
            Num2Str num = new Num2Str();
            IList<Microsoft.Reporting.WinForms.ReportParameter> param = new List<Microsoft.Reporting.WinForms.ReportParameter>();
            CuahangNongduoc.BusinessObject.CuaHang ch = ThamSo.LayCuaHang();
            param.Add(new Microsoft.Reporting.WinForms.ReportParameter("ten_cua_hang", ch.TenCuaHang));
            param.Add(new Microsoft.Reporting.WinForms.ReportParameter("dia_chi", ch.DiaChi));
            param.Add(new Microsoft.Reporting.WinForms.ReportParameter("dien_thoai", ch.DienThoai));
            param.Add(new Microsoft.Reporting.WinForms.ReportParameter("bang_chu", num.NumberToString(m_PhieuBan.TongTien.ToString())));

            this.reportViewer.LocalReport.SetParameters(param);
            this.PhieuBanBindingSource.DataSource = SalePrintData.Header(m_PhieuBan);
            System.Drawing.Printing.PageSettings page = new System.Drawing.Printing.PageSettings();
            page.PaperSize = new System.Drawing.Printing.PaperSize("A4", 827, 1169);
            page.Margins = new System.Drawing.Printing.Margins(59, 59, 59, 59);
            reportViewer.SetPageSettings(page);
            reportViewer.SetDisplayMode(Microsoft.Reporting.WinForms.DisplayMode.PrintLayout);
            reportViewer.ZoomMode = Microsoft.Reporting.WinForms.ZoomMode.PageWidth;
            this.reportViewer.RefreshReport();
        }
    }
    public static class SalePrintData
    {
        public static DataTable Header(CuahangNongduoc.BusinessObject.PhieuBan invoice)
        {
            if (invoice == null) throw new ArgumentNullException("invoice");
            DataTable table = new DataTable();
            table.Columns.Add("Id", typeof(string));
            table.Columns.Add("NgayBan", typeof(DateTime));
            table.Columns.Add("TongTien", typeof(long));
            table.Columns.Add("DaTra", typeof(long));
            table.Columns.Add("ConNo", typeof(long));
            table.Columns.Add("KhachHangHoTen", typeof(string));
            table.Columns.Add("KhachHangDiaChi", typeof(string));
            table.Columns.Add("KhachHangDienThoai", typeof(string));
            CuahangNongduoc.BusinessObject.KhachHang customer = invoice.KhachHang;
            table.Rows.Add(invoice.Id, invoice.NgayBan, invoice.TongTien, invoice.DaTra, invoice.ConNo,
                customer == null ? "" : customer.HoTen, customer == null ? "" : customer.DiaChi,
                customer == null ? "" : customer.DienThoai);
            return table;
        }

        public static DataTable Details(CuahangNongduoc.BusinessObject.PhieuBan invoice)
        {
            DataTable table = new DataTable();
            table.Columns.Add("TenSanPham", typeof(string));
            table.Columns.Add("DonGia", typeof(long));
            table.Columns.Add("SoLuong", typeof(int));
            table.Columns.Add("ThanhTien", typeof(long));
            if (invoice.ChiTiet == null) return table;
            foreach (CuahangNongduoc.BusinessObject.ChiTietPhieuBan line in invoice.ChiTiet)
            {
                string name = line.MaSanPham == null || line.MaSanPham.SanPham == null ? "" : line.MaSanPham.SanPham.TenSanPham;
                table.Rows.Add(name, line.DonGia, line.SoLuong, line.ThanhTien);
            }
            return table;
        }
    }

}