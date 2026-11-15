using System;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public partial class frmQuanLyTaiKhoan : Form
    {
        private AccountRepository _repository;

        public frmQuanLyTaiKhoan()
        {
            InitializeComponent();
        }

        public frmQuanLyTaiKhoan(AccountRepository repository) : this()
        {
            _repository = repository;
            LoadAccounts();
        }

        private string SelectedRole()
        {
            string value = Convert.ToString(cboRole.SelectedItem);
            int separator = value.IndexOf(" - ");
            if (separator >= 0) value = value.Substring(0, separator);
            return value;
        }

        private int SelectedAccountId()
        {
            if (grid.CurrentRow == null || grid.CurrentRow.Cells["MaTaiKhoan"].Value == null)
                throw new InvalidOperationException("Vui lòng chọn một tài khoản trong danh sách.");
            return Convert.ToInt32(grid.CurrentRow.Cells["MaTaiKhoan"].Value);
        }

        private void LoadAccounts()
        {
            if (_repository == null) return;
            grid.DataSource = _repository.GetAllAccounts();
            if (grid.Columns.Contains("MaTaiKhoan")) grid.Columns["MaTaiKhoan"].HeaderText = "Mã";
            if (grid.Columns.Contains("TenDangNhap")) grid.Columns["TenDangNhap"].HeaderText = "Tên đăng nhập";
            if (grid.Columns.Contains("HoTen")) grid.Columns["HoTen"].HeaderText = "Họ tên";
            if (grid.Columns.Contains("VaiTro")) grid.Columns["VaiTro"].HeaderText = "Vai trò";
            if (grid.Columns.Contains("TrangThai")) grid.Columns["TrangThai"].HeaderText = "Đang hoạt động";
            if (grid.Columns.Contains("DoiMatKhau")) grid.Columns["DoiMatKhau"].HeaderText = "Buộc đổi mật khẩu";
        }

        private void grid_SelectionChanged(object sender, EventArgs e)
        {
            if (grid.CurrentRow == null || !grid.Columns.Contains("VaiTro")) return;
            if (grid.CurrentRow.Cells["VaiTro"].Value == null) return;
            string role = Convert.ToString(grid.CurrentRow.Cells["VaiTro"].Value);
            int i;
            for (i = 0; i < cboRole.Items.Count; i++)
            {
                if (Convert.ToString(cboRole.Items[i]).StartsWith(role + " - "))
                {
                    cboRole.SelectedIndex = i;
                    break;
                }
            }
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            try
            {
                _repository.CreateAccount(txtUsername.Text, txtFullName.Text, txtPassword.Text, SelectedRole());
                MessageBox.Show("Tạo tài khoản thành công. Người dùng phải đổi mật khẩu khi đăng nhập.",
                    "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtUsername.Clear(); txtFullName.Clear(); txtPassword.Clear(); LoadAccounts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không thể tạo tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnRole_Click(object sender, EventArgs e)
        {
            try
            {
                int id = SelectedAccountId();
                UserAccount selected = _repository.GetById(id);
                if (selected == null) throw new InvalidOperationException("Không tìm thấy tài khoản.");
                if (AppSession.CurrentUser != null && selected.Id == AppSession.CurrentUser.Id)
                    throw new InvalidOperationException("Không thể tự thay đổi vai trò của tài khoản đang đăng nhập.");
                _repository.UpdateRole(id, SelectedRole());
                LoadAccounts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không thể cập nhật vai trò", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnToggle_Click(object sender, EventArgs e)
        {
            try
            {
                int id = SelectedAccountId();
                UserAccount selected = _repository.GetById(id);
                if (selected == null) throw new InvalidOperationException("Không tìm thấy tài khoản.");
                if (AppSession.CurrentUser != null && selected.Id == AppSession.CurrentUser.Id)
                    throw new InvalidOperationException("Không thể tự khóa tài khoản đang đăng nhập.");
                _repository.SetActive(id, !selected.IsActive);
                LoadAccounts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không thể đổi trạng thái", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            try
            {
                _repository.ResetPassword(SelectedAccountId(), txtPassword.Text);
                MessageBox.Show("Đặt lại mật khẩu thành công. Người dùng sẽ phải đổi mật khẩu mới khi đăng nhập.",
                    "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtPassword.Clear(); LoadAccounts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không thể đặt lại mật khẩu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e) { LoadAccounts(); }
        private void btnClose_Click(object sender, EventArgs e) { this.Close(); }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                int id = SelectedAccountId();

                UserAccount selected = _repository.GetById(id);

                if (selected == null || selected.IsDeleted)
                    throw new InvalidOperationException(
                        "Không tìm thấy tài khoản.");

                if (AppSession.CurrentUser != null &&
                    selected.Id == AppSession.CurrentUser.Id)
                {
                    throw new InvalidOperationException(
                        "Không thể xóa tài khoản đang đăng nhập.");
                }

                DialogResult result = MessageBox.Show(
                    "Bạn có chắc muốn xóa tài khoản: " +
                    selected.Username + "?\n\n" +
                    "Tài khoản sẽ không thể đăng nhập nữa.",
                    "Xác nhận xóa tài khoản",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                    return;

                _repository.DeleteAccount(id);

                MessageBox.Show(
                    "Xóa tài khoản thành công!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                LoadAccounts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Không thể xóa tài khoản",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
