namespace JitterGang.Services.Jitter;

public abstract class BaseJitter : IJitterEffect
{
    public abstract void ApplyJitter(ref int deltaX, ref int deltaY);
}