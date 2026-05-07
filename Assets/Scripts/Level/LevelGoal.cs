using UnityEngine;

// Ставится на Trigger-коллайдер, отмечающий выход уровня.
public class LevelGoal : MonoBehaviour
{
    [SerializeField] private ParticleSystem goalVFX;
    [SerializeField] private float completionDelay = 0.8f;

    private bool _triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered || !other.CompareTag("Player")) return;
        _triggered = true;

        // Unity-овский перегруженный == null корректно обрабатывает unassigned SerializeField'ы;
        // C#-ный `?.` обошёл бы эту проверку и кинул UnassignedReferenceException.
        if (goalVFX != null)
        {
            goalVFX.Stop();
            goalVFX.Play();
        }

        Invoke(nameof(Complete), completionDelay);
    }

    private void Complete()
    {
        GameManager.Instance.LevelReached();
        UIManager.Instance.ShowLevelComplete();
    }
}
