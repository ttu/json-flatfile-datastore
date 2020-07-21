using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;

internal static class ObjectExtensions
{
    /// <summary>
    /// Copy property values from the source object to the destination object
    /// </summary>
    /// <param name="source">The source</param>
    /// <param name="destination">The destination</param>
    internal static void CopyProperties(object source, object destination)
    {
        if (source == null || destination == null)
            throw new Exception("source or/and destination objects are null");

        if (source is JToken || IsDictionary(source.GetType()))
            source = JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(source), new ExpandoObjectConverter());

        if (destination is ExpandoObject)
            HandleExpando(source, destination);
        else
            HandleTyped(source, destination);
    }

    internal static void AddDataToField(object item, string fieldName, dynamic data)
    {
        if (item is JToken)
        {
            dynamic jTokenItem = item;
            jTokenItem[fieldName] = data;
        }
        else if (item is ExpandoObject)
        {
            dynamic expandoItem = item;
            var expandoDict = expandoItem as IDictionary<string, object>;
            expandoDict[fieldName] = data;
        }
        else if (IsDictionary(item.GetType()))
        {
            dynamic dictionaryItem = item;
            dictionaryItem[fieldName] = data;
        }
        else
        {
            var idProperty = item.GetType().GetProperties().FirstOrDefault(p => string.Equals(p.Name, fieldName, StringComparison.OrdinalIgnoreCase));

            if (idProperty != null && idProperty.CanWrite)
                idProperty.SetValue(item, data);
        }
    }

    internal static bool IsAnonymousType(object o)
    {
        var name = o.GetType().Name;
        return name.Length >= 3 &&
               name[0] == '<' &&
               name[1] == '>' &&
               name.IndexOf("AnonymousType", StringComparison.Ordinal) > 0;
    }

    internal static bool HasField<T>(T item, string idField)
    {
        var idProperty = item.GetType()
                             .GetProperties()
                             .FirstOrDefault(p => string.Equals(p.Name, idField, StringComparison.OrdinalIgnoreCase));

        return idProperty != null;
    }

    internal static bool FullTextSearch(dynamic source, string text, bool caseSensitive = false)
    {
        var compareFunc = caseSensitive
                       ? new Func<string, string, bool>((a, b) => a.IndexOf(b, StringComparison.Ordinal) >= 0)
                       : new Func<string, string, bool>((a, b) => a.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0);

        bool AnyPropertyHasValue(dynamic current)
        {
            if (current == null)
                return false;

            if (IsValueReferenceType(current.GetType()))
            {
                foreach (var srcProp in GetProperties(current))
                {
                    if (IsEnumerable(srcProp.PropertyType) && srcProp.PropertyType != typeof(ExpandoObject))
                    {
                        var collection = GetValue(current, srcProp);

                        if (collection == null)
                            continue;

                        foreach (var i in collection as IEnumerable)
                        {
                            if (AnyPropertyHasValue(i))
                                return true;
                        }
                    }
                    else
                    {
                        if (AnyPropertyHasValue(GetValue(current, srcProp)))
                            return true;
                    }
                }
            }
            else
            {
                if (compareFunc(current.ToString(), text))
                    return true;
            }

            return false;
        }

        return AnyPropertyHasValue(source);
    }

    internal static bool IsReferenceType(dynamic o) => IsValueReferenceType(o.GetType());

    internal static dynamic GetDefaultValue<T>(string fieldName)
    {
        var idProp = typeof(T).GetProperties().FirstOrDefault(p => string.Equals(p.Name, fieldName, StringComparison.OrdinalIgnoreCase));

        switch (idProp)
        {
            case null:
                return 0;

            case var p when p.PropertyType.IsValueType:
                return Activator.CreateInstance(p.PropertyType);

            case var p when p.PropertyType == typeof(string):
                return "0";

            default:
                return null;
        }
    }

    internal static dynamic GetFieldValue(object source, string fieldName)
    {
        if (source is ExpandoObject sourceExpando)
        {
            var sourceExpandoDict = new Dictionary<string, dynamic>(sourceExpando, StringComparer.OrdinalIgnoreCase);
            return sourceExpandoDict.ContainsKey(fieldName) ? sourceExpandoDict[fieldName] : null;
        }

        var srcProp = source.GetType().GetProperties().FirstOrDefault(p => string.Equals(p.Name, fieldName, StringComparison.OrdinalIgnoreCase));
        return srcProp?.GetValue(source, null);
    }

    private static void HandleTyped(object source, object destination)
    {
        foreach (var srcProp in GetProperties(source))
        {
            var targetProperty = destination.GetType().GetProperty((string)srcProp.Name)
                                    ?? destination.GetType().GetProperty(SwitchFirstChar((string)srcProp.Name));

            if (targetProperty == null)
                continue;

            if (srcProp.PropertyType == typeof(ExpandoObject))
            {
                var targetValue = targetProperty.GetValue(destination, null);
                var sourceValue = GetValue(source, srcProp);
                HandleTyped(sourceValue, targetValue);
                continue;
            }

            if (IsDictionary(srcProp.PropertyType))
            {
                var targetDict = (IDictionary)targetProperty.GetValue(destination, null);
                var sourceDict = (IDictionary)GetValue(source, srcProp);

                targetDict.Clear();

                foreach (var item in sourceDict)
                {
                    var kvp = (DictionaryEntry)item;
                    targetDict.Add(kvp.Key, kvp.Value);
                }

                continue;
            }

            if (IsEnumerable(srcProp.PropertyType))
            {
                var sourceArray = (IList)GetValue(source, srcProp);
                var targetArray = (IList)targetProperty.GetValue(destination, null);

                if (sourceArray == null)
                {
                    targetProperty.SetValue(destination, null);
                    continue;
                }
                else if (targetArray == null)
                {
                    targetArray = CreateInstance(srcProp.PropertyType);
                    targetProperty.SetValue(destination, targetArray);
                }

                var type = targetProperty.PropertyType;

                if (IsGenericListOrColletion(type))
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
                            var newTargetItem = CreateInstance(type);
                            targetArray.Add(newTargetItem);
                        }

                        if (type.GetTypeInfo().IsValueType || type == typeof(string))
                            targetArray[i] = sourceValue;
                        else
                            CopyProperties(sourceValue, targetArray[i]);
                    }
                }

                continue;
            }

            if (!targetProperty.CanWrite)
                continue;

            if (IsPropertyReferenceType(srcProp) && IsPropertyReferenceType(targetProperty))
            {
                var target = targetProperty.GetValue(destination, null);
                var sourcePropertyValue = GetValue(source, srcProp);

                if (target == null || sourcePropertyValue == null)
                    targetProperty.SetValue(destination, sourcePropertyValue);
                else
                    CopyProperties(sourcePropertyValue, target);

                continue;
            }

            if (targetProperty.GetSetMethod(true)?.IsPrivate ?? true)
                continue;

            if ((targetProperty.GetSetMethod().Attributes & MethodAttributes.Static) != 0)
                continue;

            if (!targetProperty.PropertyType.IsAssignableFrom((Type)srcProp.PropertyType))
                continue;

            targetProperty.SetValue(destination, GetValue(source, srcProp), null);
        }
    }

    private static string SwitchFirstChar(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var chars = name.ToCharArray();
        var first = chars.First();

        return (char.IsLower(first) ? char.ToString(first).ToUpper() : char.ToString(first).ToLower()) +
                    (chars.Length > 1 ? new string(chars.Skip(1).ToArray()) : string.Empty);
    }

    private static void HandleExpando(object source, object destination)
    {
        foreach (var srcProp in GetProperties(source))
        {
            if (srcProp.PropertyType == typeof(ExpandoObject))
            {
                var destExpandoDict = ((IDictionary<string, object>)destination);

                if (!destExpandoDict.ContainsKey(srcProp.Name))
                    destExpandoDict.Add(srcProp.Name, CreateInstance(srcProp.PropertyType));

                var sourceValue = GetValue(source, srcProp);
                HandleExpando(sourceValue, destExpandoDict[srcProp.Name]);
            }
            else if (IsDictionary(srcProp.PropertyType))
            {
                var destExpandoDict = ((IDictionary<string, object>)destination);

                if (!destExpandoDict.ContainsKey(srcProp.Name))
                    destExpandoDict.Add(srcProp.Name, CreateInstance(srcProp.PropertyType));

                var targetDict = (IDictionary)destExpandoDict[srcProp.Name];
                var sourceDict = (IDictionary)GetValue(source, srcProp);

                targetDict.Clear();

                foreach (var item in sourceDict)
                {
                    var kvp = (DictionaryEntry)item;
                    targetDict.Add(kvp.Key, kvp.Value);
                }

                continue;
            }
            else if (IsEnumerable(srcProp.PropertyType))
            {
                var destExpandoDict = ((IDictionary<string, object>)destination);

                if (!destExpandoDict.ContainsKey(srcProp.Name))
                    destExpandoDict.Add(srcProp.Name, CreateInstance(srcProp.PropertyType));

                var targetArray = (IList)destExpandoDict[srcProp.Name];
                var sourceArray = (IList)GetValue(source, srcProp);

                if (sourceArray == null)
                {
                    destExpandoDict[srcProp.Name] = null;
                    continue;
                }
                else if (targetArray == null)
                {
                    targetArray = CreateInstance(srcProp.PropertyType);
                    destExpandoDict[srcProp.Name] = targetArray;
                }

                var type = targetArray.GetType();

                if (IsGenericListOrColletion(type))
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
                                targetArray.Add(CreateInstance(type));
                            }

                            if (type.GetTypeInfo().IsValueType || type == typeof(string))
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
            }
            else
            {
                ((IDictionary<string, object>)destination)[srcProp.Name] = GetValue(source, srcProp);
            }
        }
    }

    private static bool IsPropertyReferenceType(dynamic srcProp)
    {
        return srcProp.PropertyType.IsClass && srcProp.PropertyType != typeof(string);
    }

    private static bool IsValueReferenceType(dynamic type)
    {
        return !type.IsValueType && !type.IsPrimitive && type != typeof(string);
    }

    private static object GetValue(object source, dynamic srcProp)
    {
        return source is ExpandoObject ? srcProp.Value : srcProp.GetValue(source, null);
    }

    private static IEnumerable<dynamic> GetProperties(object source)
    {
        if (source is ExpandoObject)
        {
            return ((IDictionary<string, object>)source)
                .Select(i => new { Name = i.Key, Value = i.Value, PropertyType = i.Value?.GetType() })
                .ToList();
        }
        else
        {
            return source.GetType().GetProperties();
        }
    }

    private static bool IsEnumerable(Type toTest)
    {
        return typeof(IEnumerable).IsAssignableFrom(toTest) && toTest != typeof(string);
    }

    private static bool IsDictionary(Type toTest)
    {
        return typeof(IDictionary).IsAssignableFrom(toTest) && toTest != typeof(string);
    }

    private static bool IsGenericListOrColletion(Type toTest)
    {
        return toTest.GetGenericTypeDefinition() == typeof(IList<>) ||
               toTest.GetGenericTypeDefinition() == typeof(List<>) ||
               toTest.GetGenericTypeDefinition() == typeof(ICollection<>) ||
               toTest.GetGenericTypeDefinition() == typeof(Collection<>);
    }

    private static dynamic CreateInstance(Type type)
    {
        return type != typeof(string) ? Activator.CreateInstance(type) : string.Empty;
    }
}