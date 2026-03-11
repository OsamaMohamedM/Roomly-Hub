namespace Application.DTOs
{
    public class LoginResponseDto
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string Message { get; set; }

        public LoginResponseDto(string accessToken, string refreshToken, string message = "Login successful")
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            Message = message;
        }
    }
}
