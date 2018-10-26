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

/*
**  Do shell-style pattern matching for ?, \, [], and * characters.
**  It is 8bit clean.
**
**  Written by Rich $alz, mirror!rs, Wed Nov 26 19:03:17 EST 1986.
**  Rich $alz is now <rsalz@bbn.com>.
**
**  Modified by Wayne Davison to special-case '/' matching, to make '**'
**  work differently than '*', and to fix the character-class code.
*/

#define NEGATE_CLASS2

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo

using System;
using System.Diagnostics;

namespace WildWildMatch
{
    static class GitWildMatch
    {
        /// <summary>
        /// Poor man's emulation of C's <c>int</c>.
        /// </summary>

        [DebuggerDisplay("{" + nameof(n) + "}")]
        struct @int
        {
            readonly int n;

            @int(int n) => this.n = n;
            public override string ToString() => n.ToString();

            public static bool operator !(@int n) => !(bool) n;

            public static implicit operator @int(int n) => new @int(n);
            public static implicit operator bool(@int n) => n.n != 0;
            public static implicit operator char(@int n) => (char) n.n;
            public static implicit operator int(@int n) => n.n;
            public static implicit operator @int(bool f) => f ? 1 : 0;

            public static bool operator ==(@int a, @int b) => a.n == b.n;
            public static bool operator !=(@int a, @int b) => !(a == b);

            public override int GetHashCode() => throw new NotImplementedException();
            public override bool Equals(object o) => throw new NotImplementedException();
        }

        /// <summary>
        /// Poor man's emulation of <c>uchar</c>.
        /// </summary>

        [DebuggerDisplay("{" + nameof(ch) + "}")]
        struct uchar
        {
            readonly char ch;

            public uchar(char ch) => this.ch = ch;
            public override string ToString() => ch.ToString();

            public static bool operator ==(uchar a, uchar b) => a.ch == b.ch;
            public static bool operator !=(uchar a, uchar b) => !(a == b);

            public static implicit operator char(uchar f) => f.ch;
            public static implicit operator uchar(int ch) => new uchar((char) ch);
            public static implicit operator bool(uchar f) => f.ch != '\0';

            public override int GetHashCode() => throw new NotImplementedException();
            public override bool Equals(object o) => throw new NotImplementedException();
        }

        /// <summary>
        /// Poor man's emulation of <c>uchar *</c>.
        /// </summary>

        [DebuggerDisplay("{" + nameof(ToString) + "()}")]
        struct uchar_ptr
        {
            readonly string _str;   // base memory
            readonly int _i;        // offset

            uchar_ptr(string str, int i = 0) { _str = str; _i = i; }

            string str => _str ?? throw new NullReferenceException();
            int i => _i >= 0 && _i <= str?.Length ? _i : throw new NullReferenceException();

            public uchar this[int index] => i + index == str.Length ? '\0' : str[i + index];
            uchar deref => this ? str[i] : '\0';

            public uchar_ptr strchr(char ch)
            {
                var i = str.IndexOf(ch, this.i);
                return i < 0 ? new uchar_ptr() : new uchar_ptr(str, i);
            }

            public uchar_ptr strstr(string s)
                => str.IndexOf(s, this.i, StringComparison.Ordinal) is int i && i >= 0
                 ? new uchar_ptr(_str, i)
                 : default;

            public override string ToString() => _str?.Substring(i);

            public static uchar_ptr operator ++(uchar_ptr p) => p + 1;
            public static int? operator -(uchar_ptr a, uchar_ptr b) => ReferenceEquals(a.str, b.str) ? a._i - b._i : (int?) null;
            public static uchar_ptr operator -(uchar_ptr p, int offset) => new uchar_ptr(p.str, p._i - offset);
            public static uchar_ptr operator +(uchar_ptr p, int offset) => new uchar_ptr(p.str, p._i + offset);

            public static bool operator !(uchar_ptr p) => p ? false : true;
            public static bool operator true(uchar_ptr p) => p._str != null && p._i >=0 && p._i < p._str.Length;
            public static bool operator false(uchar_ptr p) => !p;

            public static bool operator ==(uchar_ptr p, char ch) => p.deref == ch;
            public static bool operator !=(uchar_ptr p, char ch) => !(p == ch);
            public static bool operator ==(uchar_ptr a, uchar_ptr b) => ReferenceEquals(a._str, b._str) && a._i == b._i;
            public static bool operator !=(uchar_ptr a, uchar_ptr b) => !(a == b);
            public static bool operator ==(uchar_ptr p, int @null) => @null == 0 && p._str == null ? true : throw new NotImplementedException();
            public static bool operator !=(uchar_ptr p, int @null) => @null == 0 ? p._str != null : throw new NotImplementedException();

            public static implicit operator char(uchar_ptr p) => p.deref;
            public static implicit operator uchar(uchar_ptr p) => new uchar(p.deref);
            public static implicit operator uchar_ptr(string s) => new uchar_ptr(s);

            public static bool? operator <(uchar_ptr a, uchar_ptr b) => ReferenceEquals(a.str, b.str) ? a._i < b._i : (bool?) null;
            public static bool? operator >(uchar_ptr a, uchar_ptr b) => ReferenceEquals(a.str, b.str) ? a._i > b._i : (bool?) null;

            public override int GetHashCode() => throw new NotImplementedException();
            public override bool Equals(object o) => throw new NotImplementedException();
        }

        /// <summary>
        /// Poor man's emulation of C's <c>unsigned int</c>.
        /// </summary>

        [DebuggerDisplay("{" + nameof(n) + "}")]
        struct unsigned_int
        {
            readonly uint n;
            unsigned_int(uint n) => this.n = n;
            public static bool operator &(unsigned_int f, uint x) => (f.n & x) == x;
            public static implicit operator unsigned_int(uint n) => new unsigned_int(n);
        }

        public const int WM_ABORT_MALFORMED = 2;
        public const int WM_NOMATCH = 1;
        public const int WM_MATCH = 0;
        public const int WM_ABORT_ALL = -1;
        public const int WM_ABORT_TO_STARSTAR = -2;

        const char NEGATE_CLASS = '!';
        const char NEGATE_CLASS2 = '^';

        static bool CC_EQ(uchar_ptr s, int i, string @class) => s.strstr(@class) == s;

        static uchar_ptr strchr(uchar_ptr s, char ch) => s.strchr(ch);

        public const uint WM_CASEFOLD = 1;
        public const uint WM_PATHNAME = 2;

        const int NULL = 0;

        public static int dowild(string p, string text, WildMatchFlags flags) =>
            dowild(p, text, (uint) flags);

        //
        // The following implementation is 99% identical (counting
        // non-whitespace characters) to the C source code found in Git:
        //
        // https://github.com/git/git/blob/53f9a3e157dbbc901a02ac2c73346d375e24978c/wildmatch.c
        //
        // Most changes amounted to simply replacing `uchar *` with `uchar_ptr`
        // and removing the pointer dereferences (i.e. `*`).
        //

        static int dowild(uchar_ptr p, uchar_ptr text, unsigned_int flags)
        {
            uchar p_ch;
            uchar_ptr pattern = p;

            for ( ; (p_ch = p) != '\0'; text++, p++) {
                @int matched, match_slash, negated;
                uchar t_ch, prev_ch;
                if ((t_ch = text) == '\0' && p_ch != '*')
                    return WM_ABORT_ALL;
                if ((flags & WM_CASEFOLD) && ISUPPER(t_ch))
                    t_ch = tolower(t_ch);
                if ((flags & WM_CASEFOLD) && ISUPPER(p_ch))
                    p_ch = tolower(p_ch);
                switch (p_ch) {
                case '\\':
                    /* Literal match with following character.  Note that the test
                     * in "default" handles the p[1] == '\0' failure case. */
                    p_ch = ++p;
                    /* FALLTHROUGH */
                    goto default;
                default:
                    if (t_ch != p_ch)
                        return WM_NOMATCH;
                    continue;
                case '?':
                    /* Match anything but '/'. */
                    if ((flags & WM_PATHNAME) && t_ch == '/')
                        return WM_NOMATCH;
                    continue;
                case '*':
                    if (++p == '*') {
                        uchar_ptr prev_p = p - 2;
                        while (++p == '*') {}
                        if (!(flags & WM_PATHNAME))
                            /* without WM_PATHNAME, '*' == '**' */
                            match_slash = 1;
                        else if ((prev_p < pattern == true || prev_p == '/') &&
                            (p == '\0' || p == '/' ||
                             (p[0] == '\\' && p[1] == '/'))) {
                            /*
                             * Assuming we already match 'foo/' and are at
                             * <star star slash>, just assume it matches
                             * nothing and go ahead match the rest of the
                             * pattern with the remaining string. This
                             * helps make foo/<*><*>/bar (<> because
                             * otherwise it breaks C comment syntax) match
                             * both foo/bar and foo/a/bar.
                             */
                            if (p[0] == '/' &&
                                dowild(p + 1, text, flags) == WM_MATCH)
                                return WM_MATCH;
                            match_slash = 1;
                        } else
                            return WM_ABORT_MALFORMED;
                    } else
                        /* without WM_PATHNAME, '*' == '**' */
                        match_slash = flags & WM_PATHNAME ? 0 : 1;
                    if (p == '\0') {
                        /* Trailing "**" matches everything.  Trailing "*" matches
                         * only if there are no more slash characters. */
                        if (!match_slash) {
                            if (strchr(text, '/') != NULL)
                                return WM_NOMATCH;
                        }
                        return WM_MATCH;
                    } else if (!match_slash && p == '/') {
                        /*
                         * _one_ asterisk followed by a slash
                         * with WM_PATHNAME matches the next
                         * directory
                         */
                        uchar_ptr slash = strchr(text, '/');
                        if (!slash)
                            return WM_NOMATCH;
                        text = slash;
                        /* the slash is consumed by the top-level for loop */
                        break;
                    }
                    while (true) {
                        if (t_ch == '\0')
                            break;
                        /*
                         * Try to advance faster when an asterisk is
                         * followed by a literal. We know in this case
                         * that the string before the literal
                         * must belong to "*".
                         * If match_slash is false, do not look past
                         * the first slash as it cannot belong to '*'.
                         */
                        if (!is_glob_special(p)) {
                            p_ch = p;
                            if ((flags & WM_CASEFOLD) && ISUPPER(p_ch))
                                p_ch = tolower(p_ch);
                            while ((t_ch = text) != '\0' &&
                                   (match_slash || t_ch != '/')) {
                                if ((flags & WM_CASEFOLD) && ISUPPER(t_ch))
                                    t_ch = tolower(t_ch);
                                if (t_ch == p_ch)
                                    break;
                                text++;
                            }
                            if (t_ch != p_ch)
                                return WM_NOMATCH;
                        }
                        if ((matched = dowild(p, text, flags)) != WM_NOMATCH) {
                            if (!match_slash || matched != WM_ABORT_TO_STARSTAR)
                                return matched;
                        } else if (!match_slash && t_ch == '/')
                            return WM_ABORT_TO_STARSTAR;
                        t_ch = ++text;
                    }
                    return WM_ABORT_ALL;
                case '[':
                    p_ch = ++p;
        #if NEGATE_CLASS2
                    if (p_ch == NEGATE_CLASS2)
                        p_ch = NEGATE_CLASS;
        #endif
                    /* Assign literal 1/0 because of "matched" comparison. */
                    negated = p_ch == NEGATE_CLASS ? 1 : 0;
                    if (negated) {
                        /* Inverted character class. */
                        p_ch = ++p;
                    }
                    prev_ch = 0;
                    matched = 0;
                    do {
                        if (!p_ch)
                            return WM_ABORT_ALL;
                        if (p_ch == '\\') {
                            p_ch = ++p;
                            if (!p_ch)
                                return WM_ABORT_ALL;
                            if (t_ch == p_ch)
                                matched = 1;
                        } else if (p_ch == '-' && prev_ch && p[1] && p[1] != ']') {
                            p_ch = ++p;
                            if (p_ch == '\\') {
                                p_ch = ++p;
                                if (!p_ch)
                                    return WM_ABORT_ALL;
                            }
                            if (t_ch <= p_ch && t_ch >= prev_ch)
                                matched = 1;
                            else if ((flags & WM_CASEFOLD) && ISLOWER(t_ch)) {
                                uchar t_ch_upper = toupper(t_ch);
                                if (t_ch_upper <= p_ch && t_ch_upper >= prev_ch)
                                    matched = 1;
                            }
                            p_ch = 0; /* This makes "prev_ch" get set to 0. */
                        } else if (p_ch == '[' && p[1] == ':') {
                            uchar_ptr s;
                            int i;
                            for (s = p += 2; (p_ch = p) && p_ch != ']'; p++) {} /*SHARED ITERATOR*/
                            if (!p_ch)
                                return WM_ABORT_ALL;
                            i = (int) (p - s - 1);
                            if (i < 0 || p[-1] != ':') {
                                /* Didn't find ":]", so treat like a normal set. */
                                p = s - 2;
                                p_ch = '[';
                                if (t_ch == p_ch)
                                    matched = 1;
                                continue;
                            }
                            if (CC_EQ(s,i, "alnum")) {
                                if (ISALNUM(t_ch))
                                    matched = 1;
                            } else if (CC_EQ(s,i, "alpha")) {
                                if (ISALPHA(t_ch))
                                    matched = 1;
                            } else if (CC_EQ(s,i, "blank")) {
                                if (ISBLANK(t_ch))
                                    matched = 1;
                            } else if (CC_EQ(s,i, "cntrl")) {
                                if (ISCNTRL(t_ch))
                                    matched = 1;
                            } else if (CC_EQ(s,i, "digit")) {
                                if (ISDIGIT(t_ch))
                                    matched = 1;
                            } else if (CC_EQ(s,i, "graph")) {
                                if (ISGRAPH(t_ch))
                                    matched = 1;
                            } else if (CC_EQ(s,i, "lower")) {
                                if (ISLOWER(t_ch))
                                    matched = 1;
                            } else if (CC_EQ(s,i, "print")) {
                                if (ISPRINT(t_ch))
                                    matched = 1;
                            } else if (CC_EQ(s,i, "punct")) {
                                if (ISPUNCT(t_ch))
                                    matched = 1;
                            } else if (CC_EQ(s,i, "space")) {
                                if (ISSPACE(t_ch))
                                    matched = 1;
                            } else if (CC_EQ(s,i, "upper")) {
                                if (ISUPPER(t_ch))
                                    matched = 1;
                                else if ((flags & WM_CASEFOLD) && ISLOWER(t_ch))
                                    matched = 1;
                            } else if (CC_EQ(s,i, "xdigit")) {
                                if (ISXDIGIT(t_ch))
                                    matched = 1;
                            } else /* malformed [:class:] string */
                                return WM_ABORT_ALL;
                            p_ch = 0; /* This makes "prev_ch" get set to 0. */
                        } else if (t_ch == p_ch)
                            matched = 1;
                        prev_ch = p_ch;
                    } while (/*prev_ch = p_ch,*/ (p_ch = ++p) != ']');
                    if (matched == negated ||
                        ((flags & WM_PATHNAME) && t_ch == '/'))
                        return WM_NOMATCH;
                    continue;
                }
            }

            return text ? WM_NOMATCH : WM_MATCH;
        }

        static bool ISPRINT(char c)  => (ISASCII(c) && isprint(c));
        static bool ISDIGIT(char c)  => (ISASCII(c) && isdigit(c));
        static bool ISALNUM(char c)  => (ISASCII(c) && isalnum(c));
        static bool ISALPHA(char c)  => (ISASCII(c) && isalpha(c));
        static bool ISCNTRL(char c)  => (ISASCII(c) && iscntrl(c));
        static bool ISLOWER(char c)  => (ISASCII(c) && islower(c));
        static bool ISPUNCT(char c)  => (ISASCII(c) && ispunct(c));
        static bool ISSPACE(char c)  => (ISASCII(c) && isspace(c));
        static bool ISUPPER(char c)  => (ISASCII(c) && isupper(c));
        static bool ISXDIGIT(char c) => (ISASCII(c) && isxdigit(c));

        static bool ISASCII(char c) => isascii(c);
        static bool ISBLANK(char c) => char.IsWhiteSpace(c);
        static bool ISGRAPH(char c) => ISPRINT(c) && c != 0x20;

        static bool sane_istest(@int x, byte mask) => ((sane_ctype[(byte)(x)] & (mask)) != 0);
        static bool isascii(char x) => (((x) & ~0x7f) == 0);
        static bool isspace(char x) => sane_istest(x, GIT_SPACE);
        static bool isdigit(char x) => sane_istest(x, GIT_DIGIT);
        static bool isalpha(char x) => sane_istest(x, GIT_ALPHA);
        static bool isalnum(char x) => sane_istest(x, GIT_ALPHA | GIT_DIGIT);
        static bool isprint(char x) => ((x) >= 0x20 && (x) <= 0x7e);
        static bool islower(char x) => sane_iscase(x, 1);
        static bool isupper(char x) => sane_iscase(x, 0);
        static bool is_glob_special(char x) => sane_istest(x, GIT_GLOB_SPECIAL);
        static bool iscntrl(char x) => (sane_istest(x, GIT_CNTRL));
        static bool ispunct(char x) => sane_istest(x, GIT_PUNCT | GIT_REGEX_SPECIAL
                                                    | GIT_GLOB_SPECIAL | GIT_PATHSPEC_MAGIC);
        static bool isxdigit(char x) => (hexval_table[(byte)(x)] != -1);
        static char tolower(char x) => sane_case((byte)(x), 0x20);
        static char toupper(char x) => sane_case((byte)(x), 0);

        static @int sane_case(@int x, int high)
        {
            if (sane_istest(x, GIT_ALPHA))
                x = (x & ~0x20) | high;
            return x;
        }

        static @int sane_iscase(int x, @int is_lower)
        {
            if (!sane_istest(x, GIT_ALPHA))
                return 0;

            if (is_lower)
                return (x & 0x20) != 0;
            else
                return (x & 0x20) == 0;
        }

        const byte GIT_SPACE = 0x01;
        const byte GIT_DIGIT = 0x02;
        const byte GIT_ALPHA = 0x04;
        const byte GIT_GLOB_SPECIAL = 0x08;
        const byte GIT_REGEX_SPECIAL = 0x10;
        const byte GIT_PATHSPEC_MAGIC = 0x20;
        const byte GIT_CNTRL = 0x40;
        const byte GIT_PUNCT = 0x80;

        const byte S = GIT_SPACE;
        const byte A = GIT_ALPHA;
        const byte D = GIT_DIGIT;
        const byte G = GIT_GLOB_SPECIAL;   /* *, ?, [, \\ */
        const byte R = GIT_REGEX_SPECIAL;  /* $, (, ), +, ., ^, {, | */
        const byte P = GIT_PATHSPEC_MAGIC; /* other non-alnum, except for ] and } */
        const byte X = GIT_CNTRL;
        const byte U = GIT_PUNCT;
        const byte Z = GIT_CNTRL | GIT_SPACE;

        static GitWildMatch()
        {
            Array.Resize(ref sane_ctype, 256);
        }

        static readonly byte[] sane_ctype = {
            X, X, X, X, X, X, X, X, X, Z, Z, X, X, Z, X, X,     /*   0.. 15 */
            X, X, X, X, X, X, X, X, X, X, X, X, X, X, X, X,     /*  16.. 31 */
            S, P, P, P, R, P, P, P, R, R, G, R, P, P, R, P,     /*  32.. 47 */
            D, D, D, D, D, D, D, D, D, D, P, P, P, P, P, G,     /*  48.. 63 */
            P, A, A, A, A, A, A, A, A, A, A, A, A, A, A, A,     /*  64.. 79 */
            A, A, A, A, A, A, A, A, A, A, A, G, G, U, R, P,     /*  80.. 95 */
            P, A, A, A, A, A, A, A, A, A, A, A, A, A, A, A,     /*  96..111 */
            A, A, A, A, A, A, A, A, A, A, A, R, R, U, P, X,     /* 112..127 */
            /* Nothing in the 128.. range */
        };

        static readonly sbyte[] hexval_table = new sbyte[256] {
             -1, -1, -1, -1, -1, -1, -1, -1,        /* 00-07 */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* 08-0f */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* 10-17 */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* 18-1f */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* 20-27 */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* 28-2f */
              0,  1,  2,  3,  4,  5,  6,  7,        /* 30-37 */
              8,  9, -1, -1, -1, -1, -1, -1,        /* 38-3f */
             -1, 10, 11, 12, 13, 14, 15, -1,        /* 40-47 */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* 48-4f */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* 50-57 */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* 58-5f */
             -1, 10, 11, 12, 13, 14, 15, -1,        /* 60-67 */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* 68-67 */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* 70-77 */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* 78-7f */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* 80-87 */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* 88-8f */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* 90-97 */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* 98-9f */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* a0-a7 */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* a8-af */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* b0-b7 */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* b8-bf */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* c0-c7 */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* c8-cf */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* d0-d7 */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* d8-df */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* e0-e7 */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* e8-ef */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* f0-f7 */
             -1, -1, -1, -1, -1, -1, -1, -1,        /* f8-ff */
        };
    }
}
