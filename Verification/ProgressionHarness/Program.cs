using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

internal static class Program
{
    private static int passed;
    private static void Main()
    {
        string root = FindRoot();
        Resources.Configs = Directory.GetFiles(Path.Combine(root,"Assets/Resources/Islands"),"*.asset").Select(ReadIsland).ToArray();
        Run("real six-island pacing reaches 75% before boss and 100% after boss", ContentPacing);
        Run("legacy enemy composition null arrays fail closed", NullComposition);
        Run("combat caps and explicit boss snapshot bucket", StateCaps);
        Run("invalid values and duplicate ids do not consume progress", InvalidAndDuplicate);
        Run("restored event fires once across epsilon boundary", RestoredEvent);
        Run("snapshot suppression and completed ids round trip", SnapshotEvents);
        Run("unknown snapshot island ids cannot overwrite default island", UnknownSnapshot);
        Run("legacy configured boss progress migrates without combat cap loss", LegacyMigration);
        Run("completion unlocks next island without auto travel", UnlockAndReload);
        Run("ordinary combat cannot unlock the next island", CombatCannotAdvance);
        Run("boat travel accepts unlocked unrestored island and legacy alias", TravelAliases);
        Run("unknown destinations fail without falling back to first island", UnknownTravel);
        Run("boss remains locked until Tide restoration reaches 75%", BossGate);
        Run("failed combat retries without consuming another power point", RetryBudget);
        Run("flee returns to exploration without progress or a repeated combat", FleeRecovery);
        Run("guard combat callback cannot complete a flow puzzle", GuardCannotCompletePuzzle);
        Run("unsolved puzzle return neither restores nor clears encounter", UnsolvedPuzzle);
        Run("solved puzzle advances once and keeps stable runtime key", SolvedPuzzle);
        Run("active encounter rejects replacement until paused", ActiveReplacement);
        Run("missing puzzle data cannot launch stale puzzle context", MissingPuzzleData);
        Console.WriteLine($"ProgressionHarness: {passed} production-source checks passed. Unity scenes/coroutines are not exercised.");
    }
    private static string FindRoot()
    {
        DirectoryInfo d = new(Directory.GetCurrentDirectory());
        while(d != null) { if(File.Exists(Path.Combine(d.FullName,"Assets/IslandFlowController.cs"))) return d.FullName; d=d.Parent; }
        throw new Exception("Run from within the TIDE repository.");
    }
    private static IslandConfig ReadIsland(string path)
    {
        string text = File.ReadAllText(path);
        IslandConfig c = new() { islandId=Regex.Match(text,@"(?m)^  islandId: (.+)$").Groups[1].Value.Trim(), viceName=Path.GetFileNameWithoutExtension(path) };
        List<EncounterDefinition> encounters = new();
        foreach(Match m in Regex.Matches(text,@"(?ms)^  - encounterId: (.*?)\n(.*?)(?=^  - encounterId:|\z)"))
        {
            string block=m.Groups[2].Value;
            EncounterDefinition e=new() { encounterId=m.Groups[1].Value.Trim(), type=(EncounterType)int.Parse(Regex.Match(block,@"(?m)^    type: (\d+)").Groups[1].Value), isBossEncounter=Regex.IsMatch(block,@"(?m)^    isBossEncounter: 1$"), restorationValue=float.Parse(Regex.Match(block,@"(?m)^    restorationValue: ([0-9.]+)").Groups[1].Value,CultureInfo.InvariantCulture) };
            if(Regex.IsMatch(block,@"(?m)^    puzzleData: \{fileID: 11400000,")) e.puzzleData=new PuzzleData();
            encounters.Add(e);
        }
        c.encounters=encounters.ToArray(); return c;
    }
    private static void Run(string name,Action action) { action();passed++;Console.WriteLine("PASS "+name); }
    private static void Check(bool value,string reason) { if(!value) throw new Exception(reason); }
    private static void Near(float actual,float expected,string reason) => Check(Math.Abs(actual-expected)<0.0002f,$"{reason}: expected {expected}, got {actual}");
    private static IslandRestorationTracker Tracker()
    {
        if(IslandProgressionManager.Instance!=null) UnityEngine.Object.Destroy(IslandProgressionManager.Instance);
        if(IslandRestorationTracker.Instance!=null) UnityEngine.Object.Destroy(IslandRestorationTracker.Instance);
        GameStateManager.Instance=new GameStateManager(); PowerBudgetTracker.Instance=new PowerBudgetTracker(); DialogueSystem.Instance=null;
        return new GameObject().AddComponent<IslandRestorationTracker>();
    }
    private static IslandFlowController Flow(IslandConfig config,IslandRestorationTracker tracker)
    {
        var flow=new GameObject().AddComponent<IslandFlowController>();
        typeof(IslandFlowController).GetField("tracker",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(flow,tracker);
        flow.StartIsland(config); return flow;
    }
    private static IslandConfig Lust => IslandThemeRegistry.GetConfig("island_lust");
    private static void ContentPacing()
    {
        Check(IslandThemeRegistry.ProgressionOrder.Count==6,"Canonical GDD has six islands");
        foreach(string id in IslandThemeRegistry.ProgressionOrder)
        {
            IslandConfig config=IslandThemeRegistry.GetConfig(id); Check(config!=null&&config.IsValid(),$"{id} content valid");
            var s=new IslandRestorationState(id); var bosses=config.encounters.Where(e=>e.isBossEncounter).ToArray();
            Check(bosses.Length==1,$"{id} one boss");
            foreach(var e in config.encounters.Where(e=>!e.isBossEncounter)) { if(e.type==EncounterType.Puzzle) Check(e.puzzleData!=null,$"{id}:{e.encounterId} puzzle configured"); s.RecordCompletion(e.encounterId,e.type,e.restorationValue); }
            Near(s.RestorationPercent,75,$"{id} ordinary restoration");Check(s.CombatContribution<=.5f,$"{id} combat cap");
            foreach(var e in bosses) s.RecordCompletion(e.encounterId,e.type,e.restorationValue,true);
            Check(s.IsIslandRestored,$"{id} boss completes island");Near(s.RestorationPercent,100,$"{id} final restoration");
        }
    }
    private static void NullComposition()
    {
        var c=new EnemyComposition(){names=null,elements=null,attackModifiers=null,defenseModifiers=null,maxHpModifiers=null};
        Check(c.Count==0,"missing legacy arrays give no enemies");Check(!c.IsValidIndex(0),"null slots unavailable");Check(c.GetName(0)==null,"missing name safe");Check(c.GetAttackModifier(0)==0,"missing stat safe");
        c=EnemyComposition.Create(null,null,null,null,null);Check(c.Count==0,"Create normalizes absent arrays");
        c=new EnemyComposition(){enemyDataSlots=new EnemyData[]{null,new EnemyData(),null,new EnemyData()}};Check(c.Count==2,"data slots count only configured enemies");Check(c.GetConfiguredEnemyData(0)!=null&&c.GetConfiguredEnemyData(1)!=null&&c.GetConfiguredEnemyData(2)==null,"compact data slot lookup");
    }
    private static void StateCaps()
    {
        var s=new IslandRestorationState("island_lust"); s.RecordCompletion("c1",EncounterType.Combat,.8f);s.RecordCompletion("c2",EncounterType.Combat,.8f);Near(s.CombatContribution,.5f,"combat cap");
        s.RecordCompletion("p1",EncounterType.Puzzle,.25f);s.RecordCompletion("boss",EncounterType.Combat,.25f,true);Near(s.TotalContribution,1,"boss contribution survives cap");
        s.ApplySnapshot(new(){combatContribution=.8f,bossContribution=.1f,puzzleContribution=float.NaN});Near(s.TotalContribution,.6f,"explicit boss bucket must not migrate overflow twice");
    }
    private static void InvalidAndDuplicate()
    {
        var s=new IslandRestorationState("island_lust");s.RecordCompletion("x",EncounterType.Combat,float.NaN);s.RecordCompletion("y",EncounterType.Puzzle,float.PositiveInfinity);s.RecordCompletion("z",EncounterType.Combat,-1);Check(s.CompletedEncounterIds.Count==0,"invalid contributions consume no ids");
        s.RecordCompletion("x",EncounterType.Combat,.1f);s.RecordCompletion("x",EncounterType.Puzzle,.4f);Near(s.TotalContribution,.1f,"duplicate receipt id");
    }
    private static void RestoredEvent()
    {
        var t=Tracker();int events=0;t.OnIslandRestored+=_=>events++;t.RecordEncounterCompletion("island_lust","a",EncounterType.Puzzle,.999f);t.RecordEncounterCompletion("island_lust","b",EncounterType.Puzzle,.001f);Check(events==1,"epsilon completion must emit only once");
    }
    private static void SnapshotEvents()
    {
        var t=Tracker();t.RecordEncounterCompletion("island_lust","c",EncounterType.Combat,.25f);var snap=t.CaptureSnapshot();int events=0;t.OnRestorationChanged+=(_,_)=>events++;t.SetSuppressEvents(true);t.ApplySnapshot(snap);Check(events==0,"suppression survives snapshot");Check(t.HasClearedEncounter("island_lust","c"),"receipt restored");t.SetSuppressEvents(false);t.ApplySnapshot(snap);Check(events==1,"unsuppressed refresh");
    }
    private static void UnknownSnapshot() {var t=Tracker();t.ApplySnapshot(new(){islands=new(){new(){islandId="typo",puzzleContribution=1}}});Near(t.GetRestorationPercent("island_lust"),0,"unknown snapshot must not fallback to first island");}
    private static void LegacyMigration()
    {
        var t=Tracker();var ids=Lust.encounters.Where(e=>e.type==EncounterType.Combat).Select(e=>e.encounterId).ToList();
        t.ApplySnapshot(new(){islands=new(){new(){islandId="island_lust",combatContribution=.5f,puzzleContribution=.25f,completedEncounterIds=ids}}});Near(t.GetRestorationPercent("island_lust"),100,"legacy boss split");
    }
    private static void UnlockAndReload()
    {
        var t=Tracker();var p=new GameObject().AddComponent<IslandProgressionManager>();CompleteIsland(t,"island_lust");Check(p.IsIslandUnlocked("island_greed"),"next unlock");Check(p.ActiveIslandId=="island_lust","explicit boat travel required");var ts=t.CaptureSnapshot();var ps=p.CaptureSnapshot();t.ApplySnapshot(new());p.ResetProgressionForDebug();t.ApplySnapshot(ts);p.ApplySnapshot(ps);Check(p.IsIslandUnlocked("island_greed"),"reload unlock");Check(t.HasClearedEncounter("island_lust","lust_boss"),"reload boss receipt");
    }
    private static void CompleteIsland(IslandRestorationTracker t,string id) { foreach(var e in IslandThemeRegistry.GetConfig(id).encounters)t.RecordEncounterCompletion(id,e.encounterId,e.type,e.restorationValue); }
    private static void CombatCannotAdvance() {var t=Tracker();var p=new GameObject().AddComponent<IslandProgressionManager>();for(int i=0;i<10;i++)t.RecordEncounterCompletion("island_lust","combat"+i,EncounterType.Combat,.25f);Near(t.GetRestorationPercent("island_lust"),50,"combat cap");Check(!p.IsIslandUnlocked("island_greed"),"combat alone cannot unlock");}
    private static void TravelAliases()
    {
        Tracker();var p=new GameObject().AddComponent<IslandProgressionManager>();p.TryUnlockIsland("island_greed");TeleportAnchor.ClearRegistryForDebug();var a=new GameObject().AddComponent<TeleportAnchor>();a.Configure("dock","island_greed",Vector3.zero,true);
        Check(TravelValidationService.ValidateTravel("island_lust","island_gluttony").CanTravel,"legacy alias must find canonical dock");Check(TravelValidationService.ValidateTravel("island_hub","island_greed").CanTravel,"hub travel bypasses absent in-scene docks");Check(TravelValidationService.ValidateTravel("island_lust","island_hub").CanTravel,"hub always reachable");
    }
    private static void UnknownTravel() {Tracker();new GameObject().AddComponent<IslandProgressionManager>();Check(!TravelValidationService.ValidateTravel("island_hub","typo").CanTravel,"unknown id cannot fallback");}
    private static void BossGate()
    {
        var t=Tracker();var custom=new IslandConfig(){islandId="island_lust",encounters=new[]{Lust.encounters[^1],Lust.encounters[0],Lust.encounters[1]}};var f=Flow(custom,t);Check(f.CurrentEncounterIndex==1,"locked boss skipped");Check(GameStateManager.Instance.PendingCombatEncounterId=="lust_c1","ordinary combat first");f.ResetForNewGame();
        t.SetIslandRestorationPercentForDebug("island_lust",75);GameStateManager.Instance.Busy=false;f.StartIsland(custom);Check(f.CurrentEncounterIndex==0,"75% unlocks boss");
    }
    private static void RetryBudget() {var t=Tracker();var f=Flow(Lust,t);Check(PowerBudgetTracker.Instance.Consumptions==1,"first combat spend");GameStateManager.Instance.Busy=false;f.OnReturnFromCombat(false);Check(PowerBudgetTracker.Instance.Consumptions==1,"retry preserves reservation");Check(!t.HasClearedEncounter("island_lust","lust_c1"),"loss remains incomplete");}
    private static void FleeRecovery() {var t=Tracker();var f=Flow(Lust,t);GameStateManager.Instance.Busy=false;f.OnReturnFromCombat(false,true);Check(!f.IsActive,"flee pauses flow");Check(GameStateManager.Instance.CombatEntries==1,"flee must not immediately retry");Near(t.GetRestorationPercent("island_lust"),0,"flee no restoration");Near(PowerBudgetTracker.Instance.Remaining,100,"flee reservation refunded");}
    private static IslandFlowController PuzzleFlow(out IslandRestorationTracker t) {t=Tracker();t.RecordEncounterCompletion("island_lust","lust_c1",EncounterType.Combat,.125f);return Flow(Lust,t);}
    private static void GuardCannotCompletePuzzle() {var f=PuzzleFlow(out var t);GameStateManager.Instance.Busy=false;f.OnReturnFromCombat(true);Near(t.GetRestorationPercent("island_lust"),12.5f,"guard callback changes no puzzle progress");Check(f.CurrentEncounterIndex==1,"guard doesn't advance flow");}
    private static void UnsolvedPuzzle() {var f=PuzzleFlow(out var t);GameStateManager.Instance.Busy=false;f.OnReturnFromPuzzle(false);Near(t.GetRestorationPercent("island_lust"),12.5f,"abort progress");Check(!t.HasClearedEncounter("island_lust","lust_p1"),"abort receipt absent");Check(!f.IsActive,"abort returns player to exploration");}
    private static void SolvedPuzzle() {var f=PuzzleFlow(out var t);Check(GameStateManager.Instance.PendingPuzzleBoxId=="__flow_puzzle__::island_lust::lust_p1","stable puzzle key");GameStateManager.Instance.Busy=false;f.OnReturnFromPuzzle(true);Near(t.GetRestorationPercent("island_lust"),18.75f,"solved puzzle restoration");f.OnReturnFromPuzzle(true);Near(t.GetRestorationPercent("island_lust"),18.75f,"duplicate cannot complete next combat");Check(f.CurrentEncounterIndex==2,"next combat queued");}
    private static void ActiveReplacement() {var t=Tracker();var f=Flow(Lust,t);f.StartIsland(IslandThemeRegistry.GetConfig("island_greed"));Check(f.IslandId=="island_lust","cannot replace pending encounter");f.PauseForHub();GameStateManager.Instance.Busy=false;f.StartIsland(IslandThemeRegistry.GetConfig("island_greed"));Check(f.IslandId=="island_greed","paused flow can travel");}
    private static void MissingPuzzleData() {var t=Tracker();var config=new IslandConfig(){islandId="island_lust",encounters=new[]{new EncounterDefinition(){encounterId="missing",type=EncounterType.Puzzle,restorationValue=.25f}}};var f=Flow(config,t);Check(GameStateManager.Instance.PuzzleEntries==0,"missing data cannot enter");f.OnReturnFromPuzzle(true);Near(t.GetRestorationPercent("island_lust"),0,"missing data cannot resolve");}
}
