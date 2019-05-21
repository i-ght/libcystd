using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;

namespace LibCyStd
{
    public static class RegexModule
    {
        public static Option<string> TryParseGroup1(this Regex r, string input)
        {
            var m = r.Match(input);
            if (m.Success && !string.IsNullOrWhiteSpace(m.Groups[1].Value))
                return m.Groups[1].Value;
            return Option.None;
        }
    }
}
