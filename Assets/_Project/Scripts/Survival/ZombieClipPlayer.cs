using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[DisallowMultipleComponent]
public class ZombieClipPlayer : MonoBehaviour
{
    [SerializeField] private AnimationClip clip;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool randomStartTime = true;

    private Animator animator;
    private PlayableGraph graph;
    private bool graphValid;

    public void SetClip(AnimationClip animationClip)
    {
        clip = animationClip;
        RebuildGraph();
    }

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }

        animator.enabled = true;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    private void OnEnable()
    {
        RebuildGraph();
    }

    private void OnDisable()
    {
        DestroyGraph();
    }

    private void OnDestroy()
    {
        DestroyGraph();
    }

    private void RebuildGraph()
    {
        DestroyGraph();

        if (!enabled || clip == null || animator == null)
        {
            return;
        }

        clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;

        graph = PlayableGraph.Create($"{name}_ZombieClipGraph");
        var output = AnimationPlayableOutput.Create(graph, "Animation", animator);
        var clipPlayable = AnimationClipPlayable.Create(graph, clip);

        if (randomStartTime && clip.length > 0.01f)
        {
            double randomTime = Random.Range(0f, clip.length);
            clipPlayable.SetTime(randomTime);
        }

        output.SetSourcePlayable(clipPlayable);
        graph.Play();
        graphValid = true;
    }

    private void DestroyGraph()
    {
        if (!graphValid)
        {
            return;
        }

        if (graph.IsValid())
        {
            graph.Destroy();
        }

        graphValid = false;
    }
}
