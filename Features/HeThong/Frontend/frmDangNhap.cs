using System;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public partial class frmDangNhap : Form
    {
        private AuthService _authService;
        private AccountRepository _repository;

        // Constructor không tham số để Visual Studio mở được Form Designer.
        public frmDangNhap()
        {
            InitializeComponent();
            CuahangNongduoc.ResponsiveLayout.Attach(this);
            AcceptButton = btnLogin;
        }

        public frmDangNhap(AuthService authService, AccountRepository repository) : this()
        {
            _authService = authService;
            _repository = repository;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            try
            {
                if (_authService == null || _repository == null)
                    throw new InvalidOperationException("Form chưa được khởi tạo dịch vụ đăng nhập.");

                LoginContext result = _authService.Login(txtUsername.Text, txtPassword.Text);
                if (!result.Success)
                {
                    MessageBox.Show(result.ErrorMessage, "Đăng nhập", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPassword.SelectAll();
                    txtPassword.Focus();
                    return;
                }

                result.Account.LegacyPassword = null;
                result.Account.PasswordHash = null;
                result.Account.Salt = null;
                result.Password = null;
                AppSession.CurrentUser = result.Account;

                if (result.Account.MustChangePassword)
                {
                    using (frmDoiMatKhau changeForm = new frmDoiMatKhau(_repository, true))
                    {
                        if (changeForm.ShowDialog(this) != DialogResult.OK)
                        {
                            AppSession.CurrentUser = null;
                            MessageBox.Show("Bạn phải đổi mật khẩu để tiếp tục sử dụng hệ thống.",
                                "Đổi mật khẩu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            txtPassword.Clear();
                            return;
                        }
                    }
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể đăng nhập. Vui lòng kiểm tra kết nối cơ sở dữ liệu.\r\n" + ex.Message,
                    "Lỗi đăng nhập", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
