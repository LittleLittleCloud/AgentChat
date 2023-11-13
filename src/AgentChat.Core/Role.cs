using System;
using System.Collections.Generic;
using System.Text;

namespace AgentChat
{
    public class Role
    {
        private readonly string _name;

        internal Role(string name)
        {
            _name = name;
        }

        public static Role User { get; } = new Role("user");

        public static Role Assistant { get; } = new Role("assistant");

        public static Role System { get; } = new Role("system");

        public static Role Function { get; } = new Role("function");

        public override string ToString()
        {
            return _name;
        }
    }
}
