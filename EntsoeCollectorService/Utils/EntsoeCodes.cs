namespace EntsoeCollectorService.Utils;

public static class EntsoeCodes
{
    #region Static Fields

    public static readonly Dictionary<string, string> Areas = new()
    {
        // 10Y1001A1001A44P IPA|SE1, BZN|SE1, MBA|SE1, SCA|SE1
        { "10Y1001A1001A44P", "SE1" },
        // 10Y1001A1001A45N SCA|SE2, MBA|SE2, BZN|SE2, IPA|SE2
        { "10Y1001A1001A45N", "SE2" },
        // 10Y1001A1001A46L IPA|SE3, BZN|SE3, MBA|SE3, SCA|SE3
        { "10Y1001A1001A46L", "SE3" },
        // 10Y1001A1001A47J SCA|SE4, MBA|SE4, BZN|SE4, IPA|SE4
        { "10Y1001A1001A47J", "SE4" }
    };

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

    #endregion
}
