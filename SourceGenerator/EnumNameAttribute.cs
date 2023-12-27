using System;

namespace SourceGenerator;

public class EnumNameAttribute : Attribute
{
    public EnumNameAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}