using CircuitEditor.App.Interaction;
using CircuitEditor.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class BreadboardViewModel
{
    private const double NoteEditorRoom = 340;
    private const double NoteEditorHeight = 40;
    private const double WrappedNoteEditorHeight = 76;
    [ObservableProperty] private bool isNoteToolActive;
    private MockupNote? editedNote;
    private double noteX, noteY;
    [ObservableProperty] private string noteText = "";
    [ObservableProperty] private bool isEditingNote;
    [ObservableProperty] private bool canDeleteNote;
    [ObservableProperty] private double noteEditorX;
    [ObservableProperty] private double noteEditorY;

    [RelayCommand]
    private void BeginNote()
    {
        if (!Project.CanEdit || Project.InGesture) return;
        SelectTool("Select");
        CancelNote();
        IsNoteToolActive = true;
        Project.StatusMessage = "Щёлкните на макете, чтобы напечатать подпись.";
    }

    private bool RouteNote(CanvasInput input)
    {
        if (input.Action == CanvasAction.Cancel)
        {
            CancelNote();
            return false;
        }
        if (!Project.CanEdit || (input.Pan && !input.RightClick) || input.Action != CanvasAction.Down || Project.Wiring.IsWiring) return false;

        var hit = Project.Model.MockupNotes.LastOrDefault(note =>
            CursorX >= note.X - 1 && CursorX <= note.X + Math.Max(5, note.Text.Length * 1.45) + 1 &&
            CursorY >= note.Y - 1 && CursorY <= note.Y + 4);
        if (input.RightClick && hit is null || !IsNoteToolActive && hit is null) return false;
        if (Project.InGesture) return true;

        editedNote = hit;
        CanDeleteNote = hit is not null;
        noteX = hit?.X ?? CursorX;
        noteY = hit?.Y ?? CursorY;
        var height = ViewportWidth < NoteEditorRoom ? WrappedNoteEditorHeight : NoteEditorHeight;
        NoteEditorX = Math.Clamp(input.X, 0, Math.Max(0, ViewportWidth - NoteEditorRoom));
        NoteEditorY = Math.Clamp(input.Y, 0, Math.Max(0, ViewportHeight - height));
        NoteText = hit?.Text ?? "";
        IsNoteToolActive = false;
        IsEditingNote = true;
        return true;
    }

    [RelayCommand]
    private void SaveNote()
    {
        if (!IsEditingNote || !Project.CanEdit) return;
        var text = NoteText.Replace("\r", "").Replace("\n", " ").Trim();
        if (text.Length > MockupNote.MaximumLength) return;
        if (text.Length == 0 && editedNote is null) { CancelNote(); return; }
        CommitNote(text);
    }

    [RelayCommand]
    private void DeleteNote()
    {
        if (IsEditingNote && editedNote is not null && Project.CanEdit)
            CommitNote("");
    }

    private void CommitNote(string text)
    {
        if (!Project.BeginGesture("Подпись на макете")) return;
        var notes = Project.Model.MockupNotes.Where(note => !ReferenceEquals(note, editedNote)).ToList();
        if (text.Length != 0) notes.Add(new(noteX, noteY, text));
        Project.Model.SetMockupNotes(notes);
        Project.EndGesture();
        Project.Refresh();
        CancelNote();
    }

    [RelayCommand]
    private void CancelNote()
    {
        IsNoteToolActive = false;
        editedNote = null;
        CanDeleteNote = false;
        IsEditingNote = false;
    }
}
