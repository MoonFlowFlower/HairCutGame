using Godot;
using Hairball.Core;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
namespace Hairball;
public partial class SalonView
{
    SubViewport? photoViewport;
    Node3D? photoScene,galleryWall;
    Label? photoCountdown;
    readonly string galleryPath=ProjectSettings.GlobalizePath("user://gallery");
    int galleryRound=-1,savedRound=-1;string galleryMatch="";
    bool takingPhoto,afterStarted,groupStarted;
    double countdownAt=-1;
    byte[]? pendingPhoto;
    readonly Dictionary<string,byte[]> photoAssets=new();
    public string LastPhotoPath="";public Texture2D? RevealPhoto;
    public int PhotoCountdown {get;private set;}
    static double PhotoClock=>Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency;
    public void SyncGallery(WorldState w)
    {
        if(!w.Experiment.IsB||DisplayServer.GetName()=="headless")return;
        if(galleryWall==null)RefreshGallery();
        if(galleryRound!=w.Round||galleryMatch!=w.MatchTag){if(galleryMatch!=w.MatchTag)roundPhotos.Clear();galleryMatch=w.MatchTag;galleryRound=w.Round;savedRound=-1;pendingPhoto=null;RevealPhoto=null;photoAssets.Clear();afterStarted=groupStarted=false;countdownAt=-1;}
        PhotoCountdown=countdownAt>=0&&!groupStarted?Math.Max(0,3-(int)(PhotoClock-countdownAt)):0;
        if(photoCountdown!=null){photoCountdown.Visible=PhotoCountdown>0;photoCountdown.Text=(L.Locale=="zh"?"合影 · 摆好姿势！ ":"GROUP PHOTO · STRIKE A POSE! ")+PhotoCountdown;}
        if(takingPhoto)return;
        if(!photoAssets.ContainsKey("before")&&w.Phase==Phase.Arrival){CapturePhoto(w,"before");return;}
        bool reveal=w.Experiment.Leave==LeaveStage.Mirror||w.Experiment.Resolved;
        if(reveal&&!afterStarted){afterStarted=true;CapturePhoto(w,"after");return;}
        if(w.Experiment.Resolved&&w.Time>=Session.PieStart(w)+8&&countdownAt<0)countdownAt=PhotoClock;
        if(reveal&&!groupStarted&&countdownAt>=0&&PhotoClock-countdownAt>=3){groupStarted=true;CapturePhoto(w,"group");return;}
        if(w.Experiment.Resolved&&pendingPhoto!=null&&photoAssets.ContainsKey("group")&&savedRound!=w.Round)SavePhoto(w);
    }
    // A separate render world copies current visible poses. Never relocate actors or FPS cameras.
    // Private HUD and shop walls are excluded so every participant fits the group frame.
    async Task<byte[]> RenderPhoto(WorldState w,string kind,int portrait=-1)
    {
        if(photoViewport==null){photoViewport=new(){Size=new(640,480),OwnWorld3D=true,RenderTargetUpdateMode=SubViewport.UpdateMode.Disabled};AddChild(photoViewport);var layer=new CanvasLayer{Layer=8};AddChild(layer);photoCountdown=new(){Position=new(290,325),Size=new(700,90),HorizontalAlignment=HorizontalAlignment.Center};photoCountdown.AddThemeFontSizeOverride("font_size",36);photoCountdown.AddThemeColorOverride("font_color",new Color("ffe7a0"));photoCountdown.AddThemeFontOverride("font",LanguageSettings.Font);layer.AddChild(photoCountdown);}
        if(photoScene!=null){photoViewport.RemoveChild(photoScene);photoScene.QueueFree();}
        photoScene=new();photoViewport.AddChild(photoScene);
        photoScene.AddChild(new DirectionalLight3D{RotationDegrees=new(-35,-25,0),LightEnergy=1.5f});
        photoScene.AddChild(new WorldEnvironment{Environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new("4e737f"),AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=Colors.White,AmbientLightEnergy=.75f}});
        bool group=kind=="group",avatar=portrait>=0;
        var points=new List<Vector3>();var included=new HashSet<int>();
        void CopyModel(Node3D model,Transform3D pose){var copy=(Node3D)model.Duplicate((int)Node.DuplicateFlags.UseInstantiation);photoScene.AddChild(copy);copy.Transform=pose;copy.Visible=true;Art.Layers(copy,1);points.Add(pose.Origin+Vector3.Up*(avatar?1.5f:0));points.Add(pose.Origin+Vector3.Up*(avatar?2.3f:2));}
        if((!avatar||w.Players.Any(p=>p.Active&&p.Customer&&p.Slot==portrait))&&customers.TryGetValue(0,out var customer))CopyModel(customer,customer.GlobalTransform);
        if(group||avatar)foreach(var p in w.Players.Where(p=>p.Active&&!p.Customer&&(group||p.Slot==portrait))){included.Add(p.Id);if(Bodies.TryGetValue(p.Id,out var body))CopyModel(body.Model,body.Model.GlobalTransform);else {var model=Art.Person(photoScene,Art.Team[p.Slot]);model.Position=Art.V(p.Position);model.Rotation=new(0,p.Yaw,0);points.Add(model.Position);points.Add(model.Position+Vector3.Up*2);}if(group){Art.Label(photoScene,p.Name,Art.V(p.Position)+Vector3.Up*2.8f,20);points.Add(Art.V(p.Position)+Vector3.Up*3);}}
        foreach(var h in w.Heads.Where(h=>!h.Loose&&!h.Miniature&&(!h.Barber&&(!avatar||w.Players.Any(p=>p.Active&&p.Customer&&p.Slot==portrait))||h.Barber&&included.Contains(h.Owner))))
        {
            var hair=new HeadView();photoScene.AddChild(hair);hair.Update(h,w.Time);if(heads.TryGetValue(h.Id,out var current))hair.Transform=current.GlobalTransform;
            points.AddRange(h.Volume.Samples().Where((_,i)=>i%12==0).Select(v=>hair.ToGlobal(Art.V(v))));points.Add(hair.Position);
        }
        if(group||avatar)foreach(var p in w.Players.Where(p=>p.Active&&(group||p.Slot==portrait))){if(revealMasks.TryGetValue(p.Id,out var mask)&&mask.Visible)CopyModel(mask,mask.GlobalTransform);if(likes.TryGetValue(p.Id,out var like)&&like.Visible)CopyModel(like,like.GlobalTransform);}
        if(!avatar)foreach(var prop in w.Props.Where(p=>p.Attached)){var view=CreateProp(photoScene,prop.Goal);view.Position=Art.V(prop.Position);view.Rotation=Art.V(prop.Rotation);points.Add(view.Position);}
        if(catView!=null&&catView.Visible&&!avatar)CopyModel(catView,catView.GlobalTransform);
        if(points.Count==0)points.Add(Art.V(w.SharedHead.Position));
        var min=points.Aggregate(new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity),(a,p)=>new(Math.Min(a.X,p.X),Math.Min(a.Y,p.Y),Math.Min(a.Z,p.Z)));
        var max=points.Aggregate(new Vector3(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity),(a,p)=>new(Math.Max(a.X,p.X),Math.Max(a.Y,p.Y),Math.Max(a.Z,p.Z)));
        var focus=(min+max)*.5f;float radius=Math.Max(.7f,(max-min).Length()*.5f);var camera=new Camera3D{Current=true,Fov=57,Near=.05f};photoScene.AddChild(camera);
        camera.Position=focus+(avatar?Vector3.Back:new Vector3(.4f,.25f,1).Normalized())*(radius/MathF.Sin(Mathf.DegToRad(28.5f))*1.12f);camera.LookAt(focus);
        Art.Box(photoScene,new(focus.X,-.05f,focus.Z),new(Math.Max(14,radius*3),.1f,Math.Max(14,radius*3)),new("344f5c"));
        photoViewport.RenderTargetUpdateMode=SubViewport.UpdateMode.Always;
        await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        if(!IsInsideTree())return [];
        using var image=photoViewport.GetTexture().GetImage();var result=image.SavePngToBuffer();photoViewport.RenderTargetUpdateMode=SubViewport.UpdateMode.Disabled;return result;
    }
    async void CapturePhoto(WorldState w,string kind)
    {
        takingPhoto=true;int round=w.Round;
        try{var png=await RenderPhoto(w,kind);if(round!=galleryRound||png.Length==0)return;if(kind=="after"){pendingPhoto=png;using var image=new Image();image.LoadPngFromBuffer(png);RevealPhoto=ImageTexture.CreateFromImage(image);roundPhotos[round]=RevealPhoto;}else photoAssets[kind]=png;
            if(kind=="group")foreach(var p in w.Players.Where(p=>p.Active).ToArray()){var portrait=await RenderPhoto(w,"avatar",p.Slot);if(round!=galleryRound)return;photoAssets["avatar-"+p.Slot]=portrait;}
        }catch(Exception e){GD.Print("GALLERY_PHOTO_UNAVAILABLE "+e.Message);}finally{takingPhoto=false;}
    }
    void SavePhoto(WorldState w)
    {
        try{string id=DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")+"-"+Guid.NewGuid().ToString("N");
            GalleryStore.Save(galleryPath,id,pendingPhoto!,new(w.Round,w.Experiment.Success,w.Job.Goal,w.Experiment.Curtain,w.Experiment.ResultActions.ToArray(),DateTime.UtcNow.ToString("O"),Hair:w.Experiment.ResultHair.ToArray(),Match:w.MatchTag,Target:w.Experiment.ResultTarget,Family:w.Twist.ResultFamily),photoAssets);
            savedRound=w.Round;LastPhotoPath=Path.Combine(galleryPath,"photo-"+id+".png");RefreshGallery();GD.Print("GALLERY_SAVED "+LastPhotoPath);
        }catch(IOException e){savedRound=w.Round;GD.Print("GALLERY_WRITE_UNAVAILABLE "+e.Message);}catch(UnauthorizedAccessException e){savedRound=w.Round;GD.Print("GALLERY_WRITE_UNAVAILABLE "+e.Message);}
    }
    public void RefreshGallery()
    {
        if(galleryWall!=null){RemoveChild(galleryWall);galleryWall.QueueFree();}
        galleryWall=new(){Name="PartyPhotoWall"};AddChild(galleryWall);Art.Label(galleryWall,L.Locale=="zh"?"照片墙 · 改造前 / 改造后 / 合影":"PHOTO WALL · BEFORE / AFTER / GROUP",new(0,4.55f,5.83f),24);
        var photos=GalleryStore.Load(galleryPath);
        void Frame(string path,Vector3 pos,Vector3 size){using var image=Image.LoadFromFile(path);if(image==null||image.IsEmpty())return;var frame=Art.Box(galleryWall!,pos,size,Colors.White);frame.MaterialOverride=new StandardMaterial3D{AlbedoTexture=ImageTexture.CreateFromImage(image),ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded};}
        for(int i=0;i<photos.Length;i++)try{var item=photos[i];var pos=new Vector3(-4.6f+(i%8)*1.25f,3.90f-(i/8)*1.40f,5.82f);bool pair=item.Metadata.Assets?.Any(a=>a.Key=="before")==true;
            Frame(item.Image,pos+(pair?new Vector3(.28f,0,0):Vector3.Zero),new(pair?.53f:1.1f,.40f,.03f));
            if(pair)Frame(GalleryStore.AssetPath(item.Image,"before"),pos-new Vector3(.28f,0,0),new(.53f,.40f,.03f));
            Art.Label(galleryWall,pair?(L.Locale=="zh"?"改造前        改造后":"BEFORE      AFTER"):"",pos+new Vector3(0,.27f,-.03f),10);
            if(item.Metadata.Assets?.Any(a=>a.Key=="group")==true)Frame(GalleryStore.AssetPath(item.Image,"group"),pos+new Vector3(0,-.45f,0),new(1.1f,.38f,.03f));
            foreach(var asset in item.Metadata.Assets??[])if(asset.Key.StartsWith("avatar-")){int slot=asset.Key[7]-'0';Frame(GalleryStore.AssetPath(item.Image,asset.Key),pos+new Vector3(-.42f+slot*.28f,-.81f,0),new(.24f,.20f,.03f));}
            var label=Art.Label(galleryWall,"",pos+new Vector3(0,-1.03f,-.05f),10);var localized=(LocalizedLabel3D)label;localized.DynamicText=()=> (item.Metadata.Success?L.T("COMPLETE"):L.T("INCOMPLETE"))+" · "+string.Join(" / ",item.Metadata.Actions.Take(1).Select(PartyResults.Text));label.Text=localized.DynamicText();
        }catch(Exception e){GD.Print("GALLERY_ITEM_SKIPPED "+e.Message);}
    }
}
