namespace Library.CredentialsClasses
{
    internal abstract class Credentials
    {
        public int Id { get; set; }
        public string? Login { get; set; }
        public string? Password { get; set; }

        protected Credentials(int id, string login, string password)
        {
            Id = id;
            Login = login;
            Password = password;
        }
    }
}
