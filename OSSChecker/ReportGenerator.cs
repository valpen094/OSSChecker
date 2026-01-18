using System.Text;

namespace OSSChecker;

public class ReportGenerator
{
    public static void GenerateReport(string packageName, List<VulnerabilityRecord> vulnerabilities)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# OSS Vulnerability Report: {packageName}");
        sb.AppendLine($"**Generated on:** {DateTime.Now}");
        sb.AppendLine($"**Source:** [OSV.dev](https://osv.dev)");
        sb.AppendLine("");

        if (vulnerabilities.Count > 0)
        {
            sb.AppendLine($"Found {vulnerabilities.Count} vulnerability records.");
            sb.AppendLine("");

            // 1. Summary Table
            sb.AppendLine("## Summary Table");
            sb.AppendLine("");
            sb.AppendLine("| ID | CVE | Summary | URL |");
            sb.AppendLine("|---|---|---|---|");

            foreach (var vuln in vulnerabilities)
            {
                // Escape pipes in summary just in case
                string summary = vuln.Summary.Replace("|", "\\|");
                // Truncate summary for table readability
                string displaySummary = summary.Length > 60 ? summary.Substring(0, 57) + "..." : summary;
                
                string cveDisplay = vuln.CveIds.Count > 0 ? string.Join(", ", vuln.CveIds) : "-";

                sb.AppendLine($"| {vuln.Id} | {cveDisplay} | {displaySummary} | [Link]({vuln.DetailsUrl}) |");
            }

            sb.AppendLine("");
            sb.AppendLine("## Detailed Findings");
            sb.AppendLine("");

            // 2. Details with full version lists
            foreach (var vuln in vulnerabilities)
            {
                sb.AppendLine($"### {vuln.Id}");
                sb.AppendLine($"**Summary:** {vuln.Summary}");
                sb.AppendLine("");
                
                if (vuln.CveIds.Count > 0)
                {
                    sb.AppendLine($"**CVE:** {string.Join(", ", vuln.CveIds)}");
                    sb.AppendLine("");
                }

                sb.AppendLine($"**URL:** {vuln.DetailsUrl}");
                sb.AppendLine("");
                sb.AppendLine("**Affected Versions:**");
                
                if (vuln.AffectedVersions.Count > 0)
                {
                    sb.AppendLine("```");
                    
                    var versionStr = string.Join(", ", vuln.AffectedVersions);
                    int maxLineLength = 80;
                    int currentIdx = 0;
                    while (currentIdx < versionStr.Length)
                    {
                        var len = Math.Min(maxLineLength, versionStr.Length - currentIdx);
                        if (currentIdx + len < versionStr.Length) {
                            int lastComma = versionStr.LastIndexOf(',', currentIdx + len);
                            if (lastComma > currentIdx)
                            {
                                len = lastComma - currentIdx + 1;
                            }
                        }
                        
                        sb.AppendLine(versionStr.Substring(currentIdx, len).Trim());
                        currentIdx += len;
                    }
                    sb.AppendLine("```");
                }
                else
                {
                    sb.AppendLine("_No specific versions listed in affected range._");
                }
                sb.AppendLine("");
                sb.AppendLine("---");
                sb.AppendLine("");
            }
        }
        else
        {
            sb.AppendLine("## Status: SAFE");
            sb.AppendLine("No vulnerabilities found in the checked versions.");
        }

        var fileName = $"Report_{packageName}_{DateTime.Now:yyyyMMddHHmmss}.md";
        
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
