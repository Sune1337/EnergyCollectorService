namespace EnergyCollectorService.CurrencyConversion.RiksbankenApi.Serialization;

using System.Reflection;

public class UrlParameterFormatter : Refit.DefaultUrlParameterFormatter
{
    #region Public Methods and Operators

    public override string? Format(object? parameterValue, ICustomAttributeProvider attributeProvider, Type type)
    {
        if (typeof(DateTime?).IsAssignableFrom(type) && parameterValue != null)
        {
            return ((DateTime)parameterValue).ToString("yyyy-MM-dd");
        }

        return base.Format(parameterValue, attributeProvider, type);
    }

    #endregion
}
