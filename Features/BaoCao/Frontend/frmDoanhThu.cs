using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public partial class frmDoanhThu : Form
    {
        public frmDoanhThu()
        {
            InitializeComponent();
            CuahangNongduoc.ResponsiveLayout.Attach(this);
        }

        private void frmDoanhThu_Load(object sender, EventArgs e)
        {

            this.reportViewer.RefreshReport();
        }
    }
}