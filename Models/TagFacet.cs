namespace AutomotiveInfo.Models;

/// <summary>A tag in use by published articles, with the number of articles carrying it.</summary>
public sealed record TagFacet(string Name, int Count);
