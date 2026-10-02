using Godot;
using Hairball.Core;
using System.Linq;
namespace Hairball;
public partial class Hud
{
    public bool CatOn=true;
    public int ReturningRounds,ReviewPhotoRound;
    Label? worldLabel;
    void SetupWorldMenu()
    {
        var cat=new CheckBox{ButtonPressed=CatOn};menuBox.AddChild(cat);cat.Toggled+=v=>CatOn=v;translations.Add(()=>cat.Text=L.Locale=="zh"?"店猫（房主，35% 出现）":"Shop cat (host, 35% chance)");
        var drag=new CheckBox{ButtonPressed=PresentationSettings.AllowDrag};menuBox.AddChild(drag);drag.Toggled+=v=>{PresentationSettings.AllowDrag=v;PresentationSettings.Save();};translations.Add(()=>drag.Text=L.Locale=="zh"?"允许粘住后拖行（默认关闭）":"Allow glued dragging (default off)");
        var returning=new OptionButton();returning.AddItem("",0);returning.AddItem("",3);returning.AddItem("",4);menuBox.AddChild(returning);returning.ItemSelected+=i=>ReturningRounds=returning.GetItemId((int)i);
        translations.Add(()=>{returning.SetItemText(0,L.Locale=="zh"?"普通场次 · 3 局":"Ordinary session · 3 rounds");returning.SetItemText(1,L.Locale=="zh"?"回头客 · 3 局":"Returning customer · 3 rounds");returning.SetItemText(2,L.Locale=="zh"?"回头客 · 4 局":"Returning customer · 4 rounds");});
    }
    void UpdateWorldHud(WorldState w,PlayerState p)
    {
        worldLabel??=Text(new(460,155),new(380,80),17);bool zh=L.Locale=="zh";worldLabel.Visible=!menu.Visible;
        worldLabel.Text=w.Cat.Mode==CatMode.Telegraph?(zh?"猫盯住目标了！E 抱走 / 吹风 / 扔道具引开":"CAT IS WATCHING! E carry / blow / throw a lure"):w.Cat.Mode==CatMode.Sleeping?(zh?"猫睡在头上 · 增重 2.5":"CAT ON HAIR · +2.5 mass"):p.GlueHead&&w.Time<p.GlueUntil?(zh?"手被粘住了 · 热风或火焰可解开":"HAND GLUED · hot air or flame releases it"):w.Experiment.Drowsiness>.2f?(zh?"顾客打瞌睡了……":"CUSTOMER IS DOZING…"):"";
        if(w.Phase==Phase.Complete&&w.ReturningHeads){scores.Size=new(610,480);scores.HorizontalAlignment=HorizontalAlignment.Left;scores.Text=(zh?"回头客场次 · 每局目标和照片\n":"RETURNING SESSION · TARGETS AND PHOTOS\n")+string.Join("\n",w.RoundHistory.Select(r=>$"{r.Round}. {r.Target.Title} · "+(r.Success?"✓":"×")))+"\n\n"+(w.RoundHistory.FirstOrDefault(r=>r.Round==ReviewPhotoRound)?.Target.Face(true)??"");}
    }
}
