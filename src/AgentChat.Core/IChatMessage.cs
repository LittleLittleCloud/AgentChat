namespace AgentChat
{
    public interface IChatMessage
    {
        Role Role { get; set; }

        // TODO
        // add image

        string? Content { get; set; }

        string? From { get; set; }

        AgentFunction? Function { get; set; }
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

        public Role Role { get; set; }

        public string? Content { get; set; }

        public string? From { get; set; }

        public AgentFunction? Function { get; set;}
    }
}
