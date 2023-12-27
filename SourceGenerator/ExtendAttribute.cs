using System;

namespace SourceGenerator;

public class ExtendAttribute<T> : Attribute where T : class
{
    private readonly string _name;

    public ExtendAttribute()
    {
        _name = typeof(T).Name;
    }
}
