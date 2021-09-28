using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AdminController : BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPhotoService _photoService;
        public AdminController(UserManager<AppUser> userManager, IUnitOfWork unitOfWork, IPhotoService photoService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _photoService = photoService;
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("users-with-roles")]
        public async Task<ActionResult> GetUsersWithRoles(){
            var users = await _userManager.Users
            .Include(r=> r.UserRoles)
            .ThenInclude(r => r.Role)
            .OrderBy(x=> x.UserName)
            .Select(u=> new 
            {
                u.Id,
                Username = u.UserName,
                Roles = u.UserRoles.Select(r=> r.Role.Name).ToList()
            })
            .ToListAsync();

            return Ok(users);
        }

        [HttpPost("edit-roles/{username}")]
        public async Task<ActionResult> EditRoles(string username, [FromQuery] string roles)
        {
            var selectedRoles = roles.Split(',').ToArray();
            
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return NotFound("Could not find user");
            
            var userRoles = await _userManager.GetRolesAsync(user);
            
            var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

            if(!result.Succeeded) return BadRequest("Failed to add to roles");

            result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));
            if(!result.Succeeded) return BadRequest("Failed to remove from roles");

            return Ok(await _userManager.GetRolesAsync(user));
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpGet("photos-to-moderate")]
        public async Task<ActionResult<IEnumerable<PhotoForApprovalDto>>> GetPhotosForApproval()
        {
            return new OkObjectResult(await _unitOfWork.PhotoRepository.GetUnapprovedPhotos());
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpPut("approve-photo/{photoId}")]
        public async Task<ActionResult> ApprovePhoto(int photoId)
        {
            var user = await _unitOfWork.UserRepository.GetUserByPhotoId(photoId);
            if(user == null) return NotFound();
            if(user.Photos == null) return NotFound();

            var photo = user.Photos.FirstOrDefault(x=>x.Id == photoId);

            if(photo.IsApproved) return BadRequest("Photo already approved");
            
            photo.IsApproved = true;   

            if(!user.Photos.Any(x=>x.IsMain)){
                photo.IsMain = true;
            }

            if(await _unitOfWork.Complete()){
                return NoContent();
            }

            return BadRequest("Failed to approve main photo");
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpPut("reject-photo/{photoId}")]
        public async Task<ActionResult> RejectPhoto(int photoId)
        {
            var user = await _unitOfWork.UserRepository.GetUserByPhotoId(photoId);
            if(user == null) return NotFound();

            var photo = user.Photos.FirstOrDefault(x=> x.Id == photoId);           

            if(!string.IsNullOrWhiteSpace(photo.PublicId))
            {
                var result = await _photoService.DeletePhotoAsync(photo.PublicId);
                
                if(result.Error!=null) return BadRequest(result.Error.Message);

                user.Photos.Remove(photo);

                if(await _unitOfWork.Complete()) return Ok();
            }
            return BadRequest("Failed to reject the photo");
        }

    }
}