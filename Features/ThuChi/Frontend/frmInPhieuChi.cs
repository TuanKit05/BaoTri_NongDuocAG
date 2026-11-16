using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public partial class frmInPhieuChi : Form
    {
        CuahangNongduoc.BusinessObject.PhieuChi m_PhieuChi;
        public frmInPhieuChi(CuahangNongduoc.BusinessObject.PhieuChi ph)
        {
            InitializeComponent();
            m_PhieuChi = ph;
        }

        private void frmInPhieuChi_Load(object sender, EventArgs e)
        {
            this.reportViewer.LocalReport.SetParameters(
                ThuChiReportParameters.Tao(m_PhieuChi));
            this.PhieuChiBindingSource.DataSource = m_PhieuChi;
            this.reportViewer.RefreshReport();
        }
    }
}
