namespace EntsoeCollectorService.EntsoeApi;

using global::EntsoeCollectorService.EntsoeApi.Models;

using Refit;

public interface IEntsoeApiClient
{
    #region Public Methods and Operators

    [Get("/api?documentType=A75&processType=A16")]
    Task<GL_MarketDocument> ActualGenerationPerProductionType(string? securityToken, string in_Domain, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);

    #endregion
}
