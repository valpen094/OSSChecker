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

        string currentEcosystem = "NuGet"; // Default

        while (true)
        {
            Console.WriteLine($"\nCurrent Ecosystem: {currentEcosystem}");
            Console.WriteLine("Select action:");
            Console.WriteLine("[1] Search/Check Package");
            Console.WriteLine("[2] Switch Ecosystem");
            Console.WriteLine("[Q] Quit");
            Console.Write("> ");
            var action = Console.ReadLine()?.Trim().ToUpper();

            if (action == "Q") break;
            
            if (action == "2")
            {
                Console.WriteLine("\nSelect Ecosystem:");
                Console.WriteLine("[1] NuGet (.NET)");
                Console.WriteLine("[2] npm (JS/Node)");
                Console.WriteLine("[3] PyPI (Python)");
                Console.WriteLine("[4] Maven (Java)");
                Console.WriteLine("[5] Go (Golang)");
                Console.WriteLine("[6] crates.io (Rust)");
                Console.WriteLine("[7] RubyGems (Ruby)");
                Console.WriteLine("[8] Packagist (PHP)");
                Console.WriteLine("[9] Pub (Dart/Flutter)");
                Console.Write("> ");
                var ecoSelection = Console.ReadLine()?.Trim();
                switch (ecoSelection)
                {
                    case "1": currentEcosystem = "NuGet"; break;
                    case "2": currentEcosystem = "npm"; break;
                    case "3": currentEcosystem = "PyPI"; break;
                    case "4": currentEcosystem = "Maven"; break;
                    case "5": currentEcosystem = "Go"; break;
                    case "6": currentEcosystem = "crates.io"; break;
                    case "7": currentEcosystem = "RubyGems"; break;
                    case "8": currentEcosystem = "Packagist"; break;
                    case "9": currentEcosystem = "Pub"; break;
                    default: Console.WriteLine("Invalid selection, keeping current."); break;
                }
                continue;
            }

            if (action != "1" && !string.IsNullOrEmpty(action)) continue; 

            Console.WriteLine($"\nEnter the {currentEcosystem} package name to check:");
            if (currentEcosystem == "PyPI" || currentEcosystem == "Go") Console.WriteLine("(Note: Exact match required for validity check in this ecosystem)");
            
            Console.Write("> ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input)) continue;

            Console.WriteLine($"\nSearching for '{input}' in {currentEcosystem} ecosystem...");
            
            // 1. Search for package candidates
            var candidates = await searchService.SearchPackagesAsync(input, currentEcosystem);

            string targetPackage = "";

            if (candidates.Count == 0)
            {
                Console.WriteLine($"No candidates found in {currentEcosystem}. Using input '{input}' directly.");
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

            Console.WriteLine($"\nChecking vulnerabilities for: {targetPackage} ({currentEcosystem}) ...");
            
            // 2. Check vulnerabilities
            var vulnerabilities = await vulnService.CheckVulnerabilitiesAsync(targetPackage, currentEcosystem);

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
            
            // 4. Generate Report
            // User requirement: Create report if package exists (even if safe), skip only if search had no hits.
            if (candidates.Count > 0)
            {
                ReportGenerator.GenerateReport(targetPackage, vulnerabilities);
            }
            else
            {
                 Console.WriteLine($"\nPackage source not found (no search hits). Skipping report generation.");
            }
        }
        
        Console.WriteLine("Exiting...");
    }
}
