namespace LTOCS.Config
{
    public class JwtConfig
    {
        public char[] Key { get; set; } = new char[32];

        public string Issuer { get; set; } = string.Empty;

        public string Audience { get; set; } = string.Empty;

        public int AccessTokenExpiryMinutes { get; set; } = 15;

        public int RefreshTokenExpiryDays { get; set; } = 7;
    }
}