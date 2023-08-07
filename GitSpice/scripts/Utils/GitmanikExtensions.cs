using System.Collections.Generic;

namespace Gitmanik.Utils.Extensions;

static class GitmanikExtensions
{
    public static string JoinList(this IEnumerable<object> objects, string delim = ",") => string.Join(delim, objects);
}