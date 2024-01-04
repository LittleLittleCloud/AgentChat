using System;

namespace AgentChat;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class FunctionAttribution : Attribute
{
    public string? FunctionName { get; }

    public string? Description { get; }

    public FunctionAttribution(string? functionName = null, string? description = null)
    {
        FunctionName = functionName;
        Description = description;
    }
}