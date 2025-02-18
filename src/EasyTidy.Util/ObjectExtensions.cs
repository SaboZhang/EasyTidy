using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Util;

public static class ObjectExtensions
{
    public static List<T> Clone<T>(this List<T> source) where T : ICloneable
    {
        if (source == null)
            return [];

        var newList = new List<T>();
        foreach (var item in source)
        {
            var clonedItem = (T)item.Clone();
            newList.Add(clonedItem);
        }

        return newList;
    }
}
