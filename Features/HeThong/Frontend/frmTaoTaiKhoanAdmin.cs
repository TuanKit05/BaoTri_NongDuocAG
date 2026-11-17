using System;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public partial class frmTaoTaiKhoanAdmin : Form
    {
        private AccountRepository _repository;

        public frmTaoTaiKhoanAdmin()
        {
            InitializeComponent();
            txtUsername.Text = "admin";
        }

        public frmTaoTaiKhoanAdmin(AccountRepository repository) : this()
        {
            _repository = repository;
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            try
            {
                if (_repository == null)
                    throw new InvalidOperationException("Không tìm thấy dịch vụ tạo tài khoản.");
                if (txtPassword.Text != txtConfirm.Text)
                    throw new InvalidOperationException("Mật khẩu nhập lại không khớp.");
                _repository.CreateInitialAdmin(txtUsername.Text, txtFullName.Text, txtPassword.Text);
                MessageBox.Show("Tạo tài khoản quản trị thành công. Bạn có thể đăng nhập.",
                    "Thiết lập ban đầu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không thể tạo tài khoản quản trị", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
