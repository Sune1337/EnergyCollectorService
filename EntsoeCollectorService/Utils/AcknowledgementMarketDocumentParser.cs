namespace EntsoeCollectorService.Utils;

using System.Xml.Serialization;

using global::EntsoeCollectorService.EntsoeApi.Models;

public static class AcknowledgementMarketDocumentParser
{
    #region Public Methods and Operators

    public static Acknowledgement_MarketDocument? ParseXml(string? xml)
    {
        if (xml == null)
        {
            return null;
        }

        try
        {
            var xmlSerializer = new XmlSerializer(typeof(Acknowledgement_MarketDocument));

            using var stringReader = new StringReader(xml);
            return xmlSerializer.Deserialize(stringReader) as Acknowledgement_MarketDocument;
        }

        catch
        {
            return null;
        }
    }

    #endregion
}
