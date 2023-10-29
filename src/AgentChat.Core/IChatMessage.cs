namespace AgentChat
{
    public interface IChatMessage
    {
        Role Role { get; }

        // TODO
        // add image

        string? Content { get; }

        string? From { get; }
    }

    /// <summary>
    /// A universal chat message that can be used by different chat models.
    /// </summary>
    public class Message : IChatMessage
    {
        public Message(
            Role role,
            string? content = null,
            string? from = null)
        {
            Role = role;
            Content = content;
            From = from;
        }

        public Role Role { get; }

        public string? Content { get; }

        public string? From { get; }
    }
}
