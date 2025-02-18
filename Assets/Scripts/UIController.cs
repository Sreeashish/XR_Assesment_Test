using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public CanvasGroup enemyLifeCanvas, endScreen, HUD;
    public Image enemyLifeFillBar;
    public TMP_Text playerLife, endMessage;
    public Button restartBtn;

    float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }


    void Start()
    {
        Reset();
    }

    public void EnemyHealthAppear()
    {
        if(enemyLifeCanvas.alpha<=0)
        CanvasOnOrOff(enemyLifeCanvas);
    }

    public void EndScreen(string message)
    {
        MainController.instance.EndGame();
        restartBtn.onClick.RemoveAllListeners();
        restartBtn.onClick.AddListener(MainController.instance.RestartGame);
        endMessage.text = message;
        CanvasOnOrOff(endScreen);
        CanvasOnOrOff(HUD, false);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CalculateEnemyFillbar(float amount)
    {
        enemyLifeFillBar.fillAmount = Remap(amount, 0, 100, 0, 1);
    }

    public void DisplayPlayerHealth(float amount)
    {
        playerLife.text = ((int)amount).ToString();
    }

    void CanvasOnOrOff(CanvasGroup canvas, bool on = true)
    {
        if (on)
        {
            canvas.alpha = 1;
            canvas.interactable = true;
            canvas.blocksRaycasts = true;
        }
        else
        {
            canvas.alpha = 0;
            canvas.interactable = false;
            canvas.blocksRaycasts = false;
        }
    }

    public void Reset()
    {
        CanvasOnOrOff(HUD);
        CanvasOnOrOff(endScreen, false);
        CanvasOnOrOff(enemyLifeCanvas, false);
    }
}
