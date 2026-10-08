using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
internal static class Program
{
 internal static string RepositoryRoot=System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory,"../../../../../"));
 private static object Invoke(object target,string method,params object[] args)=>target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);
 private static void Field(object target,string field,object value)=>target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
 private static object Read(object target,string field)=>target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
 private static void QteSessionConsumesMpOnceAndResumesQueue()=>RunQteSessionScenario(false);
 private static void DisabledQteSettlesOnlyWhenReenabled()=>RunQteSessionScenario(true);
 private static void RunQteSessionScenario(bool disableAndResume)
 {
  UnityEngine.Application.isPlaying=true;
  var host=new UnityEngine.GameObject("LiveQteAdapterBattle");var heroHost=new UnityEngine.GameObject("Hero");var enemyHost=new UnityEngine.GameObject("Enemy");
  try
  {
   BattleManager battle=host.AddComponent<BattleManager>();Field(battle,"enableActionAnimations",false);
   CombatUnit hero=heroHost.AddComponent<CombatUnit>();CombatUnit enemy=enemyHost.AddComponent<CombatUnit>();
   enemy.Type=CombatUnit.UnitType.Enemy;hero.Speed=30;enemy.Speed=20;hero.Attack=enemy.Attack=10;hero.Defense=enemy.Defense=0;
   hero.CritRate=enemy.CritRate=0;hero.ElementType=CombatUnit.Element.Fire;enemy.ElementType=CombatUnit.Element.Water;
   hero.ApplyStatusEffect(new StatusEffect(StatusEffectType.Poison,1,1,"Regression"));
   var heroSkill=UnityEngine.ScriptableObject.CreateInstance<SkillData>();heroSkill.skillName="Hero flame";heroSkill.element=CombatUnit.Element.Fire;heroSkill.mpCost=4;
   var enemySkill=UnityEngine.ScriptableObject.CreateInstance<SkillData>();enemySkill.skillName="Enemy flame";enemySkill.element=CombatUnit.Element.Fire;enemySkill.mpCost=3;
   battle.RegisterUnit(hero);battle.RegisterUnit(enemy);battle.StartBattle();battle.AdvancePhase();
   ((System.Collections.Generic.Dictionary<CombatUnit,PlannedAction>)Read(battle,"selectedPlayerActions"))[hero]=new PlannedAction(CombatActionType.Skill,hero,enemy,heroSkill);
   ((System.Collections.Generic.Dictionary<CombatUnit,PlannedAction>)Read(battle,"enemyPlannedActions"))[enemy]=new PlannedAction(CombatActionType.Skill,enemy,hero,enemySkill);
   battle.AdvancePhase();Invoke(battle,"HandleActionExecution");
   Assert.IsTrue(battle.IsNeutralClashQtePending,"Neutral authored action elements must start the real session branch.");
   Assert.AreEqual(46,hero.MP);Assert.AreEqual(47,enemy.MP);Assert.AreEqual(99,hero.HP);
   var session=(QTESession)Read(battle,"activeNeutralClashQteSession");
   if(disableAndResume)
   {
    Invoke(battle,"OnDisable");session.Resolve(true);
    Assert.AreEqual(99,hero.HP);Assert.AreEqual(100,enemy.HP);
    Invoke(battle,"OnEnable");
   }
   else session.Resolve(true);
   int expectedHeroHp=disableAndResume?84:94;
   Assert.IsFalse(battle.IsNeutralClashQtePending);Assert.AreEqual(disableAndResume?95:85,enemy.HP);Assert.AreEqual(expectedHeroHp,hero.HP);
   Field(battle,"actionStepTimer",0f);Invoke(battle,"HandleActionExecution");
   Field(battle,"actionStepTimer",0f);Invoke(battle,"HandleActionExecution");
   Assert.AreEqual(BattlePhase.EndTurn,battle.CurrentPhase);Assert.AreEqual(46,hero.MP);Assert.AreEqual(47,enemy.MP);
   Assert.AreEqual(expectedHeroHp,hero.HP,"Queued QTE participants must not tick poison or resolve actions twice.");
   Assert.AreEqual(0,hero.ActiveEffects.Count);
  }
  finally { UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(heroHost);UnityEngine.Object.DestroyImmediate(enemyHost);UnityEngine.Application.isPlaying=false; }
 }
 private static void TerminalNotificationIsDeliveredOnce()
 {
  var sink=new GameStateManager();GameStateManager.Instance=sink;
  var host=new UnityEngine.GameObject("CompletionAdapterBattle");
  try { var battle=host.AddComponent<BattleManager>();battle.SetVictory();battle.SetVictory();battle.SetDefeat();Assert.AreEqual(1,sink.CombatEndedCalls); }
  finally { UnityEngine.Object.DestroyImmediate(host);GameStateManager.Instance=null; }
 }
 private static int Main(string[] args)
 {
  if(args.Length>0) RepositoryRoot=System.IO.Path.GetFullPath(args[0]);
  int count=0,failures=0;
  foreach(Type suite in new[]{typeof(BattleFlowTestSuite),typeof(CombatUnitTestSuite),typeof(TideBreakTargetingTest),typeof(CombatReadinessRegressionTest)})
  foreach(MethodInfo test in suite.GetMethods().Where(m=>m.GetCustomAttribute<TestAttribute>()!=null))
  {
   object instance=Activator.CreateInstance(suite);
   try
   {
    UnityEngine.Random.InitState(1234); UnityEngine.Application.isPlaying=false;
    foreach(var setup in suite.GetMethods().Where(m=>m.GetCustomAttribute<SetUpAttribute>()!=null)) setup.Invoke(instance,null);
    test.Invoke(instance,null);count++;Console.WriteLine("PASS "+suite.Name+"."+test.Name);
   }
   catch(Exception error) { failures++;Console.WriteLine("FAIL "+suite.Name+"."+test.Name+": "+error.GetBaseException().Message); }
   finally { foreach(var cleanup in suite.GetMethods().Where(m=>m.GetCustomAttribute<TearDownAttribute>()!=null)) cleanup.Invoke(instance,null); }
  }
  foreach(MethodInfo test in typeof(AdversarialCombatChallengerRunner).GetMethods(BindingFlags.Static|BindingFlags.NonPublic).Where(m=>m.Name.StartsWith("Test",StringComparison.Ordinal)))
  {
   try { UnityEngine.Random.InitState(1234);test.Invoke(null,null);count++;Console.WriteLine("PASS AdversarialCombatChallengerRunner."+test.Name); }
   catch(Exception error) { failures++;Console.WriteLine("FAIL AdversarialCombatChallengerRunner."+test.Name+": "+error.GetBaseException().Message); }
  }
  foreach(Action scenario in new Action[]{QteSessionConsumesMpOnceAndResumesQueue,DisabledQteSettlesOnlyWhenReenabled,TerminalNotificationIsDeliveredOnce})
  {
   try { scenario();count++;Console.WriteLine("PASS AdapterScenario."+scenario.Method.Name); }
   catch(Exception error) { failures++;Console.WriteLine("FAIL AdapterScenario."+scenario.Method.Name+": "+error.GetBaseException().Message); }
  }
  Console.WriteLine($"Actual-source combat checks: {count} passed, {failures} failed. Unity adapters; Editor/build verification is still required.");
  return failures==0?0:1;
 }
}
