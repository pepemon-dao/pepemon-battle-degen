
namespace System.Collections.Generic
{
    public static class IEnumerableExtension
    { 
        /// <summary>
        /// Splits consecutive numbers into groups.
        /// </summary>
        /// <example>
        /// Given the sequence { 1, 2, 3, 4, 6, 7, 9 }, this function will return the groups: { {1, 2, 3, 4}, {6, 7}, {9} }
        /// </example>
        /// <param name="source">Source IEnumerable</param>
        /// <see cref="https://stackoverflow.com/a/48390596"/>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<ulong>> GroupConsecutive(this IEnumerable<ulong> source)
        {
            using (var e = source.GetEnumerator())
            {
                for (bool more = e.MoveNext(); more;)
                {
                    ulong first = e.Current, last = first, next;
                    while ((more = e.MoveNext()) && (next = e.Current) > last && next - last == 1)
                        last = next;
                    yield return Range(first, (last - first + 1));
                }
            }
        }

        /// <summary>
        /// Generates a sequence of ulong values within a specified range.
        /// </summary>
        /// <param name="start">The value of the first ulong in the sequence.</param>
        /// <param name="count">The number of sequential ulong values to generate.</param>
        /// <returns>An IEnumerable<ulong> that contains a sequence of ulong values.</returns>
        public static IEnumerable<ulong> Range(this ulong start, ulong count)
        {
            for (ulong i = 0; i < count; i++)
            {
                yield return start + i;
            }
        }
    }
}