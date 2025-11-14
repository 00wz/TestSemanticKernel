namespace DigitalAssistant.SmartTwinMcp;

/// <summary>
/// Значение свойства <seealso cref="BaseObjectPropertyValue"/>
/// </summary>
public record BaseObjectPropertyValueItem
{
    /// <summary>
    /// Идентификатор свойства 
    /// </summary>
    public Guid PropertyId { get; set; }

    /// <summary>
    /// Значение свойства 
    /// </summary>
    public string PropertyValue { get; set; } = null!;

    /// <summary>
    /// Синоним свойства
    /// </summary>
    public string? PropertyAlias { get; set; }

    /// <summary>
    /// Синоним секции
    /// </summary>
    public string? SectionAlias { get; set; }

    /// <summary>
    /// Идентификатор секции
    /// </summary>
    public Guid? SectionId { get; set; }
}
