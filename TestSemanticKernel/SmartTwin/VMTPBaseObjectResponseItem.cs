using System;
using System.Collections.Generic;

namespace DigitalAssistant.SmartTwinMcp;

/// <summary>
/// Ответ на запрос получения <see cref="BaseObject"/>
/// </summary>
public record VMTPBaseObjectResponseItem
{
    /// <summary>
    /// Идентификатор BaseObject
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Имя BaseObject
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Фильтрация по ключу из внешней системы
    /// </summary>
    public string? ForeignSystemKey { get; set; }

    /// <summary>
    /// Идентификатор слоя
    /// </summary>
    public Guid LayerId { get; set; }

    /// <summary>
    /// Идентификатор типа
    /// </summary>
    public Guid TypeId { get; set; }

    /// <summary>
    /// Коллекция значений свойств
    /// </summary>
    public IReadOnlyCollection<BaseObjectPropertyValueItem> PropertyValues { get; set; } = Array.Empty<BaseObjectPropertyValueItem>();

    /// <summary>
    /// Идентификатор родительского объекта
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Признак удаленного объекта
    /// </summary>
    public bool Deleted { get; set; }

    /// <summary>
    /// Гео данные BaseObject
    /// </summary>
    //public string? GeoData { get; set; }
}
