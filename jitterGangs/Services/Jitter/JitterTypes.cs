namespace JitterGang.Services.Jitter;

public class LeftRightJitter : BaseJitter
{
    private readonly (int x, int y)[] _points;
    private int _currentPoint = 0;

    public LeftRightJitter(int strength)
    {
        _points = new[]
        {
            (9, -9),
            (-9, 9),
        };
    }

    public override void ApplyJitter(ref int deltaX, ref int deltaY)
    {
        var point = _points[_currentPoint];
        deltaX += point.x;
        deltaY += point.y;

        // Switch to the next position
        _currentPoint = (_currentPoint + 1) % _points.Length;
    }
}

public class CircleJitter : BaseJitter
{
    private readonly int _radius;
    private double _angle;
    private const double AngleIncrement = Math.PI / 2;

    public CircleJitter(int radius)
    {
        _radius = radius;
    }

    public override void ApplyJitter(ref int deltaX, ref int deltaY)
    {
        deltaX += (int)(_radius * Math.Cos(_angle));
        deltaY += (int)(_radius * Math.Sin(_angle));

        _angle += AngleIncrement;
        if (_angle >= 2 * Math.PI)
        {
            _angle -= 2 * Math.PI;
        }
    }
}

public class SmoothLeftRightJitter : BaseJitter
{
    private readonly (int x, int y)[] _points;
    private int _currentPoint = 0;
    private readonly int _strength;
    private double _acceleration;
    private const double AccelerationRate = 0.1;
    private double _currentMultiplier = 0.0;

    public SmoothLeftRightJitter(int strength)
    {
        _strength = strength;
        _points = new[]
        {
            (_strength, -_strength),  // Up-Right
            (-_strength, _strength),
            (_strength, -_strength),  // Up-Right
            (-_strength, _strength),// Down-Left
        };
        _acceleration = 0.0;
    }

    public override void ApplyJitter(ref int deltaX, ref int deltaY)
    {
        // Increase acceleration up to a maximum of 1.0
        _currentMultiplier = Math.Min(1.0, _currentMultiplier + AccelerationRate);

        var point = _points[_currentPoint];
        deltaX += (int)(point.x * _currentMultiplier);
        deltaY += (int)(point.y * _currentMultiplier);

        // Switch between Up-Right and Down-Left
        _currentPoint = (_currentPoint + 1) % _points.Length;
    }
}


public class PullDownJitter : BaseJitter
{
    private readonly double _baseStrength = 0.01; // Base movement per tick
    private int _strength;
    private double _accumulatedMovement;

    public PullDownJitter(int strength)
    {
        _strength = strength;
    }

    public override void ApplyJitter(ref int deltaX, ref int deltaY)
    {
        // Calculate movement this tick
        _accumulatedMovement += _strength * _baseStrength;

        // When we accumulate >= 1 pixel of movement, apply it
        if (_accumulatedMovement >= 2)
        {
            int pixelsToMove = (int)Math.Floor(_accumulatedMovement);
            deltaY += pixelsToMove;
            _accumulatedMovement -= pixelsToMove;
        }
    }

    public void UpdateStrength(int newStrength)
    {
        _strength = newStrength;
    }
}
