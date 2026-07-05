public interface IAudioPoolManager
{
    public PooledAudioSource Borrow();
    public void Return(PooledAudioSource pooledAudioSource);
}