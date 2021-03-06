﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CRI.HitBox.Lang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;

namespace CRI.HitBox.Settings
{
    [Serializable]
    public struct MenuSettings
    {
        /// <summary>
        /// Time until the application displays the timeout screen.
        /// </summary>
        [XmlElement("timeout_screen")]
        public int timeoutScreen;
        /// <summary>
        /// Time until the application displays the timeout screen while on the final score screen.
        /// </summary>
        [XmlElement("final_score_screen_timeout")]
        public int finalScoreScreenTimeout;

        /// <summary>
        /// Time until the application returns to the home screen.
        /// </summary>
        [XmlElement("timeout")]
        public int timeout;

        /// <summary>
        /// Time until the menu goes back to its initial position
        /// </summary>
        [XmlElement("menu_bar_timeout")]
        public int timeoutMenu;

        /// <summary>
        /// Volume of the audio.
        /// </summary>
        [XmlElement("audio_volume")]
        public float audioVolume;

        /// <summary>
        /// Layout of the menu bar.
        /// </summary>
        [XmlArray("menu_bar_layout")]
        [XmlArrayItem(typeof(ButtonType), ElementName = "button_type")]
        public ButtonType[] menuBarLayout;

        /// <summary>
        /// Array of ordered pages.
        /// </summary>
        [XmlArray("page_settings")]
        [XmlArrayItem(typeof(ContentPageSettings), ElementName = "content_page")]
        [XmlArrayItem(typeof(TextOnlyPageSettings), ElementName = "text_page")]
        [XmlArrayItem(typeof(PlayerModeSettings), ElementName = "mode_page")]
        [XmlArrayItem(typeof(CatchScreenSettings), ElementName = "catchscreen_page")]
        [XmlArrayItem(typeof(SurveyPageSettings), ElementName = "survey_page")]
        [SerializeField]
        public ScreenSettings[] pageSettings;

        /// <summary>
        /// Array of unordered screens.
        /// </summary>
        [XmlArray("screen_settings")]
        [XmlArrayItem(typeof(CountdownSettings), ElementName = "countdown")]
        [XmlArrayItem(typeof(CreditsSettings), ElementName = "credits")]
        [XmlArrayItem(typeof(BigScreenSettings), ElementName = "big_screen")]
        [XmlArrayItem(typeof(FinalScoreScreenSettings), ElementName = "final_score_screen")]
        [XmlArrayItem(typeof(ScoreScreenSettings), ElementName = "score_screen")]
        [SerializeField]
        public ScreenSettings[] screenSettings;

        /// <summary>
        /// The settings for the survey part of the application.
        /// </summary>
        [XmlElement("survey_settings")]
        public SurveySettings surveySettings;

        /// <summary>
        /// All the audio paths in the menu settings.
        /// </summary>
        [XmlIgnore]
        internal StringCommon[] allMenuAudioPaths
        {
            get
            {
                return pageSettings
                .Concat(screenSettings)
                .Where(x => x.GetType().GetInterfaces().Contains(typeof(IAudioContainer)))
                .SelectMany(x => ((IAudioContainer)x).GetAudioPaths())
                .Where(x => !String.IsNullOrEmpty(x.key))
                .Distinct()
                .ToArray();
            }
        }

        /// <summary>
        /// All the video paths in the menu settings.
        /// </summary>
        [XmlIgnore]
        internal StringCommon[] allMenuVideoPaths
        {
            get
            {
                return pageSettings
                    .Concat(screenSettings)
                    .Where(x => x.GetType().GetInterfaces().Contains(typeof(IVideoContainer)))
                    .SelectMany(x => ((IVideoContainer)x).GetVideoPaths())
                    .Where(x => !String.IsNullOrEmpty(x.key))
                    .Distinct()
                    .ToArray();
            }
        }

        /// <summary>
        /// All the image paths in the menu settings.
        /// </summary>
        [XmlIgnore]
        internal StringCommon[] allMenuImagePaths
        {
            get
            {
                return pageSettings
               .Concat(screenSettings)
               .Where(x => x.GetType().GetInterfaces().Contains(typeof(IImageContainer)))
               .SelectMany(x => ((IImageContainer)x).GetImagePaths())
               .Where(x => !String.IsNullOrEmpty(x.key))
               .Distinct()
               .ToArray();
            }
        }

        public MenuSettings(int timeOutScreen,
            int timeOut,
            int timeOutMenu,
            int finalScoreScreenTimeout,
            ScreenSettings[] pageSettings,
            ScreenSettings[] screenSettings,
            string catchScreenVideoPath,
            string bigScreenVideoPath,
            ButtonType[] menuLayout,
            SurveySettings surveySettings,
            float audioVolume)
        {
            this.timeoutScreen = timeOutScreen;
            this.timeout = timeOut;
            this.finalScoreScreenTimeout = finalScoreScreenTimeout;
            this.timeoutMenu = timeOutMenu;
            this.pageSettings = pageSettings;
            this.screenSettings = screenSettings;
            this.menuBarLayout = menuLayout;
            this.surveySettings = surveySettings;
            this.audioVolume = audioVolume;
        }
    }
}