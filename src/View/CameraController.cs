using Godot;

namespace VastDark.View;

public partial class CameraController : Camera2D
{
    [Export] public float PanSpeed = 600.0f;
    [Export] public float ZoomSpeed = 0.15f;
    [Export] public float MinZoom = 0.3f;
    [Export] public float MaxZoom = 5.0f;
    [Export] public float LerpSpeed = 12.0f;

    private Vector2 _targetPosition;
    private float _targetZoom = 1.0f;
    private bool _isDragging = false;
    private Vector2 _dragStart = Vector2.Zero;
    private Rect2 _limitRect = new Rect2(-5000, -5000, 10000, 10000);

    public override void _Ready()
    {
        _targetPosition = Position;
        _targetZoom = Zoom.X;
    }

    public override void _Process(double delta)
    {
        float fDelta = (float)delta;

        // 1. Keyboard Panning
        Vector2 inputDir = Vector2.Zero;
        if (Input.IsActionPressed("ui_left") || Input.IsKeyPressed(Key.A)) inputDir.X -= 1;
        if (Input.IsActionPressed("ui_right") || Input.IsKeyPressed(Key.D)) inputDir.X += 1;
        if (Input.IsActionPressed("ui_up") || Input.IsKeyPressed(Key.W)) inputDir.Y -= 1;
        if (Input.IsActionPressed("ui_down") || Input.IsKeyPressed(Key.S)) inputDir.Y += 1;

        if (inputDir != Vector2.Zero && !_isDragging)
        {
            // Panning speed scales inversely with zoom (pan faster when zoomed out)
            float speedMultiplier = 1.0f / Mathf.Max(Zoom.X, 0.1f);
            _targetPosition += inputDir.Normalized() * PanSpeed * speedMultiplier * fDelta;
        }

        // Clamp target position
        _targetPosition.X = Mathf.Clamp(_targetPosition.X, _limitRect.Position.X, _limitRect.End.X);
        _targetPosition.Y = Mathf.Clamp(_targetPosition.Y, _limitRect.Position.Y, _limitRect.End.Y);

        // 2. Smooth Interpolation
        Position = Position.Lerp(_targetPosition, LerpSpeed * fDelta);
        float currentZoomVal = Mathf.Lerp(Zoom.X, _targetZoom, LerpSpeed * fDelta);
        Zoom = new Vector2(currentZoomVal, currentZoomVal);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // 3. Mouse Drag-to-Pan (Right Mouse Button or Middle Mouse Button)
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Right || mouseButton.ButtonIndex == MouseButton.Middle)
            {
                if (mouseButton.Pressed)
                {
                    _isDragging = true;
                    _dragStart = GetLocalMousePosition();
                }
                else
                {
                    _isDragging = false;
                }
            }

            // 4. Zooming via Scroll Wheel
            if (mouseButton.Pressed)
            {
                if (mouseButton.ButtonIndex == MouseButton.WheelUp)
                {
                    AdjustZoom(1.0f + ZoomSpeed);
                }
                else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
                {
                    AdjustZoom(1.0f - ZoomSpeed);
                }
            }
        }
        else if (@event is InputEventMouseMotion mouseMotion && _isDragging)
        {
            // Drag moves camera target in opposite direction
            Vector2 dragDelta = _dragStart - GetLocalMousePosition();
            _targetPosition += dragDelta;
        }
    }

    private void AdjustZoom(float factor)
    {
        _targetZoom = Mathf.Clamp(_targetZoom * factor, MinZoom, MaxZoom);
    }

    public void CenterOn(Vector2 position, float zoomVal)
    {
        _targetPosition = position;
        _targetZoom = zoomVal;
        
        // Force snap immediately if camera is far away to avoid long travel times
        if (Position.DistanceTo(position) > 2000)
        {
            Position = position;
            Zoom = new Vector2(zoomVal, zoomVal);
        }
    }

    public void SetLimits(Rect2 limitRect)
    {
        _limitRect = limitRect;
    }
}
