using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace FartTogether.Editor
{
    /// <summary>
    /// Editor tool that parses Assets/Ripped/GameObject/Level_Platform.prefab as YAML text,
    /// identifies unresolved MonoBehaviour components, maps their GUIDs to script names
    /// using external .cs.meta files, and displays a summary table of obstacles and serialized fields.
    /// Accessible via menu item "FartTogether/Analyze Level_Platform Obstacles (Phase 2)".
    /// </summary>
    public static class ObstacleAnalyzer
    {
        private const string PrefabPath = "Assets/Ripped/GameObject/Level_Platform.prefab";
        private const string ExternalMetaDir = "/mnt/storage/My Projects/FartTogether-work/tools/assetripper/ripped/ExportedProject/Assets/Scripts/Assembly-CSharp";

        public class ScriptInstance
        {
            public string MbId;
            public string GoId;
            public string GoName;
            public Dictionary<string, string> Fields = new Dictionary<string, string>();
        }

        public class ScriptSummary
        {
            public string Guid;
            public string ScriptName;
            public List<ScriptInstance> Instances = new List<ScriptInstance>();
            public HashSet<string> UniqueFieldNames = new HashSet<string>();
        }

        [MenuItem("FartTogether/Analyze Level_Platform Obstacles (Phase 2)", priority = 10)]
        public static void AnalyzeMenu()
        {
            AnalyzeLevelPlatform();
        }

        public static List<ScriptSummary> AnalyzeLevelPlatform()
        {
            Debug.Log("[ObstacleAnalyzer] Starting analysis of Level_Platform.prefab...");

            // 1. Build GUID -> Script Name mapping from external .cs.meta files
            Dictionary<string, string> guidToScript = LoadGuidToScriptMap();
            Debug.Log($"[ObstacleAnalyzer] Loaded {guidToScript.Count} script GUID mappings.");

            // 2. Map FileID -> GameObject Name from the prefab
            Dictionary<string, string> fileIdToGoName = LoadFileIdToGoNameMap(PrefabPath);

            // 3. Parse MonoBehaviours and their serialized fields
            Dictionary<string, ScriptSummary> summaries = ParseMonoBehaviours(PrefabPath, guidToScript, fileIdToGoName);

            // 4. Print Table to Console and Log
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("====================================================================================================");
            sb.AppendLine("LEVEL_PLATFORM PREFAB - UNRESOLVED SCRIPT COMPONENTS & SERIALIZED FIELDS");
            sb.AppendLine("====================================================================================================");
            sb.AppendLine(string.Format("{0,-32} | {1,-10} | {2,-34} | {3}", "Script Name", "Instances", "GUID", "Serialized Fields"));
            sb.AppendLine(new string('-', 100));

            var sortedList = summaries.Values.OrderByDescending(s => s.Instances.Count).ToList();

            foreach (var s in sortedList)
            {
                string fieldsList = string.Join(", ", s.UniqueFieldNames.OrderBy(f => f));
                sb.AppendLine(string.Format("{0,-32} | {1,-10} | {2,-34} | {3}", s.ScriptName, s.Instances.Count, s.Guid, fieldsList));
            }

            sb.AppendLine("====================================================================================================");
            sb.AppendLine("\nDETAILED OBSTACLE BREAKDOWN:");

            // Specifically highlight moving obstacle animations and wave bobs
            if (summaries.TryGetValue("89caabe3d6ff32eb9bba2b79b38063ae", out var animSummary)) // ImportedObstacleAnimation
            {
                sb.AppendLine($"\n--- Moving Animated Obstacles ({animSummary.Instances.Count} instances) ---");
                foreach (var inst in animSummary.Instances)
                {
                    inst.Fields.TryGetValue("_playbackSpeed", out string spd);
                    inst.Fields.TryGetValue("_phaseSeconds", out string phs);
                    inst.Fields.TryGetValue("_clip", out string clip);
                    sb.AppendLine($"  • GameObject: {inst.GoName,-28} Speed: {spd ?? "1",-5} Phase: {phs ?? "0",-6} Clip: {clip}");
                }
            }

            if (summaries.TryGetValue("ab2d42a0503492409624383c3d78966e", out var waveSummary)) // FakeWaveBob
            {
                sb.AppendLine($"\n--- Floating Water Obstacles / FakeWaveBob ({waveSummary.Instances.Count} instances) ---");
                foreach (var inst in waveSummary.Instances)
                {
                    inst.Fields.TryGetValue("_amplitude", out string amp);
                    inst.Fields.TryGetValue("_speed", out string spd);
                    inst.Fields.TryGetValue("_wavelength", out string wlen);
                    sb.AppendLine($"  • GameObject: {inst.GoName,-28} Amp: {amp ?? "0.25",-5} Speed: {spd ?? "2.5",-5} Wavelength: {wlen ?? "6"}");
                }
            }

            if (summaries.TryGetValue("a1a214f0b8db7c7e9692a5740e9fee8c", out var bounceSummary)) // BounceObstacle
            {
                sb.AppendLine($"\n--- Bouncing Spikes ({bounceSummary.Instances.Count} instances) ---");
                foreach (var inst in bounceSummary.Instances)
                {
                    inst.Fields.TryGetValue("_knockbackImpulse", out string impulse);
                    sb.AppendLine($"  • GameObject: {inst.GoName,-28} Knockback: {impulse ?? "2"}");
                }
            }

            Debug.Log(sb.ToString());
            return sortedList;
        }

        private static Dictionary<string, string> LoadGuidToScriptMap()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (Directory.Exists(ExternalMetaDir))
            {
                string[] metaFiles = Directory.GetFiles(ExternalMetaDir, "*.cs.meta", SearchOption.AllDirectories);
                foreach (string metaFile in metaFiles)
                {
                    string scriptName = Path.GetFileNameWithoutExtension(metaFile);
                    string[] lines = File.ReadAllLines(metaFile);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("guid:", StringComparison.OrdinalIgnoreCase))
                        {
                            string guid = line.Substring(5).Trim();
                            map[guid] = scriptName;
                            break;
                        }
                    }
                }
            }

            // Also check project Assets for local scripts
            string[] localMetas = Directory.GetFiles("Assets", "*.cs.meta", SearchOption.AllDirectories);
            foreach (string metaFile in localMetas)
            {
                string scriptName = Path.GetFileNameWithoutExtension(metaFile);
                string[] lines = File.ReadAllLines(metaFile);
                foreach (string line in lines)
                {
                    if (line.StartsWith("guid:", StringComparison.OrdinalIgnoreCase))
                    {
                        string guid = line.Substring(5).Trim();
                        if (!map.ContainsKey(guid))
                        {
                            map[guid] = scriptName;
                        }
                        break;
                    }
                }
            }

            return map;
        }

        private static Dictionary<string, string> LoadFileIdToGoNameMap(string prefabPath)
        {
            var map = new Dictionary<string, string>();
            if (!File.Exists(prefabPath)) return map;

            string currentId = null;
            using (var reader = new StreamReader(prefabPath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("--- !u!1 &"))
                    {
                        currentId = line.Substring(10).Trim();
                    }
                    else if (currentId != null && line.Contains("m_Name:"))
                    {
                        int idx = line.IndexOf("m_Name:", StringComparison.Ordinal);
                        string goName = line.Substring(idx + 7).Trim();
                        map[currentId] = goName;
                        currentId = null;
                    }
                }
            }
            return map;
        }

        private static Dictionary<string, ScriptSummary> ParseMonoBehaviours(
            string prefabPath,
            Dictionary<string, string> guidToScript,
            Dictionary<string, string> fileIdToGoName)
        {
            var summaries = new Dictionary<string, ScriptSummary>();
            if (!File.Exists(prefabPath)) return summaries;

            using (var reader = new StreamReader(prefabPath))
            {
                string line;
                bool inMb = false;
                string currentMbId = null;
                string currentGoId = null;
                string currentGuid = null;
                var currentFields = new Dictionary<string, string>();

                Action commitCurrent = () =>
                {
                    if (inMb && !string.IsNullOrEmpty(currentGuid))
                    {
                        if (!summaries.TryGetValue(currentGuid, out var summary))
                        {
                            guidToScript.TryGetValue(currentGuid, out string sName);
                            summary = new ScriptSummary
                            {
                                Guid = currentGuid,
                                ScriptName = !string.IsNullOrEmpty(sName) ? sName : $"Unknown ({currentGuid})"
                            };
                            summaries[currentGuid] = summary;
                        }

                        fileIdToGoName.TryGetValue(currentGoId ?? "", out string goName);
                        var inst = new ScriptInstance
                        {
                            MbId = currentMbId,
                            GoId = currentGoId,
                            GoName = goName ?? "Unknown",
                            Fields = new Dictionary<string, string>(currentFields)
                        };
                        summary.Instances.Add(inst);

                        foreach (var k in currentFields.Keys)
                        {
                            summary.UniqueFieldNames.Add(k);
                        }
                    }
                };

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("--- !u!114 &"))
                    {
                        commitCurrent();
                        inMb = true;
                        currentMbId = line.Substring(12).Trim();
                        currentGoId = null;
                        currentGuid = null;
                        currentFields.Clear();
                    }
                    else if (line.StartsWith("--- !u!"))
                    {
                        commitCurrent();
                        inMb = false;
                        currentGuid = null;
                        currentFields.Clear();
                    }
                    else if (inMb)
                    {
                        if (line.Contains("m_GameObject:"))
                        {
                            Match m = Regex.Match(line, @"fileID:\s*(\d+)");
                            if (m.Success) currentGoId = m.Groups[1].Value;
                        }
                        else if (line.Contains("m_Script:"))
                        {
                            Match m = Regex.Match(line, @"guid:\s*([a-fA-F0-9]+)");
                            if (m.Success) currentGuid = m.Groups[1].Value;
                        }
                        else
                        {
                            string trimmed = line.Trim();
                            if (!string.IsNullOrEmpty(trimmed) && trimmed.Contains(":") &&
                                !trimmed.StartsWith("m_") && !trimmed.StartsWith("-") &&
                                !trimmed.StartsWith("serializedVersion"))
                            {
                                int colonIdx = trimmed.IndexOf(':');
                                string k = trimmed.Substring(0, colonIdx).Trim();
                                string v = trimmed.Substring(colonIdx + 1).Trim();
                                if (!currentFields.ContainsKey(k))
                                {
                                    currentFields[k] = v;
                                }
                            }
                        }
                    }
                }
                commitCurrent();
            }

            return summaries;
        }
    }
}
