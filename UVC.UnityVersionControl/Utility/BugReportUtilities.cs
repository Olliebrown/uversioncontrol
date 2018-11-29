﻿// Copyright (c) <2018>
// This file is subject to the MIT License as seen in the trunk of this repository
// Maintained by: <Kristian Kjems> <kristian.kjems+UnityVC@gmail.com>
using System;
using UnityEditor;
using UnityEngine;
using UVC;
using UVC.Logging;

internal static class FogbugzUtilities
{
    private const int maxRetryCount = 5;

    public static void SubmitAutoBug(string title, string description)
    {
        SubmitBug("https://uversioncontrol.fogbugz.com/scoutSubmit.asp", "Kristian Kjems", "AutoReports", "Default", title, description, "", false);
    }

    public static void SubmitUserBug(string title, string description, string email)
    {
        SubmitBug("https://uversioncontrol.fogbugz.com/scoutSubmit.asp", "Kristian Kjems", "Inbox", "Undecided", title, description, email, true);
    }

    public static void SubmitBug(string url, string username, string project, string area, string description, string extra, string email, bool forceNewBug = false, int retryCount = 0)
    {
        string bugUrl =
            $"{url}?Description={WWW.EscapeURL(description)}&Extra={WWW.EscapeURL(extra)}&Email={WWW.EscapeURL(email)}&ScoutUserName={WWW.EscapeURL(username)}&ScoutProject={WWW.EscapeURL(project)}&ScoutArea={WWW.EscapeURL(area)}&ForceNewBug={(forceNewBug ? "1" : "0")}";
        
        var www = new WWW(bugUrl);
        ContinuationManager.Add(() => www.isDone, () =>
        {
            bool success = string.IsNullOrEmpty(www.error) && www.text.Contains("<Success>");
            if (success)
            {
                DebugLog.Log("Bug successfully reported to the 'Unity Version Control' FogBugz database.");
            }
            else
            {
                if (retryCount <= maxRetryCount)
                {
                    SubmitBug(url, username, project, area, description, extra, email, forceNewBug, ++retryCount);
                }
                else
                {
                    DebugLog.LogError("Bug report failed:\n" + www.error);
                }
            }
        });
    }
}

internal static class GitHubUtilities
{
    public static void OpenNewIssueInBrowser(string user, string repo)
    {
        var url = $"https://github.com/{user}/{repo}/issues/new";
        try
        {
            System.Diagnostics.Process.Start(url);
        }
        catch (Exception)
        {
            Debug.LogError("No default web browser installed so unable to open new github issue. Use following URL:\n" + url);
        }
    }
}

