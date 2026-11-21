namespace CuahangNongduoc
{
    public interface IAuthenticationHandler
    {
        void SetNext(IAuthenticationHandler next);
        bool Handle(LoginContext context);
    }

    public abstract class AuthenticationHandlerBase : IAuthenticationHandler
    {
        private IAuthenticationHandler _next;

        public void SetNext(IAuthenticationHandler next)
        {
            _next = next;
        }

        protected bool Continue(LoginContext context)
        {
            if (_next == null)
                return true;
            return _next.Handle(context);
        }

        public abstract bool Handle(LoginContext context);
    }

    public class ValidateLoginInputHandler : AuthenticationHandlerBase
    {
        public override bool Handle(LoginContext context)
        {
            if (context.Username == null || context.Username.Trim().Length == 0 ||
                context.Username.Trim().Length > 50 ||
                context.Password == null || context.Password.Length == 0)
            {
                context.ErrorMessage = "Vui lòng nhập tên đăng nhập và mật khẩu hợp lệ (tên đăng nhập tối đa 50 ký tự).";
                return false;
            }
            return Continue(context);
        }
    }

    public class FindAccountHandler : AuthenticationHandlerBase
    {
        private AccountRepository _repository;

        public FindAccountHandler(AccountRepository repository)
        {
            _repository = repository;
        }

        public override bool Handle(LoginContext context)
        {
            context.Account = _repository.FindByUsername(context.Username);
            if (context.Account == null)
            {
                context.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác.";
                return false;
            }
            return Continue(context);
        }
    }

    public class CheckAccountStatusHandler : AuthenticationHandlerBase
    {
        public override bool Handle(LoginContext context)
        {
            if (context.Account.Permissions.Count == 0 && !AccountRoles.IsAdmin(context.Account.Role))
            {
                context.ErrorMessage = "Tài khoản chưa được gán vai trò hoặc quyền hợp lệ. Vui lòng liên hệ quản lý.";
                return false;
            }
            if (context.Account.IsDeleted)
            {
                context.ErrorMessage =
                    "Tài khoản đã bị xóa. Vui lòng liên hệ quản trị viên.";
                return false;
            }

            if (!context.Account.IsActive)
            {
                context.ErrorMessage =
                    "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.";
                return false;
            }

            return Continue(context);
        }
    }

    public class VerifyPasswordHandler : AuthenticationHandlerBase
    {
        public override bool Handle(LoginContext context)
        {
            if (!AccountRepository.VerifyPassword(context.Account, context.Password))
            {
                context.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác.";
                return false;
            }
            return Continue(context);
        }
    }
}
