using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

internal static class Program
{
    private static int checks;
    private static void Main()
    {
        PendingReplayAndDeferredDelivery();
        DeferredFlagsUseLatestValue();
        ReentrantRewardSavesCarryTheLedger();
        ChoicesCannotFarmBonds();
        AutomaticCyclesFinishSafely();
        MissingAuthoredEntriesAreGuarded();
        ResetCancelsDialogueAndRestoresMovement();
        SequenceCompletionAllowsReentry();
        StorySnapshotsAreBounded();
        AffinitySnapshotsReplaceAndClamp();
        AuthoredHeroPairBondsReachCombat();
        CanonicalEndingHistory();
        InterruptedNarrativeBeatReplays();
        TriggerCancellationRetriesUnseenConversation();
        TriggerCompletionPersistsPairBond();
        AuthoredContentWiring();
        Console.WriteLine($"Narrative actual-source harness passed {checks} checks.");
    }

    private static void Reset()
    {
        if (DialogueSystem.Instance != null) Call(DialogueSystem.Instance,"OnDestroy");
        SetInstance(typeof(StoryProgressionService),null);
        SetInstance(typeof(RelationshipTracker),null);
        SetInstance(typeof(EndingEvaluator),null);
        UnityEngine.Object.Components.Clear();
        Debug.Errors.Clear();
        PlayerPrefs.Reset();
        HeroProgressionManager.Instance=null;
        PlayerGearInventory.Instance=null;
        TideBreakProgressionManager.Instance=null;
        GameStateManager.Instance=new GameStateManager();
        new GameObject("Dialogue").AddComponent<DialogueSystem>();
    }

    private static DialogueTreeEffect Xp() => new DialogueTreeEffect { type=DialogueEffectType.GrantXP,targetId="hero_fire",intValue=50 };
    private static void PendingReplayAndDeferredDelivery()
    {
        Reset();
        DialogueSystem d=DialogueSystem.Instance;
        d.ApplyDialogueEffect(Xp(),"hero_fire","pending","node",0);
        d.ApplyDialogueEffect(Xp(),"hero_fire","pending","node",0);
        var saved=d.CaptureDialogueStateSaveData();
        Equal(1,saved.pendingRewards.Count,"replayed failed effect keeps one pending reward");
        saved.pendingRewards[0].intValue=999;
        Equal(50,d.CaptureDialogueStateSaveData().pendingRewards[0].intValue,"captured pending reward is detached");
        saved=d.CaptureDialogueStateSaveData();
        saved.pendingRewards.Add(saved.pendingRewards[0]);
        d.ApplyDialogueStateSaveData(saved);
        Equal(1,d.CaptureDialogueStateSaveData().pendingRewards.Count,"legacy failed duplicates coalesce on restore");
        HeroProgressionManager.Instance=new HeroProgressionManager();
        d.ApplyDialogueStateSaveData(d.CaptureDialogueStateSaveData());
        Equal(50,HeroProgressionManager.Instance.Xp,"deferred XP is delivered once");
        d.ApplyDialogueEffect(Xp(),"hero_fire","pending","node",0);
        Equal(50,HeroProgressionManager.Instance.Xp,"delivered reward is protected on tree replay");
        Equal(0,d.CaptureDialogueStateSaveData().pendingRewards.Count,"delivered reward leaves pending ledger");
    }

    private static DialogueSystem.DialoguePendingRewardEntry PendingFlag(int value) => new DialogueSystem.DialoguePendingRewardEntry
    { treeId="flags",nodeId="flag",effectIndex=value,effectType=(int)DialogueEffectType.SetFlag,targetId="quest",intValue=value };
    private static void DeferredFlagsUseLatestValue()
    {
        Reset();
        var story=new GameObject("Story").AddComponent<StoryProgressionService>();
        var saved=new DialogueSystem.DialogueStateSaveData
        { setFlagIds=new List<string>{"quest"},setFlagValues=new List<bool>{false},pendingRewards=new List<DialogueSystem.DialoguePendingRewardEntry>{PendingFlag(1)} };
        DialogueSystem.Instance.ApplyDialogueStateSaveData(saved);
        Check(!story.GetFlag("quest"),"stale deferred true cannot overwrite saved false");
        saved=new DialogueSystem.DialogueStateSaveData
        { pendingRewards=new List<DialogueSystem.DialoguePendingRewardEntry>{PendingFlag(1),PendingFlag(0)} };
        DialogueSystem.Instance.ApplyDialogueStateSaveData(saved);
        Check(!story.GetFlag("quest"),"legacy pending-only flags use the last write");
        SetInstance(typeof(StoryProgressionService),null);
        DialogueSystem.Instance.ApplyDialogueEffect(new DialogueTreeEffect {type=DialogueEffectType.SetFlag,targetId="other",intValue=1},null,"one","node",0);
        DialogueSystem.Instance.ApplyDialogueEffect(new DialogueTreeEffect {type=DialogueEffectType.SetFlag,targetId="other",intValue=0},null,"two","node",0);
        var pending=DialogueSystem.Instance.CaptureDialogueStateSaveData().pendingRewards;
        Equal(1,pending.Count,"latest deferred flag replaces old writes across trees");
        Equal(0,pending[0].intValue,"latest deferred flag value retained");
    }

    private static void ReentrantRewardSavesCarryTheLedger()
    {
        Reset();
        HeroProgressionManager.Instance=new HeroProgressionManager();
        var effect=new DialogueTreeEffect {type=DialogueEffectType.GiveItem,targetId="coin",intValue=7};
        bool observedLedger=false;
        GameStateManager.Instance.OnSave=()=>observedLedger|=DialogueSystem.Instance.HasGrantedReward("currency|node|0");
        HeroProgressionManager.Instance.OnCurrencyAdded=()=>DialogueSystem.Instance.ApplyDialogueEffect(effect,null,"currency","node",0);
        DialogueSystem.Instance.ApplyDialogueEffect(effect,null,"currency","node",0);
        Equal(7,HeroProgressionManager.Instance.Currency,"synchronous callback cannot double award currency");
        Check(observedLedger,"service-initiated save sees the reward ledger");
    }

    private static DialogueTree ChoiceTree()
    {
        var root=new DialogueTreeNode {nodeId="choice",entry=new DialogueSystem.DialogueEntry{relatedHeroId="hero_fire"},
            choices=new[]{new DialogueTreeChoice{choiceText="Together",nextNodeId="end",increasesBond=true,bondAmount=5}}};
        var end=new DialogueTreeNode {nodeId="end",entry=new DialogueSystem.DialogueEntry{speakerName="Fire",dialogueText="Ready"}};
        return new DialogueTree{treeId="choice_tree",rootNode=root,allNodes=new List<DialogueTreeNode>{root,end}};
    }
    private static void ChoicesCannotFarmBonds()
    {
        Reset();
        var tree=ChoiceTree();
        var runner=new GameObject("Runner").AddComponent<DialogueTreeRunner>();
        runner.StartTree(tree);
        Check((bool)Get(runner,"waitingForChoice"),"empty decision node presents its choices");
        Call(runner,"OnChoiceSelected",tree.rootNode.choices[0]);
        Call(runner,"OnChoiceSelected",tree.rootNode.choices[0]);
        Equal(5,DialogueSystem.Instance.GetBondLevel("hero_fire","player"),"stale double-click cannot award twice");
        var replay=new GameObject("Replay").AddComponent<DialogueTreeRunner>();
        replay.StartTree(tree);
        Call(replay,"OnChoiceSelected",tree.rootNode.choices[0]);
        Equal(5,DialogueSystem.Instance.GetBondLevel("hero_fire","player"),"replaying choice does not farm bonds");
        var save=DialogueSystem.Instance.CaptureDialogueStateSaveData();
        DialogueSystem.Instance.ResetDialogueStateForDebug();
        DialogueSystem.Instance.ApplyDialogueStateSaveData(save);
        Check(DialogueSystem.Instance.HasGrantedReward("choice_tree|choice|-1"),"choice award replay key survives restore");
    }

    private static void AutomaticCyclesFinishSafely()
    {
        Reset();
        var a=new DialogueTreeNode {nodeId="a",nextNodeId="b"};
        var b=new DialogueTreeNode {nodeId="b",nextNodeId="a"};
        var runner=new GameObject("Cycle").AddComponent<DialogueTreeRunner>();
        int completed=0;
        runner.OnTreeCompleted+=id=>completed++;
        runner.StartTree(new DialogueTree{treeId="cycle",rootNode=a,allNodes=new List<DialogueTreeNode>{a,b}});
        Equal(1,completed,"silent cycle completes once without stack overflow");
        Check(Debug.Errors.Exists(s=>s.Contains("Automatic navigation limit")),"cycle surfaces authored content error");
        var fail=new DialogueTreeNode {nodeId="fail",nextNodeId="fail",conditions=new DialogueTreeCondition[]{null}};
        var runner2=new GameObject("ConditionCycle").AddComponent<DialogueTreeRunner>();
        runner2.StartTree(new DialogueTree{treeId="condition_cycle",rootNode=fail,allNodes=new List<DialogueTreeNode>{fail}});
        Check((bool)Get(runner2,"isCompleted"),"failed condition cycle terminates");
    }

    private static void MissingAuthoredEntriesAreGuarded()
    {
        Reset();
        var tree=ChoiceTree();
        tree.rootNode.choices=new[]{null,tree.rootNode.choices[0]};
        tree.rootNode.effects=new DialogueTreeEffect[]{null};
        var runner=new GameObject("NullEntries").AddComponent<DialogueTreeRunner>();
        runner.StartTree(tree);
        Check((bool)Get(runner,"waitingForChoice"),"null authored choices/effects are safely skipped");
        var node=new DialogueTreeNode {conditions=new[]{new DialogueTreeCondition{type=(DialogueConditionType)999}}};
        Check(!(bool)Call(runner,"EvaluateConditions",node),"unknown condition fails closed");
    }

    private static void ResetCancelsDialogueAndRestoresMovement()
    {
        Reset();
        var player=new GameObject("Player").AddComponent<IsometricPlayer>();
        int complete=0;
        DialogueSystem.Instance.OnDialogueTreeCompleted+=id=>complete++;
        DialogueSystem.Instance.StartDialogueTree(ChoiceTree());
        Check(!player.canMove,"dialogue locks movement");
        DialogueSystem.Instance.ResetDialogueStateForDebug();
        Check(player.canMove,"reset unwinds both dialogue movement locks");
        Equal(0,complete,"cancelled dialogue is not reported completed");
        Check(!DialogueSystem.Instance.IsDialogueActive,"reset clears active conversation");
        DialogueSystem.Instance.IncreaseBond("hero_fire","hero_water",10);
        DialogueSystem.Instance.ApplyDialogueStateSaveData(null);
        Equal(0,DialogueSystem.Instance.GetBondLevel("hero_fire","hero_water"),"missing save snapshot clears newer bonds");
        DialogueSystem.Instance.IncreaseBond("hero_fire","hero_water",100);
        DialogueSystem.Instance.IncreaseBond("hero_fire","hero_water",int.MaxValue);
        Equal(100,DialogueSystem.Instance.GetBondLevel("hero_fire","hero_water"),"bond overflow saturates instead of wrapping");
    }

    private static void SequenceCompletionAllowsReentry()
    {
        Reset();
        int completed=0;
        var first=new List<DialogueSystem.DialogueEntry>{new DialogueSystem.DialogueEntry{dialogueText="First"}};
        var second=new List<DialogueSystem.DialogueEntry>{new DialogueSystem.DialogueEntry{dialogueText="Second"}};
        DialogueSystem.Instance.OnDialogueCompleted+=entries=>{completed++; if(completed==1)DialogueSystem.Instance.ShowDialogue(second);};
        DialogueSystem.Instance.ShowDialogue(first);
        var ui=UnityEngine.Object.FindFirstObjectByType<DialogueUI>();
        Call(ui,"AdvanceEntry");
        Check(DialogueSystem.Instance.IsDialogueActive,"completion listener can start another sequence");
        Check(ReferenceEquals(Get(DialogueSystem.Instance,"pendingEntries"),second),"new pending sequence survives old completion");
        Check(ReferenceEquals(Get(ui,"currentEntries"),second),"UI retains reentrant sequence after callback");
        Call(ui,"AdvanceEntry");
        Equal(2,completed,"both sequences complete once");
    }

    private static void StorySnapshotsAreBounded()
    {
        Reset();
        var story=new GameObject("Story").AddComponent<StoryProgressionService>();
        story.ApplySnapshot(new StoryProgressionService.StoryProgressionSnapshot{currentAct=99,highestActReached=-5,endingBranch="999",endingTriggered=true});
        Equal(StoryProgressionService.StoryAct.ActIII,story.CurrentAct,"restored act clamps to defined range");
        Equal(story.CurrentAct,story.HighestActReached,"highest act cannot precede current act");
        Equal(StoryProgressionService.EndingBranch.None,story.ResolvedEndingBranch,"numeric unknown ending is rejected");
        Check(!story.IsEndingTriggered,"invalid ending cannot remain triggered");
        story.SetFlag("old",true);
        story.ApplySnapshot(null);
        Check(!story.GetFlag("old"),"missing story snapshot clears stale flags");
        GameStateManager.Instance.CurrentStoryAct=GameStateManager.StoryAct.ActI;
        GameStateManager.Instance.HighestStoryActReached=GameStateManager.StoryAct.ActIII;
        story.SyncFromGameStateManager();
        Equal(StoryProgressionService.StoryAct.ActIII,story.HighestActReached,"backtracking preserves highest act from game state");
    }

    private static void AffinitySnapshotsReplaceAndClamp()
    {
        Reset();
        var tracker=new GameObject("Affinity").AddComponent<RelationshipTracker>();
        tracker.SetAffinity("hero_fire",90);
        tracker.AdjustAffinity("hero_fire",int.MaxValue);
        Equal(100,tracker.GetAffinity("hero_fire"),"affinity overflow saturates");
        var detached=tracker.Snapshot();
        tracker.SetAffinity("hero_fire",1);
        Equal(100,detached["hero_fire"],"affinity dictionary snapshot is detached");
        tracker.ApplySnapshot(new RelationshipAffinitySnapshot{heroIds=new List<string>{"hero_water","","hero_air"},affinities=new List<int>{999,50,-100}});
        Equal(0,tracker.GetAffinity("hero_fire"),"world snapshot replaces newer sidecar affinity");
        Equal(100,tracker.GetAffinity("hero_water"),"affinity restore clamps high values");
        Equal(0,tracker.GetAffinity("hero_air"),"affinity restore clamps low values");
        var snapshot=tracker.CaptureSnapshot();
        Equal(2,snapshot.heroIds.Count,"empty hero ids are rejected on restore");
        tracker.ApplySnapshot(null);
        Equal(0,tracker.Snapshot().Count,"legacy missing affinity snapshot clears state");
        foreach(string invalid in new[]{"{","{\"heroIds\":[],\"affinities\":{}}","{\"heroIds\":[true],\"affinities\":[10]}"})
        {
            PlayerPrefs.SetString("RelationshipTracker_AffinityData",invalid);
            Call(tracker,"RestoreAffinityFromPrefs");
            Equal(0,tracker.Snapshot().Count,"corrupt sidecar does not throw or poison affinity before world load");
        }
    }

    private static void CanonicalEndingHistory()
    {
        Reset();
        var evaluator=new GameObject("Ending").AddComponent<EndingEvaluator>();
        Equal(EndingOutcome.Unresolved,evaluator.EvaluateOutcome(null),"missing world cannot resolve ending");
        Equal(EndingOutcome.Unresolved,EndingEvaluator.EvaluateOutcomeFromSnapshot(evaluator.BuildSnapshot(null)),"snapshot also retains missing-world status");
        Equal(EndingOutcome.GoodEnding,evaluator.EvaluateOutcome(GameStateManager.Instance),"no minimum-only history does not cause bad ending");
        GameStateManager.Instance.ThresholdBossIds=new List<string>(IslandThemeRegistry.ProgressionOrder).ToArray();
        Equal(EndingOutcome.BadEnding,evaluator.EvaluateOutcome(GameStateManager.Instance),"minimum-only history on all required islands causes bad ending");
        GameStateManager.Instance.ThresholdBossIds=new[]{"island_lust","island_greed"};
        Equal(EndingOutcome.GoodEnding,evaluator.EvaluateOutcome(GameStateManager.Instance),"partial minimum-only history does not cause bad ending");
        GameStateManager.Instance.FinalLosses=3;
        Equal(EndingOutcome.GoodEnding,evaluator.EvaluateOutcome(GameStateManager.Instance),"three final defeats do not trigger V2 bad ending");
        GameStateManager.Instance.FinalLosses=4;
        Equal(EndingOutcome.BadEnding,evaluator.EvaluateOutcome(GameStateManager.Instance),"fourth final defeat triggers V2 bad ending");
        GameStateManager.Instance.FinalLosses=0;
        GameStateManager.Instance.MinimumRestorationBadEndingRule=GameStateManager.MinimumRestorationBadEndingRuleMode.ProceededAtThreshold;
        GameStateManager.Instance.ThresholdProceedIds=new[]{"island_lust","island_greed","island_desire","island_anger","island_envy"};
        Equal(EndingOutcome.BadEnding,evaluator.EvaluateOutcome(GameStateManager.Instance),"departure history excludes final island as runtime does");
    }

    private static void AuthoredHeroPairBondsReachCombat()
    {
        Reset();
        var d=DialogueSystem.Instance;
        d.ApplyDialogueEffect(new DialogueTreeEffect{type=DialogueEffectType.IncreaseBond,targetId="hero_fire|hero_water",intValue=70},null,"pair","gain",0);
        var allies=new[]{new CombatUnit{HeroId="hero_fire",UnitName="Ember"},new CombatUnit{HeroId="hero_water",UnitName="Tidecaller"}};
        Equal(1.1f,RelationshipCombatEffects.GetTeamDamageMultiplier(allies),"canonical pair bond reaches combat despite different display names");
        d.ApplyDialogueEffect(new DialogueTreeEffect{type=DialogueEffectType.IncreaseBond,targetId="hero_fire|hero_water",intValue=-10},null,"pair","conflict",0);
        Equal(60,d.GetBondLevel("hero_water","hero_fire"),"authored conflict can lower the stable pair bond");
        var save=d.CaptureDialogueStateSaveData();
        d.ResetDialogueStateForDebug();
        d.ApplyDialogueStateSaveData(save);
        Equal(1f,RelationshipCombatEffects.GetTeamDamageMultiplier(allies),"pair bond combat tier survives save/restore");
        d.ApplyDialogueEffect(new DialogueTreeEffect{type=DialogueEffectType.IncreaseBond,targetId="hero_fire|hero_water",intValue=-10},null,"pair","conflict",0);
        Equal(60,d.GetBondLevel("hero_fire","hero_water"),"replaying conflict cannot lower bonds twice");
    }

    private static void InterruptedNarrativeBeatReplays()
    {
        Reset();
        var director=new GameObject("Narrative").AddComponent<NarrativeBeatDirector>();
        Check(director.ForceShowActThreeBeatForDebug(),"authored act-three conversation starts");
        Check(!GameStateManager.Instance.IsNarrativeBeatCompleted(NarrativeBeatDirector.ActThreeBeatIdPublic),"starting a conversation does not mark unseen dialogue completed");
        DialogueSystem.Instance.ResetDialogueStateForDebug();
        Check(director.ForceShowActThreeBeatForDebug(),"interrupted beat can replay");
        var tree=HeroDialogueContent.AncientTextReactionActIII();
        Call(DialogueSystem.Instance,"HandleTreeCompleted",tree.treeId);
        Check(GameStateManager.Instance.IsNarrativeBeatCompleted(NarrativeBeatDirector.ActThreeBeatIdPublic),"completed authored dialogue records its beat");
    }

    private static void AuthoredContentWiring()
    {
        Reset();
        foreach(string id in IslandThemeRegistry.ProgressionOrder)
        {
            Check(NarrativeBeatDirector.PlayPreBossDialogue(id),$"canonical pre-boss tree starts for {id}");
            DialogueSystem.Instance.ResetDialogueStateForDebug();
        }
        foreach(var factory in new Func<DialogueTree>[] {HeroDialogueContent.CharacterIntroDialogue,HeroDialogueContent.AncientTextReactionActI,HeroDialogueContent.AncientTextReactionActII,HeroDialogueContent.AncientTextReactionActIII})
        {
            var tree=factory();
            Check(tree.rootNode!=null && !string.IsNullOrEmpty(tree.treeId),$"authored tree {tree.treeId} has identity/root");
            var nodes=new HashSet<string>();
            foreach(var node in tree.allNodes) Check(nodes.Add(node.nodeId),$"authored {tree.treeId} node {node.nodeId} has unique identity");
            foreach(var node in tree.allNodes)
            {
                Check(string.IsNullOrEmpty(node.nextNodeId)||nodes.Contains(node.nextNodeId),$"authored {tree.treeId} next target resolves");
                if(node.choices!=null)foreach(var choice in node.choices)Check(string.IsNullOrEmpty(choice.nextNodeId)||nodes.Contains(choice.nextNodeId),$"authored {tree.treeId} choice target resolves");
            }
        }
    }

    private static void TriggerCancellationRetriesUnseenConversation()
    {
        Reset();
        var trigger=new GameObject("Trigger").AddComponent<DialogueTrigger>();
        var tree=ChoiceTree();
        Set(trigger,"dialogueTree",tree);
        Set(trigger,"playerInRange",true);
        Set(trigger,"entryDelay",0f);
        Call(trigger,"StartDialogue");
        Check(!(bool)Get(trigger,"hasPlayed"),"starting a one-shot trigger does not mark unseen dialogue played");
        Call(trigger,"HandleTreeDialogueCompleted","unrelated_tree");
        Check((bool)Get(trigger,"dialoguePending"),"unrelated completion cannot finish a pending trigger");
        DialogueSystem.Instance.ResetDialogueStateForDebug();
        Call(trigger,"Update");
        Check(DialogueSystem.Instance.IsDialogueActive,"cancelled trigger recovers and restarts its conversation");
        Call(DialogueSystem.Instance,"HandleTreeCompleted",tree.treeId);
        Check((bool)Get(trigger,"hasPlayed"),"completed trigger records one-shot state");
        Check(!(bool)Get(trigger,"dialoguePending"),"completed trigger releases pending state");
    }

    private static void TriggerCompletionPersistsPairBond()
    {
        Reset();
        var trigger=new GameObject("BondTrigger").AddComponent<DialogueTrigger>();
        Set(trigger,"awardBondOnCompletion",true);
        trigger.AddDialogueEntry("Fire","Ready",relatedHeroId:"hero_fire");
        trigger.AddDialogueEntry("Water","Together",relatedHeroId:"hero_water");
        DialogueSystem.DialogueStateSaveData durable=null;
        GameStateManager.Instance.OnSave=()=>durable=DialogueSystem.Instance.CaptureDialogueStateSaveData();
        Call(trigger,"StartDialogue");
        var ui=UnityEngine.Object.FindFirstObjectByType<DialogueUI>();
        Call(ui,"AdvanceEntry");
        Call(ui,"AdvanceEntry");
        Equal(5,DialogueSystem.Instance.GetBondLevel("hero_fire","hero_water"),"flat trigger awards the first authored hero pair");
        Check(durable!=null,"flat conversation completion autosaves its outcome");
        DialogueSystem.Instance.ResetDialogueStateForDebug();
        DialogueSystem.Instance.ApplyDialogueStateSaveData(durable);
        Equal(5,DialogueSystem.Instance.GetBondLevel("hero_fire","hero_water"),"immediate completion save includes the awarded pair bond");
    }

    private static object Call(object target,string name,params object[] args) => target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);
    private static object Get(object target,string name) => target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    private static void Set(object target,string name,object value) => target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    private static void SetInstance(Type type,object value) => type.GetProperty("Instance").GetSetMethod(true).Invoke(null,new[]{value});
    private static void Equal<T>(T expected,T actual,string message) { Check(EqualityComparer<T>.Default.Equals(expected,actual),$"{message}: expected {expected}, got {actual}"); }
    private static void Check(bool condition,string message) { checks++;if(!condition)throw new InvalidOperationException(message); }
}
