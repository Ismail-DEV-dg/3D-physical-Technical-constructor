using System.Collections.Immutable;
using CircuitEditor.Core.Breadboards;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Placement;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Core.Wiring;

namespace CircuitEditor.Core.Scene;

/// <summary>Строит сцену по физическим размещениям, посадкам и перемычкам проекта.</summary>
public sealed class SceneBuilder(ISceneCatalogRepository repository) : ISceneBuilder
{
    private readonly SceneCatalog catalog = Load(repository);
    private static readonly LayoutRect DefaultBody = new(0, 0, 6, 6);

    /// <inheritdoc/>
    public SceneModel Build(CircuitProject project, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var nodes = ImmutableArray.CreateBuilder<SceneNode>();
        Dictionary<WireEndpoint, ScenePoint> anchors = [];
        var bindings = project.Bindings.ToDictionary(item => (item.ComponentId, item.ContactId));
        var footprints = project.Footprints.ToDictionary(item => item.ComponentDefinitionId);
        foreach (var board in project.Breadboards)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var node = Board(board);
            nodes.Add(node);
            foreach (var hole in node.Contacts)
                anchors.Add(WireEndpoint.Hole(board.Id, hole.Id), node.Pose.World(hole.Position));
        }

        foreach (var component in project.Circuit.Components)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var footprint = footprints.GetValueOrDefault(component.Definition.Id);
            var placement = project.MockupLayout.Get(component.Id);
            var node = Part(component, footprint, placement, bindings, anchors);
            nodes.Add(node);
            foreach (var contact in node.Contacts)
                anchors.Add(WireEndpoint.Contact(node.Id, contact.Id), node.Pose.World(contact.Position));
        }

        foreach (var wire in project.Circuit.Wires)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!anchors.TryGetValue(wire.Start, out var start) || !anchors.TryGetValue(wire.End, out var end)) continue;
            var path = SceneWirePath.Create(wire, start, end, catalog.Wire);
            if (path.IsEmpty) continue;
            var bounds = SceneBounds.From(path);
            var radius = catalog.Wire.Radius;
            bounds = new(new(bounds.Min.X - radius, bounds.Min.Y - radius, bounds.Min.Z - radius),
                new(bounds.Max.X + radius, bounds.Max.Y + radius, bounds.Max.Z + radius));
            nodes.Add(new(wire.Id, SceneLayer.Wire, "wire", new(SceneShape.Tube, radius * 2, 0, wire.Color),
                default, default, bounds, [], path, ImmutableDictionary<string, LayoutRect>.Empty,
                Name: "Перемычка", Color: wire.Color, WireRadius: radius));
        }
        var result = nodes.ToImmutable();
        return new(result, SceneBounds.From(result.SelectMany(node => new[] { node.Bounds.Min, node.Bounds.Max })));
    }

    private SceneNode Board(BreadboardInstance board)
    {
        var profile = catalog.Breadboard;
        var pose = new ScenePose(new(board.X, board.Y, 0), board.Rotation);
        LayoutRect body = new(0, 0, board.Definition.Width, board.Definition.Height);
        var holes = board.Definition.Holes.Select(hole => new SceneContact(hole.Id,
            new(hole.Position.X, hole.Position.Y, Surface(profile)))).ToImmutableArray();
        return new(board.Id, SceneLayer.Breadboard, "breadboard", profile, pose, body,
            Bounds(pose, body, profile, holes), holes, [], ImmutableDictionary<string, LayoutRect>.Empty,
            Name: "Макетная плата");
    }

    private SceneNode Part(Component component, FootprintDefinition? footprint, ComponentPlacement at,
        IReadOnlyDictionary<(Guid, string), ContactBinding> bindings, IReadOnlyDictionary<WireEndpoint, ScenePoint> anchors)
    {
        var family = footprint?.Renderer ?? "unknown";
        var profile = catalog.Profiles.GetValueOrDefault(family, catalog.Fallback);
        var layer = family is "arduino-uno" or "arduino-nano" or "esp32-devkit" ? SceneLayer.Board : SceneLayer.Component;
        var contacts = footprint?.Contacts ?? [];
        ScenePoint? Hole(ContactDefinition contact) =>
            bindings.TryGetValue((component.Id, contact.Id), out var inserted) &&
            anchors.TryGetValue(WireEndpoint.Hole(inserted.BoardId, inserted.HoleId), out var position) ? position : null;
        var support = contacts.Select(Hole).OfType<ScenePoint>().Select(p => p.Z).DefaultIfEmpty(0).Max();
        var pose = new ScenePose(new(at.X, at.Y, support), at.Rotation);
        var body = footprint is null ? DefaultBody : ContactGeometry.Body(footprint, at, PhysicalSettings.FreeGridMm);
        var tips = contacts.Select(contact =>
        {
            if (Hole(contact) is { } hole)
            {
                var binding = bindings[(component.Id, contact.Id)];
                return new SceneContact(contact.Id, pose.Local(hole), binding.HoleId, binding.BoardId);
            }
            var local = ContactGeometry.Local(footprint!, contact, new ComponentPlacement(spanHoles: at.SpanHoles),
                PhysicalSettings.FreeGridMm);
            return new SceneContact(contact.Id, new(local.X, local.Y, profile.Shape == SceneShape.Board ? Surface(profile) : 0));
        }).ToImmutableArray();
        return new(component.Id, layer, family, profile, pose, body, Bounds(pose, body, profile, tips),
            tips, [], footprint?.Parts?.ToImmutableDictionary() ?? ImmutableDictionary<string, LayoutRect>.Empty,
            component.Reference, component.Definition.Name, component.Value, component.Color,
            family == "capacitor" ? component.CapacitorKind.ToString() : null, Tolerance: footprint?.TolerancePercent ?? 0,
            Knob: footprint?.RotaryControl is { } rotary ? new(rotary.X, rotary.Y) : null,
            KnobRadius: footprint?.RotaryControl?.Radius ?? 0, Angle: component.PotentiometerAngle,
            SwitchPosition: component.Inputs.Values.GetValueOrDefault(Inputs.ComponentInput.SwitchPosition));
    }

    private static double Surface(SceneProfile profile) => profile.Clearance + profile.Height;

    private static SceneBounds Bounds(ScenePose pose, LayoutRect body, SceneProfile profile, ImmutableArray<SceneContact> contacts)
    {
        var corners = from x in new[] { body.X, body.Right }
            from y in new[] { body.Y, body.Bottom }
            from z in new[] { profile.Clearance, Surface(profile) + profile.DetailHeight }
            select pose.World(new(x, y, z));
        return SceneBounds.From(corners.Concat(contacts.Select(point => pose.World(point.Position))));
    }

    private static SceneCatalog Load(ISceneCatalogRepository repository)
    {
        var result = repository.Load();
        return result.Value ?? throw new InvalidOperationException(result.Error ?? "Каталог 3D-корпусов недоступен.");
    }
}
