using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using CuahangNongduoc.BusinessObject;
using CuahangNongduoc.Controller;

namespace CuahangNongduoc
{
    public partial class frmSoLuongBan : Form
    {
        public frmSoLuongBan()
        {
            InitializeComponent();
            CuahangNongduoc.ResponsiveLayout.Attach(this);
            reportViewer.LocalReport.ExecuteReportInSandboxAppDomain();
        }

        private void frmSoLuongBan_Load(object sender, EventArgs e)
        {
            cmbThang.SelectedIndex = DateTime.Now.Month - 1;
            numNam.Value = DateTime.Now.Year;
        }

        private static DataTable DuLieuBaoCao(IList<ChiTietPhieuBan> details)
        {
            DataTable table = new DataTable();
            table.Columns.Add("MaLo", typeof(string));
            table.Columns.Add("TenSanPham", typeof(string));
            table.Columns.Add("DonGia", typeof(long));
            table.Columns.Add("SoLuong", typeof(int));
            table.Columns.Add("ThanhTien", typeof(long));
            foreach (ChiTietPhieuBan detail in details)
            {
                string lot = detail.MaSanPham == null ? "" : detail.MaSanPham.Id;
                string product = detail.MaSanPham == null || detail.MaSanPham.SanPham == null
                    ? lot : detail.MaSanPham.SanPham.TenSanPham;
                table.Rows.Add(lot, product, detail.DonGia, detail.SoLuong, detail.ThanhTien);
            }
            return table;
        }

        ChiTietPhieuBanController ctrl = new ChiTietPhieuBanController();

        
        private void btnXemNgay_Click(object sender, EventArgs e)
        {
            IList<Microsoft.Reporting.WinForms.ReportParameter> param = new List<Microsoft.Reporting.WinForms.ReportParameter>();
            param.Add(new Microsoft.Reporting.WinForms.ReportParameter("ngay", "Ngày " + dtNgay.Value.Date.ToString("dd/MM/yyyy")));

            this.reportViewer.LocalReport.SetParameters(param);

            this.ChiTietPhieuBanBindingSource.DataSource = DuLieuBaoCao(ctrl.ChiTietPhieuBan(dtNgay.Value.Date));
            this.reportViewer.RefreshReport();
        }

        private void btnXemThang_Click(object sender, EventArgs e)
        {
            IList<Microsoft.Reporting.WinForms.ReportParameter> param = new List<Microsoft.Reporting.WinForms.ReportParameter>();
            param.Add(new Microsoft.Reporting.WinForms.ReportParameter("ngay",
                "Tháng " + Convert.ToString(cmbThang.SelectedIndex + 1) + "/" + numNam.Value.ToString()));

            this.reportViewer.LocalReport.SetParameters(param);


            this.ChiTietPhieuBanBindingSource.DataSource = DuLieuBaoCao(ctrl.ChiTietPhieuBan(cmbThang.SelectedIndex + 1, Convert.ToInt32(numNam.Value)));
            this.reportViewer.RefreshReport();
        }

      
    }
}