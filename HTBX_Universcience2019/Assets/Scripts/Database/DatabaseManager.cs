﻿using CRI.HitBox.Game;
using CRI.HitBox.Lang;
using CRI.HitBox.Settings;
using CRI.HitBox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CRI.HitBox.Database
{
    public class DatabaseManager : MonoBehaviour
    {
        private static DatabaseManager s_instance;

        public static DatabaseManager instance
        {
            get
            {
                return s_instance;
            }
        }

        public SessionData currentSession;

        public List<PlayerData> currentPlayers = new List<PlayerData>();

        public List<SurveyData> currentSurveys = new List<SurveyData>();

        public CrashData currentCrash;

        public InitData currentInit;

        private void OnEnable()
        {
            ApplicationManager.onReturnToHome += OnReturnToHome;
            ApplicationManager.onTimeOutScreen += OnTimeOutScreen;
            ApplicationManager.onStartPages += OnStartPages;
            ApplicationManager.onGameModeSet += OnGameModeSet;
            ApplicationManager.onGameStart += OnGameStart;
            ApplicationManager.onGameEnd += OnGameEnd;
            UISurveyScreen.onSurveyEnd += OnSurveyEnd;
            GameManager.onPlayerSetup += OnPlayerSetup;
            TargetController.onHit += OnHit;
        }

        private void OnDisable()
        {
            ApplicationManager.onReturnToHome -= OnReturnToHome;
            ApplicationManager.onTimeOutScreen -= OnTimeOutScreen;
            ApplicationManager.onStartPages -= OnStartPages;
            ApplicationManager.onGameModeSet -= OnGameModeSet;
            ApplicationManager.onGameStart -= OnGameStart;
            ApplicationManager.onGameEnd -= OnGameEnd;
            UISurveyScreen.onSurveyEnd -= OnSurveyEnd;
            GameManager.onPlayerSetup -= OnPlayerSetup;
            TargetController.onHit -= OnHit;
        }

        private void Awake()
        {
            s_instance = this;
        }

        private async void Start()
        {
            Debug.Log("Database start");
            DataService.databaseName = ApplicationManager.instance.appSettings.databaseServerURL;
            var resultInit = await DataService.LoadData<InitData>();
            var resultTCT = await DataService.LoadData<TargetCountThresholdData>();
            if (resultInit.successful && resultTCT.successful)
            {
                if (resultInit.dataList.Count > 0)
                {
                    foreach (InitData init in resultInit.dataList)
                    {
                        if (init.Equals(ApplicationManager.instance.appSettings))
                        {
                            var targetCountThresholds = resultTCT.dataList.Count > 0 ? resultTCT.dataList.Where(x => x.initId == init.id).ToList() : new List<TargetCountThresholdData>();
                            if (CheckTargetCountThresholds(targetCountThresholds, ApplicationManager.instance.gameSettings.targetCountThreshold))
                            {
                                currentInit = init;
                                break;
                            }
                        }
                    }
                }
                if (currentInit == null)
                {
                    var newInit = InitData.CreateFromApplicationSettings(ApplicationManager.instance.appSettings);
                    currentInit = newInit;
                    DataService.InsertData(currentInit, result =>
                    {
                        if (result.successful)
                        {
                            for (int i = 0; i < ApplicationManager.instance.gameSettings.targetCountThreshold.Length; i++)
                            {
                                DataService.InsertData(new TargetCountThresholdData(i, currentInit, ApplicationManager.instance.gameSettings.targetCountThreshold[i]), x =>
                                {
                                    return;
                                });
                            }
                        }
                        else
                        {
                            Debug.LogError("Error while inserting init file.");
                        }
                    });
                }
            }
            else
            {
                Debug.LogError("Couldn't find init file.");
            }
            if (currentInit == null)
            {
                Invoke("Start", ApplicationManager.instance.appSettings.databaseInitRefreshTime);
            }
        }

        private bool CheckTargetCountThresholds(List<TargetCountThresholdData> dataList, int[] settingsList)
        {
            if (currentInit == null || dataList.Count != settingsList.Length)
                return false;
            for (int i = 0; i < dataList.Count; i++)
            {
                if (!settingsList.Contains(dataList[i].countThreshold))
                    return false;
            }
            return true;
        }

        private async void OnSurveyEnd(List<string> answersP1, List<string> answersP2)
        {
            if (currentInit == null || currentPlayers.Count == 0)
                return;
            var player1 = currentPlayers.FirstOrDefault(x => x.playerIndex == 0);
            var player2 = currentPlayers.FirstOrDefault(x => x.playerIndex == 1);
            if (answersP1 != null && player1 != null)
            {
                foreach (var answer in answersP1)
                {
                    await DataService.InsertData(new SurveyData(player1, answer));
                }
            }
            if (answersP2 != null && player2 != null)
            {
                foreach (var answer in answersP2)
                {
                    await DataService.InsertData(new SurveyData(player2, answer));
                }
            }
        }

        private async void OnPlayerSetup(Vector2 position, int playerIndex)
        {
            if (currentInit == null || currentPlayers.Count == 0)
                return;
            var player = currentPlayers.FirstOrDefault(x => x.playerIndex == playerIndex);
            if (player != null)
            {
                player.setupHitPosition = position;
                await DataService.UpdateData(player);
            }
        }

        private async void OnGameModeSet(GameMode gameMode, int soloIndex)
        {
            if (currentInit != null && currentSession != null)
            {
                if (gameMode == GameMode.P1)
                {
                    var player = new PlayerData(0, currentSession, soloIndex, Vector3.zero);
                    currentPlayers.Add(player);
                    await DataService.InsertData(player);
                }
                if (gameMode == GameMode.P2)
                {
                    var player1 = new PlayerData(0, currentSession, 0, Vector3.zero);
                    var player2 = new PlayerData(0, currentSession, 1, Vector3.zero);
                    currentPlayers.Add(player1);
                    currentPlayers.Add(player2);
                    await DataService.InsertData(player1);
                    await DataService.InsertData(player2);
                }
                currentSession.gameMode = gameMode;
                await DataService.UpdateData(currentSession);
            }
        }

        private async void OnGameStart(GameMode gameMode, int soloIndex)
        {
            if (currentInit != null && currentSession != null)
            {
                currentSession.timeSpentOnMenu = (int)DateTime.Now.Subtract(currentSession.time).TotalSeconds;
                await DataService.UpdateData(currentSession);
            }
        }

        private async void OnTimeOutScreen()
        {
            if (currentInit != null && currentSession != null)
            {
                currentSession.timeoutScreenCount++;
                await DataService.UpdateData(currentSession);
            }
        }

        private async void OnStartPages(bool switchLanguages)
        {
            if (currentInit == null)
                return;
            if (switchLanguages)
                OnReturnToHome(HomeOrigin.Quit);
            currentSession = new SessionData(0, currentInit, DateTime.Now, TextManager.instance.currentLang.code);
            await DataService.InsertData(currentSession);
        }

        private async void OnReturnToHome(HomeOrigin homeOrigin)
        {
            var currentSession = this.currentSession;
            ResetSession();
            if (currentInit != null && currentSession != null)
            {
                currentSession.timeSpentTotal = (int)DateTime.Now.Subtract(currentSession.time).TotalSeconds;
                currentSession.timeout = homeOrigin == HomeOrigin.Timeout;
                currentSession.debugExit = homeOrigin == HomeOrigin.Debug;

                // If the game has started
                if (currentSession.timeSpentOnMenu != null)
                {
                    currentSession.score = ApplicationManager.instance.GetComponent<GameManager>().playerScore;
                    currentSession.speedRating = ApplicationManager.instance.GetComponent<GameManager>().speed;
                    currentSession.precisionRating = ApplicationManager.instance.GetComponent<GameManager>().precision;
                    currentSession.highestComboMultiplier = ApplicationManager.instance.GetComponent<GameManager>().highestComboMultiplier;
                }
                else
                {
                    currentSession.timeSpentOnMenu = currentSession.timeSpentTotal;
                }
                await DataService.UpdateData(currentSession);
            }
        }

        public void ResetSession()
        {
            currentSession = null;
            currentPlayers.Clear();
            currentSurveys = null;
            currentCrash = null;
        }

        private async void OnGameEnd()
        {
            if (currentInit != null && currentSession != null)
            {
                currentSession.score = ApplicationManager.instance.GetComponent<GameManager>().playerScore;
                currentSession.speedRating = ApplicationManager.instance.GetComponent<GameManager>().speed;
                currentSession.precisionRating = ApplicationManager.instance.GetComponent<GameManager>().precision;
                currentSession.highestComboMultiplier = ApplicationManager.instance.GetComponent<GameManager>().highestComboMultiplier;
                await DataService.UpdateData(currentSession);
            }
        }

        private async void OnHit(int playerIndex, Vector2 position, bool successful, Vector3? targetCenter, Vector3? speedVector)
        {
            if (currentInit != null && currentSession != null && currentPlayers.Count > 0)
            {
                var player = GetPlayer(playerIndex);
                if (player != null)
                {
                    await DataService.InsertData(new HitData(0, player, DateTime.Now, position, successful, targetCenter, speedVector));
                }
            }
        }

        public PlayerData GetPlayer(int playerIndex)
        {
            if (currentInit == null || currentPlayers.Count == 0)
                return null;
            return this.currentPlayers.FirstOrDefault(x => x.playerIndex == playerIndex);
        }
    }
}
