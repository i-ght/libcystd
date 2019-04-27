using System;
using System.Collections.Generic;

namespace LibCyStd.LibOneOf
{
    public readonly struct OneOf<T0> : IOneOf
    {
        private readonly int _index;
        private readonly T0 _value0;

        private OneOf(in int index, in T0 value0 = default)
        {
            _index = index;
            _value0 = value0;
        }

        public object? Value
        {
            get
            {
                switch (_index)
                {
                    case 0:
                        return _value0!;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        public bool IsT0 => _index == 0;

        public T0 T0Value
        {
            get
            {
                if (_index != 0)
                {
                    throw new InvalidOperationException($"Cannot return as T0 as result is T{_index}");
                }
                return _value0;
            }
        }

        public static implicit operator OneOf<T0>(in T0 t) => new OneOf<T0>(0, value0: t);

        public void Switch(in Action<T0> f0)
        {
            if (_index == 0 && f0 != null)
            {
                f0(_value0);
                return;
            }
            throw new InvalidOperationException();
        }

        public TResult Match<TResult>(in Func<T0, TResult> f0)
        {
            if (_index == 0 && f0 != null)
            {
                return f0(_value0);
            }
            throw new InvalidOperationException();
        }

        public static OneOf<T0> FromT0(in T0 input)
        {
            return input;
        }

        public OneOf<TResult> MapT0<TResult>(Func<T0, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<TResult>>(
                input0 => mapFunc(input0)
            );
        }

        public bool Equals(in OneOf<T0> other) =>
            _index == other._index
            && EqualityComparer<T0>.Default.Equals(_value0, other._value0);

        public override bool Equals(object obj)
        {
            return !(obj is OneOf<T0> other) ? false : Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = -825743661;
            hashCode = (hashCode * -1521134295) + _index.GetHashCode();
            return (hashCode * -1521134295) + EqualityComparer<T0>.Default.GetHashCode(_value0);
        }
    }

    public readonly struct OneOf<T0, T1> : IOneOf
    {
        private readonly int _index;
        private readonly T0 _value0;
        private readonly T1 _value1;

        private OneOf(int index, T0 value0 = default, T1 value1 = default)
        {
            _index = index;
            _value0 = value0;
            _value1 = value1;
        }

        public object? Value
        {
            get
            {
                switch (_index)
                {
                    case 0:
                        return _value0!;
                    case 1:
                        return _value1!;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        public bool IsT0 => _index == 0;

        public T0 T0Value
        {
            get
            {
                if (_index != 0)
                {
                    throw new InvalidOperationException($"Cannot return as T0 as result is T{_index}");
                }
                return _value0;
            }
        }

        public bool IsT1 => _index == 1;

        public T1 T1Value
        {
            get
            {
                if (_index != 1)
                {
                    throw new InvalidOperationException($"Cannot return as T1 as result is T{_index}");
                }
                return _value1;
            }
        }

        public static implicit operator OneOf<T0, T1>(T0 t) => new OneOf<T0, T1>(0, value0: t);
        public static implicit operator OneOf<T0, T1>(T1 t) => new OneOf<T0, T1>(1, value1: t);

        public void Switch(Action<T0> f0, Action<T1> f1)
        {
            if (_index == 0 && f0 != null)
            {
                f0(_value0);
                return;
            }
            if (_index == 1 && f1 != null)
            {
                f1(_value1);
                return;
            }
            throw new InvalidOperationException();
        }

        public TResult Match<TResult>(Func<T0, TResult> f0, Func<T1, TResult> f1)
        {
            if (_index == 0 && f0 != null)
            {
                return f0(_value0);
            }
            if (_index == 1 && f1 != null)
            {
                return f1(_value1);
            }
            throw new InvalidOperationException();
        }

        public static OneOf<T0, T1> FromT0(T0 input)
        {
            return input;
        }

        public static OneOf<T0, T1> FromT1(T1 input)
        {
            return input;
        }

        public OneOf<TResult, T1> MapT0<TResult>(Func<T0, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<TResult, T1>>(
                input0 => mapFunc(input0),
                input1 => input1
            );
        }

        public OneOf<T0, TResult> MapT1<TResult>(Func<T1, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, TResult>>(
                input0 => input0,
                input1 => mapFunc(input1)
            );
        }

        public bool TryPickT0(out T0 value, out T1 remainder)
        {
            value = IsT0 ? T0Value : default!;
            remainder = IsT0 ? default! : T1Value;
            return IsT0;
        }

        public bool TryPickT1(out T1 value, out T0 remainder)
        {
            value = IsT1 ? T1Value : default!;
            remainder = IsT1 ? default! : T0Value;
            return IsT1;
        }

        private bool Equals(in OneOf<T0, T1> other)
        {
            if (_index != other._index)
            {
                return false;
            }
            switch (_index)
            {
                case 0: return Equals(_value0, other._value0);
                case 1: return Equals(_value1, other._value1);
                default: return false;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is OneOf<T0, T1> oneOf && Equals(oneOf);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode;
                switch (_index)
                {
                    case 0:
                        hashCode = _value0?.GetHashCode() ?? 0;
                        break;
                    case 1:
                        hashCode = _value1?.GetHashCode() ?? 0;
                        break;
                    default:
                        hashCode = 0;
                        break;
                }
                return (hashCode * 397) ^ _index;
            }
        }

        public void Deconstruct(out int index, out T0 value0, out T1 value1)
        {
            index = _index;
            value0 = _value0;
            value1 = _value1;
        }
    }

    public readonly struct OneOf<T0, T1, T2> : IOneOf
    {
        private readonly T0 _value0;
        private readonly T1 _value1;
        private readonly T2 _value2;
        private readonly int _index;

        private OneOf(int index, T0 value0 = default, T1 value1 = default, T2 value2 = default)
        {
            _index = index;
            _value0 = value0;
            _value1 = value1;
            _value2 = value2;
        }

        public object? Value
        {
            get
            {
                switch (_index)
                {
                    case 0:
                        return _value0!;
                    case 1:
                        return _value1!;
                    case 2:
                        return _value2!;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        public bool IsT0 => _index == 0;

        public T0 T0Value
        {
            get
            {
                if (_index != 0)
                {
                    throw new InvalidOperationException($"Cannot return as T0 as result is T{_index}");
                }
                return _value0;
            }
        }

        public static implicit operator OneOf<T0, T1, T2>(T0 t) => new OneOf<T0, T1, T2>(0, value0: t);

        public bool IsT1 => _index == 1;

        public T1 T1Value
        {
            get
            {
                if (_index != 1)
                {
                    throw new InvalidOperationException($"Cannot return as T1 as result is T{_index}");
                }
                return _value1;
            }
        }

        public static implicit operator OneOf<T0, T1, T2>(T1 t) => new OneOf<T0, T1, T2>(1, value1: t);

        public bool IsT2 => _index == 2;

        public T2 T2Value
        {
            get
            {
                if (_index != 2)
                {
                    throw new InvalidOperationException($"Cannot return as T2 as result is T{_index}");
                }
                return _value2;
            }
        }

        public static implicit operator OneOf<T0, T1, T2>(T2 t) => new OneOf<T0, T1, T2>(2, value2: t);

        public void Switch(Action<T0> f0, Action<T1> f1, Action<T2> f2)
        {
            if (_index == 0 && f0 != null)
            {
                f0(_value0);
                return;
            }
            if (_index == 1 && f1 != null)
            {
                f1(_value1);
                return;
            }
            if (_index == 2 && f2 != null)
            {
                f2(_value2);
                return;
            }
            throw new InvalidOperationException();
        }

        public TResult Match<TResult>(Func<T0, TResult> f0, Func<T1, TResult> f1, Func<T2, TResult> f2)
        {
            if (_index == 0 && f0 != null)
            {
                return f0(_value0);
            }
            if (_index == 1 && f1 != null)
            {
                return f1(_value1);
            }
            if (_index == 2 && f2 != null)
            {
                return f2(_value2);
            }
            throw new InvalidOperationException();
        }

        public static OneOf<T0, T1, T2> FromT0(T0 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2> FromT1(T1 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2> FromT2(T2 input)
        {
            return input;
        }

        public OneOf<TResult, T1, T2> MapT0<TResult>(Func<T0, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<TResult, T1, T2>>(
                input0 => mapFunc(input0),
                input1 => input1,
                input2 => input2
            );
        }

        public OneOf<T0, TResult, T2> MapT1<TResult>(Func<T1, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, TResult, T2>>(
                input0 => input0,
                input1 => mapFunc(input1),
                input2 => input2
            );
        }

        public OneOf<T0, T1, TResult> MapT2<TResult>(Func<T2, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, TResult>>(
                input0 => input0,
                input1 => input1,
                input2 => mapFunc(input2)
            );
        }

        public bool TryPickT0(out T0 value, out OneOf<T1, T2> remainder)
        {
            value = IsT0 ? T0Value : default!;
            remainder = IsT0
                ? default
                : Match<OneOf<T1, T2>>(_ => throw new InvalidOperationException(), t1 => t1, t2 => t2);
            return IsT0;
        }

        public bool TryPickT1(out T1 value, out OneOf<T0, T2> remainder)
        {
            value = IsT1 ? T1Value : default!;
            remainder = IsT1
                ? default
                : Match<OneOf<T0, T2>>(t0 => t0, _ => throw new InvalidOperationException(), t2 => t2);
            return IsT1;
        }

        public bool TryPickT2(out T2 value, out OneOf<T0, T1> remainder)
        {
            value = IsT2 ? T2Value : default!;
            remainder = IsT2
                ? default
                : Match<OneOf<T0, T1>>(t0 => t0, t1 => t1, _ => throw new InvalidOperationException());
            return IsT2;
        }

        private bool Equals(in OneOf<T0, T1, T2> other)
        {
            if (_index != other._index)
            {
                return false;
            }
            switch (_index)
            {
                case 0: return Equals(_value0, other._value0);
                case 1: return Equals(_value1, other._value1);
                case 2: return Equals(_value2, other._value2);
                default: return false;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is OneOf<T0, T1, T2> oneOf && Equals(oneOf);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode;
                switch (_index)
                {
                    case 0:
                        hashCode = _value0?.GetHashCode() ?? 0;
                        break;
                    case 1:
                        hashCode = _value1?.GetHashCode() ?? 0;
                        break;
                    case 2:
                        hashCode = _value2?.GetHashCode() ?? 0;
                        break;
                    default:
                        hashCode = 0;
                        break;
                }
                return (hashCode * 397) ^ _index;
            }
        }

        public void Deconstruct(out int index, out T0 value0, out T1 value1, out T2 value2)
        {
            index = _index;
            value0 = _value0;
            value1 = _value1;
            value2 = _value2;
        }
    }

    public readonly struct OneOf<T0, T1, T2, T3> : IOneOf
    {
        private readonly T0 _value0;
        private readonly T1 _value1;
        private readonly T2 _value2;
        private readonly T3 _value3;
        private readonly int _index;

        private OneOf(int index, T0 value0 = default, T1 value1 = default, T2 value2 = default, T3 value3 = default)
        {
            _index = index;
            _value0 = value0;
            _value1 = value1;
            _value2 = value2;
            _value3 = value3;
        }

        public object? Value
        {
            get
            {
                switch (_index)
                {
                    case 0:
                        return _value0!;
                    case 1:
                        return _value1!;
                    case 2:
                        return _value2!;
                    case 3:
                        return _value3!;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        public bool IsT0 => _index == 0;

        public T0 T0Value
        {
            get
            {
                if (_index != 0)
                {
                    throw new InvalidOperationException($"Cannot return as T0 as result is T{_index}");
                }
                return _value0;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3>(T0 t) => new OneOf<T0, T1, T2, T3>(0, value0: t);

        public bool IsT1 => _index == 1;

        public T1 T1Value
        {
            get
            {
                if (_index != 1)
                {
                    throw new InvalidOperationException($"Cannot return as T1 as result is T{_index}");
                }
                return _value1;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3>(T1 t) => new OneOf<T0, T1, T2, T3>(1, value1: t);

        public bool IsT2 => _index == 2;

        public T2 T2Value
        {
            get
            {
                if (_index != 2)
                {
                    throw new InvalidOperationException($"Cannot return as T2 as result is T{_index}");
                }
                return _value2;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3>(T2 t) => new OneOf<T0, T1, T2, T3>(2, value2: t);

        public bool IsT3 => _index == 3;

        public T3 T3Value
        {
            get
            {
                if (_index != 3)
                {
                    throw new InvalidOperationException($"Cannot return as T3 as result is T{_index}");
                }
                return _value3;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3>(T3 t) => new OneOf<T0, T1, T2, T3>(3, value3: t);

        public void Switch(Action<T0> f0, Action<T1> f1, Action<T2> f2, Action<T3> f3)
        {
            if (_index == 0 && f0 != null)
            {
                f0(_value0);
                return;
            }
            if (_index == 1 && f1 != null)
            {
                f1(_value1);
                return;
            }
            if (_index == 2 && f2 != null)
            {
                f2(_value2);
                return;
            }
            if (_index == 3 && f3 != null)
            {
                f3(_value3);
                return;
            }
            throw new InvalidOperationException();
        }

        public TResult Match<TResult>(Func<T0, TResult> f0, Func<T1, TResult> f1, Func<T2, TResult> f2, Func<T3, TResult> f3)
        {
            if (_index == 0 && f0 != null)
            {
                return f0(_value0);
            }
            if (_index == 1 && f1 != null)
            {
                return f1(_value1);
            }
            if (_index == 2 && f2 != null)
            {
                return f2(_value2);
            }
            if (_index == 3 && f3 != null)
            {
                return f3(_value3);
            }
            throw new InvalidOperationException();
        }

        public static OneOf<T0, T1, T2, T3> FromT0(T0 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3> FromT1(T1 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3> FromT2(T2 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3> FromT3(T3 input)
        {
            return input;
        }

        public OneOf<TResult, T1, T2, T3> MapT0<TResult>(Func<T0, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<TResult, T1, T2, T3>>(
                input0 => mapFunc(input0),
                input1 => input1,
                input2 => input2,
                input3 => input3
            );
        }

        public OneOf<T0, TResult, T2, T3> MapT1<TResult>(Func<T1, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, TResult, T2, T3>>(
                input0 => input0,
                input1 => mapFunc(input1),
                input2 => input2,
                input3 => input3
            );
        }

        public OneOf<T0, T1, TResult, T3> MapT2<TResult>(Func<T2, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, TResult, T3>>(
                input0 => input0,
                input1 => input1,
                input2 => mapFunc(input2),
                input3 => input3
            );
        }

        public OneOf<T0, T1, T2, TResult> MapT3<TResult>(Func<T3, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, T2, TResult>>(
                input0 => input0,
                input1 => input1,
                input2 => input2,
                input3 => mapFunc(input3)
            );
        }

        public bool TryPickT0(out T0 value, out OneOf<T1, T2, T3> remainder)
        {
            value = IsT0 ? T0Value : default!;
            remainder = IsT0
                ? default
                : Match<OneOf<T1, T2, T3>>(_ => throw new InvalidOperationException(), t1 => t1, t2 => t2, t3 => t3);
            return IsT0;
        }

        public bool TryPickT1(out T1 value, out OneOf<T0, T2, T3> remainder)
        {
            value = IsT1 ? T1Value : default!;
            remainder = IsT1
                ? default
                : Match<OneOf<T0, T2, T3>>(t0 => t0, _ => throw new InvalidOperationException(), t2 => t2, t3 => t3);
            return IsT1;
        }

        public bool TryPickT2(out T2 value, out OneOf<T0, T1, T3> remainder)
        {
            value = IsT2 ? T2Value : default!;
            remainder = IsT2
                ? default
                : Match<OneOf<T0, T1, T3>>(t0 => t0, t1 => t1, _ => throw new InvalidOperationException(), t3 => t3);
            return IsT2;
        }

        public bool TryPickT3(out T3 value, out OneOf<T0, T1, T2> remainder)
        {
            value = IsT3 ? T3Value : default!;
            remainder = IsT3
                ? default
                : Match<OneOf<T0, T1, T2>>(t0 => t0, t1 => t1, t2 => t2, _ => throw new InvalidOperationException());
            return IsT3;
        }

        private bool Equals(in OneOf<T0, T1, T2, T3> other)
        {
            if (_index != other._index)
            {
                return false;
            }
            switch (_index)
            {
                case 0: return Equals(_value0, other._value0);
                case 1: return Equals(_value1, other._value1);
                case 2: return Equals(_value2, other._value2);
                case 3: return Equals(_value3, other._value3);
                default: return false;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is OneOf<T0, T1, T2, T3> oneOf && Equals(oneOf);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode;
                switch (_index)
                {
                    case 0:
                        hashCode = _value0?.GetHashCode() ?? 0;
                        break;
                    case 1:
                        hashCode = _value1?.GetHashCode() ?? 0;
                        break;
                    case 2:
                        hashCode = _value2?.GetHashCode() ?? 0;
                        break;
                    case 3:
                        hashCode = _value3?.GetHashCode() ?? 0;
                        break;
                    default:
                        hashCode = 0;
                        break;
                }
                return (hashCode * 397) ^ _index;
            }
        }

        public void Deconstruct(out int index, out T0 value0, out T1 value1, out T2 value2, out T3 value3)
        {
            index = _index;
            value0 = _value0;
            value1 = _value1;
            value2 = _value2;
            value3 = _value3;
        }
    }

    public readonly struct OneOf<T0, T1, T2, T3, T4> : IOneOf
    {
        private readonly T0 _value0;
        private readonly T1 _value1;
        private readonly T2 _value2;
        private readonly T3 _value3;
        private readonly T4 _value4;
        private readonly int _index;

        private OneOf(int index, T0 value0 = default, T1 value1 = default, T2 value2 = default, T3 value3 = default, T4 value4 = default)
        {
            _index = index;
            _value0 = value0;
            _value1 = value1;
            _value2 = value2;
            _value3 = value3;
            _value4 = value4;
        }

        public object? Value
        {
            get
            {
                switch (_index)
                {
                    case 0:
                        return _value0!;
                    case 1:
                        return _value1!;
                    case 2:
                        return _value2!;
                    case 3:
                        return _value3!;
                    case 4:
                        return _value4!;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        public bool IsT0 => _index == 0;

        public T0 T0Value
        {
            get
            {
                if (_index != 0)
                {
                    throw new InvalidOperationException($"Cannot return as T0 as result is T{_index}");
                }
                return _value0;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4>(T0 t) => new OneOf<T0, T1, T2, T3, T4>(0, value0: t);

        public bool IsT1 => _index == 1;

        public T1 T1Value
        {
            get
            {
                if (_index != 1)
                {
                    throw new InvalidOperationException($"Cannot return as T1 as result is T{_index}");
                }
                return _value1;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4>(T1 t) => new OneOf<T0, T1, T2, T3, T4>(1, value1: t);

        public bool IsT2 => _index == 2;

        public T2 T2Value
        {
            get
            {
                if (_index != 2)
                {
                    throw new InvalidOperationException($"Cannot return as T2 as result is T{_index}");
                }
                return _value2;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4>(T2 t) => new OneOf<T0, T1, T2, T3, T4>(2, value2: t);

        public bool IsT3 => _index == 3;

        public T3 T3Value
        {
            get
            {
                if (_index != 3)
                {
                    throw new InvalidOperationException($"Cannot return as T3 as result is T{_index}");
                }
                return _value3;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4>(T3 t) => new OneOf<T0, T1, T2, T3, T4>(3, value3: t);

        public bool IsT4 => _index == 4;

        public T4 T4Value
        {
            get
            {
                if (_index != 4)
                {
                    throw new InvalidOperationException($"Cannot return as T4 as result is T{_index}");
                }
                return _value4;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4>(T4 t) => new OneOf<T0, T1, T2, T3, T4>(4, value4: t);

        public void Switch(Action<T0> f0, Action<T1> f1, Action<T2> f2, Action<T3> f3, Action<T4> f4)
        {
            if (_index == 0 && f0 != null)
            {
                f0(_value0);
                return;
            }
            if (_index == 1 && f1 != null)
            {
                f1(_value1);
                return;
            }
            if (_index == 2 && f2 != null)
            {
                f2(_value2);
                return;
            }
            if (_index == 3 && f3 != null)
            {
                f3(_value3);
                return;
            }
            if (_index == 4 && f4 != null)
            {
                f4(_value4);
                return;
            }
            throw new InvalidOperationException();
        }

        public TResult Match<TResult>(Func<T0, TResult> f0, Func<T1, TResult> f1, Func<T2, TResult> f2, Func<T3, TResult> f3, Func<T4, TResult> f4)
        {
            if (_index == 0 && f0 != null)
            {
                return f0(_value0);
            }
            if (_index == 1 && f1 != null)
            {
                return f1(_value1);
            }
            if (_index == 2 && f2 != null)
            {
                return f2(_value2);
            }
            if (_index == 3 && f3 != null)
            {
                return f3(_value3);
            }
            if (_index == 4 && f4 != null)
            {
                return f4(_value4);
            }
            throw new InvalidOperationException();
        }

        public static OneOf<T0, T1, T2, T3, T4> FromT0(T0 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4> FromT1(T1 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4> FromT2(T2 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4> FromT3(T3 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4> FromT4(T4 input)
        {
            return input;
        }

        public OneOf<TResult, T1, T2, T3, T4> MapT0<TResult>(Func<T0, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<TResult, T1, T2, T3, T4>>(
                input0 => mapFunc(input0),
                input1 => input1,
                input2 => input2,
                input3 => input3,
                input4 => input4
            );
        }

        public OneOf<T0, TResult, T2, T3, T4> MapT1<TResult>(Func<T1, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, TResult, T2, T3, T4>>(
                input0 => input0,
                input1 => mapFunc(input1),
                input2 => input2,
                input3 => input3,
                input4 => input4
            );
        }

        public OneOf<T0, T1, TResult, T3, T4> MapT2<TResult>(Func<T2, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, TResult, T3, T4>>(
                input0 => input0,
                input1 => input1,
                input2 => mapFunc(input2),
                input3 => input3,
                input4 => input4
            );
        }

        public OneOf<T0, T1, T2, TResult, T4> MapT3<TResult>(Func<T3, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, T2, TResult, T4>>(
                input0 => input0,
                input1 => input1,
                input2 => input2,
                input3 => mapFunc(input3),
                input4 => input4
            );
        }

        public OneOf<T0, T1, T2, T3, TResult> MapT4<TResult>(Func<T4, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, T2, T3, TResult>>(
                input0 => input0,
                input1 => input1,
                input2 => input2,
                input3 => input3,
                input4 => mapFunc(input4)
            );
        }

        public bool TryPickT0(out T0 value, out OneOf<T1, T2, T3, T4> remainder)
        {
            value = IsT0 ? T0Value : default!;
            remainder = IsT0
                ? default
                : Match<OneOf<T1, T2, T3, T4>>(_ => throw new InvalidOperationException(), t1 => t1, t2 => t2, t3 => t3, t4 => t4);
            return IsT0;
        }

        public bool TryPickT1(out T1 value, out OneOf<T0, T2, T3, T4> remainder)
        {
            value = IsT1 ? T1Value : default!;
            remainder = IsT1
                ? default
                : Match<OneOf<T0, T2, T3, T4>>(t0 => t0, _ => throw new InvalidOperationException(), t2 => t2, t3 => t3, t4 => t4);
            return IsT1;
        }

        public bool TryPickT2(out T2 value, out OneOf<T0, T1, T3, T4> remainder)
        {
            value = IsT2 ? T2Value : default!;
            remainder = IsT2
                ? default
                : Match<OneOf<T0, T1, T3, T4>>(t0 => t0, t1 => t1, _ => throw new InvalidOperationException(), t3 => t3, t4 => t4);
            return IsT2;
        }

        public bool TryPickT3(out T3 value, out OneOf<T0, T1, T2, T4> remainder)
        {
            value = IsT3 ? T3Value : default!;
            remainder = IsT3
                ? default
                : Match<OneOf<T0, T1, T2, T4>>(t0 => t0, t1 => t1, t2 => t2, _ => throw new InvalidOperationException(), t4 => t4);
            return IsT3;
        }

        public bool TryPickT4(out T4 value, out OneOf<T0, T1, T2, T3> remainder)
        {
            value = IsT4 ? T4Value : default!;
            remainder = IsT4
                ? default
                : Match<OneOf<T0, T1, T2, T3>>(t0 => t0, t1 => t1, t2 => t2, t3 => t3, _ => throw new InvalidOperationException());
            return IsT4;
        }

        private bool Equals(in OneOf<T0, T1, T2, T3, T4> other)
        {
            if (_index != other._index)
            {
                return false;
            }
            switch (_index)
            {
                case 0: return Equals(_value0, other._value0);
                case 1: return Equals(_value1, other._value1);
                case 2: return Equals(_value2, other._value2);
                case 3: return Equals(_value3, other._value3);
                case 4: return Equals(_value4, other._value4);
                default: return false;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is OneOf<T0, T1, T2, T3, T4> oneOf && Equals(oneOf);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode;
                switch (_index)
                {
                    case 0:
                        hashCode = _value0?.GetHashCode() ?? 0;
                        break;
                    case 1:
                        hashCode = _value1?.GetHashCode() ?? 0;
                        break;
                    case 2:
                        hashCode = _value2?.GetHashCode() ?? 0;
                        break;
                    case 3:
                        hashCode = _value3?.GetHashCode() ?? 0;
                        break;
                    case 4:
                        hashCode = _value4?.GetHashCode() ?? 0;
                        break;
                    default:
                        hashCode = 0;
                        break;
                }
                return (hashCode * 397) ^ _index;
            }
        }

        public void Deconstruct(out int index, out T0 value0, out T1 value1, out T2 value2, out T3 value3, out T4 value4)
        {
            index = _index;
            value0 = _value0;
            value1 = _value1;
            value2 = _value2;
            value3 = _value3;
            value4 = _value4;
        }
    }

    public readonly struct OneOf<T0, T1, T2, T3, T4, T5> : IOneOf
    {
        private readonly T0 _value0;
        private readonly T1 _value1;
        private readonly T2 _value2;
        private readonly T3 _value3;
        private readonly T4 _value4;
        private readonly T5 _value5;
        private readonly int _index;

        private OneOf(int index, T0 value0 = default, T1 value1 = default, T2 value2 = default, T3 value3 = default, T4 value4 = default, T5 value5 = default)
        {
            _index = index;
            _value0 = value0;
            _value1 = value1;
            _value2 = value2;
            _value3 = value3;
            _value4 = value4;
            _value5 = value5;
        }

        public object? Value
        {
            get
            {
                switch (_index)
                {
                    case 0:
                        return _value0!;
                    case 1:
                        return _value1!;
                    case 2:
                        return _value2!;
                    case 3:
                        return _value3!;
                    case 4:
                        return _value4!;
                    case 5:
                        return _value5!;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        public bool IsT0 => _index == 0;

        public T0 T0Value
        {
            get
            {
                if (_index != 0)
                {
                    throw new InvalidOperationException($"Cannot return as T0 as result is T{_index}");
                }
                return _value0;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5>(T0 t) => new OneOf<T0, T1, T2, T3, T4, T5>(0, value0: t);

        public bool IsT1 => _index == 1;

        public T1 T1Value
        {
            get
            {
                if (_index != 1)
                {
                    throw new InvalidOperationException($"Cannot return as T1 as result is T{_index}");
                }
                return _value1;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5>(T1 t) => new OneOf<T0, T1, T2, T3, T4, T5>(1, value1: t);

        public bool IsT2 => _index == 2;

        public T2 T2Value
        {
            get
            {
                if (_index != 2)
                {
                    throw new InvalidOperationException($"Cannot return as T2 as result is T{_index}");
                }
                return _value2;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5>(T2 t) => new OneOf<T0, T1, T2, T3, T4, T5>(2, value2: t);

        public bool IsT3 => _index == 3;

        public T3 T3Value
        {
            get
            {
                if (_index != 3)
                {
                    throw new InvalidOperationException($"Cannot return as T3 as result is T{_index}");
                }
                return _value3;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5>(T3 t) => new OneOf<T0, T1, T2, T3, T4, T5>(3, value3: t);

        public bool IsT4 => _index == 4;

        public T4 T4Value
        {
            get
            {
                if (_index != 4)
                {
                    throw new InvalidOperationException($"Cannot return as T4 as result is T{_index}");
                }
                return _value4;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5>(T4 t) => new OneOf<T0, T1, T2, T3, T4, T5>(4, value4: t);

        public bool IsT5 => _index == 5;

        public T5 T5Value
        {
            get
            {
                if (_index != 5)
                {
                    throw new InvalidOperationException($"Cannot return as T5 as result is T{_index}");
                }
                return _value5;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5>(T5 t) => new OneOf<T0, T1, T2, T3, T4, T5>(5, value5: t);

        public void Switch(Action<T0> f0, Action<T1> f1, Action<T2> f2, Action<T3> f3, Action<T4> f4, Action<T5> f5)
        {
            if (_index == 0 && f0 != null)
            {
                f0(_value0);
                return;
            }
            if (_index == 1 && f1 != null)
            {
                f1(_value1);
                return;
            }
            if (_index == 2 && f2 != null)
            {
                f2(_value2);
                return;
            }
            if (_index == 3 && f3 != null)
            {
                f3(_value3);
                return;
            }
            if (_index == 4 && f4 != null)
            {
                f4(_value4);
                return;
            }
            if (_index == 5 && f5 != null)
            {
                f5(_value5);
                return;
            }
            throw new InvalidOperationException();
        }

        public TResult Match<TResult>(Func<T0, TResult> f0, Func<T1, TResult> f1, Func<T2, TResult> f2, Func<T3, TResult> f3, Func<T4, TResult> f4, Func<T5, TResult> f5)
        {
            if (_index == 0 && f0 != null)
            {
                return f0(_value0);
            }
            if (_index == 1 && f1 != null)
            {
                return f1(_value1);
            }
            if (_index == 2 && f2 != null)
            {
                return f2(_value2);
            }
            if (_index == 3 && f3 != null)
            {
                return f3(_value3);
            }
            if (_index == 4 && f4 != null)
            {
                return f4(_value4);
            }
            if (_index == 5 && f5 != null)
            {
                return f5(_value5);
            }
            throw new InvalidOperationException();
        }

        public static OneOf<T0, T1, T2, T3, T4, T5> FromT0(T0 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5> FromT1(T1 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5> FromT2(T2 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5> FromT3(T3 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5> FromT4(T4 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5> FromT5(T5 input)
        {
            return input;
        }

        public OneOf<TResult, T1, T2, T3, T4, T5> MapT0<TResult>(Func<T0, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<TResult, T1, T2, T3, T4, T5>>(
                input0 => mapFunc(input0),
                input1 => input1,
                input2 => input2,
                input3 => input3,
                input4 => input4,
                input5 => input5
            );
        }

        public OneOf<T0, TResult, T2, T3, T4, T5> MapT1<TResult>(Func<T1, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, TResult, T2, T3, T4, T5>>(
                input0 => input0,
                input1 => mapFunc(input1),
                input2 => input2,
                input3 => input3,
                input4 => input4,
                input5 => input5
            );
        }

        public OneOf<T0, T1, TResult, T3, T4, T5> MapT2<TResult>(Func<T2, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, TResult, T3, T4, T5>>(
                input0 => input0,
                input1 => input1,
                input2 => mapFunc(input2),
                input3 => input3,
                input4 => input4,
                input5 => input5
            );
        }

        public OneOf<T0, T1, T2, TResult, T4, T5> MapT3<TResult>(Func<T3, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, T2, TResult, T4, T5>>(
                input0 => input0,
                input1 => input1,
                input2 => input2,
                input3 => mapFunc(input3),
                input4 => input4,
                input5 => input5
            );
        }

        public OneOf<T0, T1, T2, T3, TResult, T5> MapT4<TResult>(Func<T4, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, T2, T3, TResult, T5>>(
                input0 => input0,
                input1 => input1,
                input2 => input2,
                input3 => input3,
                input4 => mapFunc(input4),
                input5 => input5
            );
        }

        public OneOf<T0, T1, T2, T3, T4, TResult> MapT5<TResult>(Func<T5, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, T2, T3, T4, TResult>>(
                input0 => input0,
                input1 => input1,
                input2 => input2,
                input3 => input3,
                input4 => input4,
                input5 => mapFunc(input5)
            );
        }

        public bool TryPickT0(out T0 value, out OneOf<T1, T2, T3, T4, T5> remainder)
        {
            value = IsT0 ? T0Value : default!;
            remainder = IsT0
                ? default
                : Match<OneOf<T1, T2, T3, T4, T5>>(_ => throw new InvalidOperationException(), t1 => t1, t2 => t2, t3 => t3, t4 => t4, t5 => t5);
            return IsT0;
        }

        public bool TryPickT1(out T1 value, out OneOf<T0, T2, T3, T4, T5> remainder)
        {
            value = IsT1 ? T1Value : default!;
            remainder = IsT1
                ? default
                : Match<OneOf<T0, T2, T3, T4, T5>>(t0 => t0, _ => throw new InvalidOperationException(), t2 => t2, t3 => t3, t4 => t4, t5 => t5);
            return IsT1;
        }

        public bool TryPickT2(out T2 value, out OneOf<T0, T1, T3, T4, T5> remainder)
        {
            value = IsT2 ? T2Value : default!;
            remainder = IsT2
                ? default
                : Match<OneOf<T0, T1, T3, T4, T5>>(t0 => t0, t1 => t1, _ => throw new InvalidOperationException(), t3 => t3, t4 => t4, t5 => t5);
            return IsT2;
        }

        public bool TryPickT3(out T3 value, out OneOf<T0, T1, T2, T4, T5> remainder)
        {
            value = IsT3 ? T3Value : default!;
            remainder = IsT3
                ? default
                : Match<OneOf<T0, T1, T2, T4, T5>>(t0 => t0, t1 => t1, t2 => t2, _ => throw new InvalidOperationException(), t4 => t4, t5 => t5);
            return IsT3;
        }

        public bool TryPickT4(out T4 value, out OneOf<T0, T1, T2, T3, T5> remainder)
        {
            value = IsT4 ? T4Value : default!;
            remainder = IsT4
                ? default
                : Match<OneOf<T0, T1, T2, T3, T5>>(t0 => t0, t1 => t1, t2 => t2, t3 => t3, _ => throw new InvalidOperationException(), t5 => t5);
            return IsT4;
        }

        public bool TryPickT5(out T5 value, out OneOf<T0, T1, T2, T3, T4> remainder)
        {
            value = IsT5 ? T5Value : default!;
            remainder = IsT5
                ? default
                : Match<OneOf<T0, T1, T2, T3, T4>>(t0 => t0, t1 => t1, t2 => t2, t3 => t3, t4 => t4, _ => throw new InvalidOperationException());
            return IsT5;
        }

        private bool Equals(in OneOf<T0, T1, T2, T3, T4, T5> other)
        {
            if (_index != other._index)
            {
                return false;
            }
            switch (_index)
            {
                case 0: return Equals(_value0, other._value0);
                case 1: return Equals(_value1, other._value1);
                case 2: return Equals(_value2, other._value2);
                case 3: return Equals(_value3, other._value3);
                case 4: return Equals(_value4, other._value4);
                case 5: return Equals(_value5, other._value5);
                default: return false;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is OneOf<T0, T1, T2, T3, T4, T5> oneOf && Equals(oneOf);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode;
                switch (_index)
                {
                    case 0:
                        hashCode = _value0?.GetHashCode() ?? 0;
                        break;
                    case 1:
                        hashCode = _value1?.GetHashCode() ?? 0;
                        break;
                    case 2:
                        hashCode = _value2?.GetHashCode() ?? 0;
                        break;
                    case 3:
                        hashCode = _value3?.GetHashCode() ?? 0;
                        break;
                    case 4:
                        hashCode = _value4?.GetHashCode() ?? 0;
                        break;
                    case 5:
                        hashCode = _value5?.GetHashCode() ?? 0;
                        break;
                    default:
                        hashCode = 0;
                        break;
                }
                return (hashCode * 397) ^ _index;
            }
        }
    }

    public readonly struct OneOf<T0, T1, T2, T3, T4, T5, T6> : IOneOf
    {
        private readonly T0 _value0;
        private readonly T1 _value1;
        private readonly T2 _value2;
        private readonly T3 _value3;
        private readonly T4 _value4;
        private readonly T5 _value5;
        private readonly T6 _value6;
        private readonly int _index;

        private OneOf(int index, T0 value0 = default, T1 value1 = default, T2 value2 = default, T3 value3 = default, T4 value4 = default, T5 value5 = default, T6 value6 = default)
        {
            _index = index;
            _value0 = value0;
            _value1 = value1;
            _value2 = value2;
            _value3 = value3;
            _value4 = value4;
            _value5 = value5;
            _value6 = value6;
        }

        public object? Value
        {
            get
            {
                switch (_index)
                {
                    case 0:
                        return _value0!;
                    case 1:
                        return _value1!;
                    case 2:
                        return _value2!;
                    case 3:
                        return _value3!;
                    case 4:
                        return _value4!;
                    case 5:
                        return _value5!;
                    case 6:
                        return _value6!;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        public bool IsT0 => _index == 0;

        public T0 T0Value
        {
            get
            {
                if (_index != 0)
                {
                    throw new InvalidOperationException($"Cannot return as T0 as result is T{_index}");
                }
                return _value0;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6>(T0 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6>(0, value0: t);

        public bool IsT1 => _index == 1;

        public T1 T1Value
        {
            get
            {
                if (_index != 1)
                {
                    throw new InvalidOperationException($"Cannot return as T1 as result is T{_index}");
                }
                return _value1;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6>(T1 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6>(1, value1: t);

        public bool IsT2 => _index == 2;

        public T2 T2Value
        {
            get
            {
                if (_index != 2)
                {
                    throw new InvalidOperationException($"Cannot return as T2 as result is T{_index}");
                }
                return _value2;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6>(T2 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6>(2, value2: t);

        public bool IsT3 => _index == 3;

        public T3 T3Value
        {
            get
            {
                if (_index != 3)
                {
                    throw new InvalidOperationException($"Cannot return as T3 as result is T{_index}");
                }
                return _value3;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6>(T3 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6>(3, value3: t);

        public bool IsT4 => _index == 4;

        public T4 T4Value
        {
            get
            {
                if (_index != 4)
                {
                    throw new InvalidOperationException($"Cannot return as T4 as result is T{_index}");
                }
                return _value4;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6>(T4 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6>(4, value4: t);

        public bool IsT5 => _index == 5;

        public T5 T5Value
        {
            get
            {
                if (_index != 5)
                {
                    throw new InvalidOperationException($"Cannot return as T5 as result is T{_index}");
                }
                return _value5;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6>(T5 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6>(5, value5: t);

        public bool IsT6 => _index == 6;

        public T6 T6Value
        {
            get
            {
                if (_index != 6)
                {
                    throw new InvalidOperationException($"Cannot return as T6 as result is T{_index}");
                }
                return _value6;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6>(T6 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6>(6, value6: t);

        public void Switch(Action<T0> f0, Action<T1> f1, Action<T2> f2, Action<T3> f3, Action<T4> f4, Action<T5> f5, Action<T6> f6)
        {
            if (_index == 0 && f0 != null)
            {
                f0(_value0);
                return;
            }
            if (_index == 1 && f1 != null)
            {
                f1(_value1);
                return;
            }
            if (_index == 2 && f2 != null)
            {
                f2(_value2);
                return;
            }
            if (_index == 3 && f3 != null)
            {
                f3(_value3);
                return;
            }
            if (_index == 4 && f4 != null)
            {
                f4(_value4);
                return;
            }
            if (_index == 5 && f5 != null)
            {
                f5(_value5);
                return;
            }
            if (_index == 6 && f6 != null)
            {
                f6(_value6);
                return;
            }
            throw new InvalidOperationException();
        }

        public TResult Match<TResult>(Func<T0, TResult> f0, Func<T1, TResult> f1, Func<T2, TResult> f2, Func<T3, TResult> f3, Func<T4, TResult> f4, Func<T5, TResult> f5, Func<T6, TResult> f6)
        {
            if (_index == 0 && f0 != null)
            {
                return f0(_value0);
            }
            if (_index == 1 && f1 != null)
            {
                return f1(_value1);
            }
            if (_index == 2 && f2 != null)
            {
                return f2(_value2);
            }
            if (_index == 3 && f3 != null)
            {
                return f3(_value3);
            }
            if (_index == 4 && f4 != null)
            {
                return f4(_value4);
            }
            if (_index == 5 && f5 != null)
            {
                return f5(_value5);
            }
            if (_index == 6 && f6 != null)
            {
                return f6(_value6);
            }
            throw new InvalidOperationException();
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6> FromT0(T0 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6> FromT1(T1 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6> FromT2(T2 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6> FromT3(T3 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6> FromT4(T4 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6> FromT5(T5 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6> FromT6(T6 input)
        {
            return input;
        }

        public OneOf<TResult, T1, T2, T3, T4, T5, T6> MapT0<TResult>(Func<T0, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<TResult, T1, T2, T3, T4, T5, T6>>(
                input0 => mapFunc(input0),
                input1 => input1,
                input2 => input2,
                input3 => input3,
                input4 => input4,
                input5 => input5,
                input6 => input6
            );
        }

        public OneOf<T0, TResult, T2, T3, T4, T5, T6> MapT1<TResult>(Func<T1, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, TResult, T2, T3, T4, T5, T6>>(
                input0 => input0,
                input1 => mapFunc(input1),
                input2 => input2,
                input3 => input3,
                input4 => input4,
                input5 => input5,
                input6 => input6
            );
        }

        public OneOf<T0, T1, TResult, T3, T4, T5, T6> MapT2<TResult>(Func<T2, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, TResult, T3, T4, T5, T6>>(
                input0 => input0,
                input1 => input1,
                input2 => mapFunc(input2),
                input3 => input3,
                input4 => input4,
                input5 => input5,
                input6 => input6
            );
        }

        public OneOf<T0, T1, T2, TResult, T4, T5, T6> MapT3<TResult>(Func<T3, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, T2, TResult, T4, T5, T6>>(
                input0 => input0,
                input1 => input1,
                input2 => input2,
                input3 => mapFunc(input3),
                input4 => input4,
                input5 => input5,
                input6 => input6
            );
        }

        public OneOf<T0, T1, T2, T3, TResult, T5, T6> MapT4<TResult>(Func<T4, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, T2, T3, TResult, T5, T6>>(
                input0 => input0,
                input1 => input1,
                input2 => input2,
                input3 => input3,
                input4 => mapFunc(input4),
                input5 => input5,
                input6 => input6
            );
        }

        public OneOf<T0, T1, T2, T3, T4, TResult, T6> MapT5<TResult>(Func<T5, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, T2, T3, T4, TResult, T6>>(
                input0 => input0,
                input1 => input1,
                input2 => input2,
                input3 => input3,
                input4 => input4,
                input5 => mapFunc(input5),
                input6 => input6
            );
        }

        public OneOf<T0, T1, T2, T3, T4, T5, TResult> MapT6<TResult>(Func<T6, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, T2, T3, T4, T5, TResult>>(
                input0 => input0,
                input1 => input1,
                input2 => input2,
                input3 => input3,
                input4 => input4,
                input5 => input5,
                input6 => mapFunc(input6)
            );
        }

        public bool TryPickT0(out T0 value, out OneOf<T1, T2, T3, T4, T5, T6> remainder)
        {
            value = IsT0 ? T0Value : default!;
            remainder = IsT0
                ? default
                : Match<OneOf<T1, T2, T3, T4, T5, T6>>(_ => throw new InvalidOperationException(), t1 => t1, t2 => t2, t3 => t3, t4 => t4, t5 => t5, t6 => t6);
            return IsT0;
        }

        public bool TryPickT1(out T1 value, out OneOf<T0, T2, T3, T4, T5, T6> remainder)
        {
            value = IsT1 ? T1Value : default!;
            remainder = IsT1
                ? default
                : Match<OneOf<T0, T2, T3, T4, T5, T6>>(t0 => t0, _ => throw new InvalidOperationException(), t2 => t2, t3 => t3, t4 => t4, t5 => t5, t6 => t6);
            return IsT1;
        }

        public bool TryPickT2(out T2 value, out OneOf<T0, T1, T3, T4, T5, T6> remainder)
        {
            value = IsT2 ? T2Value : default!;
            remainder = IsT2
                ? default
                : Match<OneOf<T0, T1, T3, T4, T5, T6>>(t0 => t0, t1 => t1, _ => throw new InvalidOperationException(), t3 => t3, t4 => t4, t5 => t5, t6 => t6);
            return IsT2;
        }

        public bool TryPickT3(out T3 value, out OneOf<T0, T1, T2, T4, T5, T6> remainder)
        {
            value = IsT3 ? T3Value : default!;
            remainder = IsT3
                ? default
                : Match<OneOf<T0, T1, T2, T4, T5, T6>>(t0 => t0, t1 => t1, t2 => t2, _ => throw new InvalidOperationException(), t4 => t4, t5 => t5, t6 => t6);
            return IsT3;
        }

        public bool TryPickT4(out T4 value, out OneOf<T0, T1, T2, T3, T5, T6> remainder)
        {
            value = IsT4 ? T4Value : default!;
            remainder = IsT4
                ? default
                : Match<OneOf<T0, T1, T2, T3, T5, T6>>(t0 => t0, t1 => t1, t2 => t2, t3 => t3, _ => throw new InvalidOperationException(), t5 => t5, t6 => t6);
            return IsT4;
        }

        public bool TryPickT5(out T5 value, out OneOf<T0, T1, T2, T3, T4, T6> remainder)
        {
            value = IsT5 ? T5Value : default!;
            remainder = IsT5
                ? default
                : Match<OneOf<T0, T1, T2, T3, T4, T6>>(t0 => t0, t1 => t1, t2 => t2, t3 => t3, t4 => t4, _ => throw new InvalidOperationException(), t6 => t6);
            return IsT5;
        }

        public bool TryPickT6(out T6 value, out OneOf<T0, T1, T2, T3, T4, T5> remainder)
        {
            value = IsT6 ? T6Value : default!;
            remainder = IsT6
                ? default
                : Match<OneOf<T0, T1, T2, T3, T4, T5>>(t0 => t0, t1 => t1, t2 => t2, t3 => t3, t4 => t4, t5 => t5, _ => throw new InvalidOperationException());
            return IsT6;
        }

        private bool Equals(in OneOf<T0, T1, T2, T3, T4, T5, T6> other)
        {
            if (_index != other._index)
            {
                return false;
            }
            switch (_index)
            {
                case 0: return Equals(_value0, other._value0);
                case 1: return Equals(_value1, other._value1);
                case 2: return Equals(_value2, other._value2);
                case 3: return Equals(_value3, other._value3);
                case 4: return Equals(_value4, other._value4);
                case 5: return Equals(_value5, other._value5);
                case 6: return Equals(_value6, other._value6);
                default: return false;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is OneOf<T0, T1, T2, T3, T4, T5, T6> oneOf && Equals(oneOf);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode;
                switch (_index)
                {
                    case 0:
                        hashCode = _value0?.GetHashCode() ?? 0;
                        break;
                    case 1:
                        hashCode = _value1?.GetHashCode() ?? 0;
                        break;
                    case 2:
                        hashCode = _value2?.GetHashCode() ?? 0;
                        break;
                    case 3:
                        hashCode = _value3?.GetHashCode() ?? 0;
                        break;
                    case 4:
                        hashCode = _value4?.GetHashCode() ?? 0;
                        break;
                    case 5:
                        hashCode = _value5?.GetHashCode() ?? 0;
                        break;
                    case 6:
                        hashCode = _value6?.GetHashCode() ?? 0;
                        break;
                    default:
                        hashCode = 0;
                        break;
                }
                return (hashCode * 397) ^ _index;
            }
        }
    }

    public readonly struct OneOf<T0, T1, T2, T3, T4, T5, T6, T7> : IOneOf
    {
        private readonly T0 _value0;
        private readonly T1 _value1;
        private readonly T2 _value2;
        private readonly T3 _value3;
        private readonly T4 _value4;
        private readonly T5 _value5;
        private readonly T6 _value6;
        private readonly T7 _value7;
        private readonly int _index;

        private OneOf(int index, T0 value0 = default, T1 value1 = default, T2 value2 = default, T3 value3 = default, T4 value4 = default, T5 value5 = default, T6 value6 = default, T7 value7 = default)
        {
            _index = index;
            _value0 = value0;
            _value1 = value1;
            _value2 = value2;
            _value3 = value3;
            _value4 = value4;
            _value5 = value5;
            _value6 = value6;
            _value7 = value7;
        }

        public object? Value
        {
            get
            {
                switch (_index)
                {
                    case 0:
                        return _value0!;
                    case 1:
                        return _value1!;
                    case 2:
                        return _value2!;
                    case 3:
                        return _value3!;
                    case 4:
                        return _value4!;
                    case 5:
                        return _value5!;
                    case 6:
                        return _value6!;
                    case 7:
                        return _value7!;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        public bool IsT0 => _index == 0;

        public T0 T0Value
        {
            get
            {
                if (_index != 0)
                {
                    throw new InvalidOperationException($"Cannot return as T0 as result is T{_index}");
                }
                return _value0;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6, T7>(0, value0: t);

        public bool IsT1 => _index == 1;

        public T1 T1Value
        {
            get
            {
                if (_index != 1)
                {
                    throw new InvalidOperationException($"Cannot return as T1 as result is T{_index}");
                }
                return _value1;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6, T7>(T1 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6, T7>(1, value1: t);

        public bool IsT2 => _index == 2;

        public T2 T2Value
        {
            get
            {
                if (_index != 2)
                {
                    throw new InvalidOperationException($"Cannot return as T2 as result is T{_index}");
                }
                return _value2;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6, T7>(T2 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6, T7>(2, value2: t);

        public bool IsT3 => _index == 3;

        public T3 T3Value
        {
            get
            {
                if (_index != 3)
                {
                    throw new InvalidOperationException($"Cannot return as T3 as result is T{_index}");
                }
                return _value3;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6, T7>(T3 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6, T7>(3, value3: t);

        public bool IsT4 => _index == 4;

        public T4 T4Value
        {
            get
            {
                if (_index != 4)
                {
                    throw new InvalidOperationException($"Cannot return as T4 as result is T{_index}");
                }
                return _value4;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6, T7>(T4 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6, T7>(4, value4: t);

        public bool IsT5 => _index == 5;

        public T5 T5Value
        {
            get
            {
                if (_index != 5)
                {
                    throw new InvalidOperationException($"Cannot return as T5 as result is T{_index}");
                }
                return _value5;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6, T7>(T5 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6, T7>(5, value5: t);

        public bool IsT6 => _index == 6;

        public T6 T6Value
        {
            get
            {
                if (_index != 6)
                {
                    throw new InvalidOperationException($"Cannot return as T6 as result is T{_index}");
                }
                return _value6;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6, T7>(T6 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6, T7>(6, value6: t);

        public bool IsT7 => _index == 7;

        public T7 T7Value
        {
            get
            {
                if (_index != 7)
                {
                    throw new InvalidOperationException($"Cannot return as T7 as result is T{_index}");
                }
                return _value7;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6, T7>(T7 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6, T7>(7, value7: t);

        public void Switch(Action<T0> f0, Action<T1> f1, Action<T2> f2, Action<T3> f3, Action<T4> f4, Action<T5> f5, Action<T6> f6, Action<T7> f7)
        {
            if (_index == 0 && f0 != null)
            {
                f0(_value0);
                return;
            }
            if (_index == 1 && f1 != null)
            {
                f1(_value1);
                return;
            }
            if (_index == 2 && f2 != null)
            {
                f2(_value2);
                return;
            }
            if (_index == 3 && f3 != null)
            {
                f3(_value3);
                return;
            }
            if (_index == 4 && f4 != null)
            {
                f4(_value4);
                return;
            }
            if (_index == 5 && f5 != null)
            {
                f5(_value5);
                return;
            }
            if (_index == 6 && f6 != null)
            {
                f6(_value6);
                return;
            }
            if (_index == 7 && f7 != null)
            {
                f7(_value7);
                return;
            }
            throw new InvalidOperationException();
        }

        public TResult Match<TResult>(Func<T0, TResult> f0, Func<T1, TResult> f1, Func<T2, TResult> f2, Func<T3, TResult> f3, Func<T4, TResult> f4, Func<T5, TResult> f5, Func<T6, TResult> f6, Func<T7, TResult> f7)
        {
            if (_index == 0 && f0 != null)
            {
                return f0(_value0);
            }
            if (_index == 1 && f1 != null)
            {
                return f1(_value1);
            }
            if (_index == 2 && f2 != null)
            {
                return f2(_value2);
            }
            if (_index == 3 && f3 != null)
            {
                return f3(_value3);
            }
            if (_index == 4 && f4 != null)
            {
                return f4(_value4);
            }
            if (_index == 5 && f5 != null)
            {
                return f5(_value5);
            }
            if (_index == 6 && f6 != null)
            {
                return f6(_value6);
            }
            if (_index == 7 && f7 != null)
            {
                return f7(_value7);
            }
            throw new InvalidOperationException();
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6, T7> FromT0(T0 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6, T7> FromT1(T1 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6, T7> FromT2(T2 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6, T7> FromT3(T3 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6, T7> FromT4(T4 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6, T7> FromT5(T5 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6, T7> FromT6(T6 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6, T7> FromT7(T7 input)
        {
            return input;
        }

        public OneOf<TResult, T1, T2, T3, T4, T5, T6, T7> MapT0<TResult>(Func<T0, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<TResult, T1, T2, T3, T4, T5, T6, T7>>(
                input0 => mapFunc(input0),
                input1 => input1,
                input2 => input2,
                input3 => input3,
                input4 => input4,
                input5 => input5,
                input6 => input6,
                input7 => input7
            );
        }

        public OneOf<T0, TResult, T2, T3, T4, T5, T6, T7> MapT1<TResult>(Func<T1, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, TResult, T2, T3, T4, T5, T6, T7>>(
                input0 => input0,
                input1 => mapFunc(input1),
                input2 => input2,
                input3 => input3,
                input4 => input4,
                input5 => input5,
                input6 => input6,
                input7 => input7
            );
        }

        public OneOf<T0, T1, TResult, T3, T4, T5, T6, T7> MapT2<TResult>(Func<T2, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, TResult, T3, T4, T5, T6, T7>>(
                input0 => input0,
                input1 => input1,
                input2 => mapFunc(input2),
                input3 => input3,
                input4 => input4,
                input5 => input5,
                input6 => input6,
                input7 => input7
            );
        }

        public OneOf<T0, T1, T2, TResult, T4, T5, T6, T7> MapT3<TResult>(Func<T3, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, T2, TResult, T4, T5, T6, T7>>(
                input0 => input0,
                input1 => input1,
                input2 => input2,
                input3 => mapFunc(input3),
                input4 => input4,
                input5 => input5,
                input6 => input6,
                input7 => input7
            );
        }

        public OneOf<T0, T1, T2, T3, TResult, T5, T6, T7> MapT4<TResult>(Func<T4, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, T2, T3, TResult, T5, T6, T7>>(
                input0 => input0,
                input1 => input1,
                input2 => input2,
                input3 => input3,
                input4 => mapFunc(input4),
                input5 => input5,
                input6 => input6,
                input7 => input7
            );
        }

        public OneOf<T0, T1, T2, T3, T4, TResult, T6, T7> MapT5<TResult>(Func<T5, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, T2, T3, T4, TResult, T6, T7>>(
                input0 => input0,
                input1 => input1,
                input2 => input2,
                input3 => input3,
                input4 => input4,
                input5 => mapFunc(input5),
                input6 => input6,
                input7 => input7
            );
        }

        public OneOf<T0, T1, T2, T3, T4, T5, TResult, T7> MapT6<TResult>(Func<T6, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, T2, T3, T4, T5, TResult, T7>>(
                input0 => input0,
                input1 => input1,
                input2 => input2,
                input3 => input3,
                input4 => input4,
                input5 => input5,
                input6 => mapFunc(input6),
                input7 => input7
            );
        }

        public OneOf<T0, T1, T2, T3, T4, T5, T6, TResult> MapT7<TResult>(Func<T7, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, T2, T3, T4, T5, T6, TResult>>(
                input0 => input0,
                input1 => input1,
                input2 => input2,
                input3 => input3,
                input4 => input4,
                input5 => input5,
                input6 => input6,
                input7 => mapFunc(input7)
            );
        }

        public bool TryPickT0(out T0 value, out OneOf<T1, T2, T3, T4, T5, T6, T7> remainder)
        {
            value = IsT0 ? T0Value : default!;
            remainder = IsT0
                ? default
                : Match<OneOf<T1, T2, T3, T4, T5, T6, T7>>(_ => throw new InvalidOperationException(), t1 => t1, t2 => t2, t3 => t3, t4 => t4, t5 => t5, t6 => t6, t7 => t7);
            return IsT0;
        }

        public bool TryPickT1(out T1 value, out OneOf<T0, T2, T3, T4, T5, T6, T7> remainder)
        {
            value = IsT1 ? T1Value : default!;
            remainder = IsT1
                ? default
                : Match<OneOf<T0, T2, T3, T4, T5, T6, T7>>(t0 => t0, _ => throw new InvalidOperationException(), t2 => t2, t3 => t3, t4 => t4, t5 => t5, t6 => t6, t7 => t7);
            return IsT1;
        }

        public bool TryPickT2(out T2 value, out OneOf<T0, T1, T3, T4, T5, T6, T7> remainder)
        {
            value = IsT2 ? T2Value : default!;
            remainder = IsT2
                ? default
                : Match<OneOf<T0, T1, T3, T4, T5, T6, T7>>(t0 => t0, t1 => t1, _ => throw new InvalidOperationException(), t3 => t3, t4 => t4, t5 => t5, t6 => t6, t7 => t7);
            return IsT2;
        }

        public bool TryPickT3(out T3 value, out OneOf<T0, T1, T2, T4, T5, T6, T7> remainder)
        {
            value = IsT3 ? T3Value : default!;
            remainder = IsT3
                ? default
                : Match<OneOf<T0, T1, T2, T4, T5, T6, T7>>(t0 => t0, t1 => t1, t2 => t2, _ => throw new InvalidOperationException(), t4 => t4, t5 => t5, t6 => t6, t7 => t7);
            return IsT3;
        }

        public bool TryPickT4(out T4 value, out OneOf<T0, T1, T2, T3, T5, T6, T7> remainder)
        {
            value = IsT4 ? T4Value : default!;
            remainder = IsT4
                ? default
                : Match<OneOf<T0, T1, T2, T3, T5, T6, T7>>(t0 => t0, t1 => t1, t2 => t2, t3 => t3, _ => throw new InvalidOperationException(), t5 => t5, t6 => t6, t7 => t7);
            return IsT4;
        }

        public bool TryPickT5(out T5 value, out OneOf<T0, T1, T2, T3, T4, T6, T7> remainder)
        {
            value = IsT5 ? T5Value : default!;
            remainder = IsT5
                ? default
                : Match<OneOf<T0, T1, T2, T3, T4, T6, T7>>(t0 => t0, t1 => t1, t2 => t2, t3 => t3, t4 => t4, _ => throw new InvalidOperationException(), t6 => t6, t7 => t7);
            return IsT5;
        }

        public bool TryPickT6(out T6 value, out OneOf<T0, T1, T2, T3, T4, T5, T7> remainder)
        {
            value = IsT6 ? T6Value : default!;
            remainder = IsT6
                ? default
                : Match<OneOf<T0, T1, T2, T3, T4, T5, T7>>(t0 => t0, t1 => t1, t2 => t2, t3 => t3, t4 => t4, t5 => t5, _ => throw new InvalidOperationException(), t7 => t7);
            return IsT6;
        }

        public bool TryPickT7(out T7 value, out OneOf<T0, T1, T2, T3, T4, T5, T6> remainder)
        {
            value = IsT7 ? T7Value : default!;
            remainder = IsT7
                ? default
                : Match<OneOf<T0, T1, T2, T3, T4, T5, T6>>(t0 => t0, t1 => t1, t2 => t2, t3 => t3, t4 => t4, t5 => t5, t6 => t6, _ => throw new InvalidOperationException());
            return IsT7;
        }

        private bool Equals(in OneOf<T0, T1, T2, T3, T4, T5, T6, T7> other)
        {
            if (_index != other._index)
            {
                return false;
            }
            switch (_index)
            {
                case 0: return Equals(_value0, other._value0);
                case 1: return Equals(_value1, other._value1);
                case 2: return Equals(_value2, other._value2);
                case 3: return Equals(_value3, other._value3);
                case 4: return Equals(_value4, other._value4);
                case 5: return Equals(_value5, other._value5);
                case 6: return Equals(_value6, other._value6);
                case 7: return Equals(_value7, other._value7);
                default: return false;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is OneOf<T0, T1, T2, T3, T4, T5, T6, T7> oneOf && Equals(oneOf);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode;
                switch (_index)
                {
                    case 0:
                        hashCode = _value0?.GetHashCode() ?? 0;
                        break;
                    case 1:
                        hashCode = _value1?.GetHashCode() ?? 0;
                        break;
                    case 2:
                        hashCode = _value2?.GetHashCode() ?? 0;
                        break;
                    case 3:
                        hashCode = _value3?.GetHashCode() ?? 0;
                        break;
                    case 4:
                        hashCode = _value4?.GetHashCode() ?? 0;
                        break;
                    case 5:
                        hashCode = _value5?.GetHashCode() ?? 0;
                        break;
                    case 6:
                        hashCode = _value6?.GetHashCode() ?? 0;
                        break;
                    case 7:
                        hashCode = _value7?.GetHashCode() ?? 0;
                        break;
                    default:
                        hashCode = 0;
                        break;
                }
                return (hashCode * 397) ^ _index;
            }
        }
    }

    public readonly struct OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8> : IOneOf
    {
        private readonly T0 _value0;
        private readonly T1 _value1;
        private readonly T2 _value2;
        private readonly T3 _value3;
        private readonly T4 _value4;
        private readonly T5 _value5;
        private readonly T6 _value6;
        private readonly T7 _value7;
        private readonly T8 _value8;
        private readonly int _index;

        private OneOf(int index, T0 value0 = default, T1 value1 = default, T2 value2 = default, T3 value3 = default, T4 value4 = default, T5 value5 = default, T6 value6 = default, T7 value7 = default, T8 value8 = default)
        {
            _index = index;
            _value0 = value0;
            _value1 = value1;
            _value2 = value2;
            _value3 = value3;
            _value4 = value4;
            _value5 = value5;
            _value6 = value6;
            _value7 = value7;
            _value8 = value8;
        }

        public object? Value
        {
            get
            {
                switch (_index)
                {
                    case 0:
                        return _value0!;
                    case 1:
                        return _value1!;
                    case 2:
                        return _value2!;
                    case 3:
                        return _value3!;
                    case 4:
                        return _value4!;
                    case 5:
                        return _value5!;
                    case 6:
                        return _value6!;
                    case 7:
                        return _value7!;
                    case 8:
                        return _value8!;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        public bool IsT0 => _index == 0;

        public T0 T0Value
        {
            get
            {
                if (_index != 0)
                {
                    throw new InvalidOperationException($"Cannot return as T0 as result is T{_index}");
                }
                return _value0;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8>(T0 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8>(0, value0: t);

        public bool IsT1 => _index == 1;

        public T1 T1Value
        {
            get
            {
                if (_index != 1)
                {
                    throw new InvalidOperationException($"Cannot return as T1 as result is T{_index}");
                }
                return _value1;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8>(T1 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8>(1, value1: t);

        public bool IsT2 => _index == 2;

        public T2 T2Value
        {
            get
            {
                if (_index != 2)
                {
                    throw new InvalidOperationException($"Cannot return as T2 as result is T{_index}");
                }
                return _value2;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8>(T2 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8>(2, value2: t);

        public bool IsT3 => _index == 3;

        public T3 T3Value
        {
            get
            {
                if (_index != 3)
                {
                    throw new InvalidOperationException($"Cannot return as T3 as result is T{_index}");
                }
                return _value3;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8>(T3 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8>(3, value3: t);

        public bool IsT4 => _index == 4;

        public T4 T4Value
        {
            get
            {
                if (_index != 4)
                {
                    throw new InvalidOperationException($"Cannot return as T4 as result is T{_index}");
                }
                return _value4;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8>(T4 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8>(4, value4: t);

        public bool IsT5 => _index == 5;

        public T5 T5Value
        {
            get
            {
                if (_index != 5)
                {
                    throw new InvalidOperationException($"Cannot return as T5 as result is T{_index}");
                }
                return _value5;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8>(T5 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8>(5, value5: t);

        public bool IsT6 => _index == 6;

        public T6 T6Value
        {
            get
            {
                if (_index != 6)
                {
                    throw new InvalidOperationException($"Cannot return as T6 as result is T{_index}");
                }
                return _value6;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8>(T6 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8>(6, value6: t);

        public bool IsT7 => _index == 7;

        public T7 T7Value
        {
            get
            {
                if (_index != 7)
                {
                    throw new InvalidOperationException($"Cannot return as T7 as result is T{_index}");
                }
                return _value7;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8>(T7 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8>(7, value7: t);

        public bool IsT8 => _index == 8;

        public T8 T8Value
        {
            get
            {
                if (_index != 8)
                {
                    throw new InvalidOperationException($"Cannot return as T8 as result is T{_index}");
                }
                return _value8;
            }
        }

        public static implicit operator OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8>(T8 t) => new OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8>(8, value8: t);

        public void Switch(Action<T0> f0, Action<T1> f1, Action<T2> f2, Action<T3> f3, Action<T4> f4, Action<T5> f5, Action<T6> f6, Action<T7> f7, Action<T8> f8)
        {
            if (_index == 0 && f0 != null)
            {
                f0(_value0);
                return;
            }
            if (_index == 1 && f1 != null)
            {
                f1(_value1);
                return;
            }
            if (_index == 2 && f2 != null)
            {
                f2(_value2);
                return;
            }
            if (_index == 3 && f3 != null)
            {
                f3(_value3);
                return;
            }
            if (_index == 4 && f4 != null)
            {
                f4(_value4);
                return;
            }
            if (_index == 5 && f5 != null)
            {
                f5(_value5);
                return;
            }
            if (_index == 6 && f6 != null)
            {
                f6(_value6);
                return;
            }
            if (_index == 7 && f7 != null)
            {
                f7(_value7);
                return;
            }
            if (_index == 8 && f8 != null)
            {
                f8(_value8);
                return;
            }
            throw new InvalidOperationException();
        }

        public TResult Match<TResult>(Func<T0, TResult> f0, Func<T1, TResult> f1, Func<T2, TResult> f2, Func<T3, TResult> f3, Func<T4, TResult> f4, Func<T5, TResult> f5, Func<T6, TResult> f6, Func<T7, TResult> f7, Func<T8, TResult> f8)
        {
            if (_index == 0 && f0 != null)
            {
                return f0(_value0);
            }
            if (_index == 1 && f1 != null)
            {
                return f1(_value1);
            }
            if (_index == 2 && f2 != null)
            {
                return f2(_value2);
            }
            if (_index == 3 && f3 != null)
            {
                return f3(_value3);
            }
            if (_index == 4 && f4 != null)
            {
                return f4(_value4);
            }
            if (_index == 5 && f5 != null)
            {
                return f5(_value5);
            }
            if (_index == 6 && f6 != null)
            {
                return f6(_value6);
            }
            if (_index == 7 && f7 != null)
            {
                return f7(_value7);
            }
            if (_index == 8 && f8 != null)
            {
                return f8(_value8);
            }
            throw new InvalidOperationException();
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8> FromT0(T0 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8> FromT1(T1 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8> FromT2(T2 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8> FromT3(T3 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8> FromT4(T4 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8> FromT5(T5 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8> FromT6(T6 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8> FromT7(T7 input)
        {
            return input;
        }

        public static OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8> FromT8(T8 input)
        {
            return input;
        }

        public OneOf<TResult, T1, T2, T3, T4, T5, T6, T7, T8> MapT0<TResult>(Func<T0, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<TResult, T1, T2, T3, T4, T5, T6, T7, T8>>(
                input0 => mapFunc(input0),
                input1 => input1,
                input2 => input2,
                input3 => input3,
                input4 => input4,
                input5 => input5,
                input6 => input6,
                input7 => input7,
                input8 => input8
            );
        }

        public OneOf<T0, TResult, T2, T3, T4, T5, T6, T7, T8> MapT1<TResult>(Func<T1, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, TResult, T2, T3, T4, T5, T6, T7, T8>>(
                input0 => input0,
                input1 => mapFunc(input1),
                input2 => input2,
                input3 => input3,
                input4 => input4,
                input5 => input5,
                input6 => input6,
                input7 => input7,
                input8 => input8
            );
        }

        public OneOf<T0, T1, TResult, T3, T4, T5, T6, T7, T8> MapT2<TResult>(Func<T2, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, TResult, T3, T4, T5, T6, T7, T8>>(
                input0 => input0,
                input1 => input1,
                input2 => mapFunc(input2),
                input3 => input3,
                input4 => input4,
                input5 => input5,
                input6 => input6,
                input7 => input7,
                input8 => input8
            );
        }

        public OneOf<T0, T1, T2, TResult, T4, T5, T6, T7, T8> MapT3<TResult>(Func<T3, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, T2, TResult, T4, T5, T6, T7, T8>>(
                input0 => input0,
                input1 => input1,
                input2 => input2,
                input3 => mapFunc(input3),
                input4 => input4,
                input5 => input5,
                input6 => input6,
                input7 => input7,
                input8 => input8
            );
        }

        public OneOf<T0, T1, T2, T3, TResult, T5, T6, T7, T8> MapT4<TResult>(Func<T4, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, T2, T3, TResult, T5, T6, T7, T8>>(
                input0 => input0,
                input1 => input1,
                input2 => input2,
                input3 => input3,
                input4 => mapFunc(input4),
                input5 => input5,
                input6 => input6,
                input7 => input7,
                input8 => input8
            );
        }

        public OneOf<T0, T1, T2, T3, T4, TResult, T6, T7, T8> MapT5<TResult>(Func<T5, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, T2, T3, T4, TResult, T6, T7, T8>>(
                input0 => input0,
                input1 => input1,
                input2 => input2,
                input3 => input3,
                input4 => input4,
                input5 => mapFunc(input5),
                input6 => input6,
                input7 => input7,
                input8 => input8
            );
        }

        public OneOf<T0, T1, T2, T3, T4, T5, TResult, T7, T8> MapT6<TResult>(Func<T6, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, T2, T3, T4, T5, TResult, T7, T8>>(
                input0 => input0,
                input1 => input1,
                input2 => input2,
                input3 => input3,
                input4 => input4,
                input5 => input5,
                input6 => mapFunc(input6),
                input7 => input7,
                input8 => input8
            );
        }

        public OneOf<T0, T1, T2, T3, T4, T5, T6, TResult, T8> MapT7<TResult>(Func<T7, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, T2, T3, T4, T5, T6, TResult, T8>>(
                input0 => input0,
                input1 => input1,
                input2 => input2,
                input3 => input3,
                input4 => input4,
                input5 => input5,
                input6 => input6,
                input7 => mapFunc(input7),
                input8 => input8
            );
        }

        public OneOf<T0, T1, T2, T3, T4, T5, T6, T7, TResult> MapT8<TResult>(Func<T8, TResult> mapFunc)
        {
            if (mapFunc == null)
            {
                throw new ArgumentNullException(nameof(mapFunc));
            }
            return Match<OneOf<T0, T1, T2, T3, T4, T5, T6, T7, TResult>>(
                input0 => input0,
                input1 => input1,
                input2 => input2,
                input3 => input3,
                input4 => input4,
                input5 => input5,
                input6 => input6,
                input7 => input7,
                input8 => mapFunc(input8)
            );
        }

        public bool TryPickT0(out T0 value, out OneOf<T1, T2, T3, T4, T5, T6, T7, T8> remainder)
        {
            value = IsT0 ? T0Value : default!;
            remainder = IsT0
                ? default
                : Match<OneOf<T1, T2, T3, T4, T5, T6, T7, T8>>(_ => throw new InvalidOperationException(), t1 => t1, t2 => t2, t3 => t3, t4 => t4, t5 => t5, t6 => t6, t7 => t7, t8 => t8);
            return IsT0;
        }

        public bool TryPickT1(out T1 value, out OneOf<T0, T2, T3, T4, T5, T6, T7, T8> remainder)
        {
            value = IsT1 ? T1Value : default!;
            remainder = IsT1
                ? default
                : Match<OneOf<T0, T2, T3, T4, T5, T6, T7, T8>>(t0 => t0, _ => throw new InvalidOperationException(), t2 => t2, t3 => t3, t4 => t4, t5 => t5, t6 => t6, t7 => t7, t8 => t8);
            return IsT1;
        }

        public bool TryPickT2(out T2 value, out OneOf<T0, T1, T3, T4, T5, T6, T7, T8> remainder)
        {
            value = IsT2 ? T2Value : default!;
            remainder = IsT2
                ? default
                : Match<OneOf<T0, T1, T3, T4, T5, T6, T7, T8>>(t0 => t0, t1 => t1, _ => throw new InvalidOperationException(), t3 => t3, t4 => t4, t5 => t5, t6 => t6, t7 => t7, t8 => t8);
            return IsT2;
        }

        public bool TryPickT3(out T3 value, out OneOf<T0, T1, T2, T4, T5, T6, T7, T8> remainder)
        {
            value = IsT3 ? T3Value : default!;
            remainder = IsT3
                ? default
                : Match<OneOf<T0, T1, T2, T4, T5, T6, T7, T8>>(t0 => t0, t1 => t1, t2 => t2, _ => throw new InvalidOperationException(), t4 => t4, t5 => t5, t6 => t6, t7 => t7, t8 => t8);
            return IsT3;
        }

        public bool TryPickT4(out T4 value, out OneOf<T0, T1, T2, T3, T5, T6, T7, T8> remainder)
        {
            value = IsT4 ? T4Value : default!;
            remainder = IsT4
                ? default
                : Match<OneOf<T0, T1, T2, T3, T5, T6, T7, T8>>(t0 => t0, t1 => t1, t2 => t2, t3 => t3, _ => throw new InvalidOperationException(), t5 => t5, t6 => t6, t7 => t7, t8 => t8);
            return IsT4;
        }

        public bool TryPickT5(out T5 value, out OneOf<T0, T1, T2, T3, T4, T6, T7, T8> remainder)
        {
            value = IsT5 ? T5Value : default!;
            remainder = IsT5
                ? default
                : Match<OneOf<T0, T1, T2, T3, T4, T6, T7, T8>>(t0 => t0, t1 => t1, t2 => t2, t3 => t3, t4 => t4, _ => throw new InvalidOperationException(), t6 => t6, t7 => t7, t8 => t8);
            return IsT5;
        }

        public bool TryPickT6(out T6 value, out OneOf<T0, T1, T2, T3, T4, T5, T7, T8> remainder)
        {
            value = IsT6 ? T6Value : default!;
            remainder = IsT6
                ? default
                : Match<OneOf<T0, T1, T2, T3, T4, T5, T7, T8>>(t0 => t0, t1 => t1, t2 => t2, t3 => t3, t4 => t4, t5 => t5, _ => throw new InvalidOperationException(), t7 => t7, t8 => t8);
            return IsT6;
        }

        public bool TryPickT7(out T7 value, out OneOf<T0, T1, T2, T3, T4, T5, T6, T8> remainder)
        {
            value = IsT7 ? T7Value : default!;
            remainder = IsT7
                ? default
                : Match<OneOf<T0, T1, T2, T3, T4, T5, T6, T8>>(t0 => t0, t1 => t1, t2 => t2, t3 => t3, t4 => t4, t5 => t5, t6 => t6, _ => throw new InvalidOperationException(), t8 => t8);
            return IsT7;
        }

        public bool TryPickT8(out T8 value, out OneOf<T0, T1, T2, T3, T4, T5, T6, T7> remainder)
        {
            value = IsT8 ? T8Value : default!;
            remainder = IsT8
                ? default
                : Match<OneOf<T0, T1, T2, T3, T4, T5, T6, T7>>(t0 => t0, t1 => t1, t2 => t2, t3 => t3, t4 => t4, t5 => t5, t6 => t6, t7 => t7, _ => throw new InvalidOperationException());
            return IsT8;
        }

        private bool Equals(in OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8> other)
        {
            if (_index != other._index)
            {
                return false;
            }
            switch (_index)
            {
                case 0: return Equals(_value0, other._value0);
                case 1: return Equals(_value1, other._value1);
                case 2: return Equals(_value2, other._value2);
                case 3: return Equals(_value3, other._value3);
                case 4: return Equals(_value4, other._value4);
                case 5: return Equals(_value5, other._value5);
                case 6: return Equals(_value6, other._value6);
                case 7: return Equals(_value7, other._value7);
                case 8: return Equals(_value8, other._value8);
                default: return false;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is OneOf<T0, T1, T2, T3, T4, T5, T6, T7, T8> oneOf && Equals(oneOf);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode;
                switch (_index)
                {
                    case 0:
                        hashCode = _value0?.GetHashCode() ?? 0;
                        break;
                    case 1:
                        hashCode = _value1?.GetHashCode() ?? 0;
                        break;
                    case 2:
                        hashCode = _value2?.GetHashCode() ?? 0;
                        break;
                    case 3:
                        hashCode = _value3?.GetHashCode() ?? 0;
                        break;
                    case 4:
                        hashCode = _value4?.GetHashCode() ?? 0;
                        break;
                    case 5:
                        hashCode = _value5?.GetHashCode() ?? 0;
                        break;
                    case 6:
                        hashCode = _value6?.GetHashCode() ?? 0;
                        break;
                    case 7:
                        hashCode = _value7?.GetHashCode() ?? 0;
                        break;
                    case 8:
                        hashCode = _value8?.GetHashCode() ?? 0;
                        break;
                    default:
                        hashCode = 0;
                        break;
                }
                return (hashCode * 397) ^ _index;
            }
        }
    }
}
