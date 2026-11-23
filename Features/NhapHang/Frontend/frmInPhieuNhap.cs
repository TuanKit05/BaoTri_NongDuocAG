using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public partial class frmInPhieuNhap : Form
    {
        CuahangNongduoc.BusinessObject.PhieuNhap m_PhieuNhap = null;
        public frmInPhieuNhap(CuahangNongduoc.BusinessObject.PhieuNhap ph)
        {
            m_PhieuNhap = ph;
            InitializeComponent();
            Text = "In phiếu nhập hàng";
            Rectangle area = Screen.FromControl(this).WorkingArea;
            Size = new Size(Math.Min(1050, area.Width - 32), Math.Min(800, area.Height - 32));
            StartPosition = FormStartPosition.CenterScreen;
            reportViewer.ZoomMode = Microsoft.Reporting.WinForms.ZoomMode.PageWidth;
            CuahangNongduoc.ResponsiveLayout.Attach(this);

            reportViewer.LocalReport.ExecuteReportInSandboxAppDomain();
            this.reportViewer.LocalReport.SubreportProcessing += new Microsoft.Reporting.WinForms.SubreportProcessingEventHandler(LocalReport_SubreportProcessing);
        }

        void LocalReport_SubreportProcessing(object sender, Microsoft.Reporting.WinForms.SubreportProcessingEventArgs e)
        {
            e.DataSources.Clear();
            DataTable details = new DataTable();
            details.Columns.Add("Id", typeof(string));
            details.Columns.Add("TenSanPham", typeof(string));
            details.Columns.Add("GiaNhap", typeof(long));
            details.Columns.Add("SoLuong", typeof(int));
            details.Columns.Add("ThanhTien", typeof(long));
            details.Columns.Add("NgaySanXuat", typeof(DateTime));
            details.Columns.Add("NgayHetHan", typeof(DateTime));
            if (m_PhieuNhap.ChiTiet != null)
                foreach (CuahangNongduoc.BusinessObject.MaSanPham lot in m_PhieuNhap.ChiTiet)
                    details.Rows.Add(lot.Id, lot.SanPham == null ? lot.Id : lot.SanPham.TenSanPham,
                        lot.GiaNhap, lot.SoLuong, lot.ThanhTien, lot.NgaySanXuat, lot.NgayHetHan);
            e.DataSources.Add(new Microsoft.Reporting.WinForms.ReportDataSource("CuahangNongduoc_BusinessObject_MaSanPham", details));
        }

        private void frmInPhieuNhap_Load(object sender, EventArgs e)
        {
            Num2Str num = new Num2Str();
            IList<Microsoft.Reporting.WinForms.ReportParameter> param = new List<Microsoft.Reporting.WinForms.ReportParameter>();
            CuahangNongduoc.BusinessObject.CuaHang ch = ThamSo.LayCuaHang();
            param.Add(new Microsoft.Reporting.WinForms.ReportParameter("ten_cua_hang", ch.TenCuaHang));
            param.Add(new Microsoft.Reporting.WinForms.ReportParameter("dia_chi", ch.DiaChi));
            param.Add(new Microsoft.Reporting.WinForms.ReportParameter("dien_thoai", ch.DienThoai));

            param.Add(new Microsoft.Reporting.WinForms.ReportParameter("bang_chu", num.NumberToString(m_PhieuNhap.TongTien.ToString())));

            this.reportViewer.LocalReport.SetParameters(param);
            DataTable header = new DataTable();
            header.Columns.Add("Id", typeof(string));
            header.Columns.Add("NgayNhap", typeof(DateTime));
            header.Columns.Add("TongTien", typeof(long));
            header.Columns.Add("DaTra", typeof(long));
            header.Columns.Add("ConNo", typeof(long));
            header.Columns.Add("TenNhaCungCap", typeof(string));
            header.Rows.Add(m_PhieuNhap.Id, m_PhieuNhap.NgayNhap, m_PhieuNhap.TongTien,
                m_PhieuNhap.DaTra, m_PhieuNhap.ConNo,
                m_PhieuNhap.NhaCungCap == null ? "" : m_PhieuNhap.NhaCungCap.HoTen);
            this.PhieuNhapBindingSource.DataSource = header;
            this.reportViewer.RefreshReport();
        }
    }
}
