using System;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public partial class frmDoiMatKhau : Form
    {
        private AccountRepository _repository;
        private bool _firstLogin;
        private System.Drawing.Size _compactClientSize;

        public frmDoiMatKhau()
        {
            InitializeComponent();
            CuahangNongduoc.ResponsiveLayout.Attach(this);
            _compactClientSize = ClientSize;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            AcceptButton = btnSave;
            CancelButton = btnCancel;
            Shown += delegate { RestoreCompactLayout(); };
        }

        public frmDoiMatKhau(AccountRepository repository, bool firstLogin) : this()
        {
            _repository = repository;
            _firstLogin = firstLogin;
            lblTitle.Text = _firstLogin ? "Đổi mật khẩu lần đăng nhập đầu" : "Đổi mật khẩu tài khoản";
        }

        internal void RestoreCompactLayout()
        {
            if (MdiParent == null || IsDisposed) return;
            WindowState = FormWindowState.Normal;
            ClientSize = _compactClientSize;
            foreach (Control control in MdiParent.Controls)
            {
                MdiClient content = control as MdiClient;
                if (content == null) continue;
                Location = new System.Drawing.Point(
                    Math.Max(0, (content.ClientSize.Width - Width) / 2),
                    Math.Max(0, (content.ClientSize.Height - Height) / 2));
                break;
            }
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
                MessageBox.Show(ErrorLog.UserMessage(ex, "Không thể hoàn tất thao tác"), "Không thể đổi mật khẩu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
