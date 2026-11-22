using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public partial class frmSanphamHethan : Form
    {
        public frmSanphamHethan()
        {
            InitializeComponent();
            CuahangNongduoc.ResponsiveLayout.Attach(this);
        }

        private void btnXem_Click(object sender, EventArgs e)
        {
            IList<CuahangNongduoc.BusinessObject.MaSanPham> data = CuahangNongduoc.Controller.MaSanPhamController.LayMaSanPhamHetHan(dt.Value.Date);
            IList<Microsoft.Reporting.WinForms.ReportParameter> param = new List<Microsoft.Reporting.WinForms.ReportParameter>();

            param.Add(new Microsoft.Reporting.WinForms.ReportParameter("ngay_tinh", dt.Value.Date.ToString("dd/MM/yyyy")));
            DataTable table = new DataTable();
            table.Columns.Add("Id", typeof(string));
            table.Columns.Add("TenSanPham", typeof(string));
            table.Columns.Add("GiaNhap", typeof(long));
            table.Columns.Add("SoLuong", typeof(int));
            table.Columns.Add("NgayNhap", typeof(DateTime));
            table.Columns.Add("NgaySanXuat", typeof(DateTime));
            table.Columns.Add("NgayHetHan", typeof(DateTime));
            foreach (CuahangNongduoc.BusinessObject.MaSanPham lot in data)
                table.Rows.Add(lot.Id, lot.SanPham == null ? lot.Id : lot.SanPham.TenSanPham,
                    lot.GiaNhap, lot.SoLuong, lot.NgayNhap, lot.NgaySanXuat, lot.NgayHetHan);
            this.MaSanPhamBindingSource.DataSource = table;
            this.reportViewer.LocalReport.SetParameters(param);
            this.reportViewer.RefreshReport();
        }

        private void frmSanphamHethan_Load(object sender, EventArgs e)
        {

        }
    }
}
