﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.Serialization;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Xml;
using System.Text;
using System.IO;
using CRI.HitBox.Lang;


namespace CRI.HitBox.Settings
{
    /// <summary>
    /// A button type for the menu bar.
    /// </summary>
    public enum ButtonType
    {
        [XmlEnum("start")]
        Start,
        [XmlEnum("sound")]
        Sound,
        [XmlEnum("copyright")]
        Copyright,
        [XmlEnum("separator")]
        Separator,
    }

    [System.Serializable]
    public struct P1Mode
    {
        [XmlAttribute("enabled")]
        public string enabledSerialized
        {
            get
            {
                return this.enabled ? "True" : "False";
            }
            set
            {
                if (value.ToUpper().Equals("TRUE"))
                    this.enabled = true;
                else if (value.ToUpper().Equals("FALSE"))
                    this.enabled = false;
                else
                    this.enabled = XmlConvert.ToBoolean(value);
            }
        }
        /// <summary>
        /// If true, only the one-player mode will be enabled.
        /// </summary>
        [XmlIgnore]
        public bool enabled { get; private set; }
        /// <summary>
        /// The index of the player that will be automatically chosen in P1Mode.
        /// </summary>
        [XmlAttribute("index")]
        public int p1Index;
    }

    /// <summary>
    /// The settings of the game, which are the different timeout values, the settings for the serial components, the settings for the gameplay and the settings for the differents pages of the interface.
    /// </summary>
    [System.Serializable]
    [XmlRoot(ElementName = "application_settings")]
    public class ApplicationSettings
    {
        /// <summary>
        /// The name of the application.
        /// </summary>
        public string name;
        /// <summary>
        /// All of the languages available for the translation of the application.
        /// </summary>
        [XmlArray("lang_app_available")]
        [XmlArrayItem(typeof(LangApp), ElementName = "lang_app")]
        [SerializeField]
        public LangApp[] langAppAvailable;

        /// <summary>
        /// Code of the enabled languages in priority order of appearance in the menus
        /// </summary>
        [XmlArray("lang_app_enable")]
        [XmlArrayItem(typeof(string), ElementName = "code")]
        [SerializeField]
        public string[] langAppEnableCodes;

        /// <summary>
        /// Enabled languages in priority order of appearance in the menus
        /// </summary>
        protected LangApp[] _langAppEnable;

        /// <summary>
        /// Enabled languages in priority order of appearance in the menus.
        /// </summary>
        [XmlIgnore]
        public IList<LangApp> langAppEnable
        {
            get
            {
                return _langAppEnable.ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Serialized version of the cursor visible field.
        /// </summary>
        [XmlElement("cursor_visible")]
        public string cursorVisibleSerialized
        {
            get { return this.cursorVisible ? "True" : "False"; }
            set
            {
                if (value.ToUpper().Equals("TRUE"))
                    this.cursorVisible = true;
                else if (value.ToUpper().Equals("FALSE"))
                    this.cursorVisible = false;
                else
                    this.cursorVisible = XmlConvert.ToBoolean(value);
            }
        }
        /// <summary>
        /// Is the cursor enabled ?
        /// </summary>
        [XmlIgnore]
        public bool cursorVisible { get; private set; }

        /// <summary>
        /// Is the p1 mode activated ?
        /// </summary>
        [XmlElement("p1_mode")]
        public P1Mode p1Mode;

        /// <summary>
        /// Color of the P1 as a hex.
        /// </summary>
        [XmlElement("p1_color")]
        public string p1Color;

        /// <summary>
        /// Color of the P2 as a hex.
        /// </summary>
        [XmlElement("p2_color")]
        public string p2Color;
        /// <summary>
        /// URL of the server where the database is hosted.
        /// </summary>
        [XmlElement("server_url")]
        public string databaseServerURL;
        /// <summary>
        /// Refresh time of the init file (in seconds). If the init file is not found at the start of the application, it will try again every N seconds.
        /// </summary>
        [XmlElement("database_init_refresh_time")]
        public float databaseInitRefreshTime;
        /// <summary>
        /// The settings for all the menu elements.
        /// </summary>
        [XmlElement("menu_settings")]
        public MenuSettings menuSettings;

        /// <summary>
        /// The settings for the gameplay components.
        /// </summary>
        [XmlElement("game_settings")]
        public GameSettings gameSettings;

        /// <summary>
        /// The settings for the serial components.
        /// </summary>
        [XmlElement("serial_settings")]
        public SerialSettings serialSettings;

        /// <summary>
        /// Gets the default language, which is the first of the list.
        /// </summary>
        /// <value>The default language.</value>
        [XmlIgnore]
        public LangApp defaultLanguage
        {
            get
            {
                return _langAppEnable[0];
            }
        }

        /// <summary>
        /// All audio paths.
        /// </summary>
        [XmlIgnore]
        public StringCommon[] allAudioPaths
        {
            get
            {
                return menuSettings.allMenuAudioPaths.Concat(gameSettings.allGameplayAudioPaths).ToArray();
            }
        }

        /// <summary>
        /// All video paths.
        /// </summary>
        [XmlIgnore]
        public StringCommon[] allVideoPaths
        {
            get
            {
                return menuSettings.allMenuVideoPaths;
            }
        }

        /// <summary>
        /// All image paths.
        /// </summary>
        [XmlIgnore]
        public StringCommon[] allImagePaths
        {
            get
            {
                return menuSettings.allMenuImagePaths;
            }
        }

        public const int PlayerNumber = 2;


        /// <summary>
        /// Save the data to an XML file.
        /// </summary>
        /// <param name="path">The path where the XML file will be saved.</param>
        public void Save(string path)
        {
            var serializer = new XmlSerializer(typeof(ApplicationSettings));
            using (var stream = new FileStream(Path.Combine(Application.dataPath, path), FileMode.Create))
            using (var xmlWriter = new XmlTextWriter(stream, Encoding.UTF8))
            {
                xmlWriter.Formatting = Formatting.Indented;
                serializer.Serialize(xmlWriter, this);
            }
        }

        /// <summary>
        /// Loads a game setting data from the XML file at the specified path.
        /// </summary>
        /// <returns>The game setting data.</returns>
        /// <param name="path">The path here the XML file is located.</param>
        public static ApplicationSettings Load(string path)
        {
            var serializer = new XmlSerializer(typeof(ApplicationSettings));
            using (var stream = new FileStream(Path.Combine(Application.dataPath, path), FileMode.Open))
            {
                var gameSettings = serializer.Deserialize(stream) as ApplicationSettings;
                gameSettings._langAppEnable = new LangApp[gameSettings.langAppEnableCodes.Length];
                for (int i = 0; i < gameSettings.langAppEnableCodes.Length; i++)
                {
                    var code = gameSettings.langAppEnableCodes[i];
                    var langApp = gameSettings.langAppAvailable.First(x => x.code == code);
                    gameSettings._langAppEnable[i] = langApp;
                }
                return gameSettings;
            }
        }

        /// <summary>
        /// Loads a game setting from an XML text.
        /// </summary>
        /// <returns>The game setting data.</returns>
        /// <param name="text">A text in XML format that contains the data that will be loaded.</param>
        public static ApplicationSettings LoadFromText(string text)
        {
            var serializer = new XmlSerializer(typeof(ApplicationSettings));
            return serializer.Deserialize(new StringReader(text)) as ApplicationSettings;
        }
    }
}
