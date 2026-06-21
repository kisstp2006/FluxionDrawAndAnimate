using System;
using Avalonia;

namespace FluxionDrawAndAnimate.Controls.Canvas;

/// <summary>
/// Tracks touch gestures separately from drawing logic so two-finger navigation
/// cannot accidentally feed the one-finger stroke path.
/// </summary>
internal sealed class GestureStateMachine
{
    private readonly double _panDeadzone;
    private readonly double _zoomDeadzone;
    private readonly TouchPoint[] _touches = new TouchPoint[4];
    private int _touchCount;
    private double _startDistance;
    private Point _startCenter;
    private Point _anchorDocumentPoint;
    private double _startAngle;
    private ViewportSnapshot _startViewport;

    public GestureStateMachine(double panDeadzone, double zoomDeadzone)
    {
        _panDeadzone = panDeadzone;
        _zoomDeadzone = zoomDeadzone;
    }

    public bool IsGestureInProgress { get; private set; }

    public bool Press(
        int pointerId,
        Point position,
        ViewportState viewport,
        Size viewportSize,
        Size documentSize)
    {
        Remove(pointerId);
        if (_touchCount < _touches.Length)
        {
            _touches[_touchCount++] = new TouchPoint(pointerId, position);
        }

        if (_touchCount < 2)
        {
            return false;
        }

        BeginTwoFingerGesture(viewport, viewportSize, documentSize);
        return true;
    }

    public bool Move(
        int pointerId,
        Point position,
        ViewportState viewport,
        Size viewportSize,
        Size documentSize)
    {
        Update(pointerId, position);
        if (_touchCount < 2 || !IsGestureInProgress)
        {
            return false;
        }

        var first = _touches[0].Position;
        var second = _touches[1].Position;
        viewport.ApplyGesture(
            viewportSize,
            documentSize,
            _anchorDocumentPoint,
            _startCenter,
            Mid(first, second),
            _startDistance,
            Dist(first, second),
            _startAngle,
            Angle(first, second),
            _startViewport,
            _zoomDeadzone,
            _panDeadzone);
        return true;
    }

    public void Release(int pointerId)
    {
        Remove(pointerId);
        if (_touchCount < 2)
        {
            IsGestureInProgress = false;
            _startDistance = 0;
        }
        else
        {
            IsGestureInProgress = true;
        }
    }

    private void BeginTwoFingerGesture(ViewportState viewport, Size viewportSize, Size documentSize)
    {
        var first = _touches[0].Position;
        var second = _touches[1].Position;
        _startDistance = Dist(first, second);
        _startCenter = Mid(first, second);
        _startAngle = Angle(first, second);
        _startViewport = viewport.Capture();

        if (!viewport.TryScreenToDocumentUnclamped(viewportSize, documentSize, _startCenter, out _anchorDocumentPoint))
        {
            _anchorDocumentPoint = new Point(documentSize.Width / 2.0, documentSize.Height / 2.0);
        }

        IsGestureInProgress = true;
    }

    private void Update(int pointerId, Point position)
    {
        for (var i = 0; i < _touchCount; i++)
        {
            if (_touches[i].PointerId == pointerId)
            {
                _touches[i] = new TouchPoint(pointerId, position);
                return;
            }
        }

        if (_touchCount < _touches.Length)
        {
            _touches[_touchCount++] = new TouchPoint(pointerId, position);
        }
    }

    private void Remove(int pointerId)
    {
        for (var i = 0; i < _touchCount; i++)
        {
            if (_touches[i].PointerId != pointerId)
            {
                continue;
            }

            for (var j = i; j < _touchCount - 1; j++)
            {
                _touches[j] = _touches[j + 1];
            }

            _touchCount--;
            return;
        }
    }

    private static double Dist(Point a, Point b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static Point Mid(Point a, Point b) => new((a.X + b.X) / 2, (a.Y + b.Y) / 2);
    private static double Angle(Point a, Point b) => Math.Atan2(b.Y - a.Y, b.X - a.X);

    private readonly record struct TouchPoint(int PointerId, Point Position);
}
