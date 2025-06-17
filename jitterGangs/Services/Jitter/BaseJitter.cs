namespace JitterGang.Services.Jitter;

public abstract class BaseJitter
{
    public abstract void ApplyJitter(ref int deltaX, ref int deltaY);
}