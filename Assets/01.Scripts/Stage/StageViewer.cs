using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface IStageView
{
    event Action OnClickSpawnMiddBoss;
    void ShowMiddBossButton();
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

    [SerializeField] private Button _spawnMiddBossButton;

    public event Action OnClickSpawnMiddBoss;

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

    public void ShowMiddBossButton()
    {
        _spawnMiddBossButton.gameObject.SetActive(true);
    }
    public void OnClickSpawnMiddBossButton()
    {
        _spawnMiddBossButton.gameObject.SetActive(false);
        OnClickSpawnMiddBoss?.Invoke();
    }
}
