using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
  [Authorize]
  public class UsersController : BaseApiController
  {
    private readonly IMapper _mapper;
    private readonly IPhotoService _photoService;
    private readonly IUnitOfWork _unitOfWork;

  public UsersController(IUnitOfWork unitOfWork, IMapper mapper, IPhotoService photoService)
  {
      _unitOfWork = unitOfWork;
      _photoService = photoService;
      _mapper = mapper;
  }

  [HttpGet]
  public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers([FromQuery] UserParams userParams)
  {
    var username = User.GetUsername();
    var gender = await _unitOfWork.UserRepository.GetUserGender(username);
    userParams.CurrentUsername = User.GetUsername();
    if (string.IsNullOrWhiteSpace(userParams.Gender)) 
      userParams.Gender = gender == "male" ? "female" : "male";
    
    var users = await _unitOfWork.UserRepository.GetMembersAsync(userParams);
    Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);
    return new OkObjectResult(users);
  }

  [Authorize(Roles = "Member")]
  [HttpGet("{username}", Name = "GetUser")]
  public async Task<ActionResult<MemberDto>> GetUser(string username)
  {
    var currentUsername = User.GetUsername();
    return await _unitOfWork.UserRepository.GetMemberAsync(username, currentUsername == username);
  }

  [HttpPut]
  public async Task<ActionResult> UpdateMember(MemberUpdateDto member)
  {
    var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());
    var appUser = _mapper.Map(member, user);

    _unitOfWork.UserRepository.Update(appUser);
    if(await _unitOfWork.Complete()) return NoContent();

    return BadRequest("Failed to update user");
  }

  [HttpPost("add-photo")]
  public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
  {
    var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());
    var result = await _photoService.AddPhotoAsync(file);
    
    if(result.Error!=null){
      return BadRequest(result.Error.Message);
    }

    var photo = new Photo 
    {
      Url = result.SecureUrl.AbsoluteUri,
      PublicId = result.PublicId,
      IsApproved = false      
    };
    
    user.Photos.Add(photo);
    
    if(await _unitOfWork.Complete()) {

      return CreatedAtRoute("GetUser", new { username = user.UserName },_mapper.Map<PhotoDto>(photo));
    }

    return BadRequest("Problem adding photo");
  }

  [HttpPut("set-main-photo/{photoId}")]
  public async Task<ActionResult> SetMainPhoto(int photoId)
  {
    var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());

    var photo = user.Photos.FirstOrDefault(x=> x.Id == photoId);
    if(photo.IsMain) return BadRequest("This is already your main photo");
    if(!photo.IsApproved) return BadRequest("Unapproved photo cannot be set as main");

    var currentMain = user.Photos.FirstOrDefault(x=> x.IsMain);
    if(currentMain != null ) currentMain.IsMain = false;
    photo.IsMain = true;

    if(await _unitOfWork.Complete()){
      return NoContent();
    }

     return BadRequest("Failed to set main photo");
  }

  [HttpDelete("delete-photo/{photoId}")]
  public async Task<ActionResult> DeletePhoto(int photoId){
    var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());

    var photo = user.Photos.FirstOrDefault(x=> x.Id == photoId);
    if(photo == null) return NotFound();

    if(photo.IsMain) return BadRequest("You cannot delete your main photo");

    if(!string.IsNullOrWhiteSpace(photo.PublicId))
    {
      var result = await _photoService.DeletePhotoAsync(photo.PublicId);
      
      if(result.Error!=null) return BadRequest(result.Error.Message);

      user.Photos.Remove(photo);

      if(await _unitOfWork.Complete()) return Ok();
    }

    return BadRequest("Failed to delete the photo");

  }
 }
}