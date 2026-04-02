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
        IFieldEnemyService _fieldEnemy;

        public void Init(IStageService stageService, IFieldEnemyService fieldEnemyService)
        {
            _stage = stageService;
            _fieldEnemy = fieldEnemyService;
        }

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(1.0f);
            _stage.StartStage();
        }
    }
}
