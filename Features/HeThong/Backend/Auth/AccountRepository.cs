using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;

namespace CuahangNongduoc
{
    public class AccountRepository
    {
        private OleDbConnection CreateConnection() { return new OleDbConnection(DataService.m_ConnectString); }

        public void EnsureSchema()
        {
            using (OleDbConnection c = CreateConnection())
            {
                c.Open();
                if (!TableExists(c, "TAI_KHOAN")) throw new InvalidOperationException("Không tìm thấy bảng TAI_KHOAN.");
                bool migration = !TableExists(c, "TAI_KHOAN_VAI_TRO");
                if (migration)
                {
                    string source = new OleDbConnectionStringBuilder(DataService.m_ConnectString).DataSource;
                    string folder = Path.Combine(Path.GetDirectoryName(source), "Backups");
                    Directory.CreateDirectory(folder);
                    File.Copy(source, Path.Combine(folder, "cuahang_truoc_phan_quyen_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".mdb"));
                }
                AddColumn(c, "TAI_KHOAN", "MatKhauHash", "TEXT(64)");
                AddColumn(c, "TAI_KHOAN", "Salt", "TEXT(32)");
                AddColumn(c, "TAI_KHOAN", "DoiMatKhau", "YESNO");
                AddColumn(c, "TAI_KHOAN", "DaXoa", "YESNO");
                AddColumn(c, "PHIEU_BAN", "ID_NHAN_VIEN", "TEXT(50)");
                CreateTable(c, "VAI_TRO", "[MaVaiTro] TEXT(20) PRIMARY KEY, [TenVaiTro] TEXT(100) NOT NULL");
                CreateTable(c, "QUYEN", "[MaQuyen] TEXT(50) PRIMARY KEY, [TenQuyen] TEXT(100)");
                bool newMapping = !TableExists(c, "VAI_TRO_QUYEN");
                CreateTable(c, "VAI_TRO_QUYEN", "[MaVaiTro] TEXT(20), [MaQuyen] TEXT(50), PRIMARY KEY ([MaVaiTro], [MaQuyen])");
                CreateTable(c, "TAI_KHOAN_VAI_TRO", "[ID_TAI_KHOAN] TEXT(50), [MaVaiTro] TEXT(20), PRIMARY KEY ([ID_TAI_KHOAN], [MaVaiTro])");
                CreateTable(c, "NHAT_KY", "[ID] COUNTER PRIMARY KEY, [THOI_GIAN] DATETIME, [ID_TAI_KHOAN] TEXT(50), [THAO_TAC] TEXT(50), [DOI_TUONG] TEXT(100), [NOI_DUNG] MEMO");
                using (OleDbTransaction tr = c.BeginTransaction())
                {
                    try
                    {
                        Seed(c, tr, "VAI_TRO", "MaVaiTro", "TenVaiTro", "ADMIN", "Quản lý");
                        Seed(c, tr, "VAI_TRO", "MaVaiTro", "TenVaiTro", "KHO", "Thủ kho");
                        Seed(c, tr, "VAI_TRO", "MaVaiTro", "TenVaiTro", "BANHANG", "Bán hàng");
                        Seed(c, tr, "VAI_TRO", "MaVaiTro", "TenVaiTro", "KETOAN", "Kế toán");
                        foreach (string permission in PermissionCatalog.All)
                            Seed(c, tr, "QUYEN", "MaQuyen", "TenQuyen", permission, permission);
                        if (newMapping)
                            foreach (string role in new string[] { "ADMIN", "KHO", "BANHANG", "KETOAN" })
                                foreach (string permission in PermissionCatalog.ForRole(role))
                                    Execute(c, tr, "INSERT INTO VAI_TRO_QUYEN (MaVaiTro, MaQuyen) VALUES (?, ?)", role, permission);
                        DataTable accounts = ReadTable(c, tr, "SELECT * FROM TAI_KHOAN");
                        foreach (DataRow row in accounts.Rows)
                        {
                            string id = Convert.ToString(row["ID"]);
                            if (Convert.ToInt32(Scalar(c, tr, "SELECT COUNT(*) FROM TAI_KHOAN_VAI_TRO WHERE ID_TAI_KHOAN=?", id)) == 0)
                            {
                                foreach (string role in AccountRoles.Split(Convert.ToString(row["VAI_TRO"])))
                                    if (Convert.ToInt32(Scalar(c, tr, "SELECT COUNT(*) FROM VAI_TRO WHERE MaVaiTro=?", role)) > 0)
                                        Execute(c, tr, "INSERT INTO TAI_KHOAN_VAI_TRO (ID_TAI_KHOAN, MaVaiTro) VALUES (?, ?)", id, role);
                            }
                            if (String.IsNullOrEmpty(Convert.ToString(row["MatKhauHash"])) && !String.IsNullOrEmpty(Convert.ToString(row["MAT_KHAU"])))
                            {
                                string salt = PasswordHasher.CreateSalt();
                                Execute(c, tr, "UPDATE TAI_KHOAN SET MatKhauHash=?, Salt=?, MAT_KHAU=NULL WHERE ID=?",
                                    PasswordHasher.HashPassword(Convert.ToString(row["MAT_KHAU"]), salt), salt, id);
                            }
                        }
                        tr.Commit();
                    }
                    catch { ErrorLog.Rollback(tr, "Rollback Access"); throw; }
                }
                IdGenerator.EnsureSchema(c);
                WarehouseSettings.EnsureSchema(c);
            }
        }

        private static bool TableExists(OleDbConnection c, string name)
        {
            foreach (DataRow row in c.GetSchema("Tables").Rows)
                if (String.Equals(Convert.ToString(row["TABLE_NAME"]), name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
        private static void CreateTable(OleDbConnection c, string name, string fields)
        {
            if (!TableExists(c, name)) using (OleDbCommand cmd = new OleDbCommand("CREATE TABLE [" + name + "] (" + fields + ")", c)) cmd.ExecuteNonQuery();
        }
        private static void AddColumn(OleDbConnection c, string table, string column, string type)
        {
            DataTable columns = c.GetOleDbSchemaTable(OleDbSchemaGuid.Columns, new object[] { null, null, table, column });
            if (columns == null || columns.Rows.Count == 0)
                using (OleDbCommand cmd = new OleDbCommand("ALTER TABLE [" + table + "] ADD COLUMN [" + column + "] " + type, c)) cmd.ExecuteNonQuery();
        }
        private static void Seed(OleDbConnection c, OleDbTransaction tr, string table, string key, string label, string code, string name)
        {
            if (Convert.ToInt32(Scalar(c, tr, "SELECT COUNT(*) FROM [" + table + "] WHERE [" + key + "]=?", code)) == 0)
                Execute(c, tr, "INSERT INTO [" + table + "] ([" + key + "], [" + label + "]) VALUES (?, ?)", code, name);
        }
        internal static OleDbCommand Command(OleDbConnection c, OleDbTransaction tr, string sql, params object[] values)
        {
            OleDbCommand command = new OleDbCommand(sql, c, tr);
            foreach (object value in values)
            {
                OleDbType type = value is bool ? OleDbType.Boolean : value is DateTime ? OleDbType.Date : OleDbType.VarWChar;
                command.Parameters.Add("?", type).Value = value ?? DBNull.Value;
            }
            return command;
        }
        internal static int Execute(OleDbConnection c, OleDbTransaction tr, string sql, params object[] values)
        { using (OleDbCommand cmd = Command(c, tr, sql, values)) return cmd.ExecuteNonQuery(); }
        internal static object Scalar(OleDbConnection c, OleDbTransaction tr, string sql, params object[] values)
        { using (OleDbCommand cmd = Command(c, tr, sql, values)) return cmd.ExecuteScalar(); }
        internal static DataTable ReadTable(OleDbConnection c, OleDbTransaction tr, string sql, params object[] values)
        {
            using (OleDbCommand cmd = Command(c, tr, sql, values)) using (OleDbDataAdapter adapter = new OleDbDataAdapter(cmd))
            { DataTable table = new DataTable(); adapter.Fill(table); return table; }
        }
        internal static void Audit(OleDbConnection c, OleDbTransaction tr, string action, string target, string detail)
        {
            Execute(c, tr, "INSERT INTO NHAT_KY (THOI_GIAN, ID_TAI_KHOAN, THAO_TAC, DOI_TUONG, NOI_DUNG) VALUES (?, ?, ?, ?, ?)",
                DateTime.Now, AppSession.CurrentUser == null ? "SYSTEM" : AppSession.CurrentUser.Id, action, target, detail);
        }

        public DataTable GetRoles()
        {
            using (OleDbConnection c = CreateConnection()) { c.Open(); return ReadTable(c, null, "SELECT MaVaiTro, TenVaiTro FROM VAI_TRO WHERE MaVaiTro IN ('ADMIN','KHO','BANHANG','KETOAN') ORDER BY TenVaiTro"); }
        }
        public int GetAccountCount()
        { using (OleDbConnection c = CreateConnection()) { c.Open(); return Convert.ToInt32(Scalar(c, null, "SELECT COUNT(*) FROM TAI_KHOAN")); } }
        public UserAccount FindByUsername(string username) { return Find("TEN_DANG_NHAP", username == null ? "" : username.Trim()); }
        public UserAccount GetById(string id) { return Find("ID", id); }
        private UserAccount Find(string column, string value)
        {
            using (OleDbConnection c = CreateConnection())
            {
                c.Open();
                DataTable table = ReadTable(c, null, "SELECT * FROM TAI_KHOAN WHERE [" + column + "]=?", value);
                if (table.Rows.Count != 1) return null;
                DataRow row = table.Rows[0];
                UserAccount account = new UserAccount();
                account.Id = Convert.ToString(row["ID"]); account.Username = Convert.ToString(row["TEN_DANG_NHAP"]);
                account.FullName = Convert.ToString(row["HO_TEN"]); account.PasswordHash = Convert.ToString(row["MatKhauHash"]);
                account.Salt = Convert.ToString(row["Salt"]); account.IsActive = row["DANG_HOAT_DONG"] != DBNull.Value && Convert.ToBoolean(row["DANG_HOAT_DONG"]);
                account.IsDeleted = row["DaXoa"] != DBNull.Value && Convert.ToBoolean(row["DaXoa"]);
                account.MustChangePassword = row["DoiMatKhau"] != DBNull.Value && Convert.ToBoolean(row["DoiMatKhau"]);
                List<string> roles = new List<string>();
                foreach (DataRow role in ReadTable(c, null, "SELECT MaVaiTro FROM TAI_KHOAN_VAI_TRO WHERE ID_TAI_KHOAN=? ORDER BY MaVaiTro", account.Id).Rows)
                    roles.Add(Convert.ToString(role[0]));
                account.Role = String.Join(",", roles.ToArray());
                foreach (DataRow permission in ReadTable(c, null, "SELECT DISTINCT V.MaQuyen FROM VAI_TRO_QUYEN V INNER JOIN TAI_KHOAN_VAI_TRO T ON V.MaVaiTro=T.MaVaiTro WHERE T.ID_TAI_KHOAN=?", account.Id).Rows)
                    account.Permissions.Add(Convert.ToString(permission[0]));
                return account;
            }
        }
        public DataTable GetAllAccounts()
        {
            AccessControl.DemandCurrent("QuanLyTaiKhoan");
            using (OleDbConnection c = CreateConnection())
            {
                c.Open();
                DataTable table = ReadTable(c, null, "SELECT ID AS MaTaiKhoan, TEN_DANG_NHAP AS TenDangNhap, HO_TEN AS HoTen, VAI_TRO AS VaiTro, DANG_HOAT_DONG AS TrangThai, DoiMatKhau FROM TAI_KHOAN WHERE DaXoa=False OR DaXoa IS NULL ORDER BY TEN_DANG_NHAP");
                table.Columns.Add("TenVaiTro", typeof(string));
                DataTable catalog = GetRoles();
                foreach (DataRow row in table.Rows)
                {
                    UserAccount account = GetById(Convert.ToString(row["MaTaiKhoan"]));
                    row["VaiTro"] = account.Role;
                    List<string> names = new List<string>();
                    foreach (DataRow role in catalog.Rows) if (AccountRoles.Contains(account.Role, Convert.ToString(role[0]))) names.Add(Convert.ToString(role[1]));
                    row["TenVaiTro"] = String.Join(", ", names.ToArray());
                }
                return table;
            }
        }
        public void CreateInitialAdmin(string username, string fullName, string password)
        { throw new InvalidOperationException("Hệ thống dùng tài khoản có sẵn trong Access. Không tạo tài khoản khi đăng nhập lần đầu."); }
        public void CreateAccount(string username, string fullName, string password, string roles)
        {
            AccessControl.DemandCurrent("QuanLyTaiKhoan"); ValidateNewPassword(password);
            username = username == null ? "" : username.Trim(); fullName = fullName == null ? "" : fullName.Trim();
            if (username.Length == 0 || username.Length > 50 || fullName.Length > 100) throw new ArgumentException("Tên đăng nhập hoặc họ tên không hợp lệ.");
            if (FindByUsername(username) != null) throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");
            string id = IdGenerator.Next("TAI_KHOAN"); string salt = PasswordHasher.CreateSalt();
            using (OleDbConnection c = CreateConnection())
            {
                c.Open(); using (OleDbTransaction tr = c.BeginTransaction())
                {
                    try
                    {
                        ValidateRoles(c, tr, roles);
                        Execute(c, tr, "INSERT INTO TAI_KHOAN (ID,TEN_DANG_NHAP,HO_TEN,MAT_KHAU,MatKhauHash,Salt,VAI_TRO,DANG_HOAT_DONG,DoiMatKhau,DaXoa,NGAY_TAO) VALUES (?,?,?,NULL,?,?,?,?,?,?,?)",
                            id, username, fullName, PasswordHasher.HashPassword(password,salt), salt, roles, true, true, false, DateTime.Now);
                        WriteRoles(c, tr, id, roles); Audit(c, tr, "TAO_TAI_KHOAN", id, roles); tr.Commit();
                    }
                    catch { ErrorLog.Rollback(tr, "Rollback Access"); throw; }
                }
            }
        }
        private static void ValidateRoles(OleDbConnection c, OleDbTransaction tr, string roles)
        {
            string[] codes = AccountRoles.Split(roles);
            if (codes.Length != 1) throw new ArgumentException("Mỗi tài khoản chỉ được chọn một vai trò.");
            foreach (string code in codes)
                if (Convert.ToInt32(Scalar(c,tr,"SELECT COUNT(*) FROM VAI_TRO WHERE MaVaiTro=?",code)) != 1)
                    throw new ArgumentException("Vai trò không tồn tại trong Access: " + code);
        }
        private static void WriteRoles(OleDbConnection c, OleDbTransaction tr, string id, string roles)
        {
            Execute(c,tr,"DELETE FROM TAI_KHOAN_VAI_TRO WHERE ID_TAI_KHOAN=?",id);
            foreach (string role in AccountRoles.Split(roles)) Execute(c,tr,"INSERT INTO TAI_KHOAN_VAI_TRO (ID_TAI_KHOAN,MaVaiTro) VALUES (?,?)",id,role);
        }
        public void UpdateRole(string id, string roles)
        {
            AccessControl.DemandCurrent("QuanLyTaiKhoan");
            if (id == AppSession.CurrentUser.Id) throw new InvalidOperationException("Không tự thay đổi vai trò đang đăng nhập.");
            UserAccount account = GetById(id);
            if (account == null || account.IsDeleted) throw new InvalidOperationException("Không tìm thấy tài khoản.");
            using (OleDbConnection c = CreateConnection())
            {
                c.Open(); using (OleDbTransaction tr = c.BeginTransaction())
                {
                    try
                    {
                        ValidateRoles(c,tr,roles);
                        if (AccountRoles.IsAdmin(account.Role) && !AccountRoles.IsAdmin(roles)) ProtectLastAdmin(c,tr,id);
                        Execute(c,tr,"UPDATE TAI_KHOAN SET VAI_TRO=? WHERE ID=?",roles,id); WriteRoles(c,tr,id,roles);
                        Audit(c,tr,"DOI_VAI_TRO",id,account.Role+" -> "+roles); tr.Commit();
                    }
                    catch { ErrorLog.Rollback(tr, "Rollback Access"); throw; }
                }
            }
        }
        public void UpdateAccount(string id, string fullName, string roles)
        {
            AccessControl.DemandCurrent("QuanLyTaiKhoan");
            fullName = fullName == null ? "" : fullName.Trim();
            if (fullName.Length == 0 || fullName.Length > 100)
                throw new ArgumentException("Họ tên phải có từ 1 đến 100 ký tự.");
            UserAccount account = GetById(id);
            if (account == null || account.IsDeleted)
                throw new InvalidOperationException("Không tìm thấy tài khoản.");
            bool roleChanged = !String.Equals(account.Role, roles, StringComparison.OrdinalIgnoreCase);
            if (id == AppSession.CurrentUser.Id && roleChanged)
                throw new InvalidOperationException("Không thể tự thay đổi vai trò đang đăng nhập.");
            using (OleDbConnection c = CreateConnection())
            {
                c.Open();
                using (OleDbTransaction tr = c.BeginTransaction())
                {
                    try
                    {
                        ValidateRoles(c, tr, roles);
                        if (AccountRoles.IsAdmin(account.Role) && !AccountRoles.IsAdmin(roles))
                            ProtectLastAdmin(c, tr, id);
                        if (Execute(c, tr, "UPDATE TAI_KHOAN SET HO_TEN=?, VAI_TRO=? WHERE ID=?", fullName, roles, id) != 1)
                            throw new InvalidOperationException("Không tìm thấy tài khoản.");
                        WriteRoles(c, tr, id, roles);
                        Audit(c, tr, "SUA_TAI_KHOAN", id, account.Role + " -> " + roles);
                        tr.Commit();
                    }
                    catch { ErrorLog.Rollback(tr, "Rollback Access"); throw; }
                }
            }
        }
        private static void ProtectLastAdmin(OleDbConnection c, OleDbTransaction tr, string id)
        {
            if (Convert.ToInt32(Scalar(c,tr,"SELECT COUNT(*) FROM TAI_KHOAN T INNER JOIN TAI_KHOAN_VAI_TRO V ON T.ID=V.ID_TAI_KHOAN WHERE V.MaVaiTro='ADMIN' AND T.DANG_HOAT_DONG=True AND (T.DaXoa=False OR T.DaXoa IS NULL) AND T.ID<>?",id)) == 0)
                throw new InvalidOperationException("Phải giữ ít nhất một tài khoản quản lý đang hoạt động.");
        }
        public void SetActive(string id, bool active)
        {
            AccessControl.DemandCurrent("QuanLyTaiKhoan");
            if (id == AppSession.CurrentUser.Id) throw new InvalidOperationException("Không tự khóa tài khoản đang đăng nhập.");
            UserAccount account=GetById(id); if(account==null || account.IsDeleted) throw new InvalidOperationException("Không tìm thấy tài khoản.");
            using(OleDbConnection c=CreateConnection())
            {
                c.Open(); using(OleDbTransaction tr=c.BeginTransaction())
                {
                    try { if(!active && AccountRoles.IsAdmin(account.Role)) ProtectLastAdmin(c,tr,id);
                        Execute(c,tr,"UPDATE TAI_KHOAN SET DANG_HOAT_DONG=? WHERE ID=?",active,id);
                        Audit(c,tr,active?"MO_KHOA":"KHOA_TAI_KHOAN",id,""); tr.Commit(); }
                    catch { ErrorLog.Rollback(tr, "Rollback Access"); throw; }
                }
            }
        }
        public void DeleteAccount(string id) { SetActive(id,false); }
        public void ResetPassword(string id,string password)
        { AccessControl.DemandCurrent("QuanLyTaiKhoan"); ValidateNewPassword(password); UpdatePassword(id,password,true); }
        public void ChangeOwnPassword(UserAccount account,string oldPassword,string password)
        {
            if(AppSession.CurrentUser==null || account.Id!=AppSession.CurrentUser.Id) throw new UnauthorizedAccessException("Không được đổi mật khẩu tài khoản khác.");
            UserAccount current=GetById(account.Id);
            if(current==null || !current.IsActive || current.IsDeleted || !VerifyPassword(current,oldPassword)) throw new InvalidOperationException("Mật khẩu hiện tại không đúng hoặc tài khoản đã bị khóa.");
            ValidateNewPassword(password); UpdatePassword(account.Id,password,false); account.MustChangePassword=false;
        }
        private void UpdatePassword(string id,string password,bool mustChange)
        {
            string salt=PasswordHasher.CreateSalt(); using(OleDbConnection c=CreateConnection())
            {
                c.Open(); using(OleDbTransaction tr=c.BeginTransaction())
                {
                    try {
                        if(Execute(c,tr,"UPDATE TAI_KHOAN SET MatKhauHash=?,Salt=?,MAT_KHAU=NULL,DoiMatKhau=? WHERE ID=?",PasswordHasher.HashPassword(password,salt),salt,mustChange,id)!=1) throw new InvalidOperationException("Không tìm thấy tài khoản.");
                        Audit(c,tr,mustChange?"DAT_LAI_MAT_KHAU":"DOI_MAT_KHAU",id,""); tr.Commit();
                    } catch { ErrorLog.Rollback(tr, "Rollback Access"); throw; }
                }
            }
        }
        public static bool VerifyPassword(UserAccount account,string password)
        { return account!=null && PasswordHasher.VerifyPassword(password,account.Salt,account.PasswordHash); }
        public static void ValidateNewPassword(string password)
        { if(password==null || password.Length<8) throw new ArgumentException("Mật khẩu mới phải có ít nhất 8 ký tự."); }
    }
}
