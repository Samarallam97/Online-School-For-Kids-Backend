using Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {

        private readonly IMediator _mediator;
        private readonly IUserRepository _userRepository;

        public StudentController(IMediator mediator, IUserRepository userRepository)
        {
            _mediator = mediator;
            _userRepository = userRepository;
        }

        [HttpGet("courses")]
        [Authorize]
        public async Task<IActionResult> GetEnrolledCourses()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return NotFound();

            // Fetch courses based on user.EnrolledCourseIds
            //var courses = await _courseRepository.GetCoursesByIdsAsync(user.EnrolledCourseIds);


            return Ok();
        }

        [HttpGet("achievements")]
        [Authorize]
        public async Task<IActionResult> GetAchievements()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return NotFound();

            // Fetch achievements based on user.AchievementIds
            //var achievements = await _achievementRepository.GetAchievementsByIdsAsync(user.AchievementIds);

            return Ok();
        }
    }
}
