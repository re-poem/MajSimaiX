using System;
using System.Collections.Generic;
using System.Text;

namespace MajSimai
{
    public readonly struct SimaiCommand : IEquatable<SimaiCommand>
    {
        public string Prefix { get; }
        public string Value { get; }

        readonly int _hashCode;
        public SimaiCommand(string prefix, string value)
        {
            Prefix = prefix;
            Value = value;
            _hashCode = HashCode.Combine(prefix, value);
        }

        public bool Equals(SimaiCommand cmd)
        {
            return cmd._hashCode == _hashCode;
        }
        public override bool Equals(object? cmd)
        {
            if (cmd is SimaiCommand cmd2)
            {
                return cmd2._hashCode == _hashCode;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return _hashCode;
        }
        public static bool operator ==(SimaiCommand left, SimaiCommand right)
        {
            return left._hashCode == right._hashCode;
        }
        public static bool operator !=(SimaiCommand left, SimaiCommand right)
        {
            return left._hashCode != right._hashCode;
        }
        public static implicit operator KeyValuePair<string, string>(SimaiCommand cmd)
        {
            return new KeyValuePair<string, string>(cmd.Prefix, cmd.Value);
        }
    }
}
