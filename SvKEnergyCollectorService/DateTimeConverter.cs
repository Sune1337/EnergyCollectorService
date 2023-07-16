namespace SvKEnergyCollectorService;

using System.Text.Json;
using System.Text.Json.Serialization;

public class DateTimeConverter : JsonConverter<DateTime>
{
    #region Static Fields

    private static readonly DateTime Epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    #endregion

    #region Public Methods and Operators

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Epoch.AddMilliseconds(reader.GetDouble());
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue((value - Epoch).TotalMilliseconds);
    }

    #endregion
}
