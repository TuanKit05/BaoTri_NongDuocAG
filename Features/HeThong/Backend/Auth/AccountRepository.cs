using System;
using System.Data;
using System.Data.OleDb;

namespace CuahangNongduoc
{
    public class AccountRepository
    {
        private OleDbConnection CreateConnection()
        {
            return new OleDbConnection(DataService.m_ConnectString);
        }

        public void EnsureSchema()
        {
            using (OleDbConnection connection = CreateConnection())
            {
                connection.Open();
                if (!TableExists(connection, "TaiKhoan"))
                {
                    OleDbCommand create = connection.CreateCommand();
                    create.CommandText = "CREATE TABLE [TaiKhoan] (" +
                        "[MaTaiKhoan] COUNTER CONSTRAINT [PK_TaiKhoan] PRIMARY KEY, " +
                        "[TenDangNhap] TEXT(50) NOT NULL, " +
                        "[MatKhauHash] TEXT(64) NOT NULL, " +
                        "[Salt] TEXT(32) NOT NULL, " +
                        "[HoTen] TEXT(100), " +
                        "[VaiTro] TEXT(20) NOT NULL, " +
                        "[TrangThai] YESNO NOT NULL, " +
                        "[DoiMatKhau] YESNO NOT NULL)";
                    create.ExecuteNonQuery();

                    OleDbCommand index = connection.CreateCommand();
                    index.CommandText = "CREATE UNIQUE INDEX [UX_TaiKhoan_TenDangNhap] " +
                        "ON [TaiKhoan] ([TenDangNhap])";
                    index.ExecuteNonQuery();
                }
                // Bổ sung cột DaXoa nếu cơ sở dữ liệu chưa có
                if (!ColumnExists(connection, "TaiKhoan", "DaXoa"))
                {
                    OleDbCommand addColumn = connection.CreateCommand();
                    addColumn.CommandText =
                        "ALTER TABLE [TaiKhoan] ADD COLUMN [DaXoa] YESNO";
                    addColumn.ExecuteNonQuery();
                }

                // Tài khoản cũ mặc định chưa bị xóa
                OleDbCommand updateDeleted = connection.CreateCommand();
                updateDeleted.CommandText =
                    "UPDATE [TaiKhoan] SET [DaXoa] = False WHERE [DaXoa] IS NULL";
                updateDeleted.ExecuteNonQuery();

            }
        }

        public int GetAccountCount()
        {
            using (OleDbConnection connection = CreateConnection())
            {
                connection.Open();
                OleDbCommand command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM [TaiKhoan]";
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        public void CreateInitialAdmin(string username, string fullName, string password)
        {
            ValidateNewPassword(password);
            username = username == null ? "" : username.Trim();
            fullName = fullName == null ? "" : fullName.Trim();
            if (username.Length == 0)
                throw new ArgumentException("Tên đăng nhập không được để trống.");
            if (username.Length > 50 || fullName.Length > 100)
                throw new ArgumentException("Tên đăng nhập tối đa 50 ký tự, họ tên tối đa 100 ký tự.");
            if (FindByUsername(username) != null)
                throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");

            InsertAccount(username, fullName, password, AccountRoles.Admin, false);
        }

        private bool TableExists(OleDbConnection connection, string tableName)
        {
            DataTable tables = connection.GetSchema("Tables");
            int i;
            for (i = 0; i < tables.Rows.Count; i++)
            {
                string name = Convert.ToString(tables.Rows[i]["TABLE_NAME"]);
                if (String.Compare(name, tableName, true) == 0)
                    return true;
            }
            return false;
        }
        private bool ColumnExists(
    OleDbConnection connection,
    string tableName,
    string columnName)
        {
            DataTable columns = connection.GetOleDbSchemaTable(
                OleDbSchemaGuid.Columns,
                new object[] { null, null, tableName, columnName });

            return columns != null && columns.Rows.Count > 0;
        }
        public UserAccount FindByUsername(string username)
        {
            using (OleDbConnection connection = CreateConnection())
            {
                connection.Open();
                OleDbCommand command = connection.CreateCommand();
                command.CommandText = "SELECT [MaTaiKhoan], [TenDangNhap], [MatKhauHash], [Salt], " +
                    "[HoTen], [VaiTro], [TrangThai], [DoiMatKhau], [DaXoa] " +
                    "FROM [TaiKhoan] WHERE [TenDangNhap] = ?";
                AddText(command, username.Trim(), 50);
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                        return ReadAccount(reader);
                }
            }
            return null;
        }

        public UserAccount GetById(int id)
        {
            using (OleDbConnection connection = CreateConnection())
            {
                connection.Open();
                OleDbCommand command = connection.CreateCommand();
                command.CommandText = "SELECT [MaTaiKhoan], [TenDangNhap], [MatKhauHash], [Salt], " +
                    "[HoTen], [VaiTro], [TrangThai], [DoiMatKhau], [DaXoa] " +
                    "FROM [TaiKhoan] WHERE [MaTaiKhoan] = ?";
                command.Parameters.Add("?", OleDbType.Integer).Value = id;
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                        return ReadAccount(reader);
                }
            }
            return null;
        }

        private UserAccount ReadAccount(OleDbDataReader reader)
        {
            UserAccount account = new UserAccount();
            account.Id = Convert.ToInt32(reader["MaTaiKhoan"]);
            account.Username = Convert.ToString(reader["TenDangNhap"]);
            account.PasswordHash = Convert.ToString(reader["MatKhauHash"]);
            account.Salt = Convert.ToString(reader["Salt"]);
            account.FullName = Convert.ToString(reader["HoTen"]);
            account.Role = Convert.ToString(reader["VaiTro"]);
            account.IsActive = Convert.ToBoolean(reader["TrangThai"]);
            account.MustChangePassword = Convert.ToBoolean(reader["DoiMatKhau"]);
            account.IsDeleted = Convert.ToBoolean(reader["DaXoa"]);
            return account;
        }

        public DataTable GetAllAccounts()
        {
            using (OleDbConnection connection = CreateConnection())
            {
                connection.Open();
                OleDbCommand command = connection.CreateCommand();
                command.CommandText = "SELECT [MaTaiKhoan], [TenDangNhap], [HoTen], [VaiTro], " +
    "[TrangThai], [DoiMatKhau] FROM [TaiKhoan] " +
    "WHERE [DaXoa] = False ORDER BY [TenDangNhap]";
                OleDbDataAdapter adapter = new OleDbDataAdapter(command);
                DataTable table = new DataTable();
                adapter.Fill(table);
                return table;
            }
        }

        public void CreateAccount(string username, string fullName, string password, string role)
        {
            ValidateNewPassword(password);
            username = username == null ? "" : username.Trim();
            fullName = fullName == null ? "" : fullName.Trim();
            if (username.Length == 0)
                throw new ArgumentException("Tên đăng nhập không được để trống.");
            if (username.Length > 50 || fullName.Length > 100)
                throw new ArgumentException("Tên đăng nhập tối đa 50 ký tự, họ tên tối đa 100 ký tự.");
            if (!AccountRoles.IsValid(role))
                throw new ArgumentException("Vai trò không hợp lệ.");
            if (FindByUsername(username) != null)
                throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");

            InsertAccount(username, fullName, password, role, true);
        }

        private void InsertAccount(string username, string fullName,
    string password, string role, bool mustChangePassword)
        {
            string salt = PasswordHasher.CreateSalt();
            string hash = PasswordHasher.HashPassword(password, salt);

            using (OleDbConnection connection = CreateConnection())
            {
                connection.Open();

                OleDbCommand command = connection.CreateCommand();

                command.CommandText = "INSERT INTO [TaiKhoan] " +
                    "([TenDangNhap], [MatKhauHash], [Salt], [HoTen], " +
                    "[VaiTro], [TrangThai], [DoiMatKhau], [DaXoa]) " +
                    "VALUES (?, ?, ?, ?, ?, ?, ?, ?)";

                AddText(command, username, 50);
                AddText(command, hash, 64);
                AddText(command, salt, 32);
                AddText(command, fullName, 100);
                AddText(command, role, 20);

                command.Parameters.Add("?", OleDbType.Boolean).Value = true;
                command.Parameters.Add("?", OleDbType.Boolean).Value = mustChangePassword;
                command.Parameters.Add("?", OleDbType.Boolean).Value = false;

                command.ExecuteNonQuery();
            }
        }

        public void UpdateRole(int id, string role)
        {
            if (!AccountRoles.IsValid(role))
                throw new ArgumentException("Vai trò không hợp lệ.");
            UserAccount account = GetById(id);
            if (account == null)
                throw new InvalidOperationException("Không tìm thấy tài khoản.");
            if (account.IsActive && account.Role == AccountRoles.Admin && role != AccountRoles.Admin && CountActiveAdmins() <= 1)
                throw new InvalidOperationException("Không thể hạ quyền Admin cuối cùng đang hoạt động.");

            using (OleDbConnection connection = CreateConnection())
            {
                connection.Open();
                OleDbCommand command = connection.CreateCommand();
                command.CommandText = "UPDATE [TaiKhoan] SET [VaiTro] = ? WHERE [MaTaiKhoan] = ?";
                AddText(command, role, 20);
                command.Parameters.Add("?", OleDbType.Integer).Value = id;
                command.ExecuteNonQuery();
            }
        }

        public void SetActive(int id, bool active)
        {
            UserAccount account = GetById(id);
            if (account == null)
                throw new InvalidOperationException("Không tìm thấy tài khoản.");
            if (account.Role == AccountRoles.Admin && !active && CountActiveAdmins() <= 1)
                throw new InvalidOperationException("Không thể khóa Admin cuối cùng đang hoạt động.");

            using (OleDbConnection connection = CreateConnection())
            {
                connection.Open();
                OleDbCommand command = connection.CreateCommand();
                command.CommandText = "UPDATE [TaiKhoan] SET [TrangThai] = ? WHERE [MaTaiKhoan] = ?";
                command.Parameters.Add("?", OleDbType.Boolean).Value = active;
                command.Parameters.Add("?", OleDbType.Integer).Value = id;
                command.ExecuteNonQuery();
            }
        }

        public void DeleteAccount(int id)
        {
            UserAccount account = GetById(id);

            if (account == null || account.IsDeleted)
                throw new InvalidOperationException("Không tìm thấy tài khoản.");

            // Không cho xóa Admin cuối cùng đang hoạt động
            if (account.Role == AccountRoles.Admin &&
                account.IsActive && CountActiveAdmins() <= 1)
            {
                throw new InvalidOperationException(
                    "Không thể xóa Admin cuối cùng đang hoạt động.");
            }

            using (OleDbConnection connection = CreateConnection())
            {
                connection.Open();

                OleDbCommand command = connection.CreateCommand();

                command.CommandText =
                    "UPDATE [TaiKhoan] SET [DaXoa] = ?, [TrangThai] = ? " +
                    "WHERE [MaTaiKhoan] = ? AND [DaXoa] = False";

                command.Parameters.Add("?", OleDbType.Boolean).Value = true;
                command.Parameters.Add("?", OleDbType.Boolean).Value = false;
                command.Parameters.Add("?", OleDbType.Integer).Value = id;

                if (command.ExecuteNonQuery() == 0)
                    throw new InvalidOperationException(
                        "Không thể xóa tài khoản.");
            }
        }

        public void ResetPassword(int id, string newPassword)
        {
            ValidateNewPassword(newPassword);
            UpdatePassword(id, newPassword, true);
        }

        public void ChangeOwnPassword(UserAccount account, string oldPassword, string newPassword)
        {
            ValidateNewPassword(newPassword);
            UserAccount current = GetById(account.Id);
            if (current == null || !current.IsActive)
                throw new InvalidOperationException("Tài khoản không còn hoạt động.");
            if (!PasswordHasher.VerifyPassword(oldPassword, current.Salt, current.PasswordHash))
                throw new InvalidOperationException("Mật khẩu hiện tại không chính xác.");
            UpdatePassword(account.Id, newPassword, false);
            account.MustChangePassword = false;
        }

        private void UpdatePassword(int id, string password, bool mustChange)
        {
            string salt = PasswordHasher.CreateSalt();
            string hash = PasswordHasher.HashPassword(password, salt);
            using (OleDbConnection connection = CreateConnection())
            {
                connection.Open();
                OleDbCommand command = connection.CreateCommand();
                command.CommandText = "UPDATE [TaiKhoan] SET [MatKhauHash] = ?, [Salt] = ?, " +
                    "[DoiMatKhau] = ? WHERE [MaTaiKhoan] = ?";
                AddText(command, hash, 64);
                AddText(command, salt, 32);
                command.Parameters.Add("?", OleDbType.Boolean).Value = mustChange;
                command.Parameters.Add("?", OleDbType.Integer).Value = id;
                if (command.ExecuteNonQuery() == 0)
                    throw new InvalidOperationException("Không tìm thấy tài khoản cần cập nhật.");
            }
        }

        private int CountActiveAdmins()
        {
            using (OleDbConnection connection = CreateConnection())
            {
                connection.Open();
                OleDbCommand command = connection.CreateCommand();
                command.CommandText =
                  "SELECT COUNT(*) FROM [TaiKhoan] " +
                  "WHERE [VaiTro] = ? AND [TrangThai] = ? AND [DaXoa] = False";
                AddText(command, AccountRoles.Admin, 20);
                command.Parameters.Add("?", OleDbType.Boolean).Value = true;
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private void AddText(OleDbCommand command, string value, int size)
        {
            command.Parameters.Add("?", OleDbType.VarWChar, size).Value = value;
        }

        public static void ValidateNewPassword(string password)
        {
            if (password == null || password.Length < 8)
                throw new ArgumentException("Mật khẩu phải có ít nhất 8 ký tự.");
        }
    }
}
