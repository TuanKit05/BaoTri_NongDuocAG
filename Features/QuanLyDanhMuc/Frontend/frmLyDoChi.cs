using CuahangNongduoc.Controller;
using System;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public partial class frmLyDoChi : BaseDanhMucForm
    {
        private readonly LyDoChiController _ctrl;

        protected override BaseController Controller => _ctrl;
        protected override string TenDanhMuc => "Lý do chi";

        public frmLyDoChi()
        {
            InitializeComponent();
            if (IsDesignMode) return;
            _ctrl = new LyDoChiController();
            CuahangNongduoc.ResponsiveLayout.Attach(this);
            base.dataGridView = this.dataGridView;
            base.bindingNavigator = this.bindingNavigator;
            base.bindingNavigatorPositionItem = this.bindingNavigatorPositionItem;
        }

        private void frmLyDoChi_Load(object sender, EventArgs e)
        {
            Base_Load(sender, e);
        }

        private void toolLuu_Click(object sender, EventArgs e)
        {
            Luu();
            LoadData();
            bindingNavigator.BindingSource.MoveLast();
        }

        private void toolThoat_Click(object sender, EventArgs e)
        {
            Thoat();
        }

        private void bindingNavigatorDeleteItem_Click(object sender, EventArgs e)
        {
            XoaDongHienTai();
        }

        private void dataGridView_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            HuyXoaDong(e);
        }
    }
}