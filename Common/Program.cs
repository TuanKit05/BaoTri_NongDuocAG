using System;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs e) {
                MessageBox.Show(ErrorLog.Describe(e.Exception, "Không thể hoàn tất thao tác"),
                    "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e) {
                Exception error = e.ExceptionObject as Exception;
                if (error != null) ErrorLog.Record(error, "Lỗi ngoài luồng giao diện");
            };
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                AccountRepository repository = new AccountRepository();
                repository.EnsureSchema();

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

                    Application.Idle += ResponsiveLayout.ApplyOpenForms;
                    Application.Idle += SecuredUi.ApplyOpenForms;
                    Application.Run(new frmMain());
                    Application.Idle -= SecuredUi.ApplyOpenForms;
                    Application.Idle -= ResponsiveLayout.ApplyOpenForms;
                    runLoginAgain = AppSession.LogoutRequested;
                    AppSession.CurrentUser = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ErrorLog.Describe(ex, "Không thể khởi động hệ thống: " + ex.Message),
                    "Lỗi khởi động", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                AppSession.CurrentUser = null;
            }
        }
    }
}
