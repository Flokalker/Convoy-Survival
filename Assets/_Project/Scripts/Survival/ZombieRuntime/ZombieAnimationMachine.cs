using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[DisallowMultipleComponent]
public class ZombieAnimationMachine : MonoBehaviour
{
    private enum ClipSlot
    {
        Idle = 0,
        Walk = 1,
        Run = 2,
        Attack = 3,
        Death = 4
    }

    [SerializeField] private float blendSpeed = 7f;
    [SerializeField] private float runThreshold = 0.65f;
    [SerializeField] private bool randomizeStartTime = true;

    private Animator animator;
    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;
    private readonly float[] weights = new float[5];
    private readonly float[] targets = new float[5];
    private bool graphReady;
    private bool isDead;
    private float attackUntilTime;
    private AnimationClip attackClip;
    private AnimationClip deathClip;

    public void ConfigureRuntime(
        Animator targetAnimator,
        AnimationClip idle,
        AnimationClip walk,
        AnimationClip run,
        AnimationClip attack,
        AnimationClip death)
    {
        animator = targetAnimator;
        attackClip = attack;
        deathClip = death;
        BuildGraph(idle, walk, run, attack, death);
    }

    private void Update()
    {
        if (!graphReady)
        {
            return;
        }

        for (int i = 0; i < weights.Length; i++)
        {
            float w = Mathf.MoveTowards(weights[i], targets[i], blendSpeed * Time.deltaTime);
            weights[i] = w;
            mixer.SetInputWeight(i, w);
        }
    }

    private void OnDisable()
    {
        DestroyGraph();
    }

    private void OnDestroy()
    {
        DestroyGraph();
    }

    public void SetMotion(float speed01, bool isMoving, bool attacking)
    {
        if (!graphReady || isDead)
        {
            return;
        }

        if (Time.time < attackUntilTime || attacking)
        {
            SetExclusive(ClipSlot.Attack);
            return;
        }

        if (!isMoving)
        {
            SetExclusive(ClipSlot.Idle);
            return;
        }

        SetExclusive(speed01 >= runThreshold ? ClipSlot.Run : ClipSlot.Walk);
    }

    public void PlayAttack()
    {
        if (!graphReady || isDead)
        {
            return;
        }

        float length = attackClip != null && attackClip.length > 0.05f ? attackClip.length : 0.7f;
        attackUntilTime = Time.time + length * 0.9f;
        SetExclusive(ClipSlot.Attack);
    }

    public void PlayDeath()
    {
        if (!graphReady)
        {
            return;
        }

        isDead = true;
        SetExclusive(ClipSlot.Death);
    }

    private void SetExclusive(ClipSlot slot)
    {
        for (int i = 0; i < targets.Length; i++)
        {
            targets[i] = i == (int)slot ? 1f : 0f;
        }
    }

    private void BuildGraph(AnimationClip idle, AnimationClip walk, AnimationClip run, AnimationClip attack, AnimationClip death)
    {
        DestroyGraph();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = gameObject.AddComponent<Animator>();
            }
        }

        animator.enabled = true;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        AnimationClip idleFinal = idle != null ? idle : walk;
        AnimationClip walkFinal = walk != null ? walk : idleFinal;
        AnimationClip runFinal = run != null ? run : walkFinal;
        AnimationClip attackFinal = attack != null ? attack : walkFinal;
        AnimationClip deathFinal = death != null ? death : idleFinal;

        if (idleFinal == null || walkFinal == null || runFinal == null || attackFinal == null || deathFinal == null)
        {
            return;
        }

        graph = PlayableGraph.Create(name + "_ZombieAnimGraph");
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Animation", animator);
        mixer = AnimationMixerPlayable.Create(graph, 5, true);
        output.SetSourcePlayable(mixer);

        AddClipToMixer(idleFinal, ClipSlot.Idle, true);
        AddClipToMixer(walkFinal, ClipSlot.Walk, true);
        AddClipToMixer(runFinal, ClipSlot.Run, true);
        AddClipToMixer(attackFinal, ClipSlot.Attack, false);
        AddClipToMixer(deathFinal, ClipSlot.Death, false);

        SetExclusive(ClipSlot.Idle);
        weights[(int)ClipSlot.Idle] = 1f;
        for (int i = 0; i < weights.Length; i++)
        {
            mixer.SetInputWeight(i, weights[i]);
        }

        graph.Play();
        graphReady = true;
    }

    private void AddClipToMixer(AnimationClip clip, ClipSlot slot, bool loop)
    {
        clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
        AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);

        if (randomizeStartTime && loop && clip.length > 0.01f)
        {
            playable.SetTime(Random.Range(0f, clip.length));
        }

        graph.Connect(playable, 0, mixer, (int)slot);
    }

    private void DestroyGraph()
    {
        if (!graphReady)
        {
            return;
        }

        if (graph.IsValid())
        {
            graph.Destroy();
        }

        graphReady = false;
        isDead = false;
        attackUntilTime = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = 0f;
            targets[i] = 0f;
        }
    }
}
