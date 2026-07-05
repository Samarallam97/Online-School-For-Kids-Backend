using Application.Commands;
using Application.Commands.Course;
using Application.Queries;
using Localization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;
using static Application.Commands.Course.CreateSectionHandler;
using static Application.Commands.UpdateCourseHandler;
using CreateCourseDto = Application.Commands.CreateCourseDto;


namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "ContentCreator")]
    public class CourseCreatorController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CourseCreatorController> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public CourseCreatorController(
            IMediator mediator,
            ILogger<CourseCreatorController> logger,
            IStringLocalizer<SharedResource> localizer)
        {
            _mediator = mediator;
            _logger = logger;
            _localizer = localizer;
        }


        [HttpGet("courses/mine")]
        public async Task<IActionResult> GetMyCourses(CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();

                var query = new GetMyCoursesQuery { InstructorId = userId };
                var result = await _mediator.Send(query, cancellationToken);

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting instructor's courses");
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
            }
        }

        [HttpPost("courses")]
        public async Task<IActionResult> CreateCourse(
            [FromBody] CreateCourseDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();
                var command = new CreateCourseCommand { Dto = dto, InstructorId = userId };
                var result = await _mediator.Send(command, cancellationToken);

                if (result == null)
                    return BadRequest(new { message = _localizer["FailedToCreateCourse"], success = false });

                return Ok(new
                {
                    data = result,
                    message = _localizer["CourseCreatedSuccessfully"],
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating course");
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
            }
        }
        [HttpPut("courses/{courseId}")]
        public async Task<IActionResult> UpdateCourse(
           string courseId,
           [FromBody] UpdateCourseDto dto,
           CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();
                var command = new UpdateCourseCommand
                {
                    CourseId = courseId,
                    InstructorId = userId,
                    Dto = dto
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return NotFound(new { message = _localizer["CourseNotFound"], success = false });

                return Ok(new { message = _localizer["CourseUpdatedSuccessfully"], success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course");
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
            }
        }
        [HttpDelete("courses/{courseId}")]
        public async Task<IActionResult> DeleteCourse(
            string courseId,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();
                var command = new DeleteCourseCommand { CourseId = courseId, InstructorId = userId };
                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return NotFound(new { message = _localizer["CourseNotFound"], success = false });

                return Ok(new { message = _localizer["CourseDeletedSuccessfully"], success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course");
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
            }
        }
        [HttpPost("courses/{courseId}/publish")]
        public async Task<IActionResult> PublishCourse(
           string courseId,
           [FromBody] PublishCourseRequest request,
           CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();
                var command = new PublishCourseCommand
                {
                    CourseId = courseId,
                    InstructorId = userId,
                    Publish = request.Publish
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return NotFound(new { message = _localizer["CourseNotFound"], success = false });

                return Ok(new
                {
                    message = request.Publish ? _localizer["CoursePublished"] : _localizer["CourseUnPublished"],
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing course");
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
            }
        }
        [HttpPost("sections")]
        public async Task<IActionResult> CreateSection(
           [FromBody] CreateSectionDto dto,
           CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();
                var command = new CreateSectionCommand { Dto = dto, InstructorId = userId };
                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return BadRequest(new { message = "Failed to create section", success = false });

                return Ok(new { message = "Section created successfully", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating section");
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
            }
        }
        [HttpPut("sections/{courseId}/{sectionId}")]
        public async Task<IActionResult> UpdateSection(
                  string courseId,
                  string sectionId,
                  [FromBody] UpdateSectionDto dto,
                  CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();
                var command = new UpdateSectionCommand
                {
                    CourseId = courseId,
                    SectionId = sectionId,
                    InstructorId = userId,
                    Dto = dto
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return NotFound(new { message = _localizer["SectionNotFound"], success = false });

                return Ok(new { message = _localizer["SectionUpdatedSuccessfully"], success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _localizer["ErrorUpdatingSection"]);
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
            }
        }
        [HttpDelete("sections/{courseId}/{sectionId}")]
        public async Task<IActionResult> DeleteSection(
            string courseId,
            string sectionId,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();
                var command = new DeleteSectionCommand
                {
                    CourseId = courseId,
                    SectionId = sectionId,
                    InstructorId = userId
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return NotFound(new { message = _localizer["SectionNotFound"], success = false });

                return Ok(new { message = _localizer["SectionDeletedSuccessfully"], success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting section");
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
            }
        }
        [HttpPost("lessons")]
        public async Task<IActionResult> CreateLesson(
           [FromBody] CreateLessonDto dto,
           CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();
                var command = new CreateLessonCommand { Dto = dto, InstructorId = userId };
                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return BadRequest(new { message = _localizer["FailedToCreateLesson"], success = false });

                return Ok(new { message = _localizer["LessonCreatedSuccessfully"], success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lesson");
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
            }
        }

        [HttpPut("lessons/{courseId}/{sectionId}/{lessonId}")]
        public async Task<IActionResult> UpdateLesson(
            string courseId,
            string sectionId,
            string lessonId,
            [FromBody] UpdateLessonDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();
                var command = new UpdateLessonCommand
                {
                    InstructorId = userId,
                    CourseId = courseId,
                    SectionId = sectionId,
                    LessonId = lessonId,
                    Dto = dto
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return NotFound(new { message = _localizer["LessonNotFound"], success = false });

                return Ok(new { message = _localizer["LessonUpdatedSuccessfully"], success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lesson");
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
            }
        }

        [HttpDelete("lessons/{courseId}/{sectionId}/{lessonId}")]
        public async Task<IActionResult> DeleteLesson(
            string courseId,
            string sectionId,
            string lessonId,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();
                var command = new DeleteLessonCommand
                {
                    InstructorId = userId,
                    CourseId = courseId,
                    SectionId = sectionId,
                    LessonId = lessonId
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return NotFound(new { message = "Lesson not found", success = false });

                return Ok(new { message = _localizer["LessonDeletedSuccessfully"], success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting lesson");
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
            }
        }
        [HttpPost("materials")]
        public async Task<IActionResult> AddMaterial(
           [FromBody] AddMaterialDto dto,
           CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();
                var command = new AddMaterialCommand { Dto = dto, InstructorId = userId };
                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return BadRequest(new { message = _localizer["FailedToAddMaterial"], success = false });

                return Ok(new { message = _localizer["MaterialAddedSuccessfully"], success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding material");
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
            }
        }



        /// <summary>
        /// Full course detail (sections + lessons) for the instructor's course
        /// management page. Works for draft/unpublished courses, unlike the
        /// public GetCourseById endpoint.
        /// GET /api/coursecreator/courses/{courseId}/management
        /// </summary>
        [HttpGet("courses/{courseId}/management")]
        public async Task<IActionResult> GetCourseManagementDetail(
            string courseId,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();

                var query = new GetCourseManagementDetailQuery
                {
                    CourseId = courseId,
                    InstructorId = userId
                };
                var result = await _mediator.Send(query, cancellationToken);

                if (result == null)
                    return NotFound(new { message = "Course not found", success = false });

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course management detail");
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
            }
        }

    }
}