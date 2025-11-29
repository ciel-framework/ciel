using System.Collections;
using System.Globalization;

namespace Ciel.Birb;

public class Headers : IEnumerable<Header>
{
    private readonly List<Header> _items = new();

    public int? ContentLength
    {
        get
        {
            var contentLength = First(Header.ContentLength);
            if (string.IsNullOrEmpty(contentLength)) return null;
            if (int.TryParse(contentLength, NumberStyles.None, CultureInfo.InvariantCulture, out var result))
                return result;
            return null;
        }

        set => Set(Header.ContentLength, value?.ToString(CultureInfo.InvariantCulture));
    }

    public IEnumerator<Header> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(Header from)
    {
        _items.Add(from);
    }

    public void Add(string key, string value)
    {
        _items.Add(new Header(key, value));
    }

    public void Set(Header header)
    {
        for (var i = 0; i < _items.Count; i++)
            if (_items[i].Key == header.Key)
                _items[i] = header;
        Add(header);
    }

    public void Set(string key, string value)
    {
        Add(new Header(key, value));
    }

    public string? First(string key)
    {
        foreach (var item in _items)
            if (string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))
                return item.Value;

        return null;
    }
}