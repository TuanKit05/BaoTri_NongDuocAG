namespace CuahangNongduoc
{
    // Strategy: mỗi vai trò có một bộ quy tắc truy cập riêng.
    public interface IRolePolicy
    {
        bool CanAccess(string feature);
    }

    public class AdminRolePolicy : IRolePolicy
    {
        public bool CanAccess(string feature) { return true; }
    }

    public class WarehouseRolePolicy : IRolePolicy
    {
        public bool CanAccess(string feature)
        {
            return feature == "NhapHang" || feature == "DonViTinh" ||
                feature == "SanPham" || feature == "NhaCungCap" ||
                feature == "BaoCaoTonKho";
        }
    }

    public class SalesRolePolicy : IRolePolicy
    {
        public bool CanAccess(string feature)
        {
            return feature == "BanHang" || feature == "KhachHang" ||
                feature == "DaiLy" || feature == "SanPham" ||
                feature == "BaoCaoBanHang";
        }
    }

    public class CashierRolePolicy : IRolePolicy
    {
        public bool CanAccess(string feature)
        {
            return feature == "ThuChi" || feature == "KhachHang" ||
                feature == "LyDoChi";
        }
    }

    public static class RolePolicyFactory
    {
        public static IRolePolicy Create(string role)
        {
            if (role == AccountRoles.Admin) return new AdminRolePolicy();
            if (role == AccountRoles.Warehouse) return new WarehouseRolePolicy();
            if (role == AccountRoles.Sales) return new SalesRolePolicy();
            if (role == AccountRoles.Cashier) return new CashierRolePolicy();
            return new DenyAllRolePolicy();
        }
    }

    public class DenyAllRolePolicy : IRolePolicy
    {
        public bool CanAccess(string feature) { return false; }
    }

    // Proxy: kiểm tra phiên đăng nhập và trạng thái tài khoản trước khi
    // chuyển yêu cầu kiểm tra quyền đến Strategy tương ứng với vai trò.
    public class AuthorizationProxy : IRolePolicy
    {
        private UserAccount _account;
        private IRolePolicy _rolePolicy;

        public AuthorizationProxy(UserAccount account, IRolePolicy rolePolicy)
        {
            _account = account;
            _rolePolicy = rolePolicy;
        }

        public bool CanAccess(string feature)
        {
            if (_account == null || !_account.IsActive || _rolePolicy == null)
                return false;
            return _rolePolicy.CanAccess(feature);
        }
    }
}
