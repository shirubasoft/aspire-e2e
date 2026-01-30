namespace ProjectOrImage.Extensions;

public record ImageReference(string Image, string Tag = "latest", string? Registry = null);
