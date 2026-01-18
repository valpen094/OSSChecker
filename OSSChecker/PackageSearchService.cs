using System.Threading;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace OSSChecker;

public class PackageSearchService
{
    public async Task<List<string>> SearchPackagesAsync(string searchTerm)
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
                take: 5, // Suggest top 5 matches
                logger,
                cancellationToken);

            foreach (var package in packages)
            {
                results.Add(package.Identity.Id);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching packages: {ex.Message}");
        }

        return results;
    }
}
