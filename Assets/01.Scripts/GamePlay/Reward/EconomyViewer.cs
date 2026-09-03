using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class EconomyViewer : MonoBehaviour
{
    [SerializeField] private TMP_Text _moneyText;

    public void UpdateMoneyText(int money)
    {
        _moneyText.text = $"$ {money}";
    }
}
