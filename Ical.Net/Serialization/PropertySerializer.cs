//using Ical.Net.DataTypes;
//using Ical.Net.Utility;
//using System;
//using System.IO;
//using System.Linq;
//using System.Text;

//namespace Ical.Net.Serialization
//{
//    public class PropertySerializer : SerializerBase
//    {
//        public PropertySerializer() { }

//        public PropertySerializer(SerializationContext ctx) : base(ctx) { }

//        public override Type TargetType => typeof(CalendarProperty);

//        public override string SerializeToString(object obj)
//        {
//            var prop = obj as ICalendarProperty;
//            if (prop?.Values == null || !prop.Values.Any())
//            {
//                return null;
//            }

//            // Push this object on the serialization context.
//            SerializationContext.Push(prop);

//            // Get a serializer factory that we can use to serialize
//            // the property and parameter values
//            var sf = GetService<ISerializerFactory>();

//            var result = new StringBuilder();
//            if (prop.Name == "CATEGORIES")
//            {
//                var v = prop.Values;
//                var valueSerializer = sf.Build(v.GetType(), SerializationContext) as IStringSerializer;
//                var value = valueSerializer.SerializeToString(v);
//                var parameterList = prop.Parameters;
//                if (v is ICalendarDataType)
//                {
//                    parameterList = (v as ICalendarDataType).Parameters;
//                }

//                var sb = new StringBuilder();
//                sb.Append(prop.Name);
//                if (parameterList.Any())
//                {
//                    // Get a serializer for parameters
//                    var parameterSerializer = sf.Build(typeof(CalendarParameter), SerializationContext) as IStringSerializer;
//                    if (parameterSerializer != null)
//                    {
//                        // Serialize each parameter
//                        // Separate parameters with semicolons
//                        sb.Append(";");
//                        sb.Append(string.Join(";", parameterList.Select(param => parameterSerializer.SerializeToString(param))));
//                    }
//                }
//                sb.Append(":");
//                sb.Append(value);

//                result.Append(TextUtil.FoldLines(sb.ToString()));
//            }
//            else
//            {
//                foreach (var v in prop.Values.Where(value => value != null))
//                {
//                    // Get a serializer to serialize the property's value.
//                    // If we can't serialize the property's value, the next step is worthless anyway.
//                    var valueSerializer = sf.Build(v.GetType(), SerializationContext) as IStringSerializer;

//                    // Iterate through each value to be serialized,
//                    // and give it a property (with parameters).
//                    // FIXME: this isn't always the way this is accomplished.
//                    // Multiple values can often be serialized within the
//                    // same property.  How should we fix this?

//                    // NOTE:
//                    // We Serialize the property's value first, as during
//                    // serialization it may modify our parameters.
//                    // FIXME: the "parameter modification" operation should
//                    // be separated from serialization. Perhaps something
//                    // like PreSerialize(), etc.
//                    var value = valueSerializer.SerializeToString(v);

//                    // Get the list of parameters we'll be serializing
//                    var parameterList = prop.Parameters;
//                    if (v is ICalendarDataType)
//                    {
//                        parameterList = (v as ICalendarDataType).Parameters;
//                    }

//                    //This says that the TZID property of an RDATE/EXDATE collection is owned by the PeriodList that contains it. There's nothing in the spec that
//                    //prohibits having multiple EXDATE or RDATE collections, each of which specifies a different TZID. What *should* happen during serialization is
//                    //that we should work with a single collection of zoned datetime objects, and we should create distinct RDATE and EXDATE collections based on
//                    //those values. Right now, if you add CalDateTime objects, each of which specifies a different time zone, the first one "wins". This means
//                    //application developers will need to handle those cases outside the library.
//                    if (v is PeriodList)
//                    {
//                        var typed = (PeriodList)v;
//                        if (!string.IsNullOrWhiteSpace(typed.TzId) && parameterList.All(p => string.Equals("TZID", p.Value, StringComparison.OrdinalIgnoreCase)))
//                        {
//                            parameterList.Set("TZID", typed.TzId);
//                        }
//                    }

//                    var sb = new StringBuilder();
//                    sb.Append(prop.Name);
//                    if (parameterList.Any())
//                    {
//                        // Get a serializer for parameters
//                        var parameterSerializer = sf.Build(typeof(CalendarParameter), SerializationContext) as IStringSerializer;
//                        if (parameterSerializer != null)
//                        {
//                            // Serialize each parameter
//                            // Separate parameters with semicolons
//                            sb.Append(";");
//                            sb.Append(string.Join(";", parameterList.Select(param => parameterSerializer.SerializeToString(param))));
//                        }
//                    }
//                    sb.Append(":");
//                    sb.Append(value);

//                    result.Append(TextUtil.FoldLines(sb.ToString()));
//                }
//            }



//            // Pop the object off the serialization context.
//            SerializationContext.Pop();
//            return result.ToString();
//        }

//        public override object Deserialize(TextReader tr) => null;
//    }
//}

//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ical.Net.DataTypes;
using Ical.Net.Utility;

namespace Ical.Net.Serialization;

public class PropertySerializer : SerializerBase
{
    public PropertySerializer() { }

    public PropertySerializer(SerializationContext ctx) : base(ctx) { }

    public override Type TargetType => typeof(CalendarProperty);

    public override string? SerializeToString(object? obj)
    {
        var prop = obj as ICalendarProperty;
        if (prop?.Values == null || !prop.Values.Any())
        {
            return null;
        }

        // Push this object on the serialization context.
        SerializationContext.Push(prop);

        // Get a serializer factory that we can use to serialize
        // the property and parameter values
        var sf = GetService<ISerializerFactory>();

        var result = new StringBuilder();
        if (prop.Name == "CATEGORIES")
        {
            SerializeValue(result, prop, prop.Values, sf);
        }
        else
        {
            foreach (var v in prop.Values.Where(value => value != null))
            {
                SerializeValue(result, prop, v!, sf);
            }
        }

        // Pop the object off the serialization context.
        SerializationContext.Pop();
        return result.ToString();
    }

    private void SerializeValue(StringBuilder result, ICalendarProperty prop, object value, ISerializerFactory sf)
    {
        // Get a serializer to serialize the property's value.
        // If we can't serialize the property's value, the next step is worthless anyway.
        var valueSerializer = sf.Build(value.GetType(), SerializationContext) as IStringSerializer;

        // Iterate through each value to be serialized,
        // and give it a property (with parameters).
        // FIXME: this isn't always the way this is accomplished.
        // Multiple values can often be serialized within the
        // same property. How should we fix this?

        // NOTE:
        // We Serialize the property's value first, as during
        // serialization it may modify our parameters.
        // FIXME: the "parameter modification" operation should
        // be separated from serialization. Perhaps something
        // like PreSerialize(), etc.
        var serializedValue = valueSerializer?.SerializeToString(value);

        // Get the list of parameters we'll be serializing
        var parameterList =
            (IList<CalendarParameter>?)(value as ICalendarDataType)?.Parameters
            //?? (valueSerializer as IParameterProvider)?.GetParameters(value).ToList()
            ?? (IList<CalendarParameter>)prop.Parameters;

        var sb = new StringBuilder();
        sb.Append(prop.Name);
        if (parameterList.Count != 0)
        {
            // Get a serializer for parameters
            var parameterSerializer = sf.Build(typeof(CalendarParameter), SerializationContext) as IStringSerializer;
            if (parameterSerializer != null)
            {
                sb.Append(';');
                var first = true;
                // Serialize each parameter and append to the StringBuilder
                foreach (var param in parameterList)
                {
                    if (!first) sb.Append(';');

                    sb.Append(parameterSerializer.SerializeToString(param));
                    first = false;
                }
            }
        }

        sb.Append(':');
        sb.Append(serializedValue);

        //result.FoldLines(sb.ToString());
        result.Append(TextUtil.FoldLines(sb.ToString()));
    }

    public override object? Deserialize(TextReader tr) => null;
}
