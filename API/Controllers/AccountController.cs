using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using API.DTOs;
using Microsoft.EntityFrameworkCore;
using API.Interfaces;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        public DataContext DataContext { get; set; }
        public ITokenService TokenService { get; }

        public AccountController(DataContext dataContext, ITokenService tokenService)
        {
            DataContext = dataContext;
            TokenService = tokenService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto logindto)
        {
            var user = await DataContext.User
            .Include(p => p.Photos)
            .SingleOrDefaultAsync(x => x.UserName == logindto.username.ToLower());

            if (user == null) return Unauthorized("Invalid username");

            using var hmac = new HMACSHA512(user.PasswordSalt);

            var computedhash = hmac.ComputeHash(Encoding.UTF8.GetBytes(logindto.password));

            for (int i = 0; i < computedhash.Length; i++)
            {
                if (computedhash[i] != user.PasswordHash[i]) return Unauthorized("Invalid password");
            }

            return new UserDto
            {
                username = user.UserName,
                token = TokenService.CreateToken(user),
                photourl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url
            };


        }


        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerdto)
        {
            if (await CheckUserExists(registerdto.username)) return BadRequest("UserName is taken");
            using var hmac = new HMACSHA512();
            var user = new AppUser
            {
                UserName = registerdto.username.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerdto.password)),
                PasswordSalt = hmac.Key,
            };
            DataContext.User.Add(user);
            await DataContext.SaveChangesAsync();
            return new UserDto
            {
                username = user.UserName,
                token = TokenService.CreateToken(user)
            };
        }

        private async Task<bool> CheckUserExists(string username)
        {
            return await DataContext.User.AnyAsync(x => x.UserName == username.ToLower());
        }
    }
}