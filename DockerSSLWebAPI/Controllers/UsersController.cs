using DockerSSLWebAPI.Data;
using DockerSSLWebAPI.Models;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace DockerSSLWebAPI.Controllers
{
    [ApiController]
    [Route("")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok();
        }

        [HttpGet("users")]
        public IEnumerable<UserDto> GetUsers()
        {
            return _context.Users.Select(u => new UserDto
            {
                Name = u.Name,
                Email = u.Email
            });
        }

        [HttpPost("user")]
        public async Task<IActionResult> CreateUser(UserDto newUser)
        {
            var user = new UsersModel
            {
                Name = newUser.Name,
                Email = newUser.Email
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, user);
        }
    }
}
