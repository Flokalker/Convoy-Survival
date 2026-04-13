using UnityEngine;

[DisallowMultipleComponent]
public class SurvivalWeaponView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;

    [Header("Animator Triggers")]
    [SerializeField] private string fireTrigger = "Fire";
    [SerializeField] private string meleeTrigger = "Melee";
    [SerializeField] private string reloadTrigger = "Reload";

    public Transform MuzzlePoint => muzzlePoint != null ? muzzlePoint : transform;

    public void ConfigureRuntime(Transform runtimeMuzzlePoint, AudioSource runtimeAudioSource)
    {
        muzzlePoint = runtimeMuzzlePoint;
        audioSource = runtimeAudioSource;
    }

    public void PlayRangedAttack(AudioClip attackClip)
    {
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        if (animator != null && !string.IsNullOrWhiteSpace(fireTrigger))
        {
            animator.SetTrigger(fireTrigger);
        }

        PlaySound(attackClip);
    }

    public void PlayMeleeAttack(AudioClip attackClip)
    {
        if (animator != null && !string.IsNullOrWhiteSpace(meleeTrigger))
        {
            animator.SetTrigger(meleeTrigger);
        }

        PlaySound(attackClip);
    }

    public void PlayReload()
    {
        if (animator != null && !string.IsNullOrWhiteSpace(reloadTrigger))
        {
            animator.SetTrigger(reloadTrigger);
        }
    }

    public void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
