using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Stage;
using Enemies;
namespace Core.Scene
{
    public class GameSceneManager : MonoBehaviour
    {
        IStageService _stage;

        public void Init(IStageService stageService)
        {
            _stage = stageService;

            _stage.OnStageFail += OnFailWave;
            _stage.OnStageClear += OnVictory;
        }

        private void OnFailWave()
        {
            Debug.Log("¡≥¥Ÿ!!");
        }

        private void OnVictory()
        {
            Debug.Log("¿Ã∞Â¥Ÿ!");
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Game Start", GUILayout.Width(400), GUILayout.Height(150)))
            {
                _stage.StartStage();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }
    }
}
