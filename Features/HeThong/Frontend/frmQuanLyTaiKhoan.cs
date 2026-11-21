using System;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public partial class frmQuanLyTaiKhoan : Form
    {
        private AccountRepository _repository;
        private System.Data.DataTable _roleTable;

        public frmQuanLyTaiKhoan()
        {
            InitializeComponent();
            CuahangNongduoc.ResponsiveLayout.Attach(this);
        }

        public frmQuanLyTaiKhoan(AccountRepository repository) : this()
        {
            _repository = repository;
            LoadRoles();
            LoadAccounts();
        }

        private string SelectedRole()
        {
            if (cboRole.SelectedIndex < 0 || cboRole.SelectedValue == null)
                throw new InvalidOperationException("Vui lòng chọn vai trò.");
            return Convert.ToString(cboRole.SelectedValue);
        }

        private void LoadRoles()
        {
            if (_repository == null) return;
            cboRole.DataSource = null;
            _roleTable = _repository.GetRoles();
            cboRole.DataSource = _roleTable;
            cboRole.DisplayMember = "TenVaiTro";
            cboRole.ValueMember = "MaVaiTro";
            cboRole.SelectedIndex = -1;
            btnCreate.Enabled = btnRole.Enabled = _roleTable.Rows.Count > 0;
        }

        private void SelectRole(string role)
        {
            cboRole.SelectedIndex = -1;
            string[] codes = AccountRoles.Split(role);
            if (codes.Length == 1) cboRole.SelectedValue = codes[0];
        }

        private string SelectedAccountId()
        {
            if (grid.CurrentRow == null || grid.CurrentRow.Cells["MaTaiKhoan"].Value == null)
                throw new InvalidOperationException("Vui lòng chọn một tài khoản trong danh sách.");
            return Convert.ToString(grid.CurrentRow.Cells["MaTaiKhoan"].Value);
        }

        private void LoadAccounts()
        {
            if (_repository == null) return;
            grid.DataSource = _repository.GetAllAccounts();
            if (grid.Columns.Contains("VaiTro")) grid.Columns["VaiTro"].Visible = false;
            if (grid.Columns.Contains("TenVaiTro")) grid.Columns["TenVaiTro"].HeaderText = "Vai trò";
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
            SelectRole(role);
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
                string id = SelectedAccountId();
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
                string id = SelectedAccountId();
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

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            try { LoadRoles(); LoadAccounts(); }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không thể tải danh sách tài khoản và vai trò",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void btnClose_Click(object sender, EventArgs e) { this.Close(); }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Nhân viên nghỉ việc được khóa tài khoản để giữ lịch sử. Dùng nút Khóa / Mở khóa.", "Quản lý tài khoản");
        }

        private void frmQuanLyTaiKhoan_Load(object sender, EventArgs e)
        {

        }
    }
}
