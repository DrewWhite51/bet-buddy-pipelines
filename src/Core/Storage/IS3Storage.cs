using SportsBettingPipeline.Core.Models;

namespace SportsBettingPipeline.Core.Storage;

public interface IS3Storage
{
    Task<string> StoreOddsAsync(OddsData oddsData);
}
