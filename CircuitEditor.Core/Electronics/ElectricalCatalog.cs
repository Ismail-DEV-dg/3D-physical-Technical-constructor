namespace CircuitEditor.Core.Electronics;
/// <summary>Версионированные данные электрических моделей.</summary>
public sealed record ElectricalCatalog(int SchemaVersion, IReadOnlyList<ElectricalDefinition> Components, IReadOnlyList<LedProfile> LedProfiles)
{
    /// <summary>Электрическое описание типа, если он поддерживается.</summary>
    public ElectricalDefinition? Find(string id) => Components.FirstOrDefault(d => d.ComponentId == id);
    /// <summary>Ближайший цветовой профиль для пользовательского RGB-цвета LED.</summary>
    public LedProfile Led(string? color)
    {
        static (int R,int G,int B) Rgb(string s) => (Convert.ToInt32(s.Substring(1,2),16),Convert.ToInt32(s.Substring(3,2),16),Convert.ToInt32(s.Substring(5,2),16));
        var rgb = Rgb(color ?? LedProfiles[0].Color);
        return LedProfiles.MinBy(p => { var c=Rgb(p.Color); return Math.Pow(c.R-rgb.R,2)+Math.Pow(c.G-rgb.G,2)+Math.Pow(c.B-rgb.B,2); })!;
    }
}
