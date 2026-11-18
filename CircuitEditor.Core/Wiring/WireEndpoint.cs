namespace CircuitEditor.Core.Wiring;
/// <summary>Ссылка на физический контакт компонента либо отверстие платы.</summary>
public sealed record WireEndpoint(WireEndpointKind Kind, Guid OwnerId, string TerminalId)
{
    /// <summary>ID компонента для компонентного конца.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid ComponentId => Kind == WireEndpointKind.ComponentContact ? OwnerId : Guid.Empty;
    /// <summary>Создаёт ссылку на контакт.</summary>
    public static WireEndpoint Contact(Guid componentId, string contactId) => new(WireEndpointKind.ComponentContact, componentId, contactId);
    /// <summary>Создаёт ссылку на отверстие.</summary>
    public static WireEndpoint Hole(Guid boardId, string holeId) => new(WireEndpointKind.BreadboardHole, boardId, holeId);
}
