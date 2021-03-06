﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CRI.HitBox.Settings;
using CRI.HitBox.Lang;

namespace CRI.HitBox.UI
{
    public class UILangSelectButton : MonoBehaviour
    {
        public LangApp lang;

        public string textKey;

        [SerializeField]
        protected Button _button;
        [SerializeField]
        protected Text _text;
        [SerializeField]
        protected Text _highlightedText;
        [SerializeField]
        protected Image _background;
        [SerializeField]
        protected Animator _animator;

        private void Start()
        {
            if (_animator == null)
                _animator = GetComponent<Animator>();

            if (_button == null)
                _button = GetComponentInChildren<Button>();
            _button.onClick.AddListener(() =>
               {
                   ApplicationManager.instance.StartPages(lang);
               });

            _background.color = lang.color;

            if (_text != null)
            {
                _text.text = TextManager.instance.GetText(textKey, lang.code);
                _text.fontStyle = ApplicationManager.instance.appSettings.defaultLanguage.Equals(lang) ? FontStyle.Bold : FontStyle.Normal;
            }
            if (_highlightedText != null)
            {
                _highlightedText.text = TextManager.instance.GetText(textKey, lang.code);
                _highlightedText.fontStyle = ApplicationManager.instance.appSettings.defaultLanguage.Equals(lang) ? FontStyle.Bold : FontStyle.Normal;
                _highlightedText.color = lang.color;
            }
        }
    }
}