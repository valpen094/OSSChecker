using System.Text.Json;
using System.Threading;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace OSSChecker;

public class PackageSearchService
{
    private readonly HttpClient _httpClient;

    public PackageSearchService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "OSSChecker/1.0");
    }

    public async Task<List<string>> SearchPackagesAsync(string searchTerm, string ecosystem)
    {
        return ecosystem.ToLower() switch
        {
            "nuget" => await SearchNuGetAsync(searchTerm),
            "npm" => await SearchNpmAsync(searchTerm),
            "pypi" => await SearchPyPiAsync(searchTerm),
            "maven" => await SearchMavenAsync(searchTerm),
            "go" => await SearchGoAsync(searchTerm),
            "crates.io" => await SearchCratesAsync(searchTerm),
            "rubygems" => await SearchRubyGemsAsync(searchTerm),
            "packagist" => await SearchPackagistAsync(searchTerm),
            "pub" => await SearchPubAsync(searchTerm),
            _ => new List<string>()
        };
    }

    private async Task<List<string>> SearchNuGetAsync(string searchTerm)
    {
        var results = new List<string>();
        try
        {
            var logger = NullLogger.Instance;
            var cancellationToken = CancellationToken.None;
            var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            var resource = await repository.GetResourceAsync<PackageSearchResource>();
            var searchFilter = new SearchFilter(includePrerelease: false);

            var packages = await resource.SearchAsync(
                searchTerm,
                searchFilter,
                skip: 0,
                take: 5,
                logger,
                cancellationToken);

            foreach (var package in packages)
            {
                results.Add(package.Identity.Id);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching NuGet: {ex.Message}");
        }
        return results;
    }

    private async Task<List<string>> SearchNpmAsync(string searchTerm)
    {
        var results = new List<string>();
        try
        {
            var url = $"https://registry.npmjs.org/-/v1/search?text={Uri.EscapeDataString(searchTerm)}&size=5";
            var json = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("objects", out var objects))
            {
                foreach (var obj in objects.EnumerateArray())
                {
                    if (obj.TryGetProperty("package", out var pkg) && pkg.TryGetProperty("name", out var name))
                    {
                        results.Add(name.GetString() ?? "");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching npm: {ex.Message}");
        }
        return results;
    }

    private async Task<List<string>> SearchPyPiAsync(string searchTerm)
    {
        var results = new List<string>();
        try
        {
            var url = $"https://pypi.org/pypi/{searchTerm}/json";
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                results.Add(searchTerm);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking PyPI: {ex.Message}");
        }
        return results;
    }

    private async Task<List<string>> SearchMavenAsync(string searchTerm)
    {
        var results = new List<string>();
        try
        {
            // Maven Central Search
            var url = $"https://search.maven.org/solrsearch/select?q={Uri.EscapeDataString(searchTerm)}&rows=5&wt=json";
            var json = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("response", out var response) && 
                response.TryGetProperty("docs", out var docs))
            {
                foreach (var docObj in docs.EnumerateArray())
                {
                    var id = docObj.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                    if (!string.IsNullOrEmpty(id)) results.Add(id);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching Maven: {ex.Message}");
        }
        return results;
    }

    private async Task<List<string>> SearchGoAsync(string searchTerm)
    {
        var results = new List<string>();
        try
        {
            // Go Proxy Check (Exact match mostly, or try standard module paths)
            // https://proxy.golang.org/<module>/@v/list
            var url = $"https://proxy.golang.org/{searchTerm}/@v/list";
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                results.Add(searchTerm);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking Go Proxy: {ex.Message}");
        }
        return results;
    }

    private async Task<List<string>> SearchCratesAsync(string searchTerm)
    {
        var results = new List<string>();
        try
        {
            var url = $"https://crates.io/api/v1/crates?q={Uri.EscapeDataString(searchTerm)}&per_page=5";
            var json = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("crates", out var crates))
            {
                foreach (var crate in crates.EnumerateArray())
                {
                    if (crate.TryGetProperty("id", out var id))
                    {
                        results.Add(id.GetString() ?? "");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching crates.io: {ex.Message}");
        }
        return results;
    }

    private async Task<List<string>> SearchRubyGemsAsync(string searchTerm)
    {
        var results = new List<string>();
        try
        {
            var url = $"https://rubygems.org/api/v1/search.json?query={Uri.EscapeDataString(searchTerm)}";
            var json = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                int count = 0;
                foreach (var gem in doc.RootElement.EnumerateArray())
                {
                    if (count++ >= 5) break;
                    if (gem.TryGetProperty("name", out var name))
                    {
                        results.Add(name.GetString() ?? "");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching RubyGems: {ex.Message}");
        }
        return results;
    }

    private async Task<List<string>> SearchPackagistAsync(string searchTerm)
    {
        var results = new List<string>();
        try
        {
            var url = $"https://packagist.org/search.json?q={Uri.EscapeDataString(searchTerm)}&per_page=5";
            var json = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("results", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("name", out var name))
                    {
                        results.Add(name.GetString() ?? "");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching Packagist: {ex.Message}");
        }
        return results;
    }

    private async Task<List<string>> SearchPubAsync(string searchTerm)
    {
        var results = new List<string>();
        try
        {
            var url = $"https://pub.dev/api/search?q={Uri.EscapeDataString(searchTerm)}";
            var json = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("packages", out var packages))
            {
                int count = 0;
                foreach (var item in packages.EnumerateArray())
                {
                    if (count++ >= 5) break;
                    if (item.TryGetProperty("package", out var name))
                    {
                        results.Add(name.GetString() ?? "");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching Pub: {ex.Message}");
        }
        return results;
    }
}
