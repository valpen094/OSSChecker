using System.Text;

namespace OSSChecker;

public class ReportGenerator
{
    public static void GenerateReport(string packageName, List<string> vulnerableVersions)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"OSS Vulnerability Report");
        sb.AppendLine($"Generated on: {DateTime.Now}");
        sb.AppendLine("--------------------------------------------------");
        sb.AppendLine($"Package Name: {packageName}");
        sb.AppendLine("--------------------------------------------------");

        if (vulnerableVersions.Count > 0)
        {
            sb.AppendLine($"Vulnerabilities found in the following versions ({vulnerableVersions.Count}):");
            foreach (var version in vulnerableVersions)
            {
                sb.AppendLine($"- {version}");
            }
        }
        else
        {
            sb.AppendLine("No vulnerabilities found in the checked versions.");
        }

        sb.AppendLine("--------------------------------------------------");
        sb.AppendLine("Source: OSV.dev");

        var fileName = $"Report_{packageName}_{DateTime.Now:yyyyMMddHHmmss}.txt";
        
        try
        {
            File.WriteAllText(fileName, sb.ToString());
            Console.WriteLine($"\nReport generated: {Path.GetFullPath(fileName)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to write report file: {ex.Message}");
        }
    }
}
