namespace AuthService.Services
{
    public class PasswordService
    {
        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty or whitespace");

            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string inputPassword, string storedHash)
        {

            if (string.IsNullOrWhiteSpace(storedHash))
                return false;

            return BCrypt.Net.BCrypt.Verify(inputPassword, storedHash);
        }
    }
}
