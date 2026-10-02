using Godot;
using Hairball.Core;

namespace Hairball;
public partial class LocalizedLabel3D : Label3D
{
    public string SourceText="";
    public System.Func<string>? DynamicText;
    public override void _Ready(){Font=LanguageSettings.Font;Refresh();L.Changed+=Refresh;}
    public override void _ExitTree(){L.Changed-=Refresh;}
    void Refresh(){Text=DynamicText?.Invoke()??L.Text(SourceText);}
}
