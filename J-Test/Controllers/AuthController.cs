using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using J_Test.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace J_Test.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuthController : ControllerBase
    {
        private readonly JTestCredentialsContext _context;
        private readonly IPasswordHasher<Credential> _passwordHasher;
        private readonly IDataProtector _jiraTokenProtector;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            JTestCredentialsContext context,
            IDataProtectionProvider dataProtectionProvider,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;

            _passwordHasher = new PasswordHasher<Credential>();

            _jiraTokenProtector = dataProtectionProvider.CreateProtector(
                "JTest.Jira.ApiToken.v1");
        }

        // POST: api/auth/signup
        [AllowAnonymous]
        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] Credential credential)
        {
            if (credential == null || string.IsNullOrWhiteSpace(credential.email) || string.IsNullOrWhiteSpace(credential.password))
            {
                return BadRequest("Invalid credentials.");
            }

            // Check if email already exists
            if (await _context.Credentials.AnyAsync(c => c.email == credential.email))
            {
                return BadRequest("Email is already taken.");
            }

            // Hash the password before storing it
            credential.password = _passwordHasher.HashPassword(
                credential,
                credential.password);

            _context.Credentials.Add(credential);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User successfully created." });
        }

        // POST: api/auth/login
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Credential loginCredential)
        {
            if (loginCredential == null || string.IsNullOrWhiteSpace(loginCredential.email) || string.IsNullOrWhiteSpace(loginCredential.password))
            {
                return BadRequest("Invalid login credentials.");
            }

            // Look for user by email
            var user = await _context.Credentials
                .FirstOrDefaultAsync(c => c.email == loginCredential.email);

            if (user == null)
            {
                return Unauthorized("User not found.");
            }

            // Validate hashed password
            var verificationResult = _passwordHasher.VerifyHashedPassword(
                user,
                user.password,
                loginCredential.password);

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Invalid password.");
            }

            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];

            if (string.IsNullOrWhiteSpace(jwtKey) ||
                string.IsNullOrWhiteSpace(jwtIssuer) ||
                string.IsNullOrWhiteSpace(jwtAudience))
            {
                return StatusCode(
                    500,
                    "Authentication configuration is unavailable.");
            }

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.email),
                new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
                new Claim(ClaimTypes.Email, user.email)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials);

            var tokenValue =
                new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                message = "Login successful.",
                token = tokenValue,
                expiresIn = 3600
            });
        }

        // GET: api/settings/details
        [HttpGet("details")]
        public async Task<IActionResult> Getdetails()
        {
            var details = await _context.Details
                .Select(d => new
                {
                    id = d.Id,
                    d.domain,
                    d.username
                })
                .ToListAsync();

            return Ok(details);
        }

        // GET: api/settings/credentials
        [HttpGet("credentials")]
        public async Task<IActionResult> GetCredentials()
        {
            var credentials = await _context.Credentials
                .Select(c => new
                {
                    c.id,
                    c.email
                })
                .ToListAsync();

            return Ok(credentials);
        }

        // POST: api/settings/save
        [HttpPost("save")]
        public async Task<IActionResult> SaveCredentials([FromBody] Detail credential)
        {
            if (credential == null)
            {
                return BadRequest("Credentials cannot be null.");
            }

            if (string.IsNullOrEmpty(credential.domain) ||
                string.IsNullOrEmpty(credential.username) ||
                string.IsNullOrEmpty(credential.apiToken))
            {
                return BadRequest(
                    "Domain, Username, and ApiToken cannot be empty.");
            }

            try
            {
                credential.apiToken =
                    _jiraTokenProtector.Protect(
                        credential.apiToken);

                _context.Details.Add(credential);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Credentials saved successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while saving Jira credentials.");

                return StatusCode(
                    500,
                    "An internal server error occurred.");
            }
        }

        // DELETE: api/settings/delete/{id}
        [HttpDelete("detail/delete/{id}")]
        public async Task<IActionResult> DeleteDetail(int id)
        {
            // Find the credential by its Id in the Details table
            var credential = await _context.Details.FindAsync(id);

            if (credential == null)
            {
                return NotFound(new { message = "Credential not found." });
            }

            // Remove the credential from the Details table
            _context.Details.Remove(credential);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Credential deleted successfully." });
        }

        // DELETE: api/settings/delete/{id}
        [HttpDelete("credential/delete/{id}")]
        public async Task<IActionResult> DeleteCredential(int id)
        {
            // Find the credential by its Id in the Details table
            var credential = await _context.Credentials.FindAsync(id);

            if (credential == null)
            {
                return NotFound(new { message = "Credential not found." });
            }

            // Remove the credential from the Credentials table
            _context.Credentials.Remove(credential);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Credential deleted successfully." });
        }
    }
}