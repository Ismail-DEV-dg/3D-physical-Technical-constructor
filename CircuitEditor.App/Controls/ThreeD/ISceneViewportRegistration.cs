namespace CircuitEditor.App.Controls.ThreeD;

internal interface ISceneViewportRegistration
{
    void Attach(SceneViewport viewport);
    void Detach(SceneViewport viewport);
}
