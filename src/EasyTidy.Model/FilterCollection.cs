using System;
using System.Collections;
using System.Collections.Generic;

namespace EasyTidy.Model;

public partial class FilterCollection : IReadOnlyList<FilterItem>
{
    private readonly List<FilterItem> _filters = new();

    public FilterItem this[int index] => _filters[index];
    public int Count => _filters.Count;

    public IEnumerator<FilterItem> GetEnumerator() => _filters.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(FilterItem filter) => _filters.Add(filter);
}
