using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using API.Interfaces;
using AutoMapper;
using API.DTOs;
using System.Security.Claims;
using API.Extensions;

namespace API.Controllers
{

    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IuserRepository _repository;
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;

        public UsersController(IuserRepository repository, IMapper mapper, IPhotoService photoService)
        {
            _repository = repository;
            _mapper = mapper;
            _photoService = photoService;
        }

        [HttpGet]

        public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers()
        {
            var users = await _repository.GetMembersAsync();

            return Ok(users);

        }

        // [HttpGet("{id}")]
        // public async Task<ActionResult<AppUser>> GetUserByID(int id)
        // {

        //     return Ok(await _repository.GetUserByIdAysnc(id));
        // }

        [HttpGet("{username}", Name = "GetUser")]
        public async Task<ActionResult<MemberDto>> GetUser(string username)
        {
            var user = await _repository.GetMemberAsync(username);



            return Ok(user);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
        {
            var username = User.GetUserName();

            var user = await _repository.GetUserBynameAysnc(username);

            _mapper.Map(memberUpdateDto, user);

            _repository.Update(user);

            if (await _repository.SaveAllAsync()) return NoContent();

            else return BadRequest("Failed to update user");

        }


        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
        {
            var user = await _repository.GetUserBynameAysnc(User.GetUserName());

            var result = await _photoService.AddPhotoAsync(file);

            if (result.Error != null) return BadRequest(result.Error.Message);

            var photo = new Photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId,
            };

            if (user.Photos.Count == 0)
            {
                photo.IsMain = true;
            }
            user.Photos.Add(photo);

            if (await _repository.SaveAllAsync())
            {
                return CreatedAtRoute("GetUser", new { username = user.UserName },
                 _mapper.Map<PhotoDto>(photo));
            }

            return BadRequest("Problem Occured adding Photo");
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {


            var user = await _repository.GetUserBynameAysnc(User.GetUserName());

            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

            if (photo.IsMain) return BadRequest("This is already your main photo");

            var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);

            if (currentMain != null) currentMain.IsMain = false;

            photo.IsMain = true;

            if (await _repository.SaveAllAsync()) return NoContent();

            return BadRequest("Failed to set Main Photo");
        }

    }
}