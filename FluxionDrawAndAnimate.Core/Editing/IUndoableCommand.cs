namespace FluxionDrawAndAnimate.Core.Editing;

public interface IUndoableCommand
{
    void Undo();
    void Redo();
}
