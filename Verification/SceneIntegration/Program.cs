using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// This is a source/serialized-reference check. It does not compile Unity code,
// restore Unity packages, import assets, execute MonoBehaviours, or run a player.
string root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
if (!Directory.Exists(Path.Combine(root, "Assets")) || !File.Exists(Path.Combine(root, "ProjectSettings/EditorBuildSettings.asset")))
{
    Console.Error.WriteLine("Usage: dotnet run --project Verification/SceneIntegration -- <Unity project root>");
    return 2;
}

var audit = new IntegrationAudit(root);
audit.CheckSource();
audit.CheckSerializedReferences();
Console.WriteLine($"Source/scene audit: {audit.Errors.Count} error(s), {audit.Warnings.Count} warning(s).");
foreach (string item in audit.Errors) Console.Error.WriteLine($"ERROR: {item}");
foreach (string item in audit.Warnings) Console.WriteLine($"WARNING: {item}");
Console.WriteLine("Unity compilation, package GUID resolution, asset import, Play Mode, and player builds remain unverified.");
return audit.Errors.Count == 0 ? 0 : 1;

sealed class IntegrationAudit(string root)
{
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];
    private readonly Dictionary<string, string> assetByGuid = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> packageDependencies = JsonSerializer.Deserialize<PackageManifest>(File.ReadAllText(Path.Combine(root, "Packages/manifest.json")))?.dependencies ?? new();

    // These references belong to installed Unity packages. Presence in manifest
    // proves the dependency is declared; only Unity package import proves GUIDs.
    private static readonly Dictionary<string, string> PackageGuids = new(StringComparer.OrdinalIgnoreCase)
    {
        ["a79441f348de89743a2939f4d699eac1"] = "com.unity.render-pipelines.universal",
        ["474bcb49853aa07438625e644c072ee6"] = "com.unity.render-pipelines.universal",
        ["31321ba15b8f8eb4c954353edc038b1d"] = "com.unity.render-pipelines.universal",
        ["01614664b831546d2ae94a42149d80ac"] = "com.unity.inputsystem",
        ["ca9f5fa95ffab41fb9a615ab714db018"] = "com.unity.inputsystem",
        ["76c392e42b5098c458856cdf6ecaaaa1"] = "com.unity.ugui",
    };

    public void CheckSource()
    {
        string[] files = Directory.GetFiles(Path.Combine(root, "Assets"), "*.cs", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        foreach (string file in files)
            if (!File.Exists(file + ".meta")) Errors.Add($"{Relative(file)} has no Unity metadata file.");
        string[] shared = ["UNITY_6000_0_OR_NEWER", "UNITY_6000_3_OR_NEWER", "UNITY_2022_2_OR_NEWER", "UNITY_STANDALONE", "UNITY_STANDALONE_LINUX", "ENABLE_LEGACY_INPUT_MANAGER"];
        (string Name, string[] Symbols, bool Player)[] configs =
        [
            ("editor + input system", [.. shared, "UNITY_EDITOR", "UNITY_EDITOR_LINUX", "UNITY_ASSERTIONS", "ENABLE_INPUT_SYSTEM"], false),
            ("release player + input system", [.. shared, "ENABLE_INPUT_SYSTEM"], true),
            ("development player + input system", [.. shared, "DEVELOPMENT_BUILD", "UNITY_ASSERTIONS", "ENABLE_INPUT_SYSTEM"], true),
            ("release player + legacy input", shared, true),
            ("mobile player + input system", [.. shared.Where(s => !s.StartsWith("UNITY_STANDALONE", StringComparison.Ordinal)), "UNITY_ANDROID", "ENABLE_INPUT_SYSTEM"], true),
        ];

        var nunitFiles = new HashSet<string>(StringComparer.Ordinal);
        var editorTestTypes = new HashSet<string>(StringComparer.Ordinal);
        var projectTypes = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var editorOptions = new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: configs[0].Symbols);
        foreach (string file in files)
        {
            SyntaxNode syntax = CSharpSyntaxTree.ParseText(File.ReadAllText(file), editorOptions).GetRoot();
            foreach (TypeDeclarationSyntax type in syntax.DescendantNodes().OfType<TypeDeclarationSyntax>().Where(c => c.Parent is CompilationUnitSyntax or NamespaceDeclarationSyntax))
            {
                if (!projectTypes.TryGetValue(type.Identifier.ValueText, out HashSet<string>? nested))
                    projectTypes[type.Identifier.ValueText] = nested = new(StringComparer.Ordinal);
                foreach (BaseTypeDeclarationSyntax memberType in type.Members.OfType<BaseTypeDeclarationSyntax>())
                    nested.Add(memberType.Identifier.ValueText);
            }
            if (!syntax.DescendantNodes().OfType<UsingDirectiveSyntax>().Any(u => (u.Name?.ToString() ?? "").StartsWith("NUnit.", StringComparison.Ordinal))) continue;
            foreach (ClassDeclarationSyntax type in syntax.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c => c.Parent is CompilationUnitSyntax or NamespaceDeclarationSyntax))
                editorTestTypes.Add(type.Identifier.ValueText);
        }
        foreach (var config in configs)
        {
            int parsed = 0;
            var options = new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: config.Symbols);
            foreach (string file in files)
            {
                if (config.Player && Relative(file).Split('/').Contains("Editor", StringComparer.Ordinal)) continue;
                SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), options, path: Relative(file));
                foreach (Diagnostic diagnostic in tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
                    Errors.Add($"[{config.Name}] {diagnostic}");
                // QualifiedNameSyntax is a type/name context (unlike expression
                // member access). Catch project-type nesting mismatches without
                // pretending that unavailable engine APIs can be compiled.
                foreach (QualifiedNameSyntax name in tree.GetRoot().DescendantNodes().OfType<QualifiedNameSyntax>())
                    if (name.Left is IdentifierNameSyntax owner
                        && projectTypes.TryGetValue(owner.Identifier.ValueText, out HashSet<string>? nested)
                        && !nested.Contains(name.Right.Identifier.ValueText))
                        Errors.Add($"[{config.Name}] {Relative(file)} references absent nested project type {name}.");
                parsed++;
                if (!config.Player) continue;
                foreach (IdentifierNameSyntax name in tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>())
                    if (editorTestTypes.Contains(name.Identifier.ValueText))
                        Errors.Add($"[{config.Name}] {Relative(file)} references verification type {name.Identifier.ValueText} in player source.");
                foreach (UsingDirectiveSyntax import in tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>())
                {
                    string name = import.Name?.ToString() ?? "";
                    if (name == "UnityEditor" || name.StartsWith("UnityEditor.", StringComparison.Ordinal))
                        Errors.Add($"[{config.Name}] {Relative(file)} imports editor-only {name} in player source.");
                    if (name == "NUnit" || name.StartsWith("NUnit.", StringComparison.Ordinal)) nunitFiles.Add(Relative(file));
                }
            }
            Console.WriteLine($"Parsed {parsed} C# sources with C# 9 / {config.Name} symbols.");
        }
        if (nunitFiles.Count > 0)
            Errors.Add($"{nunitFiles.Count} player-active files import NUnit in the flat runtime assembly: {string.Join(", ", nunitFiles.OrderBy(x => x))}");
    }

    public void CheckSerializedReferences()
    {
        foreach (string meta in Directory.GetFiles(Path.Combine(root, "Assets"), "*.meta", SearchOption.AllDirectories).OrderBy(x => x))
        {
            string asset = meta[..^5];
            Match guidMatch = Regex.Match(File.ReadAllText(meta), @"^guid: ([a-fA-F0-9]{32})\s*$", RegexOptions.Multiline);
            if (!guidMatch.Success)
            {
                Errors.Add($"{Relative(meta)} has no valid GUID.");
                continue;
            }
            string guid = guidMatch.Groups[1].Value;
            if (!assetByGuid.TryAdd(guid, asset)) Errors.Add($"Duplicate GUID {guid}: {Relative(assetByGuid[guid])}, {Relative(asset)}.");
            if (!File.Exists(asset) && !Directory.Exists(asset)) Errors.Add($"Orphan meta {Relative(meta)}.");
        }

        string settings = File.ReadAllText(Path.Combine(root, "ProjectSettings/EditorBuildSettings.asset"));
        var enabledScenes = new List<string>();
        foreach (Match scene in Regex.Matches(settings, @"- enabled: ([01])\s+path: ([^\r\n]+)\s+guid: ([a-fA-F0-9]{32})"))
        {
            string relative = scene.Groups[2].Value.Trim();
            string scenePath = Path.GetFullPath(Path.Combine(root, relative));
            if (!File.Exists(scenePath)) Errors.Add($"Build settings scene does not exist: {relative}.");
            if (!assetByGuid.TryGetValue(scene.Groups[3].Value, out string? actual) || actual != scenePath)
                Errors.Add($"Build settings GUID does not match {relative}.");
            if (scene.Groups[1].Value == "1") enabledScenes.Add(relative);
        }
        string[] requiredScenes = ["TitleScene", "HubScene", "level_1", "PuzzleScene", "CombatScene"];
        foreach (string scene in requiredScenes)
            if (!enabledScenes.Contains($"Assets/Scenes/{scene}.unity", StringComparer.Ordinal)) Errors.Add($"Required scene {scene} is not enabled in build settings.");
        if (enabledScenes.FirstOrDefault() != "Assets/Scenes/TitleScene.unity") Errors.Add("TitleScene must be the first enabled scene.");

        // Include every scene, gameplay prefab, and Resources asset. Render
        // pipeline/TMP configuration references require package import instead.
        var serializedFiles = Directory.GetFiles(Path.Combine(root, "Assets/Scenes"), "*.unity", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(Path.Combine(root, "Assets/Resources"), "*.asset", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(Path.Combine(root, "Assets"), "*.prefab", SearchOption.TopDirectoryOnly)).OrderBy(x => x).ToArray();
        var checkedPackageGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string file in serializedFiles)
        {
            string yaml = File.ReadAllText(file);
            CheckLocalReferences(file, yaml);
            foreach (Match reference in Regex.Matches(yaml, @"guid: ([a-fA-F0-9]{32})"))
            {
                string guid = reference.Groups[1].Value;
                if (assetByGuid.ContainsKey(guid) || guid.StartsWith("0000000000000000", StringComparison.Ordinal)) continue;
                if (PackageGuids.TryGetValue(guid, out string? package))
                {
                    if (!packageDependencies.ContainsKey(package)) Errors.Add($"{Relative(file)} requires missing package {package} ({guid}).");
                    checkedPackageGuids.Add(guid);
                }
                else Errors.Add($"{Relative(file)} has unresolved project reference {guid}.");
            }
            foreach (Match script in Regex.Matches(yaml, @"m_Script: \{fileID: ([^,}]+)(?:, guid: ([a-fA-F0-9]{32}), type: \d+)?\}"))
            {
                if (script.Groups[1].Value == "0") Errors.Add($"{Relative(file)} has a missing script.");
                string guid = script.Groups[2].Value;
                if (!assetByGuid.TryGetValue(guid, out string? source)) continue;
                if (!source.EndsWith(".cs", StringComparison.Ordinal)) Errors.Add($"{Relative(file)} script GUID points to non-C# asset {Relative(source)}.");
                else CheckScriptClass(file, source);
            }
        }
        CheckSceneComponents("TitleScene", "GameStateManager", "TitleScreenUI");
        CheckSceneComponents("HubScene", "GameStateManager", "IsometricPlayer", "PartyManager", "IslandBoatInteractable", "TeleportAnchor");
        CheckSceneComponents("level_1", "GameStateManager", "IsometricPlayer", "PartyManager", "TopDownFollowCamera");
        CheckSceneComponents("PuzzleScene", "PuzzleSceneBootstrap");
        CheckSceneComponents("CombatScene", "CombatSceneBootstrap");
        Console.WriteLine($"Checked {assetByGuid.Count} project GUIDs, {enabledScenes.Count} enabled scenes, {serializedFiles.Length} scene/gameplay asset files, and local fileID references.");
        Console.WriteLine($"{checkedPackageGuids.Count} known package GUIDs require declared dependencies; their Unity import remains unverified.");
    }

    private void CheckScriptClass(string file, string source)
    {
        string typeName = Path.GetFileNameWithoutExtension(source);
        string content = File.ReadAllText(source);
        SyntaxNode syntax = CSharpSyntaxTree.ParseText(content, new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: ["UNITY_EDITOR", "ENABLE_INPUT_SYSTEM"])).GetRoot();
        if (!syntax.DescendantNodes().OfType<ClassDeclarationSyntax>().Any(c => c.Identifier.ValueText == typeName))
            Errors.Add($"{Relative(file)} references script {Relative(source)} without matching {typeName} class.");
        SyntaxNode playerSyntax = CSharpSyntaxTree.ParseText(content, new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: ["UNITY_6000_0_OR_NEWER", "UNITY_6000_3_OR_NEWER", "UNITY_2022_2_OR_NEWER", "UNITY_STANDALONE", "ENABLE_INPUT_SYSTEM", "ENABLE_LEGACY_INPUT_MANAGER"])).GetRoot();
        if (!playerSyntax.DescendantNodes().OfType<ClassDeclarationSyntax>().Any(c => c.Identifier.ValueText == typeName))
            Errors.Add($"{Relative(file)} references {typeName}, which is absent from release player source.");
    }

    private void CheckSceneComponents(string sceneName, params string[] expectedTypes)
    {
        string file = Path.Combine(root, "Assets/Scenes", sceneName + ".unity");
        if (!File.Exists(file)) return;
        string yaml = File.ReadAllText(file);
        var activeScriptTypes = new HashSet<string>(StringComparer.Ordinal);
        var blocks = new Dictionary<string, (string Kind, string Body)>(StringComparer.Ordinal);
        foreach (Match entry in Regex.Matches(yaml, @"^--- !u!(\d+) &(-?\d+)(?: stripped)?[^\r\n]*\r?\n(.*?)(?=^--- !u!|\z)", RegexOptions.Multiline | RegexOptions.Singleline))
            blocks.TryAdd(entry.Groups[2].Value, (entry.Groups[1].Value, entry.Groups[3].Value));
        foreach (var block in blocks.Values.Where(b => b.Kind == "114"))
        {
            Match script = Regex.Match(block.Body, @"m_Script: \{fileID: [^,]+, guid: ([a-fA-F0-9]{32})");
            Match owner = Regex.Match(block.Body, @"m_GameObject: \{fileID: (-?\d+)\}");
            if (!script.Success || !owner.Success || !Regex.IsMatch(block.Body, @"^  m_Enabled: 1\s*$", RegexOptions.Multiline)) continue;
            if (!blocks.TryGetValue(owner.Groups[1].Value, out var gameObject) || gameObject.Kind != "1"
                || !Regex.IsMatch(gameObject.Body, @"^  m_IsActive: 1\s*$", RegexOptions.Multiline)) continue;
            if (assetByGuid.TryGetValue(script.Groups[1].Value, out string? source)) activeScriptTypes.Add(Path.GetFileNameWithoutExtension(source));
        }
        foreach (string type in expectedTypes)
            if (!activeScriptTypes.Contains(type)) Errors.Add($"{sceneName} lacks expected enabled component {type} on an active GameObject.");
    }

    private void CheckLocalReferences(string file, string yaml)
    {
        string[] ids = Regex.Matches(yaml, @"^--- !u!\d+ &(-?\d+)(?: stripped)?\s*$", RegexOptions.Multiline).Select(m => m.Groups[1].Value).ToArray();
        var unique = new HashSet<string>(ids, StringComparer.Ordinal);
        if (unique.Count != ids.Length) Errors.Add($"{Relative(file)} has duplicate serialized fileIDs.");
        foreach (Match local in Regex.Matches(yaml, @"\{fileID: (-?\d+)([^}]*)\}"))
            if (local.Groups[1].Value != "0" && !local.Groups[2].Value.Contains("guid:", StringComparison.Ordinal) && !unique.Contains(local.Groups[1].Value))
                Errors.Add($"{Relative(file)} has unresolved local fileID {local.Groups[1].Value}.");
    }

    private string Relative(string file) => Path.GetRelativePath(root, file).Replace('\\', '/');
    private sealed class PackageManifest
    {
        public Dictionary<string, string> dependencies { get; set; } = new();
    }
}
