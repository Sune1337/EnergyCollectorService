namespace EntsoeCollectorService.Utils;

public class EntsoeArea
{
    #region Fields

    public string Code;
    public string CountryCode;
    public string Description;

    #endregion
}

public static class EntsoeCodes
{
    #region Static Fields

    public static readonly List<EntsoeArea> Areas = new()
    {
        // 10Y1001A1001A44P IPA|SE1, BZN|SE1, MBA|SE1, SCA|SE1
        new EntsoeArea { Code = "10Y1001A1001A44P", Description = "SE1", CountryCode = "SE" },
        // 10Y1001A1001A45N SCA|SE2, MBA|SE2, BZN|SE2, IPA|SE2
        new EntsoeArea { Code = "10Y1001A1001A45N", Description = "SE2", CountryCode = "SE" },
        // 10Y1001A1001A46L IPA|SE3, BZN|SE3, MBA|SE3, SCA|SE3
        new EntsoeArea { Code = "10Y1001A1001A46L", Description = "SE3", CountryCode = "SE" },
        // 10Y1001A1001A47J SCA|SE4, MBA|SE4, BZN|SE4, IPA|SE4
        new EntsoeArea { Code = "10Y1001A1001A47J", Description = "SE4", CountryCode = "SE" },

        // Neighbours for power-transfer.
        new EntsoeArea { Code = "10YFI-1--------U", Description = "FI", CountryCode = "FI" },
        new EntsoeArea { Code = "10YNO-1--------2", Description = "NO1", CountryCode = "NO" },
        new EntsoeArea { Code = "10YNO-3--------J", Description = "NO3", CountryCode = "NO" },
        new EntsoeArea { Code = "10YNO-4--------9", Description = "NO4", CountryCode = "NO" },
        new EntsoeArea { Code = "10YDK-1--------W", Description = "DK1", CountryCode = "DK" },
        new EntsoeArea { Code = "10YDK-2--------M", Description = "DK2", CountryCode = "DK" },
        new EntsoeArea { Code = "10Y1001A1001A63L", Description = "DE_AT_LU", CountryCode = "DE" },
        new EntsoeArea { Code = "10Y1001A1001A82H", Description = "DE_LU", CountryCode = "DE" },
        new EntsoeArea { Code = "10YLT-1001A0008Q", Description = "LT", CountryCode = "LT" },
        new EntsoeArea { Code = "10YPL-AREA-----S", Description = "PL", CountryCode = "PL" }
    };

    public static readonly Dictionary<string, EntsoeArea> AreaCodeLookup = Areas.ToDictionary(a => a.Code, a => a);
    public static readonly Dictionary<string, EntsoeArea> AreaDescriptionLookup = Areas.ToDictionary(a => a.Description, a => a);


    public static readonly Dictionary<string, string> EnergyTypes = new()
    {
        // B01 Biomass
        { "B01", "Värmekraft" },
        // B02 Fossil Brown coal/Lignite
        { "B02", "Värmekraft" },
        // B03 Fossil Coal-derived gas
        { "B03", "Värmekraft" },
        // B04 Fossil Gas
        { "B04", "Värmekraft" },
        // B05 Fossil Hard coal
        { "B05", "Värmekraft" },
        // B06 Fossil Oil
        { "B06", "Värmekraft" },
        // B07 Fossil Oil shale
        { "B07", "Värmekraft" },
        // B08 Fossil Peat
        { "B08", "Värmekraft" },
        // B09 Geothermal
        { "B09", "Värmekraft" },
        // B10 Hydro Pumped Storage
        { "B10", "Vattenkraft" },
        // B11 Hydro Run-of-river and poundage
        { "B11", "Vattenkraft" },
        // B12 Hydro Water Reservoir
        { "B12", "Vattenkraft" },
        // B13 Marine
        { "B13", "Ospecificerat" },
        // B14 Nuclear
        { "B14", "Kärnkraft" },
        // B15 Other renewable
        { "B15", "Ospecificerat" },
        // B16 Solar
        { "B16", "Solkraft" },
        // B17 Waste
        { "B17", "Värmekraft" },
        // B18 Wind Offshore
        { "B18", "Vindkraft" },
        // B19 Wind Onshore
        { "B19", "Vindkraft" },
        // B20 Other
        { "B20", "Ospecificerat" },
        // B21 AC Link
        { "B21", "Ospecificerat" },
        // B22 DC Link
        { "B22", "Ospecificerat" },
        // B23 Substation
        { "B23", "Ospecificerat" },
        // B24 Transformer
        { "B24", "Ospecificerat" },
    };

    public static readonly Dictionary<string, string[]> TransferNeighbours = new()
    {
        { "SE1", new[] { "SE2", "FI", "NO4" } },
        { "SE2", new[] { "SE1", "SE3", "NO3", "NO4" } },
        { "SE3", new[] { "SE2", "SE4", "DK1", "FI", "NO1" } },
        { "SE4", new[] { "SE3", "DE_AT_LU", "DE_LU", "DK2", "LT", "PL" } }
    };

    #endregion
}
