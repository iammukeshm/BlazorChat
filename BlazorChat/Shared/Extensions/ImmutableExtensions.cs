using System;
using System.Collections.Immutable;

namespace BlazorChat.Shared.Extensions
{
    public static class ImmutableExtensions
    {
        public static ImmutableList<T> Update<T>(this ImmutableList<T> list,Predicate<T> predicate,Action<T> update)
        {
            var index = list.FindIndex(predicate);
            if(index >= 0)
            {
                var item = list[index];
                update(item);
                return list.SetItem(index, item);
            }
            return list;
        }
    }
}
