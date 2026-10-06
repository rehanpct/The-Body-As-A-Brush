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

/// <summary>
/// Speaks automatic AI feedback through the Windows voice installed on this PC.
/// Manual review is routed by AIFeedbackClient directly to its text panel.
/// </summary>
public sealed class AIFeedbackVoice : MonoBehaviour
{
    [Header("Windows Voice")]
    [SerializeField] private bool voiceEnabled = true;
    [SerializeField, Range(-10, 10)] private int speechRate = 0;
    [SerializeField, Range(0, 100)] private int volume = 100;
    [SerializeField, Tooltip("Choose an installed Windows voice. If blank, Zira or the first installed voice is used.")]
    private string windowsVoiceName = string.Empty;
    [SerializeField, Tooltip("When enabled, new feedback interrupts the current speech. Otherwise, it is queued.")]
    private bool interruptCurrentSpeech = true;

    private const int SpeechTimeoutMilliseconds = 90000;

    private readonly object stateLock = new object();
    private readonly Queue<SpeechJob> pendingSpeech = new Queue<SpeechJob>();
    private readonly Queue<VoiceLog> pendingLogs = new Queue<VoiceLog>();

    private bool speechWorkerRunning;
    private bool shuttingDown;
    private Process activeProcess;
    private SpeechJob activeJob;

    [Serializable]
    private sealed class SpeechEnvelope
    {
        public string message;
    }

    private sealed class SpeechJob
    {
        public readonly string message;
        public readonly int rate;
        public readonly int volume;
        public readonly string voiceName;
        public bool cancelled;

        public SpeechJob(string text, int speechRate, int speechVolume, string selectedVoice)
        {
            message = text;
            rate = speechRate;
            volume = speechVolume;
            voiceName = selectedVoice;
        }
    }

    private sealed class VoiceLog
    {
        public readonly string message;
        public readonly bool warning;

        public VoiceLog(string text, bool isWarning)
        {
            message = text;
            warning = isWarning;
        }
    }

    /// <summary>UnityEvent<string> receiver for automatic AI feedback.</summary>
    public void Speak(string message)
    {
        if (!voiceEnabled)
        {
            Debug.Log("[AIFeedbackVoice] Voice disabled.");
            return;
        }

        if (!IsWindows())
        {
            Debug.LogWarning("[AIFeedbackVoice] Windows TTS unavailable. Continuing without voice output.");
            return;
        }

        string cleanMessage = CleanMessageForSpeech(message);
        if (string.IsNullOrWhiteSpace(cleanMessage))
        {
            Debug.LogWarning("[AIFeedbackVoice] No human-readable message to speak.");
            return;
        }

        SpeechJob job = new SpeechJob(
            cleanMessage,
            Mathf.Clamp(speechRate, -10, 10),
            Mathf.Clamp(volume, 0, 100),
            windowsVoiceName == null ? string.Empty : windowsVoiceName.Trim()
        );

        lock (stateLock)
        {
            if (shuttingDown)
                return;

            if (interruptCurrentSpeech)
            {
                CancelActiveJobLocked();
                while (pendingSpeech.Count > 0)
                    pendingSpeech.Dequeue().cancelled = true;
            }

            pendingSpeech.Enqueue(job);
            if (!speechWorkerRunning)
            {
                speechWorkerRunning = true;
                try
                {
                    Task.Run(DrainSpeechQueue);
                }
                catch
                {
                    speechWorkerRunning = false;
                    pendingSpeech.Clear();
                    Debug.LogWarning("[AIFeedbackVoice] Windows TTS unavailable. Continuing without voice output.");
                    return;
                }
            }
        }

        Debug.Log("[AIFeedbackVoice] Speaking automatic AI feedback.");
    }

    private void Update()
    {
        while (true)
        {
            VoiceLog entry;
            lock (stateLock)
            {
                if (pendingLogs.Count == 0)
                    return;
                entry = pendingLogs.Dequeue();
            }

            if (entry.warning)
                Debug.LogWarning(entry.message);
            else
                Debug.Log(entry.message);
        }
    }

    private void OnDisable()
    {
        CancelSpeech(false);
    }

    private void OnDestroy()
    {
        CancelSpeech(true);
    }

    private void CancelSpeech(bool destroying)
    {
        lock (stateLock)
        {
            if (destroying)
                shuttingDown = true;

            CancelActiveJobLocked();
            while (pendingSpeech.Count > 0)
                pendingSpeech.Dequeue().cancelled = true;
        }
    }

    private void CancelActiveJobLocked()
    {
        if (activeJob != null)
            activeJob.cancelled = true;

        if (activeProcess == null)
            return;

        try
        {
            if (!activeProcess.HasExited)
                activeProcess.Kill();
        }
        catch
        {
            // The process may have completed between the check and Kill.
        }
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

            if (job.cancelled)
            {
                FinishJob(job);
                continue;
            }

            SpeakOnWorker(job);
        }
    }

    private void SpeakOnWorker(SpeechJob job)
    {
        Process process = null;
        try
        {
            process = new Process();
            process.StartInfo = BuildProcessStartInfo(job);

            lock (stateLock)
            {
                if (shuttingDown || job.cancelled)
                {
                    if (activeJob == job)
                        activeJob = null;
                    return;
                }

                if (!process.Start())
                    throw new InvalidOperationException("PowerShell did not start.");

                activeProcess = process;
            }

            QueueLog("[AIFeedbackVoice] Speech started.", false);
            bool exited = process.WaitForExit(SpeechTimeoutMilliseconds);
            if (!exited)
            {
                try { process.Kill(); }
                catch { }
            }

            int exitCode = exited ? process.ExitCode : -1;
            bool cancelled;
            lock (stateLock)
            {
                if (activeProcess == process)
                    activeProcess = null;
                if (activeJob == job)
                    activeJob = null;
                cancelled = job.cancelled || shuttingDown;
            }

            if (cancelled)
                return;

            if (exited && exitCode == 0)
                QueueLog("[AIFeedbackVoice] Speech finished.", false);
            else if (exited && exitCode == 4)
                QueueLog("[AIFeedbackVoice] No Windows TTS voices available.", true);
            else if (exited && exitCode == 5)
            {
                QueueLog("[AIFeedbackVoice] Selected voice not available: " + job.voiceName, true);
                QueueLog("[AIFeedbackVoice] Falling back to Windows default voice.", true);
                QueueLog("[AIFeedbackVoice] Speech finished.", false);
            }
            else
                QueueLog("[AIFeedbackVoice] Windows TTS unavailable. Continuing without voice output.", true);
        }
        catch
        {
            bool cancelled;
            lock (stateLock)
            {
                if (activeProcess == process)
                    activeProcess = null;
                if (activeJob == job)
                    activeJob = null;
                cancelled = job.cancelled || shuttingDown;
            }

            if (!cancelled)
                QueueLog("[AIFeedbackVoice] Windows TTS unavailable. Continuing without voice output.", true);
        }
        finally
        {
            if (process != null)
                process.Dispose();
        }
    }

    private void FinishJob(SpeechJob job)
    {
        lock (stateLock)
        {
            if (activeJob == job)
                activeJob = null;
        }
    }

    private void QueueLog(string message, bool warning)
    {
        lock (stateLock)
            pendingLogs.Enqueue(new VoiceLog(message, warning));
    }

    private static bool IsWindows()
    {
        return Application.platform == RuntimePlatform.WindowsEditor ||
               Application.platform == RuntimePlatform.WindowsPlayer;
    }

    private static ProcessStartInfo BuildProcessStartInfo(SpeechJob job)
    {
        string messageBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(job.message));
        string voiceBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(job.voiceName));
        string script =
            "$ErrorActionPreference='Stop';$synth=$null;try{" +
            "Add-Type -AssemblyName System.Speech;" +
            "$synth=New-Object System.Speech.Synthesis.SpeechSynthesizer;" +
            "$voices=@($synth.GetInstalledVoices()|Where-Object{$_.Enabled}|ForEach-Object{$_.VoiceInfo.Name});" +
            "if($voices.Count -eq 0){$synth.Dispose();exit 4};" +
            "$voice=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + voiceBase64 + "'));" +
            "if([string]::IsNullOrWhiteSpace($voice)){if($voices -contains 'Microsoft Zira Desktop'){$voice='Microsoft Zira Desktop'}else{$voice=$voices[0]}};" +
            "$fallback=$false;if($voices -contains $voice){$synth.SelectVoice($voice)}else{$fallback=$true};" +
            "$synth.Rate=" + job.rate + ";" +
            "$synth.Volume=" + job.volume + ";" +
            "$text=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + messageBase64 + "'));" +
            "if([string]::IsNullOrWhiteSpace($text)){exit 2};" +
            "$synth.Speak($text);$synth.Dispose();if($fallback){exit 5}else{exit 0}" +
            "}catch{if($null -ne $synth){$synth.Dispose()};exit 3}";
        string encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

        string systemDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
        string executable = Path.Combine(systemDirectory, "WindowsPowerShell\\v1.0\\powershell.exe");
        if (!File.Exists(executable))
            executable = "powershell.exe";

        return new ProcessStartInfo
        {
            FileName = executable,
            Arguments = "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand " + encodedCommand,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
    }

    private static string CleanMessageForSpeech(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;

        string cleaned = message.Trim();
        if (cleaned.StartsWith("{", StringComparison.Ordinal))
        {
            try
            {
                SpeechEnvelope envelope = JsonUtility.FromJson<SpeechEnvelope>(cleaned);
                if (envelope == null || string.IsNullOrWhiteSpace(envelope.message))
                    return string.Empty;
                cleaned = envelope.message;
            }
            catch
            {
                return string.Empty;
            }
        }
        else if (cleaned.StartsWith("[", StringComparison.Ordinal) && cleaned.EndsWith("]", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        cleaned = WebUtility.HtmlDecode(cleaned);
        cleaned = Regex.Replace(cleaned, @"(?s)\x60{3}.*?\x60{3}", " ");
        cleaned = Regex.Replace(cleaned, @"(?im)^[ \t]*(?:feedbackType|action|priority|cooldownSeconds|requestId|sessionId|debug(?:ging)?(?:\s+info)?|unity\s+log)[ \t]*[:=][^\r\n]*(?:\r?\n|$)", " ");
        cleaned = Regex.Replace(cleaned, @"\[(?:AIFeedbackClient|AIFeedbackVoice|DEBUG|INFO|WARNING|ERROR)\]\s*", " ", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"\[(?<label>[^\]]+)\]\([^)]+\)", "$1");
        cleaned = Regex.Replace(cleaned, @"</?[^>]+>", " ");
        cleaned = Regex.Replace(cleaned, @"\*\*(.*?)\*\*|__(.*?)__", "$1$2");
        cleaned = Regex.Replace(cleaned, @"(?m)^[ \t]*(?:>[ \t]*|[-*+][ \t]+|\d+[.)][ \t]+)", " ");
        cleaned = Regex.Replace(cleaned, @"[*_~#\x60]", " ");
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

        if (cleaned.StartsWith("{", StringComparison.Ordinal) || cleaned.StartsWith("[", StringComparison.Ordinal))
            return string.Empty;

        return cleaned;
    }
}
