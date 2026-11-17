namespace CuahangNongduoc
{
    public class AuthService
    {
        private AccountRepository _repository;

        public AuthService(AccountRepository repository)
        {
            _repository = repository;
        }

        public LoginContext Login(string username, string password)
        {
            LoginContext context = new LoginContext();
            context.Username = username;
            context.Password = password;

            ValidateLoginInputHandler input = new ValidateLoginInputHandler();
            FindAccountHandler find = new FindAccountHandler(_repository);
            CheckAccountStatusHandler status = new CheckAccountStatusHandler();
            VerifyPasswordHandler verify = new VerifyPasswordHandler();
            input.SetNext(find);
            find.SetNext(status);
            status.SetNext(verify);

            context.Success = input.Handle(context);
            return context;
        }
    }
}
