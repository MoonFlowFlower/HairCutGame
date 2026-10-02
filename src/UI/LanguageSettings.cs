using Godot;
using Hairball.Core;

namespace Hairball;
public static class LanguageSettings
{
    public const string Path="user://settings.cfg";
    public static readonly SystemFont Font=new(){FontNames=["Microsoft YaHei UI","Microsoft YaHei","Noto Sans CJK SC","sans-serif"],AllowSystemFallback=true};
    public static void Load(string path=Path)
    {
        var config=new ConfigFile();var error=config.Load(path);
        L.SetLocale(error==Error.Ok?config.GetValue("interface","language",OS.GetLocaleLanguage()).AsString():OS.GetLocaleLanguage());
    }
    public static Error Select(string locale,string path=Path)
    {
        L.SetLocale(locale);
        var config=new ConfigFile();config.Load(path);
        config.SetValue("interface","language",L.Locale);
        var result=config.Save(path);
        if(result!=Error.Ok)GD.PushWarning($"Could not save language preference: {result}");
        return result;
    }
}
