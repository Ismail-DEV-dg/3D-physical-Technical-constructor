using System.Windows.Media.Media3D;
using CircuitEditor.Core.Scene;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.App.Controls.ThreeD;

internal sealed class SceneReactionBuilder
{
    private readonly Dictionary<(Guid Id, string Role), (SceneMeshPart Part, string Key, SceneReaction Reaction)> cache = [];

    public IReadOnlyList<SceneReaction> Build(IReadOnlyList<PreparedSceneNode> nodes, SimulationState state,
        bool showVoltage, CancellationToken token)
    {
        List<SceneReaction> result = [];
        foreach (var prepared in nodes)
        {
            token.ThrowIfCancellationRequested();
            var node = prepared.Node;
            var signal = state.Components.GetValueOrDefault(node.Id);
            foreach (var part in prepared.Parts)
            {
                SceneReaction? reaction = part.Role switch
                {
                    "lens" => Light(node, part, signal?.Intensity ?? 0, part.MaterialKey),
                    "board-led" => Light(node, part, signal?.Intensity ?? 0, "#FFD769"),
                    "buzzer" => Light(node, part, signal?.Active == true ? .65 : 0, "#FFC448"),
                    "rgb-r" => Light(node, part, Channel(signal, 0), "#FF391F"),
                    "rgb-g" => Light(node, part, Channel(signal, 1), "#42EF28"),
                    "rgb-b" => Light(node, part, Channel(signal, 2), "#2DA2FF"),
                    "wire" => Wire(node, part, showVoltage ? state.Wires.GetValueOrDefault(node.Id)?.Voltage : null),
                    "screen" => Screen(node, part, signal),
                    "servo" => Rotate(node, part, signal?.Angle ?? 0),
                    "knob" => Rotate(node, part, signal?.Angle ?? node.Angle),
                    "switch" => Translate(node, part,
                        signal?.Measurements?.GetValueOrDefault("SwitchPosition") ?? node.SwitchPosition,
                        part.Pivot.X, 0),
                    "button" => Translate(node, part, signal?.Active == true ? 1 : 0, 0, -.8),
                    _ when part.Role.StartsWith("segment-", StringComparison.Ordinal) =>
                        Light(node, part, (((signal?.Segments ?? 0) >> (part.Role[^1] - '0')) & 1), "#FF3445"),
                    _ => null
                };
                if (reaction is not null) result.Add(reaction);
            }
        }
        return result;
    }

    private static double Channel(ComponentSignal? signal, int index) =>
        signal is { Channels.IsDefaultOrEmpty: false } && signal.Channels.Length > index ? signal.Channels[index] : 0;

    private SceneReaction Light(SceneNode n, SceneMeshPart p, double intensity, string color)
    {
        var level = SceneSignalStyle.Brightness(intensity);
        return Get(n, p, level.ToString(), () => level == 0 ? p.Model :
            SceneMeshes.Model(p.Model.Geometry, SceneMaterials.Glow(p.MaterialKey, color, level, p.Role == "lens" || p.Role.StartsWith("rgb-", StringComparison.Ordinal)
                ? SceneFinish.Gloss : SceneFinish.Matte)));
    }

    private SceneReaction Wire(SceneNode n, SceneMeshPart p, double? voltage)
    {
        var color = voltage is double v ? SceneSignalStyle.VoltageColor(v) : p.MaterialKey;
        return Get(n, p, color, () => color == p.MaterialKey ? p.Model :
            SceneMeshes.Model(p.Model.Geometry, SceneMaterials.Solid(color)));
    }

    private SceneReaction Screen(SceneNode n, SceneMeshPart p, ComponentSignal? state)
    {
        var label = state?.Active == true ? state.Text ?? "" : null;
        return Get(n, p, label is null ? "off" : "on:" + label, () =>
        {
            if (label is null) return p.Model;
            var bounds = p.Model.Geometry.Bounds;
            var background = n.Family == "oled-ssd1306" ? "#071721" : "#244BA9";
            var material = SceneMaterials.Texture(label, "#D5FEFF", background, bounds.SizeX / bounds.SizeY);
            return SceneMeshes.Model(p.Model.Geometry, material);
        });
    }

    private SceneReaction Rotate(SceneNode n, SceneMeshPart p, double angle) =>
        Get(n, p, Math.Round(angle, 1).ToString(System.Globalization.CultureInfo.InvariantCulture), () => p.Model,
            () => new RotateTransform3D(new AxisAngleRotation3D(new(0, 0, 1), -Math.Round(angle, 1)), p.Pivot));

    private SceneReaction Translate(SceneNode n, SceneMeshPart p, double value, double x, double z) =>
        Get(n, p, value > 0 ? "1" : "0", () => p.Model,
            () => new TranslateTransform3D(x * (value > 0 ? 1 : -1), 0, value > 0 ? z : 0));

    private SceneReaction Get(SceneNode node, SceneMeshPart part, string value, Func<GeometryModel3D> model,
        Func<Transform3D>? transform = null)
    {
        var key = (node.Id, part.Role);
        if (cache.TryGetValue(key, out var old) && ReferenceEquals(old.Part, part) && old.Key == value)
            return old.Reaction;
        var pose = transform?.Invoke();
        pose?.Freeze();
        var result = new SceneReaction(node.Id, part.Role, model(), pose);
        cache[key] = (part, value, result);
        return result;
    }
}
