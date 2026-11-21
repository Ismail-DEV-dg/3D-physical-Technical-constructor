namespace CircuitEditor.Core.Scene;

/// <summary>Положение локального начала и поворот в плоскости «Макета».</summary>
public readonly record struct ScenePose(ScenePoint Origin, int Rotation)
{
    /// <summary>Переводит локальную точку в координаты стола.</summary>
    public ScenePoint World(ScenePoint local)
    {
        var angle = Rotation * Math.PI / 180;
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);
        return new(Origin.X + local.X * cos - local.Y * sin,
            Origin.Y + local.X * sin + local.Y * cos, Origin.Z + local.Z);
    }

    /// <summary>Переводит точку стола в локальные координаты корпуса.</summary>
    public ScenePoint Local(ScenePoint world)
    {
        var delta = new ScenePoint(world.X - Origin.X, world.Y - Origin.Y, world.Z - Origin.Z);
        return new ScenePose(default, -Rotation).World(delta);
    }
}
