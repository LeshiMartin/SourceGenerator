using SourceGenarator;
using SourceGenerator;
using System.ComponentModel;

Console.WriteLine("Hello World");
var dict = new Dictionary<string, string>();

if (dict.TryGetValue("", out var val))
{

}
var e = SomeEnum.One;
var f = new Foo();
var uid = f.Uid();
var _name = f.Name();
f.Name("SomeName");
Console.WriteLine(f.Name());
f.MiddleName();
f.Today();

Console.WriteLine(f.Age());
Console.WriteLine(f.DateOfBirth());
//var uid = f.Uid();
//Console.WriteLine(uid);


Console.WriteLine(SomeEnumExtensions.GetName(e));
//Console.WriteLine(ClassNames.ClassName);
foreach (var name in ClassNames.Names)
{
    Console.WriteLine(name);
}



public class Foo
{
    [DisplayName("Id")]
    public int Id { get; set; } = 1;
}
class Bar
{
    public string Name { get; set; } = "Bar";
}

[Extend<Foo>]
public class C
{
    public Guid Uid { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Martin";
    public int Age { get; set; } = 23;
    public DateTime DateOfBirth { get; set; } = new DateTime(1988, 06, 20);

    public DateTime Today { get; set; } = DateTime.Now;
    public string LastName { get; set; }
    public string MiddleName { get; set; }
}

