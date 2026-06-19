using FluxionDrawAndAnimate.Core.Editing;
using Xunit;

namespace FluxionDrawAndAnimate.Core.Tests.Editing;

public class UndoStackTests
{
    private sealed class SpyCommand : IUndoableCommand
    {
        public int UndoCalls { get; private set; }
        public int RedoCalls { get; private set; }

        public void Undo() => UndoCalls++;
        public void Redo() => RedoCalls++;
    }

    [Fact]
    public void New_stack_cannot_undo_or_redo()
    {
        var stack = new UndoStack();

        Assert.False(stack.CanUndo);
        Assert.False(stack.CanRedo);
    }

    [Fact]
    public void Push_makes_undo_available_and_clears_redo()
    {
        var stack = new UndoStack();
        stack.Push(new SpyCommand());

        Assert.True(stack.CanUndo);
        Assert.False(stack.CanRedo);
    }

    [Fact]
    public void Undo_invokes_command_and_moves_it_to_redo_stack()
    {
        var stack = new UndoStack();
        var cmd = new SpyCommand();
        stack.Push(cmd);

        stack.Undo();

        Assert.Equal(1, cmd.UndoCalls);
        Assert.False(stack.CanUndo);
        Assert.True(stack.CanRedo);
    }

    [Fact]
    public void Redo_invokes_command_and_moves_it_back_to_undo_stack()
    {
        var stack = new UndoStack();
        var cmd = new SpyCommand();
        stack.Push(cmd);
        stack.Undo();

        stack.Redo();

        Assert.Equal(1, cmd.RedoCalls);
        Assert.True(stack.CanUndo);
        Assert.False(stack.CanRedo);
    }

    [Fact]
    public void Push_after_undo_clears_the_redo_stack()
    {
        var stack = new UndoStack();
        stack.Push(new SpyCommand());
        stack.Undo();

        // New action invalidates the redo history.
        stack.Push(new SpyCommand());

        Assert.False(stack.CanRedo);
    }

    [Fact]
    public void Undo_on_empty_stack_is_noop()
    {
        var stack = new UndoStack();

        stack.Undo(); // must not throw

        Assert.False(stack.CanUndo);
        Assert.False(stack.CanRedo);
    }

    [Fact]
    public void Redo_on_empty_stack_is_noop()
    {
        var stack = new UndoStack();

        stack.Redo(); // must not throw

        Assert.False(stack.CanRedo);
    }

    [Fact]
    public void Clear_empties_both_stacks()
    {
        var stack = new UndoStack();
        stack.Push(new SpyCommand());
        stack.Undo();

        stack.Clear();

        Assert.False(stack.CanUndo);
        Assert.False(stack.CanRedo);
    }

    [Fact]
    public void StateChanged_fires_on_push_undo_redo_and_clear()
    {
        var stack = new UndoStack();
        var fires = 0;
        stack.StateChanged += () => fires++;

        stack.Push(new SpyCommand());
        stack.Undo();
        stack.Redo();
        stack.Clear();

        Assert.Equal(4, fires);
    }

    [Fact]
    public void Depth_limit_drops_oldest_command_when_exceeded()
    {
        var stack = new UndoStack(maxDepth: 2);
        var oldest = new SpyCommand();
        stack.Push(oldest);
        stack.Push(new SpyCommand());
        stack.Push(new SpyCommand());

        // After 3 pushes with depth 2, the oldest should have been dropped.
        stack.Undo(); // pops most recent
        stack.Undo(); // pops second
        Assert.False(stack.CanUndo); // oldest is gone

        Assert.Equal(0, oldest.UndoCalls);
    }
}
