using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTOs;

namespace UserService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserContext _context;

        public UsersController(UserContext context)
        {
            _context = context;
        }

        [HttpGet("profile")]
        public async Task<ActionResult<UserDto>> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(Guid.Parse(userId));
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber
            });
        }

        [HttpPut("profile")]
        public async Task<ActionResult> UpdateProfile(UpdateProfileDto updateDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(Guid.Parse(userId));
            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = updateDto.FirstName ?? user.FirstName;
            user.LastName = updateDto.LastName ?? user.LastName;
            user.PhoneNumber = updateDto.PhoneNumber ?? user.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("change-password")]
        public async Task<ActionResult> ChangePassword(UpdatePasswordDto updatePasswordDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(Guid.Parse(userId));
            if (user == null)
            {
                return NotFound();
            }

            // Verifiera nuvarande lösenord
            if (!BCrypt.Net.BCrypt.Verify(updatePasswordDto.CurrentPassword, user.PasswordHash))
            {
                return BadRequest("Felaktigt nuvarande lösenord");
            }

            // Uppdatera till nytt lösenord
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updatePasswordDto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Lösenordet har uppdaterats" });
        }
    }

    public class UpdateProfileDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
    }
}