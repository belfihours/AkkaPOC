namespace WordCounter.Messages.Commands;

// counter inputs
public static class CounterCommands
{
    public sealed record CountTokens(IReadOnlyList<string> Tokens);

    // parser reached end of file
    public sealed record ExpectNoMoreTokens();
}