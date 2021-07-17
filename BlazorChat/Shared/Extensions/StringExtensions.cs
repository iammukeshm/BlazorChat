namespace BlazorChat.Shared.Extensions
{
    public static class StringExtensions
    {
        public static bool IsNullEmpty(this string str)
        => string.IsNullOrEmpty(str);
    }
}
