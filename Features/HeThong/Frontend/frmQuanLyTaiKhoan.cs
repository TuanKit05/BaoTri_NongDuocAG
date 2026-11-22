using System;
using System.Data;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public partial class frmQuanLyTaiKhoan : Form
    {
        private const string DefaultPassword = "12345678";
        private AccountRepository _repository;
        private readonly BindingSource _accounts = new BindingSource();
        private enum EditMode { View, Add, Edit }
        private EditMode _mode;
        private string _editingId;

        public frmQuanLyTaiKhoan()
        {
            InitializeComponent();
            btnLuu.Click += btnSave_Click;
            lblPassword.AutoSize = true;
            grid.DataSource = _accounts;
            _accounts.CurrentChanged += grid_SelectionChanged;
            Disposed += FormDisposed;
            Load += FormLoaded;
            CuahangNongduoc.ResponsiveLayout.Attach(this);
            AcceptButton = btnLuu;
            SetMode(EditMode.View);
        }

        public frmQuanLyTaiKhoan(AccountRepository repository) : this()
        {
            _repository = repository;
            ReloadData();
        }

        private void FormLoaded(object sender, EventArgs e)
        {
            if (_repository != null) return;

            try
            {
                _repository = new AccountRepository();
                ReloadData();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void FormDisposed(object sender, EventArgs e)
        {
            _accounts.Dispose();
        }

        private void ReloadData()
        {
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
            cboRole.DataSource = null;
            DataTable roles = _repository.GetRoles();
            cboRole.DisplayMember = "TenVaiTro";
            cboRole.ValueMember = "MaVaiTro";
            cboRole.DataSource = roles;
            cboRole.SelectedIndex = -1;
        }

        private DataRowView CurrentAccount { get { return _accounts.Current as DataRowView; } }

        private string SelectedAccountId()
        {
            if (CurrentAccount == null) throw new InvalidOperationException("Vui lòng chọn tài khoản.");
            return Convert.ToString(CurrentAccount["MaTaiKhoan"]);
        }

        private void LoadAccounts()
        {
            if (_repository == null) return;
            string id = CurrentAccount == null ? null : SelectedAccountId();
            _accounts.DataSource = _repository.GetAllAccounts();
            int position = id == null ? -1 : _accounts.Find("MaTaiKhoan", id);
            if (position >= 0) _accounts.Position = position;
            ConfigureGridColumns();
            DisplaySelectedAccount();
        }

        private void ConfigureGridColumns()
        {
            if (grid.Columns.Contains("VaiTro")) grid.Columns["VaiTro"].Visible = false;
            if (grid.Columns.Contains("DoiMatKhau")) grid.Columns["DoiMatKhau"].Visible = false;
            string[] fields = { "MaTaiKhoan", "TenDangNhap", "HoTen", "TrangThai", "TenVaiTro" };
            string[] titles = { "Mã", "Tên đăng nhập", "Họ tên", "Đang hoạt động", "Vai trò" };
            for (int i = 0; i < fields.Length; i++)
                if (grid.Columns.Contains(fields[i])) grid.Columns[fields[i]].HeaderText = titles[i];
        }

        private void SetMode(EditMode mode)
        {
            _mode = mode;
            bool viewing = mode == EditMode.View;
            bool hasAccount = CurrentAccount != null;
            grid.Enabled = viewing;
            grid.ReadOnly = true;
            txtUsername.Enabled = mode == EditMode.Add;
            txtFullName.Enabled = !viewing;
            txtUsername.ReadOnly = mode != EditMode.Add;
            txtFullName.ReadOnly = viewing;
            cboRole.Enabled = !viewing;
            btnLuu.Enabled = !viewing;
            btnThem.Enabled = viewing;
            btnSua.Enabled = viewing && hasAccount;
            btnKhoaTaiKhoan.Enabled = viewing && hasAccount;
            bool active = CurrentAccount != null && Convert.ToBoolean(CurrentAccount["TrangThai"]);
            btnKhoaTaiKhoan.Text = active ? "Kh\u00f3a t\u00e0i kho\u1ea3n" : "M\u1edf t\u00e0i kho\u1ea3n";
            btnReset.Enabled = viewing && hasAccount;
            btnRefresh.Text = viewing ? "Làm mới" : "Hủy";
        }

        private void grid_SelectionChanged(object sender, EventArgs e)
        {
            DisplaySelectedAccount();
        }

        private void DisplaySelectedAccount()
        {
            if (_mode != EditMode.View) return;
            DataRowView current = CurrentAccount;
            txtUsername.Text = current == null ? "" : Convert.ToString(current["TenDangNhap"]);
            txtFullName.Text = current == null ? "" : Convert.ToString(current["HoTen"]);
            cboRole.SelectedIndex = -1;
            if (current != null)
            {
                string[] codes = AccountRoles.Split(Convert.ToString(current["VaiTro"]));
                if (codes.Length == 1) cboRole.SelectedValue = codes[0];
            }
            SetMode(EditMode.View);
        }

        private void btnThem_Click(object sender, EventArgs e)
        {
            SetMode(EditMode.Add);
            _editingId = null;
            grid.ClearSelection();
            txtUsername.Clear();
            txtFullName.Clear();
            cboRole.SelectedIndex = -1;
            txtUsername.Focus();
        }

        private void btnSua_Click(object sender, EventArgs e)
        {
            if (CurrentAccount == null) return;
            _editingId = SelectedAccountId();
            SetMode(EditMode.Edit);
            txtFullName.Focus();
        }

        private void SaveAccount()
        {
            if (_mode == EditMode.View) return;
            string username = txtUsername.Text.Trim();
            if (_mode == EditMode.Add)
            {
                _repository.CreateAccount(username, txtFullName.Text, DefaultPassword, SelectedRole());
            }
            else
            {
                _repository.UpdateAccount(_editingId, txtFullName.Text, SelectedRole());
            }
            SetMode(EditMode.View);
            LoadAccounts();
            int position = _accounts.Find("TenDangNhap", username);
            if (position >= 0) _accounts.Position = position;
            DisplaySelectedAccount();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                SaveAccount();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void btnKhoaTaiKhoan_Click(object sender, EventArgs e)
        {
            try
            {
                string id = SelectedAccountId();
                UserAccount selected = _repository.GetById(id);
                if (selected == null) throw new InvalidOperationException("Không tìm thấy tài khoản.");
                _repository.SetActive(id, !selected.IsActive);
                LoadAccounts();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            try
            {
                ResetSelectedPassword();
                MessageBox.Show("Đã đặt lại mật khẩu. Người dùng phải đổi mật khẩu khi đăng nhập.", "Quản lý tài khoản");
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                SetMode(EditMode.View);
                ReloadData();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }
        private void ResetSelectedPassword()
        {
            _repository.ResetPassword(SelectedAccountId(), DefaultPassword);
        }

        private void ShowError(Exception ex)
        {
            MessageBox.Show(ErrorLog.UserMessage(ex, "Không thể hoàn tất thao tác"), "Không thể lưu thay đổi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        
        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
