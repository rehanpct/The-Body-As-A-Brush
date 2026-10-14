using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public sealed class AIFeedbackVoice : MonoBehaviour
{
    [Header("Windows Voice")]
    [SerializeField] private bool voiceEnabled = true;
    [SerializeField, Range(-10, 10)] private int speechRate = 0;
    [SerializeField, Range(0, 100)] private int volume = 100;
    [SerializeField, Tooltip("Exact installed Windows voice name.")]
    private string windowsVoiceName = "Microsoft Hanhan Desktop";
    [SerializeField] private bool interruptCurrentSpeech = true;

    private const int SpeechTimeoutMilliseconds = 90000;
    private readonly object stateLock = new object();
    private readonly Queue<SpeechJob> pendingSpeech = new Queue<SpeechJob>();
    private readonly Queue<VoiceLog> pendingLogs = new Queue<VoiceLog>();
    private bool speechWorkerRunning;
    private bool shuttingDown;
    private Process activeProcess;
    private SpeechJob activeJob;

    [Serializable] private sealed class SpeechEnvelope { public string message; }

    private sealed class SpeechJob
    {
        public readonly string message;
        public readonly int rate;
        public readonly int volume;
        public readonly string voiceName;
        public bool cancelled;

        public SpeechJob(string text, int speechRate, int speechVolume, string selectedVoice)
        {
            message = text; rate = speechRate; volume = speechVolume; voiceName = selectedVoice;
        }
    }

    private sealed class VoiceLog
    {
        public readonly string message;
        public readonly bool warning;
        public VoiceLog(string text, bool isWarning) { message = text; warning = isWarning; }
    }

    public void Speak(string message)
    {
        Debug.Log("[AIFeedbackVoice][RECEIVED TEXT] " + (message ?? "<NULL>"));
        if (!voiceEnabled) { Debug.Log("[AIFeedbackVoice] Voice disabled."); return; }
        if (!IsWindows()) { Debug.LogWarning("[AIFeedbackVoice] Windows TTS is available only in Windows Editor/Player."); return; }

        string cleanMessage = CleanMessageForSpeech(message);
        Debug.Log("[AIFeedbackVoice][CLEANED TEXT] " + (cleanMessage ?? "<NULL>"));
        if (string.IsNullOrWhiteSpace(cleanMessage))
        {
            Debug.LogWarning("[AIFeedbackVoice] No human-readable message to speak.");
            return;
        }

        string requestedVoice = string.IsNullOrWhiteSpace(windowsVoiceName) ? string.Empty : windowsVoiceName.Trim();
        Debug.Log("[AIFeedbackVoice][REQUESTED VOICE] " +
                  (string.IsNullOrEmpty(requestedVoice) ? "<automatic/default>" : requestedVoice));

        SpeechJob job = new SpeechJob(cleanMessage, Mathf.Clamp(speechRate, -10, 10),
            Mathf.Clamp(volume, 0, 100), requestedVoice);

        lock (stateLock)
        {
            if (shuttingDown) return;
            if (interruptCurrentSpeech)
            {
                CancelActiveJobLocked();
                while (pendingSpeech.Count > 0) pendingSpeech.Dequeue().cancelled = true;
            }

            pendingSpeech.Enqueue(job);
            if (!speechWorkerRunning)
            {
                speechWorkerRunning = true;
                try { Task.Run(DrainSpeechQueue); }
                catch (Exception ex)
                {
                    speechWorkerRunning = false;
                    pendingSpeech.Clear();
                    Debug.LogError("[AIFeedbackVoice] Could not start speech worker: " + ex);
                    return;
                }
            }
        }
        Debug.Log("[AIFeedbackVoice] Speech queued.");
    }

    private void Update()
    {
        while (true)
        {
            VoiceLog entry;
            lock (stateLock)
            {
                if (pendingLogs.Count == 0) return;
                entry = pendingLogs.Dequeue();
            }
            if (entry.warning) Debug.LogWarning(entry.message);
            else Debug.Log(entry.message);
        }
    }

    private void OnDisable() { CancelSpeech(false); }
    private void OnDestroy() { CancelSpeech(true); }

    private void CancelSpeech(bool destroying)
    {
        lock (stateLock)
        {
            if (destroying) shuttingDown = true;
            CancelActiveJobLocked();
            while (pendingSpeech.Count > 0) pendingSpeech.Dequeue().cancelled = true;
        }
    }

    private void CancelActiveJobLocked()
    {
        if (activeJob != null) activeJob.cancelled = true;
        if (activeProcess == null) return;
        try { if (!activeProcess.HasExited) activeProcess.Kill(); }
        catch { /* Process may have completed between checking and killing it. */ }
    }

    private void DrainSpeechQueue()
    {
        while (true)
        {
            SpeechJob job;
            lock (stateLock)
            {
                if (shuttingDown || pendingSpeech.Count == 0)
                {
                    speechWorkerRunning = false;
                    return;
                }
                job = pendingSpeech.Dequeue();
                activeJob = job;
            }
            if (job.cancelled) { FinishJob(job); continue; }
            SpeakOnWorker(job);
        }
    }

    private void SpeakOnWorker(SpeechJob job)
    {
        Process process = null;
        try
        {
            process = new Process { StartInfo = BuildProcessStartInfo(job), EnableRaisingEvents = true };
            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    QueueLog("[AIFeedbackVoice][POWERSHELL] " + args.Data, false);
            };
            process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    QueueLog("[AIFeedbackVoice][POWERSHELL ERROR] " + args.Data, true);
            };

            lock (stateLock)
            {
                if (shuttingDown || job.cancelled)
                {
                    if (activeJob == job) activeJob = null;
                    process.Dispose();
                    return;
                }
                if (!process.Start()) throw new InvalidOperationException("PowerShell did not start.");
                activeProcess = process;
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            QueueLog("[AIFeedbackVoice] Speech process started.", false);
            QueueLog("[AIFeedbackVoice][SENDING TO TTS] Requested voice: " +
                (string.IsNullOrEmpty(job.voiceName) ? "<automatic/default>" : job.voiceName), false);
            QueueLog("[AIFeedbackVoice][SENDING TO TTS] Exact text: 「" + job.message + "」", false);

            bool exited = process.WaitForExit(SpeechTimeoutMilliseconds);
            if (!exited) { try { process.Kill(); } catch { } }
            if (exited) process.WaitForExit();

            int exitCode = exited ? process.ExitCode : -1;
            bool cancelled;
            lock (stateLock)
            {
                if (activeProcess == process) activeProcess = null;
                if (activeJob == job) activeJob = null;
                cancelled = job.cancelled || shuttingDown;
            }
            if (cancelled) return;

            if (exited && exitCode == 0) QueueLog("[AIFeedbackVoice] Speech finished successfully.", false);
            else if (exited && exitCode == 4) QueueLog("[AIFeedbackVoice] No enabled Windows TTS voices available.", true);
            else if (exited && exitCode == 5) QueueLog("[AIFeedbackVoice] Requested voice not found. No fallback voice was used.", true);
            else if (exited && exitCode == 6) QueueLog("[AIFeedbackVoice] Requested voice could not be selected. Speech was not spoken.", true);
            else if (!exited) QueueLog("[AIFeedbackVoice] Speech timed out.", true);
            else QueueLog("[AIFeedbackVoice] PowerShell TTS failed with exit code " + exitCode + ".", true);
        }
        catch (Exception ex)
        {
            bool cancelled;
            lock (stateLock)
            {
                if (activeProcess == process) activeProcess = null;
                if (activeJob == job) activeJob = null;
                cancelled = job.cancelled || shuttingDown;
            }
            if (!cancelled) QueueLog("[AIFeedbackVoice] Speech process exception: " + ex, true);
        }
        finally { if (process != null) process.Dispose(); }
    }

    private void FinishJob(SpeechJob job)
    {
        lock (stateLock) { if (activeJob == job) activeJob = null; }
    }

    private void QueueLog(string message, bool warning)
    {
        lock (stateLock) pendingLogs.Enqueue(new VoiceLog(message, warning));
    }

    private static bool IsWindows()
    {
        return Application.platform == RuntimePlatform.WindowsEditor ||
               Application.platform == RuntimePlatform.WindowsPlayer;
    }

    private static ProcessStartInfo BuildProcessStartInfo(SpeechJob job)
    {
        string messageBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(job.message));
        string voiceBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(job.voiceName ?? string.Empty));
        string script =
            "$ErrorActionPreference='Stop';$synth=$null;try{" +
            "Add-Type -AssemblyName System.Speech;" +
            "$synth=New-Object System.Speech.Synthesis.SpeechSynthesizer;" +
            "$voices=@($synth.GetInstalledVoices()|Where-Object{$_.Enabled}|ForEach-Object{$_.VoiceInfo.Name});" +
            "Write-Output ('TTSINSTALLEDVOICES|' + ($voices -join ';'));" +
            "if($voices.Count -eq 0){exit 4};" +
            "$voice=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + voiceBase64 + "'));" +
            "if([string]::IsNullOrWhiteSpace($voice)){if($voices -contains 'Microsoft Hanhan Desktop'){$voice='Microsoft Hanhan Desktop'}elseif($voices -contains 'Microsoft Zira Desktop'){$voice='Microsoft Zira Desktop'}else{$voice=$voices[0]}};" +
            "if($voices -notcontains $voice){Write-Output ('TTSVOICEERROR|Requested voice not installed or disabled: ' + $voice);exit 5};" +
            "try{$synth.SelectVoice($voice)}catch{Write-Output ('TTSVOICEERROR|SelectVoice failed: ' + $_.Exception.Message);exit 6};" +
            "Write-Output ('TTSVOICE|' + $synth.Voice.Name);" +
            "$synth.Rate=" + job.rate + ";" +
            "$synth.Volume=" + job.volume + ";" +
            "$text=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + messageBase64 + "'));" +
            "Write-Output ('TTSTEXTBASE64|' + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($text)));" +
            "Write-Output ('TTSTEXT|' + $text);" +
            "if([string]::IsNullOrWhiteSpace($text)){exit 2};" +
            "$synth.Speak($text);$synth.Dispose();exit 0" +
            "}catch{Write-Output ('TTSERROR|' + $_.Exception.ToString());if($null -ne $synth){$synth.Dispose()};exit 3}";

        string encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        string systemDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
        string executable = Path.Combine(systemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");
        if (!File.Exists(executable)) executable = "powershell.exe";

        return new ProcessStartInfo
        {
            FileName = executable,
            Arguments = "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand " + encodedCommand,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
    }

    private static string CleanMessageForSpeech(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return string.Empty;
        string cleaned = message.Trim();

        if (cleaned.StartsWith("{", StringComparison.Ordinal))
        {
            try
            {
                SpeechEnvelope envelope = JsonUtility.FromJson<SpeechEnvelope>(cleaned);
                if (envelope == null || string.IsNullOrWhiteSpace(envelope.message)) return string.Empty;
                cleaned = envelope.message;
            }
            catch { return string.Empty; }
        }
        else if (cleaned.StartsWith("[", StringComparison.Ordinal) &&
                 cleaned.EndsWith("]", StringComparison.Ordinal)) return string.Empty;

        cleaned = WebUtility.HtmlDecode(cleaned);
        cleaned = Regex.Replace(cleaned, @"(?s)`{3}.*?`{3}", " ");
        cleaned = Regex.Replace(cleaned, @"(?im)^\s*\*?(?:feedbackType|action|priority|cooldownSeconds|requestId|sessionId|debug(?:ging)?(?:\s+info)?|unity\s+log)\*?\s*[:=][^\r\n]*(?:\r?\n|$)", " ");
        cleaned = Regex.Replace(cleaned, @"\[(?:AIFeedbackClient|AIFeedbackVoice|DEBUG|INFO|WARNING|ERROR)\]\s*", " ", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"\[([^\]]+)\]\([^)]+\)", "$1");
        cleaned = Regex.Replace(cleaned, @"</?[^>]+>", " ");
        cleaned = Regex.Replace(cleaned, @"\*\*(.*?)\*\*|__(.*?)__", "$1$2");
        cleaned = Regex.Replace(cleaned, @"(?m)^\s*(?:>\s*|[-*+]\s+|\d+[.)]\s+)", " ");
        cleaned = Regex.Replace(cleaned, @"[*_~#`]", " ");
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

        if (cleaned.StartsWith("{", StringComparison.Ordinal) ||
            cleaned.StartsWith("[", StringComparison.Ordinal)) return string.Empty;
        return cleaned;
    }
}
