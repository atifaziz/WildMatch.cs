//
// This is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
//
// Foobar is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Foobar.  If not, see <https://www.gnu.org/licenses/>.
//

namespace WildWildMatch
{
    using System;

    [Flags]
    public enum WildMatchFlags : uint
    {
        None = 0,
        CaseFold = GitWildMatch.WM_CASEFOLD,
        PathName = GitWildMatch.WM_PATHNAME,
    }

    public partial class WildMatch
    {
        public static bool IsMatch(string pattern, string text, WildMatchFlags flags)
            => IsMatch(pattern, text, flags, out var matched) ? matched
             : throw new ArgumentException("Pattern is malformed.", nameof(pattern));

        public static bool IsMatch(string pattern, string text, WildMatchFlags flags, out bool matched)
        {
            if (pattern == null) throw new ArgumentNullException(nameof(pattern));
            if (text == null) throw new ArgumentNullException(nameof(text));

            var result = GitWildMatch.dowild(pattern, text, flags);
            matched = result == GitWildMatch.WM_MATCH;
            return matched || result == GitWildMatch.WM_NOMATCH;
        }
    }
}
