using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Stage;
using Enemies;
namespace Core.Scene
{
    public class GameSceneManager : MonoBehaviour
    {
        IStageService _stage;

        public void Init(IStageService stageService)
        {
            _stage = stageService;

            stageService.OnExceedEnemyCount += OnFailWave;
            stageService.OnTimeOut += OnFailWave;
        }

        private void OnFailWave()
        {
            Debug.Log("Á³´Ù!!");
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Game Start", GUILayout.Width(200), GUILayout.Height(50)))
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
