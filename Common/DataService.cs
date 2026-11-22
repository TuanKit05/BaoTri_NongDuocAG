using System;
using System.Data;
using System.Data.OleDb;
using System.IO;

namespace CuahangNongduoc
{
	
	public class DataService : DataTable
	{
        
		// The connection to a database of this data service.
		private static OleDbConnection	m_Connection;

        //

        public static readonly string DatabasePath = ResolveDatabasePath();

        public static string m_ConnectString = CreateConnectionString();

        private static string ResolveDatabasePath()
        {
            // A development build uses the project's database directly.
            // A deployed application uses Database beside its executable.
            string applicationDirectory = Path.GetDirectoryName(typeof(DataService).Assembly.Location);
            DirectoryInfo directory = new DirectoryInfo(applicationDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "CuahangNongduoc.csproj")))
                {
                    string projectDatabase = Path.Combine(directory.FullName, @"Database\cuahang.dll");
                    if (!File.Exists(projectDatabase))
                        throw new FileNotFoundException("Không tìm thấy cơ sở dữ liệu của dự án.", projectDatabase);
                    return projectDatabase;
                }
                directory = directory.Parent;
            }
            string deployedDatabase = Path.Combine(applicationDirectory, @"Database\cuahang.dll");
            if (!File.Exists(deployedDatabase))
                throw new FileNotFoundException("Không tìm thấy cơ sở dữ liệu Access.", deployedDatabase);
            return deployedDatabase;
        }

        private static string CreateConnectionString()
        {
            OleDbConnectionStringBuilder builder = new OleDbConnectionStringBuilder();
            builder.Provider = "Microsoft.Jet.OLEDB.4.0";
            builder.DataSource = DatabasePath;
            return builder.ConnectionString;
        }


        // The command to execute query or non-query command on a database of this data service.
        private OleDbCommand		m_Command;
      

        public DataService(){}


        public OleDbCommand Command
        {
            get { return m_Command; }
            set { m_Command = value; }
        }


        private static void EnsureConnection()
        {
            if (m_Connection == null) m_Connection = new OleDbConnection(m_ConnectString);
            if (m_Connection.State == ConnectionState.Broken) m_Connection.Close();
            if (m_Connection.State != ConnectionState.Open) m_Connection.Open();
        }

        public void Load(OleDbCommand command)
        {
            if (command == null) throw new ArgumentNullException("command");
            AccessControl.PrepareRead(command);
            try
            {
                EnsureConnection();
                command.Connection = m_Connection;
                command.Transaction = null;
                using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
                using (DataTable loaded = new DataTable())
                {
                    adapter.Fill(loaded);
                    Clear();
                    Merge(loaded, false, MissingSchemaAction.Add);
                    AcceptChanges();
                }
                m_Command = command;
                if (UsesNativeIdentity(command.CommandText) && Columns.Contains("ID"))
                {
                    Columns["ID"].AutoIncrement = true;
                    Columns["ID"].AutoIncrementSeed = -1;
                    Columns["ID"].AutoIncrementStep = -1;
                    Columns["ID"].ReadOnly = true;
                }
                AccessControl.HideCosts(this);
            }
            catch (Exception error) { throw ErrorLog.Wrap(error, "Không thể tải dữ liệu từ Access"); }
        }

        public static bool OpenConnection()
        {
            try { EnsureConnection(); return true; }
            catch (Exception error) { throw ErrorLog.Wrap(error, "Không thể kết nối cơ sở dữ liệu Access"); }
        }

        public void CloseConnection()
        {
            if (m_Connection == null) return;
            try { m_Connection.Close(); }
            catch (Exception error) { throw ErrorLog.Wrap(error, "Không thể đóng kết nối Access"); }
        }

        private static bool UsesNativeIdentity(string sql)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(sql,
                @"\bFROM\s+\[?(DON_VI_TINH|LY_DO_CHI)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        public int ExecuteNoneQuery()
        {
            if (m_Command == null) throw new InvalidOperationException("Chưa tải bảng dữ liệu cần lưu.");
            AccessControl.ValidateRows(this, m_Command.CommandText);
            string auditChanges = AccessControl.DescribeChanges(this);
            OleDbTransaction transaction = null;
            try
            {
                EnsureConnection();
                transaction = m_Connection.BeginTransaction();
                using (OleDbCommand select = (OleDbCommand)m_Command.Clone())
                using (OleDbDataAdapter adapter = new OleDbDataAdapter(select))
                using (OleDbCommandBuilder builder = new OleDbCommandBuilder(adapter))
                {
                    select.Connection = m_Connection;
                    select.Transaction = transaction;
                    adapter.AcceptChangesDuringUpdate = false;
                    adapter.RowUpdating += delegate(object sender, OleDbRowUpdatingEventArgs e) {
                        if (e.Command == null) return;
                        e.Command.Connection = m_Connection;
                        e.Command.Transaction = transaction;
                    };
                    adapter.RowUpdated += delegate(object sender, OleDbRowUpdatedEventArgs e) {
                        if (e.StatementType != StatementType.Insert || e.Errors != null || !UsesNativeIdentity(select.CommandText)) return;
                        using (OleDbCommand identity = new OleDbCommand("SELECT @@IDENTITY", m_Connection, transaction))
                        {
                            DataColumn idColumn = e.Row.Table.Columns["ID"];
                            bool readOnly = idColumn.ReadOnly;
                            idColumn.ReadOnly = false;
                            try { e.Row["ID"] = Convert.ToInt32(identity.ExecuteScalar()); }
                            finally { idColumn.ReadOnly = readOnly; }
                        }
                    };
                    int result = adapter.Update(this);
                    if (result > 0) AccountRepository.Audit(m_Connection, transaction,
                        "LUU_DU_LIEU", "DataService", m_Command.CommandText + "\n" + auditChanges);
                    transaction.Commit();
                    AcceptChanges();
                    return result;
                }
            }
            catch (Exception error)
            {
                ErrorLog.Rollback(transaction, "Lưu bảng Access");
                throw ErrorLog.Wrap(error, "Không thể lưu dữ liệu vào Access");
            }
            finally
            {
                m_Command.Transaction = null;
                if (transaction != null) transaction.Dispose();
            }
        }

        public int ExecuteNoneQuery(OleDbCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException("cmd");
            AccessControl.DemandWrite(cmd.CommandText);
            OleDbTransaction transaction = null;
            try
            {
                EnsureConnection();
                transaction = m_Connection.BeginTransaction();
                cmd.Connection = m_Connection;
                cmd.Transaction = transaction;
                int result = cmd.ExecuteNonQuery();
                AccountRepository.Audit(m_Connection, transaction, "CAP_NHAT_DU_LIEU", "DataService", cmd.CommandText);
                transaction.Commit();
                return result;
            }
            catch (Exception error)
            {
                ErrorLog.Rollback(transaction, "Cập nhật Access");
                throw ErrorLog.Wrap(error, "Không thể cập nhật dữ liệu Access");
            }
            finally
            {
                cmd.Transaction = null;
                if (transaction != null) transaction.Dispose();
            }
        }

        public object ExecuteScalar(OleDbCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException("cmd");
            AccessControl.PrepareRead(cmd);
            try
            {
                EnsureConnection();
                cmd.Connection = m_Connection;
                cmd.Transaction = null;
                object result = cmd.ExecuteScalar();
                return result == DBNull.Value ? null : result;
            }
            catch (Exception error) { throw ErrorLog.Wrap(error, "Không thể đọc giá trị từ Access"); }
        }
    }
}
