using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Registry configuration
/// </summary>
/// <param name="Uri"></param>
/// <param name="Username"></param>
/// <param name="Password"></param>
public record RegistryConfiguration(string Name, [StringSyntax(StringSyntaxAttribute.Uri)]string Uri, string Username, string Password);