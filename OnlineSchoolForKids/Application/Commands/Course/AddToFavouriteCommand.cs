using MediatR;

namespace Application.Commands
{
    public class AddToFavouriteCommand : IRequest<AddToFavouriteResponse>
    {
        public string CourseId { get; set; }=  string.Empty;
        public string UserId { get; set; }= string.Empty;
    }

    public class AddToFavouriteResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? FavouriteId { get; set; }
    }
    public class DeleteFromFavouriteCommand : IRequest<DeleteFromFavouriteResponse>
    {
        public string CourseId { get; set; }= string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    public class DeleteFromFavouriteResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
