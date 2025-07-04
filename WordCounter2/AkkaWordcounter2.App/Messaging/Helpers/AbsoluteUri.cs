namespace AkkaWordcounter2.App.Messaging.Helpers;

/// <summary>
/// Value type for enforcing absolute uris
/// </summary>
public record struct AbsoluteUri
{
    public AbsoluteUri(Uri value)
    {
        Value = value;
        if (!value.IsAbsoluteUri)
            throw new ArgumentException("Value must be an absolute URL", nameof(value));
    }

    public Uri Value { get; }

    public override string ToString() => Value.ToString();
}