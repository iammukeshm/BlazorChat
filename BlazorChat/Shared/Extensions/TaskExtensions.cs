using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace BlazorChat.Shared.Extensions
{
    public static class TaskExtensions
    {
        public static Task<TNewResult> OnCompletion<TNewResult,TResult>(this Task<TResult> task,  Func<TResult, TNewResult> continuationFunction)
         => task.ContinueWith(x => continuationFunction(x.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
        public static Task<ImmutableList<TResult>> ToImmutable<TResult>(this Task<IEnumerable<TResult>> task)
        => task.ContinueWith(x => task.Result.ToImmutableList(), TaskContinuationOptions.OnlyOnRanToCompletion);
        public static Task<ImmutableList<TResult>> ToImmutable<TResult>(this Task<List<TResult>> task)
        => task.ContinueWith(x => task.Result.ToImmutableList(), TaskContinuationOptions.OnlyOnRanToCompletion);
        public static Task<ImmutableList<TResult>> ToImmutable<TResult>(this Task<ICollection<TResult>> task)
        => task.ContinueWith(x => task.Result.ToImmutableList(), TaskContinuationOptions.OnlyOnRanToCompletion);
    }
}
