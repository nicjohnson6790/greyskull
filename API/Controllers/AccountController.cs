using API.Config;
using API.Contracts.AccountController;
using API.Database;
using API.Extensions;
using API.Models.Db;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JwtConfig _jwtConfig;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            JwtConfig configuration,
            ApplicationDbContext dbContext
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtConfig = configuration;
            _context = dbContext;
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterResponse), 200)]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest request
        )
        {
            var user = new ApplicationUser
            {
                UserName = request.Username,
                Email = request.Email
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                return Ok(new { Message = "User registered successfully" });
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("authenticate")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        public async Task<IActionResult> Authenticate(
            [FromBody] LoginRequest request
        )
        {
            var user = await _userManager.FindByNameAsync(request.Username);

            if (user != null && await _userManager.CheckPasswordAsync(user, request.Password))
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
        [ProducesResponseType(typeof(RefreshTokenResponse), 200)]
        public async Task<IActionResult> RefreshToken(
            [FromBody] RefreshTokenRequest request
        )
        {
            var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

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
        [ProducesResponseType(typeof(LogoutResponse), 200)]
        public async Task<IActionResult> Logout(
            [FromBody] LogoutRequest logoutRequest
        )
        {
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
