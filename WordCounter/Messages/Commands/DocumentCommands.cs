namespace WordCounter.Messages.Commands;

public static class DocumentCommands
{
    public sealed record ProcessDocument(string RawText);
}