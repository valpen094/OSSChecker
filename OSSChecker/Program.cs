using OSSChecker;

namespace OSSChecker;

class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("========================================");
        Console.WriteLine("       OSS Vulnerability Checker        ");
        Console.WriteLine("========================================");

        var vulnService = new VulnerabilityService();
        var searchService = new PackageSearchService();

        while (true)
        {
            Console.WriteLine("\nPlease enter the OSS name (or part of it) to check (ENTER to exit):");
            Console.Write("> ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
            {
                break;
            }

            Console.WriteLine($"\nSearching for '{input}' in NuGet ecosystem...");
            
            // 1. Search for package candidates
            var candidates = await searchService.SearchPackagesAsync(input);

            string targetPackage = "";

            if (candidates.Count == 0)
            {
                Console.WriteLine("No packages found. Trying to query OSV directly with the input name...");
                targetPackage = input;
            }
            else
            {
                Console.WriteLine($"Found {candidates.Count} candidate(s):");
                for (int i = 0; i < candidates.Count; i++)
                {
                    Console.WriteLine($"[{i + 1}] {candidates[i]}");
                }
                
                // Allow user to choose
                Console.WriteLine("\nSelect a number to check, or press ENTER to search using the first result default, or type a custom name:");
                Console.Write("> ");
                var selection = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(selection))
                {
                    targetPackage = candidates[0];
                }
                else if (int.TryParse(selection, out int index) && index > 0 && index <= candidates.Count)
                {
                    targetPackage = candidates[index - 1];
                }
                else
                {
                    targetPackage = selection;
                }
            }

            Console.WriteLine($"\nChecking vulnerabilities for: {targetPackage} ...");
            
            // 2. Check vulnerabilities
            var vulnerabilities = await vulnService.CheckVulnerabilitiesAsync(targetPackage);

            // 3. Display results
            if (vulnerabilities.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[WARNING] Found {vulnerabilities.Count} vulnerability record(s) for '{targetPackage}':");
                Console.ResetColor();

                foreach (var v in vulnerabilities)
                {
                    Console.WriteLine("--------------------------------------------------");
                    Console.WriteLine($"ID:      {v.Id}");
                    if (v.CveIds.Count > 0)
                    {
                        Console.WriteLine($"CVE:     {string.Join(", ", v.CveIds)}");
                    }
                    Console.WriteLine($"Summary: {v.Summary}");
                    Console.WriteLine($"URL:     {v.DetailsUrl}");
                    Console.WriteLine($"Affected Versions ({v.AffectedVersions.Count}):");
                    
                    // Display versions compactly in console
                    if (v.AffectedVersions.Count > 10)
                    {
                        var firstFew = string.Join(", ", v.AffectedVersions.Take(10));
                        Console.WriteLine($"  {firstFew} ... and {v.AffectedVersions.Count - 10} more (see report)");
                    }
                    else
                    {
                        Console.WriteLine($"  {string.Join(", ", v.AffectedVersions)}");
                    }
                }
                Console.WriteLine("--------------------------------------------------");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n[SAFE] No vulnerabilities reported for '{targetPackage}' in OSV database (Nuget ecosystem).");
                Console.ResetColor();
            }
            
            // 4. Generate Report (Always)
            ReportGenerator.GenerateReport(targetPackage, vulnerabilities);
        }
        
        Console.WriteLine("Exiting...");
    }
}
