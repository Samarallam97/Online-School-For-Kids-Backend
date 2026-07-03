using Application.Commands;
using Application.Commands.Course;
using Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public CourseCreatorController(
            IMediator mediator,
            ILogger<CourseCreatorController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        [HttpGet("courses/mine")]
        public async Task<IActionResult> GetMyCourses(CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var query = new GetMyCoursesQuery { InstructorId = userId };
                var result = await _mediator.Send(query, cancellationToken);

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting instructor's courses");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        [HttpPost("courses")]
        public async Task<IActionResult> CreateCourse(
            [FromBody] CreateCourseDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var command = new CreateCourseCommand { Dto = dto, InstructorId = userId };
                var result = await _mediator.Send(command, cancellationToken);

                if (result == null)
                    return BadRequest(new { message = "Failed to create course", success = false });

                return Ok(new
                {
                    data = result,
                    message = "Course created successfully",
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating course");
                return StatusCode(500, new { message = "An error occurred", success = false });
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
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var command = new UpdateCourseCommand
                {
                    CourseId = courseId,
                    InstructorId = userId,
                    Dto = dto
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return NotFound(new { message = "Course not found", success = false });

                return Ok(new { message = "Course updated successfully", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        [HttpDelete("courses/{courseId}")]
        public async Task<IActionResult> DeleteCourse(
            string courseId,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var command = new DeleteCourseCommand { CourseId = courseId, InstructorId = userId };
                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return NotFound(new { message = "Course not found", success = false });

                return Ok(new { message = "Course deleted successfully", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course");
                return StatusCode(500, new { message = "An error occurred", success = false });
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
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var command = new PublishCourseCommand
                {
                    CourseId = courseId,
                    InstructorId = userId,
                    Publish = request.Publish
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return NotFound(new { message = "Course not found", success = false });

                return Ok(new
                {
                    message = request.Publish ? "Course published" : "Course unpublished",
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing course");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        [HttpPost("sections")]
        public async Task<IActionResult> CreateSection(
           [FromBody] CreateSectionDto dto,
           CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var command = new CreateSectionCommand { Dto = dto, InstructorId = userId };
                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return BadRequest(new { message = "Failed to create section", success = false });

                return Ok(new { message = "Section created successfully", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating section");
                return StatusCode(500, new { message = "An error occurred", success = false });
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
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var command = new UpdateSectionCommand
                {
                    CourseId = courseId,
                    SectionId = sectionId,
                    InstructorId = userId,
                    Dto = dto
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return NotFound(new { message = "Section not found", success = false });

                return Ok(new { message = "Section updated successfully", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating section");
                return StatusCode(500, new { message = "An error occurred", success = false });
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
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var command = new DeleteSectionCommand
                {
                    CourseId = courseId,
                    SectionId = sectionId,
                    InstructorId = userId
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return NotFound(new { message = "Section not found", success = false });

                return Ok(new { message = "Section deleted successfully", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting section");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        [HttpPost("lessons")]
        public async Task<IActionResult> CreateLesson(
           [FromBody] CreateLessonDto dto,
           CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var command = new CreateLessonCommand { Dto = dto, InstructorId = userId };
                var result = await _mediator.Send(command, cancellationToken);

                if (!result.Success)
                    return BadRequest(new { message = result.Message, success = false });

                return Ok(new
                {
                    data = new { lessonId = result.LessonId },
                    message = result.Message,
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lesson");
                return StatusCode(500, new { message = "An error occurred", success = false });
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
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
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
                    return NotFound(new { message = "Lesson not found", success = false });

                return Ok(new { message = "Lesson updated successfully", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lesson");
                return StatusCode(500, new { message = "An error occurred", success = false });
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
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
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

                return Ok(new { message = "Lesson deleted successfully", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting lesson");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        /// <summary>PUT /api/coursecreator/lessons/{courseId}/{sectionId}/{lessonId}/quiz/{difficulty}
        /// Edits questions for one difficulty on an already-saved lesson.</summary>
        [HttpPut("lessons/{courseId}/{sectionId}/{lessonId}/quiz/{difficulty}")]
        public async Task<IActionResult> UpdateLessonQuiz(
            string courseId,
            string sectionId,
            string lessonId,
            string difficulty,
            [FromBody] List<UpdateLessonQuizQuestionDto> questions,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var command = new UpdateLessonQuizCommand
                {
                    InstructorId = userId,
                    CourseId = courseId,
                    SectionId = sectionId,
                    LessonId = lessonId,
                    Difficulty = difficulty,
                    Questions = questions
                };

                var result = await _mediator.Send(command, cancellationToken);
                if (!result)
                    return NotFound(new { message = "Lesson not found", success = false });

                return Ok(new { message = "Quiz updated successfully", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lesson quiz");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        // ── Materials ────────────────────────────────────────────────────

        [HttpPost("materials")]
        public async Task<IActionResult> AddMaterial(
           [FromBody] AddMaterialDto dto,
           CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var command = new AddMaterialCommand { Dto = dto, InstructorId = userId };
                var result = await _mediator.Send(command, cancellationToken);

                if (!result.Success)
                    return BadRequest(new { message = result.Message, success = false });

                return Ok(new
                {
                    data = new { materialId = result.MaterialId },
                    message = result.Message,
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding material");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        [HttpPut("materials")]
        public async Task<IActionResult> UpdateMaterial(
            [FromBody] UpdateMaterialDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var command = new UpdateMaterialCommand { Dto = dto, InstructorId = userId };
                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return NotFound(new { message = "Material not found", success = false });

                return Ok(new { message = "Material updated successfully", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating material");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        [HttpDelete("materials/{courseId}/{sectionId}/{lessonId}/{materialId}")]
        public async Task<IActionResult> DeleteMaterial(
            string courseId,
            string sectionId,
            string lessonId,
            string materialId,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var command = new DeleteMaterialCommand
                {
                    InstructorId = userId,
                    CourseId = courseId,
                    SectionId = sectionId,
                    LessonId = lessonId,
                    MaterialId = materialId
                };
                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return NotFound(new { message = "Material not found", success = false });

                return Ok(new { message = "Material deleted successfully", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting material");
                return StatusCode(500, new { message = "An error occurred", success = false });
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
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

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
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
    }
}