using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainController : MonoBehaviour
{
    public PlayerController playerController;
    public EnemyController enemyController;
    public UIController uIController;

    public static MainController instance;
    void Awake()
    {
        if (instance == null)
            instance = this;
    }
    void Start()
    {
        Reset();
    }

    public void EndGame()
    {
        playerController.enabled = false;
        enemyController.enabled = false;
    }

    void Reset()
    {
        Application.targetFrameRate = 60;
        playerController.enabled = true;
        enemyController.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
