using LTOCS.Config;
using LTOCS.Database;
using LTOCS.Extensions;
using LTOCS.Models.Db;
using LTOCS.Models.Request.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LTOCS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JwtConfig _jwtConfig;
        private readonly ApplicationDbContext _context;

        public AccountController (
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            JwtConfig configuration,
            ApplicationDbContext dbContext
        ) {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtConfig = configuration;
            _context = dbContext;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest registerRequest
        ) {
            var user = new ApplicationUser
            {
                UserName = registerRequest.Username,
                Email = registerRequest.Email
            };

            var result = await _userManager.CreateAsync(user, registerRequest.Password);

            if (result.Succeeded)
            {
                return Ok(new { Message = "User registered successfully" });
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate(
            [FromBody] LoginRequest loginRequest
        ) {
            var user = await _userManager.FindByNameAsync(loginRequest.Username);

            if (user != null && await _userManager.CheckPasswordAsync(user, loginRequest.Password))
            {
                var token = user.GetJwtAccessTokenString(_jwtConfig);                

                var refreshToken = new RefreshToken
                {
                    Token = Guid.NewGuid().ToString(),
                    UserId = user.Id,
                    ExpiryDate = DateTime.UtcNow.AddDays(_jwtConfig.RefreshTokenExpiryDays),
                    IsRevoked = false
                };

                _context.RefreshTokens.Add(refreshToken);
                await _context.SaveChangesAsync();

                return Ok(new { Token = token, RefreshToken = refreshToken.Token });
            }

            return Unauthorized("Invalid credentials");
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(
            [FromBody] RefreshTokenRequest refreshTokenRequest
        ) {
            var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshTokenRequest.RefreshToken);

            if (refreshToken == null || refreshToken.IsRevoked || refreshToken.ExpiryDate <= DateTime.UtcNow)
            {
                return Unauthorized("Invalid refresh token.");
            }

            var user = await _userManager.FindByIdAsync(refreshToken.UserId);
            if (user == null)
            {
                return Unauthorized("Invalid refresh token.");
            }

            var token = user.GetJwtAccessTokenString(_jwtConfig);

            return Ok(new { Token = token });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(
            [FromBody] LogoutRequest logoutRequest    
        ) {
            var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == logoutRequest.RefreshToken);

            if (refreshToken == null)
            {
                return BadRequest("Invalid refresh token");
            }

            refreshToken.IsRevoked = true;
            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync();

            await _signInManager.SignOutAsync();
            return Ok(new { Message = "User logged out successfully" });
        }
    }
}
