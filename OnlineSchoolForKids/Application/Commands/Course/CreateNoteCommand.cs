using Domain.Entities.Content.Progress;
using Domain.Interfaces.Repositories.Content;
using MediatR;

namespace Application.Commands.Course
{
    public class CreateNoteCommand : IRequest<NoteDto?>
    {
        public string UserId { get; set; } = string.Empty;
        public CreateNoteDto Dto { get; set; } = default!;
    }

    public class CreateNoteCommandHandler
    : IRequestHandler<CreateNoteCommand, NoteDto?>
    {
        private readonly INoteRepository _noteRepository;

        public CreateNoteCommandHandler(INoteRepository noteRepository)
        {
            _noteRepository = noteRepository;
        }

        public async Task<NoteDto?> Handle(
            CreateNoteCommand request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Dto.Content))
                return null;

            var note = new Note
            {
                UserId = request.UserId,
                CourseId = request.Dto.CourseId,
                LessonId = request.Dto.LessonId,
                Content = request.Dto.Content,
                VideoPosition = request.Dto.VideoPosition,
                CreatedAt = DateTime.UtcNow
            };

            await _noteRepository.CreateAsync(note, cancellationToken);

            return new NoteDto
            {
                Id = note.Id,
                LessonId = note.LessonId,
                Content = note.Content,
                VideoPosition = note.VideoPosition,
                CreatedAt = note.CreatedAt
            };
        }
    }
    public class NoteDto
    {
        public string Id { get; set; } = string.Empty;
        public string LessonId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int? VideoPosition { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class CreateNoteDto
    {
        public string CourseId { get; set; } = string.Empty;
        public string LessonId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int? VideoPosition { get; set; }
    }
}
