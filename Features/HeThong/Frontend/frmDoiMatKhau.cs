using System;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public partial class frmDoiMatKhau : Form
    {
        private AccountRepository _repository;
        private bool _firstLogin;

        public frmDoiMatKhau()
        {
            InitializeComponent();
            CuahangNongduoc.ResponsiveLayout.Attach(this);
        }

        public frmDoiMatKhau(AccountRepository repository, bool firstLogin) : this()
        {
            _repository = repository;
            _firstLogin = firstLogin;
            lblTitle.Text = _firstLogin ? "Đổi mật khẩu lần đăng nhập đầu" : "Đổi mật khẩu tài khoản";
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (_repository == null)
                    throw new InvalidOperationException("Không tìm thấy dịch vụ đổi mật khẩu.");
                if (AppSession.CurrentUser == null)
                    throw new InvalidOperationException("Phiên đăng nhập không còn hợp lệ.");
                if (txtNewPassword.Text != txtConfirmPassword.Text)
                    throw new InvalidOperationException("Mật khẩu nhập lại không khớp.");

                _repository.ChangeOwnPassword(AppSession.CurrentUser, txtOldPassword.Text, txtNewPassword.Text);
                MessageBox.Show("Đổi mật khẩu thành công.", "Đổi mật khẩu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không thể đổi mật khẩu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
