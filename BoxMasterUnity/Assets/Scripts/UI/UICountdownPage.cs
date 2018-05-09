﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class UICountdownPage : MonoBehaviour, IHideable
{
    [SerializeField]
    protected CanvasGroup _canvasGroup;
    [SerializeField]
    protected Text _countdownText;
    [SerializeField]
    protected int _countdown = 3;

    private void Start()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
        if (_countdownText == null)
            _countdownText = GetComponentInChildren<Text>();
        Hide();
    }

    public void Hide()
    {
        _canvasGroup.alpha = 0;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    public void Show()
    {
        _canvasGroup.alpha = 1;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
        StartCoroutine(Countdown());
    }

    private IEnumerator Countdown()
    {
        int countdown = _countdown;
        while (countdown >= 0)
        {
            yield return new WaitForSeconds(1.0f);
            countdown--;
            _countdownText.text = (countdown > 0) ? countdown.ToString() : "Go";
        }
        GetComponentInParent<UIScreenMenu>().GoToScoreScreen();
        GameManager.instance.StartGame();
    }
}