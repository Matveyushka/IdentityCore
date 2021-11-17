using System.Collections.Generic;
using System.Linq;

static class FilterDispatcher
{
    public static bool GetBooleanFilterValue(string filter, 
    List<string> partialMatchKeywords, 
    List<string> fullMatchKeywords)
    {
        var upperedFilter = filter.ToUpper();
        return partialMatchKeywords.Any(keyword => upperedFilter.Contains(keyword.ToUpper())) ||
            fullMatchKeywords.Any(keyword => upperedFilter == keyword.ToUpper());
    }
    public static bool GetEnabledFilterValue(string filter) => GetBooleanFilterValue(filter,
        new List<string> {"enabled"}, new List<string> {"+", "true"});

    public static bool GetDisabledFilterValue(string filter) => GetBooleanFilterValue(filter,
        new List<string> {"disabled"}, new List<string> {"-", "false"});
}