public interface IAudioPoolManager
{
    public BasePooledSource Borrow();
    public void Return(BasePooledSource pooledAudioSource);
}