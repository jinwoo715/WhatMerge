using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface IStageView
{
    void SetCurrentWave(string wave);
    void SetRemainTime(string time);
    void SetActiveEnemy(string activeEnemy, float ratio);
}

public class StageViewer : MonoBehaviour, IStageView
{
    [SerializeField] private TMP_Text _currentWaveText;
    [SerializeField] private TMP_Text _remainTimeText;

    [SerializeField] private TMP_Text _activeEnemyCountText;
    [SerializeField] private Image _activeEnemySlideImage;

    public void SetCurrentWave(string wave)
    {
        _currentWaveText.text = wave;
    }
    public void SetRemainTime(string time)
    {
        _remainTimeText.text = time;
    }
    public void SetActiveEnemy(string activeEnemy, float ratio)
    {
        _activeEnemyCountText.text = activeEnemy;
        _activeEnemySlideImage.fillAmount = ratio;
    }
}
