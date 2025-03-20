using System.Security.Cryptography;
using BusinessLayer.Interface;
using Microsoft.AspNetCore.Mvc;
using ModelLayer.DTOs;
using ModelLayer.Model;
using RepositoryLayer.Context;
using RepositoryLayer.Interface;
using ModelLayer.DTOs;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;

namespace UserAddressBook.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IJwtService _jwtService;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly JwtSettings _jwtSettings;
        public AuthController(AppDbContext context, IPasswordService passwordService, IJwtService jwtService,
                      IUserRepository userRepository, IEmailService emailService, IOptions<JwtSettings> jwtSettings)
        {
            _context = context;
            _passwordService = passwordService;
            _jwtService = jwtService;
            _userRepository = userRepository;
            _emailService = emailService;

            // Use injected IOptions<JwtSettings>
            _jwtSettings = jwtSettings.Value;
        }

        [HttpGet]
        public string Get()
        {
            return "Hello";
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterUserDTO dto)
        {
            if (_context.Users.Any(u => u.Email == dto.Email))
            {
                return BadRequest(new { message = "Email already exists." });
            }

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = _passwordService.HashPassword(dto.Password)
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new { message = "User registered successfully." });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginUserDTO dto)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == dto.Email);
            if (user == null || !_passwordService.VerifyPassword(dto.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var token = _jwtService.GenerateToken(user.Email);
            return Ok(new { token });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return BadRequest(new { message = "User not found." });
            }

            // Generate Reset Token (JWT)
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)); // Using _jwtSettings for the key
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, user.Email) }),
                Expires = DateTime.UtcNow.AddMinutes(15), // Token valid for 15 mins
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var resetToken = tokenHandler.WriteToken(token);

            // Store Token in Database (optional)
            user.ResetToken = resetToken;
            user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
            await _userRepository.UpdateAsync(user);

            // Send Email with Token
            string resetLink = $"http://localhost:4200/reset-password?token={resetToken}";
            string emailBody = $@"
    <p>Click the link below to reset your password:</p>
    <p><a href='{resetLink}' target='_blank'>{resetLink}</a></p>

";
            await _emailService.SendEmailAsync(user.Email, "Reset Password", emailBody);

            return Ok(new { message = "Password reset link has been sent to your email." });
        }



        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO request)
        {
            var user = await _userRepository.GetByResetTokenAsync(request.Token);

            if (user == null)
            {
                Console.WriteLine($"Reset token not found: {request.Token}");
                return BadRequest("Invalid or expired token. null");
            }

            if (user.ResetTokenExpiry < DateTime.UtcNow)
            {
                Console.WriteLine($"Token expired. Token Expiry: {user.ResetTokenExpiry}, Current Time: {DateTime.UtcNow}");
                return BadRequest("Invalid or expired token. time out");
            }

            // Hash New Password
            user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            await _userRepository.UpdateAsync(user);

            return Ok("Password reset successfully.");
        }

    }
}
