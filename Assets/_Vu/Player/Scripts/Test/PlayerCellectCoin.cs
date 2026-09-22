using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PlayerCellectCoin : MonoBehaviour
{
    [Header("UI Settings")]
    public TextMeshProUGUI coinText;
    public GameObject winPanel;

    [Header("Coin Stats")]
    public int currentCoins = 0;
    public int totalCoins = 3;

    void Start()
    {
        Time.timeScale = 1f;

        UpdateCoinUI();

        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Coin"))
        {
            currentCoins++;
            UpdateCoinUI();

            Destroy(other.gameObject);

            if (currentCoins >= totalCoins)
            {
                WinGame();
            }
        }
    }
    void WinGame()
    {
        Debug.Log("You Win!");

        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }

        Cursor.visible = true;

        Time.timeScale = 0f;
    }
    void UpdateCoinUI()
    {
        if (coinText != null)
        {
            coinText.text = currentCoins + "/" + totalCoins;
        }
    }

    public void OnRestart()
    {
        Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}

