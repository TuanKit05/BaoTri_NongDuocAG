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
            CuahangNongduoc.ResponsiveLayout.Attach(this);
            m_PhieuChi = ph;
        }

        private void frmInPhieuChi_Load(object sender, EventArgs e)
        {
            this.reportViewer.LocalReport.SetParameters(
                ThuChiReportParameters.Tao(m_PhieuChi));
            DataTable table = new DataTable();
            table.Columns.Add("Id", typeof(string));
            table.Columns.Add("NgayChi", typeof(DateTime));
            table.Columns.Add("TongTien", typeof(decimal));
            table.Columns.Add("GhiChu", typeof(string));
            table.Columns.Add("TenLyDoChi", typeof(string));
            table.Rows.Add(m_PhieuChi.Id, m_PhieuChi.NgayChi, m_PhieuChi.TongTien,
                m_PhieuChi.GhiChu, m_PhieuChi.LyDoChi == null ? "" : m_PhieuChi.LyDoChi.TenLyDo);
            this.PhieuChiBindingSource.DataSource = table;
            this.reportViewer.RefreshReport();
        }
    }
}
