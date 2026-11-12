using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public partial class frmInDunoKhachHang : Form
    {
        CuahangNongduoc.BusinessObject.DuNoKhachHang m_DuNo;
        public frmInDunoKhachHang(CuahangNongduoc.BusinessObject.DuNoKhachHang dn)
        {
            InitializeComponent();
            m_DuNo = dn;
        }

        private void frmInDunoKhachHang_Load(object sender, EventArgs e)
        {
            this.reportViewer.LocalReport.SetParameters(
                ThuChiReportParameters.Tao(m_DuNo.CuoiKy));
            this.DuNoKhachHangBindingSource.DataSource = m_DuNo;
            this.reportViewer.RefreshReport();
        }
    }
}
