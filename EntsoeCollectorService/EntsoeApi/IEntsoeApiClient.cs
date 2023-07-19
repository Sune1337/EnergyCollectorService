namespace EntsoeCollectorService.EntsoeApi;

using Models.Generationload;
using Models.Publication;
using Refit;

public interface IEntsoeApiClient
{
    #region Public Methods and Operators

    [Get("/api?documentType=A75&processType=A16")]
    Task<GL_MarketDocument> ActualGenerationPerProductionType(string? securityToken, string in_Domain, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);

    [Get("/api?documentType=A65&processType=A16")]
    Task<GL_MarketDocument> ActualLoadPerProductionType(string? securityToken, string outBiddingZone_Domain, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);

    [Get("/api?documentType=A44")]
    Task<Publication_MarketDocument> DayAheadPrices(string? securityToken, string in_Domain, string out_Domain, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);

    #endregion
}