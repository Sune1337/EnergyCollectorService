namespace EntsoeCollectorService.EntsoeApi.Models;

using System.Xml.Serialization;

[XmlRoot(Namespace = "urn:iec62325.351:tc57wg16:451-1:acknowledgementdocument:7:0", ElementName = "Acknowledgement_MarketDocument")]
public class Acknowledgement_MarketDocument
{
    #region Public Properties

    public Reason? Reason { get; set; }

    #endregion
}

public class Reason
{
    #region Public Properties

    public string? code { get; set; }
    public string? text { get; set; }

    #endregion
}
