using UnityEngine;
using DamageNumbersPro;
using PurrNet.Prediction;

public class DamageNumbersManager : StatelessPredictedIdentity
{
    // singleton needs more protection/validation at some point
    public static DamageNumbersManager instance;

    [SerializeField] private DamageNumber damageNumberAsset;
    [SerializeField] private PlayerHealth _playerHealth;

    private void Awake()
    {
        instance = this;
    }

    protected override void LateAwake()
    {
        base.LateAwake();

        _playerHealth._onDamageTaken.AddListener(OnDamageTaken);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_playerHealth != null)
        {
            _playerHealth._onDamageTaken.RemoveListener(OnDamageTaken);
        }
    }

    private void OnDamageTaken(PlayerHealth.DamageInfo damageInfo)
    {
        SpawnNumber(damageInfo.position, damageInfo.damage);
    }

    public void SpawnNumber(Vector3 position, float damage, Color? color = null)
    {
        damageNumberAsset.Spawn(position, damage);

        // in the future it will be better to define a set of color associations for different debuffs and pass the debuff as a parameter instead
        Color defaultColor = new Color(1f,1f, 1f, 1f);
        Color finalColor = color.HasValue ? color.Value : defaultColor;
        damageNumberAsset.SetColor(finalColor);
    }
}
