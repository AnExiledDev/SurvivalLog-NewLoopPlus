using System;
using System.Collections.Generic;
using System.Threading;
using ExiledToolkit.Kit;
using UnityEngine;

namespace NewLoopPlus.Adapters;

/// <summary>
/// The Unity object that hands frames to the table checks. Each check runs on its own, so one that fails cannot stop
/// the others. Each distinct failure is logged once, never rethrown into the game; a failure that repeats every frame
/// would otherwise flood the log. The table checks run after a wake (<see cref="Wake"/>).
/// </summary>
public sealed class ScaleTicker : MonoBehaviour
{
    private const int MaxReportedFailures = 20;

    private const double PollSeconds = 1;

    internal static readonly List<Action<double>> Checks = new();
    internal static Action<string> Log = _ => { };
    private static readonly HashSet<string> ReportedFailures = new();

    private static int _wakes;
    private static double _nextPollAt;
    private int _nextCheck;

    /// <summary>How many wakes there have been. Each table check runs again after one.</summary>
    internal static int Wakes => Volatile.Read(ref _wakes);

    /// <summary>Set when a wake hook could not go in, so the checks wake every second as they did before.</summary>
    internal static bool IsPolling;

    public ScaleTicker(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (Checks.Count == 0) return;

        // One check per frame: each check keeps its own interval, so spreading them keeps any one frame short.
        _nextCheck %= Checks.Count;

        try
        {
            var now = Clock.Now;

            if (IsPolling && now >= _nextPollAt)
            {
                _nextPollAt = now + PollSeconds;
                Wake();
            }

            Checks[_nextCheck++](now);
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
        }
    }

    /// <summary>Makes every table check run again. Safe from any thread.</summary>
    internal static void Wake() => Interlocked.Increment(ref _wakes);

    private static void ReportFailure(Exception ex)
    {
        if (ReportedFailures.Count >= MaxReportedFailures || !ReportedFailures.Add(ex.GetType().FullName + ": " + ex.Message)) return;

        Log("update failed (each distinct failure reported once): " + ex);

        if (ReportedFailures.Count == MaxReportedFailures) Log($"{MaxReportedFailures} distinct update failures reported; further ones are not logged");
    }
}
