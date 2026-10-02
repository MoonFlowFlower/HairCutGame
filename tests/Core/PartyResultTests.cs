using Hairball.Core;
internal static class PartyResultTests
{
    public static void Run(Action<string,Action> test,Action<bool,string> check)
    {
        void C(bool b,string m="result assertion")=>check(b,m);
        test("party base pay survives all ordinary penalties and tip rules are nonnegative",()=>{
            var s=ExperimentTests.World();s.State.Job.Penalty=10000;s.State.Experiment.EarlySeconds=100;
            var pay=PartyResults.Evaluate(s.State,true);C(pay.Base==40&&pay.Tip==0);
            pay=PartyResults.Evaluate(s.State,false);C(pay.Base==0&&pay.Tip==0&&pay.Reaction==CurtainReaction.Complaint);
            s.State.Job.Penalty=0;s.State.Experiment.EarlySeconds=21;pay=PartyResults.Evaluate(s.State,true);C(pay.Tip==10);
            s.State.SharedHead.Patches[0].Temperature=-40;pay=PartyResults.Evaluate(s.State,true);C(pay.Tip==20&&pay.Reaction==CurtainReaction.Funny);
        });
        test("party explicit action facts are ordered bounded and absent from build snapshots",()=>{
            var s=ExperimentTests.World();for(int i=0;i<180;i++){s.State.Time=i*3;s.RecordAction(PartyAction.Glue,1,i);}
            C(s.ActionLog.Count==Session.ActionLimit&&s.ActionLog[0].Target==52&&s.ActionLog[^1].Target==179);
            C(s.State.Experiment.ResultActions.Count==0);s.State.SharedHead.Patches[0].Source=999;s.State.SharedHead.Patches[0].BurnSource=999;
            s.State.Remaining=0;for(int i=0;i<140&&!s.State.Experiment.Resolved;i++)s.Tick(.1f);
            C(s.State.Experiment.ResultActions.Count==6&&s.State.Experiment.ResultActions.All(a=>a.Actor==1&&a.Name=="A"));
            var copy=s.State.Experiment.Clone();C(copy.ResultActions.SequenceEqual(s.State.Experiment.ResultActions));copy.ResultActions.Clear();C(s.State.Experiment.ResultActions.Count==6);
        });
        test("party gallery persists authoritative metadata caps entries and tolerates corruption",()=>{
            string path=Path.Combine(Path.GetTempPath(),"hairball-gallery-"+Guid.NewGuid().ToString("N"));
            try{Directory.CreateDirectory(path);File.WriteAllText(Path.Combine(path,"photo-corrupt.json"),"bad");C(GalleryStore.Load(path).Length==0);
                var m=new PhotoMetadata(3,false,0,CurtainReaction.Complaint,[new(2,PartyAction.Bell,7,"B",0)],"UTC");
                for(int i=0;i<27;i++)GalleryStore.Save(path,"test-"+i,[1,2,3],m);var items=GalleryStore.Load(path);C(items.Length==24&&items.All(p=>p.Metadata.Round==3&&!p.Metadata.Success&&p.Metadata.Actions[0].Actor==7));
                bool rejected=false;try{GalleryStore.Save(path,"../escape",[],m);}catch(ArgumentException){rejected=true;}C(rejected);
            }finally{Directory.Delete(path,true);}
        });
    }
}
