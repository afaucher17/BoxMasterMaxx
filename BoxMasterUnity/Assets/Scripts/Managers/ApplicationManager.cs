﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using System;
using System.Xml.Serialization;
using System.Linq;
using CRI.HitBox.Serial;
using CRI.HitBox.Settings;
using CRI.HitBox.Lang;
using CRI.HitBox.Game;
using System.IO.Ports;

namespace CRI.HitBox
{

    public enum ApplicationState
    {
        None,
        Home,
        Pages,
        Setup,
        PreGame,
        Sleep,
        Game,
        End,
    }

    public enum GameMode
    {
        [XmlEnum(Name = "P1")]
        P1,
        [XmlEnum(Name = "P2")]
        P2,
    }

    public class ApplicationManager : MonoBehaviour
    {
        public delegate void ApplicationManagerEvent();
        public delegate void GameModeEvent(GameMode gameMode, int soloIndex);
        public static event ApplicationManagerEvent onTimeOutScreen;
        public static event ApplicationManagerEvent onActivity;
        public static event ApplicationManagerEvent onReturnToHome;
        public static event ApplicationManagerEvent onStartPages;
        public static event GameModeEvent onSetupStart;
        public static event ApplicationManagerEvent onSetupEnd;
        public static event GameModeEvent onGameStart;
        public static event ApplicationManagerEvent onGameEnd;

        public static ApplicationManager instance
        {
            get
            {
                if (s_instance == null)
                {
                    new GameObject("GameManager").AddComponent<ApplicationManager>().Init();
                }
                return s_instance;
            }
        }

        private static ApplicationManager s_instance = null;

        /// <summary>
        /// The game settings.
        /// </summary>
        public ApplicationSettings appSettings;

        /// <summary>
        /// The serial settings.
        /// </summary>
        public SerialSettings serialSettings
        {
            get
            {
                return appSettings.serialSettings;
            }
        }

        public MenuSettings menuSettings
        {
            get
            {
                return appSettings.menuSettings;
            }
        }

        public GameSettings gameSettings
        {
            get
            {
                return appSettings.gameSettings;
            }
        }

        /// <summary>
        /// The current state of the game
        /// </summary>
        [Tooltip("The current state of the game")]
        [SerializeField]
        private ApplicationState _appState = ApplicationState.None;

        /// <summary>
        /// The current game mode.
        /// </summary>
        public GameMode gameMode { get; private set; }

        /// <summary>
        /// The time of the first cooldown.
        /// </summary>
        [SerializeField]
        [Tooltip("The time of the first cooldown.")]
        protected float _time1 = 0;
        /// <summary>
        /// The time of the second cooldown.
        /// </summary>
        [SerializeField]
        [Tooltip("The time of the second cooldown.")]
        protected float _time2 = 0;
        /// <summary>
        /// Time after the game started.
        /// </summary>
        [SerializeField]
        [Tooltip("Time after the game started")]
        protected float _gameTime = 0;
        /// <summary>
        /// True if sleeping mode activated.
        /// </summary>
        [Tooltip("True if sleeping mode activated.")]
        protected bool _sleep = true;

        /// <summary>
        /// Countdown until timeout.
        /// If the timeout value is greater than the timeOutScreen value in the game settings, the timeout screen will show.
        /// </summary>
        public float timeOut1
        {
            get
            {
                return Time.time - _time1;
            }
        }

        /// <summary>
        /// Countdown until the game is reset.
        /// This second countdown starts after the first countdown. If the timeout value is greater than the TimeOut value in the game settings, the game will go back to the catchscreen.
        /// </summary>
        public float timeOut2
        {
            get
            {
                return Time.time - _time2;
            }
        }

        /// <summary>
        /// The time since the game started.
        /// </summary>
        public float timeLeft
        {
            get
            {
                return gameSettings.gameDuration - (Time.time - _gameTime);
            }
        }

        /// <summary>
        /// Rank of the last player.
        /// </summary>
        public int rank { get; private set; }
        /// <summary>
        /// Camera of the first player.
        /// </summary>
        public MainCamera player1Camera { get; private set; }
        /// <summary>
        /// Camera of the second player.
        /// </summary>
        public MainCamera player2Camera { get; private set; }
        /// <summary>
        /// All the serial controllers. If there's one player, there should be two serials. If there's two players, the number of serials should be four.
        /// </summary>
        public GameObject[] serialControllers
        {
            get
            {
                return _serialControllers;
            }
        }
        /// <summary>
        /// The console text of the P1 display.
        /// </summary>
        [SerializeField]
        [Tooltip("The console text of the P1 display.")]
        protected Text _p1ConsoleText;
        /// <summary>
        /// The console text of the P2 display.
        /// </summary>
        [SerializeField]
        [Tooltip("The console text of the P2 display.")]
        protected Text _p2ConsoleText;
        /// <summary>
        /// Index of the player in solo mode.
        /// </summary>
        public int soloIndex { get; private set; }
        /// <summary>
        /// The gameplay manager.
        /// </summary>
        [SerializeField]
        [Tooltip("The gameplay manager.")]
        protected GameManager _gameManager;
        /// <summary>
        /// The prefab of the serial controller gameObject.
        /// </summary>
        [SerializeField]
        [Tooltip("Prefab of the serial controller gameObject.")]
        protected GameObject _serialControllerPrefab;
        /// <summary>
        /// The serial controller objects. Each entry corresponds to one player.
        /// </summary>
        protected GameObject[] _serialControllers = new GameObject[ApplicationSettings.PlayerNumber];

        private PlayerDatabase _database = null;

        public PlayerDatabase database
        {
            get
            {
                return _database;
            }
        }

        protected PlayerData _p1Data;

        protected PlayerData _p2Data;

        /// <summary>
        /// True if the game has started.
        /// </summary>
        public bool gamePhase
        {
            get { return _appState == ApplicationState.Game; }
        }

        /// <summary>
        /// True if the game is in its setup phase.
        /// </summary>
        public bool setupPhase
        {
            get { return _appState == ApplicationState.Setup; }
        }
        /// <summary>
        /// Path for the game settings.
        /// </summary>
        public const string appSettingsPath = "init.xml";

        public const string playerDatabasePath = "players.csv";

        public GameManager gameManager
        {
            get
            {
                return GetComponent<GameManager>();
            }
        }

        private void OnEnable()
        {
            ImpactPointControl.onImpact += OnImpact;
        }

        private void OnDisable()
        {
            ImpactPointControl.onImpact -= OnImpact;
        }

        /// <summary>
        /// Called whenever the OnImpact event of any ImpactPointControl is triggered. Counts the impact has a player activity.
        /// </summary>
        /// <param name="position">Position of the impact in the world.</param>
        /// <param name="playerIndex">Index of which player triggered the impact.</param>
        private void OnImpact(Vector2 position, int playerIndex)
        {
            Debug.Log("IMPACT: " + position);
            Activity();
        }

        /// <summary>
        /// Whenever this is called, disable the sleep mode and reset all timeout countdowns.
        /// </summary>
        private void Activity()
        {
            if (_appState != ApplicationState.Home)
            {
                _time1 = Time.time;
                _time2 = Time.time;
                _sleep = false;
                onActivity();
            }
        }

        private void Awake()
        {
            Init();
        }

        private void Start()
        {
            player1Camera = GameObject.FindGameObjectWithTag("Player1Camera").GetComponent<MainCamera>();
            player2Camera = GameObject.FindGameObjectWithTag("Player2Camera").GetComponent<MainCamera>();

            InitSerialControllers();
        }

        /// <summary>
        /// Init the GameManager.
        /// </summary>
        private void Init()
        {
            if (s_instance == null)
            {
                s_instance = this;
                DontDestroyOnLoad(gameObject);
                appSettings = ApplicationSettings.Load(Path.Combine(Application.streamingAssetsPath, appSettingsPath));
                appSettings.Save("test.xml");
                _database = PlayerDatabase.Load(Path.Combine(Application.streamingAssetsPath, playerDatabasePath));
                _appState = ApplicationState.Home;
                Cursor.visible = appSettings.cursorVisible;
                _sleep = false;
            }
            else if (s_instance != this)
            {
                Destroy(gameObject);
                Destroy(this);
            }
        }

        /// <summary>
        /// Init all the serial controller.
        /// </summary>
        private void InitSerialControllers()
        {
            _serialControllers = new GameObject[ApplicationSettings.PlayerNumber];

            for (int p = 0; p < ApplicationSettings.PlayerNumber; p++)
            {
                var go = GameObject.Instantiate(_serialControllerPrefab, this.transform);
                go.name = "Serial Controller" + p;
                go.GetComponent<SerialLedController>().Init(p,
                    serialSettings.ledControllerGrid.rows,
                    serialSettings.ledControllerGrid.cols,
                    serialSettings.ledControllerSettings[p].name,
                    serialSettings.ledControllerSettings[p].baudRate,
                    serialSettings.ledControllerSettings[p].readTimeout,
                    (Handshake)serialSettings.ledControllerSettings[p].handshake,
                    GetCamera(p).GetComponent<Camera>()
                    );
                go.GetComponent<SerialTouchController>().Init(p,
                    serialSettings.touchControllerGrid.rows,
                    serialSettings.touchControllerGrid.cols,
                    serialSettings.touchControllerSettings[p].name,
                    serialSettings.touchControllerSettings[p].baudRate,
                    serialSettings.touchControllerSettings[p].readTimeout,
                    (Handshake)serialSettings.touchControllerSettings[p].handshake,
                    GetCamera(p).GetComponent<Camera>(),
                    serialSettings.impactThreshold
                    );
                _serialControllers[p] = go;
            }
        }

        private void Update()
        {
            if (Input.anyKeyDown)
            {
                Activity();
            }
            if (Input.GetKeyUp(KeyCode.F1) /*|| Input.GetMouseButtonUp(1)*/)
            {
                Home();
            }
            if (Input.GetKeyUp(KeyCode.F2))
            {
                gameMode = GameMode.P1;
                soloIndex = UnityEngine.Random.Range(0, 2);
                if (onSetupEnd != null)
                    onSetupEnd();
            }
            if (Input.GetKeyUp(KeyCode.F3))
            {
                gameMode = GameMode.P2;
                if (onSetupEnd != null)
                    onSetupEnd();
            }
            if (_appState == ApplicationState.Game && timeLeft < 0.0f)
            {
                EndGame();
            }
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                Application.Quit();
            }
        }

        /// <summary>
        /// Sends the player back to home.
        /// </summary>
        public void Home()
        {
            _appState = ApplicationState.Home;
            if (onReturnToHome != null)
                onReturnToHome();
            StopAllCoroutines();
        }

        /// <summary>
        /// Start the pages.
        /// </summary>
        public void StartPages(LangApp lang)
        {
            _appState = ApplicationState.Pages;
            TextManager.instance.currentLang = lang;
            if (onStartPages != null)
                onStartPages();
            StartCoroutine(TimeOut());
        }

        /// <summary>
        /// Starts the setup phase of the game.
        /// </summary>
        public void StartSetup()
        {
            _appState = ApplicationState.Setup;
            if (onSetupStart != null)
                onSetupStart(gameMode, soloIndex);
        }

        public void EndSetup()
        {
            _appState = ApplicationState.PreGame;
            if (onSetupEnd != null)
                onSetupEnd();
        }

        /// <summary>
        /// Start the game.
        /// </summary>
        public void StartGame()
        {
            if (_appState != ApplicationState.Game)
            {
                _appState = ApplicationState.Game;
                _gameTime = Time.time;
                if (onGameStart != null)
                    onGameStart(gameMode, soloIndex);
            }
        }

        public void EndGame()
        {
            _appState = ApplicationState.End;
            var playersToWrite = new List<PlayerData>();
            if (gameMode == GameMode.P2 || (gameMode == GameMode.P1 && soloIndex == 0))
            {
                _p1Data.score = gameManager.playerScore;
                _p1Data.precision = gameManager.precision;
                _p1Data.speed = gameManager.speed;
                _database.players.Add(_p1Data);
                playersToWrite.Add(_p1Data);
            }
            if (gameMode == GameMode.P2 || (gameMode == GameMode.P1 && soloIndex == 1))
            {
                _p2Data.score = gameManager.playerScore;
                _p2Data.precision = gameManager.precision;
                _p2Data.speed = gameManager.speed;
                _database.players.Add(_p2Data);
                playersToWrite.Add(_p2Data);
            }
            _database.Save(Path.Combine(Application.streamingAssetsPath, playerDatabasePath), playersToWrite);
            Activity();
            if (onGameEnd != null)
                onGameEnd();
        }

        /// <summary>
        /// Coroutine that checks whether the game should time out.
        /// </summary>
        /// <returns></returns>
        private IEnumerator TimeOut()
        {
            bool timeOutScreenOn = false;
            _time1 = Time.time;
            _time2 = 0.0f;

            while (true)
            {
                yield return null;
                if (!_sleep)
                {
                    if (timeOut1 >= menuSettings.timeoutScreen
                        && !timeOutScreenOn)
                    {
                        if (onTimeOutScreen != null)
                            onTimeOutScreen();
                        timeOutScreenOn = true;
                        _time2 = Time.time;
                    }
                    else if (timeOut1 <= menuSettings.timeoutScreen)
                    {
                        timeOutScreenOn = false;
                    }
                    if (timeOut2 >= menuSettings.timeout && timeOutScreenOn)
                    {
                        Home();
                        break;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            s_instance = null;
        }

        /// <summary>
        /// Sets the game mode.
        /// </summary>
        /// <param name="gameMode">The game mode.</param>
        public void SetGameMode(GameMode gameMode)
        {
            this.gameMode = gameMode;
            if (gameMode == GameMode.P1)
            {
                soloIndex = UnityEngine.Random.Range(0, 2);
            }
        }

        /// <summary>
        /// Gets the camera of a corresponding player.
        /// </summary>
        /// <param name="playerIndex">The index of the player.</param>
        /// <returns>A camera.</returns>
        public MainCamera GetCamera(int playerIndex)
        {
            if (playerIndex == 0)
                return player1Camera;
            if (playerIndex == 1)
                return player2Camera;
            if (playerIndex == 2)
                return Camera.main.GetComponent<MainCamera>();
            return null;
        }

        /// <summary>
        /// Gets the console of a corresponding player. Used for Debug only.
        /// </summary>
        /// <param name="playerIndex">Index of the player.</param>
        /// <returns>The console.</returns>
        public Text GetConsoleText(int playerIndex)
        {
            if (playerIndex == 0)
                return _p1ConsoleText;
            if (playerIndex == 1)
                return _p2ConsoleText;
            return null;
        }

        public void AddPlayer(List<String> answers)
        {
            int index = _database.players.Count + 1;
            var p = new PlayerData(index, soloIndex, 0, GameMode.P1, DateTime.Now, 0, 0.0f, 0.0f, answers);
            if (soloIndex == 0)
                _p1Data = p;
            else
                _p2Data = p;
        }

        public void AddPlayer(List<string> p1Answers, List<string> p2Answers)
        {
            int p1Index = _database.players.Count + 1;
            int p2Index = _database.players.Count + 2;
            var p1 = new PlayerData(p1Index, 0, p2Index, GameMode.P2, DateTime.Now, 0, 0.0f, 0.0f, p1Answers);
            var p2 = new PlayerData(p2Index, 1, p1Index, GameMode.P2, DateTime.Now, 0, 0.0f, 0.0f, p2Answers);
            _p1Data = p1;
            _p2Data = p2;
        }

        public int GetRank(GameMode mode, int score)
        {
            return rank;
        }
    }
}