namespace SimpleAggregate
{
    using System;

    public class UnregisteredEventException : Exception
    {
        public UnregisteredEventException(string message) : base(message) { }
    }
}
