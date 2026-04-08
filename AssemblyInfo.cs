using System.Runtime.CompilerServices;

// Allows the test project to construct internal types (e.g. LoggerDisplaySession)
// and assert on internal implementation details without making them public.
[assembly: InternalsVisibleTo("DisplayLibrary.Tests")]
