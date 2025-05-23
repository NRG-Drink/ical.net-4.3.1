﻿//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization.DataTypes;

public abstract class DataTypeSerializer : SerializerBase
{
    protected DataTypeSerializer() { }

    protected DataTypeSerializer(SerializationContext ctx) : base(ctx) { }

    protected virtual ICalendarDataType? CreateAndAssociate()
    {
        // Create an instance of the object
        if (Activator.CreateInstance(TargetType, true) is not ICalendarDataType dt)
        {
            return null;
        }

        if (SerializationContext.Peek() is ICalendarObject associatedObject)
        {
            dt.AssociatedObject = associatedObject;
        }

        return dt;
    }
}
