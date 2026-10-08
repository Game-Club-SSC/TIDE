using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using UnityEngine;

internal static class Program
{
    private const string Key = "TIDE_WORLD_STATE_V2";
    private const string Backup = Key + "_backup";
    private const string A = "{\"puzzleStates\":[],\"generation\":1}";
    private const string B = "{\"puzzleStates\":[],\"generation\":2}";
    private const string NgKey = "NewGamePlusService";
    private static int checks;

    private static void Main()
    {
        ValidateJsonGrammarAndSchema();
        DifferentialJsonSyntaxCoverage();
        RoundTripAndGenerationBackup();
        CorruptionAndMissingPrimaryRecovery();
        FailedRetriesPreserveKnownGoodGeneration();
        BackupFailureNeverMutatesPrimary();
        FutureSchemaIsPreserved();
        DisabledAndClearBehaviour();
        PeekNeverChangesStorageOrEvents();
        NewGamePlusCorruptionAndLifecycle();
        NewGamePlusBounds();
        NewGamePlusWorldSnapshots();
        Console.WriteLine($"PASS PersistenceHarness: {checks} assertions against shipped persistence sources with PlayerPrefs/Unity test doubles.");
        Console.WriteLine("This does not verify Unity JsonUtility, platform durability, scene lifecycle, or full GameStateManager restoration.");
    }

    private static void ValidateJsonGrammarAndSchema()
    {
        string[] valid =
        {
            A, " \n {\"saveSchemaVersion\":1,\"puzzleStates\":[]} \t",
            "{\"puzzleStates\":[],\"nested\":{\"name\":\"escaped \\\" \\\\ \\u263a\",\"values\":[-2,0,1.23e+4,true,false,null]}}",
            "{\"puzzle\\u0053tates\":[]}", "{\"puzzleStates\":[],\"text\":\"saveSchemaVersion\"}"
        };
        foreach (string json in valid) Check(SaveJsonValidator.TryValidateWorldState(json, 1, out _), "Valid JSON accepted: " + json);
        string[] invalid =
        {
            null, "", "{}", "{\"puzzleStates\":[}", "{\"puzzleStates\":[]", "garbage {\"puzzleStates\":[]}",
            "{\"puzzleStates\":[]} trailing", "[ {\"puzzleStates\":[]} ]", "{\"puzzleStates\":null}",
            "{\"puzzleStates\":[],}", "{\"puzzleStates\":[1,]}", "{\"puzzleStates\":[],\"n\":01}",
            "{\"puzzleStates\":[],\"n\":1.}", "{\"puzzleStates\":[],\"n\":1e}", "{\"puzzleStates\":[],\"n\":NaN}",
            "{\"puzzleStates\":[],\"s\":\"bad\\x\"}", "{\"puzzleStates\":[],\"s\":\"line\nbreak\"}",
            "{\"puzzleStates\":[],\"puzzle\\u0053tates\":[]}", "{\"puzzleStates\":[],\"nested\":{\"a\":1,\"a\":2}}",
            "{\"puzzleStates\":[],\"saveSchemaVersion\":2}", "{\"puzzleStates\":[],\"saveSchemaVersion\":0}",
            "{\"puzzleStates\":[],\"saveSchemaVersion\":-1}", "{\"puzzleStates\":[],\"saveSchemaVersion\":1.0}",
            "{\"puzzleStates\":[],\"saveSchemaVersion\":\"1\"}", "{\"puzzleStates\":[],\"saveSchemaVersion\":2147483648}"
        };
        foreach (string json in invalid) Check(!SaveJsonValidator.TryValidateWorldState(json, 1, out _), "Malformed or unsupported JSON rejected.");
        Check(!SaveJsonValidator.TryValidateWorldState("{\"puzzleStates\":" + new string('[', 80) + "0" + new string(']', 80) + "}", 1, out _), "Deep nesting rejected without stack overflow.");
        Check(!SaveJsonValidator.TryValidateWorldState(new string(' ', 4 * 1024 * 1024 + 1), 1, out _), "Oversize input rejected.");
    }

    private static void DifferentialJsonSyntaxCoverage()
    {
        Random random = new Random(71491);
        const string alphabet = "{}[],:\"\\0123456789eEtfnrual -+\n\txyz";
        const string seed = "{\"puzzleStates\":[{\"text\":\"line\\nslash\\\\unicode\\u263a\",\"values\":[0,-4,3.14e+2,true,false,null]}],\"n\":1}";
        for (int i = 0; i < 2000; i++)
        {
            string candidate = seed;
            for (int mutations = random.Next(1, 5); mutations > 0; mutations--)
            {
                int at = random.Next(candidate.Length);
                if (random.Next(2) == 0) candidate = candidate.Remove(at, 1);
                else candidate = candidate.Substring(0, at) + alphabet[random.Next(alphabet.Length)] + candidate.Substring(at + 1);
            }
            bool expected;
            try
            {
                using (JsonDocument document = JsonDocument.Parse(candidate))
                    expected = document.RootElement.ValueKind == JsonValueKind.Object;
            }
            catch (JsonException) { expected = false; }
            Check(SaveJsonValidator.TryReadObject(candidate, out _) == expected,
                "Bounded JSON reader disagrees with independent JSON parser for deterministic mutation: " + candidate);
        }
    }

    private static void RoundTripAndGenerationBackup()
    {
        PlayerPrefs.Reset();
        WorldSaveService service = new WorldSaveService();
        int persisted = 0;
        service.OnSavePersisted += _ => persisted++;
        Check(service.TryWriteJson(" \n" + A), "Legacy save writes despite leading whitespace.");
        string first = PlayerPrefs.GetString(Key);
        Check(first.StartsWith("{\"saveSchemaVersion\":1,"), "Schema injected at root.");
        PlayerPrefs.Restart();
        Check(service.TryLoadJson(out string loaded) && loaded == first, "Round trip through flushed storage.");
        Check(service.TryWriteJson(B), "Second generation writes.");
        Check(PlayerPrefs.GetString(Backup) == first, "Backup is previous generation.");
        Check(persisted == 2, "Success event once per write.");
        Check(service.TryWriteJson(B) && PlayerPrefs.GetString(Backup) == first,
            "Identical autosave preserves previous distinct generation.");
        Check(!service.TryWriteJson("{\"puzzleStates\":[}"), "Invalid payload cannot overwrite primary or backup.");
        Check(PlayerPrefs.GetString(Backup) == first, "Invalid write leaves backup unchanged.");
    }

    private static void CorruptionAndMissingPrimaryRecovery()
    {
        foreach (string bad in new[] { null, "", "{\"puzzleStates\":[}" })
        {
            PlayerPrefs.Reset();
            WorldSaveService service = new WorldSaveService();
            PlayerPrefs.SetString(Backup, A);
            if (bad != null) PlayerPrefs.SetString(Key, bad);
            PlayerPrefs.Save();
            Check(service.HasPersistedData, "Backup alone counts as persisted data.");
            Check(service.TryLoadJson(out string loaded) && loaded == A, "Backup loads from missing, empty or corrupt primary.");
            PlayerPrefs.Restart();
            Check(PlayerPrefs.GetString(Key) == A, "Recovery repair is flushed.");
        }
        PlayerPrefs.Reset();
        WorldSaveService failed = new WorldSaveService();
        PlayerPrefs.SetString(Key, "{\"puzzleStates\":[}");
        PlayerPrefs.SetString(Backup, "bad");
        Check(!failed.TryLoadJson(out string output) && output == null, "Invalid primary plus invalid backup fails closed with null output.");
        PlayerPrefs.Reset();
        PlayerPrefs.SetString(Backup, A);
        PlayerPrefs.BeforeSet = (key, value) => key == Key ? throw new PlayerPrefsException("quota") : value;
        Check(new WorldSaveService().TryLoadJson(out output) && output == A, "Backup can be loaded even when storage rejects primary repair.");
    }

    private static void FailedRetriesPreserveKnownGoodGeneration()
    {
        foreach (bool throws in new[] { true, false })
        {
            PlayerPrefs.Reset();
            WorldSaveService service = new WorldSaveService();
            Check(service.TryWriteJson(A), "Initial known-good generation writes.");
            string knownGood = PlayerPrefs.GetString(Key);
            int remaining = 3;
            PlayerPrefs.BeforeSet = (key, value) =>
            {
                if (key != Key || remaining-- <= 0) return value;
                if (throws) throw new PlayerPrefsException("quota");
                return "corrupt interrupted write";
            };
            int succeeded = 0, failed = 0;
            service.OnSavePersisted += _ => succeeded++;
            service.OnSaveFailed += _ => failed++;
            Check(!service.TryWriteJson(B), "All write retries fail cleanly.");
            Check(PlayerPrefs.GetString(Backup) == knownGood, "Retries preserve known-good backup.");
            Check(PlayerPrefs.GetString(Key) == knownGood, "Failed generation rolls back to known-good primary.");
            PlayerPrefs.Restart();
            Check(service.TryLoadJson(out string loaded) && loaded == knownGood, "Failure recovery survives storage reload.");
            Check(failed == 1 && succeeded == 0, "One failure notification, no success notification.");
        }
    }

    private static void BackupFailureNeverMutatesPrimary()
    {
        PlayerPrefs.Reset();
        WorldSaveService service = new WorldSaveService();
        Check(service.TryWriteJson(A), "Seed initial generation.");
        string primary = PlayerPrefs.GetString(Key);
        PlayerPrefs.BeforeSet = (key, value) => key == Backup ? throw new PlayerPrefsException("quota") : value;
        Check(!service.TryWriteJson(B), "Failure preserving old generation aborts new write.");
        Check(PlayerPrefs.GetString(Key) == primary, "Primary not overwritten before backup flush succeeds.");
    }

    private static void FutureSchemaIsPreserved()
    {
        PlayerPrefs.Reset();
        string future = "{\"saveSchemaVersion\":2,\"puzzleStates\":[]}";
        PlayerPrefs.SetString(Key, future);
        PlayerPrefs.SetString(Backup, A);
        WorldSaveService service = new WorldSaveService();
        Check(service.TryLoadJson(out string old) && old == A, "Supported older backup is available.");
        Check(PlayerPrefs.GetString(Key) == future, "Opening older build preserves newer-schema primary.");
        Check(!service.TryWriteJson(B) && PlayerPrefs.GetString(Key) == future, "Autosave cannot downgrade newer schema.");
    }

    private static void DisabledAndClearBehaviour()
    {
        PlayerPrefs.Reset();
        PlayerPrefs.SetString(Backup, A);
        WorldSaveService service = new WorldSaveService();
        service.SetPersistentSaveEnabled(false);
        Check(!service.TryLoadJson(out _) && !service.TryWriteJson(B), "Disabled service performs no load/write.");
        Check(PlayerPrefs.GetString(Backup) == A, "Disabled service leaves storage unchanged.");
        int cleared = 0;
        service.OnSaveCleared += () => cleared++;
        service.Clear(); service.Clear();
        Check(!service.HasPersistedData && cleared == 1, "Orphaned backup cleared exactly once.");
    }

    private static void PeekNeverChangesStorageOrEvents()
    {
        foreach (string primary in new[] { null, "", "broken", A, "{\"saveSchemaVersion\":2,\"puzzleStates\":[]}" })
        {
            PlayerPrefs.Reset();
            if (primary != null) PlayerPrefs.SetString(Key, primary);
            PlayerPrefs.SetString(Backup, B);
            PlayerPrefs.Save();
            WorldSaveService service = new WorldSaveService();
            int loaded = 0, persisted = 0, failed = 0;
            service.OnSaveLoaded += _ => loaded++;
            service.OnSavePersisted += _ => persisted++;
            service.OnSaveFailed += _ => failed++;
            int saves = PlayerPrefs.SaveCount;
            Check(service.TryPeekJson(out string json) && json == (primary == A ? A : B), "Peek selects supported primary or backup.");
            Check(PlayerPrefs.GetString(Key, null) == primary && PlayerPrefs.GetString(Backup) == B && PlayerPrefs.SaveCount == saves,
                "Validation never repairs, writes, or flushes PlayerPrefs.");
            Check(loaded == 0 && persisted == 0 && failed == 0, "Validation never emits save/load/failure events.");
        }
    }

    private static void NewGamePlusCorruptionAndLifecycle()
    {
        foreach (string corrupt in new[] { "bad", "{}", "{\"completedRuns\":-1,\"loopIndex\":0,\"isInNewGamePlus\":false}", "{\"completedRuns\":1,\"loopIndex\":2,\"isInNewGamePlus\":true}", "{\"completedRuns\":1,\"loopIndex\":0,\"isInNewGamePlus\":true}", "{\"completedRuns\":\"1\",\"loopIndex\":0,\"isInNewGamePlus\":false}" })
        {
            PlayerPrefs.Reset();
            PlayerPrefs.SetString(NgKey, corrupt);
            NewGamePlusService service = new NewGamePlusService();
            Invoke(service, "Load");
            Check(service.CompletedRuns == 0 && service.LoopIndex == 0 && !service.IsInNewGamePlus, "Corrupt NG+ data restores normal defaults.");
        }
        PlayerPrefs.Reset();
        NewGamePlusService ng = new NewGamePlusService();
        Check(!ng.StartNewGamePlus(), "NG+ requires a completion.");
        ng.RegisterCompletion(); ng.RegisterCompletion();
        Check(ng.CompletedRuns == 1, "Repeated ending/completion callback counts once.");
        PlayerPrefs.Restart();
        NewGamePlusService restored = new NewGamePlusService(); Invoke(restored, "Load");
        Check(restored.CanStartNewGamePlus() && restored.CompletedRuns == 1, "Completion and eligibility survive reload.");
        Check(restored.StartNewGamePlus() && restored.LoopIndex == 1, "First NG+ starts.");
        Check(!restored.StartNewGamePlus() && restored.LoopIndex == 1, "Repeated start cannot advance active unfinished run.");
        restored.EndNewGamePlus();
        Check(!restored.StartNewGamePlus(), "Ending mode cannot fabricate a completed run.");
        restored.RegisterCompletion();
        Check(restored.StartNewGamePlus() && restored.LoopIndex == 2, "New completion unlocks one further loop.");
        restored.ResetForNewRun();
        Check(restored.CompletedRuns == 2 && restored.LoopIndex == 0 && !restored.IsInNewGamePlus, "Normal new run preserves completed-run history.");
        restored.ResetForDebug();
        Check(restored.CompletedRuns == 0 && !restored.CanStartNewGamePlus(), "Explicit debug reset clears account progress.");
        restored.RegisterCompletion(); PlayerPrefs.DeleteKey(NgKey); Invoke(restored, "Load");
        Check(restored.CompletedRuns == 0, "Re-enabled service cannot retain deleted-save state.");
        PlayerPrefs.SetString(NgKey, "{\"completedRuns\":1,\"loopIndex\":0,\"isInNewGamePlus\":false}");
        Invoke(restored, "Load");
        Check(restored.CanStartNewGamePlus(), "Legacy completed-run save migrates eligibility.");
    }

    private static void NewGamePlusBounds()
    {
        PlayerPrefs.Reset();
        PlayerPrefs.SetString(NgKey, "{\"completedRuns\":2147483647,\"loopIndex\":2147483647,\"isInNewGamePlus\":true,\"completedCurrentRun\":false}");
        NewGamePlusService service = new NewGamePlusService(); Invoke(service, "Load");
        Check(service.GetEnemyStatMultiplier() == 1000f && service.GetXpMultiplier() == 1000f, "Large persisted loops cannot produce infinity multipliers.");
        Check(service.GetStartingCurrency() == int.MaxValue, "Starting currency saturates instead of overflowing.");
        service.RegisterCompletion();
        Check(service.CompletedRuns == int.MaxValue && service.StartNewGamePlus() && service.LoopIndex == int.MaxValue, "Run and loop counters saturate.");
        Set(service, "ngPlusEnemyMultiplier", float.NaN);
        Check(service.GetEnemyStatMultiplier() == 1f, "Invalid serialized multiplier falls back to finite neutral value.");
        PartyManager.Instance = new PartyManager { ActiveParty = null };
        Check(service.GetCarryOverHeroIds().Count == 0, "Missing party roster returns no carryover safely.");
        PartyManager.Instance = null;
    }

    private static void NewGamePlusWorldSnapshots()
    {
        PlayerPrefs.Reset();
        NewGamePlusService service = new NewGamePlusService();
        service.RegisterCompletion();
        NewGamePlusService.RunSnapshot normal = service.CaptureSnapshot();
        Check(service.StartNewGamePlus(), "Snapshot fixture enters NG+.");
        NewGamePlusService.RunSnapshot plus = service.CaptureSnapshot();
        int flushes = PlayerPrefs.SaveCount;
        service.ApplySnapshot(normal);
        Check(!service.IsInNewGamePlus && service.LoopIndex == 0 && service.CanStartNewGamePlus(), "Saved completed normal run supersedes newer sidecar NG+ state.");
        Check(PlayerPrefs.SaveCount == flushes, "Applying world snapshot does not autosave during load.");
        service.ApplySnapshot(plus);
        Check(service.IsInNewGamePlus && service.LoopIndex == 1 && !service.CanStartNewGamePlus(), "Saved NG+ run restores its consumed completion and multiplier.");
        service.ApplySnapshot(null);
        Check(!service.IsInNewGamePlus && service.CompletedRuns == 1, "Legacy missing run snapshot clears NG+ while retaining account history.");
        NewGamePlusService fresh = new NewGamePlusService();
        fresh.ApplySnapshot(plus);
        Check(fresh.CompletedRuns == 1 && fresh.IsInNewGamePlus, "Valid world snapshot recovers completion history from a missing sidecar.");
        fresh.RegisterCompletion();
        fresh.ApplySnapshot(normal);
        Check(fresh.CompletedRuns == 2 && !fresh.IsInNewGamePlus, "Loading old world preserves newer account completion history.");
        fresh.ApplySnapshot(new NewGamePlusService.RunSnapshot { completedRuns = 2, loopIndex = int.MaxValue, isInNewGamePlus = true });
        Check(!fresh.IsInNewGamePlus && fresh.LoopIndex == 0 && fresh.CompletedRuns == 2, "Invalid run snapshot cannot invent scaling or erase account history.");
    }

    private static void Invoke(object target, string method) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new InvalidOperationException(message);
    }
}
