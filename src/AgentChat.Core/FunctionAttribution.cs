using System;
using System.Collections.Generic;
using System.Text;

namespace AgentChat.Core
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
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
}
