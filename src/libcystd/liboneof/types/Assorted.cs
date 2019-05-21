
namespace LibCyStd.LibOneOf.Types
{
    public readonly struct None
    {
        public static None Value { get; }

        static None()
        {
            Value = new None();
        }
    }
}
