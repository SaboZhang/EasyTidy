using System;
using System.Collections;
using System.Collections.Generic;

namespace EasyTidy.Model;

public partial class FilterCollection : IReadOnlyList<Func<string, bool>>
{
    private readonly List<Func<string, bool>> _filters = new();

    public Func<string, bool> this[int index] => _filters[index];
    public int Count => _filters.Count;

    public IEnumerator<Func<string, bool>> GetEnumerator() => _filters.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(Func<string, bool> filter) => _filters.Add(filter);
}
