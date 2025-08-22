using System;
using System.Linq;
using System.Text.Json;

namespace Sellorio.YouTubeMusicGrabber.Helpers
{
    internal class JsonNavigator
    {
        private readonly JsonElement _element;
        private int? _arrayLength;

        public JsonNavigator(JsonDocument document)
        {
            _element = document.RootElement;
        }

        public JsonNavigator(JsonElement element)
        {
            _element = element;
        }

        public JsonNavigator this[string propertyKey] => new(_element.GetProperty(propertyKey));
        public JsonNavigator this[int arrayIndex] => new(_element[arrayIndex]);

        public int ArrayLength => _arrayLength ?? (_arrayLength = _element.GetArrayLength()).Value;

        public JsonNavigator Nth(int arrayIndex)
        {
            return new(_element[arrayIndex]);
        }

        public JsonNavigator NthFromLast(int arrayIndex)
        {
            return new(_element[_element.GetArrayLength() - arrayIndex]);
        }

        public TValue Get<TValue>(string propertyKey)
        {
            return (TValue)Get(_element.GetProperty(propertyKey), typeof(TValue));
        }

        public TValue Get<TValue>(int arrayIndex)
        {
            return (TValue)Get(_element[arrayIndex], typeof(TValue));
        }

        private static object Get(JsonElement element, Type targetType)
        {
            if (targetType == typeof(bool))
                return element.GetBoolean();

            if (targetType == typeof(byte))
                return element.GetByte();

            if (targetType == typeof(sbyte))
                return element.GetSByte();

            if (targetType == typeof(short))
                return element.GetInt16();

            if (targetType == typeof(ushort))
                return element.GetUInt16();

            if (targetType == typeof(int))
                return element.GetInt32();

            if (targetType == typeof(uint))
                return element.GetUInt32();

            if (targetType == typeof(long))
                return element.GetInt64();

            if (targetType == typeof(ulong))
                return element.GetUInt64();

            if (targetType == typeof(float))
                return element.GetSingle();

            if (targetType == typeof(double))
                return element.GetDouble();

            if (targetType == typeof(decimal))
                return element.GetDecimal();

            if (targetType == typeof(DateTime))
                return element.GetDateTime();

            if (targetType == typeof(DateTimeOffset))
                return element.GetDateTimeOffset();

            if (targetType == typeof(byte[]))
                return element.GetBytesFromBase64();

            if (targetType == typeof(Guid))
                return element.GetGuid();

            if (targetType == typeof(string))
                return element.GetString();

            if (targetType.IsEnum)
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    var stringValue = element.GetString();
                    return targetType.GetEnumValues().Cast<object>().FirstOrDefault(x => x.ToString() == stringValue);
                }
                
                if (element.ValueKind == JsonValueKind.Number)
                {
                    var underlyingTypeEnumValue = Get(element, targetType.GetEnumUnderlyingType());
                    return Convert.ChangeType(underlyingTypeEnumValue, targetType);
                }
            }

            throw new NotSupportedException("Unexpected type requested from JSON.");
        }
    }
}
