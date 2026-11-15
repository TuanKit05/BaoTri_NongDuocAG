using System;

namespace CuahangNongduoc
{
    public class UserAccount
    {
        public int Id;
        public string Username;
        public string FullName;
        public string Role;
        public bool IsActive;
        public bool IsDeleted;
        public bool MustChangePassword;
        public string PasswordHash;
        public string Salt;
    }

    public class LoginContext
    {
        public string Username;
        public string Password;
        public UserAccount Account;
        public string ErrorMessage;
        public bool Success;
    }

    public static class AppSession
    {
        public static UserAccount CurrentUser;
        public static bool LogoutRequested;
    }

    public static class AccountRoles
    {
        public const string Admin = "ADMIN";
        public const string Warehouse = "KHO";
        public const string Sales = "BANHANG";
        public const string Cashier = "THUNGAN";

        public static bool IsValid(string role)
        {
            return role == Admin || role == Warehouse || role == Sales || role == Cashier;
        }
    }
}
