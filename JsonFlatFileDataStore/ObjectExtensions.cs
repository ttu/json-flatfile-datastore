using System;
using System.Reflection;

public static class ObjectExtensions
{
    /// <summary>
    ///  Copy property values from the source object to the destination object
    /// </summary>
    /// <param name="source">The source</param>
    /// <param name="destination">The destination</param>
    public static void CopyProperties(this object source, object destination)
    {
        if (source == null || destination == null)
            throw new Exception("Source or/and Destination óbjects are null");

        var typeDest = destination.GetType();
        var typeSrc = source.GetType();

        foreach (var srcProp in typeSrc.GetProperties())
        {
            if (!srcProp.CanRead)
                continue;

            PropertyInfo targetProperty = typeDest.GetProperty(srcProp.Name);

            if (targetProperty == null || !targetProperty.CanWrite)
                continue;

            if (srcProp.PropertyType.IsArray)
            {
                // TODO: Implementation for array types
                continue;
            }

            if (srcProp.PropertyType.GetTypeInfo().IsClass && srcProp.PropertyType != typeof(string) &&
                targetProperty.PropertyType.GetTypeInfo().IsClass && targetProperty.PropertyType != typeof(string))
            {
                CopyProperties(srcProp.GetValue(source, null), targetProperty.GetValue(destination, null));
                continue;
            }

            if (targetProperty.GetSetMethod(true)?.IsPrivate ?? true)
                continue;

            if ((targetProperty.GetSetMethod().Attributes & MethodAttributes.Static) != 0)
                continue;

            if (!targetProperty.PropertyType.IsAssignableFrom(srcProp.PropertyType))
                continue;

            targetProperty.SetValue(destination, srcProp.GetValue(source, null), null);
        }
    }
}