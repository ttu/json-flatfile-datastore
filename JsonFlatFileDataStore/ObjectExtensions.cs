using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
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
            throw new Exception("source or/and destination objects are null");

        if (destination.GetType() == typeof(ExpandoObject))
            HandleExpando(source, destination);
        else
            HandleTyped(source, destination);
    }

    private static void HandleTyped(object source, object destination)
    {
        var typeDest = destination.GetType();
        var typeSrc = source.GetType();

        foreach (var srcProp in typeSrc.GetProperties())
        {
            if (!srcProp.CanRead)
                continue;

            var targetProperty = typeDest.GetProperty(srcProp.Name);

            if (targetProperty == null)
                continue;

            if (IsArrayOrList(srcProp.PropertyType))
            {
                var arrayType = srcProp.PropertyType.GetElementType();

                var sourceArray = (IList)srcProp.GetValue(source, null);
                var targetArray = (IList)targetProperty.GetValue(destination, null);

                var type = targetProperty.PropertyType;

                if (type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    type = type.GetGenericArguments()[0];
                }

                for (int i = 0; i < sourceArray.Count; i++)
                {
                    var sourceValue = sourceArray[i];

                    if (sourceValue != null)
                    {
                        if (targetArray.Count - 1 < i)
                        {
                            var newTargetItem = Activator.CreateInstance(type);
                            targetArray.Add(newTargetItem);
                        }

                        if (type.GetTypeInfo().IsValueType)
                            targetArray[i] = sourceValue;
                        else
                            CopyProperties(sourceValue, targetArray[i]);
                    }
                }

                continue;
            }

            if (!targetProperty.CanWrite)
                continue;

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

    private static void HandleExpando(object source, object destination)
    {
        var typeDest = destination.GetType();
        var typeSrc = source.GetType();

        foreach (var srcProp in typeSrc.GetProperties())
        {
            if (!srcProp.CanRead)
                continue;

            if (IsArrayOrList(srcProp.PropertyType))
            {
                var arrayType = srcProp.PropertyType.GetElementType();

                var sourceArray = (IList)srcProp.GetValue(source, null);
                var targetArray = (IList)((IDictionary<string, object>)destination)[srcProp.Name];

                var type = targetArray.GetType();

                if (type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    type = type.GetGenericArguments()[0];
                }

                for (int i = 0; i < sourceArray.Count; i++)
                {
                    var sourceValue = sourceArray[i];

                    if (sourceValue != null)
                    {
                        if (type != typeof(ExpandoObject))
                        {
                            if (targetArray.Count - 1 < i)
                            {
                                targetArray.Add(Activator.CreateInstance(type));
                            }

                            if (type.GetTypeInfo().IsValueType)
                                targetArray[i] = sourceValue;
                            else
                                CopyProperties(sourceValue, targetArray[i]);
                        }
                        else
                        {
                            if (targetArray.Count - 1 < i)
                            {
                                targetArray.Add(new ExpandoObject());
                            }

                            HandleExpando(sourceValue, targetArray[i]);
                        }
                    }
                }

                continue;
            }

           ((IDictionary<string, object>)destination)[srcProp.Name] = srcProp.GetValue(source, null);
        }
    }

    private static bool IsArrayOrList(Type toTest)
    {
        return typeof(IEnumerable).IsAssignableFrom(toTest) && toTest != typeof(string);
    }
}