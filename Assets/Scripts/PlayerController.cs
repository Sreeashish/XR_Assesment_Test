using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public enum PlayerState { Idle, Walking, Running, Jumping, Dead }
public class PlayerController : MonoBehaviour
{
    [Header("Player Attributes")]
    public bool isControllable;
    public float life, maxLife, walkingSpeed, sprintSpeed;
    public Transform playerTransform;
    public CharacterController controller;
    public PlayerState playerState;
    public KeyCode sprintKey;
    public Animator playerAnimations;
    public PostProcessVolume volume;
    Vignette vignette;


    [Header("Camera Attributes")]
    public Transform cameraTransform;
    public Camera mainCamera;
    public float mouseSensitivity, camUpClamp, camDownClamp;

    [Header("Combat Attributes")]
    public PlayerCombatController combatController;
    float xRotation = 0;


    void Start()
    {
        Reset();
    }

    void Update()
    {
        PlayerMovement(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        PlayerLook();
        CombatControls();
    }


    public void PlayerMovement(float horizontal, float vertical)
    {
        if (isControllable)
        {
            Vector3 direction = playerTransform.TransformDirection(new (horizontal, 0f, vertical)).normalized;

            if (direction.magnitude >= 0.1f)
            {
                    if (Input.GetKey(sprintKey))
                    {
                        controller.Move(direction.normalized * sprintSpeed * Time.deltaTime);
                        SetPlayerState(PlayerState.Running);
                    }
                    else
                    {
                        controller.Move(direction.normalized * walkingSpeed * Time.deltaTime);
                        SetPlayerState(PlayerState.Walking);
                    }
            }
            else
            {
                SetPlayerState(PlayerState.Idle);
            }
        }
    }

    public void PlayerLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        playerTransform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, camDownClamp, camUpClamp);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void CombatControls()
    {
        combatController.CastSpell();
    }

    public void SetPlayerState(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Idle:
                playerState = PlayerState.Idle;
                SetAnimatorState(false, false, false);
                StartCoroutine(SmoothFovChange(75, 0.25f));
                break;
            case PlayerState.Walking:
                playerState = PlayerState.Walking;
                SetAnimatorState(true, false, false);
                StartCoroutine(SmoothFovChange(75, 0.25f));
                break;
            case PlayerState.Running:
                playerState = PlayerState.Running;
                SetAnimatorState(false, true, false);
                StartCoroutine(SmoothFovChange(90, 0.25f));
                break;
            case PlayerState.Dead:
                playerState = PlayerState.Dead;
                break;
        }
    }

    void SetAnimatorState(bool walk, bool run, bool attack)
    {
        playerAnimations.SetBool("Walk", walk);
        playerAnimations.SetBool("Run", run);
        playerAnimations.SetBool("Attack", attack);
    }

    public void LifeDepletion(float amount)
    {
        life -= amount;
        life = Mathf.Clamp(life, 0, maxLife);
        MainController.instance.uIController.DisplayPlayerHealth(life);
        StartCoroutine(FlashDamageEffect());

        if (life <= 0)
        {
            MainController.instance.uIController.EndScreen("Game Over");
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Fire"))
        {
            LifeDepletion(10 * Time.deltaTime);
        }
    }

    public IEnumerator SmoothFovChange(float target, float time)
    {
        if (mainCamera.fieldOfView != target)
        {
            float startFov = mainCamera.fieldOfView;
            float elapsedTime = 0f;

            while (elapsedTime < time)
            {
                mainCamera.fieldOfView = Mathf.Lerp(startFov, target, elapsedTime / time);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            mainCamera.fieldOfView = target;
        }
    }

    void DamageEffect(float amount)
    {
         if (volume.profile.TryGetSettings<Vignette>(out vignette))
        {
            vignette.intensity.Override(amount);
        }
    }

    IEnumerator FlashDamageEffect()
    {
        yield return null;
        DamageEffect(0);
        yield return new WaitForSeconds(0.5f);
        DamageEffect(0.45f);
        yield return new WaitForSeconds(0.5f);
        DamageEffect(0);
    }

        void Reset()
    {
        isControllable = true;
        SetPlayerState(PlayerState.Idle);
        life = maxLife;
        MainController.instance.uIController.DisplayPlayerHealth(life);
        DamageEffect(0);
    }
}
