using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public partial class frmInPhieuThanhToan : Form
    {
        CuahangNongduoc.BusinessObject.PhieuThanhToan m_PhieuThanhToan;
        public frmInPhieuThanhToan(CuahangNongduoc.BusinessObject.PhieuThanhToan ph)
        {
            InitializeComponent();
            CuahangNongduoc.ResponsiveLayout.Attach(this);
            m_PhieuThanhToan = ph;
        }

        private void frmPhieuThanhToan_Load(object sender, EventArgs e)
        {
            this.reportViewer.LocalReport.SetParameters(
                ThuChiReportParameters.Tao(m_PhieuThanhToan));
            DataTable table = new DataTable();
            table.Columns.Add("Id", typeof(string));
            table.Columns.Add("NgayThanhToan", typeof(DateTime));
            table.Columns.Add("TongTien", typeof(long));
            table.Columns.Add("GhiChu", typeof(string));
            table.Columns.Add("TenKhachHang", typeof(string));
            table.Columns.Add("DiaChiKhachHang", typeof(string));
            table.Columns.Add("DienThoaiKhachHang", typeof(string));
            CuahangNongduoc.BusinessObject.KhachHang customer = m_PhieuThanhToan.KhachHang;
            table.Rows.Add(m_PhieuThanhToan.Id, m_PhieuThanhToan.NgayThanhToan,
                m_PhieuThanhToan.TongTien, m_PhieuThanhToan.GhiChu,
                customer == null ? "" : customer.HoTen,
                customer == null ? "" : customer.DiaChi,
                customer == null ? "" : customer.DienThoai);
            this.PhieuThanhToanBindingSource.DataSource = table;
            this.reportViewer.RefreshReport();
        }
    }
}
