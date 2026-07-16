using UnityEngine;

internal sealed class ShuffleBag<T>
{
    private T[] _bag;
    private int _next;

    public void SetSource(T[] source)
    {
        if (source == null || source.Length == 0)
        {
            _bag = null;
            _next = 0;
            return;
        }

        _bag = (T[])source.Clone();
        Shuffle();
        _next = 0;
    }

    public T Next()
    {
        if (_bag == null || _bag.Length == 0)
            return default;

        if (_next >= _bag.Length)
        {
            Shuffle();
            _next = 0;
        }

        return _bag[_next++];
    }

    private void Shuffle()
    {
        for (int i = _bag.Length - 1; i > 0; --i)
        {
            int j = Random.Range(0, i + 1);
            (_bag[i], _bag[j]) = (_bag[j], _bag[i]);
        }
    }
}
