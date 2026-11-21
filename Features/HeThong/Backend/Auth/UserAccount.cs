using System;
using System.Collections.Generic;

namespace CuahangNongduoc
{
    public class UserAccount
    {
        public string Id;
        public string Username;
        public string FullName;
        public string Role;
        public readonly List<string> Permissions = new List<string>();
        public bool IsActive;
        public bool IsDeleted;
        public bool MustChangePassword;
        public string PasswordHash;
        public string Salt;
        internal string LegacyPassword;
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
        public const string Admin="ADMIN", Warehouse="KHO", Sales="BANHANG", Accountant="KETOAN", Cashier="KETOAN";
        public static string Normalize(string role)
        {
            role=(role??"").Trim();
            if(role=="Quánr lý") return Admin;
            if(role=="Thủ kho") return Warehouse;
            if(role=="Bán hàng") return Sales;
            if(role=="Kế toán") return Accountant;
            return role;
        }
        public static string[] Split(string value)
        {
            List<string> roles=new List<string>();
            foreach(string part in (value??"").Split(new char[]{',',';'}))
            { string role=Normalize(part); if(role.Length>0 && !roles.Contains(role)) roles.Add(role); }
            return roles.ToArray();
        }
        public static bool Contains(string roles,string code)
        { foreach(string role in Split(roles)) if(role==code) return true; return false; }
        public static bool IsAdmin(string roles) { return Contains(roles,Admin); }
        public static bool IsValid(string roles)
        { string[] codes=Split(roles); foreach(string code in codes) if(code!=Admin && code!=Warehouse && code!=Sales && code!=Accountant) return false; return codes.Length>0; }
    }
}
