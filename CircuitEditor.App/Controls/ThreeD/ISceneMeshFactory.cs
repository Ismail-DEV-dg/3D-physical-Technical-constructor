using System.Collections.Immutable;
using CircuitEditor.Core.Scene;

namespace CircuitEditor.App.Controls.ThreeD;

internal interface ISceneMeshFactory
{
    ImmutableArray<SceneMeshPart> Build(SceneNode node, CancellationToken cancellationToken);
}
