public abstract class AManagerBase : IManager, ITickable
{
    public bool IsInitialized { get; private set; }

    public void Init()
    {
        if (IsInitialized == true)
        {
            return;
        }

        OnInit();

        IsInitialized = true;
    }

    public void Main()
    {
        if (IsInitialized == false)
        {
            return;
        }

        OnMain();
    }

    public void Tick(float deltaTime)
    {
        if (IsInitialized == false)
        {
            return;
        }

        OnTick(deltaTime);
    }

    protected abstract void OnInit();

    protected virtual void OnMain()
    {
    }

    protected virtual void OnTick(float deltaTime)
    {
    }
}