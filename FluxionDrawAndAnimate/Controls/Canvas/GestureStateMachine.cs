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
    private double _lastDistance;
    private Point _startCenter;
    private Point _lastCenter;
    private double _startAngle;
    private double _lastAngle;
    private bool _hasPanStarted;
    private bool _hasZoomStarted;

    public GestureStateMachine(double panDeadzone, double zoomDeadzone)
    {
        _panDeadzone = panDeadzone;
        _zoomDeadzone = zoomDeadzone;
    }

    public bool IsGestureInProgress { get; private set; }

    public bool Press(int pointerId, Point position)
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

        BeginTwoFingerGesture();
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
        var currentCenter = Mid(first, second);
        var currentDistance = Dist(first, second);
        var currentAngle = Angle(first, second);

        ApplyTwoFingerPan(viewport, currentCenter);
        ApplyPinchZoom(viewport, viewportSize, currentCenter, currentDistance);
        ApplyTwoFingerRotation(viewport, viewportSize, documentSize, currentCenter, currentAngle);

        return true;
    }

    public void Release(int pointerId)
    {
        Remove(pointerId);
        if (_touchCount < 2)
        {
            IsGestureInProgress = false;
            ResetGestureState();
        }
        else
        {
            IsGestureInProgress = true;
        }
    }

    private void BeginTwoFingerGesture()
    {
        var first = _touches[0].Position;
        var second = _touches[1].Position;
        _startDistance = Dist(first, second);
        _lastDistance = _startDistance;
        _startCenter = Mid(first, second);
        _lastCenter = _startCenter;
        _startAngle = Angle(first, second);
        _lastAngle = _startAngle;
        _hasPanStarted = false;
        _hasZoomStarted = false;

        IsGestureInProgress = true;
    }

    private void ApplyTwoFingerPan(ViewportState viewport, Point currentCenter)
    {
        var centerDeltaX = currentCenter.X - _startCenter.X;
        var centerDeltaY = currentCenter.Y - _startCenter.Y;
        var centerDrift = Math.Sqrt(centerDeltaX * centerDeltaX + centerDeltaY * centerDeltaY);
        if (!_hasPanStarted && centerDrift <= _panDeadzone)
        {
            _lastCenter = currentCenter;
            return;
        }

        _hasPanStarted = true;
        viewport.PanBy(currentCenter.X - _lastCenter.X, currentCenter.Y - _lastCenter.Y);
        _lastCenter = currentCenter;
    }

    private void ApplyPinchZoom(ViewportState viewport, Size viewportSize, Point currentCenter, double currentDistance)
    {
        var distanceDelta = Math.Abs(currentDistance - _startDistance);
        if (!_hasZoomStarted && distanceDelta <= _zoomDeadzone)
        {
            _lastDistance = currentDistance;
            return;
        }

        _hasZoomStarted = true;
        if (_lastDistance > 0)
        {
            viewport.ZoomAt(viewportSize, currentCenter, currentDistance / _lastDistance);
        }

        _lastDistance = currentDistance;
    }

    private void ApplyTwoFingerRotation(
        ViewportState viewport,
        Size viewportSize,
        Size documentSize,
        Point currentCenter,
        double currentAngle)
    {
        viewport.RotateAt(viewportSize, documentSize, currentCenter, currentAngle - _lastAngle);
        _lastAngle = currentAngle;
    }

    private void ResetGestureState()
    {
        _startDistance = 0;
        _lastDistance = 0;
        _startCenter = default;
        _lastCenter = default;
        _startAngle = 0;
        _lastAngle = 0;
        _hasPanStarted = false;
        _hasZoomStarted = false;
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
