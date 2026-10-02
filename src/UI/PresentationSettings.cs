using Godot;
using Hairball.Core;
namespace Hairball;
public static class PresentationSettings
{
    public static bool Numbers;
    public static bool AllowDown,AllowViewChanges,AllowDrag;
    public static void Load(){var c=new ConfigFile();if(c.Load("user://presentation.cfg")==Error.Ok){Numbers=c.GetValue("assist","numbers",false).AsBool();AllowDrag=c.GetValue("comfort","drag",false).AsBool();AllowViewChanges=c.GetValue("comfort","views",false).AsBool();AllowDown=c.GetValue("comfort","down",false).AsBool();}}
    public static void Save(){var c=new ConfigFile();c.SetValue("assist","numbers",Numbers);c.SetValue("comfort","down",AllowDown);c.SetValue("comfort","views",AllowViewChanges);c.SetValue("comfort","drag",AllowDrag);c.Save("user://presentation.cfg");}
}
public partial class Hud
{
    void SetupPresentationMenu(){PresentationSettings.Load();var toggle=new CheckBox{ButtonPressed=PresentationSettings.Numbers};menuBox.AddChild(toggle);translations.Add(()=>toggle.Text=L.Locale=="zh"?"显示施工数值辅助（可选）":"Construction numbers (optional)");toggle.Toggled+=on=>{PresentationSettings.Numbers=on;PresentationSettings.Save();};var down=new CheckBox{ButtonPressed=PresentationSettings.AllowDown};menuBox.AddChild(down);translations.Add(()=>down.Text=L.Locale=="zh"?"允许倒地效果（默认关闭）":"Allow downed effects (default off)");down.Toggled+=on=>{PresentationSettings.AllowDown=on;PresentationSettings.Save();};}
}
