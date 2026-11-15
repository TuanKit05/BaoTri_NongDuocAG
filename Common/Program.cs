using System;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                AccountRepository repository = new AccountRepository();
                repository.EnsureSchema();

                if (repository.GetAccountCount() == 0)
                {
                    using (frmTaoTaiKhoanAdmin setup = new frmTaoTaiKhoanAdmin(repository))
                    {
                        if (setup.ShowDialog() != DialogResult.OK)
                            return;
                    }
                }

                AuthService authService = new AuthService(repository);
                bool runLoginAgain = true;
                while (runLoginAgain)
                {
                    AppSession.LogoutRequested = false;
                    using (frmDangNhap login = new frmDangNhap(authService, repository))
                    {
                        if (login.ShowDialog() != DialogResult.OK)
                            break;
                    }

                    Application.Run(new frmMain());
                    runLoginAgain = AppSession.LogoutRequested;
                    AppSession.CurrentUser = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể khởi động hệ thống. Hãy kiểm tra cơ sở dữ liệu Access trong thư mục Database.\r\n" + ex.Message,
                    "Lỗi khởi động", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                AppSession.CurrentUser = null;
            }
        }
    }
}
