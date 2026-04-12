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

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(1.0f);
            _stage.StartStage();
        }

        private void OnFailWave()
        {
            Debug.Log("Á³´Ù!!");
        }
    }
}
