using CuahangNongduoc.Controller;
using System;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public partial class frmDonViTinh : BaseDanhMucForm
    {
        private readonly DonViTinhController _ctrl = new DonViTinhController();

        protected override BaseController Controller => _ctrl;
        protected override string TenDanhMuc => "Đơn vị tính";

        public frmDonViTinh()
        {
            InitializeComponent();
            this.dataGridView = dataGridView;
            this.bindingNavigator = bindingNavigator;
        }

        private void frmDonViTinh_Load(object sender, EventArgs e)
        {
            Base_Load(sender, e);
        }

        private void toolLuu_Click(object sender, EventArgs e)
        {
            Luu();
        }
    }
}