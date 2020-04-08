namespace JsonPathway.Internal
{
    internal class Either<T1, T2>
    {
        private readonly T1 _value1;
        private readonly T2 _value2;

        public T1 Value1
        { 
            get
            {
                if (IsT1) return _value1;
                throw new InternalJsonPathwayException("Accessing value T1 which is not set");
            }
        }

        public T2 Value2
        {
            get
            {
                if (IsT2) return _value2;
                throw new InternalJsonPathwayException("Accessing value T2 which is not set");
            }
        }

        public bool IsT1 { get; }
        public bool IsT2 => !IsT1;

        public Either(T1 value)
        {
            _value1 = value;
            IsT1 = true;
        }

        public Either(T2 value)
        {
            _value2 = value;
        }

        public T Get<T>()
        {
            if (typeof(T) == typeof(T1)) return (T)(object)Value1;
            if (typeof(T) == typeof(T2)) return (T)(object)Value2;

            throw new InternalJsonPathwayException($"Verifying Get<{typeof(T).Name}> which is neither T1 {typeof(T1).Name} not T2 {typeof(T2).Name}");
        }

        public bool Is<T>()
        {
            if (typeof(T) == typeof(T1))
            {
                return IsT1;
            }

            if (typeof(T) == typeof(T1))
            {
                return IsT2;
            }

            throw new InternalJsonPathwayException($"Verifying Is<{typeof(T).Name}> which is neither T1 {typeof(T1).Name} not T2 {typeof(T2).Name}");
        }
    }
}
