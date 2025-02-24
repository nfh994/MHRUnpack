namespace MHRUnpack.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class VerificationAttribute<T> : Attribute
    {
        public T Value { get; set; }
        public VerificationAttribute(T value)
        {
            Value = value;
        }
    }
}
