namespace Domain.Entities.Live
{
    public class LiveSession : BaseEntity
    {
        public string HostId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }

        /// <summary>Unique room identifier used by the WebRTC / Jitsi layer.</summary>
        public string ChannelName { get; set; } = string.Empty;

        /// <summary>live | ended | scheduled</summary>
        public string Status { get; set; } = "scheduled";

        public bool AllowChat { get; set; } = true;
        public bool AllowQuestions { get; set; } = true;

        public DateTime? ScheduledAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }

        /// <summary>Snapshot count — updated by the presence hub.</summary>
        public int ViewerCount { get; set; } = 0;

        /// <summary>URL of the saved whiteboard PNG after the session ends.</summary>
        public string? WhiteboardUrl { get; set; }
    }

}
