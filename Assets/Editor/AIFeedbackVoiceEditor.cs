using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(AIFeedbackVoice))]
public sealed class AIFeedbackVoiceEditor : Editor
{
    private const string PreferredVoiceName = "Microsoft Zira Desktop";
    private static readonly List<string> installedVoiceNames = new List<string>();
    private static readonly List<AIFeedbackVoiceEditor> activeEditors = new List<AIFeedbackVoiceEditor>();

    private static Task<VoiceDiscoveryResult> discoveryTask;
    private static bool discoveryCompleted;
    private static bool pendingDiscoveryIsRefresh;
    private static bool completedDiscoveryWasRefresh;
    private static string discoveryError;
    private static int voiceListVersion;

    private SerializedProperty windowsVoiceName;
    private SerializedProperty voiceEnabled;
    private SerializedProperty speechRate;
    private SerializedProperty volume;
    private SerializedProperty interruptCurrentSpeech;
    private int observedVoiceListVersion;

    private sealed class VoiceDiscoveryResult
    {
        public readonly List<string> names;
        public readonly string error;

        public VoiceDiscoveryResult(List<string> voiceNames, string errorMessage)
        {
            names = voiceNames;
            error = errorMessage;
        }
    }

    private void OnEnable()
    {
        windowsVoiceName = serializedObject.FindProperty("windowsVoiceName");
        voiceEnabled = serializedObject.FindProperty("voiceEnabled");
        speechRate = serializedObject.FindProperty("speechRate");
        volume = serializedObject.FindProperty("volume");
        interruptCurrentSpeech = serializedObject.FindProperty("interruptCurrentSpeech");
        observedVoiceListVersion = voiceListVersion;

        if (!activeEditors.Contains(this))
            activeEditors.Add(this);

        if (discoveryTask == null && !discoveryCompleted)
            BeginVoiceDiscovery(false);
    }

    private void OnDisable()
    {
        activeEditors.Remove(this);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        ApplyCompletedDiscoveryToSelection();
        DrawVoiceSettings();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawVoiceSettings()
    {
        EditorGUILayout.LabelField("Windows Voice", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(voiceEnabled, new GUIContent("Voice Enabled"));
        EditorGUILayout.IntSlider(speechRate, -10, 10, new GUIContent("Speech Rate"));
        EditorGUILayout.IntSlider(volume, 0, 100, new GUIContent("Volume"));

        DrawVoiceSelector();

        EditorGUILayout.PropertyField(interruptCurrentSpeech, new GUIContent("Interrupt Current Speech"));
    }

    private void DrawVoiceSelector()
    {
        string selectedVoice = windowsVoiceName.stringValue ?? string.Empty;
        bool mixedSelection = windowsVoiceName.hasMultipleDifferentValues;
        string[] options;
        int selectedIndex = 0;
        bool canChooseVoice = discoveryCompleted && installedVoiceNames.Count > 0;

        if (canChooseVoice)
        {
            int voiceIndex = mixedSelection ? -1 : installedVoiceNames.IndexOf(selectedVoice);
            bool showUnavailable = !mixedSelection &&
                                   !string.IsNullOrWhiteSpace(selectedVoice) &&
                                   voiceIndex < 0;

            if (showUnavailable)
            {
                options = new string[installedVoiceNames.Count + 1];
                options[0] = selectedVoice + " (Unavailable)";
                installedVoiceNames.CopyTo(options, 1);
                selectedIndex = 0;
            }
            else
            {
                options = installedVoiceNames.ToArray();
                selectedIndex = voiceIndex >= 0 ? voiceIndex : 0;
            }
        }
        else if (!discoveryCompleted)
        {
            options = new[]
            {
                string.IsNullOrWhiteSpace(selectedVoice)
                    ? "Loading installed voices..."
                    : selectedVoice + " (checking availability...)"
            };
        }
        else
        {
            options = new[]
            {
                string.IsNullOrWhiteSpace(selectedVoice)
                    ? "No installed voices"
                    : selectedVoice + " (Unavailable)"
            };
        }

        EditorGUI.showMixedValue = mixedSelection;
        using (new EditorGUI.DisabledScope(!canChooseVoice))
        {
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup("Voice", selectedIndex, options);
            if (EditorGUI.EndChangeCheck() && canChooseVoice)
            {
                int firstInstalledVoiceOption = options.Length - installedVoiceNames.Count;
                int voiceIndex = newIndex - firstInstalledVoiceOption;
                if (voiceIndex >= 0 && voiceIndex < installedVoiceNames.Count)
                    windowsVoiceName.stringValue = installedVoiceNames[voiceIndex];
            }
        }
        EditorGUI.showMixedValue = false;

        using (new EditorGUI.DisabledScope(discoveryTask != null && !discoveryTask.IsCompleted))
        {
            if (GUILayout.Button("Refresh Voices"))
                BeginVoiceDiscovery(true);
        }

        if (!discoveryCompleted)
            EditorGUILayout.HelpBox("Searching installed Windows voices in the background.", MessageType.Info);
        else if (!string.IsNullOrEmpty(discoveryError))
            EditorGUILayout.HelpBox(discoveryError, MessageType.Warning);
        else if (installedVoiceNames.Count == 0)
            EditorGUILayout.HelpBox("No Windows speech voices were found. Voice output will be skipped until one is installed.", MessageType.Warning);
    }

    private void ApplyCompletedDiscoveryToSelection()
    {
        if (observedVoiceListVersion != voiceListVersion)
        {
            string selectedVoice = windowsVoiceName.stringValue ?? string.Empty;
            bool mixedSelection = windowsVoiceName.hasMultipleDifferentValues;
            bool selectionAvailable = installedVoiceNames.Contains(selectedVoice);

            if (!mixedSelection &&
                installedVoiceNames.Count > 0 &&
                (string.IsNullOrWhiteSpace(selectedVoice) ||
                 (completedDiscoveryWasRefresh && !selectionAvailable)))
            {
                windowsVoiceName.stringValue = GetPreferredVoiceName();
            }

            observedVoiceListVersion = voiceListVersion;
        }

        if (discoveryCompleted &&
            installedVoiceNames.Count > 0 &&
            !windowsVoiceName.hasMultipleDifferentValues &&
            string.IsNullOrWhiteSpace(windowsVoiceName.stringValue))
        {
            windowsVoiceName.stringValue = GetPreferredVoiceName();
        }
    }

    private static string GetPreferredVoiceName()
    {
        if (installedVoiceNames.Contains(PreferredVoiceName))
            return PreferredVoiceName;

        return installedVoiceNames.Count > 0 ? installedVoiceNames[0] : string.Empty;
    }

    private static void BeginVoiceDiscovery(bool isRefresh)
    {
        if (discoveryTask != null && !discoveryTask.IsCompleted)
            return;

        pendingDiscoveryIsRefresh = isRefresh;
        discoveryCompleted = false;
        discoveryError = null;

        try
        {
            discoveryTask = Task.Run(DiscoverInstalledVoiceNames);
        }
        catch (Exception exception)
        {
            discoveryTask = null;
            discoveryCompleted = true;
            discoveryError = "Could not start Windows voice discovery: " + exception.Message;
            RepaintActiveEditors();
            return;
        }

        EditorApplication.update -= PollVoiceDiscovery;
        EditorApplication.update += PollVoiceDiscovery;
        RepaintActiveEditors();
    }

    private static void PollVoiceDiscovery()
    {
        Task<VoiceDiscoveryResult> completedTask = discoveryTask;
        if (completedTask == null || !completedTask.IsCompleted)
            return;

        EditorApplication.update -= PollVoiceDiscovery;
        try
        {
            VoiceDiscoveryResult result = completedTask.GetAwaiter().GetResult();
            installedVoiceNames.Clear();
            if (result.names != null)
                installedVoiceNames.AddRange(result.names);
            discoveryError = result.error;
        }
        catch (Exception exception)
        {
            installedVoiceNames.Clear();
            discoveryError = "Could not discover Windows speech voices: " + exception.Message;
        }

        completedDiscoveryWasRefresh = pendingDiscoveryIsRefresh;
        discoveryTask = null;
        discoveryCompleted = true;
        voiceListVersion++;
        RepaintActiveEditors();
    }

    private static void RepaintActiveEditors()
    {
        for (int i = activeEditors.Count - 1; i >= 0; i--)
        {
            if (activeEditors[i] == null)
                activeEditors.RemoveAt(i);
            else
                activeEditors[i].Repaint();
        }
    }

    private static VoiceDiscoveryResult DiscoverInstalledVoiceNames()
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            return new VoiceDiscoveryResult(new List<string>(), "Windows speech voices are available only in the Windows Editor.");

        Process process = null;
        try
        {
            string script =
                "$ErrorActionPreference='Stop';try{Add-Type -AssemblyName System.Speech;" +
                "$s=New-Object System.Speech.Synthesis.SpeechSynthesizer;" +
                "try{$s.GetInstalledVoices()|Where-Object{$_.Enabled}|ForEach-Object{[Console]::Out.WriteLine($_.VoiceInfo.Name)}}" +
                "finally{$s.Dispose()}}catch{exit 3}";
            string encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            string systemDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string executable = Path.Combine(systemDirectory, "WindowsPowerShell\\v1.0\\powershell.exe");
            if (!File.Exists(executable))
                executable = "powershell.exe";

            process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand " + encodedCommand,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            if (!process.Start())
                return new VoiceDiscoveryResult(new List<string>(), "Could not start Windows voice discovery.");

            if (!process.WaitForExit(15000))
            {
                try { process.Kill(); }
                catch { }
                return new VoiceDiscoveryResult(new List<string>(), "Windows voice discovery timed out. Use Refresh Voices to try again.");
            }

            string standardOutput = process.StandardOutput.ReadToEnd();
            string standardError = process.StandardError.ReadToEnd();
            if (process.ExitCode != 0)
            {
                string details = standardError.Trim();
                return new VoiceDiscoveryResult(new List<string>(),
                    string.IsNullOrEmpty(details) ? "Could not discover Windows speech voices." : details);
            }

            List<string> names = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] lines = standardOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string name = line.Trim();
                if (name.Length > 0 && seen.Add(name))
                    names.Add(name);
            }

            return new VoiceDiscoveryResult(names, null);
        }
        catch (Exception exception)
        {
            return new VoiceDiscoveryResult(new List<string>(), "Could not discover Windows speech voices: " + exception.Message);
        }
        finally
        {
            if (process != null)
                process.Dispose();
        }
    }
}