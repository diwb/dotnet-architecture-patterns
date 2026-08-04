namespace ArchitecturePatterns.Testing;

public sealed record TestCase(string Category, string Name, Func<Task> Body);

public static class Check
{
    public static void True(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{message}. Expected {expected}, got {actual}.");
    }

    public static async Task<int> Run(IEnumerable<TestCase> tests)
    {
        var passed = 0;
        var failed = 0;
        foreach (var test in tests)
        {
            try
            {
                await test.Body();
                passed++;
                Console.WriteLine($"PASS [{test.Category}] {test.Name}");
            }
            catch (Exception ex)
            {
                failed++;
                Console.Error.WriteLine($"FAIL [{test.Category}] {test.Name}: {ex.Message}");
            }
        }

        Console.WriteLine($"RESULT passed={passed} failed={failed} total={passed + failed}");
        return failed == 0 ? 0 : 1;
    }
}
