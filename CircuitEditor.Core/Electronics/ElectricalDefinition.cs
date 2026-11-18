namespace CircuitEditor.Core.Electronics;
/// <summary>Библиотечная электрическая модель и внутренние связи.</summary>
public sealed record ElectricalDefinition(string ComponentId, string Model, IReadOnlyList<ElectricalLink> Links,
    IReadOnlyList<PowerSourceDefinition> Sources, bool RequiresPower = false, IReadOnlyList<string>? RequiredPins = null,
    double? R25 = null, double? Beta = null, double? LightResistance = null, double? DarkResistance = null, IReadOnlyDictionary<string,string>? ChannelProfiles = null);
