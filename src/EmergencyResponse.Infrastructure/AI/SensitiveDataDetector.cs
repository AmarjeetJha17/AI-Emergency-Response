using System.Text.RegularExpressions;
using EmergencyResponse.Application.AISecurity;

namespace EmergencyResponse.Infrastructure.AI;

public sealed partial class SensitiveDataDetector : ISensitiveDataDetector
{
    public SensitiveDataScanResult Scan(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new SensitiveDataScanResult();
        }

        var signals = new List<string>();
        if (JwtRegex().IsMatch(text)) signals.Add("jwt");
        if (PrivateKeyRegex().IsMatch(text)) signals.Add("private-key");
        if (ConnectionStringRegex().IsMatch(text)) signals.Add("connection-string");
        if (SecretAssignmentRegex().IsMatch(text)) signals.Add("secret-assignment");
        if (AzureKeyRegex().IsMatch(text)) signals.Add("azure-key");
        if (ApiKeyRegex().IsMatch(text)) signals.Add("api-key");

        return new SensitiveDataScanResult
        {
            Detected = signals.Count > 0,
            Signals = signals
        };
    }

    [GeneratedRegex(@"\beyJ[a-zA-Z0-9_-]{10,}\.[a-zA-Z0-9_-]{10,}\.[a-zA-Z0-9_-]{10,}\b")]
    private static partial Regex JwtRegex();

    [GeneratedRegex(@"-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----")]
    private static partial Regex PrivateKeyRegex();

    [GeneratedRegex(@"(?i)(Server|Data Source|Host)=.+;.*(Password|Pwd)=")]
    private static partial Regex ConnectionStringRegex();

    [GeneratedRegex(@"(?i)\b(password|passwd|secret|token)\s*[:=]\s*['""][^'""]{8,}")]
    private static partial Regex SecretAssignmentRegex();

    [GeneratedRegex(@"(?i)\b(azure|ocp|sk)-[a-z0-9]{20,}\b")]
    private static partial Regex AzureKeyRegex();

    [GeneratedRegex(@"(?i)\b(api[_-]?key|access[_-]?token)\s*[:=]\s*[a-z0-9_\-]{16,}\b")]
    private static partial Regex ApiKeyRegex();
}
