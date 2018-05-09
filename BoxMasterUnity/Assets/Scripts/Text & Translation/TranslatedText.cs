﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class TranslatedText : MonoBehaviour
{
    /// <summary>
    /// The key to the text to translate.
    /// </summary>
    [Tooltip("The key to the text to translate.")]
    [SerializeField]
    protected string _textKey;

    public string textKey
    {
        get
        {
            return _textKey;
        }
    }

    [SerializeField]
    private Text _text;

    private void OnEnable()
    {
        TextManager.onLangChange += OnLangChange;
    }

    private void OnLangChange(LangApp lang)
    {
        SetText();
    }

    private void SetText()
    {
        if (textKey == "")
            Debug.LogWarning("Missing Text Key");
        else
            _text.text = TextManager.instance.GetText(textKey);
    }

    private void Start()
    {
        _text = GetComponent<Text>();
        SetText();
    }

    private void OnDisable()
    {
        TextManager.onLangChange -= OnLangChange;
    }
}