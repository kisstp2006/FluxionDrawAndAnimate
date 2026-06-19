namespace FluxionDrawAndAnimate.Core.Editing;

public sealed class UndoStack
{
    private readonly Stack<IUndoableCommand> _undoStack = new();
    private readonly Stack<IUndoableCommand> _redoStack = new();
    private readonly int _maxDepth;

    public UndoStack(int maxDepth = 100)
    {
        _maxDepth = maxDepth;
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>Raised after any push, undo, redo, or clear.</summary>
    public event Action? StateChanged;

    public void Push(IUndoableCommand command)
    {
        _undoStack.Push(command);
        _redoStack.Clear();

        // Enforce depth limit
        if (_undoStack.Count > _maxDepth)
        {
            var temp = _undoStack.ToArray();
            _undoStack.Clear();
            for (var i = _maxDepth - 1; i >= 0; i--)
            {
                _undoStack.Push(temp[i]);
            }
        }

        StateChanged?.Invoke();
    }

    public void Undo()
    {
        if (!CanUndo)
        {
            return;
        }

        var command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);
        StateChanged?.Invoke();
    }

    public void Redo()
    {
        if (!CanRedo)
        {
            return;
        }

        var command = _redoStack.Pop();
        command.Redo();
        _undoStack.Push(command);
        StateChanged?.Invoke();
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke();
    }
}
