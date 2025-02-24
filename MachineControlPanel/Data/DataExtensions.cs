namespace MachineControlPanel.Data;

internal static class DataExtensions
{
    /// <summary>
    /// Attempt to get value from key in dictionary being used as a cache.
    /// If the key is not set, create and set it using provided delegate.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="dict"></param>
    /// <param name="key"></param>
    /// <param name="createValue"></param>
    /// <returns></returns>
    public static TValue GetOrCreateValue<TKey, TValue>(
        this Dictionary<TKey, TValue> dict,
        TKey key,
        Func<TKey, TValue> createValue
    )
        where TKey : notnull
    {
        if (dict.TryGetValue(key, out TValue? result))
            return result;
        result = createValue(key);
        dict[key] = result;
        return result;
    }
}
