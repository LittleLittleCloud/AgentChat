namespace AgentChat;

public class Role
{
    private readonly string _name;

    public static Role User { get; } = new("user");

    public static Role Assistant { get; } = new("assistant");

    public static Role System { get; } = new("system");

    public static Role Function { get; } = new("function");

    internal Role(string name)
    {
        _name = name;
    }

    public override string ToString() => _name;
}