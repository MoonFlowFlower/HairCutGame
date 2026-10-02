using Hairball.Core;
using System.Numerics;
internal static class PresentationTests
{
    public static void Run(Action<string,Action> test,Action<bool,string> check)
    {
        void C(bool b,string m="phase 8 assertion")=>check(b,m);
        test("p8 paired gallery preserves hashes, portraits, traces and rejects a corrupted pair",()=>{
            string folder=Path.Combine(Path.GetTempPath(),"hairball-p8-"+Guid.NewGuid());try{
                var data=new PhotoMetadata(2,true,0,CurtainReaction.Satisfied,[],DateTime.UtcNow.ToString("O"),Hair:[new(1,"A",10,6)]);
                var assets=new Dictionary<string,byte[]>{{"before",[2,3]},{"group",[4,5]},{"avatar-0",[6,7]}};
                GalleryStore.Save(folder,"paired",[1,2],data,assets);var saved=GalleryStore.Load(folder).Single();C(saved.Metadata.Assets!.Length==3&&saved.Metadata.Hair![0].LossPercent==40);
                File.WriteAllBytes(GalleryStore.AssetPath(saved.Image,"before"),[9]);C(GalleryStore.Load(folder).Length==0);
                for(int i=0;i<27;i++)GalleryStore.Save(folder,"round-"+i,[1,2],data,assets);
                C(GalleryStore.Load(folder).Length==24);C(Directory.EnumerateFiles(folder,"*.png").Count()==24*4,"all paired assets expire with their parent");
                bool bad=false;try{GalleryStore.AssetPath(saved.Image,"../other");}catch(ArgumentException){bad=true;}C(bad);
            }finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        });
        test("p8 loss uses baseline/current mass, clamps growth and empty heads",()=>{C(MaterialFeel.HairLossPercent(10,6)==40);C(MaterialFeel.HairLossPercent(10,12)==0);C(MaterialFeel.HairLossPercent(10,-1)==100);C(MaterialFeel.HairLossPercent(0,0)==0);});
        test("p8 material motion monotonically follows resistance, freeze anchors stop it",()=>{var p=new Patch();float soft=MaterialFeel.Mobility(p);p.Stiffness=.7f;C(MaterialFeel.Mobility(p)<soft*.2f);p.Temperature=-65;C(MaterialFeel.Mobility(p)==0);p.Temperature=20;p.Anchored=true;C(MaterialFeel.Mobility(p)==0);});
        test("p8 local sink and facing never mutate authoritative wire",()=>{
            var s=ExperimentTests.World();PartyPlacementTests.Place(s,false);var w=s.State;var prop=w.Props.First(p=>p.Goal==0);prop.Attached=true;w.Time=1;
            foreach(var p in w.SharedHead.Patches){p.Glue=0;p.Stiffness=0;p.Temperature=20;}
            var wire=Wire.Encode(w);float soft=MaterialFeel.Sink(w,prop);C(soft>0);C(wire.SequenceEqual(Wire.Encode(w)));
            foreach(var p in w.SharedHead.Patches)p.Glue=1;C(MaterialFeel.Sink(w,prop)==0);
            C(MaterialFeel.Facing(new(0,2,3),Vector3.Zero,new(0,1,1))>MaterialFeel.Facing(new(0,2,-3),Vector3.Zero,new(0,1,1)));
        });
    }
}
