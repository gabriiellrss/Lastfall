using UnityEngine;
using UnityEngine.Playables;
using Unity.Cinemachine;

public class EndCutsceneTrigger : MonoBehaviour
{
    [Header("Referência do Player")]
    public GameObject playerObject;
    public RuntimeAnimatorController animatorController;

    [Header("Câmeras Cinemachine")]
    public CinemachineCamera cutsceneVirtualCamera;
    public CinemachineCamera gameplayVirtualCamera;

    [Header("Timeline (Cutscene)")]
    public PlayableDirector cutsceneDirector;

    private CharacterController characterController;
    private Animator animator;
    private MonoBehaviour playerScript;
    private AudioSource[] audioSources;

    void Start()
    {
        if (cutsceneDirector != null)
        {
            cutsceneDirector.stopped += OnCutsceneEnded;
        }

        if (playerObject != null)
        {
            characterController = playerObject.GetComponent<CharacterController>();
            animator = playerObject.GetComponent<Animator>();
            playerScript = playerObject.GetComponent<MonoBehaviour>(); // Se quiser buscar por tipo específico, posso mudar
            audioSources = playerObject.GetComponents<AudioSource>();
        }
        else
        {
            Debug.LogError("PlayerObject não foi atribuído no inspector.");
        }
    }

    void OnCutsceneEnded(PlayableDirector director)
    {
        if (director != cutsceneDirector) return;

        // Ativar componentes do player
        if (characterController != null) characterController.enabled = true;
        if (playerScript != null) playerScript.enabled = true;

        if (audioSources != null)
        {
            foreach (var audio in audioSources)
            {
                audio.enabled = true;
            }
        }

        // Controlar Animator
        if (animator != null)
        {
            if (animator.runtimeAnimatorController == null)
            {
                animator.runtimeAnimatorController = animatorController;
                animator.applyRootMotion = false;
            }
            else
            {
                animator.runtimeAnimatorController = null;
                animator.applyRootMotion = true;
            }
        }

        // Trocar prioridade das câmeras Cinemachine
        if (cutsceneVirtualCamera != null) cutsceneVirtualCamera.Priority = 0;
        if (gameplayVirtualCamera != null) gameplayVirtualCamera.Priority = 10;

        Debug.Log("Cutscene finalizada. Gameplay ativado com Cinemachine.");
    }
}
