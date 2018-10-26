namespace WildWildMatch.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public partial class Tests
    {
        // ReSharper disable InconsistentNaming UnusedMember.Local

        const int WM_ABORT_MALFORMED = 2;
        const int WM_NOMATCH = 1;
        const int WM_MATCH = 0;
        const int WM_ABORT_ALL = -1;
        const int WM_ABORT_TO_STARSTAR = -2;

        // ReSharper restore InconsistentNaming UnusedMember.Local

        [TestCaseSource(nameof(WmExeTestData))]
        public void WmExe(int expected, string pattern, string text, int sourceLineNumber)
        {
            var success = WildMatch.IsMatch(pattern, text, WildMatchFlags.PathName
                                                         | WildMatchFlags.CaseFold,
                                            out var matched);

            const string message = "{0}; source line #{1}";

            switch (expected)
            {
                case WM_MATCH:
                    Assert.True(success, message, nameof(success), sourceLineNumber);
                    Assert.True(matched, message, nameof(matched), sourceLineNumber);
                    break;
                case WM_NOMATCH:
                    Assert.True(success, message, nameof(success), sourceLineNumber);
                    Assert.False(matched, message, nameof(matched), sourceLineNumber);
                    break;
                default:
                    Assert.False(success, message, nameof(success), sourceLineNumber);
                    break;
            }
        }
    }
}
