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
            m_PhieuThanhToan = ph;
        }

        private void frmPhieuThanhToan_Load(object sender, EventArgs e)
        {
            this.reportViewer.LocalReport.SetParameters(
                ThuChiReportParameters.Tao(m_PhieuThanhToan));
            this.PhieuThanhToanBindingSource.DataSource = m_PhieuThanhToan;
            this.reportViewer.RefreshReport();
        }
    }
}
